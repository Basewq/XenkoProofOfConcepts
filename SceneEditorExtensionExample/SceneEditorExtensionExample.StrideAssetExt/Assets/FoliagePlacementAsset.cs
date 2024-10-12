using SceneEditorExtensionExample.SharedData;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Serialization.Contents;

namespace SceneEditorExtensionExample.StrideAssetExt.Assets;

/**
 * The asset as seen by Game Studio.
 * Refer to Stride's source could for additional asset options, eg. referencing a file.
 */
[DataContract]
[AssetDescription(".gtfp")]
[ContentSerializer(typeof(DataContentSerializer<FoliagePlacementAsset>))]
[AssetContentType(typeof(FoliagePlacement))]
//[CategoryOrder(1000, "Foliage")]
[AssetFormatVersion(SceneEditorExtensionExampleConfig.PackageName, CurrentVersion)]
//[AssetUpgrader(SceneEditorExtensionExampleConfig.PackageName, "0.0.0.1", "1.0.0.0", typeof(FoliagePlacementAssetUpgrader))]    // Can be used to update an old asset format to a new format.
[Display(10000, "Foliage Placement")]
public class FoliagePlacementAsset : Asset
{
    private const string CurrentVersion = "0.0.0.1";

    /// <summary>
    /// Debug only field. Used to quickly determine how many <see cref="ModelPlacements"/> have been serialized.
    /// </summary>
    [Display(Browsable = false)]
    public int ModelPlacementCount { get; set; }
    // Do not make this Browsable, it'll crash the editor due to too much data.
    [Display(Browsable = false)]
    public List<ModelPlacement> ModelPlacements { get; set; } = new();
}
