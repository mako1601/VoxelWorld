using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;
using static OpenTK.Graphics.OpenGL.GL;

namespace VoxelWorld.Graphics.Renderer
{
    public class Crosshair
    {
        private readonly ShaderProgram _shader;
        private readonly VAO _vao;
        private readonly BufferObject<Vector2> _vbo;
        private readonly BufferObject<Vector2> _textureVBO;
        private readonly BufferObject<byte> _ebo;
        private readonly Texture _texture;

        public Crosshair()
        {
            _shader = new ShaderProgram("crosshair.glslv", "crosshair.glslf");

            _vao = new VAO();

            _vbo = new BufferObject<Vector2>(BufferTarget.ArrayBuffer, _vertices, false);
            VAO.LinkToVAO(0, 2);

            _textureVBO = new BufferObject<Vector2>(BufferTarget.ArrayBuffer, _textureVertices, false);
            VAO.LinkToVAO(1, 2);

            _ebo = new BufferObject<byte>(BufferTarget.ElementArrayBuffer, _indices, false);

            _texture = new Texture("utilities/crosshair.png");
        }

        public void Draw(Vector2i windowSize)
        {
            Enable(EnableCap.Blend);
            BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            _shader.Bind();
            _shader.SetMatrix4("uProjection", Matrix4.CreateOrthographicOffCenter(-windowSize.X / (float)windowSize.Y, windowSize.X / (float)windowSize.Y, -1f, 1f, -1f, 1f));
            _shader.SetMatrix4("uScale", Matrix4.CreateScale(32f / windowSize.Y));

            _texture.Bind();
            _vao.Bind();
            DrawElements(PrimitiveType.Triangles, _indices.Length, DrawElementsType.UnsignedByte, 0);

            Disable(EnableCap.Blend);
        }

        public void Delete()
        {
            _texture.Dispose();
            _ebo.Dispose();
            _textureVBO.Dispose();
            _vbo.Dispose();
            _vao.Dispose();
            _shader.Dispose();
        }

        private static readonly Vector2[] _vertices = [ (-1f, -1f), ( 1f, -1f), ( 1f,  1f), (-1f,  1f) ];
        private static readonly Vector2[] _textureVertices = [ (0f, 1f), (1f, 1f), (1f, 0f), (0f, 0f) ];
        private static readonly byte[] _indices = [0, 1, 2, 2, 3, 0];
    }
}
