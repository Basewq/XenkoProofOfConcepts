#if GAME_EDITOR
using Stride.Core.Mathematics;
using Stride.Core.Quantum;
using Stride.Core.Serialization;
using Stride.Editor.EditorGame.ContentLoader;
using Stride.Engine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
#endif

namespace SceneEditorExtensionExample.StrideEditorExt;

public interface IStrideEditorService
{
#if GAME_EDITOR
    /// <summary>
    /// Invoke an <see cref="Action"/> on the UI thread.
    /// </summary>
    void Invoke(Action action);
    Task InvokeAsync(Func<Task> actionAsync);
    void Invoke(Action<object> action);
    Task InvokeAsync(Func<object, Task> actionAsync);

    Task GameThreadInvokeAsync(Func<Task> actionAsync);

    /// <param name="outputEntityIds">Optional list to output the IDs of the generated (top-level) entity/entities.</param>
    /// <remarks>
    /// Entities are NOT immediately generated in the scene.
    /// Use <see cref="Task.Delay(TimeSpan)"/> before accessing these entities in the scene.
    /// </remarks>
    void AddPrefabToScene(UrlReference<Prefab> prefabRef, Vector3 position, Quaternion? rotation = null, Vector3? scale = null, Entity parent = null, List<Guid>? outputEntityIds = null);
    /// <returns>The ID of the generated entity.</returns>
    /// /// <remarks>
    /// Entities are NOT immediately generated in the scene.
    /// Use <see cref="Task.Delay(TimeSpan)"/> before accessing these entities in the scene.
    /// </remarks>
    Guid CreateEmptyEntity(Vector3 position = default, Quaternion? rotation = null, Vector3? scale = null, Entity parent = null, string entityName = "Entity");
    void DeleteEntity(Entity entity);

    /// <returns>The asset version of the editor scene entity component.</returns>
    T GetAssetComponent<T>(T runtimeComponent) where T : EntityComponent;

    void UpdateAssetComponentData<T>(T runtimeComponent, string propertyName, object newValue) where T : EntityComponent;
    void UpdateAssetComponentDataByEntityId<T>(Guid entityId, string propertyName, object newValue) where T : EntityComponent;

    void UpdateAssetComponentArrayData<T>(T runtimeComponent, string propertyName, object newValue, int arrayIndex) where T : EntityComponent;
    void UpdateAssetComponentArrayDataByEntityId<T>(Guid entityId, string propertyName, object newValue, int arrayIndex) where T : EntityComponent;

    void UpdateAssetCollection(object assetCollectionContainerObject, string collectionMemberName, object collectionObject);

    IDisposable CreateUndoRedoTransaction(string transactionName);
    void PushTransactionOperation(object operation);

    TAssetViewModel FindAssetViewModel<TAssetViewModel>(object proxyObject) where TAssetViewModel : class
        => null;
    TAssetViewModel FindAssetViewModelByAsset<TAssetViewModel>(object asset) where TAssetViewModel : class
        => null;
    TAssetViewModel FindAssetViewModelByUrl<TAssetViewModel>(string url) where TAssetViewModel : class
        => null;
    object FindEntityViewModel(Guid entityId) => null;

    IEditorContentLoader GetEditorContentLoader();

    IObjectNode GetOrCreateGameSideNode(object rootObject);
#endif
}
