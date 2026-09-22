using System.IO;
using UnityEngine;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     パスの絶対パス解決、親ディレクトリの自動生成、および拡張子に応じたエクスポートファイルパスの決定を担当する汎用パスユーティリティクラス。
    /// </summary>
    public static class PathUtility
    {
        /// <summary>
        ///     指定されたパスを OS のファイルシステムで安全に扱える絶対パスに変換します。
        ///     "Assets/..." などのプロジェクト相対パスは、Unity プロジェクトルートを基準とした絶対パスに解決されます。
        /// </summary>
        public static string ToFullPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return path;
            }

            if (Path.IsPathRooted(path))
            {
                return Path.GetFullPath(path);
            }

            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            return !string.IsNullOrEmpty(projectRoot)
                ? Path.GetFullPath(Path.Combine(projectRoot, path))
                : Path.GetFullPath(path);
        }

        /// <summary>
        ///     ファイルパスの親ディレクトリが存在しない場合、安全に作成します。
        /// </summary>
        public static void EnsureDirectoryExists(string filePath)
        {
            var fullPath = ToFullPath(filePath);
            var dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        /// <summary>
        ///     指定されたパスの拡張子と HDR フラグに基づいて最終的なエクスポートファイルパスを決定します。
        /// </summary>
        public static string ResolveExportFilePath(string rawPath, bool isHDR)
        {
            if (isHDR)
            {
                return Path.ChangeExtension(rawPath, ".exr");
            }

            var ext = Path.GetExtension(rawPath);
            if (string.IsNullOrEmpty(ext))
            {
                return Path.ChangeExtension(rawPath, ".png");
            }

            return ext.ToLowerInvariant() switch
            {
                ".tga" => Path.ChangeExtension(rawPath, ".tga"),
                ".jpg" => Path.ChangeExtension(rawPath, ".jpg"),
                ".jpeg" => Path.ChangeExtension(rawPath, ".jpeg"),
                _ => Path.ChangeExtension(rawPath, ".png")
            };
        }
    }
}
