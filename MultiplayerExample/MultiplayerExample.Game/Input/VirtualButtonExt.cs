using Stride.Input;

namespace MultiplayerExample.Input
{
    class VirtualButtonExt : IVirtualButton
    {
        private readonly VirtualButton _baseButton;

        public VirtualButtonExt(VirtualButton baseButton)
        {
            _baseButton = baseButton;
        }

        /// <summary>
        /// Modifiers are treated different from standard buttons.
        /// eg. Ctrl/Shift keys are generally considered modifier keys.
        /// A common situation is key combos, eg. Shift + A, where the combo requires shift to be pressed <i>first</i>
        /// for it to be triggered as uppercase 'A'.
        /// </summary>
        public bool IsTreatedAsModifier { get; set; }

        public float GetValue(InputManager manager) => _baseButton.GetValue(manager);

        public bool IsDown(InputManager manager) => _baseButton.IsDown(manager);

        public bool IsPressed(InputManager manager) => _baseButton.IsPressed(manager);

        public bool IsReleased(InputManager manager) => _baseButton.IsReleased(manager);
    }
}
