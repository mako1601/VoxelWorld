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
        private readonly VertexArrayObject _vao;
        private readonly BufferObject<Vector3> _vboVertices;
        private readonly BufferObject<Vector2> _vboUV;
        private readonly BufferObject<float> _vboBrightness;
        private readonly BufferObject<byte> _ebo;

        private readonly List<Vector2> _uvs;

        public unsafe SelectedBlock(int id)
        {
            _id = id;

            _uvs = [];
            _uvs.AddRange(Block.GetBlockUV(_id, Block.Face.Top));
            _uvs.AddRange(Block.GetBlockUV(_id, Block.Face.Front));
            _uvs.AddRange(Block.GetBlockUV(_id, Block.Face.Right));

            _shader = new ShaderProgram("selected_block.glslv", "selected_block.glslf");

            _vao = new VertexArrayObject(0);
            _vao.Bind();

            _vboVertices = new BufferObject<Vector3>(BufferTarget.ArrayBuffer, _vertices, false);
            _vao.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0);

            _vboUV = new BufferObject<Vector2>(BufferTarget.ArrayBuffer, _uvs.ToArray(), false);
            _vao.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 0);

            _vboBrightness = new BufferObject<float>(BufferTarget.ArrayBuffer, _brightness, false);
            _vao.VertexAttribPointer(2, 1, VertexAttribPointerType.Float, false, 0);

            _ebo = new BufferObject<byte>(BufferTarget.ElementArrayBuffer, _indices, false);
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

            _shader.Use();
            _shader.SetMatrix4("uProjection", Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(90f), (float)info.Player.Camera.Width / info.Player.Camera.Height, 1f, 3f));
            _shader.SetMatrix4("uView", info.Player.Camera.GetViewMatrix(info.Player.Position));
            _shader.SetMatrix4("uModel1", Matrix4.CreateTranslation(info.Player.Position + info.Player.Camera.Front));
            _shader.SetMatrix4("uModel2", info.Player.Camera.GetViewMatrix(info.Player.Position).ClearTranslation().Inverted());
            _shader.SetMatrix4("uModel3", Matrix4.CreateTranslation(2f, -2.2f, -1.8f));
            _shader.SetMatrix4("uModel4", Matrix4.CreateRotationY(MathHelper.DegreesToRadians(-55f)));

            ChunkManager.Instance.TextureAtlas.Use(TextureUnit.Texture0);
            _vao.Bind();
            DrawElements(PrimitiveType.Triangles, _indices.Length, DrawElementsType.UnsignedByte, 0);

            Disable(EnableCap.CullFace);
            Disable(EnableCap.Blend);
        }

        public void Delete()
        {
            _ebo.Dispose();
            _vboBrightness.Dispose();
            _vboUV.Dispose();
            _vboVertices.Dispose();
            _vao.Dispose();
            _shader.Dispose();
        }

        private static readonly Vector3[] _vertices =
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
        private static readonly float[] _brightness =
        [
            1f, 1f, 1f, 1f,
            0.95f, 0.95f, 0.95f, 0.95f,
            0.9f, 0.9f, 0.9f, 0.9f
        ];
        private static readonly byte[] _indices =
        [
            0, 1, 2, 2, 3, 0,
            4, 5, 6, 6, 7, 4,
            8, 9, 10, 10, 11, 8
        ];
    }
}
