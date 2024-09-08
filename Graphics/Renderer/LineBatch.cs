using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;
using static OpenTK.Graphics.OpenGL.GL;

using VoxelWorld.World;
using VoxelWorld.Entity;
using VoxelWorld.Managers;

namespace VoxelWorld.Graphics.Renderer
{
    public class LineBatch
    {
        private readonly ShaderProgram _shader;

        private readonly VertexArrayObject _chunkVAO;
        private readonly BufferObject<Vector3> _chunkVBO;

        private readonly VertexArrayObject _blockVAO;
        private readonly BufferObject<Vector3> _blockVBO;
        private readonly BufferObject<byte> _blockEBO;

        public LineBatch()
        {
            _shader = new ShaderProgram("line.glslv", "line.glslf");

            _chunkVAO = new VertexArrayObject(Marshal.SizeOf<Vector3>());
            _chunkVAO.Bind();

            _chunkVBO = new BufferObject<Vector3>(BufferTarget.ArrayBuffer, _chunkVertices, false);
            _chunkVAO.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0);

            _blockVAO = new VertexArrayObject(Marshal.SizeOf<Vector3>());
            _blockVAO.Bind();

            _blockVBO = new BufferObject<Vector3>(BufferTarget.ArrayBuffer, _blockVertices, false);
            _blockVAO.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0);

            _blockEBO = new BufferObject<byte>(BufferTarget.ElementArrayBuffer, _blockIndices, false);
        }

        public void DrawChunkBoundaries(Color3<Rgb> color, Player player)
        {
            Enable(EnableCap.CullFace);
            CullFace(TriangleFace.Back);
            Enable(EnableCap.Blend);
            BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            var c = ChunkManager.GetChunkPosition((int)MathF.Floor(player.Position.X), (int)MathF.Floor(player.Position.Z));

            _shader.Use();
            _shader.SetVector4("uColor", (color.X, color.Y, color.Z, 1f));
            _shader.SetMatrix4("uModel", Matrix4.CreateTranslation(c.X * Chunk.Size.X, player.Position.Y, c.Y * Chunk.Size.Z));
            _shader.SetMatrix4("uView", player.Camera.GetViewMatrix(player.Position));
            _shader.SetMatrix4("uProjection", player.Camera.GetProjectionMatrix());

            _chunkVAO.Bind();
            DrawArrays(PrimitiveType.Lines, 0, _chunkVertices.Length);

            Disable(EnableCap.CullFace);
            Disable(EnableCap.Blend);
        }

        public void DrawBlockOutline(Player player)
        {
            if (player.Camera.Ray.Block is null) return;

            Enable(EnableCap.CullFace);
            CullFace(TriangleFace.Back);
            Enable(EnableCap.Blend);
            BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            _shader.Use();
            _shader.SetVector4("uColor", (0f, 0f, 0f, 0.5f));
            _shader.SetMatrix4("uModel", Matrix4.CreateTranslation(player.Camera.Ray.Position));
            _shader.SetMatrix4("uView", player.Camera.GetViewMatrix(player.Position));
            _shader.SetMatrix4("uProjection", player.Camera.GetProjectionMatrix());

            _blockVAO.Bind();
            DrawElements(PrimitiveType.Lines, _blockIndices.Length, DrawElementsType.UnsignedByte, 0);

            Disable(EnableCap.CullFace);
            Disable(EnableCap.Blend);
        }

        public void Delete()
        {
            _blockEBO.Dispose();
            _blockVBO.Dispose();
            _blockVAO.Dispose();

            _chunkVBO.Dispose();
            _chunkVAO.Dispose();

            _shader.Dispose();
        }

        private static readonly Vector3[] _chunkVertices =
        [
            (0f, -1000f, 0f), (0f,  1000f, 0f), ( 4f, -1000f, 0f), ( 4f,  1000f, 0f),
            (8f, -1000f, 0f), (8f,  1000f, 0f), (12f, -1000f, 0f), (12f,  1000f, 0f),

            (16f, -1000f,  0f), (16f,  1000f,  0f), (0f, -1000f, 16f), (0f,  1000f, 16f),
            ( 4f, -1000f, 16f), ( 4f,  1000f, 16f), (8f, -1000f, 16f), (8f,  1000f, 16f),

            (12f, -1000f, 16f), (12f,  1000f, 16f), (16f, -1000f, 16f), (16f,  1000f, 16f),
            ( 0f, -1000f,  4f), ( 0f,  1000f,  4f), ( 0f, -1000f,  8f), ( 0f,  1000f,  8f),

            ( 0f, -1000f, 12f), ( 0f,  1000f, 12f), (16f, -1000f,  4f), (16f,  1000f,  4f),
            (16f, -1000f,  8f), (16f,  1000f,  8f), (16f, -1000f, 12f), (16f,  1000f, 12f),

            (-16f, -1000f,  0f), (-16f, 1000f,  0f), (-16f, -1000f, 16f), (-16f, 1000f, 16f),
            (-16f, -1000f, 32f), (-16f, 1000f, 32f), (  0f, -1000f, 32f), (  0f, 1000f, 32f),

            (16f, -1000f, 32f), (16f, 1000f, 32f), (32f, -1000f, 32f), (32f, 1000f, 32f),
            (32f, -1000f, 16f), (32f, 1000f, 16f), (32f, -1000f,  0f), (32f, 1000f,  0f),

            (32f, -1000f, -16f), (32f, 1000f, -16f), ( 16f, -1000f, -16f), ( 16f, 1000f, -16f),
            ( 0f, -1000f, -16f), ( 0f, 1000f, -16f), (-16f, -1000f, -16f), (-16f, 1000f, -16f),
        ];

        private static readonly Vector3[] _blockVertices =
        [
            (-0.004f, -0.004f, -0.004f),
            ( 1.004f, -0.004f, -0.004f),
            ( 1.004f, -0.004f,  1.004f),
            (-0.004f, -0.004f,  1.004f),
            (-0.004f,  1.004f, -0.004f),
            ( 1.004f,  1.004f, -0.004f),
            ( 1.004f,  1.004f,  1.004f),
            (-0.004f,  1.004f,  1.004f)
        ];
        private static readonly byte[] _blockIndices =
        [
            0, 1, 1, 2, 2, 3, 3, 0,
            0, 4, 1, 5, 2, 6, 3, 7,
            4, 5, 5, 6, 6, 7, 7, 4
        ];
    }
}
