using Stride.Engine;
using Stride.UI;
using System.Collections.Generic;
using System.Diagnostics;

namespace LevelEditorExtensionExample.UI
{
    readonly struct UIElementKey<TUIElement> where TUIElement : UIElement
    {
        public readonly string UIName;

        public UIElementKey(string uiName)
        {
            UIName = uiName;
        }
    }

    static class UIElementKeyExt
    {
        public static bool TryGetUI<TUIElement>(this UIComponent uiComponent, UIElementKey<TUIElement> key, out TUIElement uiElement)
            where TUIElement : UIElement
        {
            Debug.Assert(uiComponent != null, $"UIComponent not assigned.");
            return TryGetUI(uiComponent.Page, key, out uiElement);
        }

        public static bool TryGetUI<TUIElement>(this UIPage uiPage, UIElementKey<TUIElement> key, out TUIElement uiElement)
            where TUIElement : UIElement
        {
            uiElement = (TUIElement)uiPage.RootElement.FindName(key.UIName);
            return uiElement != null;
        }

        public static TUIElement GetUI<TUIElement>(this UIComponent uiComponent, UIElementKey<TUIElement> key)
            where TUIElement : UIElement
        {
            Debug.Assert(uiComponent != null, $"UIComponent not assigned.");
            return GetUI(uiComponent.Page, key);
        }

        public static TUIElement GetUI<TUIElement>(this UIPage uiPage, UIElementKey<TUIElement> key)
            where TUIElement : UIElement
        {
            var element = (TUIElement)uiPage.RootElement.FindName(key.UIName);
            Debug.Assert(element != null, $"UIElement {key.UIName} not found.");
            return element;
        }

        public static IEnumerable<TUIElement> GetAllUI<TUIElement>(this UIPage uiPage, UIElementKey<TUIElement> key)
            where TUIElement : UIElement
        {
            var rootElement = uiPage.RootElement;
            return GetAllUI<TUIElement>(rootElement, key.UIName);
        }

        public static IEnumerable<TUIElement> GetAllUI<TUIElement>(this UIElement element, UIElementKey<TUIElement> key)
            where TUIElement : UIElement
        {
            return GetAllUI<TUIElement>(element, key.UIName);
        }

        private static IEnumerable<TUIElement> GetAllUI<TUIElement>(UIElement element, string uiName)
            where TUIElement : UIElement
        {
            if (element.Name == uiName)
            {
                yield return (TUIElement)element;
            }
            foreach (var ch in element.VisualChildren)
            {
                var foundUIChildren = GetAllUI<TUIElement>(ch, uiName);
                foreach (var uiChild in foundUIChildren)
                {
                    yield return uiChild;
                }
            }
        }
    }
}
