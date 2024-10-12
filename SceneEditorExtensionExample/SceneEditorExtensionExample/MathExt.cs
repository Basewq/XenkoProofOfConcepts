using Stride.Core.Mathematics;
using System;

namespace SceneEditorExtensionExample;

public static class MathExt
{
    public static int ToIntFloor(float value) => (int)MathF.Floor(value);
    
    public static Int3 ToInt3Floor(Vector3 vec) => new Int3(ToIntFloor(vec.X), ToIntFloor(vec.Y), ToIntFloor(vec.Z));
    
    public static Vector3 ToVec3(Int3 int3) => new Vector3(int3.X, int3.Y, int3.Z);
}
