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
    ///     Equirectangular（正距円筒図法）パノラマ画像へのエクスポート処理を担当するクラス。
    /// </summary>
    public static class CubemapEquirectangularExporter
    {
        private const int PassEquirectangular = 1;
        private const int EquirectangularWidthMultiplier = 2;
        private const float ProgressAssetCreation = 0.5f;
        private const float ProgressReadback = 0.6f;
        private const float ProgressEncoding = 0.8f;

        private static readonly int IDCubeTex = Shader.PropertyToID("_CubeTex");
        private static readonly int IDRotationY = Shader.PropertyToID("_RotationY");
        private static readonly int IDScale = Shader.PropertyToID("_Scale");
        private static readonly int IDOffset = Shader.PropertyToID("_Offset");

        public static async Awaitable<bool> ExportAsync(
            string exportPath,
            float equirectangularY,
            bool importAsCubemap,
            Material matBlitter,
            RenderTexture cubeRT,
            bool isProjectAsset,
            CancellationToken cancellationToken)
        {
            Assert.IsTrue(cubeRT.dimension == TextureDimension.Cube);

            if (matBlitter == null)
            {
                var errorMsg = "Export failed: Required Blitter Material is missing.";
                Debug.LogError($"[CubemapEquirectangularExporter] {errorMsg}");
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog("Export Failed", errorMsg, "OK");
                }

                return false;
            }

            var isHDR = GraphicsFormatUtility.IsHDRFormat(cubeRT.graphicsFormat);
            var filePath = PathUtility.ResolveExportFilePath(exportPath, isHDR);

            var cubemapSize = cubeRT.width;
            var tempTex = (Texture2D)null!;
            var tempRT = (RenderTexture)null!;
            var useMipmap = cubeRT.useMipMap;
            var isSrgb = GraphicsFormatUtility.IsSRGBFormat(cubeRT.graphicsFormat);
            var textureFormat = isHDR ? TextureFormat.RGBAHalf : TextureFormat.RGBA32;

            var targetWidth = cubemapSize * EquirectangularWidthMultiplier;
            var targetHeight = cubemapSize;
            var maxTextureSize = SystemInfo.maxTextureSize;

            if (targetWidth > maxTextureSize || targetHeight > maxTextureSize)
            {
                var errorMsg =
                    $"The required image size for Equirectangular ({targetWidth}x{targetHeight}) exceeds the GPU maximum supported texture size ({maxTextureSize}x{maxTextureSize}). Please reduce the output resolution.";
                Debug.LogError($"[CubemapEquirectangularExporter] {errorMsg}");
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog("Export Failed", errorMsg, "OK");
                }

                return false;
            }

            var previousActive = RenderTexture.active;
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = linkedCts.Token;

            var progressId = Progress.Start("Exporting Equirectangular", "Processing image...",
                Progress.Options.Unmanaged);
            Progress.RegisterCancelCallback(progressId, () =>
            {
                linkedCts.Cancel();
                return true;
            });

            try
            {
                Progress.Report(progressId, ProgressAssetCreation, "Processing image...");

                matBlitter!.SetTexture(IDCubeTex, cubeRT);
                matBlitter.SetFloat(IDRotationY, equirectangularY * Mathf.Deg2Rad);

                var graphicsFormat = isHDR
                    ? GraphicsFormat.R16G16B16A16_SFloat
                    : isSrgb
                        ? GraphicsFormat.R8G8B8A8_SRGB
                        : GraphicsFormat.R8G8B8A8_UNorm;

                var rtDesc = new RenderTextureDescriptor(cubemapSize * EquirectangularWidthMultiplier, cubemapSize)
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
                Graphics.Blit(Texture2D.blackTexture, tempRT, matBlitter, PassEquirectangular);

                tempTex = new Texture2D(tempRT.width, tempRT.height, textureFormat, false, !isSrgb);
                // 2D RenderTexture (tempRT) は Blit により正立した正距円筒画像として描画されているため、
                // RequestReadbackIntoTextureAsync の Texture2D 標準 UV 座標系読み出し結果をそのまま反転なしで保存する
                await CubemapReadbackUtility.RequestReadbackIntoTextureAsync(
                    tempRT, tempTex, 0, token, "Equirectangular projection");

                Progress.Report(progressId, ProgressEncoding, "Encoding image...");

                PathUtility.EnsureDirectoryExists(filePath);
                tempTex.Apply(false, false);
                var bytes = TextureFileUtility.EncodeTexture(tempTex, filePath, isHDR);
                await TextureFileUtility.WriteAllBytesAsync(filePath, bytes, token);

                if (isProjectAsset)
                {
                    CubemapTextureImporterUtility.ConfigureTextureImporter(filePath, importAsCubemap, useMipmap,
                        isSrgb);
                    EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Texture>(filePath));
                }

                Debug.Log($"Cubemap successfully generated at: {filePath}");
                return true;
            }
            finally
            {
                Progress.Remove(progressId);
                matBlitter!.SetTexture(IDCubeTex, null); // 共有アセットへの参照残留を防止
                RenderTexture.active = previousActive;

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
