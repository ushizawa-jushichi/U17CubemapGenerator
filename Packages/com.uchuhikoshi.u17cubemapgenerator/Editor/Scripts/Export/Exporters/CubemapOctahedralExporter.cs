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
    ///     Octahedral（八面体マッピング）画像へのエクスポート処理を担当するクラス。
    /// </summary>
    public static class CubemapOctahedralExporter
    {
        private const int PassOctahedral = 4;
        private const float ProgressAssetCreation = 0.5f;
        private const float ProgressReadback = 0.6f;
        private const float ProgressEncoding = 0.8f;

        private static readonly int IDCubeTex = Shader.PropertyToID("_CubeTex");
        private static readonly int IDRotationY = Shader.PropertyToID("_RotationY");
        private static readonly int IDYUp = Shader.PropertyToID("_YUp");

        public static async Awaitable<bool> ExportAsync(
            string exportPath,
            bool yUp,
            float rotationY,
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
                Debug.LogError($"[CubemapOctahedralExporter] {errorMsg}");
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

            var targetWidth = cubemapSize;
            var targetHeight = cubemapSize;
            var maxTextureSize = SystemInfo.maxTextureSize;

            if (targetWidth > maxTextureSize || targetHeight > maxTextureSize)
            {
                var errorMsg =
                    $"The required image size for Octahedral ({targetWidth}x{targetHeight}) exceeds the GPU maximum supported texture size ({maxTextureSize}x{maxTextureSize}). Please reduce the output resolution.";
                Debug.LogError($"[CubemapOctahedralExporter] {errorMsg}");
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog("Export Failed", errorMsg, "OK");
                }

                return false;
            }

            var previousActive = RenderTexture.active;
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = linkedCts.Token;

            var progressId = Progress.Start("Exporting Octahedral", "Processing image...",
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
                matBlitter.SetFloat(IDRotationY, rotationY * Mathf.Deg2Rad);
                matBlitter.SetFloat(IDYUp, yUp ? 1.0f : 0.0f);

                var graphicsFormat = isHDR
                    ? GraphicsFormat.R16G16B16A16_SFloat
                    : isSrgb
                        ? GraphicsFormat.R8G8B8A8_SRGB
                        : GraphicsFormat.R8G8B8A8_UNorm;

                var rtDesc = new RenderTextureDescriptor(targetWidth, targetHeight)
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
                Graphics.Blit(Texture2D.blackTexture, tempRT, matBlitter, PassOctahedral);

                tempTex = new Texture2D(tempRT.width, tempRT.height, textureFormat, false, !isSrgb);
                await CubemapReadbackUtility.RequestReadbackIntoTextureAsync(
                    tempRT, tempTex, 0, token, "Octahedral projection");

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

                Debug.Log($"Cubemap successfully exported as Octahedral at: {filePath}");
                return true;
            }
            finally
            {
                Progress.Remove(progressId);
                matBlitter!.SetTexture(IDCubeTex, null);
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
