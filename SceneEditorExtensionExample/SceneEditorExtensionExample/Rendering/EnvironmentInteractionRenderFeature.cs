using Stride.Rendering;

namespace SceneEditorExtensionExample.Rendering;

public partial class EnvironmentInteractionRenderFeature : SubRenderFeature
{
    private IEnvironmentInteractionProcessor[] _interactionProcessors;

    public EnvironmentInteractionRenderFeature()
    {
        _interactionProcessors =
        [
            new Interactors(this),
            new WindSources(this)
        ];
    }

    /// <inheritdoc/>
    protected override void InitializeCore()
    {
        foreach (var proc in _interactionProcessors)
        {
            proc.Initialize();
        }
    }

    /// <inheritdoc/>
    public override void Extract()
    {
        if (Context.VisibilityGroup is null)
        {
            return;
        }

        foreach (var proc in _interactionProcessors)
        {
            proc.Extract();
        }
    }

    /// <inheritdoc/>
    public unsafe override void Prepare(RenderDrawContext context)
    {
        foreach (var proc in _interactionProcessors)
        {
            proc.Prepare(context);
        }
    }

    interface IEnvironmentInteractionProcessor
    {
        void Initialize();
        void Extract();
        void Prepare(RenderDrawContext context);
    }
}
