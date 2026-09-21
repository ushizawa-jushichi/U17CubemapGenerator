using System;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     SynchronizationContext.Post による遅延を行わず、メインスレッド上で即座（同期）に進捗コールバックを実行する IProgress&lt;T&gt; 実装。
    ///     Progress&lt;T&gt; の非同期 Post による EditorUtility.DisplayCancelableProgressBar と EditorApplication.delayCall
    ///     のデッドロックを防ぎます。
    /// </summary>
    public sealed class SynchronousProgress<T> : IProgress<T>
    {
        private readonly Action<T> _action;

        public SynchronousProgress(Action<T> action)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
        }

        public void Report(T value)
        {
            _action(value);
        }
    }
}
