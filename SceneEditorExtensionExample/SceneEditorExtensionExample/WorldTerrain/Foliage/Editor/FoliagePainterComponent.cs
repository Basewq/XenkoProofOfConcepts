using SceneEditorExtensionExample.SharedData;
using SceneEditorExtensionExample.StrideEditorExt;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Serialization;
using Stride.Engine;
using Stride.Rendering;
using System.Collections.Generic;

#if GAME_EDITOR
using SceneEditorExtensionExample.StrideAssetExt.Assets;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Engine.Design;
using System.Diagnostics;
#endif

namespace SceneEditorExtensionExample.WorldTerrain.Foliage.Editor;

[DataContract]
public enum FoliagePlacementPaintMode
{
    Disabled,
    Paint,
    Erase
}
#if GAME_EDITOR
[DefaultEntityComponentProcessor(typeof(FoliagePainterProcessor), ExecutionMode = ExecutionMode.Editor)]
#endif
public class FoliagePainterComponent : SceneEditorExtBase
{
    private FoliagePlacement _foliagePlacementAsset;
    [DataMember(order: 10)]
    public FoliagePlacement FoliagePlacementAsset
    {
        get => _foliagePlacementAsset;
        set => SetProperty(ref _foliagePlacementAsset, value);
    }

    [DataMember(order: 20)]
    public UrlReference<Prefab> PaintPlacementPreviewPrefabUrl;

    // Must add all tile sets here so the ContentManager becomes aware to allow the prefabs
    // to be loadable through ContentManager while in the editor.
    [DataMember(order: 21)]
    public UrlReference<Model> PaintFoilageModelUrl { get; set; }

    [DataMember(order: 22)]
    public List<Material> MaterialCheckFilter { get; } = new();

    [DataMember(order: 40)]
    internal FoliagePlacementPaintMode PaintMode { get; set; } = FoliagePlacementPaintMode.Disabled;
    /// <summary>
    /// Brush diameter in world units.
    /// </summary>
    [DataMember(order: 41)]
    [DataMemberRange(minimum: 0, maximum: 40, smallStep: 0.1, largeStep: 1, decimalPlaces: 2)]
    internal float BrushSize { get; set; } = 5;
    [DataMember(order: 42)]
    public float TileCellLength { get; set; } = 0.5f;

    //[DataMemberIgnore]
    //public Entity TileModelsParentEntity { get; internal set; } = default!;

#if GAME_EDITOR
    internal FoliagePlacementAsset? GetFoliagePlacementInternalAsset()
    {
        if (FoliagePlacementAsset is null)
        {
            return null;
        }
        var foliagePlacementAssetVm = StrideEditorService.FindAssetViewModel<AssetViewModel<FoliagePlacementAsset>>(FoliagePlacementAsset);
        Debug.Assert(foliagePlacementAssetVm is not null);
        return foliagePlacementAssetVm.Asset;
    }

    protected internal override void Initialize()
    {
    }

    protected internal override void Deinitialize()
    {
    }
#endif
}
