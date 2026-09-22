using System;
using System.Threading;
using UnityEngine;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     各入力モード（CurrentScene, SixSided, Cubemap）からのキューブマップ生成およびブラー処理の非同期パイプラインを制御するワーカー。
    /// </summary>
    public sealed class Worker : IDisposable
    {
        private readonly Camera _captureCamera;
        private readonly Func<Camera> _getCurrentCamera;
        private readonly Action<string?> _onWarning;
        private readonly CubemapRenderTexturePool _rtPool;
        private readonly Settings _settings;

        private bool _isDisposed;

        public Worker(
            Settings settings,
            CubemapRenderTexturePool rtPool,
            Camera captureCamera,
            Func<Camera> getCurrentCamera,
            Action<string?> onWarning)
        {
            _settings = settings;
            _rtPool = rtPool;
            _captureCamera = captureCamera;
            _getCurrentCamera = getCurrentCamera;
            _onWarning = onWarning;
        }

        public bool IsProcessing { get; private set; }

        public void Dispose()
        {
            _isDisposed = true;
        }

        public RenderTexture StartRenderCubemap(
            CubemapRenderParameters parameters,
            bool isInteractive,
            CancellationToken cancellationToken = default)
        {
            if (_isDisposed || cancellationToken.IsCancellationRequested)
            {
                return null!;
            }

            return RenderCubemap(parameters, cancellationToken, isInteractive);
        }

        public async Awaitable<RenderTexture> StartBlurredCubemapAsync(
            CubemapRenderParameters parameters,
            bool isInteractive,
            RenderTexture cubeRT,
            CancellationToken cancellationToken = default)
        {
            if (_isDisposed || cancellationToken.IsCancellationRequested)
            {
                return null!;
            }

            return await BlurredCubemapAsync(parameters, cancellationToken, cubeRT, isInteractive);
        }

        #region Render Cubemap Pipeline

        private RenderTexture RenderCubemap(
            CubemapRenderParameters parameters,
            CancellationToken cancellationToken,
            bool isInteractive)
        {
            IsProcessing = true;
            var cubeRT = (RenderTexture)null!;

            try
            {
                _onWarning?.Invoke(null);
                cancellationToken.ThrowIfCancellationRequested();

                cubeRT = parameters.InputMode switch
                {
                    InputModeType.CurrentScene => CurrentSceneCubemapRenderer.Render(
                        parameters, _settings, _rtPool, _captureCamera, _getCurrentCamera, _onWarning, isInteractive),
                    InputModeType.SixSided => SixSidedCubemapRenderer.Render(
                        parameters, _rtPool, _onWarning, cancellationToken),
                    InputModeType.Cubemap => CubemapAssetCubemapRenderer.Render(
                        parameters.CubemapAsset!, _settings.MatBlitter, _rtPool, parameters.FaceFlips, _onWarning,
                        cancellationToken),
                    _ => throw new NotSupportedException($"Unsupported InputMode: {parameters.InputMode}")
                };
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            finally
            {
                IsProcessing = false;
            }

            return cubeRT;
        }

        #endregion

        #region Blurred Cubemap Pipeline

        private async Awaitable<RenderTexture> BlurredCubemapAsync(
            CubemapRenderParameters parameters,
            CancellationToken cancellationToken,
            RenderTexture cubeRT,
            bool isInteractive)
        {
            IsProcessing = true;
            var blurredCubemap = (RenderTexture)null!;

            try
            {
                _onWarning?.Invoke(null);
                cancellationToken.ThrowIfCancellationRequested();

                var roughness = parameters.BlurEnable ? parameters.BlurRoughness : 0f;
                var maxInteractiveResolution =
                    parameters.MaxInteractiveBlurResolution > 0 ? parameters.MaxInteractiveBlurResolution : 256;
                var maxInteractiveSamples =
                    parameters.MaxInteractiveBlurNumSamples > 0 ? parameters.MaxInteractiveBlurNumSamples : 64;

                var numSamples = parameters.BlurEnable
                    ? isInteractive
                        ? Mathf.Min(parameters.BlurNumSamples, maxInteractiveSamples)
                        : parameters.BlurNumSamples
                    : 1;

                var algorithm = parameters.BlurEnable
                    ? parameters.BlurAlgorithm
                    : CubemapBlurAlgorithm.Gaussian_Bokeh;

                var blurResolution = isInteractive
                    ? Mathf.Min(cubeRT.width, maxInteractiveResolution)
                    : cubeRT.width;
                _rtPool.GetBlurredRTs(cubeRT, blurResolution, out var previewRTArray, out var previewBlurredCubeRT,
                    isInteractive);

                blurredCubemap = await CubemapBlurUtility.GenerateBlurredCubemapAsync(
                    cubeRT,
                    previewBlurredCubeRT,
                    previewRTArray,
                    roughness,
                    numSamples,
                    algorithm,
                    _settings.CubemapBlurShader,
                    cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            finally
            {
                IsProcessing = false;
            }

            return blurredCubemap;
        }

        #endregion
    }
}
