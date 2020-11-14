using Stride.Input;
using System;
using System.Collections.Generic;

namespace MultiplayerExample.Input
{
    class VirtualButtonConfigExt : VirtualButtonConfig
    {
        private readonly Dictionary<object, float> _previousValues = new Dictionary<object, float>();

        public void SaveValueAsPreviousValue(InputManager inputManager)
        {
            _previousValues.Clear();
            foreach (var name in BindingNames)
            {
                _previousValues[name] = GetValue(inputManager, name);
            }
        }

        internal float GetPreviousValue(object bindingName)
        {
            _previousValues.TryGetValue(bindingName, out float value);
            return value;
        }
    }

    static class VirtualButtonConfigExtensions
    {
        private const float IsDownValueThreshold = 0.75f;

        /// <summary>
        /// True when the button has just been pressed.
        /// </summary>
        public static bool IsJustPressed(this InputManager inputManager,
            int virtualButtonConfigIndex, object bindingName, VirtualButtonConfigExt configExt)
        {
            float inputValue = inputManager.GetVirtualButton(virtualButtonConfigIndex, bindingName);
            float prevInputValue = configExt.GetPreviousValue(bindingName);
            if (Math.Abs(inputValue) >= IsDownValueThreshold && Math.Abs(prevInputValue) < IsDownValueThreshold)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// True when the button has just been released.
        /// </summary>
        public static bool IsJustReleased(this InputManager inputManager,
            int virtualButtonConfigIndex, object bindingName, VirtualButtonConfigExt configExt)
        {
            float inputValue = inputManager.GetVirtualButton(virtualButtonConfigIndex, bindingName);
            float prevInputValue = configExt.GetPreviousValue(bindingName);
            if (Math.Abs(inputValue) < IsDownValueThreshold && Math.Abs(prevInputValue) >= IsDownValueThreshold)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// True if the button is down in this frame.
        /// </summary>
        public static bool IsDown(this InputManager inputManager,
            int virtualButtonConfigIndex, object bindingName)
        {
            float inputValue = inputManager.GetVirtualButton(virtualButtonConfigIndex, bindingName);
            if (Math.Abs(inputValue) >= IsDownValueThreshold)
            {
                return true;
            }
            return false;
        }
    }
}
