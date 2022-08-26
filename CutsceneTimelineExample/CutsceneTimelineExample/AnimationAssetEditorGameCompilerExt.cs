#if GAME_EDITOR
using Stride.Assets.Models;
using Stride.Core.Assets.Compiler;
using Stride.Editor.Preview;

namespace CutsceneTimelineExample
{
    [AssetCompiler(typeof(AnimationAsset), typeof(EditorGameCompilationContext))]
    public class AnimationAssetEditorGameCompilerExt : AnimationAssetCompiler
    {
        // Hack to trick Game Studio Editor to fully load the animation clip assets in
        // the editor scene because Game Studio Editor actually loads dummy assets
    }
}
#endif
