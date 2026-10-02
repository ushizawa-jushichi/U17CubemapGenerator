using System;
using UnityEditor;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     EditorState の EditorPrefs への保存・読み込みおよびデバウンス遅延保存を管理するマネージャー。
    /// </summary>
    public sealed class EditorStateSaver : IDisposable
    {
        private readonly EditorApplication.CallbackFunction _delayCallAction;
        private bool _isSavePending;
        private EditorState _targetState = null!;

        public EditorStateSaver()
        {
            _delayCallAction = OnDelayCallTimerExpired;
        }

        public void Dispose()
        {
            CancelPendingSave();
            _targetState = null!;
        }

        /// <summary>
        ///     遅延保存をリクエストする（連続した変更操作を1回の保存にまとめるデバウンス処理）。
        /// </summary>
        public void RequestSave(EditorState state)
        {
            if (state == null)
            {
                return;
            }

            _targetState = state;

            if (!_isSavePending)
            {
                _isSavePending = true;
                EditorApplication.delayCall += _delayCallAction;
            }
        }

        /// <summary>
        ///     予約されている遅延保存をキャンセルし、即座に保存を実行する。
        /// </summary>
        public void SaveImmediate(EditorState state)
        {
            CancelPendingSave();

            if (state != null)
            {
                state.SaveState();
            }
        }

        private void OnDelayCallTimerExpired()
        {
            if (!_isSavePending)
            {
                return;
            }

            _isSavePending = false;
            EditorApplication.delayCall -= _delayCallAction;

            if (_targetState != null)
            {
                _targetState.SaveState();
            }
        }

        private void CancelPendingSave()
        {
            if (_isSavePending)
            {
                EditorApplication.delayCall -= _delayCallAction;
                _isSavePending = false;
            }
        }
    }
}
