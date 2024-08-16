using Newtonsoft.Json;

using OpenTK.Windowing.Common;

using VoxelWorld.Window;

namespace VoxelWorld
{
    public static class Program
    {
        [STAThread]
        private static void Main()
        {
            WindowSettings settings;
            if (File.Exists("WindowSettings.json"))
            {
                string json = File.ReadAllText("WindowSettings.json");
                settings = JsonConvert.DeserializeObject<WindowSettings>(json);

                settings.X = settings.WindowState is WindowState.Minimized ? 0 : settings.X;
                settings.Y = settings.WindowState is WindowState.Minimized ? 0 : settings.Y;
                settings.WindowState = settings.WindowState is WindowState.Minimized ? WindowState.Normal : settings.WindowState;
            }
            else
            {
                settings = WindowSettings.Default;
            }

            Game game = new(settings);
            game.Run();
        }
    }
}
