using UnityEngine;
using UnityEngine.UIElements;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     CubemapGeneratorWindow の UI 要素を構築するビルダークラス。
    /// </summary>
    public static class WindowUIBuilder
    {
        public static VisualElement CreateBackgroundContainer()
        {
            var bg = new VisualElement
            {
                style =
                {
                    position = Position.Absolute,
                    left = 0,
                    right = 0,
                    top = 0,
                    bottom = 0,
                    backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1.0f)
                }
            };
#if UNITY_6000_4_OR_NEWER
            bg.style.backgroundPositionX =
                BackgroundPropertyHelper.ConvertScaleModeToBackgroundPosition(ScaleMode.ScaleAndCrop);
            bg.style.backgroundPositionY =
                BackgroundPropertyHelper.ConvertScaleModeToBackgroundPosition(ScaleMode.ScaleAndCrop);
            bg.style.backgroundRepeat =
                BackgroundPropertyHelper.ConvertScaleModeToBackgroundRepeat(ScaleMode.ScaleAndCrop);
            bg.style.backgroundSize =
                BackgroundPropertyHelper.ConvertScaleModeToBackgroundSize(ScaleMode.ScaleAndCrop);
#else
            bg.style.backgroundPositionX =
                new StyleBackgroundPosition(new BackgroundPosition(BackgroundPositionKeyword.Center));
            bg.style.backgroundPositionY =
                new StyleBackgroundPosition(new BackgroundPosition(BackgroundPositionKeyword.Center));
            bg.style.backgroundRepeat =
                new StyleBackgroundRepeat(new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat));
            bg.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
#endif
            return bg;
        }

        public static VisualElement CreateProcessingIndicator()
        {
            var indicator = new VisualElement
            {
                name = "ProcessingIndicator",
                pickingMode = PickingMode.Ignore
            };
            indicator.AddToClassList("processing-badge");

            var dot = new VisualElement { pickingMode = PickingMode.Ignore };
            dot.AddToClassList("processing-badge__dot");
            indicator.Add(dot);

            var label = new Label("Processing...") { pickingMode = PickingMode.Ignore };
            label.AddToClassList("processing-badge__label");
            indicator.Add(label);

            return indicator;
        }
    }
}
