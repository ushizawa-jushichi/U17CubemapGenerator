using System;
using System.IO;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     エクスポートの実行フロー（ダイアログ表示、パイプライン待機、処理のキック、UI無効化の制御）を管理するクラス。
    /// </summary>
    public sealed class ExportFlowController : IDisposable
    {
        private readonly CameraTracker _cameraTracker;
        private readonly EditorState _editorState;
        private readonly GeneratorTabPage _generatorTabPage;
        private readonly RenderingFlowController _pipelineController;
        private readonly Settings _settings;
        private CancellationTokenSource? _exportCts;

        private bool _isExporting;

        public ExportFlowController(
            EditorState editorState,
            Settings settings,
            RenderingFlowController pipelineController,
            CameraTracker cameraTracker,
            GeneratorTabPage generatorTabPage)
        {
            _editorState = editorState;
            _settings = settings;
            _pipelineController = pipelineController;
            _cameraTracker = cameraTracker;
            _generatorTabPage = generatorTabPage;
        }

        public void Dispose()
        {
            _exportCts?.Cancel();
            _exportCts?.Dispose();
            _exportCts = null;
            if (_isExporting)
            {
                _isExporting = false;
                _pipelineController?.EndExporting();
            }
        }

        public async Awaitable OnExportAsync()
        {
            try
            {
                await ExportAsync();
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        /// <summary>
        ///     現在レンダリングされているキューブマップを非同期でエクスポートする。
        /// </summary>
        /// <returns>エクスポートが正常に完了した場合は true、キャンセルまたは失敗した場合は false</returns>
        public async Awaitable<bool> ExportAsync()
        {
            if (_isExporting)
            {
                return false;
            }

            if (!CubemapPathUtility.IsOutputSizeValid(_editorState.OutputLayout, _editorState.OutputResolution,
                    out var errorMessage))
            {
                EditorUtility.DisplayDialog(
                    "Export Failed",
                    errorMessage ?? "The output texture size exceeds the GPU maximum supported texture size.",
                    "OK");
                return false;
            }

            var isHDR = _pipelineController?.CurrentCubemapTexture != null
                ? GraphicsFormatUtility.IsHDRFormat(_pipelineController.CurrentCubemapTexture.graphicsFormat)
                : _editorState.IsCurrentInputHDR();

            if (string.IsNullOrEmpty(_editorState.ExportPath) ||
                _editorState.ExportPath.TrimEnd('/', '\\').Equals("Assets", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrEmpty(Path.GetFileName(_editorState.ExportPath)))
            {
                var ext = _editorState.OutputLayout == OutputLayoutType.LegacyCubemap
                    ? "cubemap"
                    : isHDR
                        ? "exr"
                        : "png";
                var path = EditorUtility.SaveFilePanelInProject(
                    "Export Cubemap",
                    "GeneratedCubemap",
                    ext,
                    "Please specify the destination file name.");

                if (string.IsNullOrEmpty(path))
                {
                    return false; // ユーザーがキャンセル
                }

                _editorState.SetExportPath(path);
                _generatorTabPage?.SetExportPath(path);
            }

            _isExporting = true;
            var wasCameraTrackerPaused = _cameraTracker?.IsPaused ?? false;
            if (_cameraTracker != null)
            {
                _cameraTracker.IsPaused = true;
            }

            _pipelineController?.BeginExporting();

            _generatorTabPage?.SetExportEnabled(false);

            _exportCts?.Cancel();
            _exportCts?.Dispose();
            _exportCts = new CancellationTokenSource();

            try
            {
                if (_pipelineController != null)
                {
                    var outputRes = _editorState.OutputResolution > 0 ? _editorState.OutputResolution : 0;
                    await _pipelineController.EnsureHighQualityReadyAsync(outputRes, _exportCts.Token);
                }

                if (_exportCts == null || _exportCts.IsCancellationRequested)
                {
                    return false;
                }

                var rt = _pipelineController?.CurrentCubemapTexture;
                if (rt == null)
                {
                    Debug.LogWarning("[CubemapGenerator] No rendered cubemap available to export.");
                    return false;
                }

                bool success;
                using (new PlayerLoopUpdateScope())
                {
                    success = await CubemapExporter.ExportAsync(_editorState, _settings, rt, _exportCts.Token);
                }

                if (success)
                {
                    var isRtHDR = GraphicsFormatUtility.IsHDRFormat(rt.graphicsFormat);
                    var actualExportPath = _editorState.OutputLayout == OutputLayoutType.LegacyCubemap
                        ? Path.ChangeExtension(_editorState.ExportPath, ".cubemap")
                        : PathUtility.ResolveExportFilePath(_editorState.ExportPath, isRtHDR);

                    if (!string.Equals(_editorState.ExportPath, actualExportPath, StringComparison.OrdinalIgnoreCase))
                    {
                        _editorState.SetExportPath(actualExportPath);
                        _generatorTabPage?.SetExportPath(actualExportPath);
                    }
                }

                _pipelineController?.RequestRedraw();
                return success;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
            finally
            {
                EditorUtility.ClearProgressBar();

                _isExporting = false;
                if (_cameraTracker != null)
                {
                    _cameraTracker.IsPaused = wasCameraTrackerPaused;
                }

                _pipelineController?.EndExporting();

                _exportCts?.Dispose();
                _exportCts = null;

                var isProcessing = _pipelineController?.IsProcessing ?? false;
                _generatorTabPage?.SetExportEnabled(!isProcessing);
            }
        }
    }
}
