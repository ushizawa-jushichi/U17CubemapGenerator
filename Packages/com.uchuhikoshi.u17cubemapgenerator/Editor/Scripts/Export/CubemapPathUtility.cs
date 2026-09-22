using System;
using System.IO;
using UnityEngine;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     エクスポート先パスの解決、正規化、ディレクトリ自動生成、出力サイズ計算および検証を担当するユーティリティクラス。
    /// </summary>
    public static class CubemapPathUtility
    {
        /// <summary>
        ///     指定されたパスを正規化し、プロジェクト内パスへの変換やバリデーションを行います。
        /// </summary>
        public static bool TryResolveExportPath(
            string rawPath,
            OutputLayoutType layout,
            out string resolvedPath,
            out bool isProjectAsset,
            out string? errorMessage)
        {
            resolvedPath = string.Empty;
            isProjectAsset = false;
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(rawPath))
            {
                errorMessage = "Export path is null or empty.";
                return false;
            }

            var normalized = rawPath.Trim().Replace('\\', '/');

            // プロジェクト内の絶対パスであれば "Assets/..." に変換
            var dataPath = Application.dataPath.Replace('\\', '/');
            var dataPathWithSlash = dataPath.EndsWith("/") ? dataPath : dataPath + "/";

            if (normalized.Equals(dataPath, StringComparison.OrdinalIgnoreCase))
            {
                normalized = "Assets";
            }
            else if (normalized.StartsWith(dataPathWithSlash, StringComparison.OrdinalIgnoreCase))
            {
                normalized = "Assets/" + normalized.Substring(dataPathWithSlash.Length);
            }
            else
            {
                var projectRoot = Path.GetDirectoryName(dataPath)?.Replace('\\', '/');
                if (!string.IsNullOrEmpty(projectRoot))
                {
                    var projectRootWithSlash = projectRoot!.EndsWith("/") ? projectRoot : projectRoot + "/";
                    if (normalized.StartsWith(projectRootWithSlash, StringComparison.OrdinalIgnoreCase))
                    {
                        normalized = normalized.Substring(projectRootWithSlash.Length);
                    }
                }
            }

            isProjectAsset = normalized.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase);

            var fileName = Path.GetFileName(normalized);
            if (string.IsNullOrEmpty(fileName))
            {
                errorMessage = $"Path '{normalized}' does not contain a valid file name.";
                return false;
            }

            if (layout == OutputLayoutType.LegacyCubemap && !isProjectAsset)
            {
                errorMessage =
                    $"Cubemap asset (.cubemap) must be saved inside the 'Assets' folder. Provided path: '{normalized}'";
                return false;
            }

            resolvedPath = normalized;
            return true;
        }

        /// <summary>
        ///     指定された出力レイアウトとキューブマップ解像度に基づいて、最終的な 2D/Cube 画像のピクセルサイズを算出します。
        /// </summary>
        public static Vector2Int GetOutputImageSize(OutputLayoutType layout, int cubemapSize)
        {
            return layout switch
            {
                OutputLayoutType.CrossHorizontal => new Vector2Int(cubemapSize * 4, cubemapSize * 3),
                OutputLayoutType.CrossVertical => new Vector2Int(cubemapSize * 3, cubemapSize * 4),
                OutputLayoutType.StraightHorizontal => new Vector2Int(cubemapSize * 6, cubemapSize),
                OutputLayoutType.StraightVertical => new Vector2Int(cubemapSize, cubemapSize * 6),
                OutputLayoutType.Equirectangular => new Vector2Int(cubemapSize * 2, cubemapSize),
                OutputLayoutType.LegacyCubemap => new Vector2Int(cubemapSize, cubemapSize),
                OutputLayoutType.SixSided => new Vector2Int(cubemapSize, cubemapSize),
                OutputLayoutType.Matcap => new Vector2Int(cubemapSize, cubemapSize),
                _ => new Vector2Int(cubemapSize, cubemapSize)
            };
        }

        /// <summary>
        ///     指定された解像度とレイアウトの組み合わせが、GPU のサポートする最大テクスチャサイズ内か判定します。
        /// </summary>
        public static bool IsOutputSizeValid(OutputLayoutType layout, int cubemapSize, out string? errorMessage)
        {
            var size = GetOutputImageSize(layout, cubemapSize);
            var maxSize = layout == OutputLayoutType.LegacyCubemap
                ? SystemInfo.maxCubemapSize
                : SystemInfo.maxTextureSize;

            if (size.x > maxSize || size.y > maxSize)
            {
                errorMessage =
                    $"The required image size for {layout} ({size.x}x{size.y}) exceeds the GPU maximum supported texture size ({maxSize}x{maxSize}). Please reduce the output resolution or choose a different layout.";
                return false;
            }

            errorMessage = null;
            return true;
        }
    }
}
