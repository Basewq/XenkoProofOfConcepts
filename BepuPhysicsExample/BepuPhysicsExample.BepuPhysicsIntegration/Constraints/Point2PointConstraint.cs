using BepuPhysics;
using BepuPhysics.Constraints;
using System;
using Xenko.Core.Mathematics;

namespace BepuPhysicsExample.BepuPhysicsIntegration
{
    public class Point2PointConstraint : BepuConstraint
    {
        /// <summary>
        /// Gets or sets the pivot in a.
        /// </summary>
        /// <value>
        /// The pivot in a.
        /// </value>
        public Vector3 PivotInA
        {
            get => InternalPoint2PointConstraint.LocalOffsetA.ToXenkoVector3();
            set
            {
                InternalPoint2PointConstraint.LocalOffsetA = value.ToNumericsVector3();
                InternalPoint2PointConstraint.TargetDistance = (InternalPoint2PointConstraint.LocalOffsetB - InternalPoint2PointConstraint.LocalOffsetA).Length();
            }
        }

        /// <summary>
        /// Gets or sets the pivot in b.
        /// </summary>
        /// <value>
        /// The pivot in b.
        /// </value>
        public Vector3 PivotInB
        {
            get => InternalPoint2PointConstraint.LocalOffsetB.ToXenkoVector3();
            set
            {
                InternalPoint2PointConstraint.LocalOffsetB = value.ToNumericsVector3();
                InternalPoint2PointConstraint.TargetDistance = (InternalPoint2PointConstraint.LocalOffsetB - InternalPoint2PointConstraint.LocalOffsetA).Length();
            }
        }

        /// <summary>
        /// Gets or sets the damping.
        /// </summary>
        /// <value>
        /// The damping.
        /// </value>
        public float Damping
        {
            get => InternalPoint2PointConstraint.SpringSettings.DampingRatio;
            set { InternalPoint2PointConstraint.SpringSettings.DampingRatio = value; }
        }

        /// <summary>
        /// Gets or sets the impulse clamp.
        /// </summary>
        /// <value>
        /// The impulse clamp.
        /// </value>
        public float ImpulseClamp
        {
            get => InternalPoint2PointConstraint.ServoSettings.MaximumForce;
            set { InternalPoint2PointConstraint.ServoSettings.MaximumForce = value; }
        }

        ///// <summary>
        ///// Gets or sets the tau.
        ///// </summary>
        ///// <value>
        ///// The tau.
        ///// </value>
        //public float Tau
        //{
        //    get => InternalPoint2PointConstraint.Setting.Tau;
        //    set { InternalPoint2PointConstraint.Setting.Tau = value; }
        //}

        internal DistanceServo InternalPoint2PointConstraint;

        internal override void AddConstraintToSimulation()
        {
            //RigidBodyA.NativeBodyReference.GetDescription(out var aDescription);
            //RigidBodyB.NativeBodyReference.GetDescription(out var bDescription);

            //var a = nativeSimulation.Bodies.Add(aDescription);
            //var b = nativeSimulation.Bodies.Add(bDescription);

            var nativeSimulation = Simulation.NativeBepuSimulation;
            int handleA = RigidBodyA.NativeBodyReference.Handle;
            int handleB = RigidBodyB.NativeBodyReference.Handle;

            int constraintHandleId = nativeSimulation.Solver.Add(handleA, handleB, InternalPoint2PointConstraint);
            InternalConstraintHandleId = constraintHandleId;
        }
    }
}
