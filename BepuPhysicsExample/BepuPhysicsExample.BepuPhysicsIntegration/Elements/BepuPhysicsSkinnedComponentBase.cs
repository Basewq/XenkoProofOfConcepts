using BepuPhysics;
using System;
using Stride.Core;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;

namespace BepuPhysicsExample.BepuPhysicsIntegration
{
    /// <remarks>
    /// This is essentially all non-static physics components.
    /// </remarks>
    [DataContract("BepuPhysicsSkinnedComponentBase")]
    [Display("BepuPhysicsSkinnedComponentBase")]
    public abstract class BepuPhysicsSkinnedComponentBase : BepuPhysicsTriggerComponentBase
    {
        [DataMemberIgnore]
        internal BodyReference NativeBodyReference;
        //[DataMemberIgnore]
        //internal BodyDescription NativeBodyDescription;

        private bool canSleep;

        /// <summary>
        /// Gets or sets if this element can enter sleep state
        /// </summary>
        /// <value>
        /// true, false
        /// </value>
        /// <userdoc>
        /// Don't process this physics component when it's not moving. This saves CPU.
        /// </userdoc>
        [DataMember(55)]
        [Display("Can sleep")]
        public bool CanSleep
        {
            get
            {
                return canSleep;
            }
            set
            {
                canSleep = value;
                if (NativeBodyReference.Exists)
                {
                    NativeBodyReference.Activity.SleepCandidate = value;
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is active (awake).
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is active; otherwise, <c>false</c>.
        /// </value>
        public bool IsActive => NativeBodyReference.Exists ? NativeBodyReference.Awake : false;

        /// <summary>
        /// Awaken the collider.
        /// </summary>
        public void Activate()
        {
            NativeBodyReference.Awake = true;
        }

        /// <summary>
        /// Gets or sets the link (usually a bone).
        /// </summary>
        /// <value>
        /// The mesh's linked bone name
        /// </value>
        /// <userdoc>
        /// In the case of skinned mesh this must be the bone node name linked with this element. Cannot change during run-time.
        /// </userdoc>
        [DataMember(190)]
        public string NodeName { get; set; }

        internal void ClearNativeBodyReference()
        {
            NativeBodyReference = default;
        }

        //protected override void OnAttach()
        //{
        //    base.OnAttach();

        //    CanSleep = canSleep;
        //}

        protected void SetupBoneLink()
        {
            if (string.IsNullOrEmpty(NodeName) || Data.ModelComponent?.Skeleton == null) return;

            if (!Data.BoneMatricesUpdated)
            {
                Vector3 position, scaling;
                Quaternion rotation;
                Entity.Transform.WorldMatrix.Decompose(out scaling, out rotation, out position);
                var isScalingNegative = scaling.X * scaling.Y * scaling.Z < 0.0f;
                Data.ModelComponent.Skeleton.NodeTransformations[0].LocalMatrix = Entity.Transform.WorldMatrix;
                Data.ModelComponent.Skeleton.NodeTransformations[0].IsScalingNegative = isScalingNegative;
                Data.ModelComponent.Skeleton.UpdateMatrices();
                Data.BoneMatricesUpdated = true;
            }

            BoneIndex = Data.ModelComponent.Skeleton.Nodes.IndexOf(x => x.Name == NodeName);

            if (BoneIndex == -1)
            {
                throw new InvalidOperationException("The specified NodeName doesn't exist in the model hierarchy.");
            }

            BoneWorldMatrixOut = BoneWorldMatrix = Data.ModelComponent.Skeleton.NodeTransformations[BoneIndex].WorldMatrix;
        }
    }
}
