using UnityEngine;
using UnityEngine.UIElements;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     UI 項目間を区切る水平線を描画する VisualElement。
    /// </summary>
    public class Separator : VisualElement
    {
        public Separator(int marginTop = 10, int marginBottom = 10)
        {
            style.height = 1;
            style.backgroundColor = new StyleColor(Color.gray);
            style.marginTop = marginTop;
            style.marginBottom = marginBottom;
        }
    }
}
