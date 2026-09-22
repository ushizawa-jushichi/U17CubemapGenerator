using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     入力ソース（CurrentScene, SixSided, Cubemap）ごとのパラメータ設定 UI を管理するセクション。
    /// </summary>
    public sealed class GeneratorTabInputSettingsSection : VisualElement
    {
        private const float FieldMaxWidth = 250f;
        private static readonly string[] FaceLabels = { "+X", "-X", "+Y", "-Y", "+Z", "-Z" };
        private readonly VisualElement _faceFlipContainer;
        private readonly Button[] _flipH = new Button[6];
        private readonly Button[] _flipV = new Button[6];

        private readonly VisualElement _sixSidedX;
        private readonly VisualElement _sixSidedY;
        private readonly VisualElement _sixSidedZ;

        private readonly EditorState _state;
        private readonly HelpBox _urpRequiredHelpBox;

        public GeneratorTabInputSettingsSection(
            EditorState state,
            Action onRequestRedraw
        )
        {
            _state = state;
            pickingMode = PickingMode.Ignore;

            InputModeField = new EnumField("Input Mode", default(InputModeType))
                { bindingPath = EditorState.BindPath(nameof(_state.InputMode)) };
            InputModeField.style.maxWidth = FieldMaxWidth;
            InputModeField.RegisterValueChangedCallback(evt =>
            {
                state.SetAdaptiveSRGBEnable();
                ConfigureInputModeUI((InputModeType)evt.newValue, SyncCameraToggle?.value ?? false);
                onRequestRedraw();
            });
            Add(InputModeField);

            _urpRequiredHelpBox = new HelpBox(
                "Universal Render Pipeline (URP) is not active. Scene camera capture is unavailable in Built-in / HDRP projects. Please switch to SixSided or Cubemap mode, or activate URP.",
                HelpBoxMessageType.Warning);
            _urpRequiredHelpBox.style.maxWidth = FieldMaxWidth;
            _urpRequiredHelpBox.style.marginTop = 5;
            _urpRequiredHelpBox.style.marginBottom = 5;
            _urpRequiredHelpBox.style.display = DisplayStyle.None;
            Add(_urpRequiredHelpBox);

            CameraField = new ObjectField("Render Camera")
            {
                objectType = typeof(Camera),
                allowSceneObjects = true,
                // bindingPath により SerializedObject 経由で値が管理される (Undo/Redo 自動対応)
                bindingPath = EditorState.BindPath(nameof(_state.RenderSceneCamera))
            };
            CameraField.style.maxWidth = FieldMaxWidth;
            CameraField.RegisterValueChangedCallback(evt =>
            {
                // 値の更新は binding に委譲。ここでは再描画のみ要求する
                onRequestRedraw();
            });
            Add(CameraField);

            SyncCameraToggle = new Toggle("Sync SceneView Camera")
                { bindingPath = EditorState.BindPath(nameof(_state.RenderSceneSyncSceneViewCamera)) };
            SyncCameraToggle.style.maxWidth = FieldMaxWidth;
            SyncCameraToggle.RegisterValueChangedCallback(evt =>
            {
                ConfigureInputModeUI((InputModeType)InputModeField.value, evt.newValue);
                onRequestRedraw();
            });
            Add(SyncCameraToggle);

            RotatableToggle = new Toggle("Rotatable")
                { bindingPath = EditorState.BindPath(nameof(_state.RenderSceneRotatable)) };
            RotatableToggle.style.maxWidth = FieldMaxWidth;
            RotatableToggle.RegisterValueChangedCallback(evt => onRequestRedraw());
            Add(RotatableToggle);

            HdrToggle = new Toggle("HDR")
                { bindingPath = EditorState.BindPath(nameof(_state.RenderSceneHDR)) };
            HdrToggle.style.maxWidth = FieldMaxWidth;
            HdrToggle.RegisterValueChangedCallback(evt => onRequestRedraw());
            Add(HdrToggle);

            SrgbToggle = new Toggle("sRGB")
                { bindingPath = EditorState.BindPath(nameof(_state.RenderSceneSRGB)) };
            SrgbToggle.style.maxWidth = FieldMaxWidth;
            SrgbToggle.RegisterValueChangedCallback(evt => onRequestRedraw());
            Add(SrgbToggle);

            _sixSidedX = new VisualElement();
            _sixSidedX.style.flexDirection = FlexDirection.Row;
            _sixSidedX.style.alignItems = Align.Center;
            _sixSidedX.style.flexWrap = Wrap.Wrap;
            _sixSidedY = new VisualElement();
            _sixSidedY.style.flexDirection = FlexDirection.Row;
            _sixSidedY.style.alignItems = Align.Center;
            _sixSidedY.style.flexWrap = Wrap.Wrap;
            _sixSidedZ = new VisualElement();
            _sixSidedZ.style.flexDirection = FlexDirection.Row;
            _sixSidedZ.style.alignItems = Align.Center;
            _sixSidedZ.style.flexWrap = Wrap.Wrap;
            Add(_sixSidedX);
            Add(_sixSidedY);
            Add(_sixSidedZ);

            Action onTextureFieldChanged = () =>
            {
                _state.SetAdaptiveSRGBEnable();
                SrgbToggle.SetValueWithoutNotify(_state.RenderSceneSRGB);
                SyncFlips();
                onRequestRedraw();
            };

            SixSidedXPlus = new LargePreviewObjectField("Texture X Plus", typeof(Texture2D))
                { bindingPath = EditorState.BindPath(nameof(_state.SixSidedXPlus)) };
            SixSidedXPlus.RegisterValueChangedCallback(evt =>
            {
                _state.ResetFaceFlip(0);
                onTextureFieldChanged();
            });
            _sixSidedX.Add(SixSidedXPlus);

            SixSidedXMinus = new LargePreviewObjectField("Texture X Minus", typeof(Texture2D))
                { bindingPath = EditorState.BindPath(nameof(_state.SixSidedXMinus)) };
            SixSidedXMinus.RegisterValueChangedCallback(evt =>
            {
                _state.ResetFaceFlip(1);
                onTextureFieldChanged();
            });
            _sixSidedX.Add(SixSidedXMinus);

            SixSidedYPlus = new LargePreviewObjectField("Texture Y Plus", typeof(Texture2D))
                { bindingPath = EditorState.BindPath(nameof(_state.SixSidedYPlus)) };
            SixSidedYPlus.RegisterValueChangedCallback(evt =>
            {
                _state.ResetFaceFlip(2);
                onTextureFieldChanged();
            });
            _sixSidedY.Add(SixSidedYPlus);

            SixSidedYMinus = new LargePreviewObjectField("Texture Y Minus", typeof(Texture2D))
                { bindingPath = EditorState.BindPath(nameof(_state.SixSidedYMinus)) };
            SixSidedYMinus.RegisterValueChangedCallback(evt =>
            {
                _state.ResetFaceFlip(3);
                onTextureFieldChanged();
            });
            _sixSidedY.Add(SixSidedYMinus);

            SixSidedZPlus = new LargePreviewObjectField("Texture Z Plus", typeof(Texture2D))
                { bindingPath = EditorState.BindPath(nameof(_state.SixSidedZPlus)) };
            SixSidedZPlus.RegisterValueChangedCallback(evt =>
            {
                _state.ResetFaceFlip(4);
                onTextureFieldChanged();
            });
            _sixSidedZ.Add(SixSidedZPlus);

            SixSidedZMinus = new LargePreviewObjectField("Texture Z Minus", typeof(Texture2D))
                { bindingPath = EditorState.BindPath(nameof(_state.SixSidedZMinus)) };
            SixSidedZMinus.RegisterValueChangedCallback(evt =>
            {
                _state.ResetFaceFlip(5);
                onTextureFieldChanged();
            });
            _sixSidedZ.Add(SixSidedZMinus);

            CubemapField = new LargePreviewObjectField("Cubemap", typeof(Cubemap))
                { bindingPath = EditorState.BindPath(nameof(_state.Cubemap)) };
            CubemapField.RegisterValueChangedCallback(evt =>
            {
                _state.ResetAllFlips();
                onTextureFieldChanged();
            });
            Add(CubemapField);

            _faceFlipContainer = new VisualElement();
            _faceFlipContainer.style.maxWidth = FieldMaxWidth;
            _faceFlipContainer.style.marginTop = 6;
            _faceFlipContainer.style.marginBottom = 6;
            _faceFlipContainer.style.paddingTop = 6;
            _faceFlipContainer.style.paddingBottom = 6;
            _faceFlipContainer.style.paddingLeft = 8;
            _faceFlipContainer.style.paddingRight = 8;
            _faceFlipContainer.style.backgroundColor = new Color(0f, 0f, 0f, 0.15f);
            _faceFlipContainer.style.borderTopLeftRadius = 4;
            _faceFlipContainer.style.borderTopRightRadius = 4;
            _faceFlipContainer.style.borderBottomLeftRadius = 4;
            _faceFlipContainer.style.borderBottomRightRadius = 4;

            var flipHeaderRow = new VisualElement();
            flipHeaderRow.style.flexDirection = FlexDirection.Row;
            flipHeaderRow.style.justifyContent = Justify.SpaceBetween;
            flipHeaderRow.style.alignItems = Align.Center;
            flipHeaderRow.style.marginBottom = 6;

            var flipTitle = new Label("Face Flip (V: Vertical, H: Horizontal)");
            flipTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            flipTitle.style.fontSize = 11;
            flipHeaderRow.Add(flipTitle);

            var resetAllBtn = new Button(() =>
            {
                _state.ResetAllFlips();
                SyncFlips();
                onRequestRedraw();
            })
            {
                text = "Reset",
                tooltip = "Reset all face flips"
            };
            resetAllBtn.style.fontSize = 10;
            resetAllBtn.style.height = 18;
            resetAllBtn.style.paddingLeft = 6;
            resetAllBtn.style.paddingRight = 6;
            resetAllBtn.style.paddingTop = 0;
            resetAllBtn.style.paddingBottom = 0;
            flipHeaderRow.Add(resetAllBtn);

            _faceFlipContainer.Add(flipHeaderRow);

            for (var pair = 0; pair < 3; pair++)
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.justifyContent = Justify.FlexStart;
                row.style.alignItems = Align.Center;
                row.style.marginTop = 2;
                row.style.marginBottom = 2;

                var leftIdx = pair * 2;
                var rightIdx = pair * 2 + 1;

                var leftItem = CreateFaceFlipItem(leftIdx, onRequestRedraw);
                leftItem.style.marginRight = 14;

                var rightItem = CreateFaceFlipItem(rightIdx, onRequestRedraw);

                row.Add(leftItem);
                row.Add(rightItem);

                _faceFlipContainer.Add(row);
            }

            Add(_faceFlipContainer);

            ConfigureInputModeUI(state.InputMode, state.RenderSceneSyncSceneViewCamera);
        }

        public EnumField InputModeField { get; }
        public ObjectField CameraField { get; }
        public Toggle SyncCameraToggle { get; }
        public Toggle RotatableToggle { get; }
        public Toggle HdrToggle { get; }
        public Toggle SrgbToggle { get; }
        public LargePreviewObjectField SixSidedXPlus { get; }
        public LargePreviewObjectField SixSidedXMinus { get; }
        public LargePreviewObjectField SixSidedYPlus { get; }
        public LargePreviewObjectField SixSidedYMinus { get; }
        public LargePreviewObjectField SixSidedZPlus { get; }
        public LargePreviewObjectField SixSidedZMinus { get; }

        public LargePreviewObjectField CubemapField { get; }

        private VisualElement CreateFaceFlipItem(int faceIndex, Action onRequestRedraw)
        {
            var item = new VisualElement();
            item.style.flexDirection = FlexDirection.Row;
            item.style.alignItems = Align.Center;

            var label = new Label(FaceLabels[faceIndex]);
            label.style.width = 26;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.fontSize = 11;
            item.Add(label);

            var btnV = new Button(() =>
            {
                var next = !_state.GetFaceFlipV(faceIndex);
                _state.SetFaceFlipV(faceIndex, next);
                UpdateButtonStyle(_flipV[faceIndex], next);
                onRequestRedraw();
            })
            {
                text = "V",
                tooltip = $"{FaceLabels[faceIndex]} Vertical Flip (上下反転)"
            };
            btnV.AddToClassList("face-flip-btn");
            _flipV[faceIndex] = btnV;
            item.Add(btnV);

            var btnH = new Button(() =>
            {
                var next = !_state.GetFaceFlipH(faceIndex);
                _state.SetFaceFlipH(faceIndex, next);
                UpdateButtonStyle(_flipH[faceIndex], next);
                onRequestRedraw();
            })
            {
                text = "H",
                tooltip = $"{FaceLabels[faceIndex]} Horizontal Flip (左右反転)"
            };
            btnH.AddToClassList("face-flip-btn");
            _flipH[faceIndex] = btnH;
            item.Add(btnH);

            return item;
        }

        private static void UpdateButtonStyle(Button? btn, bool isActive)
        {
            if (btn == null)
            {
                return;
            }

            if (isActive)
            {
                btn.AddToClassList("face-flip-btn--active");
            }
            else
            {
                btn.RemoveFromClassList("face-flip-btn--active");
            }
        }

        public void SyncFlips()
        {
            for (var i = 0; i < 6; i++)
            {
                if (_flipV[i] != null)
                {
                    UpdateButtonStyle(_flipV[i], _state.GetFaceFlipV(i));
                }

                if (_flipH[i] != null)
                {
                    UpdateButtonStyle(_flipH[i], _state.GetFaceFlipH(i));
                }
            }
        }

        public void ConfigureInputModeUI(InputModeType inputMode, bool syncSceneView)
        {
            InputModeField.SetValueWithoutNotify(inputMode);

            var isUrpActive = CubemapRenderUtility.IsUniversalRenderPipelineActive();
            var displayCurrentScene = inputMode == InputModeType.CurrentScene ? DisplayStyle.Flex : DisplayStyle.None;

            if (inputMode == InputModeType.CurrentScene && !isUrpActive)
            {
                _urpRequiredHelpBox.style.display = DisplayStyle.Flex;
                CameraField.SetEnabled(false);
                SyncCameraToggle.SetEnabled(false);
                RotatableToggle.SetEnabled(false);
                HdrToggle.SetEnabled(false);
            }
            else
            {
                _urpRequiredHelpBox.style.display = DisplayStyle.None;
                CameraField.SetEnabled(!syncSceneView);
                SyncCameraToggle.SetEnabled(true);
                RotatableToggle.SetEnabled(true);
                HdrToggle.SetEnabled(true);
            }

            CameraField.style.display = displayCurrentScene;
            SyncCameraToggle.style.display = displayCurrentScene;
            RotatableToggle.style.display = displayCurrentScene;
            HdrToggle.style.display = displayCurrentScene;

            var displaySixSided = inputMode == InputModeType.SixSided ? DisplayStyle.Flex : DisplayStyle.None;
            _sixSidedX.style.display = displaySixSided;
            _sixSidedY.style.display = displaySixSided;
            _sixSidedZ.style.display = displaySixSided;

            var displayCubemap = inputMode == InputModeType.Cubemap ? DisplayStyle.Flex : DisplayStyle.None;
            CubemapField.style.display = displayCubemap;

            var displayFlip = inputMode == InputModeType.SixSided || inputMode == InputModeType.Cubemap
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            _faceFlipContainer.style.display = displayFlip;
            SyncFlips();

            SrgbToggle.SetValueWithoutNotify(_state.RenderSceneSRGB);
            SrgbToggle.SetEnabled(inputMode != InputModeType.CurrentScene);
        }
    }
}
