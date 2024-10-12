using SceneEditorExtensionExample.Rendering;
using Stride.Core.Annotations;
using Stride.Engine;
using Stride.Games;
using Stride.Rendering;
using System.Collections.Generic;

namespace SceneEditorExtensionExample.WorldTerrain.EnvironmentInteractions;

class EnvironmentInteractorProcessor : EntityProcessor<EnvironmentInteractorComponent, EnvironmentInteractorProcessor.AssociatedData>, IEntityComponentRenderProcessor
{
    private readonly List<EnvironmentInteractorComponent> _environmentInteractorComponents = new(capacity: 32);

    public VisibilityGroup VisibilityGroup { get; set; }

    public EnvironmentInteractorProcessor()
    {

    }

    protected override void OnSystemAdd()
    {
        VisibilityGroup.Tags.Set(EnvironmentInteractionRenderFeature.EnvironmentInteractorsKey, _environmentInteractorComponents);
    }

    protected override void OnSystemRemove()
    {
        VisibilityGroup.Tags.Remove(EnvironmentInteractionRenderFeature.EnvironmentInteractorsKey);
    }

    protected override AssociatedData GenerateComponentData([NotNull] Entity entity, [NotNull] EnvironmentInteractorComponent component)
    {
        return new AssociatedData
        {
        };
    }

    protected override bool IsAssociatedDataValid([NotNull] Entity entity, [NotNull] EnvironmentInteractorComponent component, [NotNull] AssociatedData data)
    {
        return true;
    }

    protected override void OnEntityComponentAdding(Entity entity, [NotNull] EnvironmentInteractorComponent component, [NotNull] AssociatedData data)
    {
        //component.Initialize(Services);
        _environmentInteractorComponents.Add(component);
    }

    protected override void OnEntityComponentRemoved(Entity entity, [NotNull] EnvironmentInteractorComponent component, [NotNull] AssociatedData data)
    {
        //component.Deinitialize();
        _environmentInteractorComponents.Remove(component);
    }

    public override void Update(GameTime time)
    {
        //foreach (var kv in ComponentDatas)
        //{
        //    var comp = kv.Key;
        //    comp.Update(time);
        //}
    }

    public class AssociatedData
    {
    }
}
