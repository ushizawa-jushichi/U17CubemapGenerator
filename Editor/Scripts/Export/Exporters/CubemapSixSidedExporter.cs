using System.IO;
using System.Text;
using System.Threading;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     キューブマップの 6 面個別画像（_XPlus, _XMinus 等）へのエクスポート処理を担当するクラス。
    /// </summary>
    public static class CubemapSixSidedExporter
    {
        public static async Awaitable<bool> ExportAsync(
            string exportPath,
            string[] suffixes,
            RenderTexture cubeRT,
            bool isProjectAsset,
            CancellationToken cancellationToken)
        {
            Assert.IsTrue(cubeRT.dimension == TextureDimension.Cube);
            Assert.IsTrue(suffixes.Length >= CubemapRenderUtility.CubemapFaceCount);

            var isHDR = GraphicsFormatUtility.IsHDRFormat(cubeRT.graphicsFormat);
            var baseExportPath = PathUtility.ResolveExportFilePath(exportPath, isHDR);

            var cubemapSize = cubeRT.width;
            var tempFace = (Texture2D)null!;
            var extension = Path.GetExtension(baseExportPath);
            var pathWithoutExt = Path.ChangeExtension(baseExportPath, null);
            var useMipmap = cubeRT.useMipMap;
            var isSrgb = GraphicsFormatUtility.IsSRGBFormat(cubeRT.graphicsFormat);
            var textureFormat = isHDR ? TextureFormat.RGBAHalf : TextureFormat.RGBA32;
            var rowBuffer = default(NativeArray<byte>);
            var prevActive = RenderTexture.active;
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = linkedCts.Token;

            var progressId = Progress.Start("Exporting Six-Sided Cubemap", "Processing faces...",
                Progress.Options.Unmanaged);
            Progress.RegisterCancelCallback(progressId, () =>
            {
                linkedCts.Cancel();
                return true;
            });

            try
            {
                tempFace = new Texture2D(cubemapSize, cubemapSize, textureFormat, false);

                var sb = new StringBuilder();
                var exportPathFirst = string.Empty;
                var facePaths = new string[CubemapRenderUtility.CubemapFaceCount];

                for (var i = 0; i < CubemapRenderUtility.CubemapFaceCount; i++)
                {
                    token.ThrowIfCancellationRequested();

                    var progressRatio = (float)i / CubemapRenderUtility.CubemapFaceCount * 0.8f;
                    Progress.Report(progressId, progressRatio,
                        $"Processing face {i + 1} / {CubemapRenderUtility.CubemapFaceCount} ({suffixes[i]})...");

                    await CubemapReadbackUtility.RequestReadbackIntoTextureAsync(
                        cubeRT, tempFace, i, token, $"SixSided face {i}");

                    // RequestReadbackIntoTextureAsync は標準 Texture2D UV 座標系（左下原点）で読み出すが、
                    // Unity の Cubemap 面テクスチャは内部仕様上 V 軸が下向き（天頂が V=0）として保持されているため、
                    // 独立した 2D 画像ファイル（PNG/EXR）として天頂を上にした正立画像として保存するために垂直反転を行う。
                    PixelBufferUtility.VerticalFlip(tempFace, ref rowBuffer);
                    tempFace.Apply(false, false);

                    var facePath = string.Concat(pathWithoutExt, suffixes[i], extension);
                    facePaths[i] = facePath;

                    sb.Append(facePath);
                    if (i != CubemapRenderUtility.CubemapFaceCount - 1)
                    {
                        sb.Append(',');
                    }

                    if (i == 0)
                    {
                        exportPathFirst = facePath;
                    }

                    PathUtility.EnsureDirectoryExists(facePath);
                    var bytes = TextureFileUtility.EncodeTexture(tempFace, facePath, isHDR);
                    await TextureFileUtility.WriteAllBytesAsync(facePath, bytes, token);
                }

                if (isProjectAsset)
                {
                    Progress.Report(progressId, 0.9f, "Importing textures...");

                    for (var i = 0; i < facePaths.Length; i++)
                    {
                        CubemapTextureImporterUtility.ConfigureTextureImporter(facePaths[i], false, useMipmap,
                            isSrgb);
                    }
                }

                Debug.Log($"Cubemap successfully generated at: {sb}");
                if (isProjectAsset && !string.IsNullOrEmpty(exportPathFirst))
                {
                    EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Texture2D>(exportPathFirst));
                }

                return true;
            }
            finally
            {
                Progress.Remove(progressId);
                RenderTexture.active = prevActive;

                if (rowBuffer.IsCreated)
                {
                    rowBuffer.Dispose();
                }

                if (tempFace != null)
                {
                    Object.DestroyImmediate(tempFace);
                }
            }
        }
    }
}
