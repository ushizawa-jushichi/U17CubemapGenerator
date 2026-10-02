using System;
using UnityEngine;
using UnityEngine.Rendering;
#if U17_HDRP_SUPPORT
using UnityEngine.Rendering.HighDefinition;
#endif
#if U17_URP_SUPPORT
using UnityEngine.Rendering.Universal;
#endif

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     現在のシーンのカメラ状態を再現し、URP / HDRP カメラ経由でキューブマップ RenderTexture をレンダリングするクラス。
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
            if (!RenderPipelineUtility.IsSupportedPipelineActive())
            {
                onWarning?.Invoke(
                    "Current active Render Pipeline is not supported. Scene cubemap capture requires Universal Render Pipeline (URP) or High Definition Render Pipeline (HDRP).");
                return null!;
            }

            if (captureCamera == null)
            {
                onWarning?.Invoke("Capture camera is null or destroyed.");
                return null!;
            }

            PrepareCaptureCamera(parameters, settings, captureCamera, getCurrentCamera);

#if U17_HDRP_SUPPORT
            if (RenderPipelineUtility.IsHighDefinitionRenderPipelineActive())
            {
                CheckHDRPExposureMode(captureCamera, onWarning);
            }
#endif

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
                onWarning?.Invoke("The selected camera does not support cubemap capture.");
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

#if U17_URP_SUPPORT
                if (RenderPipelineUtility.IsUniversalRenderPipelineActive())
                {
                    var renderAdditionalData = captureCamera.GetUniversalAdditionalCameraData();
                    if (renderAdditionalData != null)
                    {
                        if (currentCamera.TryGetComponent<UniversalAdditionalCameraData>(
                                out var currentAdditionalData) &&
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
#endif

#if U17_HDRP_SUPPORT
                if (RenderPipelineUtility.IsHighDefinitionRenderPipelineActive())
                {
                    ConfigureHDRPCameraData(captureCamera, currentCamera);
                }
#endif
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

#if U17_URP_SUPPORT
                if (RenderPipelineUtility.IsUniversalRenderPipelineActive())
                {
                    var renderAdditionalData = captureCamera.GetUniversalAdditionalCameraData();
                    if (renderAdditionalData != null)
                    {
                        renderAdditionalData.renderPostProcessing = false;
                        renderAdditionalData.renderShadows = false;
                        renderAdditionalData.volumeLayerMask = DefaultVolumeLayerMask;
                        renderAdditionalData.volumeTrigger = null;
                    }
                }
#endif

#if U17_HDRP_SUPPORT
                if (RenderPipelineUtility.IsHighDefinitionRenderPipelineActive())
                {
                    ConfigureHDRPCameraData(captureCamera, null!);
                }
#endif
            }
        }

#if U17_HDRP_SUPPORT
        private static void ConfigureHDRPCameraData(Camera captureCamera, Camera currentCamera)
        {
            if (!captureCamera.TryGetComponent<HDAdditionalCameraData>(out var additionalData) ||
                additionalData == null)
            {
                additionalData = captureCamera.gameObject.AddComponent<HDAdditionalCameraData>();
            }

            if (additionalData != null)
            {
                if (currentCamera != null &&
                    currentCamera.TryGetComponent<HDAdditionalCameraData>(out var currentAdditionalData) &&
                    currentAdditionalData != null)
                {
                    additionalData.volumeLayerMask = currentAdditionalData.volumeLayerMask;
                    additionalData.volumeAnchorOverride = currentAdditionalData.volumeAnchorOverride != null
                        ? currentAdditionalData.volumeAnchorOverride
                        : currentCamera.transform;
                }
                else
                {
                    additionalData.volumeLayerMask = DefaultVolumeLayerMask;
                }

                additionalData.customRenderingSettings = true;
                var frameSettings = additionalData.renderingPathCustomFrameSettings;
                var overrideMask = additionalData.renderingPathCustomFrameSettingsOverrideMask;

                void Override(FrameSettingsField field, bool value)
                {
                    overrideMask.mask[(uint)field] = true;
                    frameSettings.SetEnabled(field, value);
                }

                // 露出制御(Exposure)は有効のまま維持（物理光量を適正露出にスケールするため）。
                // 90度画角の面ごとに不連続が発生するスクリーンスペース効果・ボリュメトリクスのみを無効化。
                Override(FrameSettingsField.Postprocess, false);
                Override(FrameSettingsField.Tonemapping, false);
                Override(FrameSettingsField.SSAO, false);
                Override(FrameSettingsField.SSGI, false);
                Override(FrameSettingsField.SSR, false);
                Override(FrameSettingsField.ScreenSpaceShadows, false);
                Override(FrameSettingsField.Volumetrics, false);
                Override(FrameSettingsField.VolumetricClouds, false);

                additionalData.renderingPathCustomFrameSettings = frameSettings;
                additionalData.renderingPathCustomFrameSettingsOverrideMask = overrideMask;
            }
        }

        private static void CheckHDRPExposureMode(Camera captureCamera, Action<string?>? onWarning)
        {
            if (captureCamera == null)
            {
                return;
            }

            var anchor = captureCamera.transform;
            var layerMask = DefaultVolumeLayerMask;

            if (captureCamera.TryGetComponent<HDAdditionalCameraData>(out var additionalData) && additionalData != null)
            {
                if (additionalData.volumeAnchorOverride != null)
                {
                    anchor = additionalData.volumeAnchorOverride;
                }

                layerMask = additionalData.volumeLayerMask;
            }

            VolumeManager.instance.Update(anchor, layerMask);
            var stack = VolumeManager.instance.stack;
            if (stack != null)
            {
                var exposure = stack.GetComponent<Exposure>();
                if (exposure == null || !exposure.active || exposure.mode.value != ExposureMode.Fixed)
                {
                    onWarning?.Invoke(
                        "HDRP Exposure mode is not set to Fixed. Non-fixed exposure may cause inconsistent brightness across cubemap faces.");
                }
            }
        }
#endif
    }
}
