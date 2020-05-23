using BepuPhysics.Collidables;
using BepuPhysicsExample.BepuPhysicsIntegration.Engine;
using System.Collections.Generic;
using Xenko.Core;
using Xenko.Core.Diagnostics;
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Games;
using Xenko.Graphics;
using Xenko.Physics;
using Xenko.Rendering;

namespace BepuPhysicsExample.BepuPhysicsIntegration
{
    public class BepuPhysicsProcessor : EntityProcessor<BepuPhysicsComponent, BepuPhysicsProcessor.AssociatedData>
    {
        public class AssociatedData
        {
            public BepuPhysicsComponent PhysicsComponent;
            public TransformComponent TransformComponent;
            public ModelComponent ModelComponent; //not mandatory, could be null e.g. invisible triggers
            public bool BoneMatricesUpdated;
        }

        private readonly List<BepuPhysicsComponent> elements = new List<BepuPhysicsComponent>();
        private readonly List<BepuPhysicsSkinnedComponentBase> boneElements = new List<BepuPhysicsSkinnedComponentBase>();
        private readonly List<BepuRigidbodyComponent> rigidBodies = new List<BepuRigidbodyComponent>();
        //private readonly List<BepuCharacterComponent> characters = new List<BepuCharacterComponent>();

        private Bepu2PhysicsSystem physicsSystem;
        private SceneSystem sceneSystem;
        private Scene debugScene;

        private bool colliderShapesRendering;

        private BepuPhysicsShapesRenderingService debugShapeRendering;

        public BepuPhysicsProcessor()
            : base(typeof(TransformComponent))
        {
            Order = 0xFFFF;
        }

        public BepuSimulation Simulation { get; private set; }

        internal void RenderColliderShapes(bool enabled)
        {
            debugShapeRendering.Enabled = enabled;

            colliderShapesRendering = enabled;

            if (!colliderShapesRendering)
            {
                if (debugScene != null)
                {
                    debugScene.Dispose();

                    foreach (var element in elements)
                    {
                        element.RemoveDebugEntity(debugScene);
                    }

                    sceneSystem.SceneInstance.RootScene.Children.Remove(debugScene);
                }
            }
            else
            {
                debugScene = new Scene();

                foreach (var element in elements)
                {
                    if (element.Enabled)
                    {
                        element.AddDebugEntity(debugScene, Simulation.ColliderShapesRenderGroup);
                    }
                }

                sceneSystem.SceneInstance.RootScene.Children.Add(debugScene);
            }
        }

        protected override AssociatedData GenerateComponentData(Entity entity, BepuPhysicsComponent component)
        {
            var data = new AssociatedData
            {
                PhysicsComponent = component,
                TransformComponent = entity.Transform,
                ModelComponent = entity.Get<ModelComponent>(),
            };

            data.PhysicsComponent.Simulation = Simulation;
            data.PhysicsComponent.DebugShapeRendering = debugShapeRendering;

            return data;
        }

        protected override bool IsAssociatedDataValid(Entity entity, BepuPhysicsComponent physicsComponent, AssociatedData associatedData)
        {
            return
                physicsComponent == associatedData.PhysicsComponent &&
                entity.Transform == associatedData.TransformComponent &&
                entity.Get<ModelComponent>() == associatedData.ModelComponent;
        }

        protected override void OnEntityComponentAdding(Entity entity, BepuPhysicsComponent component, AssociatedData data)
        {
            component.Attach(data);

            //var character = component as BepuCharacterComponent;
            //if (character != null)
            //{
            //    characters.Add(character);
            //}

            if (colliderShapesRendering)
            {
                component.AddDebugEntity(debugScene, Simulation.ColliderShapesRenderGroup);
            }

            elements.Add(component);

            if (component.BoneIndex != -1)
            {
                boneElements.Add((BepuPhysicsSkinnedComponentBase)component);
            }
            if (component is BepuRigidbodyComponent rigidbodyComp)
            {
                rigidBodies.Add(rigidbodyComp);
            }
        }

        private void ComponentRemoval(BepuPhysicsComponent component)
        {
            Simulation.CleanContacts(component);

            if (component.BoneIndex != -1)
            {
                boneElements.Remove((BepuPhysicsSkinnedComponentBase)component);
            }
            if (component is BepuRigidbodyComponent rigidbodyComp)
            {
                rigidBodies.Remove(rigidbodyComp);
            }

            elements.Remove(component);

            if (colliderShapesRendering)
            {
                component.RemoveDebugEntity(debugScene);
            }

            //var character = component as BepuCharacterComponent;
            //if (character != null)
            //{
            //    characters.Remove(character);
            //}

            component.Detach();
        }

        private readonly List<BepuPhysicsComponent> currentFrameRemovals = new List<BepuPhysicsComponent>();

        protected override void OnEntityComponentRemoved(Entity entity, BepuPhysicsComponent component, AssociatedData data)
        {
            currentFrameRemovals.Add(component);
        }

        protected override void OnSystemAdd()
        {
            physicsSystem = (Bepu2PhysicsSystem)Services.GetService<IBepuPhysicsSystem>();
            if (physicsSystem == null)
            {
                physicsSystem = new Bepu2PhysicsSystem(Services);
                Services.AddService<IBepuPhysicsSystem>(physicsSystem);
                var gameSystems = Services.GetSafeServiceAs<IGameSystemCollection>();
                gameSystems.Add(physicsSystem);
            }

            ((IReferencable)physicsSystem).AddReference();

            // Check if PhysicsShapesRenderingService is created (and check if rendering is enabled with IGraphicsDeviceService)
            if (Services.GetService<IGraphicsDeviceService>() != null && Services.GetService<BepuPhysicsShapesRenderingService>() == null)
            {
                debugShapeRendering = new BepuPhysicsShapesRenderingService(Services);
                var gameSystems = Services.GetSafeServiceAs<IGameSystemCollection>();
                gameSystems.Add(debugShapeRendering);
            }

            Simulation = physicsSystem.Create(this);

            sceneSystem = Services.GetSafeServiceAs<SceneSystem>();
        }

        protected override void OnSystemRemove()
        {
            physicsSystem.Release(this);
            ((IReferencable)physicsSystem).Release();
        }

        internal void UpdateRigidBodies()
        {
            //var charactersProfilingState = Profiler.Begin(PhysicsProfilingKeys.CharactersProfilingKey);
            //int activeCharacters = 0;
            ref var activeBodies = ref Simulation.NativeBepuSimulation.Bodies.ActiveSet;
            for (int i = 0; i < activeBodies.Count; i++)
            {
                var handleId = activeBodies.IndexToHandle[i];
                Simulation.UpdateRigidbodyPosition(handleId);
            }
            ////foreach (var element in rigidBodies)
            ////{
            ////    if (!element.Enabled || element.ColliderShape == null) continue;

            ////    //if (element.IsKinematic)
            ////    //{
            ////    //    // We set the physics
            ////    //    element.RigidBodyGetWorldTransform(out var worldTransform);
            ////    //    worldTransform.Decompose(out _, out Quaternion rot, out var pos);
            ////    //} else {
            ////    var worldTransform = element.PhysicsWorldTransform;
            ////    element.RigidBodySetWorldTransform(ref worldTransform);

            ////    if (element.DebugEntity != null)
            ////    {
            ////        Vector3 pos;
            ////        Quaternion rot;
            ////        worldTransform.Decompose(out _, out rot, out pos);
            ////        element.DebugEntity.Transform.Position = pos;
            ////        element.DebugEntity.Transform.Rotation = rot;
            ////    }
            ////    //}

            ////    //charactersProfilingState.Mark();
            ////    //activeCharacters++;
            ////}
            //charactersProfilingState.End("Active characters: {0}", activeCharacters);
        }

        internal void UpdateCharacters()
        {
            //var charactersProfilingState = Profiler.Begin(PhysicsProfilingKeys.CharactersProfilingKey);
            //var activeCharacters = 0;
            //// Characters need manual updating
            //foreach (var element in characters)
            //{
            //    if (!element.Enabled || element.ColliderShape == null) continue;

            //    var worldTransform = Matrix.RotationQuaternion(element.Orientation) * element.PhysicsWorldTransform;
            //    element.UpdateTransformationComponent(ref worldTransform);

            //    if (element.DebugEntity != null)
            //    {
            //        Vector3 scale, pos;
            //        Quaternion rot;
            //        worldTransform.Decompose(out scale, out rot, out pos);
            //        element.DebugEntity.Transform.Position = pos;
            //        element.DebugEntity.Transform.Rotation = rot;
            //    }

            //    charactersProfilingState.Mark();
            //    activeCharacters++;
            //}
            //charactersProfilingState.End("Active characters: {0}", activeCharacters);
        }

        public override void Draw(RenderContext context)
        {
            if (BepuSimulation.DisableSimulation) return;

            foreach (var element in boneElements)
            {
                element.UpdateDraw();
            }
        }

        internal void UpdateBones()
        {
            foreach (var element in boneElements)
            {
                element.UpdateBones();
            }
        }

        public void UpdateContacts()
        {
            foreach (var dataPair in ComponentDatas)
            {
                var data = dataPair.Value;
                var shouldProcess = data.PhysicsComponent.ProcessCollisions || ((data.PhysicsComponent as BepuPhysicsTriggerComponentBase)?.IsTrigger ?? false);
                if (data.PhysicsComponent.Enabled && shouldProcess && data.PhysicsComponent.ColliderShape != null)
                {
                    Simulation.ContactTest(data.PhysicsComponent);
                }
            }
        }

        public void UpdateRemovals()
        {
            foreach (var currentFrameRemoval in currentFrameRemovals)
            {
                ComponentRemoval(currentFrameRemoval);
            }

            currentFrameRemovals.Clear();
        }
    }
}
