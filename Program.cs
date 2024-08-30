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
                settings = Newtonsoft.Json.JsonConvert.DeserializeObject<WindowSettings>(json);

                settings.Location = settings.State is WindowState.Minimized ? (0, 0) : settings.Location;
                settings.State = settings.State is WindowState.Minimized ? WindowState.Normal : settings.State;
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
