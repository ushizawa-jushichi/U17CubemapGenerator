using System;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     Cubemap アセットを検証・フォーマット適合処理し、キューブマップ RenderTexture を構築するクラス。
    /// </summary>
    public static class CubemapAssetCubemapRenderer
    {
        private const int PassCubemapFace = 3;
        private static readonly int IDCubeTex = Shader.PropertyToID("_CubeTex");
        private static readonly int IDFaceIndex = Shader.PropertyToID("_FaceIndex");
        private static readonly int IDScale = Shader.PropertyToID("_Scale");
        private static readonly int IDOffset = Shader.PropertyToID("_Offset");

        public static RenderTexture Render(
            Cubemap cubemap,
            CubemapRenderTexturePool rtPool,
            Action<string?>? onWarning = null,
            CancellationToken cancellationToken = default)
        {
            return Render(cubemap, null!, rtPool, default, onWarning, cancellationToken);
        }

        public static RenderTexture Render(
            Cubemap cubemap,
            Material matBlitter,
            CubemapRenderTexturePool rtPool,
            Action<string?>? onWarning = null,
            CancellationToken cancellationToken = default)
        {
            return Render(cubemap, matBlitter, rtPool, default, onWarning, cancellationToken);
        }

        public static RenderTexture Render(
            Cubemap cubemap,
            Material matBlitter,
            CubemapRenderTexturePool rtPool,
            FaceFlipParameters faceFlips,
            Action<string?>? onWarning = null,
            CancellationToken cancellationToken = default)
        {
            if (cubemap == null)
            {
                onWarning?.Invoke("No Cubemap asset assigned.");
                return null!;
            }

            if (matBlitter == null)
            {
                var errorMsg =
                    "Required Blitter Material (BlitterURP_Mat / BlitterHDRP_Mat) is null or missing in Settings.";
                onWarning?.Invoke(errorMsg);
                Debug.LogError($"[U17CubemapGenerator] {errorMsg}");
                return null!;
            }

            var isCubemapFile = false;
            var assetPath = AssetDatabase.GetAssetPath(cubemap);
            if (!string.IsNullOrEmpty(assetPath))
            {
                isCubemapFile = CubemapTextureImporterUtility.IsCubemapFile(assetPath);
            }

            var cubemapSize = cubemap.width;
            var graphicsFormat = cubemap.graphicsFormat;
            var isSrgb = GraphicsFormatUtility.IsSRGBFormat(graphicsFormat);
            var isHDR = GraphicsFormatUtility.IsHDRFormat(graphicsFormat);
            var isCrunch = GraphicsFormatUtility.IsCrunchFormat(cubemap.format);
            StringBuilder? sb = null;
            var isValid = true;

            StringBuilder GetBuilder()
            {
                if (sb == null)
                {
                    sb = new StringBuilder();
                }

                return sb;
            }

            if (cubemap.width != cubemap.height)
            {
                isValid = false;
                GetBuilder().Append(cubemap.name).Append(" is not match size.(").Append(cubemap.width).Append('x')
                    .Append(cubemap.height).Append(')').AppendLine();
            }

            if (!CheckGraphicsFormat(graphicsFormat))
            {
                isValid = false;
                GetBuilder().Append(cubemap.name).Append(" is not supported format:").Append(graphicsFormat)
                    .AppendLine();
            }

            var validationMessage = sb?.ToString();

            if (!string.IsNullOrEmpty(validationMessage))
            {
                onWarning?.Invoke(validationMessage);
            }

            if (!isValid)
            {
                Debug.LogError($"[U17CubemapGenerator] Cubemap validation failed:\n{validationMessage}");
                return null!;
            }

            var cubeRT = rtPool.GetPreviewCubeRenderTexture(cubemapSize, isHDR, isSrgb, true);
            Assert.IsTrue(cubeRT.dimension == TextureDimension.Cube);

            RenderTexture tempRT = null!;

            try
            {
                matBlitter.SetTexture(IDCubeTex, cubemap);

                var previousActive = RenderTexture.active;
                try
                {
                    for (var i = 0; i < 6; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var flipH = faceFlips.GetFlipH(i);
                        var flipV = faceFlips.GetFlipV(i);
                        var hasFlip = flipH || flipV;

                        // 非 .cubemap かつ非 Crunch でフォーマットが一致し、フリップ指定がない場合は直接 CopyTexture を使用
                        if (!hasFlip && !isCubemapFile && !isCrunch && cubemap.graphicsFormat == cubeRT.graphicsFormat)
                        {
                            Graphics.CopyTexture(cubemap, i, 0, cubeRT, i, 0);
                        }
                        else
                        {
                            if (tempRT == null)
                            {
                                var tempRTDesc = new RenderTextureDescriptor(cubemapSize, cubemapSize)
                                {
                                    dimension = TextureDimension.Tex2D,
                                    colorFormat = isHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.ARGB32,
                                    depthBufferBits = 0,
                                    msaaSamples = 1,
                                    sRGB = isSrgb,
                                    autoGenerateMips = false
                                };
                                tempRT = RenderTexture.GetTemporary(tempRTDesc);
                            }

                            var scaleX = flipH ? -1f : 1f;
                            var offsetX = flipH ? 1f : 0f;
                            var scaleY = flipV ? -1f : 1f;
                            var offsetY = flipV ? 1f : 0f;

                            // BlitterURP Pass 3 で GPU サンプラー経由で各面を直接サンプリング描画
                            // （Crunch 圧縮やフォーマット不一致でも安全かつ高速に展開可能）
                            matBlitter.SetInt(IDFaceIndex, i);
                            matBlitter.SetVector(IDScale, new Vector4(scaleX, scaleY, 0f, 0f));
                            matBlitter.SetVector(IDOffset, new Vector4(offsetX, offsetY, 0f, 0f));
                            Graphics.Blit(Texture2D.blackTexture, tempRT, matBlitter, PassCubemapFace);

                            Graphics.CopyTexture(tempRT, 0, 0, cubeRT, i, 0);
                        }
                    }
                }
                finally
                {
                    RenderTexture.active = previousActive;
                }

                if (cubeRT.useMipMap && !cubeRT.autoGenerateMips)
                {
                    cubeRT.GenerateMips();
                }

                return cubeRT;
            }
            finally
            {
                if (matBlitter != null)
                {
                    matBlitter.SetTexture(IDCubeTex, null); // 共有アセットへの参照残留を防止
                    matBlitter.SetInt(IDFaceIndex, 0);
                    matBlitter.SetVector(IDScale, new Vector4(1f, 1f, 0f, 0f));
                    matBlitter.SetVector(IDOffset, Vector4.zero);
                }

                if (tempRT != null)
                {
                    RenderTexture.ReleaseTemporary(tempRT);
                }
            }
        }

        private static bool CheckGraphicsFormat(GraphicsFormat format)
        {
            if (!Enum.IsDefined(typeof(GraphicsFormat), format))
            {
                return false;
            }

#if UNITY_2023_2_OR_NEWER
            return SystemInfo.IsFormatSupported(format, GraphicsFormatUsage.Sample);
#else
            return SystemInfo.IsFormatSupported(format, FormatUsage.Sample);
#endif
        }
    }
}
