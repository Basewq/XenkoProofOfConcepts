#if GAME_EDITOR
using SceneEditorExtensionExample.StrideEditorExt;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Quantum;
using Stride.Editor.EditorGame.ContentLoader;
using Stride.Rendering;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SceneEditorExtensionExample.WorldTerrain.Foliage.Editor;

// Adapted from Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game.NavigationMeshManager
[DataContract]
public class FoliageMeshManager : System.IAsyncDisposable
{
    [DataMember]
    public readonly Dictionary<AssetId, Model> FoliageMeshes = new();

    private readonly IStrideEditorService _strideEditorService;
    private readonly AbsoluteId _referencerId;
    private readonly IEditorContentLoader _loader;
    private readonly IObjectNode _meshesNode;

    public FoliageMeshManager(IStrideEditorService  strideEditorService)
    {
        _strideEditorService = strideEditorService;
        _referencerId = new AbsoluteId(AssetId.Empty, Guid.NewGuid());
        _loader = strideEditorService.GetEditorContentLoader();
        var root = strideEditorService.GetOrCreateGameSideNode(this);
        _meshesNode = root[nameof(FoliageMeshes)].Target;
        _meshesNode.ItemChanged += (sender, args) => { Changed?.Invoke(this, args); };
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var kv in FoliageMeshes)
        {
            var assetId = kv.Key;
            await _loader.Manager.ClearContentReference(_referencerId, assetId, _meshesNode, new NodeIndex(assetId));
        }
        await _loader.Manager.RemoveReferencer(_referencerId);
    }

    public event EventHandler<ItemChangeEventArgs> Changed;

    public Task Initialize()
    {
        return _loader.Manager.RegisterReferencer(_referencerId);
    }

    /// <summary>
    /// Adds a reference to a model mesh if it doesn't already exist
    /// </summary>
    /// <param name="assetId"></param>
    public async Task AddUnique(AssetId assetId)
    {
        if (FoliageMeshes.ContainsKey(assetId))
        {
            return;
        }

        FoliageMeshes.Add(assetId, new Model());
        await _strideEditorService.GameThreadInvokeAsync(() =>
        {
            return _loader.Manager.PushContentReference(_referencerId, assetId, _meshesNode, new NodeIndex(assetId));
        });
    }

    /// <summary>
    /// Removes a reference if it exists
    /// </summary>
    /// <param name="assetId"></param>
    public async Task Remove(AssetId assetId)
    {
        if (!FoliageMeshes.ContainsKey(assetId))
        {
            throw new InvalidOperationException();
        }

        FoliageMeshes.Remove(assetId);
        await _strideEditorService.GameThreadInvokeAsync(() =>
        {
            return _loader.Manager.ClearContentReference(_referencerId, assetId, _meshesNode, new NodeIndex(assetId));
        });
    }

    public async Task EnsureModelIsLoadable(string modelUrl)
    {
        var modelAssetVm = _strideEditorService.FindAssetViewModelByUrl<AssetViewModel>(modelUrl);
        var modelAssetId = modelAssetVm.Asset.Id;
        await AddUnique(modelAssetId);
    }
}
#endif
