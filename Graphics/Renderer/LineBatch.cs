﻿using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;
using static OpenTK.Graphics.OpenGL.GL;

using VoxelWorld.Entity;
using VoxelWorld.World;

namespace VoxelWorld.Graphics.Renderer
{
    public class LineBatch
    {
        private readonly ShaderProgram _shader;

        private readonly VAO _chunkVAO;
        private readonly VBO _chunkVBO;

        private readonly VAO _blockVAO;
        private readonly VBO _blockVBO;
        private readonly EBO _blockEBO;

        public LineBatch()
        {
            _shader = new ShaderProgram("line.glslv", "line.glslf");

            _chunkVAO = new VAO();
            _chunkVBO = new VBO(_chunkVertices);
            VAO.LinkToVAO(0, 3);

            _blockVAO = new VAO();
            _blockVBO = new VBO(_blockVertices);
            VAO.LinkToVAO(0, 3);
            _blockEBO = new EBO(_blockIndices);
        }

        public void DrawChunkBoundaries(Color3<Rgb> color, Player player)
        {
            Enable(EnableCap.CullFace);
            CullFace(TriangleFace.Back);
            Enable(EnableCap.Blend);
            BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            var c = Chunks.GetChunkPosition((int)MathF.Floor(player.Position.X), (int)MathF.Floor(player.Position.Z));

            _shader.Bind();
            _shader.SetVector4("color", (color.X, color.Y, color.Z, 1f));
            _shader.SetMatrix4("model", Matrix4.CreateTranslation(c.X * Chunk.Size.X, player.Position.Y, c.Y * Chunk.Size.Z));
            _shader.SetMatrix4("view", player.Camera.GetViewMatrix(player.Position));
            _shader.SetMatrix4("projection", player.Camera.GetProjectionMatrix());

            _chunkVAO.Bind();

            DrawArrays(PrimitiveType.Lines, 0, _chunkVertices.Count);

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

            _shader.Bind();
            _shader.SetVector4("color", (0f, 0f, 0f, 0.5f));
            _shader.SetMatrix4("model", Matrix4.CreateTranslation(player.Camera.Ray.Block.Position));
            _shader.SetMatrix4("view", player.Camera.GetViewMatrix(player.Position));
            _shader.SetMatrix4("projection", player.Camera.GetProjectionMatrix());

            _blockVAO.Bind();

            DrawElements(PrimitiveType.Lines, _blockIndices.Count, DrawElementsType.UnsignedInt, 0);

            Disable(EnableCap.CullFace);
            Disable(EnableCap.Blend);
        }

        public void Delete()
        {
            _blockEBO.Delete();
            _blockVBO.Delete();
            _blockVAO.Delete();

            _chunkVBO.Delete();
            _chunkVAO.Delete();

            _shader.Delete();
        }

        private static readonly List<Vector3> _chunkVertices =
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

        private static readonly List<Vector3> _blockVertices =
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
        private static readonly List<uint> _blockIndices =
        [
            0, 1, 1, 2, 2, 3, 3, 0,
            0, 4, 1, 5, 2, 6, 3, 7,
            4, 5, 5, 6, 6, 7, 7, 4
        ];
    }
}