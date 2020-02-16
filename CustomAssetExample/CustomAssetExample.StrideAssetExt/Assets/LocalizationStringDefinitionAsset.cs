using CustomAssetExample.SharedData;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Serialization.Contents;

namespace CustomAssetExample.StrideAssetExt.Assets
{
    /**
     * The asset as seen by Game Studio.
     * Refer to Stride's source could for additional asset options, eg. referencing a file.
     */
    [DataContract("LocalizationStringDefinitionAsset")]
    [AssetDescription(".lsd")]      // The file extension of the YAML file
    [ContentSerializer(typeof(DataContentSerializer<LocalizationStringDefinitionAsset>))]
    [AssetContentType(typeof(LocalizationStringDefinition))]
    //[CategoryOrder(1000, "Localization String Definition")]
    [AssetFormatVersion(CustomAssetExampleConfig.PackageName, CurrentVersion)]
    //[AssetUpgrader(CustomAssetExampleConfig.PackageName, "0.0.0.1", "1.0.0.0", typeof(LocalizationStringDefinitionAssetUpgrader))]    // Can be used to update an old asset format to a new format.
    [Display(10000, "Localization")]
    public class LocalizationStringDefinitionAsset : Asset
    {
        private const string CurrentVersion = "0.0.0.1";

        public string English { get; set; }
        public string French { get; set; }
        public string German { get; set; }
    }
}
