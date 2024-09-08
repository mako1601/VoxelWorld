using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;

using VoxelWorld.World;
using VoxelWorld.Entity;
using VoxelWorld.Managers;

namespace VoxelWorld.Graphics.Renderer
{
    public class LineBatch : IDisposable
    {
        private readonly ShaderProgram _shader;

        private readonly VertexArrayObject _chunkVAO;
        private readonly BufferObject<float> _chunkVBO;
        private readonly BufferObject<int> _chunkEBO;

        private readonly VertexArrayObject _blockVAO;
        private readonly BufferObject<float> _blockVBO;
        private readonly BufferObject<byte> _blockEBO;

        private readonly List<float> _chunkVertices;
        private readonly List<int> _chunkIndices;

        public unsafe LineBatch()
        {
            _chunkVertices = FillVertices();
            _chunkIndices = Enumerable.Range(0, _chunkVertices.Count / 2).ToList();

            _shader = new ShaderProgram("line.glslv", "line.glslf");
            _shader.Use();

            _chunkVAO = new VertexArrayObject(6 * sizeof(float));
            _chunkVAO.Bind();

            _chunkVBO = new BufferObject<float>(BufferTarget.ArrayBuffer, _chunkVertices.ToArray(), false);
            _chunkEBO = new BufferObject<int>(BufferTarget.ElementArrayBuffer, _chunkIndices.ToArray(), false);

            var location = _shader.GetAttribLocation("vPosition");
            _chunkVAO.VertexAttribPointer(location, 3, VertexAttribPointerType.Float, false, 0);

            location = _shader.GetAttribLocation("vColor");
            _chunkVAO.VertexAttribPointer(location, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float));

            _blockVAO = new VertexArrayObject(6 * sizeof(float));
            _blockVAO.Bind();

            _blockVBO = new BufferObject<float>(BufferTarget.ArrayBuffer, _blockVertices, false);
            _blockEBO = new BufferObject<byte>(BufferTarget.ElementArrayBuffer, _blockIndices, false);

            location = _shader.GetAttribLocation("vPosition");
            _blockVAO.VertexAttribPointer(location, 3, VertexAttribPointerType.Float, false, 0);

            location = _shader.GetAttribLocation("vColor");
            _blockVAO.VertexAttribPointer(location, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float));
        }

        public void DrawChunkBoundaries(Player player)
        {
            var c = ChunkManager.GetChunkPosition((int)MathF.Floor(player.Position.X), (int)MathF.Floor(player.Position.Z));

            _shader.Use();
            _shader.SetMatrix4("uModel", Matrix4.CreateTranslation(c.X * Chunk.Size.X, 0f, c.Y * Chunk.Size.Z));
            _shader.SetMatrix4("uView", player.Camera.GetViewMatrix(player.Position));
            _shader.SetMatrix4("uProjection", player.Camera.GetProjectionMatrix());

            _chunkVAO.Bind();
            GL.DrawArrays(PrimitiveType.Lines, 0, _chunkVertices.Count);
        }

        public unsafe void DrawBlockOutline(Player player)
        {
            if (player.Camera.Ray.Block is null) return;

            _shader.Use();
            _shader.SetMatrix4("uModel", Matrix4.CreateTranslation(player.Camera.Ray.Position));
            _shader.SetMatrix4("uView", player.Camera.GetViewMatrix(player.Position));
            _shader.SetMatrix4("uProjection", player.Camera.GetProjectionMatrix());

            _blockVAO.Bind();
            _blockEBO.Bind();
            GL.DrawElements(PrimitiveType.Lines, _blockIndices.Length, DrawElementsType.UnsignedByte, IntPtr.Zero);
        }

        ~LineBatch() => Dispose(false);
        public void Dispose() => Dispose(true);

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            _blockEBO.Dispose();
            _blockVBO.Dispose();
            _blockVAO.Dispose();

            _chunkEBO.Dispose();
            _chunkVBO.Dispose();
            _chunkVAO.Dispose();

            _shader.Dispose();
        }

        private static List<float> FillVertices()
        {
            List<float> list = [];

            for (int x = -16; x <= 32; x += 16)
            {
                for (int z = -16; z <= 32; z += 16)
                {
                    list.AddRange([ x, 0f,   z, 255f, 0f, 0f ]);
                    list.AddRange([ x, 256f, z, 255f, 0f, 0f ]);
                }
            }

            for (int x = 2; x <= 14; x += 2)
            {
                list.AddRange([ x, 0f,   0f, 255f, 255f, 0f ]);
                list.AddRange([ x, 256f, 0f, 255f, 255f, 0f ]);

                list.AddRange([ x, 0f,   16f, 255f, 255f, 0f ]);
                list.AddRange([ x, 256f, 16f, 255f, 255f, 0f ]);
            }

            for (int z = 2; z <= 14; z += 2)
            {
                list.AddRange([ 0f, 0f,   z, 255f, 255f, 0f ]);
                list.AddRange([ 0f, 256f, z, 255f, 255f, 0f ]);

                list.AddRange([ 16f, 0f,   z, 255f, 255f, 0f ]);
                list.AddRange([ 16f, 256f, z, 255f, 255f, 0f ]);
            }

            for (int y = 0; y <= 256; y += 2)
            {
                list.AddRange([ 0f,  y, 0f, 255f, 255f, 0f ]);
                list.AddRange([ 16f, y, 0f, 255f, 255f, 0f ]);

                list.AddRange([ 0f,  y, 16f, 255f, 255f, 0f ]);
                list.AddRange([ 16f, y, 16f, 255f, 255f, 0f ]);

                list.AddRange([ 0f, y,  0f, 255f, 255f, 0f ]);
                list.AddRange([ 0f, y, 16f, 255f, 255f, 0f ]);

                list.AddRange([ 16f, y,  0f, 255f, 255f, 0f ]);
                list.AddRange([ 16f, y, 16f, 255f, 255f, 0f ]);
            }

            return list;
        }

        private static readonly float[] _blockVertices =
        [
        //      x        y        z     r   g   b
            -0.005f, -0.005f, -0.005f, 0f, 0f, 0f,
             1.005f, -0.005f, -0.005f, 0f, 0f, 0f,
             1.005f, -0.005f,  1.005f, 0f, 0f, 0f,
            -0.005f, -0.005f,  1.005f, 0f, 0f, 0f,
            -0.005f,  1.005f, -0.005f, 0f, 0f, 0f,
             1.005f,  1.005f, -0.005f, 0f, 0f, 0f,
             1.005f,  1.005f,  1.005f, 0f, 0f, 0f,
            -0.005f,  1.005f,  1.005f, 0f, 0f, 0f
        ];
        private static readonly byte[] _blockIndices =
        [
            0, 1, 1, 2, 2, 3, 3, 0,
            0, 4, 1, 5, 2, 6, 3, 7,
            4, 5, 5, 6, 6, 7, 7, 4
        ];
    }
}
