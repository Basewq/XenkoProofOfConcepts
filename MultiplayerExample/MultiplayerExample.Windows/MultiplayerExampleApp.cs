namespace MultiplayerExample.Windows
{
    class MultiplayerExampleApp
    {
        static void Main(string[] args)
        {
            using (var game = new GameAppClient())
            {
                game.Run();
            }
        }
    }
}
