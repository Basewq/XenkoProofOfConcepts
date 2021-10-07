using Stride.Core;
using Stride.Rendering.UI;
using Stride.UI;
using Stride.UI.Renderers;
using System;
using System.Collections.Generic;
using System.Reflection;
using UINavigationExample.UI.Renderers;

namespace UINavigationExample.UI
{
    class GameUIRendererFactory : IElementRendererFactory
    {
        private readonly Dictionary<Type, ElementRenderer> _typeToRenderers = new Dictionary<Type, ElementRenderer>();

        public GameUIRendererFactory(IServiceRegistry services)
        {
            _typeToRenderers[typeof(ButtonExt)] = new ButtonExtRenderer(services);
        }

        public ElementRenderer TryCreateRenderer(UIElement element)
        {
            var currentType = element.GetType();
            while (currentType != null)
            {
                if (_typeToRenderers.TryGetValue(currentType, out var renderer))
                    return renderer;

                currentType = currentType.GetTypeInfo().BaseType;
            }

            return null;
        }

        internal void RegisterToUIRenderFeature(UIRenderFeature uiRenderFeature)
        {
            foreach (var uiType in _typeToRenderers.Keys)
            {
                uiRenderFeature.RegisterRendererFactory(uiType, this);
            }
        }
    }
}
