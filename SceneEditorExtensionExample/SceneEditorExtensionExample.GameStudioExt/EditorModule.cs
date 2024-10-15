using Stride.Core;
using Stride.Core.Reflection;

namespace SceneEditorExtensionExample.GameStudioExt
{
    /**
     * This class is used to make CompilerApp & Game Studio aware of this assembly to make it recognize our custom asset(s).
     */
    internal class EditorModule
    {
        [ModuleInitializer]
        public static void Initialize()
        {
            AssemblyRegistry.Register(typeof(EditorModule).Assembly, AssemblyCommonCategories.Assets);
        }
    }
}
