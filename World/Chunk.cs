﻿using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;

using VoxelWorld.Graphics;
using static VoxelWorld.World.Block;
using System.Threading.Tasks.Dataflow;

namespace VoxelWorld.World
{
    public class Chunk
    {
        #region structs
        private struct Data
        {
            public List<Vector3> Vertices { get; set; }
            public List<Vector3> UVs { get; set; }
            public List<float> BrightnessLevels { get; set; }
            public List<uint> Indices { get; set; }
            public uint IndexCount { get; set; } = 0;

            public VAO VAO { get; set; }
            private VBO VertexVBO { get; set; }
            private VBO UVVBO { get; set; }
            private VBO BrightnessVBO { get; set; }
            private EBO EBO { get; set; }

            public Data()
            {
                Vertices         = [];
                UVs              = [];
                BrightnessLevels = [];
                Indices          = [];
            }

            public void InitOpenGL()
            {
                VAO = new VAO();
                VertexVBO = new VBO(Vertices);
                VAO.LinkToVAO(0, 3);
                UVVBO = new VBO(UVs);
                VAO.LinkToVAO(1, 3);
                BrightnessVBO = new VBO(BrightnessLevels);
                VAO.LinkToVAO(2, 1);
                EBO = new EBO(Indices);
            }

            public void ClearingLists()
            {
                Vertices.Clear();
                UVs.Clear();
                BrightnessLevels.Clear();
                Indices.Clear();
                IndexCount = 0;
            }

            public readonly void DelOpenGL()
            {
                EBO.Delete();
                VertexVBO.Delete();
                UVVBO.Delete();
                BrightnessVBO.Delete();
                VAO.Delete();
            }
        }
        #endregion

        #region properties
        public static Vector3i Size { get; } = new Vector3i(16, 64, 16);
        public Vector2i Position { get; }
        public Dictionary<Vector3i, Block> Blocks { get; private set; }
        #endregion

        #region field
        private readonly Dictionary<string, Data> _data; 
        #endregion

        #region constructor
        public Chunk(Vector2i position)
        {
            Position = position;
            Blocks   = [];
            _data    = [];

            // filling the chunk with blocks
            for (int y = 0; y < Size.Y; y++)
            {
                for (int x = 0; x < Size.X; x++)
                {
                    for (int z = 0; z < Size.Z; z++)
                    {
                        if (y > 28)
                        {
                            Blocks[(x, y, z)] = new Block("air", (x, y, z));
                        }
                        else if (y > 27)
                        {
                            Blocks[(x, y, z)] = new Block("grass", (x, y, z));
                        }
                        else if (y > 24)
                        {
                            Blocks[(x, y, z)] = new Block("dirt", (x, y, z));
                        }
                        else
                        {
                            Blocks[(x, y, z)] = new Block("stone", (x, y, z));
                        }
                    }
                }
            }
            
            for (int i = 1; i < Block.Blocks.Count; i++)
            {
                _data.Add(Block.Blocks[i], new Data());
            }
        }
        #endregion

        #region methods
        public void Generate()
        {
            if (Chunks.ChunksArray is null) throw new Exception("ChunksArray is null");

            foreach (var data in _data)
            {
                Data newBD = data.Value;
                newBD.ClearingLists();
                _data[data.Key] = newBD;
            }

            // creating a mesh
            for (int y = 0; y < Size.Y; y++)
            {
                for (int x = 0; x < Size.X; x++)
                {
                    for (int z = 0; z < Size.Z; z++)
                    {
                        Block currentBlock = Blocks[(x, y, z)];

                        if (currentBlock.Type is not TypeOfBlock.Air)
                        {
                            SelectionOfFaces(x, y, z, currentBlock);
                        }
                    }
                }
            }

            foreach (var data in _data)
            {
                Data newBD = data.Value;
                newBD.InitOpenGL();
                _data[data.Key] = newBD;
            }
        }

        public void Draw(Dictionary<string, TextureArray> textures)
        {
            foreach (var data in _data)
            {
                textures[data.Key].Bind();
                data.Value.VAO.Bind();
                GL.DrawElements(PrimitiveType.Triangles, data.Value.Indices.Count, DrawElementsType.UnsignedInt, 0);
            }
        }

        public void Delete()
        {
            foreach (var data in _data)
            {
                Data newBD = data.Value;
                newBD.DelOpenGL();
                _data[data.Key] = newBD;
            }
        }
        
        public Block GetBlock(int x, int y, int z) => Blocks[(x, y, z)];
        public Block GetBlock(Vector3i position) => Blocks[(position.X, position.Y, position.Z)];

        public void SetBlock(string name, int x, int y, int z) => Blocks[(x, y, z)] = new Block(name, x, y, z);
        public void SetBlock(string name, Vector3i position) => Blocks[(position.X, position.Y, position.Z)] = new Block(name, position);

        public Vector3i ConvertLocalToWorld(int localX, int localY, int localZ)
        {
            int worldX = localX + Position.X * Size.X;
            int worldZ = localZ + Position.Y * Size.Z;

            return new Vector3i(worldX, localY, worldZ);
        }

        public Vector3i ConvertLocalToWorld(Vector3i local)
        {
            int worldX = local.X + Position.X * Size.X;
            int worldZ = local.Z + Position.Y * Size.Z;

            return new Vector3i(worldX, local.Y, worldZ);
        }

        private void SelectionOfFaces(int x, int y, int z, Block currentBlock)
        {
            IntegrateSideFaces(currentBlock, Face.Front, (x, y, z + 1), Position + ( 0,  1), (x, y, 0));
            IntegrateSideFaces(currentBlock, Face.Back,  (x, y, z - 1), Position + ( 0, -1), (x, y, Size.Z - 1));
            IntegrateSideFaces(currentBlock, Face.Left,  (x - 1, y, z), Position + (-1,  0), (Size.X - 1, y, z));
            IntegrateSideFaces(currentBlock, Face.Right, (x + 1, y, z), Position + ( 1,  0), (0, y, z));
            IntegrateTopBottomFaces(currentBlock, x, y, z);
        }

        private void IntegrateSideFaces(Block currentBlock, Face face, Vector3i nextBlock, Vector2i chunkOffset, Vector3i borderBlock)
        {
            if (Chunks.ChunksArray is null) throw new Exception("[DEBUG] ChunksArray is null!");

            if ((face == Face.Front && currentBlock.Position.Z < Size.Z - 1) ||
                (face == Face.Back  && currentBlock.Position.Z > 0) ||
                (face == Face.Left  && currentBlock.Position.X > 0) ||
                (face == Face.Right && currentBlock.Position.X < Size.X - 1))
            {
                if (Blocks.TryGetValue(nextBlock, out var block) && IsFaceIntegrable(block, currentBlock))
                {
                    IntegrateFaceIntoChunk(currentBlock, face);
                }
            }
            else
            {
                Chunks.ChunksArray.TryGetValue(chunkOffset, out var chunk);

                if (chunk == null ||
                    !chunk.Blocks.TryGetValue(borderBlock, out var block) ||
                    IsFaceIntegrable(block, currentBlock))
                {
                    IntegrateFaceIntoChunk(currentBlock, face);
                }
            }
        }

        private void IntegrateTopBottomFaces(Block currentBlock, int x, int y, int z)
        {
            // top face
            Blocks.TryGetValue((x, y + 1, z), out var block);
            if (y < Size.Y - 1 && IsFaceIntegrable(block, currentBlock))
            {
                IntegrateFaceIntoChunk(currentBlock, Face.Top);
            }
            else if (y >= Size.Y - 1)
            {
                IntegrateFaceIntoChunk(currentBlock, Face.Top);
            }

            // bottom face
            Blocks.TryGetValue((x, y - 1, z), out block);
            if (y > 0 && IsFaceIntegrable(block, currentBlock))
            {
                IntegrateFaceIntoChunk(currentBlock, Face.Bottom);
            }
            else if (y <= 0)
            {
                IntegrateFaceIntoChunk(currentBlock, Face.Bottom);
            }
        }

        private static bool IsFaceIntegrable(Block? nextBlock, Block currentblock)
        {
            var type = nextBlock?.Type ?? TypeOfBlock.Air;

            return type is TypeOfBlock.Air || type is TypeOfBlock.Leaves ||
                type is TypeOfBlock.Glass && currentblock.Type is not TypeOfBlock.Glass;
        }

        private void IntegrateFaceIntoChunk(Block block, Face face)
        {
            _data[block.Name].Vertices.AddRange(TransformedVertices(block, face));

            List<Vector2> uv = GetBlockUV(face);
            List<Vector3> UVsAndTexId = [];
            for (int i = 0; i < uv.Count; i++)
            {
                UVsAndTexId.Add(new Vector3(uv[i].X, uv[i].Y, GetTextureIndecies(block.Name)[(int)face]));
            }
            _data[block.Name].UVs.AddRange(UVsAndTexId);

            Vector3i worldPosition = ConvertLocalToWorld(block.Position);

            // AO coefficients
            float AOFactor = 0.1f; // todo: setting option
            float a, b, c, d, e, f, g, h;
            switch (face)
            {
                case Face.Front:
                    a = IsBlocked(worldPosition.X,     worldPosition.Y + 1, worldPosition.Z + 1) * AOFactor;
                    b = IsBlocked(worldPosition.X + 1, worldPosition.Y,     worldPosition.Z + 1) * AOFactor;
                    c = IsBlocked(worldPosition.X,     worldPosition.Y - 1, worldPosition.Z + 1) * AOFactor;
                    d = IsBlocked(worldPosition.X - 1, worldPosition.Y,     worldPosition.Z + 1) * AOFactor;

                    e = IsBlocked(worldPosition.X - 1, worldPosition.Y - 1, worldPosition.Z + 1) * AOFactor;
                    f = IsBlocked(worldPosition.X + 1, worldPosition.Y - 1, worldPosition.Z + 1) * AOFactor;
                    g = IsBlocked(worldPosition.X + 1, worldPosition.Y + 1, worldPosition.Z + 1) * AOFactor;
                    h = IsBlocked(worldPosition.X - 1, worldPosition.Y + 1, worldPosition.Z + 1) * AOFactor;

                    _data[block.Name].BrightnessLevels.Add(0.9f * (1 - c - d - e));
                    _data[block.Name].BrightnessLevels.Add(0.9f * (1 - b - c - f));
                    _data[block.Name].BrightnessLevels.Add(0.9f * (1 - a - b - g));
                    _data[block.Name].BrightnessLevels.Add(0.9f * (1 - a - d - h));

                    AddFaceIndices(a, b, c, d, e, f, g, h, block.Name);

                    break;
                case Face.Back:
                    a = IsBlocked(worldPosition.X,     worldPosition.Y + 1, worldPosition.Z - 1) * AOFactor;
                    b = IsBlocked(worldPosition.X + 1, worldPosition.Y,     worldPosition.Z - 1) * AOFactor;
                    c = IsBlocked(worldPosition.X,     worldPosition.Y - 1, worldPosition.Z - 1) * AOFactor;
                    d = IsBlocked(worldPosition.X - 1, worldPosition.Y,     worldPosition.Z - 1) * AOFactor;

                    e = IsBlocked(worldPosition.X - 1, worldPosition.Y - 1, worldPosition.Z - 1) * AOFactor;
                    f = IsBlocked(worldPosition.X + 1, worldPosition.Y - 1, worldPosition.Z - 1) * AOFactor;
                    g = IsBlocked(worldPosition.X + 1, worldPosition.Y + 1, worldPosition.Z - 1) * AOFactor;
                    h = IsBlocked(worldPosition.X - 1, worldPosition.Y + 1, worldPosition.Z - 1) * AOFactor;

                    _data[block.Name].BrightnessLevels.Add(0.8f * (1 - c - d - e));
                    _data[block.Name].BrightnessLevels.Add(0.8f * (1 - a - d - h));
                    _data[block.Name].BrightnessLevels.Add(0.8f * (1 - a - b - g));
                    _data[block.Name].BrightnessLevels.Add(0.8f * (1 - b - c - f));

                    AddFaceIndices(a, b, c, d, e, f, g, h, block.Name);

                    break;
                case Face.Right:
                    a = IsBlocked(worldPosition.X + 1, worldPosition.Y + 1, worldPosition.Z    ) * AOFactor;
                    b = IsBlocked(worldPosition.X + 1, worldPosition.Y,     worldPosition.Z + 1) * AOFactor;
                    c = IsBlocked(worldPosition.X + 1, worldPosition.Y - 1, worldPosition.Z    ) * AOFactor;
                    d = IsBlocked(worldPosition.X + 1, worldPosition.Y,     worldPosition.Z - 1) * AOFactor;

                    e = IsBlocked(worldPosition.X + 1, worldPosition.Y - 1, worldPosition.Z - 1) * AOFactor;
                    f = IsBlocked(worldPosition.X + 1, worldPosition.Y - 1, worldPosition.Z + 1) * AOFactor;
                    g = IsBlocked(worldPosition.X + 1, worldPosition.Y + 1, worldPosition.Z + 1) * AOFactor;
                    h = IsBlocked(worldPosition.X + 1, worldPosition.Y + 1, worldPosition.Z - 1) * AOFactor;

                    _data[block.Name].BrightnessLevels.Add(0.95f * (1 - c - d - e));
                    _data[block.Name].BrightnessLevels.Add(0.95f * (1 - d - a - h));
                    _data[block.Name].BrightnessLevels.Add(0.95f * (1 - a - b - g));
                    _data[block.Name].BrightnessLevels.Add(0.95f * (1 - b - c - f));

                    AddFaceIndices(a, b, c, d, e, f, g, h, block.Name);

                    break;
                case Face.Left:
                    a = IsBlocked(worldPosition.X - 1, worldPosition.Y + 1, worldPosition.Z    ) * AOFactor;
                    b = IsBlocked(worldPosition.X - 1, worldPosition.Y,     worldPosition.Z + 1) * AOFactor;
                    c = IsBlocked(worldPosition.X - 1, worldPosition.Y - 1, worldPosition.Z    ) * AOFactor;
                    d = IsBlocked(worldPosition.X - 1, worldPosition.Y,     worldPosition.Z - 1) * AOFactor;

                    e = IsBlocked(worldPosition.X - 1, worldPosition.Y - 1, worldPosition.Z - 1) * AOFactor;
                    f = IsBlocked(worldPosition.X - 1, worldPosition.Y - 1, worldPosition.Z + 1) * AOFactor;
                    g = IsBlocked(worldPosition.X - 1, worldPosition.Y + 1, worldPosition.Z + 1) * AOFactor;
                    h = IsBlocked(worldPosition.X - 1, worldPosition.Y + 1, worldPosition.Z - 1) * AOFactor;

                    _data[block.Name].BrightnessLevels.Add(0.85f * (1 - c - d - e));
                    _data[block.Name].BrightnessLevels.Add(0.85f * (1 - b - c - f));
                    _data[block.Name].BrightnessLevels.Add(0.85f * (1 - a - b - g));
                    _data[block.Name].BrightnessLevels.Add(0.85f * (1 - d - a - h));

                    AddFaceIndices(a, b, c, d, e, f, g, h, block.Name);

                    break;
                case Face.Top:
                    a = IsBlocked(worldPosition.X + 1, worldPosition.Y + 1, worldPosition.Z    ) * AOFactor;
                    b = IsBlocked(worldPosition.X,     worldPosition.Y + 1, worldPosition.Z + 1) * AOFactor;
                    c = IsBlocked(worldPosition.X - 1, worldPosition.Y + 1, worldPosition.Z    ) * AOFactor;
                    d = IsBlocked(worldPosition.X,     worldPosition.Y + 1, worldPosition.Z - 1) * AOFactor;
                    
                    e = IsBlocked(worldPosition.X - 1, worldPosition.Y + 1, worldPosition.Z - 1) * AOFactor;
                    f = IsBlocked(worldPosition.X - 1, worldPosition.Y + 1, worldPosition.Z + 1) * AOFactor;
                    g = IsBlocked(worldPosition.X + 1, worldPosition.Y + 1, worldPosition.Z + 1) * AOFactor;
                    h = IsBlocked(worldPosition.X + 1, worldPosition.Y + 1, worldPosition.Z - 1) * AOFactor;

                    _data[block.Name].BrightnessLevels.Add(1 - c - d - e);
                    _data[block.Name].BrightnessLevels.Add(1 - b - c - f);
                    _data[block.Name].BrightnessLevels.Add(1 - a - b - g);
                    _data[block.Name].BrightnessLevels.Add(1 - d - a - h);

                    AddFaceIndices(a, b, c, d, e, f, g, h, block.Name);

                    break;
                case Face.Bottom:
                    a = IsBlocked(worldPosition.X + 1, worldPosition.Y - 1, worldPosition.Z    ) * AOFactor;
                    b = IsBlocked(worldPosition.X,     worldPosition.Y - 1, worldPosition.Z + 1) * AOFactor;
                    c = IsBlocked(worldPosition.X - 1, worldPosition.Y - 1, worldPosition.Z    ) * AOFactor;
                    d = IsBlocked(worldPosition.X,     worldPosition.Y - 1, worldPosition.Z - 1) * AOFactor;

                    e = IsBlocked(worldPosition.X - 1, worldPosition.Y - 1, worldPosition.Z - 1) * AOFactor;
                    f = IsBlocked(worldPosition.X - 1, worldPosition.Y - 1, worldPosition.Z + 1) * AOFactor;
                    g = IsBlocked(worldPosition.X + 1, worldPosition.Y - 1, worldPosition.Z + 1) * AOFactor;
                    h = IsBlocked(worldPosition.X + 1, worldPosition.Y - 1, worldPosition.Z - 1) * AOFactor;

                    _data[block.Name].BrightnessLevels.Add(0.75f * (1 - c - d - e));
                    _data[block.Name].BrightnessLevels.Add(0.75f * (1 - d - a - h));
                    _data[block.Name].BrightnessLevels.Add(0.75f * (1 - a - b - g));
                    _data[block.Name].BrightnessLevels.Add(0.75f * (1 - b - c - f));

                    AddFaceIndices(a, b, c, d, e, f, g, h, block.Name);

                    break;
            }
        }

        private static List<Vector3> TransformedVertices(Block block, Face face)
        {
            List<Vector3> transformedVertices = [];

            foreach (var vertex in GetBlockVertices(face))
            {
                transformedVertices.Add(vertex + block.Position);
            }

            return transformedVertices;
        }

        public static int IsBlocked(int x, int y, int z)
        {
            if (y < 0)          return 1;
            if (y > Size.Y - 1) return 0;

            Block? block = Chunks.GetBlock(x, y, z);

            if (block == null) return 0;

            return block.Type != TypeOfBlock.Air && block.Type != TypeOfBlock.Glass ? 1 : 0;
        }

        // for correct AO
        private void AddFaceIndices(float a, float b, float c, float d, float e, float f, float g, float h, string name)
        {
            // 1 block
            if (a == 0 && b == 0 && c == 0 && d == 0 && f == 0 && h == 0 && (e > 0 || g > 0))
            {
                AddRotated(name);
            }

            // 2 blocks
            else if (a > 0 && b == 0 && c == 0 && d == 0 && e == 0 && g == 0 && (f > 0 || h > 0))
            {
                AddRotated(name);
            }
            else if (a == 0 && b > 0 && c == 0 && d == 0 && e == 0 && g == 0 && (f > 0 || h > 0))
            {
                AddRotated(name);
            }
            else if (a == 0 && b == 0 && c > 0 && d == 0 && e == 0 && g == 0 && (f > 0 || h > 0))
            {
                AddRotated(name);
            }
            else if (a == 0 && b == 0 && c == 0 && d > 0 && e == 0 && g == 0 && (f > 0 || h > 0))
            {
                AddRotated(name);
            }

            // 3 blocks
            // corner
            else if (a > 0 && b > 0 && c == 0 && d == 0 && g > 0)
            {
                AddRotated(name);
            }
            else if (a == 0 && b == 0 && c > 0 && d > 0 && e > 0)
            {
                AddRotated(name);
            }

            else if (a > 0 && b > 0 && c == 0 && d == 0 && e > 0 && f == 0 && h == 0)
            {
                AddRotated(name);
            }
            else if (a == 0 && b == 0 && c > 0 && d > 0 && f == 0 && g > 0 && h == 0)
            {
                AddRotated(name);
            }

            // B B /    \ B B
            // / \ / or \ / \
            // / B /    \ B \
            else if (a > 0 && b == 0 && c > 0 && d == 0 && e == 0 && g == 0 && (f > 0 || h > 0))
            {
                AddRotated(name);
            }
            else if (a == 0 && b > 0 && c == 0 && d > 0 && e == 0 && g == 0 && (f > 0 || h > 0))
            {
                AddRotated(name);
            }

            //// \ B B    B B /
            //// B / \ or / \ B
            //// \ / \    / \ /
            else if (a > 0 && b > 0 && c == 0 && d == 0 && e == 0 && f > 0 && g == 0 && h == 0)
            {
                AddRotated(name);
            }
            else if (a == 0 && b == 0 && c > 0 && d > 0 && e == 0 && f == 0 && g == 0 && h > 0)
            {
                AddRotated(name);
            }
            else if (a > 0 && b > 0 && c == 0 && d == 0 && e == 0 && f == 0 && g == 0 && h > 0)
            {
                AddRotated(name);
            }
            else if (a == 0 && b == 0 && c > 0 && d > 0 && e == 0 && f > 0 && g == 0 && h == 0)
            {
                AddRotated(name);
            }

            // 4 blocks
            else if (a == 0 && b > 0 && c > 0 && d > 0 && g == 0 && h > 0)
            {
                AddRotated(name);
            }
            else if (a > 0 && b > 0 && c == 0 && d > 0 && e == 0 && f > 0)
            {
                AddRotated(name);
            }
            else if (a > 0 && b > 0 && c > 0 && d == 0 && e == 0 && h > 0)
            {
                AddRotated(name);
            }
            else if (a > 0 && b == 0 && c > 0 && d > 0 && g == 0 && f > 0)
            {
                AddRotated(name);
            }

            // 5 blocks
            else if (a > 0 && b > 0 && c > 0 && d > 0 && e > 0 && f == 0 && g == 0 && h == 0)
            {
                AddRotated(name);
            }
            else if (a > 0 && b > 0 && c > 0 && d > 0 && e == 0 && f == 0 && g > 0 && h == 0)
            {
                AddRotated(name);
            }

            // 6 blocks
            else if (a > 0 && b > 0 && c > 0 && d > 0 && f > 0 && h > 0 && (e == 0 || g == 0))
            {
                AddRotated(name);
            }

            else
            {
                AddDefault(name);
            }
        }

        private void AddDefault(string name)
        {
            uint indexCount = _data[name].IndexCount;

            _data[name].Indices.Add(0 + indexCount);
            _data[name].Indices.Add(1 + indexCount);
            _data[name].Indices.Add(2 + indexCount);
            _data[name].Indices.Add(2 + indexCount);
            _data[name].Indices.Add(3 + indexCount);
            _data[name].Indices.Add(0 + indexCount);
            
            if (_data.TryGetValue(name, out Data blocksData))
            {
                blocksData.IndexCount += 4;
                _data[name] = blocksData;
            }
        }

        private void AddRotated(string name)
        {
            uint indexCount = _data[name].IndexCount;

            _data[name].Indices.Add(1 + indexCount);
            _data[name].Indices.Add(2 + indexCount);
            _data[name].Indices.Add(3 + indexCount);
            _data[name].Indices.Add(3 + indexCount);
            _data[name].Indices.Add(0 + indexCount);
            _data[name].Indices.Add(1 + indexCount);
            
            if (_data.TryGetValue(name, out Data blocksData))
            {
                blocksData.IndexCount += 4;
                _data[name] = blocksData;
            }
        }
        #endregion
    }
}
