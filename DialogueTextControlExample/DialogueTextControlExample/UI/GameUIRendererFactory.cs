using DialogueTextControlExample.UI.Controls;
using DialogueTextControlExample.UI.Renderers;
using Stride.Core;
using Stride.UI;
using Stride.UI.Renderers;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace DialogueTextControlExample.UI
{
    // This class is important to ensure Stride recognizes DialogueText uses DialogueTextRenderer,
    // otherwise it'll default back to DefaultTextBlockRenderer since DialogueText had inherited TextBlock
    class GameUIRendererFactory : IElementRendererFactory
    {
        private readonly Dictionary<Type, ElementRenderer> _uiElementTypeToRenderers = new();

        private UIRenderFeatureExt _uiRenderFeature;

        public GameUIRendererFactory(IServiceRegistry services)
        {
            _uiElementTypeToRenderers[typeof(DialogueText)] = new DialogueTextRenderer(services);
        }

        public ElementRenderer TryCreateRenderer(UIElement element)
        {
            var currentType = element.GetType();
            while (currentType != null)
            {
                if (_uiElementTypeToRenderers.TryGetValue(currentType, out var renderer))
                    return renderer;

                currentType = currentType.GetTypeInfo().BaseType;
            }

            return null;
        }

        internal void RegisterToUIRenderFeature(UIRenderFeatureExt uiRenderFeature)
        {
            _uiRenderFeature = uiRenderFeature;
            foreach (var uiType in _uiElementTypeToRenderers.Keys)
            {
                uiRenderFeature.RegisterRendererFactory(uiType, this);
            }
        }
    }
}
