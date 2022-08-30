using Stride.Engine;

namespace LevelEditorExtensionExample
{
    class LevelEditorExtensionExampleApp
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
