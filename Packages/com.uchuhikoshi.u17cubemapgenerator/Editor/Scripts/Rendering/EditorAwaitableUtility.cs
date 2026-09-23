using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     EditMode および PlayMode の両方で安全にフレーム待機（メインスレッド・GPUサイクルの進行）を行うユーティリティ。
    ///     EditMode では PlayerLoop をキューイングした上で UnitySynchronizationContext (Task.Yield) 経由で譲渡することで、
    ///     AsyncGPUReadback 等の GPU コマンド処理を確実に完了させます。
    /// </summary>
    public static class EditorAwaitableUtility
    {
        public static async Awaitable YieldAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!Application.isPlaying)
            {
                EditorApplication.QueuePlayerLoopUpdate();
                await Task.Yield();
            }
            else
            {
                await Awaitable.NextFrameAsync(cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
