using SceneEditorExtensionExample.SharedData;
using Stride.Assets.Materials;
using Stride.Assets.Models;
using Stride.Core.Assets;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Compiler;
using Stride.Core.BuildEngine;
using Stride.Core.Serialization.Contents;

namespace SceneEditorExtensionExample.StrideAssetExt.Assets;

[AssetCompiler(typeof(FoliagePlacementAsset), typeof(AssetCompilationContext))]
public class FoliagePlacementAssetCompiler : AssetCompilerBase
{
    public override IEnumerable<BuildDependencyInfo> GetInputTypes(AssetItem assetItem)
    {
        // We depend on the following Assets to ensure if FoliagePlacementAsset is the only thing that is referencing the model asset, then it
        // will actually be included in the build, otherwise Stride may think the model isn't being used.
        yield return new BuildDependencyInfo(typeof(ModelAsset), typeof(AssetCompilationContext), BuildDependencyType.Runtime | BuildDependencyType.CompileContent);
        yield return new BuildDependencyInfo(typeof(ProceduralModelAsset), typeof(AssetCompilationContext), BuildDependencyType.Runtime | BuildDependencyType.CompileContent);
        yield return new BuildDependencyInfo(typeof(PrefabModelAsset), typeof(AssetCompilationContext), BuildDependencyType.Runtime | BuildDependencyType.CompileContent);
        yield return new BuildDependencyInfo(typeof(MaterialAsset), typeof(AssetCompilationContext), BuildDependencyType.Runtime | BuildDependencyType.CompileContent);
    }

    protected override void Prepare(AssetCompilerContext context, AssetItem assetItem, string targetUrlInStorage, AssetCompilerResult result)
    {
        var asset = (FoliagePlacementAsset)assetItem.Asset;
        asset.ModelPlacementCount = asset.ModelPlacements.Count;
        result.BuildSteps = new AssetBuildStep(assetItem);
        result.BuildSteps.Add(new FoliagePlacementAssetCommand(targetUrlInStorage, asset, assetItem.Package));
    }

    private class FoliagePlacementAssetCommand : AssetCommand<FoliagePlacementAsset>
    {
        public FoliagePlacementAssetCommand(string url, FoliagePlacementAsset parameters, IAssetFinder assetFinder)
            : base(url, parameters, assetFinder)
        {
            Version = 1;
        }

        protected override Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
        {
            // Converts the 'asset' object into the real 'definition' object which will be serialised.
            var result = new FoliagePlacement
            {
                ModelPlacements = Parameters.ModelPlacements.ToList(),      // TODO Optimize better!
            };
            var assetManager = new ContentManager(MicrothreadLocalDatabases.ProviderService);
            assetManager.Save(Url, result);

            return Task.FromResult(ResultStatus.Successful);
        }
    }
}
