using Stride.Core.Annotations;
using Stride.Engine;

namespace LevelEditorExtensionExample.Editor
{
    class LevelEditProcessor : EntityProcessor<LevelEditComponent>
    {
        protected override void OnEntityComponentAdding(Entity entity, [NotNull] LevelEditComponent component, [NotNull] LevelEditComponent data)
        {
            // Disable this entity. Alternatively we could remove this entity on the next Update.
            entity.EnableAll(enabled: false);
        }
    }
}
