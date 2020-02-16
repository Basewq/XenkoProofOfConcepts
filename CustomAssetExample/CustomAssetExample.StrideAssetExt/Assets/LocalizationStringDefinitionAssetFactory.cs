using Stride.Core.Assets;

namespace CustomAssetExample.StrideAssetExt.Assets
{
    public class LocalizationStringDefinitionAssetFactory : AssetFactory<LocalizationStringDefinitionAsset>
    {
        public override LocalizationStringDefinitionAsset New()
        {
            // Can set up default values.
            return new LocalizationStringDefinitionAsset
            {

            };
        }
    }
}
