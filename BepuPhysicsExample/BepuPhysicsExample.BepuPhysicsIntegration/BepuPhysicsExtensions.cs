using BepuPhysics.Collidables;
using BepuPhysics.Constraints;
using System;
using System.Diagnostics;
using Stride.Physics;

namespace BepuPhysicsExample.BepuPhysicsIntegration
{
    static class BepuPhysicsExtensions
    {
        public static CollidableMobility ToBepuCollidableType(this RigidBodyTypes rigidBodyType)
        {
            switch (rigidBodyType)
            {
                case RigidBodyTypes.Static:
                    return CollidableMobility.Static;
                case RigidBodyTypes.Dynamic:
                    return CollidableMobility.Dynamic;
                case RigidBodyTypes.Kinematic:
                    return CollidableMobility.Kinematic;
                default:
                    Debug.Fail($"Unknown rigidBodyType: {rigidBodyType}");
                    return CollidableMobility.Static;
            }
        }

        public static void Average(in SpringSettings springSettingsA, in SpringSettings springSettingsB, out SpringSettings springSettingsOutput)
        {
            springSettingsOutput.AngularFrequency = (springSettingsA.AngularFrequency + springSettingsB.AngularFrequency) * 0.5f;
            springSettingsOutput.TwiceDampingRatio = (springSettingsA.TwiceDampingRatio + springSettingsB.TwiceDampingRatio) * 0.5f;
        }
    }
}
