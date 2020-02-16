using CustomAssetExample.SharedData;
using Stride.Core.Assets;
using Stride.Core.Assets.Compiler;
using Stride.Core.BuildEngine;
using Stride.Core.Serialization.Contents;

namespace CustomAssetExample.StrideAssetExt.Assets
{
    [AssetCompiler(typeof(LocalizationStringDefinitionAsset), typeof(AssetCompilationContext))]
    public class LocalizationStringDefinitionAssetCompiler : AssetCompilerBase
    {
        protected override void Prepare(AssetCompilerContext context, AssetItem assetItem, string targetUrlInStorage, AssetCompilerResult result)
        {
            var asset = (LocalizationStringDefinitionAsset)assetItem.Asset;
            result.BuildSteps = new AssetBuildStep(assetItem);
            result.BuildSteps.Add(new LocalizationStringDefinitionAssetCommand(targetUrlInStorage, asset, assetItem.Package));
        }

        private class LocalizationStringDefinitionAssetCommand : AssetCommand<LocalizationStringDefinitionAsset>
        {
            public LocalizationStringDefinitionAssetCommand(string url, LocalizationStringDefinitionAsset parameters, IAssetFinder assetFinder)
                : base(url, parameters, assetFinder)
            {
                Version = 1;
            }

            protected override Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
            {
                // Converts the 'asset' object into the real 'definition' object which will be serialised.
                var result = new LocalizationStringDefinition
                {
                    English = Parameters.English,
                    French = Parameters.French,
                    German = Parameters.German,
                };
                var assetManager = new ContentManager(MicrothreadLocalDatabases.ProviderService);
                assetManager.Save(Url, result);

                return Task.FromResult(ResultStatus.Successful);
            }
        }
    }
}
