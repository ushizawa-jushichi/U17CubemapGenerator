using UnityEngine;
using UnityEngine.UIElements;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     セパレータ等の定型 UI エレメントを生成するユーティリティクラス。
    /// </summary>
    public static class UIElementFactory
    {
        public static VisualElement CreateSeparator(int marginTop = 10, int marginBottom = 10)
        {
            var separator = new VisualElement();
            separator.style.height = 1;
            separator.style.backgroundColor = new StyleColor(Color.gray);
            separator.style.marginTop = marginTop;
            separator.style.marginBottom = marginBottom;
            return separator;
        }
    }
}
