using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     プレビューオブジェクトの選択や回転・表示設定を行う "Preview" タブページの UI クラス。
    /// </summary>
    public sealed class PreviewTabPage : TabPageBase
    {
        private readonly EditorState _editorState;
        private readonly Action _requestRedrawWindow;
        private readonly Settings _settings;

        private PreviewTabPageInfoBoxSection _infoBoxSection = null!;
        private Toggle _previewBgSkyboxToggle = null!;
        private EnumField _previewObjectSelectionEnumField = null!;
        private Button _resetRotationButton = null!;
        private Button _resetSettingsButton = null!;
        private RotationField _rotationField = null!;
        private Toggle _superSamplingToggle = null!;

        public PreviewTabPage(
            Settings settings,
            EditorState editorState,
            Action requestRedrawWindow)
        {
            _settings = settings;
            _editorState = editorState;
            _requestRedrawWindow = requestRedrawWindow;

            Assert.IsNotNull(_settings, $"{nameof(_settings)} is not attached.");
            Assert.IsNotNull(_editorState, $"{nameof(_editorState)} is not attached.");

            Create();
        }

        public override string Id => "Preview";
        public override string Title => "Preview";

        public event Action? OnResetSettingsRequested;
        public event Action<PreviewObjectType>? OnPreviewObjectChanged;

        public void SetRotation(Vector3 value)
        {
            _rotationField.SetValueWithoutNotify(new Vector3(
                _editorState.PreviewRotationXEnable ? value.x : _rotationField.value.x,
                _editorState.PreviewRotationYEnable ? value.y : _rotationField.value.y,
                _editorState.PreviewRotationZEnable ? value.z : _rotationField.value.z));
        }

        /// <summary>
        ///     Undo/Redo やリセット時など、外部から状態が変更された際にプレビュー UI の表示を再同期します。
        /// </summary>
        public void RefreshUI()
        {
            SetRotation(_editorState.PreviewRotation);
            _infoBoxSection?.UpdateRenderPipeline();
        }

        public void SetIntermediateSize(Vector2Int value)
        {
            _infoBoxSection.SetIntermediateSize(value);
        }

        public void OnGeometryChanged(Vector2 size)
        {
            _infoBoxSection.OnGeometryChanged(size);
        }

        protected override void OnDispose()
        {
            TabContent.Clear();
            OnResetSettingsRequested = null;
            OnPreviewObjectChanged = null;
        }

        private void Create()
        {
            var titleLabel = new Label("Preview Settings");
            titleLabel.style.fontSize = 16;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.marginBottom = 10;
            TabContent.Add(titleLabel);

            _previewObjectSelectionEnumField = new EnumField("Preview Object", default(PreviewObjectType));
            _previewObjectSelectionEnumField.style.maxWidth = 250;
            _previewObjectSelectionEnumField.bindingPath =
                EditorState.BindPath(nameof(EditorState.PreviewObjectSelection));
            _previewObjectSelectionEnumField.RegisterValueChangedCallback(evt =>
            {
                var value = (PreviewObjectType)evt.newValue;
                ConfigureUI(value);
                OnPreviewObjectChanged?.Invoke(value);
                _requestRedrawWindow.Invoke();
            });
            TabContent.Add(_previewObjectSelectionEnumField);

            _superSamplingToggle = new Toggle("Super Sampling");
            _superSamplingToggle.style.maxWidth = 250;
            _superSamplingToggle.bindingPath =
                EditorState.BindPath(nameof(EditorState.SuperSampling));
            _superSamplingToggle.RegisterValueChangedCallback(_ => _requestRedrawWindow.Invoke());
            TabContent.Add(_superSamplingToggle);

            _previewBgSkyboxToggle = new Toggle("Preview Background Skybox");
            _previewBgSkyboxToggle.style.maxWidth = 250;
            _previewBgSkyboxToggle.bindingPath =
                EditorState.BindPath(nameof(EditorState.PreviewBgSkybox));
            _previewBgSkyboxToggle.RegisterValueChangedCallback(_ => _requestRedrawWindow.Invoke());
            TabContent.Add(_previewBgSkyboxToggle);

            TabContent.Add(UIElementFactory.CreateSeparator());

            _rotationField = new RotationField();
            _rotationField.bindingPath =
                EditorState.BindPath(nameof(EditorState.PreviewRotation));
            _rotationField.RegisterValueChangedCallback(_ => _requestRedrawWindow.Invoke());
            _rotationField.ToggleX.bindingPath =
                EditorState.BindPath(nameof(EditorState.PreviewRotationXEnable));
            _rotationField.ToggleY.bindingPath =
                EditorState.BindPath(nameof(EditorState.PreviewRotationYEnable));
            _rotationField.ToggleZ.bindingPath =
                EditorState.BindPath(nameof(EditorState.PreviewRotationZEnable));
            TabContent.Add(_rotationField);

            _resetRotationButton = new Button(ResetRotation);
            _resetRotationButton.text = "Reset Rotation";
            _resetRotationButton.style.width = 120;
            TabContent.Add(_resetRotationButton);

            TabContent.Add(UIElementFactory.CreateSeparator());

            _resetSettingsButton = new Button(() => OnResetSettingsRequested?.Invoke());
            _resetSettingsButton.text = "Reset Settings";
            _resetSettingsButton.style.width = 120;
            TabContent.Add(_resetSettingsButton);

            TabContent.Add(UIElementFactory.CreateSeparator());

            _infoBoxSection = new PreviewTabPageInfoBoxSection();
            TabContent.Add(_infoBoxSection);

            ConfigureUI((PreviewObjectType)_previewObjectSelectionEnumField.value);
        }

        private void ResetRotation()
        {
            Undo.RecordObject(_editorState, "Reset Rotation");
            _editorState.ResetPreviewRotation(_settings.DefaultPreviewRotation);
            _rotationField.SetValueWithoutNotify(_settings.DefaultPreviewRotation);
            _requestRedrawWindow.Invoke();
        }

        public void ConfigureUI(PreviewObjectType previewObject)
        {
            var previewBgSkyboxAvailable = previewObject != PreviewObjectType.Skybox;

            _previewBgSkyboxToggle.SetEnabled(previewBgSkyboxAvailable);
        }
    }
}
