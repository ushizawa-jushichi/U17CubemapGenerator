using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     出力解像度、保存先パス、出力レイアウト、およびエクスポート実行ボタンを配置した UI セクション。
    /// </summary>
    public sealed class GeneratorTabExportSettingsSection : VisualElement
    {
        private const float FieldMaxWidth = 250f;
        private const float ExportPathFieldMaxWidth = 500f;
        private const float ExportButtonWidth = 80f;
        private const string DefaultExportAssetPath = "Assets/GeneratedCubemap.cubemap";
        private readonly Settings _settings;
        private readonly HelpBox _sizeExceededHelpBox;
        private readonly EditorState _state;
        private bool _isPipelineReady;
        private bool _isResolutionValid = true;

        public GeneratorTabExportSettingsSection(
            EditorState state,
            Settings settings,
            List<string> resolutionOptions,
            Action<int> onOutputResolutionChanged,
            Action onExportRequested
        )
        {
            pickingMode = PickingMode.Ignore;

            _state = state;
            _settings = settings;

            // 初期値の整合性を確保（空なら設定アセットの既定値を使用）
            if (string.IsNullOrEmpty(_state.ExportPath))
            {
                var defaultPath = !string.IsNullOrEmpty(_settings.DefaultExportPath)
                    ? _settings.DefaultExportPath
                    : DefaultExportAssetPath;
                _state.SetExportPath(defaultPath);
            }

            var defaultResolutionStr = _state.OutputResolution.ToString();
            var defaultResolutionIndex = resolutionOptions.IndexOf(defaultResolutionStr);
            OutputResolutionDropdownField =
                new DropdownField("Output Resolution", resolutionOptions,
                    defaultResolutionIndex >= 0 ? defaultResolutionIndex : 0);
            OutputResolutionDropdownField.style.maxWidth = FieldMaxWidth;
            OutputResolutionDropdownField.RegisterValueChangedCallback(evt =>
            {
                if (int.TryParse(evt.newValue, out var value))
                {
                    if (_state.OutputResolution != value)
                    {
                        _state.SetOutputResolution(value);
                        onOutputResolutionChanged(value);
                        ValidateOutputSize();
                    }
                }
            });
            Add(OutputResolutionDropdownField);

            FileSelectField = new FileSelectField("Export File", _state.ExportPath, BrowseExportPath);
            FileSelectField.RegisterValueChangedCallback(value =>
            {
                if (!_state.ExportPath.Equals(value, StringComparison.OrdinalIgnoreCase))
                {
                    _state.SetExportPath(value);
                }
            });
            FileSelectField.style.maxWidth = ExportPathFieldMaxWidth;
            Add(FileSelectField);

            OutputLayoutEnumField = new EnumField("Output Layout", default(OutputLayoutType))
                { bindingPath = EditorState.BindPath(nameof(_state.OutputLayout)) };
            OutputLayoutEnumField.style.maxWidth = FieldMaxWidth;
            OutputLayoutEnumField.RegisterValueChangedCallback(evt =>
            {
                var newLayout = (OutputLayoutType)evt.newValue;
                ConfigureOutputLayoutUI(newLayout);
                UpdateExtensionForLayout(newLayout);
                ValidateOutputSize();
            });
            Add(OutputLayoutEnumField);

            EquirectangularYFloatField = new FloatField("Equirectangular Y")
                { bindingPath = EditorState.BindPath(nameof(_state.EquirectangularY)) };
            EquirectangularYFloatField.style.maxWidth = FieldMaxWidth;
            EquirectangularYFloatField.RegisterValueChangedCallback(evt =>
            {
                var clamped = float.IsNaN(evt.newValue) ? 0f : Mathf.Repeat(evt.newValue, RotationField.MaxAngle);
                if (!Mathf.Approximately(clamped, evt.newValue))
                {
                    EquirectangularYFloatField.SetValueWithoutNotify(clamped);
                }
            });
            Add(EquirectangularYFloatField);

            EquirectangularImportAsCubemapToggle = new Toggle("Import as Cubemap")
                { bindingPath = EditorState.BindPath(nameof(_state.EquirectangularImportAsCubemap)) };
            EquirectangularImportAsCubemapToggle.style.maxWidth = FieldMaxWidth;
            Add(EquirectangularImportAsCubemapToggle);

            CrossOrStraightImportAsCubemapToggle = new Toggle("Import as Cubemap")
                { bindingPath = EditorState.BindPath(nameof(_state.CrossOrStraightImportAsCubemap)) };
            CrossOrStraightImportAsCubemapToggle.style.maxWidth = FieldMaxWidth;
            Add(CrossOrStraightImportAsCubemapToggle);

            MatcapFillOutsideToggle = new Toggle("Matcap Fill Outside")
                { bindingPath = EditorState.BindPath(nameof(_state.MatcapFillOutside)) };
            MatcapFillOutsideToggle.style.maxWidth = FieldMaxWidth;
            Add(MatcapFillOutsideToggle);

            OctahedralYUpToggle = new Toggle("Y-Up Pole Alignment")
                { bindingPath = EditorState.BindPath(nameof(_state.OctahedralYUp)) };
            OctahedralYUpToggle.style.maxWidth = FieldMaxWidth;
            Add(OctahedralYUpToggle);

            _sizeExceededHelpBox = new HelpBox(string.Empty, HelpBoxMessageType.Error);
            _sizeExceededHelpBox.style.maxWidth = FieldMaxWidth;
            _sizeExceededHelpBox.style.marginTop = 5;
            _sizeExceededHelpBox.style.marginBottom = 5;
            _sizeExceededHelpBox.style.display = DisplayStyle.None;
            Add(_sizeExceededHelpBox);

            var exportRow = new VisualElement();
            exportRow.style.flexDirection = FlexDirection.Row;
            exportRow.style.alignItems = Align.Center;

            ExportButton = new Button(onExportRequested);
            ExportButton.text = "Export";
            ExportButton.style.width = ExportButtonWidth;
            ExportButton.SetEnabled(false);
            exportRow.Add(ExportButton);
            Add(exportRow);

            ConfigureOutputLayoutUI(state.OutputLayout);
            ValidateOutputSize();
        }

        public DropdownField OutputResolutionDropdownField { get; }
        public FileSelectField FileSelectField { get; }
        public EnumField OutputLayoutEnumField { get; }
        public FloatField EquirectangularYFloatField { get; }
        public Toggle EquirectangularImportAsCubemapToggle { get; }
        public Toggle CrossOrStraightImportAsCubemapToggle { get; }
        public Toggle MatcapFillOutsideToggle { get; }
        public Toggle OctahedralYUpToggle { get; }
        public Button ExportButton { get; }

        public void SetExportEnabled(bool isPipelineReady)
        {
            _isPipelineReady = isPipelineReady;
            ExportButton.SetEnabled(_isPipelineReady && _isResolutionValid);
        }

        private void ValidateOutputSize()
        {
            _isResolutionValid = CubemapPathUtility.IsOutputSizeValid(
                _state.OutputLayout, _state.OutputResolution, out var errorMessage);

            if (!_isResolutionValid)
            {
                _sizeExceededHelpBox.text = errorMessage!;
                _sizeExceededHelpBox.style.display = DisplayStyle.Flex;
            }
            else
            {
                _sizeExceededHelpBox.style.display = DisplayStyle.None;
            }

            ExportButton.SetEnabled(_isPipelineReady && _isResolutionValid);
        }

        public void SyncResolution(int resolution)
        {
            var resStr = resolution.ToString();
            if (OutputResolutionDropdownField.choices != null && OutputResolutionDropdownField.choices.Contains(resStr))
            {
                OutputResolutionDropdownField.SetValueWithoutNotify(resStr);
                ValidateOutputSize();
            }
        }

        public void ConfigureOutputLayoutUI(OutputLayoutType value)
        {
            OutputLayoutEnumField.SetValueWithoutNotify(value);
            EquirectangularImportAsCubemapToggle.SetValueWithoutNotify(_state.EquirectangularImportAsCubemap);
            CrossOrStraightImportAsCubemapToggle.SetValueWithoutNotify(_state.CrossOrStraightImportAsCubemap);
            MatcapFillOutsideToggle.SetValueWithoutNotify(_state.MatcapFillOutside);
            OctahedralYUpToggle.SetValueWithoutNotify(_state.OctahedralYUp);

            var displayRotationY = value is OutputLayoutType.Equirectangular or OutputLayoutType.Octahedral
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            EquirectangularYFloatField.style.display = displayRotationY;

            var displayEquirectangular =
                value == OutputLayoutType.Equirectangular ? DisplayStyle.Flex : DisplayStyle.None;
            EquirectangularImportAsCubemapToggle.style.display = displayEquirectangular;

            var displayCrossOrStraight = value is OutputLayoutType.CrossHorizontal
                or OutputLayoutType.CrossVertical
                or OutputLayoutType.StraightHorizontal
                or OutputLayoutType.StraightVertical
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            CrossOrStraightImportAsCubemapToggle.style.display = displayCrossOrStraight;

            var displayMatcap = value == OutputLayoutType.Matcap ? DisplayStyle.Flex : DisplayStyle.None;
            MatcapFillOutsideToggle.style.display = displayMatcap;

            var displayOctahedral = value == OutputLayoutType.Octahedral ? DisplayStyle.Flex : DisplayStyle.None;
            OctahedralYUpToggle.style.display = displayOctahedral;
        }

        private string? BrowseExportPath(string currentPath)
        {
            var isHDR = _state.IsCurrentInputHDR();
            var ext = GetDefaultExtensionForPath(currentPath, _state.OutputLayout, isHDR);
            var defaultDir = GetInitialExportDirectory(currentPath);
            var defaultName = "GeneratedCubemap";

            if (!string.IsNullOrEmpty(currentPath))
            {
                var extName = Path.GetFileNameWithoutExtension(currentPath);
                if (!string.IsNullOrEmpty(extName))
                {
                    defaultName = extName;
                }
            }

            var selected = EditorUtility.SaveFilePanelInProject(
                "Save Export File",
                defaultName,
                ext,
                "Please enter a file name to save the exported asset.",
                defaultDir);

            return string.IsNullOrEmpty(selected) ? null : selected;
        }

        private string GetInitialExportDirectory(string currentPath)
        {
            if (!string.IsNullOrEmpty(currentPath))
            {
                var dir = Path.GetDirectoryName(currentPath)?.Replace('\\', '/');
                if (!string.IsNullOrEmpty(dir) && AssetDatabase.IsValidFolder(dir))
                {
                    return dir!;
                }
            }

            var inputAsset = GetPrimaryInputAsset();
            if (inputAsset != null)
            {
                var inputPath = AssetDatabase.GetAssetPath(inputAsset);
                if (!string.IsNullOrEmpty(inputPath))
                {
                    var inputDir = Path.GetDirectoryName(inputPath)?.Replace('\\', '/');
                    if (!string.IsNullOrEmpty(inputDir) && AssetDatabase.IsValidFolder(inputDir))
                    {
                        return inputDir!;
                    }
                }
            }

            if (Selection.activeObject != null)
            {
                var selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    if (AssetDatabase.IsValidFolder(selectedPath))
                    {
                        return selectedPath;
                    }

                    var selectedDir = Path.GetDirectoryName(selectedPath)?.Replace('\\', '/');
                    if (!string.IsNullOrEmpty(selectedDir) && AssetDatabase.IsValidFolder(selectedDir))
                    {
                        return selectedDir!;
                    }
                }
            }

            return "Assets";
        }

        private Object GetPrimaryInputAsset()
        {
            if (_state.InputMode == InputModeType.Cubemap)
            {
                return _state.Cubemap;
            }

            if (_state.InputMode == InputModeType.SixSided)
            {
                return _state.SixSidedXPlus != null ? _state.SixSidedXPlus
                    : _state.SixSidedXMinus != null ? _state.SixSidedXMinus
                    : _state.SixSidedYPlus != null ? _state.SixSidedYPlus
                    : _state.SixSidedYMinus != null ? _state.SixSidedYMinus
                    : _state.SixSidedZPlus != null ? _state.SixSidedZPlus
                    : _state.SixSidedZMinus;
            }

            return null!;
        }

        private void UpdateExtensionForLayout(OutputLayoutType outputLayout)
        {
            if (string.IsNullOrEmpty(_state.ExportPath))
            {
                return;
            }

            var currentExt = Path.GetExtension(_state.ExportPath)?.ToLowerInvariant();

            if (outputLayout == OutputLayoutType.LegacyCubemap)
            {
                if (!string.Equals(currentExt, ".cubemap", StringComparison.OrdinalIgnoreCase))
                {
                    var newPath = Path.ChangeExtension(_state.ExportPath, ".cubemap");
                    Undo.RecordObject(_state, "Change Export Extension");
                    _state.SetExportPath(newPath);
                    FileSelectField.SetValueWithoutNotify(newPath);
                }

                return;
            }

            if (_state.IsCurrentInputHDR())
            {
                if (!string.Equals(currentExt, ".exr", StringComparison.OrdinalIgnoreCase))
                {
                    var newPath = Path.ChangeExtension(_state.ExportPath, ".exr");
                    Undo.RecordObject(_state, "Change Export Extension");
                    _state.SetExportPath(newPath);
                    FileSelectField.SetValueWithoutNotify(newPath);
                }

                return;
            }

            // SDR（非 HDR）かつ 2D テクスチャ系レイアウトの場合:
            // 既存の拡張子が .png, .tga, .jpg, .jpeg のいずれかであれば維持し、それ以外（.cubemap 等）なら .png に補正
            var isSupported2DExt = currentExt is ".png" or ".tga" or ".jpg" or ".jpeg";
            if (!isSupported2DExt)
            {
                var newPath = Path.ChangeExtension(_state.ExportPath, ".png");
                Undo.RecordObject(_state, "Change Export Extension");
                _state.SetExportPath(newPath);
                FileSelectField.SetValueWithoutNotify(newPath);
            }
        }

        private static string GetDefaultExtensionForPath(string? currentPath, OutputLayoutType outputLayout, bool isHDR)
        {
            if (outputLayout == OutputLayoutType.LegacyCubemap)
            {
                return "cubemap";
            }

            if (isHDR)
            {
                return "exr";
            }

            if (!string.IsNullOrEmpty(currentPath))
            {
                var curExt = Path.GetExtension(currentPath)?.TrimStart('.').ToLowerInvariant();
                if (curExt is "tga" or "jpg" or "jpeg")
                {
                    return curExt;
                }
            }

            return "png";
        }
    }
}
