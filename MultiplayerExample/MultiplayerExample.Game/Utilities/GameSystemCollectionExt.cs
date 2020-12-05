using Stride.Games;

namespace MultiplayerExample.Utilities
{
    static class GameSystemCollectionExt
    {
        public static void TryAdd<T>(this GameSystemCollection gameSystems, T gameSystem)
            where T : GameSystemBase
        {
            for (int i = 0; i < gameSystems.Count; i++)
            {
                if (gameSystems[i] == gameSystem)
                {
                    return;
                }
            }
            gameSystems.Add(gameSystem);
        }
    }
}
