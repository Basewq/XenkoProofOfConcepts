using Stride.Core.Mathematics;
using System;
using System.Diagnostics;

namespace MultiplayerExample.Input
{
    interface IVirtualButtonValueTransform
    {
        float TransformValue(float value);
    }

    class VirtualButtonDeadZoneThresholdValue : IVirtualButtonValueTransform
    {
        private float _inputValueMultiplerAfterDeadZone;

        private float _deadZoneThreshold;
        public float DeadZoneThreshold
        {
            get => _deadZoneThreshold;
            set
            {
                Debug.Assert(0 <= _deadZoneThreshold && _deadZoneThreshold < 1);
                _deadZoneThreshold = value;
                _inputValueMultiplerAfterDeadZone = 1 / (1 - _deadZoneThreshold);
            }
        }

        public VirtualButtonDeadZoneThresholdValue(float deadZoneThreshold)
        {
            DeadZoneThreshold = deadZoneThreshold;
        }

        public float TransformValue(float value)
        {
            float inputValue = value;
            if (MathUtil.IsInRange(Math.Abs(value), 0, _deadZoneThreshold))
            {
                if (value >= 0)
                {
                    inputValue = (MathUtil.Clamp(value, _deadZoneThreshold, 1) - _deadZoneThreshold) * _inputValueMultiplerAfterDeadZone;
                }
                else
                {
                    inputValue = (MathUtil.Clamp(value, -1, -_deadZoneThreshold) + _deadZoneThreshold) * _inputValueMultiplerAfterDeadZone;
                }
            }
            return inputValue;
        }
    }

    class VirtualButtonScaleValue : IVirtualButtonValueTransform
    {
        public float ScaleValue { get; set; }

        public VirtualButtonScaleValue(float scaleValue)
        {
            ScaleValue = scaleValue;
        }

        public float TransformValue(float value) => value * ScaleValue;
    }

    class VirtualButtonClampValue : IVirtualButtonValueTransform
    {
        public float ClampMin { get; set; }
        public float ClampMax { get; set; }

        public VirtualButtonClampValue(float clampMin, float clampMax)
        {
            ClampMin = clampMin;
            ClampMax = clampMax;
        }

        public float TransformValue(float value) => MathUtil.Clamp(value, ClampMin, ClampMax);
    }
}
