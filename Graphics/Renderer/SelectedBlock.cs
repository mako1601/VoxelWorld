using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;
using static OpenTK.Graphics.OpenGL.GL;

using VoxelWorld.World;
using VoxelWorld.Window;
using VoxelWorld.Managers;

namespace VoxelWorld.Graphics.Renderer
{
    public class SelectedBlock : IDisposable
    {
        private int _id;
        private readonly ShaderProgram _shader;
        private readonly VertexArrayObject _vao;
        private readonly BufferObject<float> _vbo;
        private readonly BufferObject<byte> _ebo;

        public SelectedBlock()
        {
            _id = 0;

            _vao = new VertexArrayObject(6 * sizeof(float));
            _vao.Bind();

            _vbo = new BufferObject<float>(BufferTarget.ArrayBuffer, _vertices.Length, true);
            _ebo = new BufferObject<byte>(BufferTarget.ElementArrayBuffer, _indices, false);

            _shader = new ShaderProgram("selected_block.glslv", "selected_block.glslf");
            _shader.Use();

            var location = _shader.GetAttribLocation("vPosition");
            _vao.VertexAttribPointer(location, 3, VertexAttribPointerType.Float, false, 0);

            location = _shader.GetAttribLocation("vTexCoord");
            _vao.VertexAttribPointer(location, 2, VertexAttribPointerType.Float, false, 3 * sizeof(float));

            location = _shader.GetAttribLocation("vBrightness");
            _vao.VertexAttribPointer(location, 1, VertexAttribPointerType.Float, false, 5 * sizeof(float));

            ChunkManager.Instance.TextureAtlas.Use(TextureUnit.Texture0);

            _shader.SetInt("uTexture", 0);
        }

        public unsafe void Draw(UI.Info info)
        {
            Enable(EnableCap.CullFace);
            CullFace(TriangleFace.Back);
            Enable(EnableCap.Blend);
            BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            if (_id != info.Player.SelectedBlock)
            {
                _id = info.Player.SelectedBlock;

                var uv = Block.GetBlockUV(_id, Block.Face.Top);

                _vertices[3] = uv[0].X;
                _vertices[4] = uv[0].Y;

                _vertices[9] = uv[1].X;
                _vertices[10] = uv[1].Y;

                _vertices[15] = uv[2].X;
                _vertices[16] = uv[2].Y;

                _vertices[21] = uv[3].X;
                _vertices[22] = uv[3].Y;

                uv = Block.GetBlockUV(_id, Block.Face.Front);

                _vertices[27] = uv[0].X;
                _vertices[28] = uv[0].Y;

                _vertices[33] = uv[1].X;
                _vertices[34] = uv[1].Y;

                _vertices[39] = uv[2].X;
                _vertices[40] = uv[2].Y;

                _vertices[45] = uv[3].X;
                _vertices[46] = uv[3].Y;

                uv = Block.GetBlockUV(_id, Block.Face.Right);

                _vertices[51] = uv[0].X;
                _vertices[52] = uv[0].Y;

                _vertices[57] = uv[1].X;
                _vertices[58] = uv[1].Y;

                _vertices[63] = uv[2].X;
                _vertices[64] = uv[2].Y;

                _vertices[69] = uv[3].X;
                _vertices[70] = uv[3].Y;

                _vbo.SetData(_vertices, 0, _vertices.Length);
            }

            _vao.Bind();
            ChunkManager.Instance.TextureAtlas.Use(TextureUnit.Texture0);

            _shader.Use();
            _shader.SetMatrix4("uProjection", Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(90f), (float)info.Player.Camera.Width / info.Player.Camera.Height, 1f, 3f));
            _shader.SetMatrix4("uView", info.Player.Camera.GetViewMatrix(info.Player.Position));
            _shader.SetMatrix4("uModel1", Matrix4.CreateTranslation(info.Player.Position + info.Player.Camera.Front));
            _shader.SetMatrix4("uModel2", info.Player.Camera.GetViewMatrix(info.Player.Position).ClearTranslation().Inverted());
            _shader.SetMatrix4("uModel3", Matrix4.CreateTranslation(2f, -2.2f, -1.8f));
            _shader.SetMatrix4("uModel4", Matrix4.CreateRotationY(MathHelper.DegreesToRadians(-55f)));

            DrawElements(PrimitiveType.Triangles, _indices.Length, DrawElementsType.UnsignedByte, IntPtr.Zero);

            Disable(EnableCap.CullFace);
            Disable(EnableCap.Blend);
        }

        ~SelectedBlock() => Dispose(false);
        public void Dispose() => Dispose(true);

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            _ebo.Dispose();
            _vbo.Dispose();
            _vao.Dispose();
            _shader.Dispose();
        }

        private static readonly float[] _vertices =
        [
        //   x   y   z   u   v   br
        // top
            0f, 1f, 0f, 0f, 0f, 1f,
            0f, 1f, 1f, 0f, 0f, 1f,
            1f, 1f, 1f, 0f, 0f, 1f,
            1f, 1f, 0f, 0f, 0f, 1f,

        // front
            0f, 0f, 1f, 0f, 0f, 0.95f,
            1f, 0f, 1f, 0f, 0f, 0.95f,
            1f, 1f, 1f, 0f, 0f, 0.95f,
            0f, 1f, 1f, 0f, 0f, 0.95f,

        // right
            1f, 0f, 1f, 0f, 0f, 0.9f,
            1f, 0f, 0f, 0f, 0f, 0.9f,
            1f, 1f, 0f, 0f, 0f, 0.9f,
            1f, 1f, 1f, 0f, 0f, 0.9f,
        ];
        private static readonly byte[] _indices =
        [
            0, 1, 2, 2, 3, 0,
            4, 5, 6, 6, 7, 4,
            8, 9, 10, 10, 11, 8
        ];
    }
}
