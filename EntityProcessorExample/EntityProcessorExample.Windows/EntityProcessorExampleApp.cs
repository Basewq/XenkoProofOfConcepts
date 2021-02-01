using Stride.Engine;

namespace EntityProcessorExample.Windows
{
    class EntityProcessorExampleApp
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
