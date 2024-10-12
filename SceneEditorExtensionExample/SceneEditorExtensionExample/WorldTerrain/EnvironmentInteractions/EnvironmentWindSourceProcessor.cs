using SceneEditorExtensionExample.Rendering;
using Stride.Core.Annotations;
using Stride.Engine;
using Stride.Games;
using Stride.Rendering;
using System.Collections.Generic;

namespace SceneEditorExtensionExample.WorldTerrain.EnvironmentInteractions;

class EnvironmentWindSourceProcessor : EntityProcessor<EnvironmentWindSourceComponent, EnvironmentWindSourceProcessor.AssociatedData>, IEntityComponentRenderProcessor
{
    private readonly List<EnvironmentWindSourceComponent> _environmentWindSourceComponents = new(capacity: 32);

    public VisibilityGroup VisibilityGroup { get; set; }

    public EnvironmentWindSourceProcessor()
    {

    }

    protected override void OnSystemAdd()
    {
        VisibilityGroup.Tags.Set(EnvironmentInteractionRenderFeature.EnvironmentWindSourcesKey, _environmentWindSourceComponents);
    }

    protected override void OnSystemRemove()
    {
        VisibilityGroup.Tags.Remove(EnvironmentInteractionRenderFeature.EnvironmentWindSourcesKey);
    }

    protected override AssociatedData GenerateComponentData([NotNull] Entity entity, [NotNull] EnvironmentWindSourceComponent component)
    {
        return new AssociatedData
        {
        };
    }

    protected override bool IsAssociatedDataValid([NotNull] Entity entity, [NotNull] EnvironmentWindSourceComponent component, [NotNull] AssociatedData data)
    {
        return true;
    }

    protected override void OnEntityComponentAdding(Entity entity, [NotNull] EnvironmentWindSourceComponent component, [NotNull] AssociatedData data)
    {
        //component.Initialize(Services);
        _environmentWindSourceComponents.Add(component);
    }

    protected override void OnEntityComponentRemoved(Entity entity, [NotNull] EnvironmentWindSourceComponent component, [NotNull] AssociatedData data)
    {
        //component.Deinitialize();
        _environmentWindSourceComponents.Remove(component);
    }

    public override void Update(GameTime time)
    {
        foreach (var kv in ComponentDatas)
        {
            var comp = kv.Key;
            comp.Update(time);
        }
    }

    public class AssociatedData
    {
    }
}
