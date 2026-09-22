using System.IO;
using System.Threading;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     単一の Cubemap アセット (.cubemap) へのエクスポート処理を担当するクラス。
    /// </summary>
    public static class CubemapAssetExporter
    {
        private const float ProgressAssetCreation = 0.5f;

        public static async Awaitable<bool> ExportAsync(
            string exportPath,
            RenderTexture cubeRT,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var path = Path.ChangeExtension(exportPath, ".cubemap");
            PathUtility.EnsureDirectoryExists(path);

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = linkedCts.Token;

            Cubemap cubemap = null!;

            var progressId = Progress.Start("Exporting Cubemap", "Creating Cubemap asset...",
                Progress.Options.Unmanaged);
            Progress.RegisterCancelCallback(progressId, () =>
            {
                linkedCts.Cancel();
                return true;
            });

            try
            {
                var progress = new SynchronousProgress<(int faceIndex, int totalFaces)>(p =>
                {
                    var ratio = (float)p.faceIndex / p.totalFaces * 0.8f;
                    Progress.Report(progressId, ratio, $"Reading cubemap face {p.faceIndex} / {p.totalFaces}...");
                });

                cubemap =
                    await CubemapTextureUtility.CreateCubemapFromCubeRenderTextureAsync(cubeRT, progress, token);
                token.ThrowIfCancellationRequested();

                if (cubemap == null)
                {
                    Debug.LogError("[CubemapAssetExporter] Failed to create Cubemap from RenderTexture.");
                    return false;
                }

                Progress.Report(progressId, 0.9f, "Saving Cubemap asset...");

                // Unity の仕様: Main Object の name がアセットのファイル名と一致していないと、
                // SaveAssets 時に「Main Object Name '' does not match filename '...'」エラーが発生する
                var assetName = Path.GetFileNameWithoutExtension(path);
                cubemap.name = assetName;

                var existingAsset = AssetDatabase.LoadAssetAtPath<Cubemap>(path);
                Cubemap targetAsset;

                var isSameSpec = existingAsset != null &&
                                 existingAsset.width == cubemap.width &&
                                 existingAsset.height == cubemap.height &&
                                 existingAsset.graphicsFormat == cubemap.graphicsFormat &&
                                 existingAsset.mipmapCount == cubemap.mipmapCount;

                if (isSameSpec)
                {
                    // 同一スペックの場合はシリアライズデータ（CPU側ピクセル情報含む）を完全同期し、
                    // GPU 側のテクスチャも即時更新することで、GUID を維持したまま安全に上書き保存する
                    EditorUtility.CopySerialized(cubemap, existingAsset!);
                    Graphics.CopyTexture(cubemap, existingAsset!);
                    existingAsset!.name = assetName;
                    EditorUtility.SetDirty(existingAsset);
                    targetAsset = existingAsset;
                }
                else
                {
                    // 解像度やフォーマットが異なる場合:
                    // GUID を維持したまま新規アセットとして安全に再生成
                    targetAsset = await AssetMetaUtility.CreateAssetPreservingGuidAsync(cubemap, path, token);
                    cubemap = null!;
                }

                AssetDatabase.SaveAssetIfDirty(targetAsset);

                if (cubemap != null)
                {
                    Object.DestroyImmediate(cubemap);
                    cubemap = null!;
                }

                var createdAsset = AssetDatabase.LoadAssetAtPath<Cubemap>(path);
                if (createdAsset == null)
                {
                    Debug.LogError(
                        $"[CubemapAssetExporter] Failed to save Cubemap asset at '{path}'. Verify the path is within the project's 'Assets' directory.");
                    return false;
                }

                Debug.Log($"Cubemap successfully generated at: {path}");
                EditorGUIUtility.PingObject(createdAsset);
                return true;
            }
            catch
            {
                if (cubemap != null)
                {
                    Object.DestroyImmediate(cubemap);
                }

                throw;
            }
            finally
            {
                Progress.Remove(progressId);
            }
        }
    }
}
