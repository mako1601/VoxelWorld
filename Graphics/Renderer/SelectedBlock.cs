using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;
using static OpenTK.Graphics.OpenGL.GL;

using VoxelWorld.World;
using VoxelWorld.Window;
using VoxelWorld.Managers;

namespace VoxelWorld.Graphics.Renderer
{
    public class SelectedBlock
    {
        private int _id;
        private readonly ShaderProgram _shader;
        private readonly VAO _vao;
        private readonly VBO _vboVertices;
        private readonly VBO _vboUV;
        private readonly VBO _vboBrightness;
        private readonly EBO _ebo;

        private readonly List<Vector2> _uvs;

        public SelectedBlock(int id)
        {
            _id = id;

            _shader = new ShaderProgram("selected_block.glslv", "selected_block.glslf");

            _vao = new VAO();

            _vboVertices = new VBO(_vertices);
            VAO.LinkToVAO(0, 3);

            _uvs = [];
            _uvs.AddRange(Block.GetBlockUV(_id, Block.Face.Top));
            _uvs.AddRange(Block.GetBlockUV(_id, Block.Face.Front));
            _uvs.AddRange(Block.GetBlockUV(_id, Block.Face.Right));

            _vboUV = new VBO(_uvs);
            VAO.LinkToVAO(1, 2);

            _vboBrightness = new VBO(_brightness);
            VAO.LinkToVAO(2, 1);

            _ebo = new EBO(_indices);
        }

        public void Draw(UI.Info info)
        {
            Enable(EnableCap.CullFace);
            CullFace(TriangleFace.Back);
            Enable(EnableCap.Blend);
            BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            if (_id != info.Player.SelectedBlock)
            {
                _id = info.Player.SelectedBlock;

                _uvs.Clear();
                _uvs.AddRange(Block.GetBlockUV(_id, Block.Face.Top));
                _uvs.AddRange(Block.GetBlockUV(_id, Block.Face.Front));
                _uvs.AddRange(Block.GetBlockUV(_id, Block.Face.Right));

                _vboUV.Bind();
                BufferSubData(BufferTarget.ArrayBuffer, 0, _uvs.Count * Vector2.SizeInBytes, _uvs.ToArray());
                _vboUV.Unbind();
            }

            _shader.Bind();
            _shader.SetMatrix4("uProjection", Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(90f), info.Player.Camera.AspectRatio, 1f, 3f));
            _shader.SetMatrix4("uView", info.Player.Camera.GetViewMatrix(info.Player.Position));
            _shader.SetMatrix4("uModel1", Matrix4.CreateTranslation(info.Player.Position + info.Player.Camera.Front));
            _shader.SetMatrix4("uModel2", info.Player.Camera.GetViewMatrix(info.Player.Position).ClearTranslation().Inverted());
            _shader.SetMatrix4("uModel3", Matrix4.CreateTranslation(2f, -2.2f, -1.8f));
            _shader.SetMatrix4("uModel4", Matrix4.CreateRotationY(MathHelper.DegreesToRadians(-55f)));

            ChunkManager.Instance.TextureAtlas.Bind();
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
            (1f, 0f, 1f),
            (1f, 0f, 0f),
            (1f, 1f, 0f),
            (1f, 1f, 1f)
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
