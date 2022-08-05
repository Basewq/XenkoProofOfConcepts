using Stride.Engine;

namespace CutsceneTimelineExample
{
    class CutsceneTimelineExampleApp
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
