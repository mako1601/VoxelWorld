using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;
using static OpenTK.Graphics.OpenGL.GL;

using VoxelWorld.Entity;
using VoxelWorld.Graphics.Renderer;
using static VoxelWorld.World.Chunks;

namespace VoxelWorld.Window
{
    public class Interface
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

        public Interface(string blockName)
        {
            FontSize = 32;
            DebugInfo = true;
            Crosshair = true;
            _debugText = new Text(FontSize);
            _crosshair = new Crosshair();
            _lineBatch = new LineBatch();
            _selectedBlock = new SelectedBlock(blockName);
        }

        public uint FontSize { get; set; }
        public bool DebugInfo { get; set; }
        public bool Crosshair { get; set; }

        public void Draw(Color3<Rgb> color, Info info)
        {
            _lineBatch.DrawBlockOutline(info.Player);
            if (DebugInfo is true) _lineBatch.DrawChunkBoundaries(Color3.Yellow, info.Player);

            Clear(ClearBufferMask.DepthBufferBit);

            if (DebugInfo is true) DrawInfo(color, info);
            if (Crosshair is true) _crosshair.Draw(info.WindowSize);

            _selectedBlock.Draw(info);
        }

        public void Delete()
        {
            _debugText.Delete();
            _crosshair.Delete();
            _lineBatch.Delete();
           _selectedBlock.Delete();
        }

        private void DrawLine(string text, float x, float y, float scale/*, Vector2 dir*/)
        {
            ActiveTexture(TextureUnit.Texture0);
            BindVertexArray(_debugText.VAO.ID);

            //float angle_rad = (float)Math.Atan2(dir.Y, dir.X);
            //Matrix4 rotateM = Matrix4.CreateRotationZ(angle_rad);
            Matrix4 transOriginM = Matrix4.CreateTranslation(x, y, 0f);

            float char_x = 0f;
            foreach (var c in text)
            {
                if (!_debugText.Characters.ContainsKey(c))
                {
                    continue;
                }
                Text.Character ch = _debugText.Characters[c];

                float w = ch.Size.X * scale;
                float h = ch.Size.Y * scale;
                float xrel = char_x + ch.Bearing.X * scale;
                float yrel = (ch.Size.Y - ch.Bearing.Y) * scale;

                char_x += (ch.Advance >> 6) * scale;

                Matrix4 scaleM = Matrix4.CreateScale(w, h, 1f);
                Matrix4 transRelM = Matrix4.CreateTranslation(xrel, yrel, 0.0f);

                UniformMatrix4f(0, 1, false, transOriginM);
                //UniformMatrix4(1, false, ref rotateM);
                UniformMatrix4f(2, 1, false, transRelM);
                UniformMatrix4f(3, 1, false, scaleM);

                BindTexture(TextureTarget.Texture2d, ch.TextureID);

                DrawArrays(PrimitiveType.Triangles, 0, 6);
            }

            BindVertexArray(0);
            BindTexture(TextureTarget.Texture2d, 0);
        }

        private void DrawInfo(Color3<Rgb> color, Info info)
        {
            Enable(EnableCap.Blend);
            BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            _debugText.Shader.Bind();
            _debugText.Shader.SetMatrix4("uProjection", Matrix4.CreateOrthographicOffCenter(0f, info.WindowSize.X, info.WindowSize.Y, 0f, -1f, 1f));
            _debugText.Shader.SetVector3("uColor", (Vector3)color);

            Vector3i playerPos = ((int)MathF.Floor(info.Player.Position.X), (int)MathF.Floor(info.Player.Position.Y), (int)MathF.Floor(info.Player.Position.Z));

            DrawLine($"FPS: {info.FPS}", 5f, 20f, 0.5f);
            DrawLine($"Time spent in the world: {info.Time:0.000}", 5f, 40f, 0.5f);
            DrawLine($"Resolution: {info.WindowSize.X}x{info.WindowSize.Y}", 5f, 60f, 0.5f);
            DrawLine($"Position XYZ: ({info.Player.Position.X:0.000}, {info.Player.Position.Y:0.000}, {info.Player.Position.Z:0.000})", 5f, 80f, 0.5f);
            DrawLine($"Front XYZ: ({info.Player.Camera.Front.X:0.000}, {info.Player.Camera.Front.Y:0.000}, {info.Player.Camera.Front.Z:0.000})", 5f, 100f, 0.5f);
            DrawLine($"Right XYZ: ({info.Player.Camera.Right.X:0.000}, {info.Player.Camera.Right.Y:0.000}, {info.Player.Camera.Right.Z:0.000})", 5f, 120f, 0.5f);
            DrawLine($"FOV: {info.Player.Camera.FOV:0}", 5f, 140f, 0.5f);
            DrawLine(info.Player.Camera.Ray.Block is null ? "Block: too far" : $"Block XYZ: {info.Player.Camera.Ray.Block}", 5f, 160f, 0.5f);
            DrawLine($"Normal XYZ: {info.Player.Camera.Ray.Normal}", 5f, 180f, 0.5f);
            DrawLine($"Chunk Coords XZ: {GetChunkPosition(playerPos.Xz)}", 5f, 200f, 0.5f);
            DrawLine($"Light RGBS: {GetBlock(playerPos)?.GetLight(0)}'{GetBlock(playerPos)?.GetLight(1)}'{GetBlock(playerPos)?.GetLight(2)}'{GetBlock(playerPos)?.GetLight(3)}'", 5f, 220f, 0.5f);

            Disable(EnableCap.Blend);
        }
    }
}
