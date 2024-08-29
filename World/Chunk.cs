using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;

using static FastNoiseLite;

using VoxelWorld.Graphics;
using VoxelWorld.Managers;
using static VoxelWorld.World.Block;

namespace VoxelWorld.World
{
    public class Chunk
    {
        public static Vector3i Size { get; } = new Vector3i(16, 256, 16);
        public Vector2i Position { get; }
        public Block[] Blocks { get; set; }
        public bool IsMeshCreated { get; set; }

        public Block this[Vector3i position]
        {
            get
            {
                return Blocks[GetIndex(position)];
            }
            set
            {
                Blocks[GetIndex(position)] = value;
            }
        }

        public Block this[int x, int y, int z]
        {
            get
            {
                return Blocks[GetIndex(x, y, z)];
            }
            set
            {
                Blocks[GetIndex(x, y, z)] = value;
            }
        }

        private List<Vector3> _vertices;
        private List<Vector2> _uvs;
        private List<Vector4> _lights;
        private List<uint> _indices;
        private uint _indexCount;

        private VAO? _vao;
        private VBO? _vertexvbo;
        private VBO? _uvvbo;
        private VBO? _lightvbo;
        private EBO? _ebo;

        public Chunk(Vector2i position)
        {
            Position = position;
            Blocks   = new Block[Size.X * Size.Y * Size.Z];

            _vertices   = [];
            _uvs        = [];
            _lights     = [];
            _indices    = [];
            _indexCount = 0;

            IsMeshCreated = false;

            var noise = new FastNoiseLite(ChunkManager.Instance.Seed);
            noise.SetNoiseType(NoiseType.Perlin);
            noise.SetFrequency(0.2f);
            noise.SetFractalLacunarity(1.6f);
            noise.SetFractalType(FractalType.FBm);
            noise.SetFractalOctaves(6);

            for (int x = 0; x < Size.X; x++)
            {
                for (int z = 0; z < Size.Z; z++)
                {
                    float height = noise.GetNoise((position.X * Size.X + x) * 0.1f, (position.Y * Size.Z + z) * 0.1f) * 10f + 30f;

                    for (int y = 0; y < Size.Y; y++)
                    {
                        // flat generation
                        //if (y > 28)
                        //{
                        //    this[x, y, z] = new Block(0, x, y, z);
                        //}
                        //else if (y > 27)
                        //{
                        //    this[x, y, z] = new Block(3, x, y, z);
                        //}
                        //else if (y > 24)
                        //{
                        //    this[x, y, z] = new Block(2, x, y, z);
                        //}
                        //else
                        //{
                        //    this[x, y, z] = new Block(1, x, y, z);
                        //}

                        if (y == (int)height)
                        {
                            this[x, y, z] = new Block(3); // grass block
                        }
                        else if (y < (int)height && y >= (int)height - 3)
                        {
                            this[x, y, z] = new Block(2); // dirt block
                        }
                        else if (y < (int)height - 3)
                        {
                            this[x, y, z] = new Block(1); // stone block
                        }
                        else
                        {
                            this[x, y, z] = new Block(0); // air
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Creates a mesh of the chunk.
        /// </summary>
        public void CreateMesh()
        {
            IsMeshCreated = true;

            // creating a mesh
            for (int y = 0; y < Size.Y; y++)
            {
                for (int x = 0; x < Size.X; x++)
                {
                    for (int z = 0; z < Size.Z; z++)
                    {
                        Block currentBlock = Blocks[GetIndex(x, y, z)];

                        if (currentBlock.Type is not TypeOfBlock.Air)
                        {
                            SelectionOfFaces(x, y, z, currentBlock);
                        }
                    }
                }
            }

            _vao = new VAO();

            _vertexvbo = new VBO(_vertices);
            VAO.LinkToVAO(0, 3);

            _uvvbo = new VBO(_uvs);
            VAO.LinkToVAO(1, 2);

            _lightvbo = new VBO(_lights);
            VAO.LinkToVAO(2, 4);

            _ebo = new EBO(_indices);
        }
        /// <summary>
        /// Updates a mesh of the chunk.
        /// </summary>
        public void UpdateMesh()
        {
            _vertices.Clear();
            _uvs.Clear();
            _lights.Clear();
            _indices.Clear();
            _indexCount = 0;

            CreateMesh();
        }
        /// <summary>
        /// Draws the chunk.
        /// </summary>
        public void Draw()
        {
            ChunkManager.Instance.TextureAtlas.Bind();
            _vao?.Bind();
            GL.DrawElements(PrimitiveType.Triangles, _indices.Count, DrawElementsType.UnsignedInt, 0);
        }
        /// <summary>
        /// Deallocates all resources.
        /// </summary>
        public void Delete()
        {
            _ebo?.Delete();
            _lightvbo?.Delete();
            _uvvbo?.Delete();
            _vertexvbo?.Delete();
            _vao?.Delete();
        }
        /// <summary>
        /// Selects all faces of the block that the player can see.
        /// </summary>
        /// <param name="lx">Local coordinate X of the block</param>
        /// <param name="ly">Local coordinate Y of the block</param>
        /// <param name="lz">Local coordinate Z of the block</param>
        /// <param name="currentBlock">Current block</param>
        private void SelectionOfFaces(int lx, int ly, int lz, Block currentBlock)
        {
            IntegrateSideFaces(lx, ly, lz, currentBlock, Face.Front, (lx, ly, lz + 1), Position + ( 0,  1), (lx, ly, 0));
            IntegrateSideFaces(lx, ly, lz, currentBlock, Face.Back,  (lx, ly, lz - 1), Position + ( 0, -1), (lx, ly, Size.Z - 1));
            IntegrateSideFaces(lx, ly, lz, currentBlock, Face.Left,  (lx - 1, ly, lz), Position + (-1,  0), (Size.X - 1, ly, lz));
            IntegrateSideFaces(lx, ly, lz, currentBlock, Face.Right, (lx + 1, ly, lz), Position + ( 1,  0), (0, ly, lz));
            IntegrateTopBottomFaces(lx, ly, lz, currentBlock);
        }
        /// <summary>
        /// Adds side faces to a chunk.
        /// </summary>
        /// <param name="currentBlock">Current block</param>
        /// <param name="face">Face</param>
        /// <param name="nextBlockPosition">Next block</param>
        /// <param name="chunkOffset">Coordinates of the neighboring chunk</param>
        /// <param name="borderBlock">Coordinates of the outermost block in the chunk</param>
        private void IntegrateSideFaces(int lx, int ly, int lz, Block currentBlock, Face face, Vector3i nextBlockPosition, Vector2i chunkOffset, Vector3i borderBlock)
        {
            if ((face is Face.Front && lz < Size.Z - 1) ||
                (face is Face.Back  && lz > 0) ||
                (face is Face.Left  && lx > 0) ||
                (face is Face.Right && lx < Size.X - 1))
            {
                if (TryGetBlock(nextBlockPosition, out var block) && IsFaceIntegrable(block, currentBlock))
                {
                    IntegrateFaceIntoChunk(lx, ly, lz, currentBlock, face);
                }
            }
            else
            {
                ChunkManager.Instance.Chunks.TryGetValue(chunkOffset, out var chunk);

                if (chunk is null) return; // NOTE: if you need to render the side faces of the chunk, delete this and add to the bottom if 'chunk is null ||'

                if (!chunk.TryGetBlock(borderBlock, out var block) ||
                    IsFaceIntegrable(block, currentBlock))
                {
                    IntegrateFaceIntoChunk(lx, ly, lz, currentBlock, face);
                }
            }
        }
        /// <summary>
        /// Adds a bottom and top face to a chunk.
        /// </summary>  
        /// <param name="currentBlock">Current block</param>
        /// <param name="lx">Local coordinate X of the block</param>
        /// <param name="ly">Local coordinate Y of the block</param>
        /// <param name="lz">Local coordinate Z of the block</param>
        private void IntegrateTopBottomFaces(int lx, int ly, int lz, Block currentBlock)
        {
            // top face
            TryGetBlock(lx, ly + 1, lz, out var nextBlock);
            if (ly < Size.Y - 1 && IsFaceIntegrable(nextBlock, currentBlock))
            {
                IntegrateFaceIntoChunk(lx, ly, lz, currentBlock, Face.Top);
            }
            else if (ly >= Size.Y - 1)
            {
                IntegrateFaceIntoChunk(lx, ly, lz, currentBlock, Face.Top);
            }

            // bottom face
            TryGetBlock(lx, ly - 1, lz, out nextBlock);
            if (ly > 0 && IsFaceIntegrable(nextBlock, currentBlock))
            {
                IntegrateFaceIntoChunk(lx, ly, lz, currentBlock, Face.Bottom);
            }
            else if (ly < 0) // NOTE: if you need to render the bottom face of the chunk, replace < with the <=
            {
                IntegrateFaceIntoChunk(lx, ly, lz, currentBlock, Face.Bottom);
            }
        }
        /// <summary>
        /// Checks whether the next block is transparent.
        /// </summary>
        /// <param name="nextBlock">Next block</param>
        /// <param name="currentBlock">Сurrent block</param>
        /// <returns>Returns true if nextBlock.Type is a transparent block; otherwise, false.</returns>
        private static bool IsFaceIntegrable(Block? nextBlock, Block currentBlock)
        {
            var type = nextBlock?.Type ?? TypeOfBlock.Air;

            return type is TypeOfBlock.Air || type is TypeOfBlock.Leaves ||
                type is TypeOfBlock.Glass && currentBlock.Type is not TypeOfBlock.Glass;
        }
        /// <summary>
        /// Adds a face to a chunk.
        /// </summary>
        /// <param name="block">Block</param>
        /// <param name="face">Face</param>
        private void IntegrateFaceIntoChunk(int lx, int ly, int lz, Block block, Face face)
        {
            _vertices.AddRange(TransformedVertices(lx, ly, lz, face));
            _uvs.AddRange(GetBlockUV(block.ID, face));

            var wb = ConvertLocalToWorld(lx, ly, lz);

            float brightness;
            float lr, lr0, lr1, lr2, lr3;
            float lg, lg0, lg1, lg2, lg3;
            float lb, lb0, lb1, lb2, lb3;
            float ls, ls0, ls1, ls2, ls3;

            switch (face)
            {
                case Face.Front:
                    brightness = 0.9f;

                    lr = ChunkManager.GetLight(wb.X, wb.Y, wb.Z + 1, 0) / 15f;
                    lg = ChunkManager.GetLight(wb.X, wb.Y, wb.Z + 1, 1) / 15f;
                    lb = ChunkManager.GetLight(wb.X, wb.Y, wb.Z + 1, 2) / 15f;
                    ls = ChunkManager.GetLight(wb.X, wb.Y, wb.Z + 1, 3) / 15f;

                    lr0 = brightness / 75f * (30f * lr + ChunkManager.GetLight(wb.X - 1, wb.Y - 1, wb.Z + 1, 0) + ChunkManager.GetLight(wb.X, wb.Y - 1, wb.Z + 1, 0) + ChunkManager.GetLight(wb.X - 1, wb.Y, wb.Z + 1, 0));
                    lr1 = brightness / 75f * (30f * lr + ChunkManager.GetLight(wb.X + 1, wb.Y + 1, wb.Z + 1, 0) + ChunkManager.GetLight(wb.X, wb.Y + 1, wb.Z + 1, 0) + ChunkManager.GetLight(wb.X + 1, wb.Y, wb.Z + 1, 0));
                    lr2 = brightness / 75f * (30f * lr + ChunkManager.GetLight(wb.X - 1, wb.Y + 1, wb.Z + 1, 0) + ChunkManager.GetLight(wb.X, wb.Y + 1, wb.Z + 1, 0) + ChunkManager.GetLight(wb.X - 1, wb.Y, wb.Z + 1, 0));
                    lr3 = brightness / 75f * (30f * lr + ChunkManager.GetLight(wb.X + 1, wb.Y - 1, wb.Z + 1, 0) + ChunkManager.GetLight(wb.X, wb.Y - 1, wb.Z + 1, 0) + ChunkManager.GetLight(wb.X + 1, wb.Y, wb.Z + 1, 0));

                    lg0 = brightness / 75f * (30f * lg + ChunkManager.GetLight(wb.X - 1, wb.Y - 1, wb.Z + 1, 1) + ChunkManager.GetLight(wb.X, wb.Y - 1, wb.Z + 1, 1) + ChunkManager.GetLight(wb.X - 1, wb.Y, wb.Z + 1, 1));
                    lg1 = brightness / 75f * (30f * lg + ChunkManager.GetLight(wb.X + 1, wb.Y + 1, wb.Z + 1, 1) + ChunkManager.GetLight(wb.X, wb.Y + 1, wb.Z + 1, 1) + ChunkManager.GetLight(wb.X + 1, wb.Y, wb.Z + 1, 1));
                    lg2 = brightness / 75f * (30f * lg + ChunkManager.GetLight(wb.X - 1, wb.Y + 1, wb.Z + 1, 1) + ChunkManager.GetLight(wb.X, wb.Y + 1, wb.Z + 1, 1) + ChunkManager.GetLight(wb.X - 1, wb.Y, wb.Z + 1, 1));
                    lg3 = brightness / 75f * (30f * lg + ChunkManager.GetLight(wb.X + 1, wb.Y - 1, wb.Z + 1, 1) + ChunkManager.GetLight(wb.X, wb.Y - 1, wb.Z + 1, 1) + ChunkManager.GetLight(wb.X + 1, wb.Y, wb.Z + 1, 1));

                    lb0 = brightness / 75f * (30f * lb + ChunkManager.GetLight(wb.X - 1, wb.Y - 1, wb.Z + 1, 2) + ChunkManager.GetLight(wb.X, wb.Y - 1, wb.Z + 1, 2) + ChunkManager.GetLight(wb.X - 1, wb.Y, wb.Z + 1, 2));
                    lb1 = brightness / 75f * (30f * lb + ChunkManager.GetLight(wb.X + 1, wb.Y + 1, wb.Z + 1, 2) + ChunkManager.GetLight(wb.X, wb.Y + 1, wb.Z + 1, 2) + ChunkManager.GetLight(wb.X + 1, wb.Y, wb.Z + 1, 2));
                    lb2 = brightness / 75f * (30f * lb + ChunkManager.GetLight(wb.X - 1, wb.Y + 1, wb.Z + 1, 2) + ChunkManager.GetLight(wb.X, wb.Y + 1, wb.Z + 1, 2) + ChunkManager.GetLight(wb.X - 1, wb.Y, wb.Z + 1, 2));
                    lb3 = brightness / 75f * (30f * lb + ChunkManager.GetLight(wb.X + 1, wb.Y - 1, wb.Z + 1, 2) + ChunkManager.GetLight(wb.X, wb.Y - 1, wb.Z + 1, 2) + ChunkManager.GetLight(wb.X + 1, wb.Y, wb.Z + 1, 2));

                    ls0 = brightness / 75f * (30f * ls + ChunkManager.GetLight(wb.X - 1, wb.Y - 1, wb.Z + 1, 3) + ChunkManager.GetLight(wb.X, wb.Y - 1, wb.Z + 1, 3) + ChunkManager.GetLight(wb.X - 1, wb.Y, wb.Z + 1, 3));
                    ls1 = brightness / 75f * (30f * ls + ChunkManager.GetLight(wb.X + 1, wb.Y + 1, wb.Z + 1, 3) + ChunkManager.GetLight(wb.X, wb.Y + 1, wb.Z + 1, 3) + ChunkManager.GetLight(wb.X + 1, wb.Y, wb.Z + 1, 3));
                    ls2 = brightness / 75f * (30f * ls + ChunkManager.GetLight(wb.X - 1, wb.Y + 1, wb.Z + 1, 3) + ChunkManager.GetLight(wb.X, wb.Y + 1, wb.Z + 1, 3) + ChunkManager.GetLight(wb.X - 1, wb.Y, wb.Z + 1, 3));
                    ls3 = brightness / 75f * (30f * ls + ChunkManager.GetLight(wb.X + 1, wb.Y - 1, wb.Z + 1, 3) + ChunkManager.GetLight(wb.X, wb.Y - 1, wb.Z + 1, 3) + ChunkManager.GetLight(wb.X + 1, wb.Y, wb.Z + 1, 3));

                    _lights.Add((lr0, lg0, lb0, ls0));
                    _lights.Add((lr3, lg3, lb3, ls3));
                    _lights.Add((lr1, lg1, lb1, ls1));
                    _lights.Add((lr2, lg2, lb2, ls2));

                    AddFaceIndices();
                    break;

                case Face.Back:
                    brightness = 0.8f;

                    lr = ChunkManager.GetLight(wb.X, wb.Y, wb.Z - 1, 0) / 15f;
                    lg = ChunkManager.GetLight(wb.X, wb.Y, wb.Z - 1, 1) / 15f;
                    lb = ChunkManager.GetLight(wb.X, wb.Y, wb.Z - 1, 2) / 15f;
                    ls = ChunkManager.GetLight(wb.X, wb.Y, wb.Z - 1, 3) / 15f;

                    lr0 = brightness / 75f * (30f * lr + ChunkManager.GetLight(wb.X - 1, wb.Y - 1, wb.Z - 1, 0) + ChunkManager.GetLight(wb.X, wb.Y - 1, wb.Z - 1, 0) + ChunkManager.GetLight(wb.X - 1, wb.Y, wb.Z - 1, 0));
                    lr1 = brightness / 75f * (30f * lr + ChunkManager.GetLight(wb.X - 1, wb.Y + 1, wb.Z - 1, 0) + ChunkManager.GetLight(wb.X, wb.Y + 1, wb.Z - 1, 0) + ChunkManager.GetLight(wb.X - 1, wb.Y, wb.Z - 1, 0));
                    lr2 = brightness / 75f * (30f * lr + ChunkManager.GetLight(wb.X + 1, wb.Y + 1, wb.Z - 1, 0) + ChunkManager.GetLight(wb.X, wb.Y + 1, wb.Z - 1, 0) + ChunkManager.GetLight(wb.X + 1, wb.Y, wb.Z - 1, 0));
                    lr3 = brightness / 75f * (30f * lr + ChunkManager.GetLight(wb.X + 1, wb.Y - 1, wb.Z - 1, 0) + ChunkManager.GetLight(wb.X, wb.Y - 1, wb.Z - 1, 0) + ChunkManager.GetLight(wb.X + 1, wb.Y, wb.Z - 1, 0));

                    lg0 = brightness / 75f * (30f * lg + ChunkManager.GetLight(wb.X - 1, wb.Y - 1, wb.Z - 1, 1) + ChunkManager.GetLight(wb.X, wb.Y - 1, wb.Z - 1, 1) + ChunkManager.GetLight(wb.X - 1, wb.Y, wb.Z - 1, 1));
                    lg1 = brightness / 75f * (30f * lg + ChunkManager.GetLight(wb.X - 1, wb.Y + 1, wb.Z - 1, 1) + ChunkManager.GetLight(wb.X, wb.Y + 1, wb.Z - 1, 1) + ChunkManager.GetLight(wb.X - 1, wb.Y, wb.Z - 1, 1));
                    lg2 = brightness / 75f * (30f * lg + ChunkManager.GetLight(wb.X + 1, wb.Y + 1, wb.Z - 1, 1) + ChunkManager.GetLight(wb.X, wb.Y + 1, wb.Z - 1, 1) + ChunkManager.GetLight(wb.X + 1, wb.Y, wb.Z - 1, 1));
                    lg3 = brightness / 75f * (30f * lg + ChunkManager.GetLight(wb.X + 1, wb.Y - 1, wb.Z - 1, 1) + ChunkManager.GetLight(wb.X, wb.Y - 1, wb.Z - 1, 1) + ChunkManager.GetLight(wb.X + 1, wb.Y, wb.Z - 1, 1));

                    lb0 = brightness / 75f * (30f * lb + ChunkManager.GetLight(wb.X - 1, wb.Y - 1, wb.Z - 1, 2) + ChunkManager.GetLight(wb.X, wb.Y - 1, wb.Z - 1, 2) + ChunkManager.GetLight(wb.X - 1, wb.Y, wb.Z - 1, 2));
                    lb1 = brightness / 75f * (30f * lb + ChunkManager.GetLight(wb.X - 1, wb.Y + 1, wb.Z - 1, 2) + ChunkManager.GetLight(wb.X, wb.Y + 1, wb.Z - 1, 2) + ChunkManager.GetLight(wb.X - 1, wb.Y, wb.Z - 1, 2));
                    lb2 = brightness / 75f * (30f * lb + ChunkManager.GetLight(wb.X + 1, wb.Y + 1, wb.Z - 1, 2) + ChunkManager.GetLight(wb.X, wb.Y + 1, wb.Z - 1, 2) + ChunkManager.GetLight(wb.X + 1, wb.Y, wb.Z - 1, 2));
                    lb3 = brightness / 75f * (30f * lb + ChunkManager.GetLight(wb.X + 1, wb.Y - 1, wb.Z - 1, 2) + ChunkManager.GetLight(wb.X, wb.Y - 1, wb.Z - 1, 2) + ChunkManager.GetLight(wb.X + 1, wb.Y, wb.Z - 1, 2));

                    ls0 = brightness / 75f * (30f * ls + ChunkManager.GetLight(wb.X - 1, wb.Y - 1, wb.Z - 1, 3) + ChunkManager.GetLight(wb.X, wb.Y - 1, wb.Z - 1, 3) + ChunkManager.GetLight(wb.X - 1, wb.Y, wb.Z - 1, 3));
                    ls1 = brightness / 75f * (30f * ls + ChunkManager.GetLight(wb.X - 1, wb.Y + 1, wb.Z - 1, 3) + ChunkManager.GetLight(wb.X, wb.Y + 1, wb.Z - 1, 3) + ChunkManager.GetLight(wb.X - 1, wb.Y, wb.Z - 1, 3));
                    ls2 = brightness / 75f * (30f * ls + ChunkManager.GetLight(wb.X + 1, wb.Y + 1, wb.Z - 1, 3) + ChunkManager.GetLight(wb.X, wb.Y + 1, wb.Z - 1, 3) + ChunkManager.GetLight(wb.X + 1, wb.Y, wb.Z - 1, 3));
                    ls3 = brightness / 75f * (30f * ls + ChunkManager.GetLight(wb.X + 1, wb.Y - 1, wb.Z - 1, 3) + ChunkManager.GetLight(wb.X, wb.Y - 1, wb.Z - 1, 3) + ChunkManager.GetLight(wb.X + 1, wb.Y, wb.Z - 1, 3));

                    _lights.Add((lr3, lg3, lb3, ls3));
                    _lights.Add((lr0, lg0, lb0, ls0));
                    _lights.Add((lr1, lg1, lb1, ls1));
                    _lights.Add((lr2, lg2, lb2, ls2));

                    AddFaceIndices();
                    break;

                case Face.Right:
                    brightness = 0.95f;

                    lr = ChunkManager.GetLight(wb.X + 1, wb.Y, wb.Z, 0) / 15f;
                    lg = ChunkManager.GetLight(wb.X + 1, wb.Y, wb.Z, 1) / 15f;
                    lb = ChunkManager.GetLight(wb.X + 1, wb.Y, wb.Z, 2) / 15f;
                    ls = ChunkManager.GetLight(wb.X + 1, wb.Y, wb.Z, 3) / 15f;

                    lr0 = brightness / 75f * (30f * lr + ChunkManager.GetLight(wb.X + 1, wb.Y - 1, wb.Z - 1, 0) + ChunkManager.GetLight(wb.X + 1, wb.Y, wb.Z - 1, 0) + ChunkManager.GetLight(wb.X + 1, wb.Y - 1, wb.Z, 0));
                    lr1 = brightness / 75f * (30f * lr + ChunkManager.GetLight(wb.X + 1, wb.Y + 1, wb.Z - 1, 0) + ChunkManager.GetLight(wb.X + 1, wb.Y, wb.Z - 1, 0) + ChunkManager.GetLight(wb.X + 1, wb.Y + 1, wb.Z, 0));
                    lr2 = brightness / 75f * (30f * lr + ChunkManager.GetLight(wb.X + 1, wb.Y + 1, wb.Z + 1, 0) + ChunkManager.GetLight(wb.X + 1, wb.Y, wb.Z + 1, 0) + ChunkManager.GetLight(wb.X + 1, wb.Y + 1, wb.Z, 0));
                    lr3 = brightness / 75f * (30f * lr + ChunkManager.GetLight(wb.X + 1, wb.Y - 1, wb.Z + 1, 0) + ChunkManager.GetLight(wb.X + 1, wb.Y, wb.Z + 1, 0) + ChunkManager.GetLight(wb.X + 1, wb.Y - 1, wb.Z, 0));

                    lg0 = brightness / 75f * (30f * lg + ChunkManager.GetLight(wb.X + 1, wb.Y - 1, wb.Z - 1, 1) + ChunkManager.GetLight(wb.X + 1, wb.Y, wb.Z - 1, 1) + ChunkManager.GetLight(wb.X + 1, wb.Y - 1, wb.Z, 1));
                    lg1 = brightness / 75f * (30f * lg + ChunkManager.GetLight(wb.X + 1, wb.Y + 1, wb.Z - 1, 1) + ChunkManager.GetLight(wb.X + 1, wb.Y, wb.Z - 1, 1) + ChunkManager.GetLight(wb.X + 1, wb.Y + 1, wb.Z, 1));
                    lg2 = brightness / 75f * (30f * lg + ChunkManager.GetLight(wb.X + 1, wb.Y + 1, wb.Z + 1, 1) + ChunkManager.GetLight(wb.X + 1, wb.Y, wb.Z + 1, 1) + ChunkManager.GetLight(wb.X + 1, wb.Y + 1, wb.Z, 1));
                    lg3 = brightness / 75f * (30f * lg + ChunkManager.GetLight(wb.X + 1, wb.Y - 1, wb.Z + 1, 1) + ChunkManager.GetLight(wb.X + 1, wb.Y, wb.Z + 1, 1) + ChunkManager.GetLight(wb.X + 1, wb.Y - 1, wb.Z, 1));

                    lb0 = brightness / 75f * (30f * lb + ChunkManager.GetLight(wb.X + 1, wb.Y - 1, wb.Z - 1, 2) + ChunkManager.GetLight(wb.X + 1, wb.Y, wb.Z - 1, 2) + ChunkManager.GetLight(wb.X + 1, wb.Y - 1, wb.Z, 2));
                    lb1 = brightness / 75f * (30f * lb + ChunkManager.GetLight(wb.X + 1, wb.Y + 1, wb.Z - 1, 2) + ChunkManager.GetLight(wb.X + 1, wb.Y, wb.Z - 1, 2) + ChunkManager.GetLight(wb.X + 1, wb.Y + 1, wb.Z, 2));
                    lb2 = brightness / 75f * (30f * lb + ChunkManager.GetLight(wb.X + 1, wb.Y + 1, wb.Z + 1, 2) + ChunkManager.GetLight(wb.X + 1, wb.Y, wb.Z + 1, 2) + ChunkManager.GetLight(wb.X + 1, wb.Y + 1, wb.Z, 2));
                    lb3 = brightness / 75f * (30f * lb + ChunkManager.GetLight(wb.X + 1, wb.Y - 1, wb.Z + 1, 2) + ChunkManager.GetLight(wb.X + 1, wb.Y, wb.Z + 1, 2) + ChunkManager.GetLight(wb.X + 1, wb.Y - 1, wb.Z, 2));

                    ls0 = brightness / 75f * (30f * ls + ChunkManager.GetLight(wb.X + 1, wb.Y - 1, wb.Z - 1, 3) + ChunkManager.GetLight(wb.X + 1, wb.Y, wb.Z - 1, 3) + ChunkManager.GetLight(wb.X + 1, wb.Y - 1, wb.Z, 3));
                    ls1 = brightness / 75f * (30f * ls + ChunkManager.GetLight(wb.X + 1, wb.Y + 1, wb.Z - 1, 3) + ChunkManager.GetLight(wb.X + 1, wb.Y, wb.Z - 1, 3) + ChunkManager.GetLight(wb.X + 1, wb.Y + 1, wb.Z, 3));
                    ls2 = brightness / 75f * (30f * ls + ChunkManager.GetLight(wb.X + 1, wb.Y + 1, wb.Z + 1, 3) + ChunkManager.GetLight(wb.X + 1, wb.Y, wb.Z + 1, 3) + ChunkManager.GetLight(wb.X + 1, wb.Y + 1, wb.Z, 3));
                    ls3 = brightness / 75f * (30f * ls + ChunkManager.GetLight(wb.X + 1, wb.Y - 1, wb.Z + 1, 3) + ChunkManager.GetLight(wb.X + 1, wb.Y, wb.Z + 1, 3) + ChunkManager.GetLight(wb.X + 1, wb.Y - 1, wb.Z, 3));

                    _lights.Add((lr3, lg3, lb3, ls3));
                    _lights.Add((lr0, lg0, lb0, ls0));
                    _lights.Add((lr1, lg1, lb1, ls1));
                    _lights.Add((lr2, lg2, lb2, ls2));

                    AddFaceIndices();
                    break;

                case Face.Left:
                    brightness = 0.85f;

                    lr = ChunkManager.GetLight(wb.X - 1, wb.Y, wb.Z, 0) / 15f;
                    lg = ChunkManager.GetLight(wb.X - 1, wb.Y, wb.Z, 1) / 15f;
                    lb = ChunkManager.GetLight(wb.X - 1, wb.Y, wb.Z, 2) / 15f;
                    ls = ChunkManager.GetLight(wb.X - 1, wb.Y, wb.Z, 3) / 15f;

                    lr0 = brightness / 75f * (30f * lr + ChunkManager.GetLight(wb.X - 1, wb.Y - 1, wb.Z - 1, 0) + ChunkManager.GetLight(wb.X - 1, wb.Y, wb.Z - 1, 0) + ChunkManager.GetLight(wb.X - 1, wb.Y - 1, wb.Z, 0));
                    lr1 = brightness / 75f * (30f * lr + ChunkManager.GetLight(wb.X - 1, wb.Y + 1, wb.Z + 1, 0) + ChunkManager.GetLight(wb.X - 1, wb.Y, wb.Z + 1, 0) + ChunkManager.GetLight(wb.X - 1, wb.Y + 1, wb.Z, 0));
                    lr2 = brightness / 75f * (30f * lr + ChunkManager.GetLight(wb.X - 1, wb.Y + 1, wb.Z - 1, 0) + ChunkManager.GetLight(wb.X - 1, wb.Y, wb.Z - 1, 0) + ChunkManager.GetLight(wb.X - 1, wb.Y + 1, wb.Z, 0));
                    lr3 = brightness / 75f * (30f * lr + ChunkManager.GetLight(wb.X - 1, wb.Y - 1, wb.Z + 1, 0) + ChunkManager.GetLight(wb.X - 1, wb.Y, wb.Z + 1, 0) + ChunkManager.GetLight(wb.X - 1, wb.Y - 1, wb.Z, 0));

                    lg0 = brightness / 75f * (30f * lg + ChunkManager.GetLight(wb.X - 1, wb.Y - 1, wb.Z - 1, 1) + ChunkManager.GetLight(wb.X - 1, wb.Y, wb.Z - 1, 1) + ChunkManager.GetLight(wb.X - 1, wb.Y - 1, wb.Z, 1));
                    lg1 = brightness / 75f * (30f * lg + ChunkManager.GetLight(wb.X - 1, wb.Y + 1, wb.Z + 1, 1) + ChunkManager.GetLight(wb.X - 1, wb.Y, wb.Z + 1, 1) + ChunkManager.GetLight(wb.X - 1, wb.Y + 1, wb.Z, 1));
                    lg2 = brightness / 75f * (30f * lg + ChunkManager.GetLight(wb.X - 1, wb.Y + 1, wb.Z - 1, 1) + ChunkManager.GetLight(wb.X - 1, wb.Y, wb.Z - 1, 1) + ChunkManager.GetLight(wb.X - 1, wb.Y + 1, wb.Z, 1));
                    lg3 = brightness / 75f * (30f * lg + ChunkManager.GetLight(wb.X - 1, wb.Y - 1, wb.Z + 1, 1) + ChunkManager.GetLight(wb.X - 1, wb.Y, wb.Z + 1, 1) + ChunkManager.GetLight(wb.X - 1, wb.Y - 1, wb.Z, 1));

                    lb0 = brightness / 75f * (30f * lb + ChunkManager.GetLight(wb.X - 1, wb.Y - 1, wb.Z - 1, 2) + ChunkManager.GetLight(wb.X - 1, wb.Y, wb.Z - 1, 2) + ChunkManager.GetLight(wb.X - 1, wb.Y - 1, wb.Z, 2));
                    lb1 = brightness / 75f * (30f * lb + ChunkManager.GetLight(wb.X - 1, wb.Y + 1, wb.Z + 1, 2) + ChunkManager.GetLight(wb.X - 1, wb.Y, wb.Z + 1, 2) + ChunkManager.GetLight(wb.X - 1, wb.Y + 1, wb.Z, 2));
                    lb2 = brightness / 75f * (30f * lb + ChunkManager.GetLight(wb.X - 1, wb.Y + 1, wb.Z - 1, 2) + ChunkManager.GetLight(wb.X - 1, wb.Y, wb.Z - 1, 2) + ChunkManager.GetLight(wb.X - 1, wb.Y + 1, wb.Z, 2));
                    lb3 = brightness / 75f * (30f * lb + ChunkManager.GetLight(wb.X - 1, wb.Y - 1, wb.Z + 1, 2) + ChunkManager.GetLight(wb.X - 1, wb.Y, wb.Z + 1, 2) + ChunkManager.GetLight(wb.X - 1, wb.Y - 1, wb.Z, 2));

                    ls0 = brightness / 75f * (30f * ls + ChunkManager.GetLight(wb.X - 1, wb.Y - 1, wb.Z - 1, 3) + ChunkManager.GetLight(wb.X - 1, wb.Y, wb.Z - 1, 3) + ChunkManager.GetLight(wb.X - 1, wb.Y - 1, wb.Z, 3));
                    ls1 = brightness / 75f * (30f * ls + ChunkManager.GetLight(wb.X - 1, wb.Y + 1, wb.Z + 1, 3) + ChunkManager.GetLight(wb.X - 1, wb.Y, wb.Z + 1, 3) + ChunkManager.GetLight(wb.X - 1, wb.Y + 1, wb.Z, 3));
                    ls2 = brightness / 75f * (30f * ls + ChunkManager.GetLight(wb.X - 1, wb.Y + 1, wb.Z - 1, 3) + ChunkManager.GetLight(wb.X - 1, wb.Y, wb.Z - 1, 3) + ChunkManager.GetLight(wb.X - 1, wb.Y + 1, wb.Z, 3));
                    ls3 = brightness / 75f * (30f * ls + ChunkManager.GetLight(wb.X - 1, wb.Y - 1, wb.Z + 1, 3) + ChunkManager.GetLight(wb.X - 1, wb.Y, wb.Z + 1, 3) + ChunkManager.GetLight(wb.X - 1, wb.Y - 1, wb.Z, 3));

                    _lights.Add((lr0, lg0, lb0, ls0));
                    _lights.Add((lr3, lg3, lb3, ls3));
                    _lights.Add((lr1, lg1, lb1, ls1));
                    _lights.Add((lr2, lg2, lb2, ls2));

                    AddFaceIndices();
                    break;

                case Face.Top:
                    brightness = 1f;

                    lr = ChunkManager.GetLight(wb.X, wb.Y + 1, wb.Z, 0) / 15f;
                    lg = ChunkManager.GetLight(wb.X, wb.Y + 1, wb.Z, 1) / 15f;
                    lb = ChunkManager.GetLight(wb.X, wb.Y + 1, wb.Z, 2) / 15f;
                    ls = ChunkManager.GetLight(wb.X, wb.Y + 1, wb.Z, 3) / 15f;

                    lr0 = brightness / 75f * (30f * lr + ChunkManager.GetLight(wb.X - 1, wb.Y + 1, wb.Z, 0) + ChunkManager.GetLight(wb.X - 1, wb.Y + 1, wb.Z - 1, 0) + ChunkManager.GetLight(wb.X, wb.Y + 1, wb.Z - 1, 0));
                    lr1 = brightness / 75f * (30f * lr + ChunkManager.GetLight(wb.X - 1, wb.Y + 1, wb.Z, 0) + ChunkManager.GetLight(wb.X - 1, wb.Y + 1, wb.Z + 1, 0) + ChunkManager.GetLight(wb.X, wb.Y + 1, wb.Z + 1, 0));
                    lr2 = brightness / 75f * (30f * lr + ChunkManager.GetLight(wb.X + 1, wb.Y + 1, wb.Z, 0) + ChunkManager.GetLight(wb.X + 1, wb.Y + 1, wb.Z + 1, 0) + ChunkManager.GetLight(wb.X, wb.Y + 1, wb.Z + 1, 0));
                    lr3 = brightness / 75f * (30f * lr + ChunkManager.GetLight(wb.X + 1, wb.Y + 1, wb.Z, 0) + ChunkManager.GetLight(wb.X + 1, wb.Y + 1, wb.Z - 1, 0) + ChunkManager.GetLight(wb.X, wb.Y + 1, wb.Z - 1, 0));

                    lg0 = brightness / 75f * (30f * lg + ChunkManager.GetLight(wb.X - 1, wb.Y + 1, wb.Z, 1) + ChunkManager.GetLight(wb.X - 1, wb.Y + 1, wb.Z - 1, 1) + ChunkManager.GetLight(wb.X, wb.Y + 1, wb.Z - 1, 1));
                    lg1 = brightness / 75f * (30f * lg + ChunkManager.GetLight(wb.X - 1, wb.Y + 1, wb.Z, 1) + ChunkManager.GetLight(wb.X - 1, wb.Y + 1, wb.Z + 1, 1) + ChunkManager.GetLight(wb.X, wb.Y + 1, wb.Z + 1, 1));
                    lg2 = brightness / 75f * (30f * lg + ChunkManager.GetLight(wb.X + 1, wb.Y + 1, wb.Z, 1) + ChunkManager.GetLight(wb.X + 1, wb.Y + 1, wb.Z + 1, 1) + ChunkManager.GetLight(wb.X, wb.Y + 1, wb.Z + 1, 1));
                    lg3 = brightness / 75f * (30f * lg + ChunkManager.GetLight(wb.X + 1, wb.Y + 1, wb.Z, 1) + ChunkManager.GetLight(wb.X + 1, wb.Y + 1, wb.Z - 1, 1) + ChunkManager.GetLight(wb.X, wb.Y + 1, wb.Z - 1, 1));

                    lb0 = brightness / 75f * (30f * lb + ChunkManager.GetLight(wb.X - 1, wb.Y + 1, wb.Z, 2) + ChunkManager.GetLight(wb.X - 1, wb.Y + 1, wb.Z - 1, 2) + ChunkManager.GetLight(wb.X, wb.Y + 1, wb.Z - 1, 2));
                    lb1 = brightness / 75f * (30f * lb + ChunkManager.GetLight(wb.X - 1, wb.Y + 1, wb.Z, 2) + ChunkManager.GetLight(wb.X - 1, wb.Y + 1, wb.Z + 1, 2) + ChunkManager.GetLight(wb.X, wb.Y + 1, wb.Z + 1, 2));
                    lb2 = brightness / 75f * (30f * lb + ChunkManager.GetLight(wb.X + 1, wb.Y + 1, wb.Z, 2) + ChunkManager.GetLight(wb.X + 1, wb.Y + 1, wb.Z + 1, 2) + ChunkManager.GetLight(wb.X, wb.Y + 1, wb.Z + 1, 2));
                    lb3 = brightness / 75f * (30f * lb + ChunkManager.GetLight(wb.X + 1, wb.Y + 1, wb.Z, 2) + ChunkManager.GetLight(wb.X + 1, wb.Y + 1, wb.Z - 1, 2) + ChunkManager.GetLight(wb.X, wb.Y + 1, wb.Z - 1, 2));

                    ls0 = brightness / 75f * (30f * ls + ChunkManager.GetLight(wb.X - 1, wb.Y + 1, wb.Z, 3) + ChunkManager.GetLight(wb.X - 1, wb.Y + 1, wb.Z - 1, 3) + ChunkManager.GetLight(wb.X, wb.Y + 1, wb.Z - 1, 3));
                    ls1 = brightness / 75f * (30f * ls + ChunkManager.GetLight(wb.X - 1, wb.Y + 1, wb.Z, 3) + ChunkManager.GetLight(wb.X - 1, wb.Y + 1, wb.Z + 1, 3) + ChunkManager.GetLight(wb.X, wb.Y + 1, wb.Z + 1, 3));
                    ls2 = brightness / 75f * (30f * ls + ChunkManager.GetLight(wb.X + 1, wb.Y + 1, wb.Z, 3) + ChunkManager.GetLight(wb.X + 1, wb.Y + 1, wb.Z + 1, 3) + ChunkManager.GetLight(wb.X, wb.Y + 1, wb.Z + 1, 3));
                    ls3 = brightness / 75f * (30f * ls + ChunkManager.GetLight(wb.X + 1, wb.Y + 1, wb.Z, 3) + ChunkManager.GetLight(wb.X + 1, wb.Y + 1, wb.Z - 1, 3) + ChunkManager.GetLight(wb.X, wb.Y + 1, wb.Z - 1, 3));

                    _lights.Add((lr0, lg0, lb0, ls0));
                    _lights.Add((lr1, lg1, lb1, ls1));
                    _lights.Add((lr2, lg2, lb2, ls2));
                    _lights.Add((lr3, lg3, lb3, ls3));

                    AddFaceIndices();
                    break;

                case Face.Bottom:
                    brightness = 0.75f;

                    lr = ChunkManager.GetLight(wb.X, wb.Y - 1, wb.Z, 0) / 15f;
                    lg = ChunkManager.GetLight(wb.X, wb.Y - 1, wb.Z, 1) / 15f;
                    lb = ChunkManager.GetLight(wb.X, wb.Y - 1, wb.Z, 2) / 15f;
                    ls = ChunkManager.GetLight(wb.X, wb.Y - 1, wb.Z, 3) / 15f;

                    lr0 = brightness / 75f * (30f * lr + ChunkManager.GetLight(wb.X - 1, wb.Y - 1, wb.Z - 1, 0) + ChunkManager.GetLight(wb.X - 1, wb.Y - 1, wb.Z, 0) + ChunkManager.GetLight(wb.X, wb.Y - 1, wb.Z - 1, 0));
                    lr1 = brightness / 75f * (30f * lr + ChunkManager.GetLight(wb.X + 1, wb.Y - 1, wb.Z + 1, 0) + ChunkManager.GetLight(wb.X + 1, wb.Y - 1, wb.Z, 0) + ChunkManager.GetLight(wb.X, wb.Y - 1, wb.Z + 1, 0));
                    lr2 = brightness / 75f * (30f * lr + ChunkManager.GetLight(wb.X - 1, wb.Y - 1, wb.Z + 1, 0) + ChunkManager.GetLight(wb.X - 1, wb.Y - 1, wb.Z, 0) + ChunkManager.GetLight(wb.X, wb.Y - 1, wb.Z + 1, 0));
                    lr3 = brightness / 75f * (30f * lr + ChunkManager.GetLight(wb.X + 1, wb.Y - 1, wb.Z - 1, 0) + ChunkManager.GetLight(wb.X + 1, wb.Y - 1, wb.Z, 0) + ChunkManager.GetLight(wb.X, wb.Y - 1, wb.Z - 1, 0));

                    lg0 = brightness / 75f * (30f * lg + ChunkManager.GetLight(wb.X - 1, wb.Y - 1, wb.Z - 1, 1) + ChunkManager.GetLight(wb.X - 1, wb.Y - 1, wb.Z, 1) + ChunkManager.GetLight(wb.X, wb.Y - 1, wb.Z - 1, 1));
                    lg1 = brightness / 75f * (30f * lg + ChunkManager.GetLight(wb.X + 1, wb.Y - 1, wb.Z + 1, 1) + ChunkManager.GetLight(wb.X + 1, wb.Y - 1, wb.Z, 1) + ChunkManager.GetLight(wb.X, wb.Y - 1, wb.Z + 1, 1));
                    lg2 = brightness / 75f * (30f * lg + ChunkManager.GetLight(wb.X - 1, wb.Y - 1, wb.Z + 1, 1) + ChunkManager.GetLight(wb.X - 1, wb.Y - 1, wb.Z, 1) + ChunkManager.GetLight(wb.X, wb.Y - 1, wb.Z + 1, 1));
                    lg3 = brightness / 75f * (30f * lg + ChunkManager.GetLight(wb.X + 1, wb.Y - 1, wb.Z - 1, 1) + ChunkManager.GetLight(wb.X + 1, wb.Y - 1, wb.Z, 1) + ChunkManager.GetLight(wb.X, wb.Y - 1, wb.Z - 1, 1));

                    lb0 = brightness / 75f * (30f * lb + ChunkManager.GetLight(wb.X - 1, wb.Y - 1, wb.Z - 1, 2) + ChunkManager.GetLight(wb.X - 1, wb.Y - 1, wb.Z, 2) + ChunkManager.GetLight(wb.X, wb.Y - 1, wb.Z - 1, 2));
                    lb1 = brightness / 75f * (30f * lb + ChunkManager.GetLight(wb.X + 1, wb.Y - 1, wb.Z + 1, 2) + ChunkManager.GetLight(wb.X + 1, wb.Y - 1, wb.Z, 2) + ChunkManager.GetLight(wb.X, wb.Y - 1, wb.Z + 1, 2));
                    lb2 = brightness / 75f * (30f * lb + ChunkManager.GetLight(wb.X - 1, wb.Y - 1, wb.Z + 1, 2) + ChunkManager.GetLight(wb.X - 1, wb.Y - 1, wb.Z, 2) + ChunkManager.GetLight(wb.X, wb.Y - 1, wb.Z + 1, 2));
                    lb3 = brightness / 75f * (30f * lb + ChunkManager.GetLight(wb.X + 1, wb.Y - 1, wb.Z - 1, 2) + ChunkManager.GetLight(wb.X + 1, wb.Y - 1, wb.Z, 2) + ChunkManager.GetLight(wb.X, wb.Y - 1, wb.Z - 1, 2));

                    ls0 = brightness / 75f * (30f * ls + ChunkManager.GetLight(wb.X - 1, wb.Y - 1, wb.Z - 1, 3) + ChunkManager.GetLight(wb.X - 1, wb.Y - 1, wb.Z, 3) + ChunkManager.GetLight(wb.X, wb.Y - 1, wb.Z - 1, 3));
                    ls1 = brightness / 75f * (30f * ls + ChunkManager.GetLight(wb.X + 1, wb.Y - 1, wb.Z + 1, 3) + ChunkManager.GetLight(wb.X + 1, wb.Y - 1, wb.Z, 3) + ChunkManager.GetLight(wb.X, wb.Y - 1, wb.Z + 1, 3));
                    ls2 = brightness / 75f * (30f * ls + ChunkManager.GetLight(wb.X - 1, wb.Y - 1, wb.Z + 1, 3) + ChunkManager.GetLight(wb.X - 1, wb.Y - 1, wb.Z, 3) + ChunkManager.GetLight(wb.X, wb.Y - 1, wb.Z + 1, 3));
                    ls3 = brightness / 75f * (30f * ls + ChunkManager.GetLight(wb.X + 1, wb.Y - 1, wb.Z - 1, 3) + ChunkManager.GetLight(wb.X + 1, wb.Y - 1, wb.Z, 3) + ChunkManager.GetLight(wb.X, wb.Y - 1, wb.Z - 1, 3));

                    _lights.Add((lr2, lg2, lb2, ls2));
                    _lights.Add((lr0, lg0, lb0, ls0));
                    _lights.Add((lr3, lg3, lb3, ls3));
                    _lights.Add((lr1, lg1, lb1, ls1));

                    AddFaceIndices();
                    break;
            }
        }
        /// <summary>
        /// Converts the coordinates of the vertices of a block face to the specified coordinates.
        /// </summary>
        /// <param name="position">Block position</param>
        /// <param name="face">Block face</param>
        /// <returns>Coordinates of block face vertices.<Vector3> </returns>
        private static List<Vector3> TransformedVertices(int lx, int ly, int lz, Face face) =>
            GetBlockVertices(face)
                .Select(vertex => vertex + (lx, ly, lz))
                .ToList();
        /// <summary>
        /// Fills Indices with values.
        /// </summary>
        private void AddFaceIndices()
        {
            _indices.Add(0 + _indexCount);
            _indices.Add(1 + _indexCount);
            _indices.Add(2 + _indexCount);
            _indices.Add(2 + _indexCount);
            _indices.Add(3 + _indexCount);
            _indices.Add(0 + _indexCount);

            _indexCount += 4;
        }
        private bool TryGetBlock(int x, int y, int z, out Block? block)
        {
            if (x >= 0 && x < Size.X &&
                y >= 0 && y < Size.Y &&
                z >= 0 && z < Size.Z)
            {
                block = this[x, y, z];
                return true;
            }
            else
            {
                block = null;
                return false;
            }
        }
        private bool TryGetBlock(Vector3i position, out Block? block)
        {
            if (position.X >= 0 && position.X < Size.X &&
                position.Y >= 0 && position.Y < Size.Y &&
                position.Z >= 0 && position.Z < Size.Z)
            {
                block = this[position];
                return true;
            }
            else
            {
                block = null;
                return false;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public static int GetIndex(int x, int y, int z) =>
            x + (y * Size.X) + (z * Size.X * Size.Y);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lb"></param>
        /// <returns></returns>
        public static int GetIndex(Vector3i lb) =>
            lb.X + (lb.Y * Size.X) + (lb.Z * Size.X * Size.Y);
        /// <summary>
        /// Converts local block coordinates to world coordinates.
        /// </summary>
        /// <param name="lx">Local coordinate X of the block</param>
        /// <param name="ly">Local coordinate Y of the block</param>
        /// <param name="lz">Local coordinate Z of the block</param>
        /// <returns>Vector of world coordinates of the block.</returns>
        public Vector3i ConvertLocalToWorld(int lx, int ly, int lz) =>
            (lx + Position.X * Size.X, ly, lz + Position.Y * Size.Z);
        /// <summary>
        /// Converts local block coordinates to world coordinates.
        /// </summary>
        /// <param name="lb">Local coordinates of the block</param>
        /// <returns>Vector of world coordinates of the block.</returns>
        public Vector3i ConvertLocalToWorld(Vector3i lb) =>
            (lb.X + Position.X * Size.X, lb.Y, lb.Z + Position.Y * Size.Z);
    }
}
