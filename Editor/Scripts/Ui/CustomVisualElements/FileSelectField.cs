using System;
using System.IO;
using UnityEditor;
using UnityEngine.UIElements;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     ファイルパスのテキスト入力と「Select」ボタンによるファイル保存ダイアログを統合した VisualElement。
    /// </summary>
    public class FileSelectField : VisualElement, INotifyValueChanged<string>
    {
        private readonly TextField _textField;

        private Action<string>? _onPathChanged;

        public FileSelectField(string caption, string filePath, Func<string, string?>? onBrowseClicked = null)
        {
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;

            _textField = new TextField(caption);
            _textField.value = filePath;
            _textField.style.flexGrow = 1;

            // 内部 TextField の変更を FileSelectField 自身の ChangeEvent<string> として再通知
            _textField.RegisterValueChangedCallback(evt =>
            {
                evt.StopPropagation();

                using var customEvt = ChangeEvent<string>.GetPooled(evt.previousValue, evt.newValue);
                customEvt.target = this;
                SendEvent(customEvt);

                _onPathChanged?.Invoke(evt.newValue);
            });

            var button = new Button(() =>
            {
                var selectedPath = onBrowseClicked != null
                    ? onBrowseClicked(_textField.value)
                    : OpenDefaultSaveDialog(_textField.value);

                if (!string.IsNullOrEmpty(selectedPath))
                {
                    value = selectedPath!;
                }
            });
            button.text = "Select...";
            button.style.width = 60;
            button.style.flexShrink = 0;

            Add(_textField);
            Add(button);
        }

        public string value
        {
            get => _textField.value;
            set
            {
                if (string.Equals(_textField.value, value, StringComparison.Ordinal))
                {
                    return;
                }

                var previousValue = _textField.value;
                SetValueWithoutNotify(value);

                using var evt = ChangeEvent<string>.GetPooled(previousValue, value);
                evt.target = this;
                SendEvent(evt);

                _onPathChanged?.Invoke(value);
            }
        }

        public void SetValueWithoutNotify(string newValue)
        {
            _textField.SetValueWithoutNotify(newValue);
        }

        /// <summary>
        ///     値変更時のコールバックを登録します（既存コード互換用）。
        /// </summary>
        public void RegisterValueChangedCallback(Action<string> callback)
        {
            _onPathChanged += callback;
        }

        /// <summary>
        ///     値変更時のコールバックを解除します（既存コード互換用）。
        /// </summary>
        public void UnregisterValueChangedCallback(Action<string> callback)
        {
            _onPathChanged -= callback;
        }

        private static string? OpenDefaultSaveDialog(string currentPath)
        {
            var defaultDir = "Assets";
            var defaultName = "GeneratedCubemap";
            var ext = "cubemap";

            if (!string.IsNullOrEmpty(currentPath))
            {
                var dir = Path.GetDirectoryName(currentPath);
                if (!string.IsNullOrEmpty(dir))
                {
                    defaultDir = dir;
                }

                var name = Path.GetFileNameWithoutExtension(currentPath);
                if (!string.IsNullOrEmpty(name))
                {
                    defaultName = name;
                }

                var extension = Path.GetExtension(currentPath).TrimStart('.');
                if (!string.IsNullOrEmpty(extension))
                {
                    ext = extension;
                }
            }

            return EditorUtility.SaveFilePanelInProject("Save Export File", defaultName, ext, "Select destination",
                defaultDir);
        }
    }
}
