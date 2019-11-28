using Xenko.Engine;

namespace ScreenSpaceDecalExample.Windows
{
    class ScreenSpaceDecalExampleApp
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
