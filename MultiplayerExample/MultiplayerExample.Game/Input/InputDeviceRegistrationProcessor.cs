using Stride.Core.Annotations;
using Stride.Engine;
using Stride.Games;
using Stride.Input;

namespace MultiplayerExample.Input
{
    class InputDeviceRegistrationProcessor : EntityProcessor<InputDeviceComponent>
    {
        private InputManager _inputManager;

        protected override void OnSystemAdd()
        {
            // Stride's services should all be registered at this point, so we can get
            // any of them.
            _inputManager = Services.GetService<InputManager>();
        }

        protected override void OnEntityComponentAdding(Entity entity, [NotNull] InputDeviceComponent component, [NotNull] InputDeviceComponent data)
        {
            UpdateRegisteredInputs(entity, component);
        }

        public void UpdateRegisteredInputs(Entity entity, InputDeviceComponent component)
        {
            if (component.IsKeyboardControlsEnabled)
            {
                // Should probably loop through all keyboard devices,
                // and maybe check if any other player exists and ensure
                // we don't register against those devices.
                component.ActiveKeyboard = _inputManager.Keyboard;
                component.ActiveMouse = _inputManager.Mouse;
            }
            if (component.IsControllerEnabled)
            {
                component.ActiveController = _inputManager.DefaultGamePad;
            }
        }

        public override void Update(GameTime gameTime)
        {
            // TODO: Should probably detect device disconnection/reconnection here
            //foreach (var kv in ComponentDatas)
            //{
            //}
        }
    }
}
