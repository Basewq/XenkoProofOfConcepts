using Stride.Engine;

namespace GameScreenManagerExample.Windows
{
    class GameScreenManagerExampleApp
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
