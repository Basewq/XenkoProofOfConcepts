namespace GameScreenManagerExample.Utilities
{
    public static class MathExt
    {
        /// <summary>
        /// Wraps on zero-based index.
        /// </summary>
        public static void Wrap(ref float value, float moduloN)
        {
            float newValue = value;
            if (newValue < 0)
            {
                while (newValue < 0)
                {
                    newValue += moduloN;
                }
            }
            else if (newValue >= moduloN)
            {
                while (newValue >= moduloN)
                {
                    newValue -= moduloN;
                }
            }

            value = newValue;
        }

        public static float WrapOn(this float value, float moduloN)
        {
            Wrap(ref value, moduloN);
            return value;
        }

        /// <summary>
        /// Wraps on zero-based index.
        /// </summary>
        public static void Wrap(ref int value, int moduloN)
        {
            int newValue = value;
            if (newValue < 0)
            {
                while (newValue < 0)
                {
                    newValue += moduloN;
                }
            }
            else if (newValue >= moduloN)
            {
                while (newValue >= moduloN)
                {
                    newValue -= moduloN;
                }
            }

            value = newValue;
        }

        public static int WrapOn(this int value, int moduloN)
        {
            Wrap(ref value, moduloN);
            return value;
        }
    }
}
