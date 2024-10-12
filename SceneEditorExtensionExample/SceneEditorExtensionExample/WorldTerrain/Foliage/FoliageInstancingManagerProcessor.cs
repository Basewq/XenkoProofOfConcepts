using Stride.Core.Annotations;
using Stride.Engine;
using Stride.Games;
using Stride.Rendering;

#if GAME_EDITOR
using Stride.Assets.Presentation.AssetEditors.GameEditor.Game;
using Stride.Assets.Presentation.AssetEditors.SceneEditor.Game;
#endif

namespace SceneEditorExtensionExample.WorldTerrain.Foliage;

class FoliageInstancingManagerProcessor : EntityProcessor<FoliageInstancingManagerComponent, FoliageInstancingManagerProcessor.AssociatedData>
{
#if GAME_EDITOR
    private SceneEditorGame _sceneEditorGame;
#endif

    public VisibilityGroup VisibilityGroup { get; set; }

    public FoliageInstancingManagerProcessor()
    {
        Order = 100000;     // Make this processor occur to happen after any camera position changes
    }

    protected override void OnSystemAdd()
    {
#if GAME_EDITOR
        _sceneEditorGame = Services.GetService<IGame>() as SceneEditorGame;
#endif
    }

    protected override AssociatedData GenerateComponentData([NotNull] Entity entity, [NotNull] FoliageInstancingManagerComponent component)
    {
        return new AssociatedData
        {
        };
    }

    protected override bool IsAssociatedDataValid([NotNull] Entity entity, [NotNull] FoliageInstancingManagerComponent component, [NotNull] AssociatedData data)
    {
        return true;
    }

    protected override void OnEntityComponentAdding(Entity entity, [NotNull] FoliageInstancingManagerComponent component, [NotNull] AssociatedData data)
    {
        component.Initialize(Services);
    }

    protected override void OnEntityComponentRemoved(Entity entity, [NotNull] FoliageInstancingManagerComponent component, [NotNull] AssociatedData data)
    {
        component.Deinitialize();
    }

    public override void Draw(RenderContext context)
    {
        foreach (var kv in ComponentDatas)
        {
            CameraComponent overrideCameraComponent = null;
#if GAME_EDITOR
            // Chunk culling should be done on the editor's camera when in the editor
            var cameraService = _sceneEditorGame.EditorServices.Get<IEditorGameCameraService>();
            overrideCameraComponent = cameraService?.Component;
#endif
            var comp = kv.Key;
            var data = kv.Value;
            comp.Update(context.Time, overrideCameraComponent);
        }
    }

    public class AssociatedData
    {
    }
}
