using System;
using UnityEditor;
using UnityEngine;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     エクスポートされたテクスチャアセットの TextureImporter 設定（形状、ミップマップ、sRGB、Cubemap生成設定）を管理するユーティリティクラス。
    /// </summary>
    public static class CubemapTextureImporterUtility
    {
        /// <summary>
        ///     エクスポートされたテクスチャの TextureImporter 設定を適用します。
        /// </summary>
        /// <param name="exportPath">アセットの相対パス（Assets/...）</param>
        /// <param name="isCube">Cubemap としてインポートするかどうか</param>
        /// <param name="useMipmap">ミップマップを有効にするかどうか</param>
        /// <param name="isSrgb">sRGB カラーマップとして扱うかどうか</param>
        public static void ConfigureTextureImporter(
            string exportPath,
            bool isCube,
            bool useMipmap,
            bool isSrgb)
        {
            var textureImporter = AssetImporter.GetAtPath(exportPath) as TextureImporter;
            if (textureImporter == null)
            {
                AssetDatabase.ImportAsset(exportPath, ImportAssetOptions.ForceSynchronousImport);
                textureImporter = AssetImporter.GetAtPath(exportPath) as TextureImporter;
            }

            if (textureImporter != null)
            {
                var modified = false;
                var shape = isCube ? TextureImporterShape.TextureCube : TextureImporterShape.Texture2D;
                if (textureImporter.textureShape != shape)
                {
                    textureImporter.textureShape = shape;
                    modified = true;
                }

                if (textureImporter.mipmapEnabled != useMipmap)
                {
                    textureImporter.mipmapEnabled = useMipmap;
                    modified = true;
                }

                if (textureImporter.sRGBTexture != isSrgb)
                {
                    textureImporter.sRGBTexture = isSrgb;
                    modified = true;
                }

                if (modified)
                {
                    EditorUtility.SetDirty(textureImporter);
                    textureImporter.SaveAndReimport();
                }
            }
            else
            {
                Debug.LogError($"[CubemapTextureImporterUtility] Failed to locate TextureImporter at '{exportPath}'.");
            }
        }


        /// <summary>
        ///     レガシー形式（.cubemap アセット）のファイルパスであるかを判定します。
        /// </summary>
        public static bool IsCubemapFile(string assetPath)
        {
            return assetPath.EndsWith(".cubemap", StringComparison.OrdinalIgnoreCase);
        }
    }
}
