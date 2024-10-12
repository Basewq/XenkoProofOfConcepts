using SceneEditorExtensionExample.Rendering;
using Stride.Core;
using Stride.Games;

namespace SceneEditorExtensionExample.WorldTerrain.EnvironmentInteractions.WindSources;

public interface IEnvironmentWindSource
{
    void AddData(ref WindSourcesPerViewData windSourcesPerViewData);
    void Update(GameTime time);
}

[DataContract(Inherited = true)]
public abstract class EnvironmentWindSourceBase : IEnvironmentWindSource
{
    public abstract void AddData(ref WindSourcesPerViewData windSourcesPerViewData);

    public virtual void Update(GameTime time) { }
}
