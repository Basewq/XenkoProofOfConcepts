using Stride.Engine;

#if GAME_EDITOR
using Stride.Editor.EditorGame.ContentLoader;
using Stride.Core.IO;
using Stride.Assets.Entities;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.SceneEditor.Services;
using Stride.Assets.Presentation.AssetEditors.SceneEditor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.SceneEditor.Views;
using Stride.Assets.Presentation.ViewModel;
using Stride.Core.Annotations;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Quantum;
using Stride.Core.Mathematics;
using Stride.Core.Quantum;
using Stride.Core.Serialization;
using Stride.Games;
using Stride.GameStudio.View;
using Stride.GameStudio.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
#endif

namespace SceneEditorExtensionExample.StrideEditorExt;

class SceneEditorExtProcessor : EntityProcessor<SceneEditorExtBase>, IStrideEditorService
{
#if GAME_EDITOR
    private EditorViewModels? _editorViewModels = null;

    public SceneEditorExtProcessor()
    {

    }

    protected override void OnSystemAdd()
    {
        Services.AddService<IStrideEditorService>(this);
    }

    protected override void OnSystemRemove()
    {
        Services.RemoveService<IStrideEditorService>();
    }

    protected override void OnEntityComponentRemoved(Entity entity, [NotNull] SceneEditorExtBase component, [NotNull] SceneEditorExtBase data)
    {
        component.Deinitialize();
    }

    public override void Update(GameTime gameTime)
    {
        foreach (var kv in ComponentDatas)
        {
            var sceneEditorComp = kv.Key;
            if (!sceneEditorComp.IsInitialized)
            {
                var uiComponent = sceneEditorComp.Entity.Get<UIComponent>();
                if (uiComponent is not null && uiComponent.Page?.RootElement is null)
                {
                    // Can't initialize yet, UI not ready
                }
                else
                {
                    sceneEditorComp.Initialize(this, uiComponent);
                }
                continue;   // Don't need to update immediately after being initialized (doesn't really matter)
            }

            sceneEditorComp.Update(gameTime);
        }
    }

    void IStrideEditorService.Invoke(Action action)
    {
        var gsVm = GameStudioViewModel.GameStudio;
        if (!IsValid(gsVm))
        {
            return;
        }
        gsVm.StrideAssets.Dispatcher.Invoke(() =>
        {
            _editorViewModels = EditorViewModels.Create();
            action();
            _editorViewModels = null;
        });
    }

    void IStrideEditorService.Invoke(Action<object> action)
    {
        var gsVm = GameStudioViewModel.GameStudio;
        if (!IsValid(gsVm))
        {
            return;
        }
        gsVm.StrideAssets.Dispatcher.Invoke(() =>
        {
            _editorViewModels = EditorViewModels.Create();
            action(_editorViewModels?.SessionViewModel!);
            _editorViewModels = null;
        });
    }

    Task IStrideEditorService.InvokeAsync(Func<Task> actionAsync)
    {
        var gsVm = GameStudioViewModel.GameStudio;
        if (!IsValid(gsVm))
        {
            return Task.CompletedTask;
        }
        var task = gsVm.StrideAssets.Dispatcher.Invoke(async () =>
        {
            _editorViewModels = EditorViewModels.Create();
            await actionAsync();
            _editorViewModels = null;
        });
        return task;
    }

    Task IStrideEditorService.InvokeAsync(Func<object, Task> actionAsync)
    {
        var gsVm = GameStudioViewModel.GameStudio;
        if (!IsValid(gsVm))
        {
            return Task.CompletedTask;
        }
        var task = gsVm.StrideAssets.Dispatcher.Invoke(async () =>
        {
            _editorViewModels = EditorViewModels.Create();
            await actionAsync(_editorViewModels?.SessionViewModel!);
            _editorViewModels = null;
        });
        return task;
    }

    Task IStrideEditorService.GameThreadInvokeAsync(Func<Task> actionAsync)
    {
        var returnValue = InvokeOnUI(() =>
        {
            var sceneEditorController = GetSceneEditorController(_editorViewModels?.SceneEditorViewModel);
            var task = sceneEditorController.InvokeTask(actionAsync);
            return task;
        });
        return returnValue;
    }

    void IStrideEditorService.AddPrefabToScene(UrlReference<Prefab> prefabRef, Vector3 position, Quaternion? rotation, Vector3? scale, Entity parent, List<Guid>? outputEntityIds)
    {
        var sceneVm = _editorViewModels?.SceneViewModel;
        var sceneEditorVm = _editorViewModels?.SceneEditorViewModel;
        Debug.Assert(sceneVm is not null);
        Debug.Assert(sceneEditorVm is not null, "Ensure this is called within Invoke/InvokeAsync");

        var package = sceneVm.AssetItem.Package;
        var prefabAssetItem = package.Assets.FirstOrDefault(x => string.Equals(x.Location.FullPath, prefabRef.Url));
        var prefabAsset = prefabAssetItem?.Asset as PrefabAsset;
        var prefabVmRaw = sceneEditorVm.Session.GetAssetById(prefabAsset?.Id ?? default);
        var prefabVm = (PrefabViewModel)prefabVmRaw;
        const AddChildModifiers AddChildMod = AddChildModifiers.Alt;    // Create without a container entity

        // SceneRootViewModel/EntityViewModel has an internal method called AddEntitiesFromAssets which will be used to add a prefab into the scene
        var addEntitiesFromAssets_MethodInfo = typeof(EntityHierarchyItemViewModel).GetMethod("AddEntitiesFromAssets", BindingFlags.NonPublic | BindingFlags.Instance);
        Debug.Assert(addEntitiesFromAssets_MethodInfo is not null);

        EntityHierarchyItemViewModel entityParentViewModel;
        int entityInsertIndex;
        if (parent is not null)
        {
            entityParentViewModel = FindEntityViewModel(sceneEditorVm, parent.Id);
            entityInsertIndex = entityParentViewModel.EntityHierarchy.GetChildCount(parent);
        }
        else
        {
            //var sceneRootVm = sceneEditorVm.RootPart as SceneRootViewModel;
            //Debug.Assert(sceneRootVm is not null);
            // Add to the root scene
            entityParentViewModel = sceneEditorVm.HierarchyRoot;
            entityInsertIndex = sceneEditorVm.HierarchyRoot.EntityHierarchy.Hierarchy.RootParts.Count;
        }

        // Store the original rotation/scale, then update the prefab with new rotation/scale
        List<(Quaternion Rotation, Vector3 Scale)> rootOriginalRotScale = new();
        var prefabRootEntities = prefabVm.Asset.Hierarchy.RootParts;
        try
        {
            foreach (var prefabEnt in prefabRootEntities)
            {
                var transfComp = prefabEnt.Transform;
                rootOriginalRotScale.Add((transfComp.Rotation, transfComp.Scale));
                transfComp.Rotation *= rotation ?? Quaternion.Identity;
                transfComp.Scale += scale ?? Vector3.Zero;
            }
            var param_Assets = new[] { prefabVm };
            int param_Index = entityInsertIndex;
            var param_Modifiers = AddChildMod;
            var param_RootPosition = position;
            var methodParams = new object[] { param_Assets, param_Index, param_Modifiers, param_RootPosition };
            var methodReturnValue = addEntitiesFromAssets_MethodInfo.Invoke(entityParentViewModel, methodParams);
            if (methodReturnValue is IReadOnlyCollection<EntityViewModel> entitiesViewModels)
            {
                foreach (var evm in entitiesViewModels)
                {
                    bool isRotationChanged = rotation.HasValue;
                    bool isScaleChanged = scale.HasValue;
                    if (isRotationChanged || isScaleChanged)
                    {
                        MarkEntityPropertyAsOverridden(sceneEditorVm, evm, isRotationChanged, isScaleChanged);
                    }

                    outputEntityIds?.Add(evm.AssetSideEntity.Id);
                }
            }
        }
        finally
        {
            // Restore original rotation/scale on the original prefab
            for (int i = 0; i < prefabRootEntities.Count; i++)
            {
                var prefabEnt = prefabRootEntities[i];
                var transfComp = prefabEnt.Transform;
                transfComp.Rotation = rootOriginalRotScale[i].Rotation;
                transfComp.Scale = rootOriginalRotScale[i].Scale;
            }
        }

        static void MarkEntityPropertyAsOverridden(
            SceneEditorViewModel sceneEditorVm, EntityViewModel assetEntityViewModel,
            bool isRotationChanged, bool isScaleChanged
            )
        {
            var assetSideSceneEntity = assetEntityViewModel.AssetSideEntity;        // This is the entity on the 'master' version.
            var assetSideSceneEntityComp = assetSideSceneEntity.Transform;

            var entityCompNode = sceneEditorVm.Session.AssetNodeContainer.GetNode(assetSideSceneEntityComp);
            if (isRotationChanged)
            {
                var entityCompPropertyNodeRaw = entityCompNode[nameof(TransformComponent.Rotation)];
                var entityCompPropertyNode = entityCompPropertyNodeRaw as IAssetMemberNode;
                Debug.Assert(entityCompPropertyNode is not null, "Property was not found or invalid type.");
                if (entityCompPropertyNode.BaseNode is not null)
                {
                    entityCompPropertyNode.OverrideContent(true);
                }
            }
            if (isScaleChanged)
            {
                var entityCompPropertyNodeRaw = entityCompNode[nameof(TransformComponent.Scale)];
                var entityCompPropertyNode = entityCompPropertyNodeRaw as IAssetMemberNode;
                Debug.Assert(entityCompPropertyNode is not null, "Property was not found or invalid type.");
                if (entityCompPropertyNode.BaseNode is not null)
                {
                    entityCompPropertyNode.OverrideContent(true);
                }
            }
        }
    }

    Guid IStrideEditorService.CreateEmptyEntity(Vector3 position, Quaternion? rotation, Vector3? scale, Entity parent, string entityName)
    {
        var sceneEditorVm = _editorViewModels?.SceneEditorViewModel;
        Debug.Assert(sceneEditorVm is not null, "Ensure this is called within Invoke/InvokeAsync");

        EntityHierarchyItemViewModel entityParentViewModel;
        int entityInsertIndex;
        if (parent is not null)
        {
            entityParentViewModel = FindEntityViewModel(sceneEditorVm, parent.Id);
            entityInsertIndex = entityParentViewModel.EntityHierarchy.GetChildCount(parent);
        }
        else
        {
            //var sceneRootVm = sceneEditorVm.RootPart as SceneRootViewModel;
            //Debug.Assert(sceneRootVm is not null);
            // Add to the root scene
            entityParentViewModel = sceneEditorVm.HierarchyRoot;
            entityInsertIndex = sceneEditorVm.HierarchyRoot.EntityHierarchy.Hierarchy.RootParts.Count;
        }

        var assetSideEntity = new Entity(entityName, position);
        assetSideEntity.Transform.Rotation = rotation ?? assetSideEntity.Transform.Rotation;
        assetSideEntity.Transform.Scale = scale ?? assetSideEntity.Transform.Scale;
        // Create item ids collections for new entity before actually adding them to the asset.
        AssetCollectionItemIdHelper.GenerateMissingItemIds(assetSideEntity);

        var entityDesign = new EntityDesign(assetSideEntity, (entityParentViewModel as EntityFolderViewModel)?.Path ?? "");
        var collection = new AssetPartCollection<EntityDesign, Entity> { entityDesign };
        //parentEntityViewModel.EntityHierarchy.GetChildCount(parent);
        entityParentViewModel.Asset.AssetHierarchyPropertyGraph.AddPartToAsset(
            collection,
            entityDesign,
            (entityParentViewModel.Owner as EntityViewModel)?.AssetSideEntity,
            entityInsertIndex);

        return assetSideEntity.Id;
    }

    void IStrideEditorService.DeleteEntity(Entity entity)
    {
        Debug.Assert(_editorViewModels.HasValue);
        var sceneEditorVm = _editorViewModels.Value.SceneEditorViewModel;
        Debug.Assert(sceneEditorVm is not null, "Ensure this is called within Invoke/InvokeAsync");

        var assetEntityViewModel = FindEntityViewModel(sceneEditorVm, entity.Id);
        //var assetSideSceneEntity = assetEntityViewModel.AssetSideEntity;        // This is the entity on the 'master' version.

        HashSet<Tuple<Guid, Guid>> mapping;
        bool wasFound = assetEntityViewModel.Asset.Asset.Hierarchy.Parts.TryGetValue(entity.Id, out EntityDesign entityDesign);
        Debug.Assert(wasFound);
        var entityDesigns = new[] { entityDesign };
        assetEntityViewModel.Asset.AssetHierarchyPropertyGraph.DeleteParts(entityDesigns, out mapping);
        if (sceneEditorVm?.UndoRedoService.TransactionInProgress ?? false)
        {
            var operation = new DeletedPartsTrackingOperation<EntityDesign, Entity>(assetEntityViewModel.Asset, mapping);
            sceneEditorVm.UndoRedoService.PushOperation(operation);
        }
    }

    T IStrideEditorService.GetAssetComponent<T>(T runtimeComponent)
    {
        var sceneEditorVm = _editorViewModels?.SceneEditorViewModel;
        Debug.Assert(sceneEditorVm is not null, "Ensure this is called within Invoke/InvokeAsync");

        var sceneEntity = runtimeComponent.Entity;
        var entityId = sceneEntity.Id;

        // The Game Studio holds a master version of the scene, whereas the processor (which we're currently in) is running on
        // a separate duplicate copy of the scene.
        // We need to find the matching Id (which should be unique) of the entity from the master version, then
        // find the associated asset node which will allow us to update an entity's property on the master version
        // and this will then propagate this back to our copy of the scene.
        var assetEntityViewModel = FindEntityViewModel(sceneEditorVm, entityId);
        var assetSideSceneEntity = assetEntityViewModel.AssetSideEntity;        // This is the entity on the 'master' version.
        var assetSideSceneEntityComp = assetSideSceneEntity.Get<T>();
        return assetSideSceneEntityComp;
    }

    void IStrideEditorService.UpdateAssetComponentData<T>(T runtimeComponent, string propertyName, object newValue)
    {
        var sceneEntity = runtimeComponent.Entity;
        (this as IStrideEditorService).UpdateAssetComponentDataByEntityId<T>(sceneEntity.Id, propertyName, newValue);
    }

    void IStrideEditorService.UpdateAssetComponentDataByEntityId<T>(Guid entityId, string propertyName, object newValue)
    {
        var sceneEditorVm = _editorViewModels?.SceneEditorViewModel;
        Debug.Assert(sceneEditorVm is not null, "Ensure this is called within Invoke/InvokeAsync");

        // The Game Studio holds a master version of the scene, whereas the processor (which we're currently in) is running on
        // a separate duplicate copy of the scene.
        // We need to find the matching Id (which should be unique) of the entity from the master version, then
        // find the associated asset node which will allow us to update an entity's property on the master version
        // and this will then propagate this back to our copy of the scene.
        var assetEntityViewModel = FindEntityViewModel(sceneEditorVm, entityId);
        var assetSideSceneEntity = assetEntityViewModel.AssetSideEntity;        // This is the entity on the 'master' version.
        var assetSideSceneEntityComp = assetSideSceneEntity.Get<T>();

        var entityCompNode = sceneEditorVm.Session.AssetNodeContainer.GetNode(assetSideSceneEntityComp);
        var entityCompPropertyNodeRaw = entityCompNode[propertyName];
        var entityCompPropertyNode = entityCompPropertyNodeRaw as IAssetMemberNode;
        Debug.Assert(entityCompPropertyNode is not null, "Property was not found or invalid type.");

        entityCompPropertyNode.Update(newValue);
    }

    void IStrideEditorService.UpdateAssetComponentArrayData<T>(T runtimeComponent, string propertyName, object newValue, int arrayIndex)
    {
        var sceneEntity = runtimeComponent.Entity;
        (this as IStrideEditorService).UpdateAssetComponentArrayDataByEntityId<T>(sceneEntity.Id, propertyName, newValue, arrayIndex);
    }

    void IStrideEditorService.UpdateAssetComponentArrayDataByEntityId<T>(Guid entityId, string propertyName, object newValue, int arrayIndex)
    {
        var sceneEditorVm = _editorViewModels?.SceneEditorViewModel;
        Debug.Assert(sceneEditorVm is not null, "Ensure this is called within Invoke/InvokeAsync");

        var assetEntityViewModel = FindEntityViewModel(sceneEditorVm, entityId);
        var assetSideSceneEntity = assetEntityViewModel.AssetSideEntity;        // This is the entity on the 'master' version.
        var assetSideSceneEntityComp = assetSideSceneEntity.Get<T>();

        var entityCompNode = sceneEditorVm.Session.AssetNodeContainer.GetNode(assetSideSceneEntityComp);
        var entityCompPropertyNodeRaw = entityCompNode[propertyName];
        var entityCompPropertyNode = entityCompPropertyNodeRaw as IAssetMemberNode;
        Debug.Assert(entityCompPropertyNode is not null, "Property was not found or invalid type.");

        //var arrayData = entityCompPropertyNode.TargetReference.ObjectValue;
        var entityCompPropertyObjectNode = entityCompPropertyNode.Target;
        entityCompPropertyObjectNode.Update(newValue, new NodeIndex(arrayIndex));
    }

    void IStrideEditorService.UpdateAssetCollection(object assetCollectionContainerObject, string collectionMemberName, object collectionObject)
    {
        var sceneEditorVm = _editorViewModels?.SceneEditorViewModel;
        Debug.Assert(sceneEditorVm is not null, "Ensure this is called within Invoke/InvokeAsync");

        var nodeContainer = sceneEditorVm.Session.AssetNodeContainer;
        var collContainerNode = nodeContainer.GetNode(assetCollectionContainerObject);
        var collectionNodeRaw = collContainerNode[collectionMemberName];
        collectionNodeRaw.Target.ItemReferences.Refresh(collectionNodeRaw, nodeContainer);
    }

    void IStrideEditorService.PushTransactionOperation(object operation)
    {
        var trxOp = operation as Stride.Core.Transactions.Operation;
        Debug.Assert(trxOp is not null, $"Must be an Operation.");
        var undoRedoService = _editorViewModels?.SessionViewModel?.UndoRedoService;
        if (undoRedoService?.TransactionInProgress == true)
        {
            undoRedoService.PushOperation(trxOp);
        }
        else
        {
            Debug.Fail($"Transaction was not created.");
        }
    }

    IDisposable IStrideEditorService.CreateUndoRedoTransaction(string transactionName)
    {
        Debug.Assert(_editorViewModels.HasValue, $"Must be called within {nameof(IStrideEditorService.Invoke)}");
        var undoRedoService = _editorViewModels?.SessionViewModel?.UndoRedoService;
        if (undoRedoService is not null)
        {
            var undoRedoTransaction = undoRedoService.CreateTransaction();
            undoRedoService.SetName(undoRedoTransaction, transactionName);
            return undoRedoTransaction;
        }
        else
        {
            return DisposableExtensions.Empty;
        }
    }

    TAssetViewModel IStrideEditorService.FindAssetViewModel<TAssetViewModel>(object proxyObject)
        where TAssetViewModel : class
    {
        var returnValue = InvokeOnUI(() =>
        {
            var assetFinder = _editorViewModels?.SessionViewModel as IAssetFinder;
            Debug.Assert(assetFinder is not null);
            var assetItem = assetFinder.FindAssetFromProxyObject(proxyObject);

            var sessionVm = _editorViewModels?.SessionViewModel;
            var assetVms = sessionVm?.AllAssets;
            var assetVm = assetVms?.Where(x => x.AssetItem == assetItem).FirstOrDefault();
            var castedVm = assetVm as TAssetViewModel;
            return castedVm;
        });
        return returnValue;
    }

    TAssetViewModel IStrideEditorService.FindAssetViewModelByAsset<TAssetViewModel>(object asset)
        where TAssetViewModel : class
    {
        var returnValue = InvokeOnUI(() =>
        {
            var assetFinder = _editorViewModels?.SessionViewModel as IAssetFinder;
            Debug.Assert(assetFinder is not null);

            var sessionVm = _editorViewModels?.SessionViewModel;
            var assetVms = sessionVm?.AllAssets;
            var assetVm = assetVms?.Where(x => x.Asset == asset).FirstOrDefault();
            var castedVm = assetVm as TAssetViewModel;
            return castedVm;
        });
        return returnValue;
    }

    TAssetViewModel IStrideEditorService.FindAssetViewModelByUrl<TAssetViewModel>(string url)
        where TAssetViewModel : class
    {
        var returnValue = InvokeOnUI(() =>
        {
            var assetFinder = _editorViewModels?.SessionViewModel as IAssetFinder;
            Debug.Assert(assetFinder is not null);

            var sessionVm = _editorViewModels?.SessionViewModel;
            var assetVms = sessionVm?.AllAssets;
            var urlUFile = new UFile(url);
            var assetVm = assetVms?.Where(x => x.AssetItem.Location == urlUFile).FirstOrDefault();
            var castedVm = assetVm as TAssetViewModel;
            return castedVm;
        });
        return returnValue;
    }

    object IStrideEditorService.FindEntityViewModel(Guid entityId)
    {
        var returnValue = InvokeOnUI(() =>
        {
            var sceneEditorVm = _editorViewModels?.SceneEditorViewModel;
            if (sceneEditorVm is null)
            {
                return null;
            }
            var entityVm = FindEntityViewModel(sceneEditorVm, entityId);
            return entityVm;
        });
        return returnValue;
    }

    IEditorContentLoader IStrideEditorService.GetEditorContentLoader()
    {
        var returnValue = InvokeOnUI(() =>
        {
            var sceneEditorController = GetSceneEditorController(_editorViewModels?.SceneEditorViewModel);
            return sceneEditorController?.Loader;
        });
        return returnValue;
    }

    IObjectNode IStrideEditorService.GetOrCreateGameSideNode(object rootObject)
    {
        var returnValue = InvokeOnUI(() =>
        {
            var sceneEditorController = GetSceneEditorController(_editorViewModels?.SceneEditorViewModel);
            var rootNode = sceneEditorController?.GameSideNodeContainer?.GetOrCreateNode(rootObject);
            return rootNode;
        });
        return returnValue;
    }

    private static SceneEditorController GetSceneEditorController(SceneEditorViewModel? sceneEditorVm)
    {
        if (sceneEditorVm is null)
        {
            return null;
        }
        var getController_PropertyInfo = typeof(SceneEditorViewModel).GetProperty("Controller", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        Debug.Assert(getController_PropertyInfo is not null);
        var getController_MethodInfo = getController_PropertyInfo.GetGetMethod(nonPublic: true);
        var sceneEditorController = getController_MethodInfo?.Invoke(sceneEditorVm, []) as SceneEditorController;
        return sceneEditorController;
    }

    private TReturnValue InvokeOnUI<TReturnValue>(Func<TReturnValue> func)
    {
        if (_editorViewModels.HasValue)
        {
            // Already on UI thread
            var obj = func();
            return obj;
        }

        var gsVm = GameStudioViewModel.GameStudio;
        if (!IsValid(gsVm))
        {
            return default;
        }
        var returnValue = gsVm.StrideAssets.Dispatcher.Invoke(() =>
        {
            _editorViewModels = EditorViewModels.Create();
            var obj = func();
            _editorViewModels = null;
            return obj;
        });
        return returnValue;
    }

    private static EntityViewModel FindEntityViewModel(SceneEditorViewModel sceneEditorVm, Guid sceneEntityId)
    {
        // The entity in the game scene is NOT the same instance as the entity in the *asset file*,
        // so we need to find the 'master' entity

        var sceneRootVm = sceneEditorVm.RootPart as SceneRootViewModel;
        Debug.Assert(sceneRootVm is not null);
        if (TryFindEntityViewModel(sceneEntityId, sceneRootVm, out var assetEntity))
        {
            return assetEntity;
        }

        Debug.Fail($"Entity ID {sceneEntityId} not found.");
        throw new ArgumentException($"Entity ID {sceneEntityId} not found.", paramName: nameof(sceneEntityId));

        static bool TryFindEntityViewModel(Guid sceneEntityId, EntityHierarchyItemViewModel entityHierarchy, out EntityViewModel entityViewModel)
        {
            if (entityHierarchy is EntityViewModel evm && evm.AssetSideEntity.Id == sceneEntityId)
            {
                entityViewModel = evm;
                return true;
            }
            // This will also look into child scenes since they're part of the hierarchy
            foreach (var childHierarchy in entityHierarchy.Children)
            {
                if (TryFindEntityViewModel(sceneEntityId, childHierarchy, out entityViewModel))
                {
                    return true;
                }
            }

            entityViewModel = null!;
            return false;
        }
    }

    private static Func<GameStudioViewModel, bool>? IsGameStudioViewModelDestroyed;
    private static bool IsValid(GameStudioViewModel gsVm)
    {
        // HACK: When closing the editor, UI events may be triggered causing your own code
        // to try invoke UI methods. We check the GameStudioViewModel if its IsDestroyed value
        // which is true when the editor is closing.
        if (IsGameStudioViewModelDestroyed is null)
        {
            var isDestroyed_PropertyInfo = typeof(GameStudioViewModel).GetProperty("IsDestroyed", BindingFlags.NonPublic | BindingFlags.Instance);
            Debug.Assert(isDestroyed_PropertyInfo is not null);
            var isDestroyed_MethodInfo = isDestroyed_PropertyInfo.GetGetMethod(nonPublic: true);
            IsGameStudioViewModelDestroyed = isDestroyed_MethodInfo.CreateDelegate<Func<GameStudioViewModel, bool>>();
        }
        return !IsGameStudioViewModelDestroyed(gsVm);
    }

    private struct EditorViewModels
    {
        public SceneViewModel SceneViewModel;
        public SceneEditorViewModel SceneEditorViewModel;
        public SessionViewModel SessionViewModel;

        public static EditorViewModels Create()
        {
            var viewModels = new EditorViewModels();

            //var editorVm = Stride.Core.Assets.Editor.ViewModel.EditorViewModel.Instance as Stride.GameStudio.GameStudioViewModel;     // Unused

            // Application.Current must be accessed on the UI thread
            var window = (GameStudioWindow)System.Windows.Application.Current.MainWindow;
            var sceneEditorView = window.GetChildOfType<SceneEditorView>();
            var sceneEditorVm = sceneEditorView?.DataContext as SceneEditorViewModel;
            var sceneVm = sceneEditorVm?.Asset;

            var gsVm = GameStudioViewModel.GameStudio;

            viewModels.SceneViewModel = sceneVm;
            viewModels.SceneEditorViewModel = sceneEditorVm;
            viewModels.SessionViewModel = gsVm.Session;

            return viewModels;
        }
    }
#endif
}

#if GAME_EDITOR
static class WpfExt
{
    public static T GetChildOfType<T>(this System.Windows.DependencyObject depObj)
        where T : System.Windows.DependencyObject
    {
        if (depObj is null) return null;

        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(depObj); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(depObj, i);

            var result = child as T ?? child.GetChildOfType<T>();
            if (result is not null) return result;
        }
        return null;
    }
}
#endif
