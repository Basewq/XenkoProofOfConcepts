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

        public void TryUpdate(GameTime gameTime)
        {
            if (System.Enabled)
            {
                using (Profiler.Begin(ProfilingKey))
                {
                    System.Update(gameTime);
                }
            }
        }
    }
}
