#if GAME_EDITOR
using SceneEditorExtensionExample.SharedData;
using SceneEditorExtensionExample.StrideAssetExt.Assets;
using SceneEditorExtensionExample.StrideEditorExt;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Game;
using Stride.Assets.Presentation.AssetEditors.SceneEditor.Game;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Dirtiables;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Editor.EditorGame.Game;
using Stride.Engine;
using Stride.Engine.Processors;
using Stride.Games;
using Stride.Input;
using Stride.Rendering;
using Stride.Rendering.Lights;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SceneEditorExtensionExample.WorldTerrain.Foliage.Editor;

class FoliagePainterProcessor : EntityProcessor<FoliagePainterComponent, FoliagePainterProcessor.AssociatedData>
{
    const int VerticesPerTriangle = 3;

    private Random _random = new(Seed: 1000);

    private ContentManager _contentManager = default!;
    private InputManager _inputManager = default!;
    private SceneEditorGame _sceneEditorGame;
    private IStrideEditorService _strideEditorService;
    private FoliageMeshManager _foliageMeshManager;
    private Task _foliageMeshManagerInitializeTask;
    private FoliagePainterEditorMouseService _painterMouseService;

    private bool _isInstancingRenderFeatureCheckRequired = true;

    protected override void OnSystemAdd()
    {
        _contentManager = Services.GetSafeServiceAs<ContentManager>();
        _inputManager = Services.GetSafeServiceAs<InputManager>();
        _sceneEditorGame = Services.GetService<IGame>() as SceneEditorGame;

        _strideEditorService = Services.GetService<IStrideEditorService>();
        _foliageMeshManager = new FoliageMeshManager(_strideEditorService);
        _foliageMeshManagerInitializeTask =_foliageMeshManager.Initialize();

        _painterMouseService = _sceneEditorGame.EditorServices.Get<FoliagePainterEditorMouseService>();
        if (_painterMouseService is null)
        {
            Debug.WriteLine("GrassPainterEditorMouseService added.");

            _painterMouseService = new();

            // HACK: Every EditorGameMouseServiceBase derived classes hold a LOCAL copy of
            // every other mouse service instead of reading from some common registry...
            // This code manually goes through every other mouse service and add our one in.
            var mouseServiceType = typeof(EditorGameMouseServiceBase);
            var mouseSvceListFieldInfo = mouseServiceType.GetField("mouseServices", BindingFlags.Instance | BindingFlags.NonPublic);

            foreach (var editorService in _sceneEditorGame.EditorServices.Services)
            {
                if (editorService is not EditorGameMouseServiceBase mouseSvce)
                {
                    continue;
                }
                var mouseSvceList = mouseSvceListFieldInfo.GetValue(mouseSvce) as List<IEditorGameMouseService>;
                if (mouseSvceList is not null)
                {
                    Debug.WriteLine($"Found mouse services: {mouseSvceList.Count}");
                    mouseSvceList.Add(_painterMouseService);
                }
            }

            _painterMouseService.InitializeService(_sceneEditorGame);

            var editorServiceType = typeof(EditorGameServiceBase);
            var editorSvceRegisterMouseServicesMethodInfo = mouseServiceType.GetMethod("RegisterMouseServices", BindingFlags.Instance | BindingFlags.NonPublic);
            EditorGameServiceRegistry serviceRegistry = _sceneEditorGame.EditorServices;
            editorSvceRegisterMouseServicesMethodInfo?.Invoke(_painterMouseService, [serviceRegistry]);
            _sceneEditorGame.EditorServices.Add(_painterMouseService);
        }
        else
        {
            Debug.WriteLine("GrassPainterEditorMouseService already registered.");
        }

        var selectionService = _sceneEditorGame.EditorServices.Get<IEditorGameEntitySelectionService>();
        if (selectionService is not null)
        {
            selectionService.SelectionUpdated += EntitySelectionService_OnSelectionUpdated;
        }
    }

    private void EntitySelectionService_OnSelectionUpdated(object sender, EntitySelectionEventArgs e)
    {
        // Stop painting if the user changed entity selection
        _strideEditorService.Invoke(() =>
        {
            var deselectedPainters = ComponentDatas
                                        .Select(x => x.Key)
                                        .Where(painterComp => !e.NewSelection.Any(selectedEntity => selectedEntity.Id == painterComp.Entity.Id))    // All painters that are not selected
                                        .Where(painterComp => painterComp.PaintMode != FoliagePlacementPaintMode.Disabled)
                                        .ToList();
            if (deselectedPainters.Count > 0)
            {
                using var undoRedoTransaction = _strideEditorService.CreateUndoRedoTransaction("Painter Entity Deselected - Disable Painters");
                foreach (var painterComp in deselectedPainters)
                {
                    _strideEditorService.UpdateAssetComponentData(painterComp, propertyName: nameof(FoliagePainterComponent.PaintMode), FoliagePlacementPaintMode.Disabled);
                }
            }
        });
    }

    protected override void OnSystemRemove()
    {
        var selectionService = _sceneEditorGame.EditorServices.Get<IEditorGameEntitySelectionService>();
        if (selectionService is not null)
        {
            selectionService.SelectionUpdated -= EntitySelectionService_OnSelectionUpdated;
        }
        
        var _ = _foliageMeshManager.DisposeAsync();
    }

    protected override AssociatedData GenerateComponentData([NotNull] Entity entity, [NotNull] FoliagePainterComponent component)
    {
        var foliageInstancingManagerEntity = new Entity(name: "EditorFoliageInstancingManager");
        var foliageInstMgrComp = new FoliageInstancingManagerComponent();
        foliageInstancingManagerEntity.Add(foliageInstMgrComp);
        foliageInstancingManagerEntity.Scene = entity.Scene;

        var pendingFoliageInstancingManagerEntity = new Entity(name: "EditorPendingFoliageInstancingManager");
        var pendingFoliageInstMgrComp = new FoliageInstancingManagerComponent();
        pendingFoliageInstancingManagerEntity.Add(pendingFoliageInstMgrComp);
        pendingFoliageInstancingManagerEntity.Scene = entity.Scene;

        return new AssociatedData
        {
            FoliageInstancingEntity = foliageInstancingManagerEntity,
            FoliageInstancingManagerComponent = foliageInstMgrComp,

            PendingFoliageInstancingEntity = pendingFoliageInstancingManagerEntity,
            PendingFoliageInstancingManagerComponent = pendingFoliageInstMgrComp
        };
    }

    protected override void OnEntityComponentAdding(Entity entity, [NotNull] FoliagePainterComponent component, [NotNull] AssociatedData data)
    {
        component.PropertyChanged += OnPainterComponentPropertyChanged;
        component.PaintMode = FoliagePlacementPaintMode.Disabled;
    }

    private void OnPainterComponentPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs ev)
    {
        if (sender is not FoliagePainterComponent painterComp)
        {
            return;
        }
        switch (ev.PropertyName)
        {
            case nameof(FoliagePainterComponent.FoliagePlacementAsset):
                var foliagePlacementAsset = painterComp.GetFoliagePlacementInternalAsset();
                if (foliagePlacementAsset is not null
                    && ComponentDatas.TryGetValue(painterComp, out var data)
                    && data.IsInitialInstancingDisplayed)   // If it's still being initialized we ignore property changes
                {
                    var modelPlacementsSpan = CollectionsMarshal.AsSpan(foliagePlacementAsset.ModelPlacements);
                    data.FoliageInstancingManagerComponent.SetInstancingModels(modelPlacementsSpan);
                }
                break;
        }
    }

    protected override void OnEntityComponentRemoved(Entity entity, [NotNull] FoliagePainterComponent component, [NotNull] AssociatedData data)
    {
        component.PropertyChanged -= OnPainterComponentPropertyChanged;

        if (data.PaintPreviewEntity is not null)
        {
            data.PaintPreviewEntity.Scene = null;
        }

        if (data.FoliageInstancingEntity is not null)
        {
            data.FoliageInstancingEntity.Scene = null;
        }
    }

    public override void Update(GameTime time)
    {
        if (_sceneEditorGame is null)
        {
            return;
        }
        if (_isInstancingRenderFeatureCheckRequired)
        {
            var meshRenderFeature = _sceneEditorGame.SceneSystem.GraphicsCompositor.RenderFeatures.OfType<MeshRenderFeature>().FirstOrDefault();
            if (meshRenderFeature is not null)
            {
                if (!meshRenderFeature.RenderFeatures.Any(x => x is InstancingRenderFeature))
                {
                    var instRendFeature = new InstancingRenderFeature();
                    if (meshRenderFeature.RenderFeatures.TryFindIndex(x => x is ForwardLightingRenderFeature, out int insertIndex))
                    {
                        meshRenderFeature.RenderFeatures.Insert(insertIndex + 1, instRendFeature);
                    }
                    else
                    {
                        meshRenderFeature.RenderFeatures.Add(instRendFeature);
                    }
                }
                _isInstancingRenderFeatureCheckRequired = false;
            }
        }
        foreach (var kv in ComponentDatas)
        {
            var painterComp = kv.Key;
            var data = kv.Value;
            switch (painterComp.PaintMode)
            {
                case FoliagePlacementPaintMode.Disabled:
                    if (data.PaintPreviewModelComponent is not null && data.PaintPreviewModelComponent.Enabled)
                    {
                        data.PaintPreviewModelComponent.Enabled = false;
                    }
                    break;

                case FoliagePlacementPaintMode.Paint:
                case FoliagePlacementPaintMode.Erase:
                    if (data.PaintPreviewEntity is null && painterComp.PaintPlacementPreviewPrefabUrl is not null)
                    {
                        var prefab = _contentManager.Load(painterComp.PaintPlacementPreviewPrefabUrl);
                        data.PaintPreviewEntity = prefab.Instantiate().First();
                        var modelComp = data.PaintPreviewEntity.Get<ModelComponent>();
                        data.PaintPreviewModelComponent = modelComp;
                        data.PaintPreviewEntity.Scene = painterComp.Entity.Scene;
                    }
                    if (data.PaintPreviewModelComponent is not null && !data.PaintPreviewModelComponent.Enabled)
                    {
                        data.PaintPreviewModelComponent.Enabled = true;
                    }
                    break;
            }
            bool hasProcessed = ProcessPainter(painterComp, data);
            if (hasProcessed)
            {
                break;     // Only deal with one painterComp...
            }
        }
    }

    private ModelMeshData _modelMeshDataCache = new()
    {
        Positions = [],
        Normals = [],
        Indices = [],
    };
    private HashSet<ModelComponent> _visibleModelCompSetCache = new();
    private List<ModelPlacement> _pendingNewModelPlacements = new();
    private HashSet<ModelPlacement> _pendingRemoveModelPlacements = new();
    private HashSet<ModelPlacement> _pendingRemoveModelPlacementsDisplayable = new();
    private List<Entity> _sceneEntities = new();
    private bool ProcessPainter(FoliagePainterComponent painterComp, AssociatedData data)
    {
        if (_sceneEditorGame!.IsExiting
            || !painterComp.IsInitialized
            || painterComp.PaintFoilageModelUrl is null)
        {
            return false;
        }
        var foliagePlacementAsset = painterComp.GetFoliagePlacementInternalAsset();
        if (foliagePlacementAsset is null)
        {
            return false;
        }
        var ss = _sceneEditorGame.EditorServices.Services;
        if (!data.IsInitialInstancingDisplayed)
        {
            var uniqueModelUrls = foliagePlacementAsset.ModelPlacements.Select(x => x.ModelUrl.Url).Distinct();
            if (uniqueModelUrls.Any())
            {
                _strideEditorService.Invoke(async () =>
                {
                    foreach (var modelUrl in uniqueModelUrls)
                    {
                        if (!_contentManager.IsLoaded(modelUrl))
                        {
                            await _foliageMeshManagerInitializeTask;
                            await _foliageMeshManager.EnsureModelIsLoadable(modelUrl);
                            //var model = _contentManager.Load(modelUrl);
                            //Debug.WriteLineIf(model is null, $"Could not load model: {modelUrl.Url}");
                        }
                    }
                });
            }
            var modelPlacementsSpan = CollectionsMarshal.AsSpan(foliagePlacementAsset.ModelPlacements);
            data.FoliageInstancingManagerComponent.SetInstancingModels(modelPlacementsSpan);
            data.IsInitialInstancingDisplayed = true;
        }
        if (painterComp.PaintMode == FoliagePlacementPaintMode.Disabled || data.PaintPreviewEntity is null)
        {
            _painterMouseService.SetIsControllingMouse(false);
            return false;
        }

        if (data.BrushSize != painterComp.BrushSize)
        {
            data.PaintPreviewEntity.Transform.Scale = new Vector3(painterComp.BrushSize);
            data.BrushSize = painterComp.BrushSize;
        }

        Debug.Assert(_sceneEditorGame is not null);
        var normalisedMousePosition = _inputManager.MousePosition;
        var absMousePosition = _inputManager.AbsoluteMousePosition;

        var cameraService = _sceneEditorGame.EditorServices.Get<IEditorGameCameraService>();
        var camFrustum = cameraService.Component.Frustum;
        var mouseRay = CalculateRayFromMousePosition(cameraService.Component, normalisedMousePosition, Matrix.Invert(cameraService.ViewMatrix));

        float tileCellLength = painterComp.TileCellLength;
        var tileCellIndexToPos = new Vector3(tileCellLength, tileCellLength * 0.5f, tileCellLength);
        var posToTileCellIndex = 1f / tileCellIndexToPos;

        // Find the model the cursor is over to determine where the brush should be in the scene
        var visibleModelCompSet = _visibleModelCompSetCache;
        ModelComponent cursorHitModelComp = null;
        float cursorHitModelDistance = float.MaxValue;
        {
            var modelMeshData = _modelMeshDataCache;
            modelMeshData.Clear();

            _sceneEntities.Clear();
            _sceneEntities.AddRange(EntityManager);     // Work on copy to avoid threading issues
            foreach (var entity in _sceneEntities)
            {
                if (entity == painterComp.Entity || entity ==  data.PaintPreviewEntity)
                {
                    continue;
                }
                var modelComp = entity.Get<ModelComponent>();
                if (modelComp is null || modelComp.Model is null)
                {
                    continue;
                }
                var boundingBoxExt = new BoundingBoxExt(modelComp.BoundingBox);
                if (!camFrustum.Contains(in boundingBoxExt))
                {
                    continue;       // Not visible to the camera
                }
                var material = modelComp.GetMaterial(0);
                if (!ContainsMaterial(painterComp.MaterialCheckFilter, material))
                {
                    continue;
                }

                modelMeshData.Clear();
                if (!ModelHelper.TryGetMeshData(modelComp.Model, Services, modelMeshData))
                {
                    Debug.WriteLine("Could not retrieve Mesh Data.");
                    continue;
                }

                visibleModelCompSet.Add(modelComp);

                // Determine if the cursor is directly pointing at the model
                var modelWorldTransform = modelComp.Entity.Transform.WorldMatrix;
                ref readonly var vertexPositions = ref modelMeshData.Positions;
                ref readonly var vertexNormals = ref modelMeshData.Normals;
                ref readonly var vertexIndices = ref modelMeshData.Indices;

                // Assume model is triangle list
                for (int i = 0; i < modelMeshData.Indices.Count; i += VerticesPerTriangle)
                {
                    int idx0 = vertexIndices[i];
                    int idx1 = vertexIndices[i + 1];
                    int idx2 = vertexIndices[i + 2];

                    var pos0 = vertexPositions[idx0];
                    var pos1 = vertexPositions[idx1];
                    var pos2 = vertexPositions[idx2];
                    var norm0 = vertexNormals[idx0];
                    var norm1 = vertexNormals[idx1];
                    var norm2 = vertexNormals[idx2];

                    //var avgPos = (pos0 + pos1 + pos2) / 3f;
                    var avgNorm = Vector3.Normalize(norm0 + norm1 + norm2);
                    var worldNorm = Vector3.TransformNormal(avgNorm, modelWorldTransform);
                    worldNorm.Normalize();
                    float normDot = Vector3.Dot(mouseRay.Direction, worldNorm);
                    if (normDot > 0)
                    {
                        continue;   // Ignore backfacing triangles
                    }

                    var worldPos0 = (Vector3)Vector3.Transform(pos0, modelWorldTransform);
                    var worldPos1 = (Vector3)Vector3.Transform(pos1, modelWorldTransform);
                    var worldPos2 = (Vector3)Vector3.Transform(pos2, modelWorldTransform);
                    if (mouseRay.Intersects(in worldPos0, in worldPos1, in worldPos2, out float rayTriDistance))
                    {
                        if (cursorHitModelDistance > rayTriDistance)
                        {
                            cursorHitModelDistance = rayTriDistance;
                            cursorHitModelComp = modelComp;
                            // Note we continue to check all triangles due to the potential of overlapping triangles
                        }
                    }
                }
            }
        }
        Vector3 cursorWorldPosition;
        if (cursorHitModelComp is not null)
        {
            cursorWorldPosition = mouseRay.Position + mouseRay.Direction * cursorHitModelDistance;
        }
        else
        {
            // Cursor isn't over any model in the scene.
            // Determine if the cursor is over any foliage tile cells - this can occur if you paint
            // any foliage then remove the terrain model that it should have been on.
            // Note that in Paint mode this will do nothing since Painting requires a model to 'collide' with,
            // but should be useful in Erase mode to remove floating foliage.
            bool hasHitTile = false;
            float lastHitDistance = float.MaxValue;
            var rayBoxPoint = Vector3.Zero;
            var tileCellBoundingBoxMargin = new Vector3(0.05f);
            for (int i = 0; i < foliagePlacementAsset.ModelPlacements.Count; i++)
            {
                var modelPlacement = foliagePlacementAsset.ModelPlacements[i];
                var indexVec = modelPlacement.Position * posToTileCellIndex;
                var tileIndex = MathExt.ToInt3Floor(indexVec);
                var tileIndexPlusOne = tileIndex + Int3.One;
                var minBoundingBox = MathExt.ToVec3(tileIndex) * tileCellIndexToPos;
                var maxBoundingBox = MathExt.ToVec3(tileIndexPlusOne) * tileCellIndexToPos;
                maxBoundingBox.Y = minBoundingBox.Y + 0.01f;    // Want more of a small 'plane' rather than a box
                // Extend the bounding box to test against to be slightly larger otherwise the cursor tends to find holes in the foliage
                minBoundingBox -= tileCellBoundingBoxMargin;
                maxBoundingBox += tileCellBoundingBoxMargin;

                var tileCellBoundingBox = new BoundingBox(minBoundingBox, maxBoundingBox);
                if (CollisionHelper.RayIntersectsBox(in mouseRay, in tileCellBoundingBox, out float rayBoxPointDistance))
                {
                    if (rayBoxPointDistance < lastHitDistance)
                    {
                        rayBoxPoint = mouseRay.Position + (mouseRay.Direction * rayBoxPointDistance);
                        hasHitTile = true;
                        lastHitDistance = rayBoxPointDistance;
                    }
                }
            }
            if (hasHitTile)
            {
                cursorWorldPosition = rayBoxPoint;
            }
            else
            {
                // Hasn't hit anything, so just default to hitting the scene's ground level.
                cursorWorldPosition = _sceneEditorGame.GetPositionInScene(normalisedMousePosition);
            }
        }

        bool isLeftMousePressed = _inputManager.IsMouseButtonPressed(MouseButton.Left);
        bool isLeftMouseDown = _inputManager.IsMouseButtonDown(MouseButton.Left);
        bool isLeftMouseReleased = _inputManager.IsMouseButtonReleased(MouseButton.Left);
        var mouseButtonState = MouseButtonState.Up;
        if (isLeftMousePressed)
        {
            mouseButtonState = MouseButtonState.JustPressed;
        }
        else if (isLeftMouseReleased)
        {
            mouseButtonState = MouseButtonState.JustReleased;
        }
        else if (isLeftMouseDown)
        {
            mouseButtonState = MouseButtonState.HeldDown;
        }
        _painterMouseService.SetIsControllingMouse(isLeftMouseDown);    // TODO Should only be set if just pressed was valid?

        if (isLeftMousePressed || isLeftMouseDown || isLeftMouseReleased)
        {
            _strideEditorService.Invoke(sessionVmObj =>
            {
                var upVec = Vector3.UnitY;  // TODO allow different 'up' direction?
                var cursorSphere = new BoundingSphere(center: cursorWorldPosition, radius: painterComp.BrushSize * 0.5f);

                if (painterComp.PaintMode == FoliagePlacementPaintMode.Paint)
                {
                    ProcessPainterPaintMode(painterComp, data, visibleModelCompSet, mouseButtonState, cursorSphere, upVec, posToTileCellIndex, tileCellIndexToPos);
                }
                else if (painterComp.PaintMode == FoliagePlacementPaintMode.Erase)
                {
                    ProcessPainterEraseMode(painterComp, data, visibleModelCompSet, mouseButtonState, cursorSphere, upVec, posToTileCellIndex, tileCellIndexToPos);
                }
            });
        }

        data.PaintPreviewEntity.Transform.Position = cursorWorldPosition;
        // TODO change to brush colour based on Disabled/Erased?

        visibleModelCompSet.Clear();
        return true;

        static bool ContainsMaterial(List<Material> materialCheckFilter, Material material)
        {
            if (material is null)
            {
                return false;
            }
            foreach (var mat in materialCheckFilter)
            {
                if (mat == material)
                {
                    return true;
                }
            }
            return false;
        }
    }

    private void ProcessPainterPaintMode(
        FoliagePainterComponent painterComp, AssociatedData data, HashSet<ModelComponent> visibleModelCompSet,
        MouseButtonState mouseButtonState, BoundingSphere cursorSphere,
        Vector3 upVec, Vector3 posToTileCellIndex, Vector3 tileCellIndexToPos)
    {
        var foliagePlacementAsset = painterComp.GetFoliagePlacementInternalAsset();

        int previousPendingNewModelPlacementsCount = _pendingNewModelPlacements.Count;

        bool isMouseDown = mouseButtonState == MouseButtonState.JustPressed || mouseButtonState == MouseButtonState.HeldDown;
        if (isMouseDown)
        {
            // Get all model mesh triangles within the brush
            var trianglesInsideBrush = GetTrianglesInsideBrush(cursorSphere, visibleModelCompSet, upVec);
            Debug.WriteLine($"Hit: {trianglesInsideBrush.Count}");

            if (trianglesInsideBrush.Count > 0)
            {
                var brushTileCellIndices = new HashSet<TileCellIndexXZ>();
                PopulateBrushTileCellIndices(cursorSphere, posToTileCellIndex, brushTileCellIndices);

                var (minY, maxY) = GetVerticalExtremes(trianglesInsideBrush);
                var (minTileCellIndex, maxTileCellIndex) = GetIndexBoundingBox(cursorSphere, posToTileCellIndex);
                var tileCellBoundingBoxSize = tileCellIndexToPos;
                tileCellBoundingBoxSize.Y = (maxTileCellIndex.Y - minTileCellIndex.Y) * tileCellIndexToPos.Y;
                var modelPlacements = new List<ModelPlacement>();

                float tileCellLength = painterComp.TileCellLength;
                var posXZOffsetRange = new Vector2(-tileCellLength / 3, tileCellLength / 3);
                var cellCenterPosOffset = new Vector3(0.5f) * tileCellIndexToPos;
                // TODO painterComp properties?
                var meshScaleRange = new Vector2(0.75f, 1.25f);
                var meshYawRange = new Vector2(-MathUtil.Pi, MathUtil.Pi);

                var painterCompAssetComp = _strideEditorService.GetAssetComponent(painterComp);
                var modelUrlRef = painterCompAssetComp.PaintFoilageModelUrl;

                var occupiedTileCellIndices = new HashSet<TileCellIndexXZ>();
                PopulateOccupiedTileCellIndices(foliagePlacementAsset.ModelPlacements, posToTileCellIndex, brushTileCellIndices, occupiedTileCellIndices);
                PopulateOccupiedTileCellIndices(_pendingNewModelPlacements, posToTileCellIndex, brushTileCellIndices, occupiedTileCellIndices);

                var brushTileCellIndicesSorted = new List<TileCellIndexXZ>(brushTileCellIndices);
                brushTileCellIndicesSorted.Sort();
                var trianglesInsideBrushSpan = CollectionsMarshal.AsSpan(trianglesInsideBrush);
                foreach (var tileCellIndexXZ in brushTileCellIndicesSorted)
                {
                    var tileCellIndex = new Int3(tileCellIndexXZ.X, minTileCellIndex.Y, tileCellIndexXZ.Z);
                    var boundingBoxMinWorldPos = MathExt.ToVec3(tileCellIndex) * tileCellIndexToPos;
                    var boundingBoxMaxWorldPos = boundingBoxMinWorldPos + tileCellBoundingBoxSize;

                    if (occupiedTileCellIndices.Contains(new TileCellIndexXZ(tileCellIndex.X, tileCellIndex.Z)))
                    {
                        // Already occupied
                        continue;
                    }

                    float posX = tileCellIndex.X * tileCellIndexToPos.X + cellCenterPosOffset.X;
                    float posZ = tileCellIndex.Z * tileCellIndexToPos.Z + cellCenterPosOffset.Z;
                    float posXOffset = GetRandomValue(_random, posXZOffsetRange);
                    float posZOffset = GetRandomValue(_random, posXZOffsetRange);

                    // Populate each cell with a mesh
                    float meshPosX = posX + posXOffset;
                    float meshPosZ = posZ + posZOffset;
                    float rayPosYStart = maxY + 0.1f;    // Additional margin to ensure ray can hit bounds
                    float rayPosYEnd = minY - 0.1f;
                    // TODO allow for multitple models? ie. list of posY?
                    if (!TryGetTriangle(meshPosX, meshPosZ, rayPosYStart, rayPosYEnd, trianglesInsideBrushSpan, out int triangleIndex, out float meshPosY))
                    {
                        continue;
                    }

                    ref var triangle = ref trianglesInsideBrushSpan[triangleIndex];

                    var meshPos = new Vector3(meshPosX, meshPosY, meshPosZ);

                    float meshScaleValue = GetRandomValue(_random, meshScaleRange);
                    var meshScale = new Vector3(meshScaleValue, meshScaleValue, meshScaleValue);
                    Vector3.Dot(in upVec, in triangle.Normal, out float upDotTriangleNormal);

                    float meshYaw = GetRandomValue(_random, meshYawRange);
                    Quaternion.BetweenDirections(in upVec, in triangle.Normal, out var surfaceNormalRotation);
                    //var rotation = Quaternion.RotationAxis(Vector3.UnitY, meshYaw) * surfaceNormalRotation;
                    var meshOrientation = Quaternion.RotationAxis(Vector3.UnitY, meshYaw);

                    Matrix.Transformation(in meshScale, in meshOrientation, in meshPos, out var worldTransform);
                    var surfaceNormalModelSpace = Vector3.TransformNormal(triangle.Normal, Matrix.Invert(worldTransform));
                    surfaceNormalModelSpace.Normalize();

                    var modelPlacement = new ModelPlacement
                    {
                        ModelUrl = modelUrlRef,
                        Position = meshPos,
                        Orientation = meshOrientation,
                        Scale = meshScale,
                        SurfaceNormalModelSpace = surfaceNormalModelSpace,
                    };
                    modelPlacements.Add(modelPlacement);

                    string modelUrl = modelUrlRef.Url;
                    if (string.IsNullOrEmpty(modelUrl))
                    {
                        // HACK: When changing the model to paint with, the Editor sometimes creates an 'empty' UrlReference
                        // which is actually just a proxy object
                        var modelUrlAttachedRef = AttachedReferenceManager.GetAttachedReference(modelUrlRef);
                        modelUrl = modelUrlAttachedRef.Url;
                    }
                    if (!_contentManager.IsLoaded(modelUrl))
                    {
                        _strideEditorService.Invoke(() =>
                        {
                            var task = _foliageMeshManager.EnsureModelIsLoadable(modelUrl);
                            //var model = _contentManager.Load(modelUrlRef);
                            //Debug.WriteLineIf(model is null, $"Could not load model: {modelUrlRef.Url}");
                        });
                    }
                }

                if (modelPlacements.Count > 0)
                {
                    _pendingNewModelPlacements.AddRange(modelPlacements);
                }
            }
        }
        if (mouseButtonState == MouseButtonState.JustReleased && _pendingNewModelPlacements.Count > 0)
        {
            Debug.WriteLine($"Add Foliage Count: {_pendingNewModelPlacements.Count}");
            var newModelPlacements = _pendingNewModelPlacements.ToArray();    // Must hold a separate copy for undo/redo
            var foliagePlacementAssetVm = _strideEditorService.FindAssetViewModelByAsset<AssetViewModel<FoliagePlacementAsset>>(foliagePlacementAsset);
            int newPlacementsIndexStart = foliagePlacementAsset.ModelPlacements.Count;

            foliagePlacementAsset.ModelPlacements.AddRange(newModelPlacements);

            if (foliagePlacementAssetVm is IDirtiable dirtiable)
            {
                using var undoRedoTransaction = _strideEditorService.CreateUndoRedoTransaction("Edit Foliage Asset - Add Foliage");
                _strideEditorService.PushTransactionOperation(
                    new AnonymousDirtyingOperation(
                        dirtiables: [dirtiable],
                        undo: () =>
                        {
                            var fpAsset = painterComp.GetFoliagePlacementInternalAsset();
                            fpAsset.ModelPlacements.RemoveRange(newPlacementsIndexStart, newModelPlacements.Length);
                            // Reload all
                            var modelPlacementsSpan = CollectionsMarshal.AsSpan(fpAsset.ModelPlacements);
                            data.FoliageInstancingManagerComponent.SetInstancingModels(modelPlacementsSpan);
                            _strideEditorService.Invoke(() =>
                            {
                                // Must call _strideEditorService.Invoke because undo/redo are not in the original invoke call anymore
                                _strideEditorService.UpdateAssetCollection(fpAsset, collectionMemberName: nameof(FoliagePlacementAsset.ModelPlacements), fpAsset.ModelPlacements);
                            });
                        },
                        redo: () =>
                        {
                            var fpAsset = painterComp.GetFoliagePlacementInternalAsset();
                            fpAsset.ModelPlacements.AddRange(newModelPlacements);
                            data.FoliageInstancingManagerComponent.AppendInstancingModels(newModelPlacements);
                            _strideEditorService.Invoke(() =>
                            {
                                // Must call _strideEditorService.Invoke because undo/redo are not in the original invoke call anymore
                                _strideEditorService.UpdateAssetCollection(fpAsset, collectionMemberName: nameof(FoliagePlacementAsset.ModelPlacements), fpAsset.ModelPlacements);
                            });
                        }
                    )
                );
                dirtiable.UpdateDirtiness(true);
                _strideEditorService.UpdateAssetCollection(foliagePlacementAsset, collectionMemberName: nameof(FoliagePlacementAsset.ModelPlacements), foliagePlacementAsset.ModelPlacements);
            }

            // Add to the 'confirmed' instancing component
            data.FoliageInstancingManagerComponent.AppendInstancingModels(newModelPlacements);

            _pendingNewModelPlacements.Clear();
        }

        if (previousPendingNewModelPlacementsCount != _pendingNewModelPlacements.Count)
        {
            var pendingModelPlacementsSpan = CollectionsMarshal.AsSpan(_pendingNewModelPlacements);
            data.PendingFoliageInstancingManagerComponent.SetInstancingModels(pendingModelPlacementsSpan);
        }
    }

    private void ProcessPainterEraseMode(
        FoliagePainterComponent painterComp, AssociatedData data, HashSet<ModelComponent> visibleModelCompSet,
        MouseButtonState mouseButtonState, BoundingSphere cursorSphere,
        Vector3 upVec, Vector3 posToTileCellIndex, Vector3 tileCellIndexToPos)
    {
        var foliagePlacementAsset = painterComp.GetFoliagePlacementInternalAsset();

        int previousPendingRemoveModelPlacementsCount = _pendingRemoveModelPlacements.Count;

        bool isMouseDown = mouseButtonState == MouseButtonState.JustPressed || mouseButtonState == MouseButtonState.HeldDown;
        if (isMouseDown)
        {
            //// Get all model mesh triangles within the brush
            //var trianglesInsideBrush = GetTrianglesInsideBrush(cursorSphere, visibleModelCompSet, upVec);
            //Debug.WriteLine($"Hit: {trianglesInsideBrush.Count}");

            var brushTileCellIndices = new HashSet<TileCellIndexXZ>();
            PopulateBrushTileCellIndices(cursorSphere, posToTileCellIndex, brushTileCellIndices);
            PopulateOccupiedTileCellModelPlacements(foliagePlacementAsset.ModelPlacements, posToTileCellIndex, brushTileCellIndices, _pendingRemoveModelPlacements);

            if (mouseButtonState == MouseButtonState.JustPressed)
            {
                // To ensure the erased operation is reflected on screen, we temporarily disable 'confirmed' foliage
                // and treat everything as pending.
                data.FoliageInstancingManagerComponent.SetInstancingModels([]);

                _pendingRemoveModelPlacementsDisplayable.AddRange(foliagePlacementAsset.ModelPlacements);
                _pendingRemoveModelPlacementsDisplayable.RemoveWhere(_pendingRemoveModelPlacements.Contains);
            }
        }
        if (mouseButtonState == MouseButtonState.JustReleased && _pendingRemoveModelPlacements.Count > 0)
        {
            Debug.WriteLine($"Remove Foliage Count: {_pendingRemoveModelPlacements.Count}");
            var removeModelPlacements = _pendingRemoveModelPlacements.Select(x => (Model: x, Index: 0)).ToArray();    // Must hold a separate copy for undo/redo

            var foliagePlacementAssetVm = _strideEditorService.FindAssetViewModelByAsset<AssetViewModel<FoliagePlacementAsset>>(foliagePlacementAsset);

            for (int i = 0; i < removeModelPlacements.Length; i++)
            {
                ref var existingModelIndex = ref removeModelPlacements[i];
                existingModelIndex.Index = foliagePlacementAsset.ModelPlacements.IndexOf(existingModelIndex.Model);     // Store the index where it was removed so we can undo/redo
                foliagePlacementAsset.ModelPlacements.RemoveAt(existingModelIndex.Index);
            }

            if (foliagePlacementAssetVm is IDirtiable dirtiable)
            {
                using var undoRedoTransaction = _strideEditorService.CreateUndoRedoTransaction("Edit Foliage Asset - Remove Foliage");
                _strideEditorService.PushTransactionOperation(
                    new AnonymousDirtyingOperation(
                        dirtiables: [dirtiable],
                        undo: () =>
                        {
                            var fpAsset = painterComp.GetFoliagePlacementInternalAsset();
                            // Done in *reverse* because indices change as items are removed
                            for (int i = removeModelPlacements.Length - 1; i >= 0; i--)
                            {
                                ref var existingModelIndex = ref removeModelPlacements[i];
                                fpAsset.ModelPlacements.Insert(existingModelIndex.Index, existingModelIndex.Model);
                            }
                            // Reload all
                            var modelPlacementsSpan = CollectionsMarshal.AsSpan(fpAsset.ModelPlacements);
                            data.FoliageInstancingManagerComponent.SetInstancingModels(modelPlacementsSpan);
                            _strideEditorService.Invoke(() =>
                            {
                                // Must call _strideEditorService.Invoke because undo/redo are not in the original invoke call anymore
                                _strideEditorService.UpdateAssetCollection(fpAsset, collectionMemberName: nameof(FoliagePlacementAsset.ModelPlacements), fpAsset.ModelPlacements);
                            });
                        },
                        redo: () =>
                        {
                            var fpAsset = painterComp.GetFoliagePlacementInternalAsset();
                            for (int i = 0; i < removeModelPlacements.Length; i++)
                            {
                                ref var existingModelIndex = ref removeModelPlacements[i];
                                fpAsset.ModelPlacements.RemoveAt(existingModelIndex.Index);
                            }
                            // Reload all
                            var modelPlacementsSpan = CollectionsMarshal.AsSpan(fpAsset.ModelPlacements);
                            data.FoliageInstancingManagerComponent.SetInstancingModels(modelPlacementsSpan);
                            _strideEditorService.Invoke(() =>
                            {
                                // Must call _strideEditorService.Invoke because undo/redo are not in the original invoke call anymore
                                _strideEditorService.UpdateAssetCollection(fpAsset, collectionMemberName: nameof(FoliagePlacementAsset.ModelPlacements), fpAsset.ModelPlacements);
                            });
                        }
                    )
                );
                // Tells the painterComp the asset has changed
                dirtiable.UpdateDirtiness(true);
                _strideEditorService.UpdateAssetCollection(foliagePlacementAsset, collectionMemberName: nameof(FoliagePlacementAsset.ModelPlacements), foliagePlacementAsset.ModelPlacements);
            }

            // Reload all
            var modelPlacementsSpan = CollectionsMarshal.AsSpan(foliagePlacementAsset.ModelPlacements);
            data.FoliageInstancingManagerComponent.SetInstancingModels(modelPlacementsSpan);

            _pendingRemoveModelPlacements.Clear();
        }

        if (previousPendingRemoveModelPlacementsCount != _pendingRemoveModelPlacements.Count)
        {
            _pendingRemoveModelPlacementsDisplayable.RemoveWhere(_pendingRemoveModelPlacements.Contains);
            var pendingRemoveModelPlacementsDisplayableSpan = CollectionsMarshal.AsSpan(_pendingRemoveModelPlacementsDisplayable.ToList());
            data.PendingFoliageInstancingManagerComponent.SetInstancingModels(pendingRemoveModelPlacementsDisplayableSpan);
        }

    }

    private HashSet<ModelComponent> _brushBoundModelCompSetCache = new();
    private List<ModelMeshTriangleData> GetTrianglesInsideBrush(BoundingSphere cursorSphere, HashSet<ModelComponent> visibleModelCompSet, Vector3 upVec)
    {
        float NormalVecAndUpVecAngleDiff = MathUtil.DegreesToRadians(60);
        float NormalAndUpDotProductThreshold = MathF.Cos(NormalVecAndUpVecAngleDiff);

        // Find all models potentially within the brush
        var brushBoundModelCompSet = _brushBoundModelCompSetCache;
        foreach (var modelComp in visibleModelCompSet)
        {
            if (modelComp.BoundingBox.Contains(in cursorSphere) != ContainmentType.Disjoint)
            {
                brushBoundModelCompSet.Add(modelComp);
            }
        }

        var trianglesInsideBrush = new List<ModelMeshTriangleData>();
        foreach (var modelComp in brushBoundModelCompSet)
        {
            var modelMeshData = _modelMeshDataCache;
            modelMeshData.Clear();
            if (!ModelHelper.TryGetMeshData(modelComp.Model, Services, modelMeshData))
            {
                Debug.Fail("Could not retrieve Mesh Data on second run.");
            }
            var modelWorldTransform = modelComp.Entity.Transform.WorldMatrix;
            ref readonly var vertexPositions = ref modelMeshData.Positions;
            ref readonly var vertexNormals = ref modelMeshData.Normals;
            ref readonly var vertexIndices = ref modelMeshData.Indices;

            // Assume model is triangle list
            for (int i = 0; i < modelMeshData.Indices.Count; i += VerticesPerTriangle)
            {
                int idx0 = vertexIndices[i];
                int idx1 = vertexIndices[i + 1];
                int idx2 = vertexIndices[i + 2];

                var pos0 = vertexPositions[idx0];
                var pos1 = vertexPositions[idx1];
                var pos2 = vertexPositions[idx2];
                var norm0 = vertexNormals[idx0];
                var norm1 = vertexNormals[idx1];
                var norm2 = vertexNormals[idx2];

                //var avgPos = (pos0 + pos1 + pos2) / 3f;
                var avgNorm = Vector3.Normalize(norm0 + norm1 + norm2);

                var worldPos0 = (Vector3)Vector3.Transform(pos0, modelWorldTransform);
                var worldPos1 = (Vector3)Vector3.Transform(pos1, modelWorldTransform);
                var worldPos2 = (Vector3)Vector3.Transform(pos2, modelWorldTransform);

                var worldNorm = Vector3.TransformNormal(avgNorm, modelWorldTransform);
                worldNorm.Normalize();
                float normDot = Vector3.Dot(upVec, worldNorm);
                if (normDot < NormalAndUpDotProductThreshold)
                {
                    continue;
                }
                if (cursorSphere.Contains(in worldPos0, in worldPos1, in worldPos2) == ContainmentType.Disjoint)
                {
                    continue;
                }
                var tri = new ModelMeshTriangleData(worldPos0, worldPos1, worldPos2, worldNorm, modelComp);
                trianglesInsideBrush.Add(tri);
            }

            modelMeshData.Clear();
        }

        brushBoundModelCompSet.Clear();

        return trianglesInsideBrush;
    }

    private static (Int3 Min, Int3 Max) GetIndexBoundingBox(BoundingSphere sphere, Vector3 posToTileCellIndex)
    {
        var minWorldPos = sphere.Center - sphere.Radius;
        var maxWorldPos = sphere.Center + sphere.Radius;
        var minIndex = MathExt.ToInt3Floor(minWorldPos * posToTileCellIndex);
        var maxIndex = MathExt.ToInt3Floor(maxWorldPos * posToTileCellIndex);

        return (minIndex, maxIndex);
    }

    private static (Int3 Min, Int3 Max) GetIndexBoundingBox(Vector3 pos0, Vector3 pos1, Vector3 pos2, Vector3 posToTileCellIndex)
    {
        var idx0 = MathExt.ToInt3Floor(pos0 * posToTileCellIndex);
        var idx1 = MathExt.ToInt3Floor(pos1 * posToTileCellIndex);
        var idx2 = MathExt.ToInt3Floor(pos2 * posToTileCellIndex);

        var min = Int3.Min(idx0, idx1);
        min = Int3.Min(min, idx2);

        var max = Int3.Max(idx0, idx1);
        max = Int3.Max(max, idx2);

        return (min, max);
    }

    private static (float MinY, float MaxY) GetVerticalExtremes(List<ModelMeshTriangleData> triangleList)
    {
        float minY = triangleList[0].Pos0.Y;
        float maxY = minY;
        var triangleSpan = CollectionsMarshal.AsSpan(triangleList);
        for (int i = 0; i < triangleSpan.Length; i++)
        {
            ref var tri = ref triangleSpan[i];
            minY = Math.Min(minY, tri.Pos0.Y);
            minY = Math.Min(minY, tri.Pos1.Y);
            minY = Math.Min(minY, tri.Pos2.Y);

            maxY = Math.Max(maxY, tri.Pos0.Y);
            maxY = Math.Max(maxY, tri.Pos1.Y);
            maxY = Math.Max(maxY, tri.Pos2.Y);
        }
        return (minY, maxY);
    }

    private static void PopulateBrushTileCellIndices(
        BoundingSphere boundingSphere, Vector3 posToTileCellIndex,
        HashSet<TileCellIndexXZ> tileCellIndicesOutput)
    {
        //int radius = (int)MathF.Round(boundingSphere.Radius * posToTileCellIndex.X, digits: 0, mode: MidpointRounding.ToPositiveInfinity);
        int radius = (int)MathF.Floor(boundingSphere.Radius * posToTileCellIndex.X);
        var indexVec = boundingSphere.Center * posToTileCellIndex;
        var tileCellCenterIndex = TileCellIndexXZ.ToTileCellIndex(indexVec);

        if (radius <= 0)
        {
            tileCellIndicesOutput.Add(tileCellCenterIndex);
            return;
        }

        // Use Midpoint Circle Algorithm for to determine the brush's tile indices
        int decisionCriterion = (5 - radius * 4) / 4;
        int x = 0;
        int y = radius;

        do
        {
            FillCellIndices(tileCellCenterIndex.X, x, tileCellCenterIndex.Z - y, tileCellIndicesOutput);
            FillCellIndices(tileCellCenterIndex.X, x, tileCellCenterIndex.Z + y, tileCellIndicesOutput);

            FillCellIndices(tileCellCenterIndex.X, y, tileCellCenterIndex.Z - x, tileCellIndicesOutput);
            FillCellIndices(tileCellCenterIndex.X, y, tileCellCenterIndex.Z + x, tileCellIndicesOutput);

            if (decisionCriterion < 0)
            {
                decisionCriterion += 2 * x + 1;
            }
            else
            {
                decisionCriterion += 2 * (x - y) + 1;
                y--;
            }
            x++;
        } while (x <= y);

        static void FillCellIndices(int xCenter, int halfLength, int z, HashSet<TileCellIndexXZ> tileCellIndicesOutput)
        {
            int xStart = xCenter - halfLength;
            int xEnd = xCenter + halfLength;
            for (int x = xStart ; x <= xEnd; x++)
            {
                tileCellIndicesOutput.Add(new TileCellIndexXZ(x, z));
            }
        }
    }

    private static void PopulateOccupiedTileCellIndices(
        List<ModelPlacement> modelPlacements, Vector3 posToTileCellIndex,
        HashSet<TileCellIndexXZ> brushTileCellIndices,
        HashSet<TileCellIndexXZ> occupiedTileCellIndicesOutput)
    {
        for (int i = 0; i < modelPlacements.Count; i++)
        {
            var modPlacement = modelPlacements[i];
            var indexVec = modPlacement.Position * posToTileCellIndex;
            var tileCellIndex = TileCellIndexXZ.ToTileCellIndex(indexVec);
            // We only need to include the occupied tile cells within the brush bounds
            if (brushTileCellIndices.Contains(tileCellIndex))
            {
                occupiedTileCellIndicesOutput.Add(tileCellIndex);
            }
        }
    }

    private static void PopulateOccupiedTileCellModelPlacements(
        List<ModelPlacement> modelPlacements, Vector3 posToTileCellIndex,
        HashSet<TileCellIndexXZ> brushTileCellIndices,
        HashSet<ModelPlacement> occupiedModelPlacementsOutput)
    {
        for (int i = 0; i < modelPlacements.Count; i++)
        {
            var modPlacement = modelPlacements[i];
            var indexVec = modPlacement.Position * posToTileCellIndex;
            var tileCellIndex = TileCellIndexXZ.ToTileCellIndex(indexVec);
            // We only need to include the occupied tile cells within the brush bounds
            if (brushTileCellIndices.Contains(tileCellIndex))
            {
                occupiedModelPlacementsOutput.Add(modPlacement);
            }
        }
    }

    private static bool TryGetTriangle(float posX, float posZ, float rayPosYStart, float rayPosYEnd, Span<ModelMeshTriangleData> triangleList, out int triangleIndex, out float posY)
    {
        // We just make a ray that points straight down and find the top-most triangle that intersects the ray
        var rayStartPos = new Vector3(posX, rayPosYStart, posZ);
        var rayDir = -Vector3.UnitY;
        var ray = new Ray(rayStartPos, rayDir);

        bool hasHitTriangle = false;
        float lastHitDistance = float.MaxValue;
        triangleIndex = -1;
        posY = float.MinValue;
        for (int i = 0; i < triangleList.Length; i++)
        {
            ref var tri = ref triangleList[i];
            if (CollisionHelper.RayIntersectsTriangle(in ray, in tri.Pos0, in tri.Pos1, in tri.Pos2, out float rayTriPointDistance))
            {
                var rayTriPoint = ray.Position + (ray.Direction * rayTriPointDistance);
                if (rayTriPoint.Y > posY && rayTriPoint.Y >= rayPosYEnd && rayTriPointDistance < lastHitDistance)
                {
                    hasHitTriangle = true;
                    lastHitDistance = rayTriPointDistance;
                    posY = rayTriPoint.Y;
                    triangleIndex = i;
                }
            }
        }
        return hasHitTriangle;
    }

    private static float GetRandomValue(Random random, Vector2 valueRange)
    {
        float rndValue = random.NextSingle();
        float range = valueRange.Y - valueRange.X;
        float finalRndValue = valueRange.X + range * rndValue;
        return finalRndValue;
    }

    // Code from Stride.Assets.Presentation.AssetEditors.GameEditor.Game.EditorGameHelper
    public static Ray CalculateRayFromMousePosition([NotNull] CameraComponent camera, Vector2 normalisedMousePosition, Matrix worldView)
    {
        // determine the mouse position normalized, centered and correctly oriented
        var screenPosition = new Vector2(2f * (normalisedMousePosition.X - 0.5f), -2f * (normalisedMousePosition.Y - 0.5f));

        if (camera.Projection == CameraProjectionMode.Perspective)
        {
            // calculate the ray direction corresponding to the click in the view space
            var verticalFov = MathUtil.DegreesToRadians(camera.VerticalFieldOfView);
            var rayDirectionView = Vector3.Normalize(new Vector3(camera.AspectRatio * screenPosition.X, screenPosition.Y, -1 / MathF.Tan(verticalFov / 2f)));

            // calculate the direction of the ray in the gizmo space
            var rayDirectionGizmo = Vector3.Normalize(Vector3.TransformNormal(rayDirectionView, worldView));

            return new Ray(worldView.TranslationVector, rayDirectionGizmo);
        }
        else
        {
            // calculate the direction of the ray in the gizmo space
            var rayDirectionGizmo = Vector3.Normalize(Vector3.TransformNormal(-Vector3.UnitZ, worldView));

            // calculate the position of the ray in the gizmo space
            var halfSize = camera.OrthographicSize / 2f;
            var rayOriginOffset = new Vector3(screenPosition.X * camera.AspectRatio * halfSize, screenPosition.Y * halfSize, 0);
            var rayOrigin = Vector3.TransformCoordinate(rayOriginOffset, worldView);

            return new Ray(rayOrigin, rayDirectionGizmo);
        }
    }

    private enum MouseButtonState
    {
        Up,
        JustPressed,
        HeldDown,
        JustReleased
    }

    public class AssociatedData
    {
        public bool IsInitialInstancingDisplayed = false;
        //public bool IsEnabled = false;
        public Entity PaintPreviewEntity;
        public ModelComponent PaintPreviewModelComponent;
        public float BrushSize;

        public Entity FoliageInstancingEntity;
        public FoliageInstancingManagerComponent FoliageInstancingManagerComponent;

        public Entity PendingFoliageInstancingEntity;
        public FoliageInstancingManagerComponent PendingFoliageInstancingManagerComponent;
    }
}

public struct TileCellIndexXZ : IEquatable<TileCellIndexXZ>, IComparable<TileCellIndexXZ>
{
    public int X;
    public int Z;

    public TileCellIndexXZ(int x, int z)
    {
        X = x;
        Z = z;
    }

    public readonly bool Equals(TileCellIndexXZ other)
    {
        return X == other.X && Z == other.Z;
    }

    public override readonly bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] object obj)
    {
        return obj is TileCellIndexXZ cellIndex && Equals(cellIndex);
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(X, Z);
    }

    public readonly int CompareTo(TileCellIndexXZ other)
    {
        if (Z != other.Z)
        {
            return Z.CompareTo(other.Z);
        }
        return X.CompareTo(other.X);
    }

    public static TileCellIndexXZ ToTileCellIndex(Vector3 vector)
    {
        var cellIndex = new TileCellIndexXZ(ToIntFloor(vector.X), ToIntFloor(vector.Z));
        return cellIndex;
    }

    private static int ToIntFloor(float value) => (int)MathF.Floor(value);
}

static class ListExt
{
    public static bool TryFindIndex<TElement>(this IReadOnlyList<TElement> list, Func<TElement, bool> isMatchPredicate, out int index)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (isMatchPredicate(list[i]))
            {
                index = i;
                return true;
            }
        }
        index = -1;
        return false;
    }
}
#endif
