using Stride.Core.Assets;

namespace SceneEditorExtensionExample.StrideAssetExt.Assets;

public class FoliagePlacementAssetFactory : AssetFactory<FoliagePlacementAsset>
{
    public override FoliagePlacementAsset New()
    {
        // Can set up default values.
        return new FoliagePlacementAsset
        {

        };
    }
}
