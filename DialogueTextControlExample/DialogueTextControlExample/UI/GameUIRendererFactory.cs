using Stride.Core;
using Stride.Rendering.UI;
using Stride.UI;
using Stride.UI.Renderers;
using System;
using System.Collections.Generic;
using System.Reflection;
using DialogueTextControlExample.UI.Renderers;
using DialogueTextControlExample.UI.Controls;

namespace DialogueTextControlExample.UI
{
    // This class is important to ensure Stride recognizes DialogueText uses DialogueTextRenderer,
    // otherwise it'll default back to DefaultTextBlockRenderer since DialogueText had inherited TextBlock
    class GameUIRendererFactory : IElementRendererFactory
    {
        private readonly Dictionary<Type, ElementRenderer> _typeToRenderers = new();

        public GameUIRendererFactory(IServiceRegistry services)
        {
            _typeToRenderers[typeof(DialogueText)] = new DialogueTextRenderer(services);
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
