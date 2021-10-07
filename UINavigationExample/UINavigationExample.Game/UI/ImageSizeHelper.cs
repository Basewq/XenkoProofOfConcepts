using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.UI;
using System;

namespace UINavigationExample.UI
{
    // This code is taken from Stride's source code, which is marked internal.
    internal static class ImageSizeHelper
    {
        /// <summary>
        /// Calculates the actual image size from the size that is available.
        /// </summary>
        public static Vector3 CalculateImageSizeFromAvailable(Sprite sprite, Vector3 availableSizeWithoutMargins, StretchType stretchType, StretchDirection stretchDirection, bool isMeasuring)
        {
            if (sprite == null) // no associated image -> no region needed
                return Vector3.Zero;

            var idealSize = sprite.SizeInPixels;
            if (idealSize.X <= 0 || idealSize.Y <= 0) // image size null or invalid -> no region needed
                return Vector3.Zero;

            if (float.IsInfinity(availableSizeWithoutMargins.X) && float.IsInfinity(availableSizeWithoutMargins.Y)) // unconstrained available size -> take the best size for the image: the image size
                return new Vector3(idealSize, 0);

            // initialize the desired size with maximum available size
            var desiredSize = availableSizeWithoutMargins;

            // compute the desired image ratios
            var desiredScale = new Vector2(desiredSize.X / idealSize.X, desiredSize.Y / idealSize.Y);

            // when the size along a given axis is free take the same ratio as the constrained axis.
            if (float.IsInfinity(desiredScale.X))
                desiredScale.X = desiredScale.Y;
            if (float.IsInfinity(desiredScale.Y))
                desiredScale.Y = desiredScale.X;

            // adjust the scales depending on the type of stretch to apply
            switch (stretchType)
            {
                case StretchType.None:
                    desiredScale = Vector2.One;
                    break;
                case StretchType.Uniform:
                    desiredScale.X = desiredScale.Y = Math.Min(desiredScale.X, desiredScale.Y);
                    break;
                case StretchType.UniformToFill:
                    desiredScale.X = desiredScale.Y = Math.Max(desiredScale.X, desiredScale.Y);
                    break;
                case StretchType.FillOnStretch:
                    // if we are only measuring we prefer keeping the image resolution than using all the available space.
                    if (isMeasuring)
                        desiredScale.X = desiredScale.Y = Math.Min(desiredScale.X, desiredScale.Y);
                    break;
                case StretchType.Fill:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(stretchType));
            }

            // adjust the scales depending on the stretch directions
            switch (stretchDirection)
            {
                case StretchDirection.Both:
                    break;
                case StretchDirection.DownOnly:
                    desiredScale.X = Math.Min(desiredScale.X, 1);
                    desiredScale.Y = Math.Min(desiredScale.Y, 1);
                    break;
                case StretchDirection.UpOnly:
                    desiredScale.X = Math.Max(1, desiredScale.X);
                    desiredScale.Y = Math.Max(1, desiredScale.Y);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(stretchDirection));
            }

            // update the desired size based on the desired scales
            desiredSize = new Vector3(idealSize.X * desiredScale.X, idealSize.Y * desiredScale.Y, 0f);

            if (!isMeasuring || !sprite.HasBorders)
                return desiredSize;

            // consider sprite borders
            var borders = sprite.Borders;   // Note: the original code uses sprite.BordersInternal (to avoid copying struct)
            var borderSum = new Vector2(borders.X + borders.Z, borders.Y + borders.W);
            if (sprite.Orientation == ImageOrientation.Rotated90)
                Utilities.Swap(ref borderSum.X, ref borderSum.Y);

            return new Vector3(Math.Max(desiredSize.X, borderSum.X), Math.Max(desiredSize.Y, borderSum.Y), desiredSize.Z);
        }
    }
}
