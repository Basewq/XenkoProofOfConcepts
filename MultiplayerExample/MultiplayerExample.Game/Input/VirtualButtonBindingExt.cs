using Stride.Core.Mathematics;
using Stride.Input;
using System;

namespace MultiplayerExample.Input
{
    class VirtualButtonBindingExt : VirtualButtonBinding
    {
        private readonly float _scaleValue;
        private readonly float _deadZoneThreshold;
        private readonly float _inputValueMultiplerAfterDeadZone;

        public VirtualButtonBindingExt(
            object name, IVirtualButton button, float scaleValue = 1, float deadZoneThreshold = 0)
            : base(name, button)
        {
            _scaleValue = scaleValue;
            _deadZoneThreshold = deadZoneThreshold;
            _inputValueMultiplerAfterDeadZone = 1 / (1 - _deadZoneThreshold);
        }

        public override float GetValue(InputManager manager)
        {
            float baseValue = base.GetValue(manager);
            float inputValue = baseValue;
            if (_deadZoneThreshold > 0 && MathUtil.IsInRange(Math.Abs(baseValue), 0, _deadZoneThreshold))
            {
                if (baseValue >= 0)
                {
                    inputValue = (MathUtil.Clamp(baseValue, _deadZoneThreshold, 1) - _deadZoneThreshold) * _inputValueMultiplerAfterDeadZone;
                }
                else
                {
                    inputValue = (MathUtil.Clamp(baseValue, -1, -_deadZoneThreshold) + _deadZoneThreshold) * _inputValueMultiplerAfterDeadZone;
                }
            }
            return inputValue * _scaleValue;
        }
    }

    class VirtualButtonBindingClampExt : VirtualButtonBindingExt
    {
        private readonly float _clampMin;
        private readonly float _clampMax;

        public VirtualButtonBindingClampExt(
             object name, IVirtualButton button, float clampMin, float clampMax, float scaleValue = 1, float deadZoneThreshold = 0)
             : base(name, button, scaleValue, deadZoneThreshold)
        {
            _clampMin = clampMin;
            _clampMax = clampMax;
        }

        public override float GetValue(InputManager manager)
        {
            float baseValue = base.GetValue(manager);
            return MathUtil.Clamp(baseValue, _clampMin, _clampMax);
        }
    }
}
