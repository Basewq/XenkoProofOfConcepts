using SceneEditorExtensionExample.Rendering;

namespace SceneEditorExtensionExample.WorldTerrain.EnvironmentInteractions.WindSources;

public class EnvironmentWindAmbient : EnvironmentWindSourceBase
{
    public float WindMaxSpeed { get; set; } = 0.3f;

    public override void AddData(ref WindSourcesPerViewData windSourcesPerViewData)
    {
        windSourcesPerViewData.WindAmbient.WindSpeed = WindMaxSpeed;
    }
}
