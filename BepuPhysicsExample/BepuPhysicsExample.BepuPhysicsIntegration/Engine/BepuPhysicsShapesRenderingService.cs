using BepuPhysicsExample.BepuPhysicsIntegration.Shapes;
using System;
using System.Collections.Generic;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Games;
using Xenko.Graphics;
using Xenko.Physics;
using Xenko.Rendering;

namespace BepuPhysicsExample.BepuPhysicsIntegration.Engine
{
    public class BepuPhysicsShapesRenderingService : GameSystem
    {
        private GraphicsDevice graphicsDevice;

        private enum ComponentType
        {
            Trigger,
            Static,
            Dynamic,
            Kinematic,
            Character,
        }

        private readonly Dictionary<ComponentType, Color> componentTypeColor = new Dictionary<ComponentType, Color>
        {
            { ComponentType.Trigger, Color.Purple },
            { ComponentType.Static, Color.Red },
            { ComponentType.Dynamic, Color.Green },
            { ComponentType.Kinematic, Color.Blue },
            { ComponentType.Character, Color.LightPink },
        };

        private readonly Dictionary<ComponentType, Material> componentTypeDefaultMaterial = new Dictionary<ComponentType, Material>();
        private readonly Dictionary<ComponentType, Material> componentTypeStaticPlaneMaterial = new Dictionary<ComponentType, Material>();
        private readonly Dictionary<ComponentType, Material> componentTypeHeightfieldMaterial = new Dictionary<ComponentType, Material>();

        private readonly Dictionary<Type, IDebugPrimitive> debugMeshCache = new Dictionary<Type, IDebugPrimitive>();
        private readonly Dictionary<BepuColliderShape, IDebugPrimitive> debugMeshCache2 = new Dictionary<BepuColliderShape, IDebugPrimitive>();
        private readonly Dictionary<BepuColliderShape, IDebugPrimitive> updatableDebugMeshCache = new Dictionary<BepuColliderShape, IDebugPrimitive>();

        private readonly Dictionary<BepuColliderShape, IDebugPrimitive> updatableDebugMeshes = new Dictionary<BepuColliderShape, IDebugPrimitive>();

        public override void Initialize()
        {
            graphicsDevice = Services.GetSafeServiceAs<IGraphicsDeviceService>().GraphicsDevice;

            foreach (var typeObject in Enum.GetValues(typeof(ComponentType)))
            {
                var type = (ComponentType)typeObject;
                componentTypeDefaultMaterial[type] = PhysicsDebugShapeMaterial.CreateDefault(graphicsDevice, Color.AdjustSaturation(componentTypeColor[type], 0.77f), 1);
                componentTypeStaticPlaneMaterial[type] = componentTypeDefaultMaterial[type];
                componentTypeHeightfieldMaterial[type] = PhysicsDebugShapeMaterial.CreateHeightfieldMaterial(graphicsDevice, Color.AdjustSaturation(componentTypeColor[type], 0.77f), 1);
                // TODO enable this once material is implemented.
                // ComponentTypeStaticPlaneMaterial[type] = PhysicsDebugShapeMaterial.CreateStaticPlane(graphicsDevice, Color.AdjustSaturation(ComponentTypeColor[type], 0.77f), 1);
            }
        }

        public override void Update(GameTime gameTime)
        {
            var unusedShapes = new List<BepuColliderShape>();
            foreach (var keyValuePair in updatableDebugMeshes)
            {
                if (keyValuePair.Value != null && keyValuePair.Key.DebugEntity?.Scene != null)
                {
                    keyValuePair.Key.UpdateDebugPrimitive(Game.GraphicsContext.CommandList, keyValuePair.Value);
                }
                else
                {
                    unusedShapes.Add(keyValuePair.Key);
                }
            }
            foreach (var shape in unusedShapes)
            {
                updatableDebugMeshes.Remove(shape);
            }
        }

        public BepuPhysicsShapesRenderingService(IServiceRegistry registry) : base(registry)
        {
        }

        public Entity CreateDebugEntity(BepuPhysicsComponent component, RenderGroup renderGroup, bool alwaysAddOffset = false)
        {
            if (component?.ColliderShape == null) return null;

            if (component.DebugEntity != null) return null;

            var debugEntity = new Entity();

            var skinnedElement = component as BepuPhysicsSkinnedComponentBase;
            if (skinnedElement != null && skinnedElement.BoneIndex != -1)
            {
                Vector3 scale, pos;
                Quaternion rot;
                skinnedElement.BoneWorldMatrixOut.Decompose(out scale, out rot, out pos);
                debugEntity.Transform.Position = pos;
                debugEntity.Transform.Rotation = rot;
            }
            else
            {
                Vector3 scale, pos;
                Quaternion rot;
                component.Entity.Transform.WorldMatrix.Decompose(out scale, out rot, out pos);
                debugEntity.Transform.Position = pos;
                debugEntity.Transform.Rotation = rot;
            }

            var shouldNotAddOffset = component is BepuRigidbodyComponent;//|| component is BepuCharacterComponent;

            //don't add offset for non bone dynamic and kinematic as it is added already in the updates
            var colliderEntity = CreateChildEntity(component, component.ColliderShape, renderGroup, alwaysAddOffset || !shouldNotAddOffset);
            if (colliderEntity != null) debugEntity.AddChild(colliderEntity);

            return debugEntity;
        }

        private Entity CreateChildEntity(BepuPhysicsComponent component, BepuColliderShape shape, RenderGroup renderGroup, bool addOffset)
        {
            if (shape == null)
                return null;

            switch (shape.Type)
            {
                case ColliderShapeTypes.Compound:
                    {
                        var entity = new Entity();

                        //We got to recurse
                        var compound = (BepuCompoundColliderShape)shape;
                        for (int i = 0; i < compound.Count; i++)
                        {
                            var subShape = compound[i];
                            var subEntity = CreateChildEntity(component, subShape, renderGroup, true); //always add offsets to compounds
                            if (subEntity != null)
                            {
                                entity.AddChild(subEntity);
                            }
                        }

                        entity.Transform.LocalMatrix = Matrix.Identity;
                        entity.Transform.UseTRS = false;

                        compound.DebugEntity = entity;

                        return entity;
                    }
                case ColliderShapeTypes.Box:
                case ColliderShapeTypes.Capsule:
                case ColliderShapeTypes.ConvexHull:
                case ColliderShapeTypes.Cylinder:
                case ColliderShapeTypes.Sphere:
                case ColliderShapeTypes.Cone:
                case ColliderShapeTypes.StaticPlane:
                case ColliderShapeTypes.StaticMesh:
                case ColliderShapeTypes.Heightfield:
                    {
                        IDebugPrimitive debugPrimitive;
                        var type = shape.GetType();
                        //if (type == typeof(BepuHeightfieldColliderShape))
                        //{
                        //    if (!updatableDebugMeshCache.TryGetValue(shape, out debugPrimitive))
                        //    {
                        //        debugPrimitive = shape.CreateUpdatableDebugPrimitive(graphicsDevice);
                        //        updatableDebugMeshCache[shape] = debugPrimitive;
                        //    }
                        //    if (!updatableDebugMeshes.ContainsKey(shape))
                        //    {
                        //        updatableDebugMeshes.Add(shape, debugPrimitive);
                        //    }
                        //}
                        //else
                        if (type == typeof(BepuCapsuleColliderShape)
                            || type == typeof(BepuConvexHullColliderShape)
                            //|| type == typeof(BepuStaticMeshColliderShape)
                            )
                        {
                            if (!debugMeshCache2.TryGetValue(shape, out debugPrimitive))
                            {
                                debugPrimitive = new DebugPrimitive { shape.CreateDebugPrimitive(graphicsDevice) };
                                debugMeshCache2[shape] = debugPrimitive;
                            }
                        }
                        else
                        {
                            if (!debugMeshCache.TryGetValue(shape.GetType(), out debugPrimitive))
                            {
                                debugPrimitive = new DebugPrimitive { shape.CreateDebugPrimitive(graphicsDevice) };
                                debugMeshCache[shape.GetType()] = debugPrimitive;
                            }
                        }

                        var model = new Model
                        {
                            GetMaterial(component, shape),
                        };
                        foreach (var meshDraw in debugPrimitive.GetMeshDraws())
                        {
                            model.Add(new Mesh { Draw = meshDraw });
                        }

                        var entity = new Entity
                        {
                            new ModelComponent
                            {
                                Model = model,
                                RenderGroup = renderGroup,
                            },
                        };

                        var offset = addOffset ? Matrix.RotationQuaternion(shape.LocalRotation) * Matrix.Translation(shape.LocalOffset) : Matrix.Identity;

                        entity.Transform.LocalMatrix = shape.DebugPrimitiveMatrix * offset * Matrix.Scaling(shape.Scaling);

                        entity.Transform.UseTRS = false;

                        shape.DebugEntity = entity;

                        return entity;
                    }
                default:
                    return null;
            }
        }

        private Material GetMaterial(EntityComponent component, BepuColliderShape shape)
        {
            var componentType = ComponentType.Trigger;

            var rigidbodyComponent = component as BepuRigidbodyComponent;
            if (rigidbodyComponent != null)
            {
                componentType = rigidbodyComponent.IsTrigger ? ComponentType.Trigger :
                    rigidbodyComponent.IsKinematic ? ComponentType.Kinematic : ComponentType.Dynamic;
            }
            //else if (component is BepuCharacterComponent)
            //{
            //    componentType = ComponentType.Character;
            //}
            else if (component is BepuStaticColliderComponent staticCollider)
            {
                componentType = staticCollider.IsTrigger ? ComponentType.Trigger : ComponentType.Static;
            }

            if (shape is BepuStaticPlaneColliderShape)
            {
                return componentTypeStaticPlaneMaterial[componentType];
            }
            //else if (shape is BepuHeightfieldColliderShape)
            //{
            //    return componentTypeHeightfieldMaterial[componentType];
            //}
            else
            {
                return componentTypeDefaultMaterial[componentType];
            }
        }
    }
}
