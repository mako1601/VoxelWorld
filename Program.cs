using VoxelWorld.Window;

namespace VoxelWorld
{
    public static class Program
    {
        [STAThread]
        private static void Main()
        {
            Game game = new(1280, 768);
            game.Run();
        }
    }
}
