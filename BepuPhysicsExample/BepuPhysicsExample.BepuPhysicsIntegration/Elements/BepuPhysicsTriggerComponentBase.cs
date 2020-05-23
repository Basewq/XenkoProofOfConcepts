using System.ComponentModel;
using Xenko.Core;
using Xenko.Engine;

namespace BepuPhysicsExample.BepuPhysicsIntegration
{
    [DataContract("BepuPhysicsTriggerComponentBase")]
    [Display("BepuPhysicsTriggerComponentBase")]
    public abstract class BepuPhysicsTriggerComponentBase : BepuPhysicsComponent
    {
        [DataMember(71)]
        public bool IsTrigger { get; set; }

        /// <summary>
        /// Gets or sets if this element is enabled in the physics engine
        /// </summary>
        /// <value>
        /// true, false
        /// </value>
        /// <userdoc>
        /// If this element is enabled in the physics engine
        /// </userdoc>
        [DataMember(-10)]
        [DefaultValue(true)]
        public override bool Enabled
        {
            get
            {
                return base.Enabled;
            }
            set
            {
                base.Enabled = value;

                //if (!NativeCollidableReference.IsSet) return;

                //if (value && isTrigger)
                //{
                //    //We still have to add this flag if we are actively a trigger
                //    NativeCollisionObject.CollisionFlags |= BulletSharp.CollisionFlags.NoContactResponse;
                //}
            }
        }

        protected override void OnAttach()
        {
            base.OnAttach();
            // Set pre-set post deserialization properties
            //IsTrigger = isTrigger;
        }
    }
}
