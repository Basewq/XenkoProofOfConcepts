using SceneEditorExtensionExample.Rendering;
using SceneEditorExtensionExample.SharedData;
using Stride.Core;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;

namespace SceneEditorExtensionExample.WorldTerrain.Foliage;

/// <summary>
/// Foliage Manager for a given <see cref="SharedData.FoliagePlacement"/> asset, which manages
/// the rendering instancing dividing it into chunks.
/// </summary>
[ComponentCategory("Environment")]
[DataContract]
[DefaultEntityComponentProcessor(typeof(FoliageInstancingManagerProcessor), ExecutionMode = ExecutionMode.Runtime | ExecutionMode.Editor)]
public class FoliageInstancingManagerComponent : EntityComponent
{
    private GraphicsContext _graphicsContext;
    private GraphicsDevice _graphicsDevice;
    private ContentManager _contentManager;

    // The entire data from FoliagePlacement sorted into chunks
    private readonly Dictionary<Int3, List<FoliageChunkInstancingData>> _chunkIndexToFoliageInstancingDataList = new();
    // The visible chunks
    private Dictionary<FoliageChunkId, FoliageChunkInstancingComponent> _chunkIdToActiveChunkInstancingComponent = new();
    private Dictionary<FoliageChunkId, FoliageChunkInstancingComponent> _chunkIdToActiveChunkInstancingComponentProcessing = new();

    public UrlReference<FoliagePlacement> FoliagePlacement { get; set; }

    public Vector3 ChunkSize { get; set; } = new Vector3(32);

    /// <summary>
    /// The camera used to determine which chunks should be visible.
    /// </summary>
    public CameraComponent? CameraComponent { get; set; }
    public float MaxInstancingRenderDistance { get; set; } = 100;

    internal void Initialize(IServiceRegistry serviceRegistry)
    {
        var game = serviceRegistry.GetSafeServiceAs<IGame>();
        _graphicsContext = game.GraphicsContext;
        _graphicsDevice = game.GraphicsDevice;
        _contentManager = game.Content;

        if (Entity.EntityManager.ExecutionMode == ExecutionMode.Runtime)
        {
            // Only load the asset as-is when running as an app, the editor loads the meshes via FoliagePainterProcessor
            var foliagePlacement = _contentManager.Load(FoliagePlacement);
            var modelPlacementsSpan = CollectionsMarshal.AsSpan(foliagePlacement.ModelPlacements);
            SetInstancingModels(modelPlacementsSpan);
        }
    }

    internal void Deinitialize()
    {
        _graphicsDevice = null;

        _chunkIndexToFoliageInstancingDataList.Clear();
        _chunkIdToActiveChunkInstancingComponent.Clear();
        // Don't need to detach the child entities with FoliageChunkInstancingComponent because this entity is expected to be removed from the scene.
    }

    private static readonly Vector3[] FrustumPointsClipSpace = [
        // Far plane
        new Vector3(-1, +1, 1),      
        new Vector3(+1, +1, 1),
        new Vector3(-1, -1, 1),
        new Vector3(+1, -1, 1),
        // Near plane
        new Vector3(-1, +1, 0),
        new Vector3(+1, +1, 0),
        new Vector3(-1, -1, 0),
        new Vector3(+1, -1, 0),
    ];
    private readonly List<Int3> _visibleChunkIndexList = new();
    private readonly Vector3[] _frustumPointsWorldSpace = new Vector3[8];
    private readonly List<FoliageChunkId> _reusedChunkIds = new();
    internal void Update(GameTime time, CameraComponent overrideCameraComponent)
    {
        // Find the visible chunks
        _visibleChunkIndexList.Clear();
        var camComp = overrideCameraComponent ?? CameraComponent;
        if (camComp is null)
        {
            return;
        }
        // TODO Find a way to get all visible chunk indices based off frustum

        // Assume we're always using Perspective camera
        float fovRadians = MathUtil.DegreesToRadians(camComp.VerticalFieldOfView);
        float aspectRatio = camComp.AspectRatio;
        float zNear = camComp.NearClipPlane;
        float zFar = MaxInstancingRenderDistance;   // We use our own instead of camComp.FarClipPlane (which should be smaller) 

        Matrix.PerspectiveFovRH(fovRadians, aspectRatio, zNear, zFar, out var projMatrix);

        Matrix.Multiply(in camComp.ViewMatrix, in projMatrix, out var viewProjMatrix);
        var frustum = new BoundingFrustum(in viewProjMatrix);

        // Determine the AABB bounds of the frustum based off the corners of the frustum
        Matrix.Invert(in viewProjMatrix, out var viewProjInverseMatrix);
        for (int i = 0; i < FrustumPointsClipSpace.Length; i++)
        {
            var vec4 = Vector3.Transform(FrustumPointsClipSpace[i], viewProjInverseMatrix);
            _frustumPointsWorldSpace[i] = vec4.XYZ() / vec4.W;
        }
        BoundingBox.FromPoints(_frustumPointsWorldSpace, out var frustumBoundingBox);

        // Determine the chunks visible to the frustum
        var chunkIndexToPos = ChunkSize;
        var posToChunkIndex = 1f / chunkIndexToPos;
        var minIndex = MathExt.ToInt3Floor(frustumBoundingBox.Minimum * posToChunkIndex);
        var maxIndex = MathExt.ToInt3Floor(frustumBoundingBox.Maximum * posToChunkIndex);
        for (int z = minIndex.Z; z <= maxIndex.Z; z++)
        {
            for (int x = minIndex.X; x <= maxIndex.X; x++)
            {
                for (int y = minIndex.Y; y <= maxIndex.Y; y++)
                {
                    var chunkIndex = new Int3(x, y, z);
                    var minChunkBoundsPos = MathExt.ToVec3(chunkIndex) * chunkIndexToPos;
                    var maxChunkBoundsPos = minChunkBoundsPos + ChunkSize;
                    var chunkBoundingBox = new BoundingBoxExt(minChunkBoundsPos, maxChunkBoundsPos);
                    if (frustum.Contains(in chunkBoundingBox))
                    {
                        _visibleChunkIndexList.Add(chunkIndex);
                    }
                }
            }
        }

        // From the visible chunk index list, we go through _chunkIdToFoliageInstancingDataList and find which chunks we need to render.        
        // For performance reasons, we use try to reuse existing 'active' chunks where possible.

        // Swap the "actual" instancing dictionary with the "processing" dictionary, so that the "actual" instancing dictionary is
        // initially empty and the "processing" dictionary contains any chunks we can potentially reuse.
        Utilities.Swap(ref _chunkIdToActiveChunkInstancingComponent, ref _chunkIdToActiveChunkInstancingComponentProcessing);

        foreach (var visibleChunkIndex in _visibleChunkIndexList)
        {
            foreach (var kv in _chunkIndexToFoliageInstancingDataList)
            {
                var instancingChunkIndex = kv.Key;
                if (visibleChunkIndex != instancingChunkIndex)
                {
                    continue;       // Not visible
                }
                var instancingDataList = kv.Value;
                foreach (var instancingData in instancingDataList)
                {
                    var chunkId = instancingData.ChunkId;
                    // Reuse existing chunkInstancingComponent if possible
                    bool isBufferUpdateRequired = instancingData.IsDataUpdateRequired;
                    if (_chunkIdToActiveChunkInstancingComponentProcessing.TryGetValue(chunkId, out var chunkInstancingComponent))
                    {
                        _reusedChunkIds.Add(chunkId);
                    }
                    else
                    {
                        bool wasCreated = TryCreateActiveChunkInstancingComponent(chunkId, instancingData.ModelUrl, out chunkInstancingComponent);
                        isBufferUpdateRequired = wasCreated;      // New chunk so must always update buffer
                    }
                    if (chunkInstancingComponent is null)
                    {
                        continue;
                    }
                    _chunkIdToActiveChunkInstancingComponent[chunkId] = chunkInstancingComponent;
                    if (isBufferUpdateRequired)
                    {
                        SetActiveInstancingData(instancingData, chunkInstancingComponent);
                        instancingData.IsDataUpdateRequired = false;
                    }
                }
            }
        }

        foreach (var chunkId in _reusedChunkIds)
        {
            _chunkIdToActiveChunkInstancingComponentProcessing.Remove(chunkId);
        }
        _reusedChunkIds.Clear();

        // Any remaining chunks are no longer visible and should be removed
        foreach (var kv in _chunkIdToActiveChunkInstancingComponentProcessing)
        {
            var chunkInstancingComponent = kv.Value;
            chunkInstancingComponent.Entity.Scene = null;

        }
        _chunkIdToActiveChunkInstancingComponentProcessing.Clear();
    }

    private List<Int3> _pendingRemoveInstancingDataList = new();
    /// <summary>
    /// Clears the previous instancing data and sets the new instancing data based on <paramref name="modelPlacements"/>.
    /// </summary>
    public void SetInstancingModels(Span<ModelPlacement> modelPlacements)
    {
        // Clear old data
        foreach (var kv in _chunkIndexToFoliageInstancingDataList)
        {
            var instancingDataList = kv.Value;
            foreach (var instancingData in instancingDataList)
            {
                instancingData.InstanceWorldTransformList.Clear();
                instancingData.InstanceDataList.Clear();
            }
        }

        // Populate the new data
        var posToChunkIndex = 1f / ChunkSize;
        foreach (var modelPlacement in modelPlacements)
        {
            var chunkIndexVec = modelPlacement.Position * posToChunkIndex;
            var chunkIndex = MathExt.ToInt3Floor(chunkIndexVec);
            AddModelInstanceData(modelPlacement, chunkIndex);
        }

        // Remove all empty chunks/data list
        foreach (var kv in _chunkIndexToFoliageInstancingDataList)
        {
            var chunkIndex = kv.Key;
            var instancingDataList = kv.Value;
            instancingDataList.RemoveAll(x => x.InstanceDataList.Count == 0);
            if (instancingDataList.Count == 0)
            {
                _pendingRemoveInstancingDataList.Add(chunkIndex);
            }
        }
        foreach (var chunkIndex in _pendingRemoveInstancingDataList)
        {
            _chunkIndexToFoliageInstancingDataList.Remove(chunkIndex);
        }
        _pendingRemoveInstancingDataList.Clear();
    }

    private bool TryCreateActiveChunkInstancingComponent(FoliageChunkId chunkId, string modelUrl, [NotNullWhen(true)] out FoliageChunkInstancingComponent? chunkInstancingComponent)
    {
        var instancingEntity = new Entity();
        var model = _contentManager.Load<Model>(modelUrl);
        if (model is null)
        {
            // The editor can sometimes bug out and not load the model...
            //throw new ApplicationException($"Failed to load model: {modelUrl.Url}");
            chunkInstancingComponent = null;
            return false;
        }
        var modelComp = new ModelComponent
        {
            Model = model,
            IsShadowCaster = model.Materials.FirstOrDefault()?.IsShadowCaster ?? false,
        };
        //// Because we divide instancing into chunks we need to clone the material
        //// so each model chunk have separate instancing data.
        //var material = modelComp.Model.Materials.FirstOrDefault();
        //if (material is not null)
        //{
        //    // Make a clone of the material
        //    var newMaterial = CloneMaterial(material.Material);
        //    modelComp.Materials.Add(key: 0, newMaterial);
        //}

        instancingEntity.Add(modelComp);
        var instancingArray = new InstancingUserArray();
        var instComp = new InstancingComponent
        {
            Type = instancingArray
        };
        instancingEntity.Add(instComp);

        chunkInstancingComponent = new FoliageChunkInstancingComponent
        {
            ChunkId = chunkId,
            ModelComponent = modelComp,
            InstancingArray = instancingArray,
            InstanceDataBuffer = null
        };
        instancingEntity.Add(chunkInstancingComponent);
        return true;
    }

    //private static Material CloneMaterial(Material sourceMaterial)
    //{
    //    var destMaterial = new Material();
    //    foreach (var pass in sourceMaterial.Passes)
    //    {
    //        var newPass = new MaterialPass()
    //        {
    //            CullMode = pass.CullMode,
    //            BlendState = pass.BlendState,
    //            TessellationMethod = pass.TessellationMethod,
    //            HasTransparency = pass.HasTransparency,
    //            AlphaToCoverage = pass.AlphaToCoverage,
    //            IsLightDependent = pass.IsLightDependent,
    //            PassIndex = pass.PassIndex,
    //            Parameters = new ParameterCollection(pass.Parameters)
    //        };
    //        destMaterial.Passes.Add(newPass);
    //    }
    //    return destMaterial;
    //}

    private void AddModelInstanceData(ModelPlacement modelPlacement, Int3 chunkIndex)
    {
        string modelUrl = modelPlacement.ModelUrl.Url;
        if (string.IsNullOrEmpty(modelUrl))
        {
            // The editor can bug out sometimes with the UrlReference...just skip, otherwise it'll crash the app.
#if GAME_EDITOR
            // HACK: When changing the model to paint with, the Editor sometimes creates an 'empty' UrlReference
            // which is actually just a proxy object
            var modelUrlAttachedRef = AttachedReferenceManager.GetAttachedReference(modelPlacement.ModelUrl);
            modelUrl = modelUrlAttachedRef.Url;
            if (string.IsNullOrEmpty(modelUrl))
            {
                return;     // Unknown issue...just skip, otherwise it'll crash the editor.
            }
#else
            Debug.Fail("modelPlacement.ModelUrl was empty.");
            return;     // Unknown issue...just skip, otherwise it'll crash the app.
#endif
        }
        var chunkId = new FoliageChunkId(chunkIndex, modelUrl);
        var instancingDataList = _chunkIndexToFoliageInstancingDataList.GetOrCreateValue(chunkIndex, _ => new());
        var instancingData = instancingDataList.FirstOrDefault(x => x.ChunkId == chunkId);
        if (instancingData is null)
        {
            instancingData = new FoliageChunkInstancingData(chunkId, modelUrl);
            instancingDataList.Add(instancingData);
        }
        AddFoliageInstanceData(modelPlacement, instancingData);
        instancingData.IsDataUpdateRequired = true;
}

    private static void AddFoliageInstanceData(ModelPlacement modelPlacement, FoliageChunkInstancingData instancingData)
    {
        var scale = modelPlacement.Scale;
        var orientation = modelPlacement.Orientation;
        var pos = modelPlacement.Position;

        Matrix.Transformation(in scale, in orientation, in pos, out var worldTransform);
        instancingData.InstanceWorldTransformList.Add(worldTransform);

        var instData = new FoliageInstanceData
        {
            SurfaceNormalModelSpace = modelPlacement.SurfaceNormalModelSpace,
        };
        instancingData.InstanceDataList.Add(instData);
    }

    private List<Matrix> _modelInstanceWorldTransformsCache = new(capacity: 32);    // Not thread safe!
    private void SetActiveInstancingData(FoliageChunkInstancingData instancingData, FoliageChunkInstancingComponent chunkInstancingComponent)
    {
        // Update instancing transform matrix array
        var instanceWorldTransformSpan = CollectionsMarshal.AsSpan(instancingData.InstanceWorldTransformList);
        for (int i = 0; i < instanceWorldTransformSpan.Length; i++)
        {
            ref var transformMatrix = ref instanceWorldTransformSpan[i];
            _modelInstanceWorldTransformsCache.Add(transformMatrix);
        }
        chunkInstancingComponent.InstancingArray.UpdateWorldMatrices(_modelInstanceWorldTransformsCache.ToArray(), _modelInstanceWorldTransformsCache.Count);
        _modelInstanceWorldTransformsCache.Clear();

        // Update instancing custom data
        if (chunkInstancingComponent.InstanceDataBuffer is null || chunkInstancingComponent.InstanceDataBuffer.ElementCount < instancingData.InstanceDataList.Count)
        {
            // Create buffer or recreate to fit new data size
            chunkInstancingComponent.InstanceDataBuffer?.Dispose();
            chunkInstancingComponent.InstanceDataBuffer = _graphicsDevice.CreateShaderBuffer<FoliageInstanceData>(instancingData.InstanceDataList.Count);
        }
        chunkInstancingComponent.InstanceDataBuffer.SetData(_graphicsContext.CommandList, instancingData.InstanceDataList.ToArray());

        // If this was a new entity, ensure it is attached to our entity/scene
        if (chunkInstancingComponent.Entity.Scene is null)
        {
            chunkInstancingComponent.Entity.SetParent(Entity);
        }
    }

    public void AppendInstancingModels(Span<ModelPlacement> modelPlacements)
    {
        // Populate the new data
        var posToChunkIndex = 1f / ChunkSize;
        foreach (var modelPlacement in modelPlacements)
        {
            var chunkIndexVec = modelPlacement.Position * posToChunkIndex;
            var chunkIndex = MathExt.ToInt3Floor(chunkIndexVec);
            AddModelInstanceData(modelPlacement, chunkIndex);
        }
    }

    private class FoliageChunkInstancingData
    {
        public readonly FoliageChunkId ChunkId;
        public readonly string ModelUrl;        // The editor sometimes loses UrlReference<Model>, so just store the string
        public readonly List<Matrix> InstanceWorldTransformList;
        public readonly List<FoliageInstanceData> InstanceDataList;
        public bool IsDataUpdateRequired = true;

        public FoliageChunkInstancingData(FoliageChunkId chunkId, string modelUrl)
        {
            ChunkId = chunkId;
            ModelUrl = modelUrl;
            InstanceWorldTransformList = new(capacity: 32);
            InstanceDataList = new(capacity: 32);
        }
    }
}
