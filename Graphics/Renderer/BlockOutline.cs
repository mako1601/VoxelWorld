using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;
using static OpenTK.Graphics.OpenGL.GL;

using VoxelWorld.World;
using VoxelWorld.Entity;

namespace VoxelWorld.Graphics.Renderer
{
    public class Outline
    {
        private readonly ShaderProgram _shader;

        private readonly VAO _lineVAO;
        private readonly VBO _lineVBO;
        private readonly EBO _lineEBO;

        private readonly VAO _blockVAO;
        private readonly VBO _blockVBO;
        private readonly EBO _blockEBO;

        public Outline()
        {
            _shader = new ShaderProgram("line.glslv", "line.glslf");
            _lineVAO = new VAO();
            _lineVBO = new VBO(_lineVertices);
            VAO.LinkToVAO(0, 3);
            _lineEBO = new EBO(_lineIndices);

            _blockVAO = new VAO();
            _blockVBO = new VBO(_blockVertices);
            VAO.LinkToVAO(0, 3);
            _blockEBO = new EBO(_blockIndices);
        }

        public void Draw(Player player, Block? block)
        {
            if (block is null) return;

            Enable(EnableCap.CullFace);
            CullFace(TriangleFace.Back);
            Enable(EnableCap.Blend);
            BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            _shader.Bind();
            _shader.SetVector4("color", (0f, 0f, 0f, 0.5f));
            _shader.SetMatrix4("model", Matrix4.CreateTranslation(block.Position));
            _shader.SetMatrix4("view", player.Camera.GetViewMatrix(player.Position));
            _shader.SetMatrix4("projection", player.Camera.GetProjectionMatrix());

            _lineVAO.Bind();

            DrawElements(PrimitiveType.Lines, _lineIndices.Count, DrawElementsType.UnsignedInt, 0);

            _shader.SetVector4("color", (1f, 1f, 1f, 0.04f));
            _blockVAO.Bind();

            DrawElements(PrimitiveType.Triangles, _blockIndices.Count, DrawElementsType.UnsignedInt, 0);

            Disable(EnableCap.CullFace);
            Disable(EnableCap.Blend);
        }

        public void Delete()
        {
            _lineEBO.Delete();
            _lineVBO.Delete();
            _lineVAO.Delete();

            _blockEBO.Delete();
            _blockVBO.Delete();
            _blockVAO.Delete();

            _shader.Delete();
        }

        private static readonly List<Vector3> _lineVertices =
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
        private static readonly List<uint> _lineIndices =
        [
            0, 1, 1, 2, 2, 3, 3, 0,
            0, 4, 1, 5, 2, 6, 3, 7,
            4, 5, 5, 6, 6, 7, 7, 4
        ];

        private static readonly List<Vector3> _blockVertices =
        [
            (-0.009f, -0.009f,  1.009f),
            ( 1.009f, -0.009f,  1.009f),
            ( 1.009f,  1.009f,  1.009f),
            (-0.009f,  1.009f,  1.009f),

            (-0.009f, -0.009f, -0.009f),
            (-0.009f,  1.009f, -0.009f),
            ( 1.009f,  1.009f, -0.009f),
            ( 1.009f, -0.009f, -0.009f),

            (-0.009f, -0.009f, -0.009f),
            (-0.009f, -0.009f,  1.009f),
            (-0.009f,  1.009f,  1.009f),
            (-0.009f,  1.009f, -0.009f),

            ( 1.009f, -0.009f, -0.009f),
            ( 1.009f,  1.009f, -0.009f),
            ( 1.009f,  1.009f,  1.009f),
            ( 1.009f, -0.009f,  1.009f),

            (-0.009f,  1.009f, -0.009f),
            (-0.009f,  1.009f,  1.009f),
            ( 1.009f,  1.009f,  1.009f),
            ( 1.009f,  1.009f, -0.009f),

            (-0.009f, -0.009f, -0.009f),
            ( 1.009f, -0.009f, -0.009f),
            ( 1.009f, -0.009f,  1.009f),
            (-0.009f, -0.009f,  1.009f)
        ];
        private static readonly List<uint> _blockIndices =
        [
            0, 1, 2, 2, 3, 0,
            4, 5, 6, 6, 7, 4,
            8, 9, 10, 10, 11, 8,
            12, 13, 14, 14, 15, 12,
            16, 17, 18, 18, 19, 16,
            20, 21, 22, 22, 23, 20
        ];
    }
}
