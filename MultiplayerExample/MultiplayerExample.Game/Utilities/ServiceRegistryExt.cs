using Stride.Core;

namespace MultiplayerExample.Utilities
{
    static class ServiceRegistryExt
    {
        public static void AddOrOverwriteService<T>(this ServiceRegistry services, T service)
            where T : class
        {
            var existingService = services.GetService<T>();
            if (existingService != null)
            {
                if (existingService == service)
                {
                    // Already registered.
                    return;
                }

                services.RemoveService<T>();
            }

            services.AddService<T>(service);
        }
    }
}
