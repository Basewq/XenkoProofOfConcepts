using SceneEditorExtensionExample.WorldTerrain.EnvironmentInteractions.WindSources;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Games;

namespace SceneEditorExtensionExample.WorldTerrain.EnvironmentInteractions;

[ComponentCategory("Environment")]
[DataContract]
[AllowMultipleComponents]
[DefaultEntityComponentRenderer(typeof(EnvironmentWindSourceProcessor))]
public class EnvironmentWindSourceComponent : EntityComponent
{
    public bool IsEnabled { get; set; } = true;

    [Stride.Core.Annotations.NotNull]
    [Display(Expand = ExpandRule.Always)]
    public IEnvironmentWindSource WindType { get; set; } = new EnvironmentWindAmbient();

    internal void Update(GameTime time) => WindType.Update(time);
}
