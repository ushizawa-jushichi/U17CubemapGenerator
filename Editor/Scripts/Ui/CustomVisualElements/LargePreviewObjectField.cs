using System;
using UnityEditor.UIElements;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     テクスチャプレビュー用のスタイルを適用した大きめの ObjectField。
    /// </summary>
    public class LargePreviewObjectField : ObjectField
    {
        public LargePreviewObjectField(string label, Type type)
            : base(label)
        {
            objectType = type;
            style.maxWidth = 400;
            style.minWidth = 220;
            AddToClassList("large-preview-field");
        }
    }
}
