using System;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     非同期処理中（AsyncGPUReadback 待機時など）に EditMode でも確実にフレーム・GPU サイクルを進行させるため、
    ///     EditorApplication.update に EditorApplication.QueuePlayerLoopUpdate を登録し、
    ///     using スコープ終了時に確実に解除するスコープクラス。
    ///     参照カウント方式により、入れ子や複数のテクスチャ面の非同期処理が同時に走る場合でも安全に動作します。
    ///     <para>※ Unity エディター API（EditorApplication.update）依存およびスレッド同期を行っていないため、メインスレッド限定で使用すること。</para>
    /// </summary>
    public sealed class PlayerLoopUpdateScope : IDisposable
    {
        // ※ EditorApplication.update 操作のためメインスレッドからのみアクセスされる前提
        private static int s_MainThreadId = Thread.CurrentThread.ManagedThreadId;
        private static int s_RefCount;
        private static readonly EditorApplication.CallbackFunction s_Callback = ForceUpdate;
        private bool _isDisposed;

        public PlayerLoopUpdateScope(bool active = true)
        {
            _isDisposed = !active;
            if (active)
            {
                if (Thread.CurrentThread.ManagedThreadId != s_MainThreadId)
                {
                    Debug.LogWarning(
                        "[PlayerLoopUpdateScope] PlayerLoopUpdateScope was created from a thread other than the main thread.");
                    _isDisposed = true;
                    return;
                }

                if (s_RefCount == 0)
                {
                    EditorApplication.update += s_Callback;
                }

                s_RefCount++;
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            if (Thread.CurrentThread.ManagedThreadId != s_MainThreadId)
            {
                Debug.LogWarning(
                    "[PlayerLoopUpdateScope] PlayerLoopUpdateScope was disposed from a thread other than the main thread.");
                return;
            }

            s_RefCount--;
            if (s_RefCount <= 0)
            {
                s_RefCount = 0;
                EditorApplication.update -= s_Callback;
            }
        }

        [InitializeOnLoadMethod]
        [InitializeOnEnterPlayMode]
        private static void ResetOnDomainReload()
        {
            s_MainThreadId = Thread.CurrentThread.ManagedThreadId;
            s_RefCount = 0;
            EditorApplication.update -= s_Callback;
        }

        private static void ForceUpdate()
        {
            EditorApplication.QueuePlayerLoopUpdate();
        }
    }
}
