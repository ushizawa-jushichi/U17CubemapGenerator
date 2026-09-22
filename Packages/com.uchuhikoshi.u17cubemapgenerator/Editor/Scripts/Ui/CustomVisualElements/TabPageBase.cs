using UnityEngine.UIElements;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     ツールバーと連動してコンテンツの表示・非表示を切り替えるタブページの基底抽象クラス。
    /// </summary>
    public abstract class TabPageBase : IToolbarTabPage
    {
        public bool IsDisposed { get; private set; }
        public abstract string Id { get; }
        public abstract string Title { get; }
        public VisualElement TabContent { get; } = new() { pickingMode = PickingMode.Ignore };

        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            IsDisposed = true;
            OnDispose();
        }

        public virtual void Show()
        {
            TabContent.style.display = DisplayStyle.Flex;
        }

        public virtual void OnHide()
        {
            TabContent.style.display = DisplayStyle.None;
        }

        protected abstract void OnDispose();
    }
}
