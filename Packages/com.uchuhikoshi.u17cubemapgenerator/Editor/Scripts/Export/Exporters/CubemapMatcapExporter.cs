using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     Matcap（球状テクスチャ）形式へのエクスポート処理を担当するクラス。
    /// </summary>
    public static class CubemapMatcapExporter
    {
        private const int PassMatcap = 2;
        private const float ProgressRendering = 0.2f;
        private const float ProgressReadback = 0.6f;
        private const float ProgressEncoding = 0.8f;

        private static readonly int IDCubeTex = Shader.PropertyToID("_CubeTex");
        private static readonly int IDFillOutside = Shader.PropertyToID("_FillOutside");

        public static async Awaitable<bool> ExportAsync(
            string exportPath,
            bool matcapFillOutside,
            Material matBlitter,
            RenderTexture cubeRT,
            bool isProjectAsset,
            CancellationToken cancellationToken)
        {
            Assert.IsTrue(cubeRT.dimension == TextureDimension.Cube);
            Assert.IsTrue(matBlitter != null, $"{nameof(matBlitter)} is null or destroyed.");

            var isHDR = GraphicsFormatUtility.IsHDRFormat(cubeRT.graphicsFormat);
            var filePath = PathUtility.ResolveExportFilePath(exportPath, isHDR);

            var matcapSize = cubeRT.width;
            var tempTex = (Texture2D)null!;
            var tempRT = (RenderTexture)null!;
            var useMipmap = cubeRT.useMipMap;
            var isSrgb = GraphicsFormatUtility.IsSRGBFormat(cubeRT.graphicsFormat);
            var textureFormat = isHDR ? TextureFormat.RGBAHalf : TextureFormat.RGBA32;

            var previousRT = RenderTexture.active;
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = linkedCts.Token;

            var progressId = Progress.Start("Exporting Matcap", "Rendering Matcap...", Progress.Options.Unmanaged);
            Progress.RegisterCancelCallback(progressId, () =>
            {
                linkedCts.Cancel();
                return true;
            });

            try
            {
                Progress.Report(progressId, ProgressRendering, "Rendering Matcap...");

                matBlitter!.SetTexture(IDCubeTex, cubeRT);
                matBlitter.SetFloat(IDFillOutside, matcapFillOutside ? 1.0f : 0.0f);

                var graphicsFormat = isHDR
                    ? GraphicsFormat.R16G16B16A16_SFloat
                    : isSrgb
                        ? GraphicsFormat.R8G8B8A8_SRGB
                        : GraphicsFormat.R8G8B8A8_UNorm;

                var rtDesc = new RenderTextureDescriptor(matcapSize, matcapSize)
                {
                    dimension = TextureDimension.Tex2D,
                    graphicsFormat = graphicsFormat,
                    depthBufferBits = 0,
                    msaaSamples = 1,
                    sRGB = isSrgb,
                    autoGenerateMips = false,
                    enableRandomWrite = false
                };
                tempRT = RenderTexture.GetTemporary(rtDesc);

                Graphics.Blit(Texture2D.blackTexture, tempRT, matBlitter, PassMatcap);

                tempTex = new Texture2D(tempRT.width, tempRT.height, textureFormat, false, !isSrgb);
                // 2D RenderTexture (tempRT) は Blit により正立した Matcap 画像として描画されているため、
                // RequestReadbackIntoTextureAsync の Texture2D 標準 UV 座標系読み出し結果をそのまま反転なしで保存する
                await CubemapReadbackUtility.RequestReadbackIntoTextureAsync(
                    tempRT, tempTex, 0, token, "Matcap projection");

                Progress.Report(progressId, ProgressEncoding, "Encoding Matcap...");

                PathUtility.EnsureDirectoryExists(filePath);
                tempTex.Apply(false, false);
                var bytes = TextureFileUtility.EncodeTexture(tempTex, filePath, isHDR);
                await TextureFileUtility.WriteAllBytesAsync(filePath, bytes, token);

                if (isProjectAsset)
                {
                    CubemapTextureImporterUtility.ConfigureTextureImporter(filePath, false, useMipmap, isSrgb);
                    EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Texture2D>(filePath));
                }

                Debug.Log($"Matcap successfully generated at: {filePath} (FillOutside: {matcapFillOutside})");
                return true;
            }
            finally
            {
                matBlitter!.SetTexture(IDCubeTex, null); // 共有アセットへの参照残留を防止
                Progress.Remove(progressId);
                RenderTexture.active = previousRT;

                if (tempRT != null)
                {
                    RenderTexture.ReleaseTemporary(tempRT);
                }

                if (tempTex != null)
                {
                    Object.DestroyImmediate(tempTex);
                }
            }
        }
    }
}
