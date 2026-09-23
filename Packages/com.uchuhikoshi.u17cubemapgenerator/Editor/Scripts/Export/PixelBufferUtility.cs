using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     NativeArray を用いた CPU メモリ上の高速な画像ピクセルバッファ操作（行スワップ、反転等）を提供するユーティリティクラス。
    /// </summary>
    public static class PixelBufferUtility
    {
        /// <summary>
        ///     Texture2D の全ミップレベルのピクセルデータ（CPU メモリ）を上下反転します。
        ///     本メソッドは CPU メモリのみを変更するため、ファイルエンコードや GPU 表示を行う前に
        ///     呼び出し元で <c>tex.Apply(false, false)</c> を呼び出してネイティブ側にコミットしてください。
        /// </summary>
        /// <returns>反転処理が正常に完了した場合は true、サポート外フォーマット等でスキップされた場合は false</returns>
        public static bool VerticalFlip(Texture2D tex, ref NativeArray<byte> rowBuffer)
        {
            if (!TryValidateTexture(tex, out var bytesPerPixel))
            {
                return false;
            }

            // 最大行ピッチ（Mip 0）で一時行バッファを確保
            var maxRowPitch = tex.width * bytesPerPixel;
            if (!rowBuffer.IsCreated || rowBuffer.Length < maxRowPitch)
            {
                if (rowBuffer.IsCreated)
                {
                    rowBuffer.Dispose();
                }

                rowBuffer = new NativeArray<byte>(maxRowPitch, Allocator.Persistent);
            }

            // 全ミップレベルを反転
            for (var mip = 0; mip < tex.mipmapCount; mip++)
            {
                var mipWidth = Mathf.Max(1, tex.width >> mip);
                var mipHeight = Mathf.Max(1, tex.height >> mip);
                if (mipHeight <= 1)
                {
                    continue; // 1行以下の場合は反転不要
                }

                var mipData = tex.GetPixelData<byte>(mip);
                var rowPitch = mipWidth * bytesPerPixel;

                for (var y = 0; y < mipHeight / 2; y++)
                {
                    var topRowIndex = y * rowPitch;
                    var bottomRowIndex = (mipHeight - 1 - y) * rowPitch;
                    var topSlice = mipData.GetSubArray(topRowIndex, rowPitch);
                    var bottomSlice = mipData.GetSubArray(bottomRowIndex, rowPitch);
                    topSlice.CopyTo(rowBuffer);
                    bottomSlice.CopyTo(topSlice);
                    rowBuffer.CopyTo(bottomSlice);
                }
            }

            return true;
        }

        /// <summary>
        ///     Texture2D のピクセルデータ（CPU メモリ）を上下反転します。
        ///     内部で一時行バッファを自動確保・破棄します。
        /// </summary>
        /// <returns>反転処理が正常に完了した場合は true、サポート外フォーマット等でスキップされた場合は false</returns>
        public static bool VerticalFlip(Texture2D tex)
        {
            var rowBuffer = default(NativeArray<byte>);
            try
            {
                return VerticalFlip(tex, ref rowBuffer);
            }
            finally
            {
                if (rowBuffer.IsCreated)
                {
                    rowBuffer.Dispose();
                }
            }
        }

        /// <summary>
        ///     Texture2D の全ミップレベルのピクセルデータ（CPU メモリ）を左右反転（水平反転）します。
        ///     本メソッドは CPU メモリのみを変更するため、ファイルエンコードや GPU 表示を行う前に
        ///     呼び出し元で <c>tex.Apply(false, false)</c> を呼び出してネイティブ側にコミットしてください。
        /// </summary>
        /// <returns>反転処理が正常に完了した場合は true、サポート外フォーマット等でスキップされた場合は false</returns>
        public static bool HorizontalFlip(Texture2D tex)
        {
            if (!TryValidateTexture(tex, out var bytesPerPixel))
            {
                return false;
            }

            for (var mip = 0; mip < tex.mipmapCount; mip++)
            {
                var mipWidth = Mathf.Max(1, tex.width >> mip);
                var mipHeight = Mathf.Max(1, tex.height >> mip);
                if (mipWidth <= 1)
                {
                    continue; // 1列以下の場合は反転不要
                }

                var data = tex.GetPixelData<byte>(mip);
                var halfWidth = mipWidth / 2;

                if (bytesPerPixel == 4)
                {
                    var pixels = data.Reinterpret<uint>(1);
                    for (var y = 0; y < mipHeight; y++)
                    {
                        var rowOffset = y * mipWidth;
                        for (var x = 0; x < halfWidth; x++)
                        {
                            var leftIdx = rowOffset + x;
                            var rightIdx = rowOffset + (mipWidth - 1 - x);
                            var tmp = pixels[leftIdx];
                            pixels[leftIdx] = pixels[rightIdx];
                            pixels[rightIdx] = tmp;
                        }
                    }
                }
                else if (bytesPerPixel == 8)
                {
                    var pixels = data.Reinterpret<ulong>(1);
                    for (var y = 0; y < mipHeight; y++)
                    {
                        var rowOffset = y * mipWidth;
                        for (var x = 0; x < halfWidth; x++)
                        {
                            var leftIdx = rowOffset + x;
                            var rightIdx = rowOffset + (mipWidth - 1 - x);
                            var tmp = pixels[leftIdx];
                            pixels[leftIdx] = pixels[rightIdx];
                            pixels[rightIdx] = tmp;
                        }
                    }
                }
                else if (bytesPerPixel == 1)
                {
                    for (var y = 0; y < mipHeight; y++)
                    {
                        var rowOffset = y * mipWidth;
                        for (var x = 0; x < halfWidth; x++)
                        {
                            var leftIdx = rowOffset + x;
                            var rightIdx = rowOffset + (mipWidth - 1 - x);
                            var tmp = data[leftIdx];
                            data[leftIdx] = data[rightIdx];
                            data[rightIdx] = tmp;
                        }
                    }
                }
                else if (bytesPerPixel == 2)
                {
                    var pixels = data.Reinterpret<ushort>(1);
                    for (var y = 0; y < mipHeight; y++)
                    {
                        var rowOffset = y * mipWidth;
                        for (var x = 0; x < halfWidth; x++)
                        {
                            var leftIdx = rowOffset + x;
                            var rightIdx = rowOffset + (mipWidth - 1 - x);
                            var tmp = pixels[leftIdx];
                            pixels[leftIdx] = pixels[rightIdx];
                            pixels[rightIdx] = tmp;
                        }
                    }
                }
                else
                {
                    var rowPitch = mipWidth * bytesPerPixel;
                    for (var y = 0; y < mipHeight; y++)
                    {
                        var rowOffset = y * rowPitch;
                        for (var x = 0; x < halfWidth; x++)
                        {
                            var leftOffset = rowOffset + x * bytesPerPixel;
                            var rightOffset = rowOffset + (mipWidth - 1 - x) * bytesPerPixel;
                            for (var b = 0; b < bytesPerPixel; b++)
                            {
                                var tmp = data[leftOffset + b];
                                data[leftOffset + b] = data[rightOffset + b];
                                data[rightOffset + b] = tmp;
                            }
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        ///     Texture2D のピクセルデータ（CPU メモリ）を左右反転（水平反転）します。
        ///     VerticalFlip との呼び出しシグネチャ統一用オーバーロードです。
        /// </summary>
        /// <returns>反転処理が正常に完了した場合は true、サポート外フォーマット等でスキップされた場合は false</returns>
        public static bool HorizontalFlip(Texture2D tex, ref NativeArray<byte> rowBuffer)
        {
            return HorizontalFlip(tex);
        }

        /// <summary>
        ///     Texture2D の引数およびピクセルバッファの妥当性を検証し、1ピクセルあたりのバイト数を算出します。
        ///     サポート外フォーマットの場合は警告ログを出力して false を返します。
        /// </summary>
        private static bool TryValidateTexture(
            Texture2D tex,
            out int bytesPerPixel)
        {
            bytesPerPixel = 0;
            if (tex == null)
            {
                Debug.LogWarning("[PixelBufferUtility] Texture is null.");
                return false;
            }

            if (tex.width <= 0 || tex.height <= 0)
            {
                Debug.LogWarning(
                    $"[PixelBufferUtility] Texture '{tex.name}' has invalid dimensions: {tex.width}x{tex.height}.");
                return false;
            }

            if (!tex.isReadable)
            {
                Debug.LogWarning(
                    $"[PixelBufferUtility] Texture '{tex.name}' is not readable. Make sure Read/Write is enabled.");
                return false;
            }

            var format = tex.graphicsFormat;
            if (GraphicsFormatUtility.GetBlockWidth(format) != 1 ||
                GraphicsFormatUtility.GetBlockHeight(format) != 1 ||
                GraphicsFormatUtility.IsCompressedFormat(format))
            {
                Debug.LogWarning(
                    $"[PixelBufferUtility] Compressed or block-based format '{format}' on texture '{tex.name}' is not supported for CPU pixel operations.");
                return false;
            }

            bytesPerPixel = (int)GraphicsFormatUtility.GetBlockSize(format);
            if (bytesPerPixel <= 0)
            {
                Debug.LogWarning(
                    $"[PixelBufferUtility] Unsupported texture format '{format}' (BlockSize: {bytesPerPixel}) on texture '{tex.name}' for CPU pixel operations.");
                return false;
            }

            return true;
        }
    }
}
