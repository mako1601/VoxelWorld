using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;
using static OpenTK.Graphics.OpenGL.GL;

namespace VoxelWorld.Graphics.Renderer
{
    public class Crosshair
    {
        private readonly ShaderProgram _shader;
        private readonly VAO _vao;
        private readonly VBO _vbo;
        private readonly VBO _textureVBO;
        private readonly EBO _ebo;
        private readonly Texture _texture;

        public Crosshair()
        {
            _shader = new ShaderProgram("crosshair.glslv", "crosshair.glslf");
            _vao = new VAO();
            _vbo = new VBO(_vertices);
            VAO.LinkToVAO(0, 2);
            _textureVBO = new VBO(_textureVertices);
            VAO.LinkToVAO(1, 2);
            _ebo = new EBO(_indices);
            _texture = new Texture("crosshair.png");

            _shader.Bind();
            _shader.SetInt("uTexture", 0);
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
            DrawElements(PrimitiveType.Triangles, _indices.Count, DrawElementsType.UnsignedInt, 0);

            Disable(EnableCap.Blend);
        }

        public void Delete()
        {
            _texture.Delete();
            _ebo.Delete();
            _textureVBO.Delete();
            _vbo.Delete();
            _vao.Delete();
            _shader.Delete();
        }

        private static readonly List<Vector2> _vertices =
        [
            (-1f, -1f),
            ( 1f, -1f),
            ( 1f,  1f),
            (-1f,  1f),
        ];
        private static readonly List<Vector2> _textureVertices =
        [
            (0f, 1f),
            (1f, 1f),
            (1f, 0f),
            (0f, 0f)
        ];
        private static readonly List<uint> _indices = [0, 1, 2, 2, 3, 0];
    }
}
