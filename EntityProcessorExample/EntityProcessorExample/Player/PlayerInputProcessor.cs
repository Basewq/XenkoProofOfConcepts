using System.Collections.Generic;
using Xenko.Core.Annotations;
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Games;
using Xenko.Input;

namespace EntityProcessorExample.Player
{
    /* Notes:
     * Processor must be added to Game.SceneSystem.SceneInstance.Processors in order for it to run.
     * For the sake of this example, because PlayerInputComponent has the
     * DefaultEntityComponentProcessor attribute tagged to this processor, it will
     * automatically be registered. If DefaultEntityComponentProcessor cannot
     * be tagged to the processor, eg. multiple processors operating on the same Component,
     * either add a dummy Component (easy/hacky way), or register it manually (harder way,
     * since Game.SceneSystem.SceneInstance is not set up immediately, and may also change).
     */
    class PlayerInputProcessor : EntityProcessor<PlayerInputComponent, PlayerInputProcessor.AssociatedData>
    {
        private InputManager _inputManager;

        /* Alternatively we could just iterate through ComponentDatas instead of storing a
         * copy ourselves in this list, but for the purpose of this example, we want to
         * show how we can do this.
         */
        private List<AssociatedData> _registeredInputListeners = new List<AssociatedData>(4);

        /* Adding addition EntityComponent types to requiredAdditionalTypes means that
         * the processor will only collect entities that include these additional components.
         */
        public PlayerInputProcessor()
            : base(requiredAdditionalTypes: typeof(PlayerActionComponent))
        {
            /* Ensure this occurs before PlayerActionProcessor.
             * Not happy that we need to fish through each file to find the order,
             * but we must make do with the hand we're given.
             */
            Order = 10;
        }

        protected override void OnSystemAdd()
        {
            // Xenko's services should all be registered at this point, so we can get
            // any of them.
            _inputManager = Services.GetService<InputManager>();
        }

        protected override void OnEntityComponentAdding(Entity entity, [NotNull] PlayerInputComponent component, [NotNull] AssociatedData data)
        {
            _registeredInputListeners.Add(data);
            UpdateRegisteredInputs(entity, component);
        }

        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] PlayerInputComponent component, [NotNull] AssociatedData data)
        {
            _registeredInputListeners.Remove(data);
        }

        protected override AssociatedData GenerateComponentData([NotNull] Entity entity, [NotNull] PlayerInputComponent component)
        {
            return new AssociatedData
            {
                InputComponent = component,
                ActionComponent = entity.Get<PlayerActionComponent>()
                // Can also add other info/components here
            };
        }

        protected override bool IsAssociatedDataValid([NotNull] Entity entity, [NotNull] PlayerInputComponent component, [NotNull] AssociatedData associatedData)
        {
            /* Check the all the components are still the same.
             * I believe this can fail if something happens like a dependent (or optional)
             * component is removed from the entity.
             */
            return associatedData.InputComponent == component
                && associatedData.ActionComponent == entity.Get<PlayerActionComponent>();
        }


        public void UpdateRegisteredInputs(Entity entity, PlayerInputComponent component)
        {
            if (component.IsKeyboardEnabled)
            {
                /* Should probably loop through all keyboard devices,
                 * and maybe check if any other player exists and ensure
                 * we don't register against those devices.
                 */
                component.ActiveKeyboardId = _inputManager.Keyboard?.Id;
            }
        }

        public override void Update(GameTime gameTime)
        {
            foreach (var data in _registeredInputListeners)
            {
                var inputComp = data.InputComponent;
                var actionComp = data.ActionComponent;
                actionComp.InputDirectionStrength = Vector2.Zero;
                if (inputComp.IsKeyboardEnabled)
                {
                    var moveVelocityDir = Vector2.Zero;
                    if (_inputManager.IsKeyDown(Keys.W))
                    {
                        moveVelocityDir.Y -= 1;
                    }
                    if (_inputManager.IsKeyDown(Keys.S))
                    {
                        moveVelocityDir.Y += 1;
                    }
                    if (_inputManager.IsKeyDown(Keys.A))
                    {
                        moveVelocityDir.X -= 1;
                    }
                    if (_inputManager.IsKeyDown(Keys.D))
                    {
                        moveVelocityDir.X += 1;
                    }

                    actionComp.InputDirectionStrength += moveVelocityDir;
                }

                var dirStrSqrd = actionComp.InputDirectionStrength.LengthSquared();
                if (dirStrSqrd > 1)
                {
                    actionComp.InputDirectionStrength.Normalize();
                }
            }
        }

        public class AssociatedData
        {
            public PlayerInputComponent InputComponent { get; set; }
            public PlayerActionComponent ActionComponent { get; set; }
        }
    }
}
