using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     URP / HDRP カメラから 6 面のシーン描画を行い、Cubemap RenderTexture を生成するユーティリティ。
    /// </summary>
    public static class CubemapRenderUtility
    {
        public const int CubemapFaceCount = 6;
        public const float CubemapFaceFov = 90f;
        public const float CubemapFaceAspect = 1f;

        /// <summary>
        ///     現在のプロジェクトで Universal Render Pipeline (URP) が有効に設定されているかを判定します。
        /// </summary>
        public static bool IsUniversalRenderPipelineActive()
        {
            return RenderPipelineUtility.IsUniversalRenderPipelineActive();
        }

        /// <summary>
        ///     指定されたカメラから同一フレーム内で 6 面をレンダリングし、キューブマップ RenderTexture を構築します。
        /// </summary>
        /// <param name="cubeRT">描画先 Cubemap RenderTexture</param>
        /// <param name="renderCamera">レンダリング用カメラ</param>
        /// <param name="baseRotation">カメラに適用するベース回転（null の場合は Quaternion.identity）</param>
        /// <returns>描画に成功した場合は true、パイプライン非アクティブやカメラ非対応時は false</returns>
        public static bool RenderCubemap(RenderTexture cubeRT, Camera renderCamera, Quaternion? baseRotation = null)
        {
            Assert.IsNotNull(cubeRT, $"{nameof(cubeRT)} is null.");
            Assert.IsNotNull(renderCamera, $"{nameof(renderCamera)} is null.");

            if (!RenderPipelineUtility.IsSupportedPipelineActive())
            {
                Debug.LogWarning(
                    "[CubemapRenderUtility] Scene cubemap capture requires Universal Render Pipeline (URP) or High Definition Render Pipeline (HDRP).");
                return false;
            }

            var rotation = baseRotation ?? Quaternion.identity;
            var tempTarget = renderCamera.targetTexture;
            var tempFov = renderCamera.fieldOfView;
            var tempAspect = renderCamera.aspect;
            var tempRotation = renderCamera.transform.rotation;
            var tempProjection = renderCamera.projectionMatrix;

            renderCamera.ResetProjectionMatrix();
            var defaultProjection = renderCamera.projectionMatrix;
            var hadCustomProjection = tempProjection != defaultProjection;
            if (hadCustomProjection)
            {
                renderCamera.projectionMatrix = tempProjection;
            }

            RenderTexture tempRT = null!;
            RenderTexture flippedRT = null!;
            var previousActive = RenderTexture.active;

            try
            {
                tempRT = RenderTexture.GetTemporary(cubeRT.width, cubeRT.height, 24, cubeRT.graphicsFormat);
                flippedRT = RenderTexture.GetTemporary(cubeRT.width, cubeRT.height, 0, cubeRT.graphicsFormat);
                renderCamera.fieldOfView = CubemapFaceFov;
                renderCamera.aspect = CubemapFaceAspect;

#if U17_URP_SUPPORT
                if (RenderPipelineUtility.IsUniversalRenderPipelineActive())
                {
                    var singleCameraRequest = new UniversalRenderPipeline.SingleCameraRequest
                    {
                        destination = tempRT
                    };

                    if (!RenderPipeline.SupportsRenderRequest(renderCamera, singleCameraRequest))
                    {
                        Debug.LogWarning(
                            $"[CubemapRenderUtility] Camera '{renderCamera.name}' does not support URP SingleCameraRequest.");
                        return false;
                    }

                    for (var i = 0; i < CubemapFaceCount; i++)
                    {
                        var face = (CubemapFace)i;
                        RenderFaceURP(renderCamera, face, rotation, singleCameraRequest);
                        Graphics.Blit(tempRT, flippedRT, new Vector2(1f, -1f), new Vector2(0f, 1f));
                        Graphics.CopyTexture(flippedRT, 0, 0, cubeRT, i, 0);
                    }
                }
                else
#endif
                {
                    renderCamera.targetTexture = tempRT;
                    for (var i = 0; i < CubemapFaceCount; i++)
                    {
                        var face = (CubemapFace)i;
                        RenderFaceStandard(renderCamera, face, rotation);
                        Graphics.Blit(tempRT, flippedRT, new Vector2(1f, -1f), new Vector2(0f, 1f));
                        Graphics.CopyTexture(flippedRT, 0, 0, cubeRT, i, 0);
                    }
                }

                if (cubeRT.useMipMap && !cubeRT.autoGenerateMips)
                {
                    cubeRT.GenerateMips();
                }

                return true;
            }
            finally
            {
                renderCamera.targetTexture = tempTarget;
                renderCamera.fieldOfView = tempFov;
                renderCamera.aspect = tempAspect;
                renderCamera.transform.rotation = tempRotation;
                if (hadCustomProjection)
                {
                    renderCamera.projectionMatrix = tempProjection;
                }
                else
                {
                    renderCamera.ResetProjectionMatrix();
                }

                RenderTexture.active = previousActive;

                if (tempRT != null)
                {
                    RenderTexture.ReleaseTemporary(tempRT);
                }

                if (flippedRT != null)
                {
                    RenderTexture.ReleaseTemporary(flippedRT);
                }
            }
        }

#if U17_URP_SUPPORT
        private static void RenderFaceURP(
            Camera renderCamera,
            CubemapFace face,
            Quaternion rotation,
            UniversalRenderPipeline.SingleCameraRequest request)
        {
            var rot = GetFaceRotation(face);
            renderCamera.transform.rotation = rotation * rot;
            renderCamera.ResetProjectionMatrix();
            RenderPipeline.SubmitRenderRequest(renderCamera, request);
        }
#endif

        private static void RenderFaceStandard(
            Camera renderCamera,
            CubemapFace face,
            Quaternion rotation)
        {
            var rot = GetFaceRotation(face);
            renderCamera.transform.rotation = rotation * rot;
            renderCamera.ResetProjectionMatrix();
            renderCamera.Render();
        }

        private static Quaternion GetFaceRotation(CubemapFace face)
        {
            switch (face)
            {
                case CubemapFace.PositiveX: return Quaternion.Euler(0f, 90f, 0f);
                case CubemapFace.NegativeX: return Quaternion.Euler(0f, -90f, 0f);
                case CubemapFace.PositiveY: return Quaternion.Euler(-90f, 0f, 0f);
                case CubemapFace.NegativeY: return Quaternion.Euler(90f, 0f, 0f);
                case CubemapFace.PositiveZ: return Quaternion.Euler(0f, 0f, 0f);
                case CubemapFace.NegativeZ: return Quaternion.Euler(0f, 180f, 0f);
                default: return Quaternion.identity;
            }
        }
    }
}
