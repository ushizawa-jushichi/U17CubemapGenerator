using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     Unity アセットの .meta ファイル操作および GUID（プロジェクト内参照）の維持を担当するユーティリティクラス。
    /// </summary>
    public static class AssetMetaUtility
    {
        private const int MaxRetryCount = 3;

        private static readonly Regex GuidRegex = new(@"^guid:\s*([0-9a-fA-F]{32})",
            RegexOptions.Multiline | RegexOptions.Compiled);

        /// <summary>
        ///     既存アセットの GUID を維持した状態で新規アセットを作成・上書き保存します。
        ///     解像度や内部フォーマットが変更されて新規インスタンスの生成が必要な場合でも、
        ///     プロジェクト内の他アセット（Prefab や Material 等）からの参照リンク（GUID）を破壊しません。
        /// </summary>
        public static async Awaitable<T> CreateAssetPreservingGuidAsync<T>(T newAsset, string assetPath,
            CancellationToken cancellationToken = default) where T : Object
        {
            if (newAsset == null)
            {
                throw new ArgumentNullException(nameof(newAsset));
            }

            if (string.IsNullOrEmpty(assetPath))
            {
                throw new ArgumentException("Asset path cannot be null or empty.", nameof(assetPath));
            }

            var existingGuid = AssetDatabase.AssetPathToGUID(assetPath);

            // 新規アセットとして保存
            AssetDatabase.CreateAsset(newAsset, assetPath);
            AssetDatabase.SaveAssetIfDirty(newAsset);

            // 以前の GUID が存在していれば .meta を復元して同期インポート
            if (!string.IsNullOrEmpty(existingGuid))
            {
                if (await RestoreAssetGuidAsync(assetPath, existingGuid, cancellationToken))
                {
                    AssetDatabase.ImportAsset(assetPath,
                        ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

                    // GUID が正常に認識されたかを検証
                    var reloadedGuid = AssetDatabase.AssetPathToGUID(assetPath);
                    if (!string.Equals(reloadedGuid, existingGuid, StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.LogError(
                            $"[AssetMetaUtility] GUID mismatch for '{assetPath}' after restore! Expected: {existingGuid}, Actual: {reloadedGuid}");
                    }

                    var reloaded = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                    if (reloaded != null)
                    {
                        return reloaded;
                    }
                }
                else
                {
                    Debug.LogWarning(
                        $"[AssetMetaUtility] Could not restore original GUID '{existingGuid}' for '{assetPath}'. Existing references might be broken.");
                }
            }

            return newAsset;
        }

        /// <summary>
        ///     指定されたアセットの .meta ファイル内の guid 行を元の GUID に書き換えます。
        ///     ディスクフラッシュ待ちおよびファイルロック解除のための非同期リトライ機構を備えています。
        /// </summary>
        public static async Awaitable<bool> RestoreAssetGuidAsync(string assetPath, string originalGuid,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(assetPath) || string.IsNullOrEmpty(originalGuid))
            {
                return false;
            }

            var metaPath = AssetDatabase.GetTextMetaFilePathFromAssetPath(assetPath);

            for (var attempt = 1; attempt <= MaxRetryCount; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!File.Exists(metaPath))
                {
                    if (attempt < MaxRetryCount)
                    {
                        await EditorAwaitableUtility.YieldAsync(cancellationToken);
                        continue;
                    }

                    Debug.LogWarning($"[AssetMetaUtility] Meta file '{metaPath}' was not found.");
                    return false;
                }

                try
                {
                    var content = File.ReadAllText(metaPath, Encoding.UTF8);
                    var match = GuidRegex.Match(content);
                    if (!match.Success)
                    {
                        return false;
                    }

                    var currentGuid = match.Groups[1].Value;
                    if (string.Equals(currentGuid, originalGuid, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    var updatedContent = GuidRegex.Replace(content, $"guid: {originalGuid}", 1);

                    var attrs = File.GetAttributes(metaPath);
                    var isReadOnly = (attrs & FileAttributes.ReadOnly) != 0;
                    if (isReadOnly)
                    {
                        File.SetAttributes(metaPath, attrs & ~FileAttributes.ReadOnly);
                    }

                    try
                    {
                        File.WriteAllText(metaPath, updatedContent, new UTF8Encoding(false));
                        return true;
                    }
                    finally
                    {
                        if (isReadOnly && File.Exists(metaPath))
                        {
                            File.SetAttributes(metaPath, attrs);
                        }
                    }
                }
                catch (IOException) when (attempt < MaxRetryCount)
                {
                    await EditorAwaitableUtility.YieldAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[AssetMetaUtility] Failed to restore GUID for '{metaPath}': {ex.Message}");
                    return false;
                }
            }

            return false;
        }
    }
}
