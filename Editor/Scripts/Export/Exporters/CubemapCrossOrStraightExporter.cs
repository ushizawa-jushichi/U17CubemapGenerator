using System;
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
    ///     Cross（十字展開）および Straight（一列展開）形式へのエクスポート処理を担当するクラス。
    /// </summary>
    public static class CubemapCrossOrStraightExporter
    {
        private const float ProgressRendering = 0.2f;
        private const float ProgressReadback = 0.6f;
        private const float ProgressEncoding = 0.8f;

        private static readonly Vector2Int GridCrossHorizontal = new(4, 3);
        private static readonly Vector2Int GridCrossVertical = new(3, 4);
        private static readonly Vector2Int GridStraightHorizontal = new(6, 1);
        private static readonly Vector2Int GridStraightVertical = new(1, 6);

        // 各面をクロス／ストレート配置のキャンバス上で正しい向きに合わせるため、
        // デフォルトで垂直反転（FlipV）を適用する（縦十字展開の -Z 面のみ水平反転 FlipH を指定）
        private static readonly FlipType[] s_DefaultFlips =
        {
            FlipType.FlipV, FlipType.FlipV, FlipType.FlipV, FlipType.FlipV, FlipType.FlipV, FlipType.FlipV
        };

        private static readonly FlipType[] s_CrossVerticalFlips =
        {
            FlipType.FlipV, FlipType.FlipV, FlipType.FlipV, FlipType.FlipV, FlipType.FlipV, FlipType.FlipH
        };

        private static readonly Vector2Int[] s_CrossHorizontalPositions =
        {
            new(2, 1), new(0, 1), new(1, 2), new(1, 0), new(1, 1), new(3, 1)
        };

        private static readonly Vector2Int[] s_CrossVerticalPositions =
        {
            new(2, 2), new(0, 2), new(1, 3), new(1, 1), new(1, 2), new(1, 0)
        };

        private static readonly Vector2Int[] s_StraightHorizontalPositions =
        {
            new(0, 0), new(1, 0), new(2, 0), new(3, 0), new(4, 0), new(5, 0)
        };

        private static readonly Vector2Int[] s_StraightVerticalPositions =
        {
            new(0, 5), new(0, 4), new(0, 3), new(0, 2), new(0, 1), new(0, 0)
        };

        private static (Vector2Int gridSize, Vector2Int[] facePositions, FlipType[] flips) GetLayoutConfig(
            OutputLayoutType layout)
        {
            return layout switch
            {
                OutputLayoutType.CrossHorizontal => (GridCrossHorizontal, s_CrossHorizontalPositions, s_DefaultFlips),
                OutputLayoutType.CrossVertical => (GridCrossVertical, s_CrossVerticalPositions, s_CrossVerticalFlips),
                OutputLayoutType.StraightHorizontal => (GridStraightHorizontal, s_StraightHorizontalPositions,
                    s_DefaultFlips),
                OutputLayoutType.StraightVertical => (GridStraightVertical, s_StraightVerticalPositions,
                    s_DefaultFlips),
                _ => throw new ArgumentOutOfRangeException(nameof(layout), layout, null)
            };
        }

        public static async Awaitable<bool> ExportAsync(
            string exportPath,
            OutputLayoutType layout,
            bool importAsCubemap,
            RenderTexture cubeRT,
            bool isProjectAsset,
            CancellationToken cancellationToken)
        {
            Assert.IsTrue(cubeRT.dimension == TextureDimension.Cube);

            var isHDR = GraphicsFormatUtility.IsHDRFormat(cubeRT.graphicsFormat);
            var filePath = PathUtility.ResolveExportFilePath(exportPath, isHDR);

            var cubemapSize = cubeRT.width;
            var tempTex = (Texture2D)null!;
            var useMipmap = cubeRT.useMipMap;
            var isSrgb = GraphicsFormatUtility.IsSRGBFormat(cubeRT.graphicsFormat);
            var textureFormat = isHDR ? TextureFormat.RGBAHalf : TextureFormat.RGBA32;

            var (gridSize, destPos, flip) = GetLayoutConfig(layout);
            var tmpSize = new Vector2Int(cubemapSize * gridSize.x, cubemapSize * gridSize.y);
            var maxTextureSize = SystemInfo.maxTextureSize;

            if (tmpSize.x > maxTextureSize || tmpSize.y > maxTextureSize)
            {
                var errorMsg =
                    $"The required image size for {layout} ({tmpSize.x}x{tmpSize.y}) exceeds the GPU maximum supported texture size ({maxTextureSize}x{maxTextureSize}). Please reduce the output resolution or choose a different layout.";
                Debug.LogError($"[CubemapCrossOrStraightExporter] {errorMsg}");
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog("Export Failed", errorMsg, "OK");
                }

                return false;
            }

            var tempFace = (Texture2D)null!;

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = linkedCts.Token;

            var progressId = Progress.Start("Exporting Cubemap", $"Exporting {layout}...", Progress.Options.Unmanaged);
            Progress.RegisterCancelCallback(progressId, () =>
            {
                linkedCts.Cancel();
                return true;
            });

            try
            {
                tempTex = new Texture2D(tmpSize.x, tmpSize.y, textureFormat, false, !isSrgb);
                tempFace = new Texture2D(cubemapSize, cubemapSize, textureFormat, false);

                var dstData = tempTex.GetRawTextureData<byte>();
                dstData.AsSpan().Clear();

                var bytesPerPixel = dstData.Length / (tmpSize.x * tmpSize.y);
                var facePitch = cubemapSize * bytesPerPixel;
                var dstPitch = tmpSize.x * bytesPerPixel;

                for (var i = 0; i < CubemapRenderUtility.CubemapFaceCount; i++)
                {
                    token.ThrowIfCancellationRequested();

                    var progressRatio = (float)i / CubemapRenderUtility.CubemapFaceCount * ProgressReadback;
                    Progress.Report(progressId, progressRatio,
                        $"Reading face {i + 1} / {CubemapRenderUtility.CubemapFaceCount}...");

                    await CubemapReadbackUtility.RequestReadbackIntoTextureAsync(
                        cubeRT, tempFace, i, token, $"Cross/Straight face {i}");

                    var faceData = tempFace.GetRawTextureData<byte>();
                    var destX = destPos[i].x * cubemapSize;
                    var destY = destPos[i].y * cubemapSize;
                    var flipType = flip[i];

                    CopyFaceToCanvas(faceData, dstData, cubemapSize, destX, destY,
                        facePitch, dstPitch, bytesPerPixel, flipType);
                }

                Progress.Report(progressId, ProgressEncoding, $"Encoding {layout}...");

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

                if (tempFace != null)
                {
                    Object.DestroyImmediate(tempFace);
                }

                if (tempTex != null)
                {
                    Object.DestroyImmediate(tempTex);
                }
            }
        }

        /// <summary>
        ///     面テクスチャをキャンバスバッファへコピーする。FlipType に応じて各行を転送する。
        /// </summary>
        private static void CopyFaceToCanvas(
            NativeArray<byte> faceData,
            NativeArray<byte> dstData,
            int cubemapSize,
            int destX, int destY,
            int facePitch, int dstPitch,
            int bytesPerPixel,
            FlipType flipType)
        {
            for (var y = 0; y < cubemapSize; y++)
            {
                var dstRowOffset = (destY + y) * dstPitch + destX * bytesPerPixel;
                CopyFaceRow(faceData, dstData, cubemapSize, y, facePitch, dstRowOffset, bytesPerPixel, flipType);
            }
        }

        /// <summary>
        ///     面テクスチャの 1 行分をキャンバスバッファの指定オフセットへ転送する。
        ///     FlipType によってソース行および水平方向の反転有無が変わる。
        /// </summary>
        private static void CopyFaceRow(
            NativeArray<byte> faceData,
            NativeArray<byte> dstData,
            int cubemapSize,
            int y,
            int facePitch,
            int dstRowOffset,
            int bytesPerPixel,
            FlipType flipType)
        {
            switch (flipType)
            {
                case FlipType.FlipH:
                {
                    var srcRowOffset = y * facePitch;
                    CopyRowHorizontalFlip(faceData, dstData, cubemapSize, srcRowOffset, dstRowOffset, facePitch,
                        bytesPerPixel);
                    break;
                }
                case FlipType.FlipV:
                {
                    var srcRowOffset = (cubemapSize - 1 - y) * facePitch;
                    var srcSlice = faceData.GetSubArray(srcRowOffset, facePitch);
                    var dstSlice = dstData.GetSubArray(dstRowOffset, facePitch);
                    srcSlice.CopyTo(dstSlice);
                    break;
                }
                case FlipType.FlipHV:
                {
                    var srcRowOffset = (cubemapSize - 1 - y) * facePitch;
                    CopyRowHorizontalFlip(faceData, dstData, cubemapSize, srcRowOffset, dstRowOffset, facePitch,
                        bytesPerPixel);
                    break;
                }
                default:
                {
                    var srcRowOffset = y * facePitch;
                    var srcSlice = faceData.GetSubArray(srcRowOffset, facePitch);
                    var dstSlice = dstData.GetSubArray(dstRowOffset, facePitch);
                    srcSlice.CopyTo(dstSlice);
                    break;
                }
            }
        }

        /// <summary>
        ///     src の指定行を水平反転してdst の指定オフセットへ書き込む。
        ///     bytesPerPixel が 4 / 8 の場合は型変換により高速化する。
        /// </summary>
        private static void CopyRowHorizontalFlip(
            NativeArray<byte> src,
            NativeArray<byte> dst,
            int cubemapSize,
            int srcRowOffset,
            int dstRowOffset,
            int facePitch,
            int bytesPerPixel)
        {
            if (bytesPerPixel == 4)
            {
                var srcRow = src.GetSubArray(srcRowOffset, facePitch).Reinterpret<uint>(1);
                var dstRow = dst.GetSubArray(dstRowOffset, facePitch).Reinterpret<uint>(1);
                for (var x = 0; x < cubemapSize; x++)
                {
                    dstRow[x] = srcRow[cubemapSize - 1 - x];
                }
            }
            else if (bytesPerPixel == 8)
            {
                var srcRow = src.GetSubArray(srcRowOffset, facePitch).Reinterpret<ulong>(1);
                var dstRow = dst.GetSubArray(dstRowOffset, facePitch).Reinterpret<ulong>(1);
                for (var x = 0; x < cubemapSize; x++)
                {
                    dstRow[x] = srcRow[cubemapSize - 1 - x];
                }
            }
            else
            {
                for (var x = 0; x < cubemapSize; x++)
                {
                    var srcPixelOffset = srcRowOffset + (cubemapSize - 1 - x) * bytesPerPixel;
                    var dstPixelOffset = dstRowOffset + x * bytesPerPixel;
                    for (var b = 0; b < bytesPerPixel; b++)
                    {
                        dst[dstPixelOffset + b] = src[srcPixelOffset + b];
                    }
                }
            }
        }

        private enum FlipType
        {
            None,
            FlipV,
            FlipH,
            FlipHV
        }
    }
}
