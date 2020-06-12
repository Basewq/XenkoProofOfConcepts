using Stride.Engine;

namespace MultiplayerExample.Windows
{
    class MultiplayerExampleApp
    {
        static void Main(string[] args)
        {
            using (var game = new Game())
            {
                game.Run();
            }
        }
    }
}
