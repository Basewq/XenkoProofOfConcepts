using CustomAssetExample.GameStudioExt.Resources;
using CustomAssetExample.StrideAssetExt.Assets;
using Stride.Core.Assets.Compiler;
using Stride.Editor.Thumbnails;

namespace CustomAssetExample.GameStudioExt.Assets
{
    [AssetCompiler(typeof(LocalizationStringDefinitionAsset), typeof(ThumbnailCompilationContext))]
    public class LocalizationStringDefinitionThumbnailCompiler : StaticThumbnailCompiler<LocalizationStringDefinitionAsset>
    {
        public LocalizationStringDefinitionThumbnailCompiler()
            : base(CustomAssetThumbnails.LocalizationThumbnail)
        {
        }
    }
}
