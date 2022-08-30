using Stride.Core;
using Stride.Core.Serialization;
using Stride.Engine;
using Stride.Engine.Design;

namespace LevelEditorExtensionExample.Editor
{
    /* Notes:
     * Make sure the class is public, to ensure Stride Editor detects it.
     *
     * Ensure DataContract is Stride.Core.DataContract, not System.Runtime.Serialization.DataContract, otherwise
     * Xenko Editor ignores this component.
     *
     * DefaultEntityComponentProcessor attribute ensures PlayerInputProcessor is automatically registered
     * to the EntityManager/SceneInstance.
     */
    [DataContract]
    [DefaultEntityComponentProcessor(typeof(LevelEditProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [DefaultEntityComponentProcessor(typeof(LevelEditEditorProcessor), ExecutionMode = ExecutionMode.Editor)]
    public class LevelEditComponent : EntityComponent
    {
        [DataMember(10)]
        public UrlReference<Prefab> BoxPrefab { get; set; }
        [DataMember(11)]
        public int PrefabNextYPosition { get; set; }

        [DataMember(20)]
        public int[] InternalData { get; set; } = new[]
        {
            1, 2, 3, 4, 5, 6
        };
    }
}
