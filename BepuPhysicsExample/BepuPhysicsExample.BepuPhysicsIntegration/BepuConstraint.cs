using BepuPhysics;
using System;

namespace BepuPhysicsExample.BepuPhysicsIntegration
{
    public abstract class BepuConstraint : IDisposable, IBepuRelative
    {
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public virtual void Dispose()
        {
            if (InternalConstraintHandleId == ConstraintHandleId.NotSet) return;

            InternalConstraintHandleId = ConstraintHandleId.NotSet;

            if (RigidBodyA != null && RigidBodyA.LinkedConstraints.Contains(this))
            {
                RigidBodyA.LinkedConstraints.Remove(this);
            }

            if (RigidBodyB != null && RigidBodyB.LinkedConstraints.Contains(this))
            {
                RigidBodyB.LinkedConstraints.Remove(this);
            }
        }

        /// <summary>
        /// Gets the rigid body a.
        /// </summary>
        /// <value>
        /// The rigid body a.
        /// </value>
        public BepuRigidbodyComponent RigidBodyA { get; internal set; }
        /// <summary>
        /// Gets the rigid body b.
        /// </summary>
        /// <value>
        /// The rigid body b.
        /// </value>
        public BepuRigidbodyComponent RigidBodyB { get; internal set; }

        ///// <summary>
        ///// Gets or sets a value indicating whether this <see cref="BepuConstraint"/> is enabled.
        ///// </summary>
        ///// <value>
        /////   <c>true</c> if enabled; otherwise, <c>false</c>.
        ///// </value>
        //public bool Enabled
        //{
        //    get { return InternalConstraint.IsEnabled; }
        //    set { InternalConstraint.IsEnabled = value; }
        //}

        ///// <summary>
        ///// Gets or sets the breaking impulse threshold.
        ///// </summary>
        ///// <value>
        ///// The breaking impulse threshold.
        ///// </value>
        //public float BreakingImpulseThreshold
        //{
        //    get { return InternalConstraint.BreakingImpulseThreshold; }
        //    set { InternalConstraint.BreakingImpulseThreshold = value; }
        //}

        /// <summary>
        /// Gets the applied impulse.
        /// </summary>
        /// <value>
        /// The applied impulse.
        /// </value>
        public float AppliedImpulse
        {
            get
            {
                var impulseValue = Simulation.NativeBepuSimulation.Solver.GetAccumulatedImpulseMagnitude(InternalConstraintHandleId);
                return impulseValue;

            }
        }

        internal ConstraintHandleId InternalConstraintHandleId = ConstraintHandleId.NotSet;

        abstract internal void AddConstraintToSimulation();

        /// <summary>
        /// Gets the Simulation where this Constraint is being processed.
        /// </summary>
        public BepuSimulation Simulation { get; internal set; }
    }
}
