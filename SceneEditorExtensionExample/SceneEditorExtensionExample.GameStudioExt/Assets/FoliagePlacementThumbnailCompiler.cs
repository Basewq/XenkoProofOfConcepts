using SceneEditorExtensionExample.GameStudioExt.Resources;
using SceneEditorExtensionExample.StrideAssetExt.Assets;
using Stride.Core.Assets.Compiler;
using Stride.Editor.Thumbnails;

namespace SceneEditorExtensionExample.GameStudioExt.Assets
{
    [AssetCompiler(typeof(FoliagePlacementAsset), typeof(ThumbnailCompilationContext))]
    public class FoliagePlacementThumbnailCompiler : StaticThumbnailCompiler<FoliagePlacementAsset>
    {
        public FoliagePlacementThumbnailCompiler()
            : base(SceneEditorExtensionExampleAssetsThumbnails.FoliageThumbnail)
        {
        }
    }
}
