using Stride.Core.Mathematics;
using Stride.Physics;
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace MultiplayerExample.Network.SnapshotStores
{
    static class BulletPhysicsExt
    {
        private static Action<CharacterComponent, Simulation, float> _simulateCharacterComponentFunction = null;
        private static Action<CharacterComponent, Simulation, float> CreateSimulateCharacterComponentFunction(CharacterComponent characterComponent)
        {
            var simulateMethod = new DynamicMethod(
                "SimulateCharacterComponentFunction",
                returnType: null,
                parameterTypes: new Type[] { typeof(CharacterComponent), typeof(Simulation), typeof(float) },
                restrictedSkipVisibility: true
            );

            // We essentially want to make the following call, where (characterComp, simulation, dt) are (arg0, arg1, arg2) for our static method:
            // characterComp.KinematicCharacter.UpdateAction(simulation.discreteDynamicsWorld, dt);

            var generator = simulateMethod.GetILGenerator();

            // Type.GetType("BulletSharp.KinematicCharacterController"); doesn't seem to work since we don't load
            // the BulletSharp lib directly, so a hacky way is just get the type from CharacterComponent where we
            // can get the class type directly from a instantiated object.
            var charCompKinematicCharField = typeof(CharacterComponent).GetField("KinematicCharacter", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var bulletKinChar = charCompKinematicCharField.GetValue(characterComponent);
            var bulletCharControllerType = bulletKinChar.GetType();

            //var bulletWorldType = Type.GetType("BulletSharp.DiscreteDynamicsWorld");
            var simBulletWorldField = typeof(Simulation).GetField("discreteDynamicsWorld", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            var bulletKinematicCharUpdateActionMethod = bulletCharControllerType.GetMethod("UpdateAction");

            // Push the method's arg0 (characterComp)
            generator.Emit(OpCodes.Ldarg_0);
            // Read arg0.field (characterComp.KinematicCharacter)
            generator.Emit(OpCodes.Ldfld, charCompKinematicCharField);

            // Push the method's arg1 (simulation)
            generator.Emit(OpCodes.Ldarg_1);
            // Read arg1.field (simulation.discreteDynamicsWorld)
            generator.Emit(OpCodes.Ldfld, simBulletWorldField);

            // Push the method's arg2 (dt)
            generator.Emit(OpCodes.Ldarg_2);

            // Call characterComp.KinematicCharacter.UpdateAction(simulation.discreteDynamicsWorld, dt);
            generator.Emit(OpCodes.Callvirt, bulletKinematicCharUpdateActionMethod);

            // void return
            generator.Emit(OpCodes.Ret);

            return (Action<CharacterComponent, Simulation, float>)simulateMethod.CreateDelegate(typeof(Action<CharacterComponent, Simulation, float>));
        }

        private delegate void UpdateTransformFromPhysics(CharacterComponent characterComponent, ref Matrix physicsObjectWorldMatrix);
        private static UpdateTransformFromPhysics _updateTransformFromPhysicsComponentFunction = null;
        private static UpdateTransformFromPhysics CreateUpdateTransformFromPhysicsComponentFunction()
        {
            var simulateMethod = new DynamicMethod(
                "SimulateCharacterComponentFunction",
                returnType: null,
                parameterTypes: new Type[] { typeof(CharacterComponent), typeof(Matrix).MakeByRefType() },
                restrictedSkipVisibility: true
            );

            // We essentially want to make the following call, where (characterComp, physicsObjectWorldMatrix) are (arg0, arg1) for our static method:
            // characterComp.UpdateTransformationComponent(ref physicsObjectWorldMatrix);

            var generator = simulateMethod.GetILGenerator();

            var updateTransformationCompMethod = typeof(CharacterComponent).GetMethod("UpdateTransformationComponent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            // Push the method's arg0 (characterComp)
            generator.Emit(OpCodes.Ldarg_0);
            // Push the method's arg1 (simulation)
            generator.Emit(OpCodes.Ldarg_1);

            // Call characterComp.KinematicCharacter.UpdateAction(simulation.discreteDynamicsWorld, dt);
            generator.Emit(OpCodes.Callvirt, updateTransformationCompMethod);

            // void return
            generator.Emit(OpCodes.Ret);

            return (UpdateTransformFromPhysics)simulateMethod.CreateDelegate(typeof(UpdateTransformFromPhysics));
        }

        private static Func<CharacterComponent, Vector3> _getLinearVelocityFunction = null;
        private static Func<CharacterComponent, Vector3> CreateGetLinearVelocityFunction(CharacterComponent characterComponent)
        {
            var simulateMethod = new DynamicMethod(
                "GetLinearVelocityFunction",
                returnType: typeof(Vector3),
                parameterTypes: new Type[] { typeof(CharacterComponent) },
                restrictedSkipVisibility: true
            );

            // We essentially want to make the following call, where (characterComp) is (arg0) for our static method:
            // return characterComp.KinematicCharacter.LinearVelocity;

            var generator = simulateMethod.GetILGenerator();

            // Type.GetType("BulletSharp.KinematicCharacterController"); doesn't seem to work since we don't load
            // the BulletSharp lib directly, so a hacky way is just get the type from CharacterComponent where we
            // can get the class type directly from a instantiated object.
            var charCompKinematicCharField = typeof(CharacterComponent).GetField("KinematicCharacter", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var bulletKinChar = charCompKinematicCharField.GetValue(characterComponent);
            var bulletCharControllerType = bulletKinChar.GetType();

            var bulletKinematicCharLinearVelocityProperty = bulletCharControllerType.GetProperty("LinearVelocity");
            var bulletKinematicCharLinearVelocityGetterMethod = bulletKinematicCharLinearVelocityProperty.GetGetMethod();

            var bulletVec3 = bulletKinematicCharLinearVelocityProperty.GetValue(bulletKinChar);
            var bulletVec3Type = bulletVec3.GetType();

            var bulletToStrideVec3Method = bulletVec3Type.GetMethod("op_Implicit", BindingFlags.Static | BindingFlags.Public, binder: null, types: new[] { bulletVec3Type }, modifiers: null);

            // Push the method's arg0 (characterComp)
            generator.Emit(OpCodes.Ldarg_0);
            // Read arg0.field (characterComp.KinematicCharacter)
            generator.Emit(OpCodes.Ldfld, charCompKinematicCharField);

            // Call characterComp.KinematicCharacter.LinearVelocity;
            generator.Emit(OpCodes.Callvirt, bulletKinematicCharLinearVelocityGetterMethod);

            // Call implicit operator converting Bullet Vector3 to Stride Vector3
            generator.Emit(OpCodes.Call, bulletToStrideVec3Method);

            // Return the top stack value (ie. the Vector3)
            generator.Emit(OpCodes.Ret);

            return (Func<CharacterComponent, Vector3>)simulateMethod.CreateDelegate(typeof(Func<CharacterComponent, Vector3>));
        }

        private static Action<CharacterComponent, Vector3> _setLinearVelocityFunction = null;
        private static Action<CharacterComponent, Vector3> CreateSetLinearVelocityFunction(CharacterComponent characterComponent)
        {
            var simulateMethod = new DynamicMethod(
                "SetLinearVelocityFunction",
                returnType: null,
                parameterTypes: new Type[] { typeof(CharacterComponent), typeof(Vector3) },
                restrictedSkipVisibility: true
            );

            // We essentially want to make the following call, where (characterComp) is (arg0, arg1) for our static method:
            // characterComp.KinematicCharacter.LinearVelocity = linearVelocity;

            var generator = simulateMethod.GetILGenerator();

            // Type.GetType("BulletSharp.KinematicCharacterController"); doesn't seem to work since we don't load
            // the BulletSharp lib directly, so a hacky way is just get the type from CharacterComponent where we
            // can get the class type directly from a instantiated object.
            var charCompKinematicCharField = typeof(CharacterComponent).GetField("KinematicCharacter", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var bulletKinChar = charCompKinematicCharField.GetValue(characterComponent);
            var bulletCharControllerType = bulletKinChar.GetType();

            var bulletKinematicCharLinearVelocityProperty = bulletCharControllerType.GetProperty("LinearVelocity");
            var bulletKinematicCharLinearVelocitySetterMethod = bulletKinematicCharLinearVelocityProperty.GetSetMethod();

            var bulletVec3 = bulletKinematicCharLinearVelocityProperty.GetValue(bulletKinChar);
            var bulletVec3Type = bulletVec3.GetType();

            var strideToBulletVec3Method = bulletVec3Type.GetMethod("op_Implicit", BindingFlags.Static | BindingFlags.Public, binder: null, types: new[] { typeof(Vector3) }, modifiers: null);

            // Push the method's arg0 (characterComp)
            generator.Emit(OpCodes.Ldarg_0);
            // Read arg0.field (characterComp.KinematicCharacter)
            generator.Emit(OpCodes.Ldfld, charCompKinematicCharField);

            // Push the method's arg1 (linearVelocity)
            generator.Emit(OpCodes.Ldarg_1);

            // Call implicit operator converting Stride Vector3 to Bullet Vector3
            generator.Emit(OpCodes.Call, strideToBulletVec3Method);

            // Call characterComp.KinematicCharacter.LinearVelocity = converted(linearVelocity);
            generator.Emit(OpCodes.Callvirt, bulletKinematicCharLinearVelocitySetterMethod);

            // void return
            generator.Emit(OpCodes.Ret);

            return (Action<CharacterComponent, Vector3>)simulateMethod.CreateDelegate(typeof(Action<CharacterComponent, Vector3>));
        }

        private static Action<CharacterComponent, float> _setLinearVelocityYFunction = null;
        private static Action<CharacterComponent, float> CreateSetLinearVelocityYFunction(CharacterComponent characterComponent)
        {
            var simulateMethod = new DynamicMethod(
                "SetLinearVelocityYFunction",
                returnType: null,
                parameterTypes: new Type[] { typeof(CharacterComponent), typeof(float) },
                restrictedSkipVisibility: true
            );

            // We essentially want to make the following call, where (characterComp) is (arg0, arg1) for our static method:
            // characterComp.KinematicCharacter.m_verticalVelocity = linearVelocityY;

            var generator = simulateMethod.GetILGenerator();

            // Type.GetType("BulletSharp.KinematicCharacterController"); doesn't seem to work since we don't load
            // the BulletSharp lib directly, so a hacky way is just get the type from CharacterComponent where we
            // can get the class type directly from a instantiated object.
            var charCompKinematicCharField = typeof(CharacterComponent).GetField("KinematicCharacter", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var bulletKinChar = charCompKinematicCharField.GetValue(characterComponent);
            var bulletCharControllerType = bulletKinChar.GetType();

            var bulletKinematicCharVerticalVelocityField = bulletCharControllerType.GetField("m_verticalVelocity", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            // Push the method's arg0 (characterComp)
            generator.Emit(OpCodes.Ldarg_0);
            // Read arg0.field (characterComp.KinematicCharacter)
            generator.Emit(OpCodes.Ldfld, charCompKinematicCharField);

            // Push the method's arg1 (linearVelocityY)
            generator.Emit(OpCodes.Ldarg_1);

            // Call characterComp.KinematicCharacter.m_verticalVelocity = linearVelocityY;
            generator.Emit(OpCodes.Stfld, bulletKinematicCharVerticalVelocityField);

            // void return
            generator.Emit(OpCodes.Ret);

            return (Action<CharacterComponent, float>)simulateMethod.CreateDelegate(typeof(Action<CharacterComponent, float>));
        }

        internal static void SimulateCharacter(CharacterComponent characterComponent, Simulation simulation, float deltaTimeInSeconds)
        {
            _simulateCharacterComponentFunction ??= CreateSimulateCharacterComponentFunction(characterComponent);
            _updateTransformFromPhysicsComponentFunction ??= CreateUpdateTransformFromPhysicsComponentFunction();

            _simulateCharacterComponentFunction(characterComponent, simulation, deltaTimeInSeconds);

            var physicsObjectWorldMatrix = Matrix.RotationQuaternion(characterComponent.Orientation) * characterComponent.PhysicsWorldTransform;
            _updateTransformFromPhysicsComponentFunction(characterComponent, ref physicsObjectWorldMatrix);
        }

        internal static Vector3 GetLinearVelocity(CharacterComponent characterComponent)
        {
            _getLinearVelocityFunction ??= CreateGetLinearVelocityFunction(characterComponent);
            var vec3 = _getLinearVelocityFunction(characterComponent);
            return vec3;
        }

        internal static void SetLinearVelocity(CharacterComponent characterComponent, ref Vector3 linearVelocity)
        {
            if (linearVelocity.Y == 0)
            {
                // TODO: m_verticalVelocity doesn't seem to be set properly. Not sure if this needs to be set all the time,
                // only when velocity is zero (for the time being only set when zero)
                _setLinearVelocityYFunction ??= CreateSetLinearVelocityYFunction(characterComponent);
                _setLinearVelocityYFunction(characterComponent, linearVelocity.Y);
            }
            _setLinearVelocityFunction ??= CreateSetLinearVelocityFunction(characterComponent);
            _setLinearVelocityFunction(characterComponent, linearVelocity);
        }
    }
}
