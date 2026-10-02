using System;
using System.Threading;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     キューブマップ RenderTexture のクリアや、Cubemap アセットへの変換を行うテクスチャ操作ユーティリティ。
    /// </summary>
    public static class CubemapTextureUtility
    {
        private const int PassCubemapFace = 3;
        private static readonly int IDCubeTex = Shader.PropertyToID("_CubeTex");
        private static readonly int IDFaceIndex = Shader.PropertyToID("_FaceIndex");
        private static readonly int IDScale = Shader.PropertyToID("_Scale");
        private static readonly int IDOffset = Shader.PropertyToID("_Offset");

        public static void ClearAllFaces(RenderTexture cubeRT, Color color = default)
        {
            Assert.IsNotNull(cubeRT, $"{nameof(cubeRT)} is null.");

            if (cubeRT.dimension != TextureDimension.Cube)
            {
                throw new ArgumentException($"{nameof(cubeRT)} is not Cube");
            }

            if (!cubeRT.IsCreated())
            {
                cubeRT.Create();
            }

            var previousActive = RenderTexture.active;
            try
            {
                for (var i = 0; i < 6; i++)
                {
                    Graphics.SetRenderTarget(cubeRT, 0, (CubemapFace)i);
                    GL.Clear(false, true, color);
                }
            }
            finally
            {
                RenderTexture.active = previousActive;
            }
        }


        /// <summary>
        ///     CubeRenderTexture から Cubemap アセット用オブジェクトを非同期に生成します。
        ///     AsyncGPUReadback を使用してメインスレッドのブロッキングを防ぎます。
        /// </summary>
        public static async Awaitable<Cubemap> CreateCubemapFromCubeRenderTextureAsync(
            RenderTexture cubeRT,
            IProgress<(int faceIndex, int totalFaces)>? progress = null,
            CancellationToken cancellationToken = default)
        {
            Assert.IsNotNull(cubeRT, $"{nameof(cubeRT)} is null.");
            if (cubeRT.dimension != TextureDimension.Cube)
            {
                throw new InvalidOperationException("renderTexture is not Cube");
            }

            cancellationToken.ThrowIfCancellationRequested();

            var isHDR = GraphicsFormatUtility.IsHDRFormat(cubeRT.graphicsFormat);
            var cubemapSize = cubeRT.width;
            var textureFormat = isHDR ? TextureFormat.RGBAHalf : TextureFormat.RGBA32;
            var useMipMap = cubeRT.useMipMap;

            Cubemap cubemap = null!;
            Texture2D faceTex = null!;
            var rowBuffer = default(NativeArray<byte>);

            try
            {
                cubemap = new Cubemap(cubemapSize, textureFormat, useMipMap);
                faceTex = new Texture2D(cubemapSize, cubemapSize, textureFormat, false);

                for (var i = 0; i < CubemapRenderUtility.CubemapFaceCount; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    progress?.Report((i + 1, CubemapRenderUtility.CubemapFaceCount));

                    await CubemapReadbackUtility.RequestReadbackIntoTextureAsync(
                        cubeRT,
                        faceTex,
                        i,
                        cancellationToken,
                        $"Cubemap face {i}");

                    // Cubemap.SetPixelData への格納にあたり、面テクスチャの向きを合わせるため水平反転を行う。
                    PixelBufferUtility.HorizontalFlip(faceTex, ref rowBuffer);
                    cubemap.SetPixelData(faceTex.GetRawTextureData<byte>(), 0, (CubemapFace)i);
                }

                progress?.Report((CubemapRenderUtility.CubemapFaceCount, CubemapRenderUtility.CubemapFaceCount));

                cubemap.Apply(useMipMap, false);
                return cubemap;
            }
            catch
            {
                if (cubemap != null)
                {
                    Object.DestroyImmediate(cubemap);
                }

                throw;
            }
            finally
            {
                if (rowBuffer.IsCreated)
                {
                    rowBuffer.Dispose();
                }

                if (faceTex != null)
                {
                    Object.DestroyImmediate(faceTex);
                }
            }
        }

        /// <summary>
        ///     CubeRenderTexture の全 6 面をバイリニアリサイズし、指定解像度の新しい一時 RT を生成します。
        ///     matBlitter が指定されている場合は GPU Blit により CPU ReadPixels なしで高速実行されます。
        /// </summary>
        public static RenderTexture CreateScaledCubemap(RenderTexture source, int targetResolution,
            Material matBlitter = null!)
        {
            Assert.IsNotNull(source, $"{nameof(source)} is null.");
            if (source.dimension != TextureDimension.Cube)
            {
                throw new ArgumentException($"{nameof(source)} is not a Cube RenderTexture.");
            }

            if (targetResolution <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(targetResolution),
                    "Target resolution must be greater than zero.");
            }

            if (matBlitter == null)
            {
                Debug.LogError(
                    "[CubemapTextureUtility] CreateScaledCubemap failed: Required Blitter Material is null.");
                return null!;
            }

            var desc = source.descriptor;
            desc.width = targetResolution;
            desc.height = targetResolution;

            RenderTexture scaledCubeRT = null!;
            RenderTexture tempDst = null!;
            var previousActive = RenderTexture.active;

            try
            {
                scaledCubeRT = RenderTexture.GetTemporary(desc);
                if (!scaledCubeRT.IsCreated())
                {
                    scaledCubeRT.Create();
                }

                var dstFaceDesc = desc;
                dstFaceDesc.dimension = TextureDimension.Tex2D;
                dstFaceDesc.depthBufferBits = 0;

                tempDst = RenderTexture.GetTemporary(dstFaceDesc);

                // GPU 間 Blit: CPU ReadPixels を完全に排除し、BlitterURP Pass 3 で GPU 上で直接リサイズ
                matBlitter.SetTexture(IDCubeTex, source);
                matBlitter.SetVector(IDScale, Vector2.one);
                matBlitter.SetVector(IDOffset, Vector2.zero);

                for (var i = 0; i < CubemapRenderUtility.CubemapFaceCount; i++)
                {
                    matBlitter.SetInt(IDFaceIndex, i);
                    Graphics.Blit(Texture2D.blackTexture, tempDst, matBlitter, PassCubemapFace);
                    Graphics.CopyTexture(tempDst, 0, 0, scaledCubeRT, i, 0);
                }

                if (scaledCubeRT.useMipMap && !scaledCubeRT.autoGenerateMips)
                {
                    scaledCubeRT.GenerateMips();
                }

                return scaledCubeRT;
            }
            catch
            {
                if (scaledCubeRT != null)
                {
                    RenderTexture.ReleaseTemporary(scaledCubeRT);
                }

                throw;
            }
            finally
            {
                if (matBlitter != null)
                {
                    matBlitter.SetTexture(IDCubeTex, null); // 共有アセットへの参照残留を防止
                }

                RenderTexture.active = previousActive;

                if (tempDst != null)
                {
                    RenderTexture.ReleaseTemporary(tempDst);
                }
            }
        }
    }
}
