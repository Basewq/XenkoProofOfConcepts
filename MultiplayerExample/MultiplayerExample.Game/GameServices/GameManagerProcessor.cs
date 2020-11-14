using Stride.Core.Annotations;
using Stride.Engine;

namespace MultiplayerExample.GameServices
{
    class GameManagerProcessor : EntityProcessor<GameManager>
    {
        protected override void OnEntityComponentAdding(Entity entity, [NotNull] GameManager component, [NotNull] GameManager data)
        {
            component.Initialize(Services);
        }
    }
}
