using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     ブラーの有効化、アルゴリズム選択、Roughness やサンプル数設定を行う UI セクション。
    /// </summary>
    public sealed class GeneratorTabBlurSettingsSection : VisualElement
    {
        public GeneratorTabBlurSettingsSection(
            EditorState state,
            Action onRequestBlurRedraw
        )
        {
            pickingMode = PickingMode.Ignore;

            BlurEnableToggle = new Toggle("Blur Enable")
                { bindingPath = EditorState.BindPath(nameof(state.BlurEnable)) };
            BlurEnableToggle.style.maxWidth = 250;
            BlurEnableToggle.RegisterValueChangedCallback(evt =>
            {
                ConfigureBlurAlgorithmUI(evt.newValue, (CubemapBlurAlgorithm)BlurAlgorithmEnumField!.value);
                onRequestBlurRedraw();
            });
            Add(BlurEnableToggle);

            BlurAlgorithmEnumField = new EnumField("Blur Algorithm", default(CubemapBlurAlgorithm))
                { bindingPath = EditorState.BindPath(nameof(state.BlurAlgorithm)) };
            BlurAlgorithmEnumField.style.maxWidth = 250;
            BlurAlgorithmEnumField.RegisterValueChangedCallback(evt =>
            {
                ConfigureBlurAlgorithmUI(BlurEnableToggle.value, (CubemapBlurAlgorithm)evt.newValue);
                onRequestBlurRedraw();
            });
            Add(BlurAlgorithmEnumField);

            BlurRoughness_GGX_SpecularIBLSlider = new Slider("Roughness")
            {
                lowValue = 0f, highValue = 1f, showInputField = true,
                bindingPath = EditorState.BindPath(nameof(state.BlurRoughness_GGX_SpecularIBL))
            };
            BlurRoughness_GGX_SpecularIBLSlider.RegisterValueChangedCallback(evt =>
            {
                var clamped = float.IsNaN(evt.newValue) ? 0.15f : Mathf.Clamp01(evt.newValue);
                if (!Mathf.Approximately(clamped, evt.newValue))
                {
                    BlurRoughness_GGX_SpecularIBLSlider.SetValueWithoutNotify(clamped);
                }

                onRequestBlurRedraw();
            });
            Add(BlurRoughness_GGX_SpecularIBLSlider);

            BlurRoughness_Gaussian_BokehSlider = new Slider("Blur Radius(Strength)")
            {
                lowValue = 0f, highValue = 1f, showInputField = true,
                bindingPath = EditorState.BindPath(nameof(state.BlurRoughness_Gaussian_Bokeh))
            };
            BlurRoughness_Gaussian_BokehSlider.RegisterValueChangedCallback(evt =>
            {
                var clamped = float.IsNaN(evt.newValue) ? 0.15f : Mathf.Clamp01(evt.newValue);
                if (!Mathf.Approximately(clamped, evt.newValue))
                {
                    BlurRoughness_Gaussian_BokehSlider.SetValueWithoutNotify(clamped);
                }

                onRequestBlurRedraw();
            });
            Add(BlurRoughness_Gaussian_BokehSlider);

            BlurNumSamples_GGX_SpecularIBLSlider = new SliderInt("Samples")
            {
                lowValue = 16, highValue = 4096, showInputField = true,
                bindingPath = EditorState.BindPath(nameof(state.BlurNumSamples_GGX_SpecularIBL))
            };
            BlurNumSamples_GGX_SpecularIBLSlider.RegisterValueChangedCallback(evt =>
            {
                var clamped = Mathf.Clamp(evt.newValue, 16, 4096);
                if (clamped != evt.newValue)
                {
                    BlurNumSamples_GGX_SpecularIBLSlider.SetValueWithoutNotify(clamped);
                }

                onRequestBlurRedraw();
                UpdateHighSamplesWarning();
            });
            Add(BlurNumSamples_GGX_SpecularIBLSlider);

            BlurNumSamples_Gaussian_BokehSlider = new SliderInt("Samples")
            {
                lowValue = 16, highValue = 4096, showInputField = true,
                bindingPath = EditorState.BindPath(nameof(state.BlurNumSamples_Gaussian_Bokeh))
            };
            BlurNumSamples_Gaussian_BokehSlider.RegisterValueChangedCallback(evt =>
            {
                var clamped = Mathf.Clamp(evt.newValue, 16, 4096);
                if (clamped != evt.newValue)
                {
                    BlurNumSamples_Gaussian_BokehSlider.SetValueWithoutNotify(clamped);
                }

                onRequestBlurRedraw();
                UpdateHighSamplesWarning();
            });
            Add(BlurNumSamples_Gaussian_BokehSlider);

            HighSamplesHelpBox = new HelpBox(
                "High sample count (>= 1024) increases blur render time. Heavy workloads are processed progressively in chunks to prevent device errors.",
                HelpBoxMessageType.Info);
            HighSamplesHelpBox.style.display = DisplayStyle.None;
            Add(HighSamplesHelpBox);

            ConfigureBlurAlgorithmUI(state.BlurEnable, state.BlurAlgorithm);
        }

        public Toggle BlurEnableToggle { get; }
        public EnumField BlurAlgorithmEnumField { get; }
        public Slider BlurRoughness_GGX_SpecularIBLSlider { get; }
        public Slider BlurRoughness_Gaussian_BokehSlider { get; }
        public SliderInt BlurNumSamples_GGX_SpecularIBLSlider { get; }
        public SliderInt BlurNumSamples_Gaussian_BokehSlider { get; }
        public HelpBox HighSamplesHelpBox { get; }

        public void ConfigureBlurAlgorithmUI(bool blurEnable, CubemapBlurAlgorithm blurAlgorithm)
        {
            BlurEnableToggle.SetValueWithoutNotify(blurEnable);
            BlurAlgorithmEnumField.SetValueWithoutNotify(blurAlgorithm);

            var displayBlur_GGX_SpecularIBL = blurAlgorithm == CubemapBlurAlgorithm.GGX_SpecularIBL
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            var displayBlur_Gaussian_Bokeh = blurAlgorithm == CubemapBlurAlgorithm.Gaussian_Bokeh
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            BlurRoughness_GGX_SpecularIBLSlider.style.display = displayBlur_GGX_SpecularIBL;
            BlurNumSamples_GGX_SpecularIBLSlider.style.display = displayBlur_GGX_SpecularIBL;
            BlurRoughness_Gaussian_BokehSlider.style.display = displayBlur_Gaussian_Bokeh;
            BlurNumSamples_Gaussian_BokehSlider.style.display = displayBlur_Gaussian_Bokeh;

            BlurAlgorithmEnumField.SetEnabled(blurEnable);
            BlurRoughness_GGX_SpecularIBLSlider.SetEnabled(blurEnable);
            BlurRoughness_Gaussian_BokehSlider.SetEnabled(blurEnable);
            BlurNumSamples_GGX_SpecularIBLSlider.SetEnabled(blurEnable);
            BlurNumSamples_Gaussian_BokehSlider.SetEnabled(blurEnable);

            UpdateHighSamplesWarning();
        }

        private void UpdateHighSamplesWarning()
        {
            if (!BlurEnableToggle.value)
            {
                HighSamplesHelpBox.style.display = DisplayStyle.None;
                return;
            }

            var currentSamples =
                (CubemapBlurAlgorithm)BlurAlgorithmEnumField.value == CubemapBlurAlgorithm.GGX_SpecularIBL
                    ? BlurNumSamples_GGX_SpecularIBLSlider.value
                    : BlurNumSamples_Gaussian_BokehSlider.value;

            HighSamplesHelpBox.style.display = currentSamples >= 1024 ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
