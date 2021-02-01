using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;

namespace EntityProcessorExample.Player
{
    /* Notes:
     * Processor must be added to Game.SceneSystem.SceneInstance.Processors in order for it to run.
     * For the sake of this example, because PlayerActionComponent has the
     * DefaultEntityComponentProcessor attribute tagged to this processor, it will
     * automatically be registered. If DefaultEntityComponentProcessor cannot
     * be tagged to the processor, eg. multiple processors operating on the same Component,
     * either add a dummy Component (easy/hacky way), or register it manually (harder way,
     * since Game.SceneSystem.SceneInstance is not set up immediately, and may also change).
     */
    class PlayerActionProcessor : EntityProcessor<PlayerActionComponent>
    {
        public PlayerActionProcessor() : base()
        {
            Order = 20; // Ensure this occurs after PlayerInputProcessor
        }

        public override void Update(GameTime gameTime)
        {
            foreach (var kv in ComponentDatas)
            {
                var actionComp = kv.Key;
                var moveVelocity = actionComp.InputDirectionStrength;
                moveVelocity *= 10f;
                var offsetPosition2d = moveVelocity * (float)gameTime.Elapsed.TotalSeconds;
                var offsetPosition = new Vector3(offsetPosition2d.X, 0, offsetPosition2d.Y);
                actionComp.Entity.Transform.Position += offsetPosition;
            }
        }
    }
}
