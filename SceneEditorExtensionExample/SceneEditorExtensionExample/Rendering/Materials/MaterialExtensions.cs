using Stride.Core.Mathematics;
using Stride.Rendering.Materials;
using System.Globalization;

namespace SceneEditorExtensionExample.Rendering.Materials
{
    internal static class MaterialExtensions
    {
        public static string ToShaderString(this bool boolValue)
        {
            return boolValue ? "true" : "false";
        }

        public static string ToShaderString(this ColorChannel channel)
        {
            return channel.ToString().ToLowerInvariant();
        }

        public static string ToShaderString(this float floatValue)
        {
            return floatValue.ToString(CultureInfo.InvariantCulture);
        }

        public static string ToShaderString(this Vector2 v)
        {
            return string.Format(CultureInfo.InvariantCulture, "float2({0}, {1})", v.X, v.Y);
        }

        public static string ToShaderString(this Vector3 v)
        {
            return string.Format(CultureInfo.InvariantCulture, "float3({0}, {1}, {2})", v.X, v.Y, v.Z);
        }

        public static string ToShaderString(this Vector4 v)
        {
            return string.Format(CultureInfo.InvariantCulture, "float4({0}, {1}, {2}, {3})", v.X, v.Y, v.Z, v.W);
        }

        public static string ToShaderString(this Color3 c)
        {
            return string.Format(CultureInfo.InvariantCulture, "float3({0}, {1}, {2})", c.R, c.G, c.B);
        }

        public static string ToShaderString(this Color4 c)
        {
            return string.Format(CultureInfo.InvariantCulture, "float4({0}, {1}, {2}, {3})", c.R, c.G, c.B, c.A);
        }
    }
}
