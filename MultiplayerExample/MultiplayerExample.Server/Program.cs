using System;

namespace MultiplayerExample.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting ServerGame.");

            using (var game = new GameAppServer())
            {
                game.Run();
            }
        }
    }
}
