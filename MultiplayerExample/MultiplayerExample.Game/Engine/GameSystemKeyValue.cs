using Stride.Core.Diagnostics;
using Stride.Games;

namespace MultiplayerExample.Engine
{
    internal readonly struct GameSystemKeyValue<T> where T : GameSystemBase
    {
        public readonly ProfilingKey ProfilingKey;
        public readonly T System;

        public GameSystemKeyValue(ProfilingKey profilingKey, T system)
        {
            ProfilingKey = profilingKey;
            System = system;
        }

        public void UpdateIfEnabled(GameTime gameTime)
        {
            if (System.Enabled)
            {
                using (Profiler.Begin(ProfilingKey))
                {
                    System.Update(gameTime);
                }
            }
        }

        public readonly void UpdateIfEnabled(GameTime gameTime, bool canUpdate)
        {
            if (canUpdate && System.Enabled)
            {
                using (Profiler.Begin(ProfilingKey))
                {
                    System.Update(gameTime);
                }
            }
        }
    }
}
