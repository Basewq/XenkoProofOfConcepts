using BepuPhysics;
using Xenko.Core;
using Xenko.Data;

namespace BepuPhysicsExample.BepuPhysicsIntegration
{
    [DataContract]
    [Display("Physics (Bepu)")]
    public class BepuPhysicsSettings : Configuration
    {
        [DataMember(10)]
        public BepuPhysicsEngineFlags Flags;

        /// <userdoc>
        /// The maximum number of simulations the the physics engine can run in a frame to compensate for slowdown
        /// </userdoc>
        [DataMember(20)]
        public int MaxSubSteps = 1;

        /// <userdoc>
        /// The length in seconds of a physics simulation frame. The default is 0.016667 (one sixtieth of a second)
        /// </userdoc>
        [DataMember(30)]
        public float FixedTimeStep = 1.0f / 60.0f;

        [DataMember(40)]
        public AngularIntegrationMode AngularIntegrationMode;
    }
}
