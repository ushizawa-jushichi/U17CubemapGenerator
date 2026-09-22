using UnityEngine.UIElements;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     ラベル名と値テキストを横並びで表示する情報表示用 VisualElement。
    /// </summary>
    public class InfoItem : VisualElement
    {
        private readonly Label _text;

        public InfoItem(string label, string? text = null)
        {
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;

            var label1 = new Label(label);
            label1.AddToClassList("info-label");

            _text = new Label(text ?? string.Empty);
            _text.style.flexGrow = 1;
            _text.AddToClassList("info-label");

            Add(label1);
            Add(_text);
        }

        public void SetValue(string value)
        {
            _text.text = value;
        }
    }
}
