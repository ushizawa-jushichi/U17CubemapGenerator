using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     キューブマップ生成・ブラー・エクスポート設定を行う "Generator" タブページの UI クラス。
    /// </summary>
    public sealed class GeneratorTabPage : TabPageBase
    {
        private readonly EditorState _editorState;
        private readonly Action _requestRenderBlurredCubemap;
        private readonly Action _requestRenderCubemap;
        private readonly List<string> _resolutionOptions;
        private readonly Settings _settings;

        private GeneratorTabBlurSettingsSection _blurSection = null!;
        private GeneratorTabExportSettingsSection _exportSection = null!;
        private InfoBox _infoBoxCubemapWarning = null!;
        private GeneratorTabInputSettingsSection _inputSection = null!;
        private Label _labelCubemapWarning = null!;
        private Button _redrawButton = null!;

        public GeneratorTabPage(
            Settings settings,
            EditorState editorState,
            IReadOnlyList<int> resolutions,
            Action requestRenderCubemap,
            Action requestRenderBlurredCubemap)
        {
            _settings = settings;
            _editorState = editorState;
            _resolutionOptions =
                resolutions != null ? resolutions.Select(r => r.ToString()).ToList() : new List<string>();
            _requestRenderCubemap = requestRenderCubemap;
            _requestRenderBlurredCubemap = requestRenderBlurredCubemap;

            Assert.IsNotNull(_settings, $"{nameof(_settings)} is not attached.");
            Assert.IsNotNull(_editorState, $"{nameof(_editorState)} is not attached.");
            Assert.IsNotNull(_resolutionOptions, $"{nameof(_resolutionOptions)} is not attached.");

            Create();
        }

        public override string Id => "Generator";
        public override string Title => "Generator";

        public event Action? OnRedrawRequested;
        public event Action? OnExportRequested;

        public void SetRedrawEnabled(bool value)
        {
            _redrawButton.SetEnabled(value);
        }

        public void SetExportEnabled(bool value)
        {
            _exportSection.SetExportEnabled(value);
        }

        public void SetExportPath(string path)
        {
            _exportSection.FileSelectField.value = path;
        }

        /// <summary>
        ///     Undo/Redo やリセット時など、外部から状態が変更された際に条件付き UI の表示・非表示を再同期します。
        /// </summary>
        public void RefreshUI()
        {
            _inputSection?.ConfigureInputModeUI(_editorState.InputMode, _editorState.RenderSceneSyncSceneViewCamera);
            _inputSection?.SyncFlips();
            _blurSection?.ConfigureBlurAlgorithmUI(_editorState.BlurEnable, _editorState.BlurAlgorithm);
            _exportSection?.ConfigureOutputLayoutUI(_editorState.OutputLayout);
            _exportSection?.FileSelectField.SetValueWithoutNotify(_editorState.ExportPath);
            _exportSection?.SyncResolution(_editorState.OutputResolution);
        }

        public void SetWarning(string? text)
        {
            if (text == null)
            {
                _infoBoxCubemapWarning.style.display = DisplayStyle.None;
                _labelCubemapWarning.text = string.Empty;
            }
            else
            {
                _infoBoxCubemapWarning.style.display = DisplayStyle.Flex;
                _labelCubemapWarning.text = $"⚠ Warning:\n{text}";
            }
        }

        protected override void OnDispose()
        {
            TabContent.Clear();
            _resolutionOptions?.Clear();
            OnRedrawRequested = null;
            OnExportRequested = null;
        }

        private void Create()
        {
            var titleLabel = new Label("Generator Settings");
            titleLabel.style.fontSize = 16;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.marginBottom = 10;
            TabContent.Add(titleLabel);

            _inputSection = new GeneratorTabInputSettingsSection(_editorState, () => _requestRenderCubemap.Invoke());
            TabContent.Add(_inputSection);

            _redrawButton = new Button(() => OnRedrawRequested?.Invoke());
            _redrawButton.text = "Redraw";
            _redrawButton.style.width = 80;
            _redrawButton.SetEnabled(false);
            TabContent.Add(_redrawButton);

            TabContent.Add(new Separator());

            _blurSection =
                new GeneratorTabBlurSettingsSection(_editorState, () => _requestRenderBlurredCubemap.Invoke());
            TabContent.Add(_blurSection);

            TabContent.Add(new Separator());

            _exportSection = new GeneratorTabExportSettingsSection(
                _editorState,
                _settings,
                _resolutionOptions,
                _ => _requestRenderCubemap.Invoke(),
                () => OnExportRequested?.Invoke()
            );
            TabContent.Add(_exportSection);

            _infoBoxCubemapWarning = new InfoBox();
            TabContent.Add(_infoBoxCubemapWarning);
            _labelCubemapWarning = new Label();
            _labelCubemapWarning.AddToClassList("info-label");
            _labelCubemapWarning.AddToClassList("info-label--warning");
            _infoBoxCubemapWarning.Add(_labelCubemapWarning);
            _infoBoxCubemapWarning.style.display = DisplayStyle.None;
        }
    }
}
