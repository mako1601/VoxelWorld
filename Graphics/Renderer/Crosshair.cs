using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;

namespace VoxelWorld.Graphics.Renderer
{
    public class Crosshair : IDisposable
    {
        private readonly ShaderProgram _shader;
        private readonly VertexArrayObject _vao;
        private readonly BufferObject<float> _vbo;
        private readonly BufferObject<byte> _ebo;
        private readonly Texture _texture;

        public unsafe Crosshair()
        {
            _vbo = new BufferObject<float>(BufferTarget.ArrayBuffer, _vertices, false);
            _ebo = new BufferObject<byte>(BufferTarget.ElementArrayBuffer, _indices, false);

            _shader = new ShaderProgram("crosshair.glslv", "crosshair.glslf");
            _shader.Use();

            _vao = new VertexArrayObject(4 * sizeof(float));
            _vao.Bind();

            var location = _shader.GetAttribLocation("vPosition");
            _vao.VertexAttribPointer(location, 2, VertexAttribPointerType.Float, false, 0);

            location = _shader.GetAttribLocation("vTexCoord");
            _vao.VertexAttribPointer(location, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float));

            _texture = new Texture("utilities/crosshair.png");
            _texture.Use(TextureUnit.Texture0);

            _shader.SetInt("uTexture", 0);
        }

        public unsafe void Draw(Vector2i windowSize)
        {
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            _shader.Use();
            _shader.SetMatrix4("uProjection", Matrix4.CreateOrthographicOffCenter(-windowSize.X / (float)windowSize.Y, windowSize.X / (float)windowSize.Y, -1f, 1f, -1f, 1f));
            _shader.SetMatrix4("uScale", Matrix4.CreateScale(32f / windowSize.Y));

            _vao.Bind();
            _ebo.Bind();
            _texture.Use(TextureUnit.Texture0);

            GL.DrawElements(PrimitiveType.Triangles, _indices.Length, DrawElementsType.UnsignedByte, IntPtr.Zero);

            GL.Disable(EnableCap.Blend);
        }

        ~Crosshair() => Dispose(false);
        public void Dispose() => Dispose(true);

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            _texture.Dispose();
            _vao.Dispose();
            _vbo.Dispose();
            _ebo.Dispose();
            _shader.Dispose();
        }

        private static readonly float[] _vertices =
        [
          //  x    y   u   v
            -1f, -1f, 0f, 1f,
             1f, -1f, 1f, 1f,
             1f,  1f, 1f, 0f,
            -1f,  1f, 0f, 0f
        ];
        private static readonly byte[] _indices = [ 0, 1, 2, 2, 3, 0 ];
    }
}
