using System;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     キューブマップ生成、ブラー処理、およびプレビュー画面更新（デバウンス制御含む）のパイプラインを一元管理するコントローラー。
    /// </summary>
    public sealed class RenderingFlowController : IDisposable
    {
        private const double InteractiveThrottleIntervalSec = 0.033; // 約 30fps 上限
        private const double ContinuousOperationThresholdSec = 0.15; // 150ms 以内の連続リクエストはドラッグ等の連続操作と判定
        private readonly EditorState _editorState;
        private readonly VisualElement _schedulerRoot;
        private readonly Settings _settings;
        private readonly ViewportController _viewportController;
        private readonly Worker _worker;

        private RenderTexture _activeCubemapTexture = null!;
        private RenderTexture _blurredCubeRTCache = null!;
        private CancellationToken _externalPipelineToken;
        private bool _hasPendingFullRender;
        private IVisualElementScheduledItem? _highQualityDebounceTask;
        private HighQualityReadyPromise? _highQualityReadyPromise;
        private IVisualElementScheduledItem? _interactiveThrottleTask;
        private bool _isCurrentRunInteractive;
        private bool _isDebouncing;
        private bool _isDisposed;
        private bool _isPipelineRunning;
        private double _lastInteractiveRenderTime;
        private double _lastRequestTime;
        private CubemapRenderParameters _pendingParameters;

        private PendingRequest _pendingRequest = PendingRequest.None;
        private CancellationTokenSource? _pipelineCts;
        private IVisualElementScheduledItem? _redrawDebounceTask;
        private IVisualElementScheduledItem? _resizeDebounceTask;

        public RenderingFlowController(
            VisualElement schedulerRoot,
            EditorState editorState,
            Settings settings,
            CubemapRenderTexturePool rtPool,
            Camera captureCamera,
            ViewportController viewportController,
            Func<Camera> getCurrentCamera)
        {
            _schedulerRoot = schedulerRoot;
            _editorState = editorState;
            _settings = settings;
            _viewportController = viewportController;

            Assert.IsNotNull(_schedulerRoot, $"{nameof(_schedulerRoot)} is null.");
            Assert.IsNotNull(_editorState, $"{nameof(_editorState)} is null.");
            Assert.IsNotNull(_settings, $"{nameof(_settings)} is null.");
            Assert.IsNotNull(rtPool, $"{nameof(rtPool)} is null.");
            Assert.IsNotNull(captureCamera, $"{nameof(captureCamera)} is null.");
            Assert.IsNotNull(_viewportController, $"{nameof(_viewportController)} is null.");
            Assert.IsNotNull(getCurrentCamera, $"{nameof(getCurrentCamera)} is null.");

            _worker = new Worker(
                _settings,
                rtPool,
                captureCamera,
                getCurrentCamera,
                msg => OnWarning?.Invoke(msg));
        }

        public RenderTexture CurrentCubemapTexture =>
            _activeCubemapTexture != null
                ? _activeCubemapTexture
                : SourceCubemapTexture != null
                    ? SourceCubemapTexture
                    : null!;

        public RenderTexture SourceCubemapTexture { get; private set; } = null!;

        public bool IsProcessing => _isPipelineRunning || _worker.IsProcessing;
        public bool IsExporting { get; private set; }

        public bool IsCurrentTextureHighQuality { get; private set; }
        public Awaitable? RunningPipelineTask { get; private set; }

        public void Dispose()
        {
            _isDisposed = true;
            IsExporting = false; // Dispose 時に残留しないよう明示リセット
            _pendingRequest = PendingRequest.None;
            _hasPendingFullRender = false;
            _isDebouncing = false;

            _highQualityReadyPromise?.TrySetCanceled();
            _highQualityReadyPromise = null;

            _pipelineCts?.Cancel();
            _pipelineCts?.Dispose();
            _pipelineCts = null;

            _highQualityDebounceTask?.Pause();
            _highQualityDebounceTask = null;
            _interactiveThrottleTask?.Pause();
            _interactiveThrottleTask = null;
            _redrawDebounceTask?.Pause();
            _redrawDebounceTask = null;
            _resizeDebounceTask?.Pause();
            _resizeDebounceTask = null;

            _worker?.Dispose();

            _blurredCubeRTCache = null!;
            SourceCubemapTexture = null!;
            _activeCubemapTexture = null!;
            IsCurrentTextureHighQuality = false;

            OnIntermediateSizeChanged = null;
            OnWarning = null;
            OnProcessingStateChanged = null;
            OnPipelineCompleted = null;
        }

        /// <summary>
        ///     エクスポート開始を通知する。呼び出し後は RequestRenderCubemap / RequestRenderBlurredCubemap が弾かれる。
        ///     必ず対になる EndExporting() を呼ぶこと。
        /// </summary>
        public void BeginExporting()
        {
            IsExporting = true;
        }

        /// <summary>
        ///     エクスポート終了を通知する。BeginExporting() と必ず対で呼ぶこと。
        /// </summary>
        public void EndExporting()
        {
            IsExporting = false;
        }

        public event Action<Vector2Int>? OnIntermediateSizeChanged;
        public event Action<string?>? OnWarning;
        public event Action<bool, bool>? OnProcessingStateChanged; // (isProcessing, isExportReady)
        public event Action? OnPipelineCompleted;

        private void SetProcessingState(bool isProcessing, bool isExportReady)
        {
            if (_isDisposed)
            {
                return;
            }

            OnProcessingStateChanged?.Invoke(isProcessing, isExportReady);
        }

        public void RequestRedraw()
        {
            if (_isDisposed)
            {
                return;
            }

            _redrawDebounceTask?.Pause();
            _redrawDebounceTask = _schedulerRoot.schedule
                .Execute(() =>
                {
                    if (!_isDisposed)
                    {
                        _viewportController.Render();
                    }
                })
                .StartingIn(Mathf.Max(_settings.RedrawDebounceTimeMs, 1));
        }

        /// <summary>
        ///     デバウンス（遅延）を待たずに即座にプレビュー描画を実行します。
        /// </summary>
        public void RedrawImmediate()
        {
            if (_isDisposed)
            {
                return;
            }

            _redrawDebounceTask?.Pause();
            _viewportController.Render();
        }

        public void RequestRenderCubemap()
        {
            RequestPipeline(false);
        }

        public void RequestRenderBlurredCubemap()
        {
            RequestPipeline(true);
        }

        private void RequestPipeline(bool blurOnly)
        {
            if (_isDisposed || IsExporting || _viewportController.IsDragging || _highQualityReadyPromise != null)
            {
                return;
            }

            SetProcessingState(true, false);

            if (!blurOnly)
            {
                _hasPendingFullRender = true;
            }

            _pendingParameters = CubemapRenderParameters.CreateSnapshot(_editorState, _settings);

            var now = EditorApplication.timeSinceStartup;
            var timeSinceLastRequest = now - _lastRequestTime;
            _lastRequestTime = now;

            // 高品質描画の実行中に次のリクエストが届いた場合、直ちにキャンセルしてデバウンスモードに移行
            if (_isPipelineRunning && !_isCurrentRunInteractive)
            {
                _pipelineCts?.Cancel();
                _isDebouncing = true;
            }
            // 前回の要求から短時間（150ms以内）に連続して届いた場合も連続操作（デバウンス中）と判定
            else if (timeSinceLastRequest < ContinuousOperationThresholdSec)
            {
                _isDebouncing = true;
            }

            if (_isDebouncing)
            {
                // デバウンスモード中は保留中の未実行要求をインタラクティブに統一
                if (_pendingRequest == PendingRequest.RenderCubemap)
                {
                    _pendingRequest = PendingRequest.RenderCubemapInteractive;
                }
                else if (_pendingRequest == PendingRequest.BlurOnly)
                {
                    _pendingRequest = PendingRequest.BlurOnlyInteractive;
                }

                // 連続操作（デバウンス中）: 操作停止検知用の高品質タスクを再スケジュールし、低解像度スロットリングで追従
                ScheduleHighQualityDebounce();
                ScheduleInteractiveThrottle();
            }
            else
            {
                // 単発リクエスト（非デバウンス中）:
                // 低解像度スロットリングを挟まず、直ちに高解像度（非インタラクティブ）で 1 回描画して完結させる
                _highQualityDebounceTask?.Pause();
                _highQualityDebounceTask = null;
                _interactiveThrottleTask?.Pause();
                _interactiveThrottleTask = null;

                var runBlurOnly = !_hasPendingFullRender;
                _hasPendingFullRender = false;
                RunPipeline(runBlurOnly);
            }
        }

        private void ScheduleHighQualityDebounce()
        {
            _highQualityDebounceTask?.Pause();
            var debounceMs = _hasPendingFullRender
                ? _settings.RenderDebounceTimeMs
                : _settings.BlurDebounceTimeMs;
            // スロットリング（約33ms）との競合を防ぎ、操作停止後に確実に高品質描画へ遷移するため最低100msを確保
            var safeDebounceMs = Mathf.Max(debounceMs, 100);

            _highQualityDebounceTask = _schedulerRoot.schedule
                .Execute(ExecuteHighQualityPipeline)
                .StartingIn(safeDebounceMs);
        }

        private void ScheduleInteractiveThrottle()
        {
            var now = EditorApplication.timeSinceStartup;
            var elapsed = now - _lastInteractiveRenderTime;

            if (elapsed >= InteractiveThrottleIntervalSec)
            {
                _lastInteractiveRenderTime = now;
                _interactiveThrottleTask?.Pause();
                _schedulerRoot.schedule.Execute(ExecuteInteractivePipeline);
            }
            else
            {
                var delayMs = (int)((InteractiveThrottleIntervalSec - elapsed) * 1000);
                _interactiveThrottleTask?.Pause();
                _interactiveThrottleTask = _schedulerRoot.schedule
                    .Execute(() =>
                    {
                        _lastInteractiveRenderTime = EditorApplication.timeSinceStartup;
                        ExecuteInteractivePipeline();
                    })
                    .StartingIn(Mathf.Max(delayMs, 1));
            }
        }

        private void ExecuteInteractivePipeline()
        {
            if (_isDisposed || IsExporting)
            {
                return;
            }

            var blurOnly = !_hasPendingFullRender;
            RunPipeline(blurOnly, true);
        }

        private void ExecuteHighQualityPipeline()
        {
            if (_isDisposed || IsExporting)
            {
                return;
            }

            _isDebouncing = false;

            // 高品質描画が確定したため、保留中の遅延インタラクティブタスクを完全破棄して低解像度での上書きを物理的に阻止する
            _interactiveThrottleTask?.Pause();
            _interactiveThrottleTask = null;

            var blurOnly = !_hasPendingFullRender;
            _hasPendingFullRender = false;

            RunPipeline(blurOnly);
        }

        public void RequestResize(float width, float height)
        {
            if (_isDisposed)
            {
                return;
            }

            _resizeDebounceTask?.Pause();
            _resizeDebounceTask = _schedulerRoot.schedule
                .Execute(() =>
                {
                    if (!_isDisposed)
                    {
                        _viewportController.ResizePreview(width, height);
                        _viewportController.Render();
                    }
                })
                .StartingIn(Mathf.Max(_settings.ResizeDebounceTimeMs, 1));
        }

        private bool IsBlurNeeded()
        {
            if (SourceCubemapTexture == null)
            {
                return false;
            }

            var blurRoughness = _editorState.BlurAlgorithm == CubemapBlurAlgorithm.GGX_SpecularIBL
                ? _editorState.BlurRoughness_GGX_SpecularIBL
                : _editorState.BlurRoughness_Gaussian_Bokeh;
            var blurNumSamples = _editorState.BlurAlgorithm == CubemapBlurAlgorithm.GGX_SpecularIBL
                ? _editorState.BlurNumSamples_GGX_SpecularIBL
                : _editorState.BlurNumSamples_Gaussian_Bokeh;

            return CubemapBlurUtility.IsBlurApplicable(_editorState.BlurEnable, blurRoughness, blurNumSamples);
        }

        /// <summary>
        ///     保留中のデバウンスタスクがあれば停止し、完全品質（非インタラクティブ、フル解像度・フルサンプル）のテクスチャを強制生成して待機します。
        /// </summary>
        public Awaitable EnsureHighQualityReadyAsync(CancellationToken cancellationToken)
        {
            return EnsureHighQualityReadyAsync(0, cancellationToken);
        }

        /// <summary>
        ///     保留中のデバウンスタスクがあれば停止し、指定解像度または設定解像度での完全品質（非インタラクティブ、フルサンプル）のテクスチャを強制生成して待機します。
        /// </summary>
        public async Awaitable EnsureHighQualityReadyAsync(int targetResolution = 0,
            CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
            {
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            _highQualityDebounceTask?.Pause();
            _interactiveThrottleTask?.Pause();
            _interactiveThrottleTask = null;
            _hasPendingFullRender = false;
            _isDebouncing = false;

            if (targetResolution <= 0)
            {
                targetResolution = _editorState.InputMode == InputModeType.CurrentScene &&
                                   _editorState.OutputResolution > 0
                    ? _editorState.OutputResolution
                    : SourceCubemapTexture != null
                        ? SourceCubemapTexture.width
                        : _editorState.OutputResolution > 0
                            ? _editorState.OutputResolution
                            : _settings.DefaultOutputResolution;
            }
            else if (_editorState.InputMode != InputModeType.CurrentScene)
            {
                targetResolution = SourceCubemapTexture != null
                    ? SourceCubemapTexture.width
                    : targetResolution;
            }

            var isTextureMatchingResolution =
                _activeCubemapTexture != null && _activeCubemapTexture.width == targetResolution;

            if (IsCurrentTextureHighQuality && isTextureMatchingResolution && !_isPipelineRunning &&
                _pendingRequest == PendingRequest.None)
            {
                return;
            }

            var canReuseSourceRT = SourceCubemapTexture != null &&
                                   !(_editorState.InputMode == InputModeType.CurrentScene &&
                                     SourceCubemapTexture.width != targetResolution);

            // 元テクスチャを再利用可能であればブラー処理のみ先行実行
            var blurOnly = IsBlurNeeded() && canReuseSourceRT;

            _externalPipelineToken = cancellationToken;
            _highQualityReadyPromise?.TrySetCanceled();
            var promise = new HighQualityReadyPromise();
            _highQualityReadyPromise = promise;

            using var registration = cancellationToken.CanBeCanceled
                ? cancellationToken.Register(() =>
                {
                    _pipelineCts?.Cancel();
                    promise.TrySetCanceled();
                })
                : default;

            try
            {
                RunPipeline(blurOnly, false, true, targetResolution);
                await promise.Awaitable;
                cancellationToken.ThrowIfCancellationRequested();
            }
            finally
            {
                if (_highQualityReadyPromise == promise)
                {
                    _highQualityReadyPromise = null;
                }

                _externalPipelineToken = default;
            }
        }

        private void RunPipeline(bool blurOnly, bool isInteractive = false, bool force = false,
            int overrideResolution = 0)
        {
            if (_isDisposed || (IsExporting && !force) || (_highQualityReadyPromise != null && isInteractive))
            {
                return;
            }

            var newRequest = blurOnly
                ? isInteractive ? PendingRequest.BlurOnlyInteractive : PendingRequest.BlurOnly
                : isInteractive
                    ? PendingRequest.RenderCubemapInteractive
                    : PendingRequest.RenderCubemap;

            // パラメータスナップショットをリクエスト時点で確定する（非同期実行中のUI変更による混入を防ぐ）
            var snapshot = CubemapRenderParameters.CreateSnapshot(_editorState, _settings, overrideResolution);
            _pendingRequest = MergePendingRequest(_pendingRequest, newRequest);
            _pendingParameters = snapshot;

            if (_isPipelineRunning)
            {
                // エクスポート前準備など強制実行の場合は直ちに先行処理をキャンセルして割り込む
                if (force)
                {
                    _pipelineCts?.Cancel();
                    return;
                }

                // 既にインタラクティブ描画が進行中かつ新リクエストもインタラクティブの場合は
                // 実行中タスクをキャンセルせず完走させ、最新のリクエストを合流（Coalescing）して次回ループで処理する
                if (_isCurrentRunInteractive && isInteractive)
                {
                    return;
                }

                _pipelineCts?.Cancel();
                return;
            }

            RunningPipelineTask = ProcessPipelineQueueAsync();
        }

        private PendingRequest MergePendingRequest(PendingRequest current, PendingRequest incoming)
        {
            if (current == PendingRequest.None)
            {
                return incoming;
            }

            if (incoming == PendingRequest.None)
            {
                return current;
            }

            var requiresRender = current == PendingRequest.RenderCubemap ||
                                 current == PendingRequest.RenderCubemapInteractive ||
                                 incoming == PendingRequest.RenderCubemap ||
                                 incoming == PendingRequest.RenderCubemapInteractive;

            // どちらか一方でも高品質（非インタラクティブ）要求が含まれる場合は、高品質を維持する
            var isCurrentInteractive = current == PendingRequest.RenderCubemapInteractive ||
                                       current == PendingRequest.BlurOnlyInteractive;
            var isIncomingInteractive = incoming == PendingRequest.RenderCubemapInteractive ||
                                        incoming == PendingRequest.BlurOnlyInteractive;
            var isInteractive = _highQualityReadyPromise == null && isCurrentInteractive && isIncomingInteractive;

            if (requiresRender)
            {
                return isInteractive ? PendingRequest.RenderCubemapInteractive : PendingRequest.RenderCubemap;
            }

            return isInteractive ? PendingRequest.BlurOnlyInteractive : PendingRequest.BlurOnly;
        }

        private async Awaitable ProcessPipelineQueueAsync()
        {
            if (_isDisposed)
            {
                return;
            }

            _isPipelineRunning = true;
            SetProcessingState(true, false);

            try
            {
                using (new PlayerLoopUpdateScope())
                {
                    while (!_isDisposed && _pendingRequest != PendingRequest.None)
                    {
                        var req = _pendingRequest;
                        _pendingRequest = PendingRequest.None;
                        var parameters = _pendingParameters; // スナップショットをイテレーション先頭で確定

                        _pipelineCts?.Dispose();
                        _pipelineCts = _externalPipelineToken.CanBeCanceled
                            ? CancellationTokenSource.CreateLinkedTokenSource(_externalPipelineToken)
                            : new CancellationTokenSource();
                        var token = _pipelineCts.Token;

                        var isBlurOnly = req == PendingRequest.BlurOnly || req == PendingRequest.BlurOnlyInteractive;
                        var isInteractive = req == PendingRequest.RenderCubemapInteractive ||
                                            req == PendingRequest.BlurOnlyInteractive;
                        _isCurrentRunInteractive = isInteractive;

                        try
                        {
                            if (!isBlurOnly || SourceCubemapTexture == null)
                            {
                                var cubeRT = _worker.StartRenderCubemap(parameters, isInteractive, token);
                                if (_isDisposed || token.IsCancellationRequested)
                                {
                                    continue;
                                }

                                if (cubeRT == null)
                                {
                                    // 設定が無効などの理由で正当に失敗した場合のみクリアする
                                    if (_pendingRequest == PendingRequest.None)
                                    {
                                        SourceCubemapTexture = null!;
                                        _blurredCubeRTCache = null!;
                                        _activeCubemapTexture = null!;
                                        IsCurrentTextureHighQuality = false;
                                        _viewportController.ApplyCubemap(null!);
                                        _viewportController.Render();
                                        OnIntermediateSizeChanged?.Invoke(Vector2Int.zero);
                                    }

                                    continue;
                                }

                                SourceCubemapTexture = cubeRT;
                                OnIntermediateSizeChanged?.Invoke(new Vector2Int(SourceCubemapTexture.width,
                                    SourceCubemapTexture.height));
                            }

                            if (parameters.IsBlurNeeded(SourceCubemapTexture))
                            {
                                var blurredRT =
                                    await _worker.StartBlurredCubemapAsync(parameters, isInteractive,
                                        SourceCubemapTexture,
                                        token);
                                if (_isDisposed || token.IsCancellationRequested)
                                {
                                    continue;
                                }

                                if (blurredRT == null)
                                {
                                    if (_pendingRequest == PendingRequest.None)
                                    {
                                        _blurredCubeRTCache = null!;
                                        _activeCubemapTexture = null!;
                                        IsCurrentTextureHighQuality = false;
                                        _viewportController.ApplyCubemap(null!);
                                        _viewportController.Render();
                                    }

                                    continue;
                                }

                                _blurredCubeRTCache = blurredRT;
                                _activeCubemapTexture = _blurredCubeRTCache;
                            }
                            else
                            {
                                _blurredCubeRTCache = null!;
                                _activeCubemapTexture = SourceCubemapTexture;
                            }

                            IsCurrentTextureHighQuality = !isInteractive && _activeCubemapTexture != null;

                            _viewportController.ApplyCubemap(_activeCubemapTexture!);
                            _viewportController.Render();
                            OnPipelineCompleted?.Invoke();

                            if (IsCurrentTextureHighQuality && _highQualityReadyPromise != null)
                            {
                                var p = _highQualityReadyPromise;
                                _highQualityReadyPromise = null;
                                p.TrySetResult();
                            }
                        }
                        catch (OperationCanceledException)
                        {
                        }
                        catch (ObjectDisposedException)
                        {
                            return;
                        }
                        catch (Exception ex)
                        {
                            if (!_isDisposed)
                            {
                                Debug.LogException(ex);
                                OnWarning?.Invoke($"Pipeline error: {ex.Message}");
                            }
                        }
                    }
                }
            }
            finally
            {
                _isCurrentRunInteractive = false;
                _isPipelineRunning = false;
                RunningPipelineTask = null;
                _pipelineCts?.Dispose();
                _pipelineCts = null;

                if (!_isDisposed)
                {
                    if (_pendingRequest != PendingRequest.None)
                    {
                        var req = _pendingRequest;
                        _pendingRequest = PendingRequest.None;
                        var isBlurOnly = req == PendingRequest.BlurOnly || req == PendingRequest.BlurOnlyInteractive;
                        var isInteractive = _highQualityReadyPromise == null &&
                                            (req == PendingRequest.RenderCubemapInteractive ||
                                             req == PendingRequest.BlurOnlyInteractive);
                        var isForced = _highQualityReadyPromise != null;
                        RunPipeline(isBlurOnly, isInteractive, isForced, _pendingParameters.OutputResolution);
                    }
                    else
                    {
                        SetProcessingState(false, CurrentCubemapTexture != null);
                        if (_highQualityReadyPromise != null)
                        {
                            var p = _highQualityReadyPromise;
                            _highQualityReadyPromise = null;
                            if (IsCurrentTextureHighQuality)
                            {
                                p.TrySetResult();
                            }
                            else
                            {
                                p.TrySetException(new InvalidOperationException(
                                    "Pipeline completed without producing high quality texture."));
                            }
                        }
                    }
                }
                else
                {
                    _highQualityReadyPromise?.TrySetCanceled();
                    _highQualityReadyPromise = null;
                }
            }
        }

        private sealed class HighQualityReadyPromise
        {
            private readonly AwaitableCompletionSource _acs = new();
            private int _completed;

            public Awaitable Awaitable => _acs.Awaitable;

            public bool TrySetResult()
            {
                if (Interlocked.Exchange(ref _completed, 1) == 0)
                {
                    _acs.SetResult();
                    return true;
                }

                return false;
            }

            public bool TrySetCanceled()
            {
                if (Interlocked.Exchange(ref _completed, 1) == 0)
                {
                    _acs.SetCanceled();
                    return true;
                }

                return false;
            }

            public bool TrySetException(Exception ex)
            {
                if (Interlocked.Exchange(ref _completed, 1) == 0)
                {
                    _acs.SetException(ex);
                    return true;
                }

                return false;
            }
        }

        private enum PendingRequest
        {
            None,
            BlurOnly,
            BlurOnlyInteractive,
            RenderCubemap,
            RenderCubemapInteractive
        }
    }
}
