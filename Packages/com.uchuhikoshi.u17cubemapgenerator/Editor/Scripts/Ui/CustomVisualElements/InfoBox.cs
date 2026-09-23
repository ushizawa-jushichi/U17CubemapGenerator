using UnityEngine.UIElements;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     情報ラベル群をまとめて表示するためのコンテナ VisualElement。
    /// </summary>
    public class InfoBox : VisualElement
    {
        public InfoBox()
        {
            style.maxWidth = 300;
            AddToClassList("info-container");
        }
    }
}
