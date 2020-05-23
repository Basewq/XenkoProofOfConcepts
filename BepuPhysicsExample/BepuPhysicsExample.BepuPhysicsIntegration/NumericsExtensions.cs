using Xenko.Core.Mathematics;

namespace BepuPhysicsExample.BepuPhysicsIntegration
{
    static class NumericsExtensions
    {
        public static System.Numerics.Vector3 ToNumericsVector3(this in Vector3 vector)
        {
            return new System.Numerics.Vector3(vector.X, vector.Y, vector.Z);
        }

        public static Vector3 ToXenkoVector3(this in System.Numerics.Vector3 vector)
        {
            return new Vector3(vector.X, vector.Y, vector.Z);
        }

        public static BepuUtilities.Quaternion ToBepuQuaternion(this in Quaternion quaternion)
        {
            return new BepuUtilities.Quaternion(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);
        }
        public static Quaternion ToXenkoQuaternion(this in BepuUtilities.Quaternion quaternion)
        {
            return new Quaternion(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);
        }
    }
}
