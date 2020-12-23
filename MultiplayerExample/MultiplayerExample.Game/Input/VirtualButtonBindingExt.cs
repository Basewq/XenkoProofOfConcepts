using Stride.Input;

namespace MultiplayerExample.Input
{
    class VirtualButtonBindingExt : VirtualButtonBinding
    {
        private readonly IVirtualButtonValueTransform[] _transforms;

        public bool IsEnabled { get; set; } = true;

        public VirtualButtonBindingExt(
            object name, IVirtualButton button, params IVirtualButtonValueTransform[] transforms)
            : base(name, button)
        {
            _transforms = transforms;
        }

        public override float GetValue(InputManager manager)
        {
            if (!IsEnabled)
            {
                return 0;
            }

            float baseValue = base.GetValue(manager);
            float inputValue = baseValue;
            for (int i = 0; i < _transforms.Length; i++)
            {
                inputValue = _transforms[i].TransformValue(inputValue);
            }
            return inputValue;
        }
    }
}
