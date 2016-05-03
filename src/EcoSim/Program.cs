using System;

namespace EcoSim
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            int gridSize = 300;
            bool fullScreen = false;

            if (args != null && args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i].Equals("-size") && (i+1) < args.Length)
                    {
                        gridSize = int.Parse(args[i + 1]);
                        i++;
                    }
                    else if (args[i].Equals("-f"))
                    {
                        fullScreen = true;
                    }
                }
            }

            using (var game = new EcoSimGame(true, gridSize, fullScreen))
            {
                game.Run();
            }
        }
    }
}
