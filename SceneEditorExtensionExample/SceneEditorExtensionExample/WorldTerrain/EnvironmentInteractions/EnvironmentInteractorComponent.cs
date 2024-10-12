using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;

namespace SceneEditorExtensionExample.WorldTerrain.EnvironmentInteractions;

[ComponentCategory("Environment")]
[DataContract]
[DefaultEntityComponentRenderer(typeof(EnvironmentInteractorProcessor))]
public class EnvironmentInteractorComponent : EntityComponent
{
    public bool IsEnabled { get; set; } = true;

    public float Radius { get; set; } = 1;

    public Vector3 PositionOffset { get; set; }

    public Vector3 GroundPosition
    {
        get
        {
            var pos = Entity.Transform.Position;    // For performance reasons we only handle top-level entities
            pos += PositionOffset;
            return pos;
        }
    }
}
