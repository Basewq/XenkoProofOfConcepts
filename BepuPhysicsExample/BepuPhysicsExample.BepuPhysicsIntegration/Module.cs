using System.Reflection;
using Stride.Core;
using Stride.Core.Reflection;

namespace BepuPhysicsExample.BepuPhysicsIntegration
{
    internal class Module
    {
        [ModuleInitializer]
        public static void Initialize()
        {
            AssemblyRegistry.Register(typeof(Module).GetTypeInfo().Assembly, AssemblyCommonCategories.Assets);
        }
    }
}
