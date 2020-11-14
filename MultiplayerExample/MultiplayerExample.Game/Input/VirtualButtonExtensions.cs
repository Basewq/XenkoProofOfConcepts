using Stride.Input;

namespace MultiplayerExample.Input
{
    static class VirtualButtonExtensions
    {
        public static VirtualButton.GamePad WithIndex(this VirtualButton.GamePad gamePad, int index)
        {

            if (gamePad.PadIndex != index)
            {
                return gamePad.OfGamePad(index);
            }
            else
            {
                // Don't allocate new object
                return gamePad;
            }
        }
    }
}
