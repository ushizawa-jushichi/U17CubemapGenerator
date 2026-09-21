using UnityEngine.UIElements;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     UI 項目間に一定の垂直余白を設けるための VisualElement。
    /// </summary>
    public class Spacer : VisualElement
    {
        public Spacer(int height = 20)
        {
            style.height = height;
        }
    }
}
