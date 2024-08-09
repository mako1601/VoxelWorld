using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;
using static OpenTK.Graphics.OpenGL4.GL;

using VoxelWorld.World;
using VoxelWorld.Window;

namespace VoxelWorld.Graphics.Renderer
{
    public class SelectedBlock
    {
        private string _name;
        private readonly ShaderProgram _shader;
        private readonly VAO _vao;
        private readonly VBO _vboVertices;
        private readonly VBO _vboUV;
        private readonly VBO _vboBrightness;
        private readonly EBO _ebo;

        public SelectedBlock(string name)
        {
            _name = name;

            _shader = new ShaderProgram("selected_block.glslv", "selected_block.glslf");

            _vao = new VAO();
            _vboVertices = new VBO(_vertices);
            VAO.LinkToVAO(0, 3);
            _vboUV = new VBO(_uv);
            VAO.LinkToVAO(1, 3);
            _vboBrightness = new VBO(_brightness);
            VAO.LinkToVAO(2, 1);

            _ebo = new EBO(_indices);
        }

        public void Draw(Interface.Info info)
        {
            if (Chunks.Textures is null) throw new Exception("[WARNING] Textures is null");

            Enable(EnableCap.CullFace);
            CullFace(CullFaceMode.Back);
            Enable(EnableCap.Blend);
            BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            if (!_name.Equals(info.Player.SelectedBlock))
            {
                _name = info.Player.SelectedBlock;
                for (int face = 0, index = 0; face < _uv.Count; index++)
                {
                    _uv[face] = (_uv[face].X, _uv[face].Y, Block.GetTextureIndecies(_name)[index]); face++;
                    _uv[face] = (_uv[face].X, _uv[face].Y, Block.GetTextureIndecies(_name)[index]); face++;
                    _uv[face] = (_uv[face].X, _uv[face].Y, Block.GetTextureIndecies(_name)[index]); face++;
                    _uv[face] = (_uv[face].X, _uv[face].Y, Block.GetTextureIndecies(_name)[index]); face++;
                }

                _vboUV.Bind();
                BufferSubData(BufferTarget.ArrayBuffer, 0, _uv.Count * Vector3.SizeInBytes, _uv.ToArray());
                _vboUV.Unbind();
            }

            _shader.Bind();
            _shader.SetMatrix4("projection", Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(90f), info.Player.Camera.AspectRatio, 1f, 3f));
            _shader.SetMatrix4("view", info.Player.Camera.GetViewMatrix(info.Player.Position));
            _shader.SetMatrix4("model1", Matrix4.CreateTranslation(info.Player.Position + info.Player.Camera.Front));
            _shader.SetMatrix4("model2", info.Player.Camera.GetViewMatrix(info.Player.Position).ClearTranslation().Inverted());
            _shader.SetMatrix4("model3", Matrix4.CreateTranslation(2f, -2.2f, -1.8f));
            _shader.SetMatrix4("model4", Matrix4.CreateRotationY(MathHelper.DegreesToRadians(-55f)));

            Chunks.Textures[_name].Bind();

            _vao.Bind();
            DrawElements(PrimitiveType.Triangles, _indices.Count, DrawElementsType.UnsignedInt, 0);

            Disable(EnableCap.CullFace);
            Disable(EnableCap.Blend);
        }

        public void Delete()
        {
            _ebo.Delete();
            _vboBrightness.Delete();
            _vboUV.Delete();
            _vboVertices.Delete();
            _vao.Delete();
            _shader.Delete();
        }

        private static readonly List<Vector3> _vertices =
        [
            // top
            (0f, 1f, 0f),
            (0f, 1f, 1f),
            (1f, 1f, 1f),
            (1f, 1f, 0f),

            // front
            (0f, 0f, 1f),
            (1f, 0f, 1f),
            (1f, 1f, 1f),
            (0f, 1f, 1f),

            // right
            (1f, 0f, 0f),
            (1f, 1f, 0f),
            (1f, 1f, 1f),
            (1f, 0f, 1f)
        ];
        private static readonly List<Vector3> _uv =
        [
            (0f, 1f, 0f),
            (0f, 0f, 0f),
            (1f, 0f, 0f),
            (1f, 1f, 0f),

            (0f, 0f, 0f),
            (1f, 0f, 0f),
            (1f, 1f, 0f),
            (0f, 1f, 0f),

            (1f, 0f, 0f),
            (1f, 1f, 0f),
            (0f, 1f, 0f),
            (0f, 0f, 0f)
        ];
        private static readonly List<float> _brightness =
        [
            1f, 1f, 1f, 1f,
            0.95f, 0.95f, 0.95f, 0.95f,
            0.9f, 0.9f, 0.9f, 0.9f
        ];
        private static readonly List<uint> _indices =
        [
            0, 1, 2, 2, 3, 0,
            4, 5, 6, 6, 7, 4,
            8, 9, 10, 10, 11, 8
        ];
    }
}
