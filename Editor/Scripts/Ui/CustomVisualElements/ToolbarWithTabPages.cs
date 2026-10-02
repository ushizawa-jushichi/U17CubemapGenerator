using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     タブ切り替えトグルボタンを備え、各タブページの表示・切り替えを制御するツールバー VisualElement。
    /// </summary>
    public class ToolbarWithTabPages : Toolbar, IDisposable
    {
        private readonly List<IToolbarTabPage> _tabs = new();

        private readonly List<ToolbarToggle> _toggles = new();

        public IReadOnlyList<IToolbarTabPage> Tabs => _tabs;
        public IToolbarTabPage? ActiveTabPage { get; private set; }

        public void Dispose()
        {
            foreach (var tab in _tabs)
            {
                tab.Dispose();
            }

            _tabs.Clear();
        }

        public ToolbarWithTabPages AddTabPage(IToolbarTabPage tab, Action<string> onShowTab, VisualElement container)
        {
            _tabs.Add(tab);
            var toggle = new ToolbarToggle
            {
                text = tab.Title
            };
            container.Add(tab.TabContent);

            toggle.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                {
                    onShowTab(tab.Id);
                }
                else if (ActiveTabPage?.Id == tab.Id)
                {
                    toggle.SetValueWithoutNotify(true);
                }
            });

            _toggles.Add(toggle);
            Add(toggle);

            if (ActiveTabPage == null)
            {
                ActiveTabPage = tab;
                ActiveTabPage.Show();
                toggle.SetValueWithoutNotify(true);
            }
            else
            {
                tab.OnHide();
                toggle.SetValueWithoutNotify(false);
            }

            return this;
        }

        public int SwitchTabPage(string id)
        {
            if (ActiveTabPage != null)
            {
                ActiveTabPage.OnHide();
                ActiveTabPage = null;
            }

            var index = _tabs.FindIndex(t => t.Id == id);
            if (index >= 0)
            {
                ActiveTabPage = _tabs[index];
                ActiveTabPage.Show();

                for (var i = 0; i < _toggles.Count; i++)
                {
                    _toggles[i].SetValueWithoutNotify(i == index);
                }

                return index;
            }

            return -1;
        }

        public int SwitchTabPage(IToolbarTabPage nextTab)
        {
            return SwitchTabPage(nextTab.Id);
        }


        public void HideAllTabPages(bool keepActiveTabPage = false)
        {
            foreach (var tab in _tabs)
            {
                tab.OnHide();
            }

            if (!keepActiveTabPage)
            {
                ActiveTabPage = null;
                foreach (var toggle in _toggles)
                {
                    toggle.SetValueWithoutNotify(false);
                }
            }
        }
    }

    /// <summary>
    ///     ToolbarWithTabPages で管理されるタブページのインターフェース。
    /// </summary>
    public interface IToolbarTabPage : IDisposable
    {
        public string Id { get; }
        public string Title { get; }
        public VisualElement TabContent { get; }
        public void Show();
        public void OnHide();
    }
}
