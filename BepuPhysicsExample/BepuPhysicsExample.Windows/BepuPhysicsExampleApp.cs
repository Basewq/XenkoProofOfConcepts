using Xenko.Engine;

namespace BepuPhysicsExample.Windows
{
    class BepuPhysicsExampleApp
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
