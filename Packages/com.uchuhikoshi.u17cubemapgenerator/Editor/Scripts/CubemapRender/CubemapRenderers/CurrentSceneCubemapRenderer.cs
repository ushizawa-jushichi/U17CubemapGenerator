using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     現在のシーンのカメラ状態を再現し、URP カメラ経由でキューブマップ RenderTexture をレンダリングするクラス。
    /// </summary>
    public static class CurrentSceneCubemapRenderer
    {
        private const int DefaultFallbackResolution = 1024;
        private const float DefaultCameraNearClip = 0.3f;
        private const float DefaultCameraFarClip = 1000f;
        private const int DefaultVolumeLayerMask = 1;

        public static RenderTexture Render(
            in CubemapRenderParameters parameters,
            Settings settings,
            CubemapRenderTexturePool rtPool,
            Camera captureCamera,
            Func<Camera> getCurrentCamera,
            Action<string?>? onWarning,
            bool isInteractive)
        {
            if (!CubemapRenderUtility.IsUniversalRenderPipelineActive())
            {
                onWarning?.Invoke(
                    "Current active Render Pipeline is not URP. Scene cubemap capture requires Universal Render Pipeline.");
                return null!;
            }

            if (captureCamera == null)
            {
                onWarning?.Invoke("Capture camera is null or destroyed.");
                return null!;
            }

            PrepareCaptureCamera(parameters, settings, captureCamera, getCurrentCamera);

            var fallbackResolution =
                settings != null ? settings.DefaultOutputResolution : DefaultFallbackResolution;
            var outputResolution =
                isInteractive ? Mathf.Min(parameters.OutputResolution, 256) : parameters.OutputResolution;
            var cubemapSize = outputResolution > 0 ? outputResolution : fallbackResolution;
            var cubeRT = rtPool.GetPreviewCubeRenderTexture(cubemapSize, parameters.RenderSceneHDR,
                parameters.RenderSceneSRGB, true, isInteractive);

            var rotation = parameters.RenderSceneRotatable ? captureCamera.transform.rotation : Quaternion.identity;
            if (!CubemapRenderUtility.RenderCubemap(cubeRT, captureCamera, rotation))
            {
                onWarning?.Invoke("The selected camera does not support URP cubemap capture.");
                return null!;
            }

            return cubeRT;
        }

        private static void PrepareCaptureCamera(
            in CubemapRenderParameters parameters,
            Settings settings,
            Camera captureCamera,
            Func<Camera> getCurrentCamera)
        {
            if (captureCamera == null)
            {
                throw new ArgumentNullException(nameof(captureCamera));
            }

            var currentCamera = getCurrentCamera?.Invoke();
            var renderAdditionalData = captureCamera.GetUniversalAdditionalCameraData();

            if (currentCamera != null)
            {
                captureCamera.transform.position = currentCamera.transform.position;
                captureCamera.transform.rotation = parameters.RenderSceneRotatable
                    ? currentCamera.transform.rotation
                    : Quaternion.identity;
                captureCamera.nearClipPlane = currentCamera.nearClipPlane;
                captureCamera.farClipPlane = currentCamera.farClipPlane;
                captureCamera.cullingMask = currentCamera.cullingMask;
                captureCamera.clearFlags = currentCamera.clearFlags;
                captureCamera.backgroundColor = currentCamera.backgroundColor;
                captureCamera.useOcclusionCulling = currentCamera.useOcclusionCulling;

                if (renderAdditionalData != null)
                {
                    if (currentCamera.TryGetComponent<UniversalAdditionalCameraData>(out var currentAdditionalData) &&
                        currentAdditionalData != null)
                    {
                        renderAdditionalData.renderPostProcessing = currentAdditionalData.renderPostProcessing;
                        renderAdditionalData.renderShadows = currentAdditionalData.renderShadows;
                        renderAdditionalData.volumeLayerMask = currentAdditionalData.volumeLayerMask;
                        renderAdditionalData.volumeTrigger = currentAdditionalData.volumeTrigger;
                    }
                    else
                    {
                        renderAdditionalData.renderPostProcessing = false;
                        renderAdditionalData.renderShadows = false;
                        renderAdditionalData.volumeLayerMask = DefaultVolumeLayerMask;
                        renderAdditionalData.volumeTrigger = null;
                    }
                }
            }
            else
            {
                captureCamera.transform.position = Vector3.zero;
                captureCamera.transform.rotation = Quaternion.identity;
                captureCamera.nearClipPlane =
                    settings != null ? settings.DefaultCameraNearClipPlane : DefaultCameraNearClip;
                captureCamera.farClipPlane =
                    settings != null ? settings.DefaultCameraFarClipPlane : DefaultCameraFarClip;
                captureCamera.cullingMask = -1;
                captureCamera.clearFlags = CameraClearFlags.Skybox;
                captureCamera.backgroundColor = Color.black;
                captureCamera.useOcclusionCulling = true;

                if (renderAdditionalData != null)
                {
                    renderAdditionalData.renderPostProcessing = false;
                    renderAdditionalData.renderShadows = false;
                    renderAdditionalData.volumeLayerMask = DefaultVolumeLayerMask;
                    renderAdditionalData.volumeTrigger = null;
                }
            }
        }
    }
}
