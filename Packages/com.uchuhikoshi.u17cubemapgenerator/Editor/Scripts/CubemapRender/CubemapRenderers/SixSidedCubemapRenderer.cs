using System;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     6枚のテクスチャ（+X, -X, +Y, -Y, +Z, -Z）を検証・反転Blitし、キューブマップ RenderTexture を構築するクラス。
    /// </summary>
    public static class SixSidedCubemapRenderer
    {
        private static readonly string[] s_FaceNames =
            { "X+ (+X)", "X- (-X)", "Y+ (+Y)", "Y- (-Y)", "Z+ (+Z)", "Z- (-Z)" };

        public static RenderTexture Render(
            in CubemapRenderParameters parameters,
            CubemapRenderTexturePool rtPool,
            Action<string?>? onWarning,
            CancellationToken cancellationToken)
        {
            var cubemapSize = 0;
            var graphicsFormat = GraphicsFormat.None;
            var isSrgb = false;
            var isHDR = false;
            StringBuilder? sb = null;
            var isValid = true;
            var isImportant = false;

            StringBuilder GetBuilder()
            {
                if (sb == null)
                {
                    sb = StringBuilderPool.Get();
                }

                return sb;
            }

            for (var i = 0; i < 6; i++)
            {
                var tex = GetSixSidedTexture(parameters, i);
                if (tex == null)
                {
                    continue;
                }

                cubemapSize = Mathf.Max(tex.width, tex.height);
                graphicsFormat = tex.graphicsFormat;
                isSrgb = GraphicsFormatUtility.IsSRGBFormat(graphicsFormat);
                isHDR = GraphicsFormatUtility.IsHDRFormat(graphicsFormat);
                break;
            }

            if (cubemapSize == 0)
            {
                isValid = false;
                GetBuilder().AppendLine("No Six-Sided textures are assigned.");
            }
            else
            {
                for (var i = 0; i < 6; i++)
                {
                    var tex = GetSixSidedTexture(parameters, i);
                    if (tex == null)
                    {
                        isValid = false;
                        GetBuilder().Append("Missing texture for face: ").AppendLine(s_FaceNames[i]);
                        continue;
                    }

                    if (!CheckGraphicsFormat(tex.graphicsFormat))
                    {
                        isValid = false;
                        isImportant = true;
                        GetBuilder().Append(tex.name).Append(" (").Append(s_FaceNames[i])
                            .Append(") has unsupported format: ").Append(tex.graphicsFormat).AppendLine();
                    }

                    if (cubemapSize != tex.width || cubemapSize != tex.height)
                    {
                        isValid = false;
                        isImportant = true;
                        GetBuilder().Append(tex.name).Append(" (").Append(s_FaceNames[i])
                            .Append(") size (").Append(tex.width).Append('x').Append(tex.height)
                            .Append(") does not match expected size (").Append(cubemapSize).Append('x')
                            .Append(cubemapSize)
                            .Append(')').AppendLine();
                    }

                    if (graphicsFormat != tex.graphicsFormat)
                    {
                        isValid = false;
                        isImportant = true;
                        GetBuilder().Append(tex.name).Append(" (").Append(s_FaceNames[i])
                            .Append(") format (").Append(tex.graphicsFormat)
                            .Append(") does not match expected format (").Append(graphicsFormat).Append(')')
                            .AppendLine();
                    }
                }
            }

            var validationMessage = sb?.ToString();
            if (!string.IsNullOrEmpty(validationMessage))
            {
                onWarning?.Invoke(validationMessage);
            }

            if (!isValid)
            {
                if (isImportant)
                {
                    Debug.LogWarning($"[U17CubemapGenerator] Six-Sided validation failed:\n{validationMessage}");
                }

                return null!;
            }

            var cubeRT = rtPool.GetPreviewCubeRenderTexture(cubemapSize, isHDR, isSrgb, true);
            Assert.IsTrue(cubeRT.dimension == TextureDimension.Cube);

            var tempRTDesc = new RenderTextureDescriptor(cubemapSize, cubemapSize)
            {
                dimension = TextureDimension.Tex2D,
                colorFormat = isHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.ARGB32,
                depthBufferBits = 0,
                msaaSamples = 1,
                sRGB = isSrgb,
                autoGenerateMips = false
            };
            var tempRT = RenderTexture.GetTemporary(tempRTDesc);

            try
            {
                for (var i = 0; i < 6; i++)
                {
                    var tex = GetSixSidedTexture(parameters, i);
                    if (tex == null)
                    {
                        continue;
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    var previousActive = RenderTexture.active;
                    try
                    {
                        // Graphics.Blit は圧縮テクスチャもサンプラー経由で直接サンプリング可能なため ConvertTexture の迂回は不要。
                        // また Cubemap 面の仕様（V軸下向き）に合わせて通常時は Y 軸を反転させて転送する。
                        // 水平（H）または垂直（V）フリップが有効な場合は反転を適用する。
                        var flipH = parameters.FaceFlips.GetFlipH(i);
                        var flipV = parameters.FaceFlips.GetFlipV(i);
                        var scaleX = flipH ? -1f : 1f;
                        var offsetX = flipH ? 1f : 0f;
                        var scaleY = flipV ? 1f : -1f;
                        var offsetY = flipV ? 0f : 1f;

                        Graphics.Blit(tex, tempRT, new Vector2(scaleX, scaleY), new Vector2(offsetX, offsetY));

                        Graphics.CopyTexture(tempRT, 0, 0, cubeRT, i, 0);
                    }
                    finally
                    {
                        RenderTexture.active = previousActive;
                    }
                }

                if (cubeRT.useMipMap && !cubeRT.autoGenerateMips)
                {
                    cubeRT.GenerateMips();
                }

                return cubeRT;
            }
            finally
            {
                RenderTexture.ReleaseTemporary(tempRT);
            }
        }

        private static Texture2D GetSixSidedTexture(in CubemapRenderParameters parameters, int index)
        {
            return index switch
            {
                0 => parameters.SixSidedXPlus,
                1 => parameters.SixSidedXMinus,
                2 => parameters.SixSidedYPlus,
                3 => parameters.SixSidedYMinus,
                4 => parameters.SixSidedZPlus,
                5 => parameters.SixSidedZMinus,
                _ => null!
            };
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
