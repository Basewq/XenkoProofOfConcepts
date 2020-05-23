using System.Reflection;
using Xenko.Core;
using Xenko.Core.Reflection;

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
