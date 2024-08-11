using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;
using static OpenTK.Graphics.OpenGL.GL;

using VoxelWorld.World;
using VoxelWorld.Entity;

namespace VoxelWorld.Graphics.Renderer
{
    public class ChunkBoundaries
    {
        private readonly ShaderProgram _shader;

        private readonly VAO _VAO;
        private readonly VBO _VBO;

        public ChunkBoundaries()
        {
            _shader = new ShaderProgram("line.glslv", "line.glslf");
            _VAO = new VAO();
            _VBO = new VBO(_vertices);
            VAO.LinkToVAO(0, 3);
        }

        public void Draw(Color3<Rgb> color, Player player)
        {
            Enable(EnableCap.CullFace);
            CullFace(TriangleFace.Back);
            Enable(EnableCap.Blend);
            BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            var c = Chunks.GetChunkPosition((int)MathF.Floor(player.Position.X), (int)MathF.Floor(player.Position.Z));

            _shader.Bind();
            _shader.SetVector4("color", (color.X, color.Y, color.Z, 1f));
            _shader.SetMatrix4("model", Matrix4.CreateTranslation(c.X * Chunk.Size.X, player.Position.Y, c.Y * Chunk.Size.Z));
            _shader.SetMatrix4("view", player.Camera.GetViewMatrix(player.Position));
            _shader.SetMatrix4("projection", player.Camera.GetProjectionMatrix());

            _VAO.Bind();

            DrawArrays(PrimitiveType.Lines, 0, _vertices.Count);

            Disable(EnableCap.CullFace);
            Disable(EnableCap.Blend);
        }

        public void Delete()
        {
            _VBO.Delete();
            _VAO.Delete();

            _shader.Delete();
        }

        private static readonly List<Vector3> _vertices =
        [
            (0f, -100f, 0f),
            (0f,  100f, 0f),

            (2f, -100f, 0f),
            (2f,  100f, 0f),

            (4f, -100f, 0f),
            (4f,  100f, 0f)
        ];
    }
}
