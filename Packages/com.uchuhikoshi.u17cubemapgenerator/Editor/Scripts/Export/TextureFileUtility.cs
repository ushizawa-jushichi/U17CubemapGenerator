using System;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading;
using UnityEngine;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     テクスチャの各種画像フォーマット（PNG / JPG / TGA / EXR）へのエンコードおよび非同期ファイル書き込みを担当するユーティリティクラス。
    /// </summary>
    public static class TextureFileUtility
    {
        /// <summary>
        ///     ファイルパスの拡張子に応じたフォーマットで Texture2D をエンコードします。
        /// </summary>
        public static byte[] EncodeTexture(Texture2D texture, string filePath, bool isHDR)
        {
            if (isHDR)
            {
                return texture.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat);
            }

            var ext = Path.GetExtension(filePath)?.ToLowerInvariant();
            return ext switch
            {
                ".tga" => texture.EncodeToTGA(),
                ".jpg" or ".jpeg" => texture.EncodeToJPG(100),
                _ => texture.EncodeToPNG()
            };
        }

        /// <summary>
        ///     バイト配列を非同期でファイルに書き込みます。
        ///     パスは自動的に絶対パスに解決されてから System.IO.File に渡されます。
        ///     例外やキャンセルが発生した場合でも、必ず Unity のメインスレッドに復帰してから例外を再スローすることで、
        ///     呼び出し側の finally ブロック等で安全に Unity API（オブジェクト破棄や UI 操作）が実行できるようにします。
        /// </summary>
        public static async Awaitable WriteAllBytesAsync(
            string filePath,
            byte[] bytes,
            CancellationToken cancellationToken = default)
        {
            var fullPath = PathUtility.ToFullPath(filePath);
            Exception? capturedException = null;

            try
            {
                PathUtility.EnsureDirectoryExists(fullPath);

                if (File.Exists(fullPath))
                {
                    var attrs = File.GetAttributes(fullPath);
                    if ((attrs & FileAttributes.ReadOnly) != 0)
                    {
                        File.SetAttributes(fullPath, attrs & ~FileAttributes.ReadOnly);
                    }
                }

                await File.WriteAllBytesAsync(fullPath, bytes, cancellationToken);
            }
            catch (Exception ex)
            {
                capturedException = ex;
            }

            await Awaitable.MainThreadAsync();

            cancellationToken.ThrowIfCancellationRequested();

            if (capturedException != null)
            {
                ExceptionDispatchInfo.Capture(capturedException).Throw();
            }
        }
    }
}
