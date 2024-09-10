using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;
using static OpenTK.Graphics.OpenGL.GL;

using FontStashSharp;

using VoxelWorld.Entity;
using VoxelWorld.Graphics.Renderer;
using VoxelWorld.Managers;

namespace VoxelWorld.Window
{
    public class UI
    {
        public struct Info
        {
            public Player Player { get; set; }
            public uint FPS { get; set; }
            public Vector2i WindowSize { get; set; }
            public double Time { get; set; }
        }

        private readonly Text _debugText;
        private readonly Crosshair _crosshair;
        private readonly LineBatch _lineBatch;
        private readonly SelectedBlock _selectedBlock;
        private readonly FontSystem _fontSystem;

        public UI()
        {
            FontSize = 32;
            DebugInfo = true;
            Crosshair = true;
            _debugText = new Text();
            _crosshair = new Crosshair();
            _lineBatch = new LineBatch();
            _selectedBlock = new SelectedBlock();

            var settings = new FontSystemSettings
            {
                FontResolutionFactor = 2,
                KernelWidth = 2,
                KernelHeight = 2
            };

            _fontSystem = new FontSystem(settings);
            _fontSystem.AddFont(File.ReadAllBytes("resources/fonts/BitmapMc.ttf"));
        }

        public uint FontSize { get; set; }
        public bool DebugInfo { get; set; }
        public bool Crosshair { get; set; }
        public bool ChunkBoundaries { get; set; }

        public void Draw(FSColor color, Info info)
        {
            _lineBatch.DrawBlockOutline(info.Player);
            if (ChunkBoundaries is true) _lineBatch.DrawChunkBoundaries(info.Player);

            Clear(ClearBufferMask.DepthBufferBit);

            if (DebugInfo is true)
            {
                var text = $"FPS: {info.FPS}\n" +
                    $"Resolution: {info.WindowSize.X}x{info.WindowSize.Y}\n" +
                    $"Time spent in the world: {info.Time:0.000}\n" +
                    $"Position XYZ: ({info.Player.Position.X:0.000}, {info.Player.Position.Y:0.000}, {info.Player.Position.Z:0.000})\n" +
                    $"Chunk Coords XZ: {ChunkManager.GetChunkPosition(info.Player.RoundedPosition.Xz)}\n" +
                    $"Front XYZ: ({info.Player.Camera.Front.X:0.000}, {info.Player.Camera.Front.Y:0.000}, {info.Player.Camera.Front.Z:0.000})\n" +
                    $"Right XYZ: ({info.Player.Camera.Right.X:0.000}, {info.Player.Camera.Right.Y:0.000}, {info.Player.Camera.Right.Z:0.000})\n" +
                    $"FOV: {info.Player.Camera.FOV:0}\n";
                text += info.Player.Camera.Ray.Block is null ? "Block: too far\n" : $"Block XYZ: {info.Player.Camera.Ray.Block} {info.Player.Camera.Ray.Position}\n";
                text += $"Normal XYZ: {info.Player.Camera.Ray.Normal}\n" +
                    $"Light RGBS: {ChunkManager.GetLight(info.Player.RoundedPosition):X4}\n\n" +
                    $"Number of Chunks: {ChunkManager.Instance.Chunks.Count}\n" +
                    $"AddQueue: {ChunkManager.Instance.AddQueue.Count}\n" +
                    $"RemoveQueue: {ChunkManager.Instance.RemoveQueue.Count}\n" +
                    $"CreateMesh: {ChunkManager.Instance.CreateMesh.Count}\n" +
                    $"UpdateMesh: {ChunkManager.Instance.UpdateMesh.Count}";

                var font = _fontSystem.GetFont(FontSize);

                _debugText.Begin(info.WindowSize);
                font.DrawText(_debugText, text, new System.Numerics.Vector2(5f, 5f), color, scale: new System.Numerics.Vector2(0.7f, 0.7f), effect: FontSystemEffect.Stroked, effectAmount: 4);
                _debugText.End();
            }

            if (Crosshair is true) _crosshair.Draw(info.WindowSize);

            _selectedBlock.Draw(info);
        }

        public void Delete()
        {
            _debugText.Dispose();
            _crosshair.Dispose();
            _lineBatch.Dispose();
            _selectedBlock.Dispose();
        }
    }
}
