using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     URP カメラから 6 面のシーン描画を行い、Cubemap RenderTexture を生成するユーティリティ。
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
            var pipeline = QualitySettings.renderPipeline != null
                ? QualitySettings.renderPipeline
                : GraphicsSettings.currentRenderPipeline;
            return pipeline is UniversalRenderPipelineAsset;
        }

        /// <summary>
        ///     指定されたカメラから同一フレーム内で 6 面をレンダリングし、キューブマップ RenderTexture を構築します。
        /// </summary>
        /// <remarks>
        ///     【副作用についての注意】<br />
        ///     本メソッドは 6 面の描画を行うため、実行中に渡された <paramref name="renderCamera" /> の以下のプロパティを一時的に変更します。<br />
        ///     - <see cref="Camera.fieldOfView" />（90 度に変更）<br />
        ///     - <see cref="Camera.aspect" />（1.0 に変更）<br />
        ///     - <see cref="Transform.rotation" />（各面に対応する方向に変更）<br />
        ///     - <see cref="Camera.targetTexture" /><br />
        ///     - <see cref="Camera.projectionMatrix" /><br />
        ///     これらのプロパティは try-finally ブロック内で元の値へ復元されますが、レンダリング中のカメラコールバック等への影響を避けるため、
        ///     シーン上の本番カメラではなく一時カメラ（<c>TemporaryCamera</c> 等）を渡すことを推奨します。
        /// </remarks>
        /// <param name="cubeRT">描画先 Cubemap RenderTexture</param>
        /// <param name="renderCamera">レンダリング用カメラ</param>
        /// <param name="baseRotation">カメラに適用するベース回転（null の場合は Quaternion.identity）</param>
        /// <returns>描画に成功した場合は true、URP 非アクティブやカメラ非対応時は false</returns>
        public static bool RenderCubemap(RenderTexture cubeRT, Camera renderCamera, Quaternion? baseRotation = null)
        {
            Assert.IsNotNull(cubeRT, $"{nameof(cubeRT)} is null.");
            Assert.IsNotNull(renderCamera, $"{nameof(renderCamera)} is null.");

            if (!IsUniversalRenderPipelineActive())
            {
                Debug.LogWarning(
                    "[CubemapRenderUtility] Scene cubemap capture requires Universal Render Pipeline (URP).");
                return false;
            }

            var singleCameraRequest = new UniversalRenderPipeline.SingleCameraRequest();
            if (!RenderPipeline.SupportsRenderRequest(renderCamera, singleCameraRequest))
            {
                Debug.LogWarning(
                    $"[CubemapRenderUtility] Camera '{renderCamera.name}' does not support URP SingleCameraRequest.");
                return false;
            }

            var rotation = baseRotation ?? Quaternion.identity;
            var tempTarget = renderCamera.targetTexture;
            var tempFov = renderCamera.fieldOfView;
            var tempAspect = renderCamera.aspect;
            var tempRotation = renderCamera.transform.rotation;
            var tempProjection = renderCamera.projectionMatrix;

            // 元のカメラがカスタムプロジェクション行列を使用していたかを判定
            renderCamera.ResetProjectionMatrix();
            var defaultProjection = renderCamera.projectionMatrix;
            var hadCustomProjection = tempProjection != defaultProjection;
            if (hadCustomProjection)
            {
                renderCamera.projectionMatrix = tempProjection;
            }

            // 注意: URP の UniversalRenderPipeline.ProcessRenderRequests は destination.dimension が Cube の場合、
            // 内部で RTDesc = new RenderTextureDescriptor() を実行して descriptor.sRGB を初期化しないため、
            // sRGB 設定が欠落してリニア値のまま書き込まれる不具合が存在します。
            // そのため、cubeRT.graphicsFormat を明示指定した 2D 一時 RenderTexture (tempRT) を経由してレンダリングし、
            // CopyTexture で転送することで sRGB / Linear カラーマネジメントの正確性を 100% 保証します。
            RenderTexture tempRT = null!;

            try
            {
                tempRT = RenderTexture.GetTemporary(cubeRT.width, cubeRT.height, 24, cubeRT.graphicsFormat);
                singleCameraRequest.destination = tempRT;

                renderCamera.fieldOfView = CubemapFaceFov;
                renderCamera.aspect = CubemapFaceAspect;

                for (var i = 0; i < CubemapFaceCount; i++)
                {
                    var face = (CubemapFace)i;
                    RenderFace(renderCamera, face, rotation, singleCameraRequest);
                    Graphics.CopyTexture(tempRT, 0, 0, cubeRT, i, 0);
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

                if (tempRT != null)
                {
                    RenderTexture.ReleaseTemporary(tempRT);
                }
            }
        }

        // renderCameraのプロパティを書き換えることに注意。復元は呼び出し側の責務
        private static void RenderFace(
            Camera renderCamera,
            CubemapFace face,
            Quaternion rotation,
            UniversalRenderPipeline.SingleCameraRequest request)
        {
            Quaternion rot = default;
            switch (face)
            {
                case CubemapFace.PositiveX: rot = Quaternion.Euler(0f, 90f, 0f); break;
                case CubemapFace.NegativeX: rot = Quaternion.Euler(0f, -90f, 0f); break;
                case CubemapFace.PositiveY: rot = Quaternion.Euler(-90f, 0f, 0f); break;
                case CubemapFace.NegativeY: rot = Quaternion.Euler(90f, 0f, 0f); break;
                case CubemapFace.PositiveZ: rot = Quaternion.Euler(0f, 0f, 0f); break;
                case CubemapFace.NegativeZ: rot = Quaternion.Euler(0f, 180f, 0f); break;
            }

            renderCamera.transform.rotation = rotation * rot;
            renderCamera.ResetProjectionMatrix();
            renderCamera.projectionMatrix = Matrix4x4.Scale(new Vector3(1f, -1f, 1f)) * renderCamera.projectionMatrix;

            // destination は 2D RenderTexture (tempRT) のため request.face の指定は不要。
            // 描画結果は呼び出し元で Graphics.CopyTexture により cubeRT の各面スライスへ転送される。
            // プロジェクション行列の Y 反転に伴うポリゴン巻き順の反転を InvertCullingScope で補正
            using (new InvertCullingScope(true))
            {
                RenderPipeline.SubmitRenderRequest(renderCamera, request);
            }
        }

        /// <summary>
        ///     GL.invertCulling の状態をスコープ終了時に確実に復元する構造体。
        /// </summary>
        private readonly struct InvertCullingScope : IDisposable
        {
            private readonly bool _previousState;

            public InvertCullingScope(bool invert)
            {
                _previousState = GL.invertCulling;
                GL.invertCulling = invert;
            }

            public void Dispose()
            {
                GL.invertCulling = _previousState;
            }
        }
    }
}
