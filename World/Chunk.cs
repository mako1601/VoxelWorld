using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;

using VoxelWorld.Graphics;
using static VoxelWorld.World.Block;

namespace VoxelWorld.World
{
    public class Chunk
    {
        private struct Data
        {
            public List<Vector3> Vertices { get; set; }
            public List<Vector3> UVs { get; set; }
            public List<Vector4> Lights { get; set; }
            public List<uint> Indices { get; set; }
            public uint IndexCount { get; set; }

            public VAO VAO { get; set; }
            private VBO VertexVBO { get; set; }
            private VBO UVVBO { get; set; }
            private VBO LightVBO { get; set; }
            private EBO EBO { get; set; }

            public Data()
            {
                Vertices   = [];
                UVs        = [];
                Lights     = [];
                Indices    = [];
                IndexCount = 0;
            }

            public void InitOpenGL()
            {
                VAO = new VAO();
                VertexVBO = new VBO(Vertices);
                VAO.LinkToVAO(0, 3);
                UVVBO = new VBO(UVs);
                VAO.LinkToVAO(1, 3);
                LightVBO = new VBO(Lights);
                VAO.LinkToVAO(2, 4);
                EBO = new EBO(Indices);
            }

            public void Clear()
            {
                Vertices.Clear();
                UVs.Clear();
                Lights.Clear();
                Indices.Clear();
                IndexCount = 0;
            }

            public readonly void DelOpenGL()
            {
                EBO.Delete();
                VertexVBO.Delete();
                UVVBO.Delete();
                LightVBO.Delete();
                VAO.Delete();
            }
        }

        public static Vector3i Size { get; } = new Vector3i(16, 64, 16);
        public Vector2i Position { get; }
        private Dictionary<Vector3i, Block> Blocks { get; set; }
        private Dictionary<string, Data> Info { get; set; }

        public Chunk(Vector2i position)
        {
            Position = position;
            Blocks   = [];
            Info     = [];

            // filling Blocks
            for (int y = 0; y < Size.Y; y++)
            {
                for (int x = 0; x < Size.X; x++)
                {
                    for (int z = 0; z < Size.Z; z++)
                    {
                        if (y > 28)
                        {
                            Blocks[(x, y, z)] = new Block("air", x, y, z);
                        }
                        else if (y > 27)
                        {
                            Blocks[(x, y, z)] = new Block("grass", x, y, z);
                        }
                        else if (y > 24)
                        {
                            Blocks[(x, y, z)] = new Block("dirt", x, y, z);
                        }
                        else
                        {
                            Blocks[(x, y, z)] = new Block("stone", x, y, z);
                        }
                    }
                }
            }

            // filling Lightmap
            for (int x = 0; x < Size.X; x++)
            {
                for (int z = 0; z < Size.Z; z++)
                {
                    for (int y = Size.Y - 1; y > -1; y--)
                    {
                        Block currentBlock = Blocks[(x, y, z)];

                        if (currentBlock.Type is not TypeOfBlock.Air) break;

                        currentBlock.SetLightS(0xF);
                    }
                }
            }

            // filling _data
            for (int i = 1; i < Block.Blocks.Count; i++) // i = 0 - air, but it has no texture :)
            {
                Info.Add(Block.Blocks[i], new Data());
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public void CreateLightmap()
        {
            //for (int y = 0; y < Size.Y; y++)
            //{
            //    for (int x = 0; x < Size.X; x++)
            //    {
            //        for (int z = 0; z < Size.Z; z++)
            //        {
            //            if (Blocks[(x, y, z)].IsLightSource)
            //            {
            //                Chunks.SolverR.Add(ConvertLocalToWorld(x, y, z), 15);
            //                Chunks.SolverG.Add(ConvertLocalToWorld(x, y, z), 15);
            //                Chunks.SolverB.Add(ConvertLocalToWorld(x, y, z), 15);
            //            }
            //        }
            //    }
            //}

            for (int x = 0; x < Size.X; x++)
            {
                for (int z = 0; z < Size.Z; z++)
                {
                    for (int y = Size.Y - 1; y > -1; y--)
                    {
                        Block currentBlock = Blocks[(x, y, z)];

                        if (currentBlock.IsLightPassing is false) break;

                        if (Chunks.GetLight(ConvertLocalToWorld(x, y - 1, z), 3) == 0 ||
                            Chunks.GetLight(ConvertLocalToWorld(x, y + 1, z), 3) == 0 ||
                            Chunks.GetLight(ConvertLocalToWorld(x - 1, y, z), 3) == 0 ||
                            Chunks.GetLight(ConvertLocalToWorld(x + 1, y, z), 3) == 0 ||
                            Chunks.GetLight(ConvertLocalToWorld(x, y, z - 1), 3) == 0 ||
                            Chunks.GetLight(ConvertLocalToWorld(x, y, z + 1), 3) == 0)
                        {
                            Chunks.SolverS.Add(ConvertLocalToWorld(x, y, z));
                        }

                        currentBlock.SetLightS(0xF);
                    }
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public void CreateMesh()
        {
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

            foreach (var data in Info)
            {
                Data newBD = data.Value;
                newBD.InitOpenGL();
                Info[data.Key] = newBD;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public void UpdateMesh()
        {
            // clear lists in _data
            foreach (var data in Info)
            {
                Data newData = data.Value;
                newData.Clear();
                Info[data.Key] = newData;
            }

            CreateMesh();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="textures"></param>
        public void Draw(Dictionary<string, TextureArray> textures)
        {
            foreach (var data in Info)
            {
                textures[data.Key].Bind();
                data.Value.VAO.Bind();
                GL.DrawElements(PrimitiveType.Triangles, data.Value.Indices.Count, DrawElementsType.UnsignedInt, 0);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public void Delete()
        {
            foreach (var data in Info)
            {
                Data newBD = data.Value;
                newBD.DelOpenGL();
                Info[data.Key] = newBD;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lx"></param>
        /// <param name="ly"></param>
        /// <param name="lz"></param>
        /// <returns></returns>
        public Block GetBlock(int lx, int ly, int lz) => Blocks[(lx, ly, lz)];
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lb"></param>
        /// <returns></returns>
        public Block GetBlock(Vector3i lb) => Blocks[lb];
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="lx"></param>
        /// <param name="ly"></param>
        /// <param name="lz"></param>
        /// <param name="light"></param>
        public void SetBlock(string name, int lx, int ly, int lz, ushort light = 0x0000) =>
            Blocks[(lx, ly, lz)] = new Block(name, lx, ly, lz, light);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="lb"></param>
        /// <param name="light"></param>
        public void SetBlock(string name, Vector3i lb, ushort light = 0x0000) =>
            Blocks[lb] = new Block(name, lb, light);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="newBlock"></param>
        public void SetBlock(Block newBlock) => Blocks[newBlock.Position] = newBlock;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lx"></param>
        /// <param name="ly"></param>
        /// <param name="lz"></param>
        /// <returns></returns>
        public Vector3i ConvertLocalToWorld(int lx, int ly, int lz)
        {
            int wx = lx + Position.X * Size.X;
            int wz = lz + Position.Y * Size.Z;

            return (wx, ly, wz);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lb"></param>
        /// <returns></returns>
        public Vector3i ConvertLocalToWorld(Vector3i lb)
        {
            int wx = lb.X + Position.X * Size.X;
            int wz = lb.Z + Position.Y * Size.Z;

            return (wx, lb.Y, wz);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="currentBlock"></param>
        private void SelectionOfFaces(int x, int y, int z, Block currentBlock)
        {
            IntegrateSideFaces(currentBlock, Face.Front, (x, y, z + 1), Position + ( 0,  1), (x, y, 0));
            IntegrateSideFaces(currentBlock, Face.Back,  (x, y, z - 1), Position + ( 0, -1), (x, y, Size.Z - 1));
            IntegrateSideFaces(currentBlock, Face.Left,  (x - 1, y, z), Position + (-1,  0), (Size.X - 1, y, z));
            IntegrateSideFaces(currentBlock, Face.Right, (x + 1, y, z), Position + ( 1,  0), (0, y, z));
            IntegrateTopBottomFaces(currentBlock, x, y, z);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="currentBlock"></param>
        /// <param name="face"></param>
        /// <param name="nextBlock"></param>
        /// <param name="chunkOffset"></param>
        /// <param name="borderBlock"></param>
        private void IntegrateSideFaces(Block currentBlock, Face face, Vector3i nextBlock, Vector2i chunkOffset, Vector3i borderBlock)
        {
            if ((face is Face.Front && currentBlock.Position.Z < Size.Z - 1) ||
                (face is Face.Back  && currentBlock.Position.Z > 0) ||
                (face is Face.Left  && currentBlock.Position.X > 0) ||
                (face is Face.Right && currentBlock.Position.X < Size.X - 1))
            {
                if (Blocks.TryGetValue(nextBlock, out var block) && IsFaceIntegrable(block, currentBlock))
                {
                    IntegrateFaceIntoChunk(currentBlock, face);
                }
            }
            else
            {
                Chunks.ChunksArray.TryGetValue(chunkOffset, out var chunk);

                if (chunk is null ||
                    !chunk.Blocks.TryGetValue(borderBlock, out var block) ||
                    IsFaceIntegrable(block, currentBlock))
                {
                    IntegrateFaceIntoChunk(currentBlock, face);
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="currentBlock"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="nextBlock"></param>
        /// <param name="currentblock"></param>
        /// <returns></returns>
        private static bool IsFaceIntegrable(Block? nextBlock, Block currentblock)
        {
            var type = nextBlock?.Type ?? TypeOfBlock.Air;

            return type is TypeOfBlock.Air || type is TypeOfBlock.Leaves ||
                type is TypeOfBlock.Glass && currentblock.Type is not TypeOfBlock.Glass;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="block"></param>
        /// <param name="face"></param>
        private void IntegrateFaceIntoChunk(Block block, Face face)
        {
            Info[block.Name].Vertices.AddRange(TransformedVertices(block, face));

            List<Vector2> uv = GetBlockUV(face);
            List<Vector3> UVsAndTexId = [];
            for (int i = 0; i < uv.Count; i++)
            {
                UVsAndTexId.Add(new Vector3(uv[i].X, uv[i].Y, GetTextureIndecies(block.Name)[(int)face]));
            }
            Info[block.Name].UVs.AddRange(UVsAndTexId);

            var wb = ConvertLocalToWorld(block.Position);

            float brightness;
            float lr, lr0, lr1, lr2, lr3;
            float lg, lg0, lg1, lg2, lg3;
            float lb, lb0, lb1, lb2, lb3;
            float ls, ls0, ls1, ls2, ls3;

            switch (face)
            {
                case Face.Front:
                    brightness = 0.9f;

                    lr = Chunks.GetLight(wb.X, wb.Y, wb.Z + 1, 0) / 15f;
                    lg = Chunks.GetLight(wb.X, wb.Y, wb.Z + 1, 1) / 15f;
                    lb = Chunks.GetLight(wb.X, wb.Y, wb.Z + 1, 2) / 15f;
                    ls = Chunks.GetLight(wb.X, wb.Y, wb.Z + 1, 3) / 15f;

                    lr0 = brightness / 75f * (30f * lr + Chunks.GetLight(wb.X - 1, wb.Y - 1, wb.Z + 1, 0) + Chunks.GetLight(wb.X, wb.Y - 1, wb.Z + 1, 0) + Chunks.GetLight(wb.X - 1, wb.Y, wb.Z + 1, 0));
                    lr1 = brightness / 75f * (30f * lr + Chunks.GetLight(wb.X + 1, wb.Y + 1, wb.Z + 1, 0) + Chunks.GetLight(wb.X, wb.Y + 1, wb.Z + 1, 0) + Chunks.GetLight(wb.X + 1, wb.Y, wb.Z + 1, 0));
                    lr2 = brightness / 75f * (30f * lr + Chunks.GetLight(wb.X - 1, wb.Y + 1, wb.Z + 1, 0) + Chunks.GetLight(wb.X, wb.Y + 1, wb.Z + 1, 0) + Chunks.GetLight(wb.X - 1, wb.Y, wb.Z + 1, 0));
                    lr3 = brightness / 75f * (30f * lr + Chunks.GetLight(wb.X + 1, wb.Y - 1, wb.Z + 1, 0) + Chunks.GetLight(wb.X, wb.Y - 1, wb.Z + 1, 0) + Chunks.GetLight(wb.X + 1, wb.Y, wb.Z + 1, 0));

                    lg0 = brightness / 75f * (30f * lg + Chunks.GetLight(wb.X - 1, wb.Y - 1, wb.Z + 1, 1) + Chunks.GetLight(wb.X, wb.Y - 1, wb.Z + 1, 1) + Chunks.GetLight(wb.X - 1, wb.Y, wb.Z + 1, 1));
                    lg1 = brightness / 75f * (30f * lg + Chunks.GetLight(wb.X + 1, wb.Y + 1, wb.Z + 1, 1) + Chunks.GetLight(wb.X, wb.Y + 1, wb.Z + 1, 1) + Chunks.GetLight(wb.X + 1, wb.Y, wb.Z + 1, 1));
                    lg2 = brightness / 75f * (30f * lg + Chunks.GetLight(wb.X - 1, wb.Y + 1, wb.Z + 1, 1) + Chunks.GetLight(wb.X, wb.Y + 1, wb.Z + 1, 1) + Chunks.GetLight(wb.X - 1, wb.Y, wb.Z + 1, 1));
                    lg3 = brightness / 75f * (30f * lg + Chunks.GetLight(wb.X + 1, wb.Y - 1, wb.Z + 1, 1) + Chunks.GetLight(wb.X, wb.Y - 1, wb.Z + 1, 1) + Chunks.GetLight(wb.X + 1, wb.Y, wb.Z + 1, 1));

                    lb0 = brightness / 75f * (30f * lb + Chunks.GetLight(wb.X - 1, wb.Y - 1, wb.Z + 1, 2) + Chunks.GetLight(wb.X, wb.Y - 1, wb.Z + 1, 2) + Chunks.GetLight(wb.X - 1, wb.Y, wb.Z + 1, 2));
                    lb1 = brightness / 75f * (30f * lb + Chunks.GetLight(wb.X + 1, wb.Y + 1, wb.Z + 1, 2) + Chunks.GetLight(wb.X, wb.Y + 1, wb.Z + 1, 2) + Chunks.GetLight(wb.X + 1, wb.Y, wb.Z + 1, 2));
                    lb2 = brightness / 75f * (30f * lb + Chunks.GetLight(wb.X - 1, wb.Y + 1, wb.Z + 1, 2) + Chunks.GetLight(wb.X, wb.Y + 1, wb.Z + 1, 2) + Chunks.GetLight(wb.X - 1, wb.Y, wb.Z + 1, 2));
                    lb3 = brightness / 75f * (30f * lb + Chunks.GetLight(wb.X + 1, wb.Y - 1, wb.Z + 1, 2) + Chunks.GetLight(wb.X, wb.Y - 1, wb.Z + 1, 2) + Chunks.GetLight(wb.X + 1, wb.Y, wb.Z + 1, 2));

                    ls0 = brightness / 75f * (30f * ls + Chunks.GetLight(wb.X - 1, wb.Y - 1, wb.Z + 1, 3) + Chunks.GetLight(wb.X, wb.Y - 1, wb.Z + 1, 3) + Chunks.GetLight(wb.X - 1, wb.Y, wb.Z + 1, 3));
                    ls1 = brightness / 75f * (30f * ls + Chunks.GetLight(wb.X + 1, wb.Y + 1, wb.Z + 1, 3) + Chunks.GetLight(wb.X, wb.Y + 1, wb.Z + 1, 3) + Chunks.GetLight(wb.X + 1, wb.Y, wb.Z + 1, 3));
                    ls2 = brightness / 75f * (30f * ls + Chunks.GetLight(wb.X - 1, wb.Y + 1, wb.Z + 1, 3) + Chunks.GetLight(wb.X, wb.Y + 1, wb.Z + 1, 3) + Chunks.GetLight(wb.X - 1, wb.Y, wb.Z + 1, 3));
                    ls3 = brightness / 75f * (30f * ls + Chunks.GetLight(wb.X + 1, wb.Y - 1, wb.Z + 1, 3) + Chunks.GetLight(wb.X, wb.Y - 1, wb.Z + 1, 3) + Chunks.GetLight(wb.X + 1, wb.Y, wb.Z + 1, 3));

                    Info[block.Name].Lights.Add((lr0, lg0, lb0, ls0));
                    Info[block.Name].Lights.Add((lr3, lg3, lb3, ls3));
                    Info[block.Name].Lights.Add((lr1, lg1, lb1, ls1));
                    Info[block.Name].Lights.Add((lr2, lg2, lb2, ls2));

                    AddFaceIndices(block.Name);
                    break;

                case Face.Back:
                    brightness = 0.8f;

                    lr = Chunks.GetLight(wb.X, wb.Y, wb.Z - 1, 0) / 15f;
                    lg = Chunks.GetLight(wb.X, wb.Y, wb.Z - 1, 1) / 15f;
                    lb = Chunks.GetLight(wb.X, wb.Y, wb.Z - 1, 2) / 15f;
                    ls = Chunks.GetLight(wb.X, wb.Y, wb.Z - 1, 3) / 15f;

                    lr0 = brightness / 75f * (30f * lr + Chunks.GetLight(wb.X - 1, wb.Y - 1, wb.Z - 1, 0) + Chunks.GetLight(wb.X, wb.Y - 1, wb.Z - 1, 0) + Chunks.GetLight(wb.X - 1, wb.Y, wb.Z - 1, 0));
                    lr1 = brightness / 75f * (30f * lr + Chunks.GetLight(wb.X - 1, wb.Y + 1, wb.Z - 1, 0) + Chunks.GetLight(wb.X, wb.Y + 1, wb.Z - 1, 0) + Chunks.GetLight(wb.X - 1, wb.Y, wb.Z - 1, 0));
                    lr2 = brightness / 75f * (30f * lr + Chunks.GetLight(wb.X + 1, wb.Y + 1, wb.Z - 1, 0) + Chunks.GetLight(wb.X, wb.Y + 1, wb.Z - 1, 0) + Chunks.GetLight(wb.X + 1, wb.Y, wb.Z - 1, 0));
                    lr3 = brightness / 75f * (30f * lr + Chunks.GetLight(wb.X + 1, wb.Y - 1, wb.Z - 1, 0) + Chunks.GetLight(wb.X, wb.Y - 1, wb.Z - 1, 0) + Chunks.GetLight(wb.X + 1, wb.Y, wb.Z - 1, 0));

                    lg0 = brightness / 75f * (30f * lg + Chunks.GetLight(wb.X - 1, wb.Y - 1, wb.Z - 1, 1) + Chunks.GetLight(wb.X, wb.Y - 1, wb.Z - 1, 1) + Chunks.GetLight(wb.X - 1, wb.Y, wb.Z - 1, 1));
                    lg1 = brightness / 75f * (30f * lg + Chunks.GetLight(wb.X - 1, wb.Y + 1, wb.Z - 1, 1) + Chunks.GetLight(wb.X, wb.Y + 1, wb.Z - 1, 1) + Chunks.GetLight(wb.X - 1, wb.Y, wb.Z - 1, 1));
                    lg2 = brightness / 75f * (30f * lg + Chunks.GetLight(wb.X + 1, wb.Y + 1, wb.Z - 1, 1) + Chunks.GetLight(wb.X, wb.Y + 1, wb.Z - 1, 1) + Chunks.GetLight(wb.X + 1, wb.Y, wb.Z - 1, 1));
                    lg3 = brightness / 75f * (30f * lg + Chunks.GetLight(wb.X + 1, wb.Y - 1, wb.Z - 1, 1) + Chunks.GetLight(wb.X, wb.Y - 1, wb.Z - 1, 1) + Chunks.GetLight(wb.X + 1, wb.Y, wb.Z - 1, 1));

                    lb0 = brightness / 75f * (30f * lb + Chunks.GetLight(wb.X - 1, wb.Y - 1, wb.Z - 1, 2) + Chunks.GetLight(wb.X, wb.Y - 1, wb.Z - 1, 2) + Chunks.GetLight(wb.X - 1, wb.Y, wb.Z - 1, 2));
                    lb1 = brightness / 75f * (30f * lb + Chunks.GetLight(wb.X - 1, wb.Y + 1, wb.Z - 1, 2) + Chunks.GetLight(wb.X, wb.Y + 1, wb.Z - 1, 2) + Chunks.GetLight(wb.X - 1, wb.Y, wb.Z - 1, 2));
                    lb2 = brightness / 75f * (30f * lb + Chunks.GetLight(wb.X + 1, wb.Y + 1, wb.Z - 1, 2) + Chunks.GetLight(wb.X, wb.Y + 1, wb.Z - 1, 2) + Chunks.GetLight(wb.X + 1, wb.Y, wb.Z - 1, 2));
                    lb3 = brightness / 75f * (30f * lb + Chunks.GetLight(wb.X + 1, wb.Y - 1, wb.Z - 1, 2) + Chunks.GetLight(wb.X, wb.Y - 1, wb.Z - 1, 2) + Chunks.GetLight(wb.X + 1, wb.Y, wb.Z - 1, 2));

                    ls0 = brightness / 75f * (30f * ls + Chunks.GetLight(wb.X - 1, wb.Y - 1, wb.Z - 1, 3) + Chunks.GetLight(wb.X, wb.Y - 1, wb.Z - 1, 3) + Chunks.GetLight(wb.X - 1, wb.Y, wb.Z - 1, 3));
                    ls1 = brightness / 75f * (30f * ls + Chunks.GetLight(wb.X - 1, wb.Y + 1, wb.Z - 1, 3) + Chunks.GetLight(wb.X, wb.Y + 1, wb.Z - 1, 3) + Chunks.GetLight(wb.X - 1, wb.Y, wb.Z - 1, 3));
                    ls2 = brightness / 75f * (30f * ls + Chunks.GetLight(wb.X + 1, wb.Y + 1, wb.Z - 1, 3) + Chunks.GetLight(wb.X, wb.Y + 1, wb.Z - 1, 3) + Chunks.GetLight(wb.X + 1, wb.Y, wb.Z - 1, 3));
                    ls3 = brightness / 75f * (30f * ls + Chunks.GetLight(wb.X + 1, wb.Y - 1, wb.Z - 1, 3) + Chunks.GetLight(wb.X, wb.Y - 1, wb.Z - 1, 3) + Chunks.GetLight(wb.X + 1, wb.Y, wb.Z - 1, 3));

                    Info[block.Name].Lights.Add((lr0, lg0, lb0, ls0));
                    Info[block.Name].Lights.Add((lr1, lg1, lb1, ls1));
                    Info[block.Name].Lights.Add((lr2, lg2, lb2, ls2));
                    Info[block.Name].Lights.Add((lr3, lg3, lb3, ls3));

                    AddFaceIndices(block.Name);
                    break;

                case Face.Right:
                    brightness = 0.95f;

                    lr = Chunks.GetLight(wb.X + 1, wb.Y, wb.Z, 0) / 15f;
                    lg = Chunks.GetLight(wb.X + 1, wb.Y, wb.Z, 1) / 15f;
                    lb = Chunks.GetLight(wb.X + 1, wb.Y, wb.Z, 2) / 15f;
                    ls = Chunks.GetLight(wb.X + 1, wb.Y, wb.Z, 3) / 15f;

                    lr0 = brightness / 75f * (30f * lr + Chunks.GetLight(wb.X + 1, wb.Y - 1, wb.Z - 1, 0) + Chunks.GetLight(wb.X + 1, wb.Y, wb.Z - 1, 0) + Chunks.GetLight(wb.X + 1, wb.Y - 1, wb.Z, 0));
                    lr1 = brightness / 75f * (30f * lr + Chunks.GetLight(wb.X + 1, wb.Y + 1, wb.Z - 1, 0) + Chunks.GetLight(wb.X + 1, wb.Y, wb.Z - 1, 0) + Chunks.GetLight(wb.X + 1, wb.Y + 1, wb.Z, 0));
                    lr2 = brightness / 75f * (30f * lr + Chunks.GetLight(wb.X + 1, wb.Y + 1, wb.Z + 1, 0) + Chunks.GetLight(wb.X + 1, wb.Y, wb.Z + 1, 0) + Chunks.GetLight(wb.X + 1, wb.Y + 1, wb.Z, 0));
                    lr3 = brightness / 75f * (30f * lr + Chunks.GetLight(wb.X + 1, wb.Y - 1, wb.Z + 1, 0) + Chunks.GetLight(wb.X + 1, wb.Y, wb.Z + 1, 0) + Chunks.GetLight(wb.X + 1, wb.Y - 1, wb.Z, 0));

                    lg0 = brightness / 75f * (30f * lg + Chunks.GetLight(wb.X + 1, wb.Y - 1, wb.Z - 1, 1) + Chunks.GetLight(wb.X + 1, wb.Y, wb.Z - 1, 1) + Chunks.GetLight(wb.X + 1, wb.Y - 1, wb.Z, 1));
                    lg1 = brightness / 75f * (30f * lg + Chunks.GetLight(wb.X + 1, wb.Y + 1, wb.Z - 1, 1) + Chunks.GetLight(wb.X + 1, wb.Y, wb.Z - 1, 1) + Chunks.GetLight(wb.X + 1, wb.Y + 1, wb.Z, 1));
                    lg2 = brightness / 75f * (30f * lg + Chunks.GetLight(wb.X + 1, wb.Y + 1, wb.Z + 1, 1) + Chunks.GetLight(wb.X + 1, wb.Y, wb.Z + 1, 1) + Chunks.GetLight(wb.X + 1, wb.Y + 1, wb.Z, 1));
                    lg3 = brightness / 75f * (30f * lg + Chunks.GetLight(wb.X + 1, wb.Y - 1, wb.Z + 1, 1) + Chunks.GetLight(wb.X + 1, wb.Y, wb.Z + 1, 1) + Chunks.GetLight(wb.X + 1, wb.Y - 1, wb.Z, 1));

                    lb0 = brightness / 75f * (30f * lb + Chunks.GetLight(wb.X + 1, wb.Y - 1, wb.Z - 1, 2) + Chunks.GetLight(wb.X + 1, wb.Y, wb.Z - 1, 2) + Chunks.GetLight(wb.X + 1, wb.Y - 1, wb.Z, 2));
                    lb1 = brightness / 75f * (30f * lb + Chunks.GetLight(wb.X + 1, wb.Y + 1, wb.Z - 1, 2) + Chunks.GetLight(wb.X + 1, wb.Y, wb.Z - 1, 2) + Chunks.GetLight(wb.X + 1, wb.Y + 1, wb.Z, 2));
                    lb2 = brightness / 75f * (30f * lb + Chunks.GetLight(wb.X + 1, wb.Y + 1, wb.Z + 1, 2) + Chunks.GetLight(wb.X + 1, wb.Y, wb.Z + 1, 2) + Chunks.GetLight(wb.X + 1, wb.Y + 1, wb.Z, 2));
                    lb3 = brightness / 75f * (30f * lb + Chunks.GetLight(wb.X + 1, wb.Y - 1, wb.Z + 1, 2) + Chunks.GetLight(wb.X + 1, wb.Y, wb.Z + 1, 2) + Chunks.GetLight(wb.X + 1, wb.Y - 1, wb.Z, 2));

                    ls0 = brightness / 75f * (30f * ls + Chunks.GetLight(wb.X + 1, wb.Y - 1, wb.Z - 1, 3) + Chunks.GetLight(wb.X + 1, wb.Y, wb.Z - 1, 3) + Chunks.GetLight(wb.X + 1, wb.Y - 1, wb.Z, 3));
                    ls1 = brightness / 75f * (30f * ls + Chunks.GetLight(wb.X + 1, wb.Y + 1, wb.Z - 1, 3) + Chunks.GetLight(wb.X + 1, wb.Y, wb.Z - 1, 3) + Chunks.GetLight(wb.X + 1, wb.Y + 1, wb.Z, 3));
                    ls2 = brightness / 75f * (30f * ls + Chunks.GetLight(wb.X + 1, wb.Y + 1, wb.Z + 1, 3) + Chunks.GetLight(wb.X + 1, wb.Y, wb.Z + 1, 3) + Chunks.GetLight(wb.X + 1, wb.Y + 1, wb.Z, 3));
                    ls3 = brightness / 75f * (30f * ls + Chunks.GetLight(wb.X + 1, wb.Y - 1, wb.Z + 1, 3) + Chunks.GetLight(wb.X + 1, wb.Y, wb.Z + 1, 3) + Chunks.GetLight(wb.X + 1, wb.Y - 1, wb.Z, 3));

                    Info[block.Name].Lights.Add((lr0, lg0, lb0, ls0));
                    Info[block.Name].Lights.Add((lr1, lg1, lb1, ls1));
                    Info[block.Name].Lights.Add((lr2, lg2, lb2, ls2));
                    Info[block.Name].Lights.Add((lr3, lg3, lb3, ls3));

                    AddFaceIndices(block.Name);
                    break;

                case Face.Left:
                    brightness = 0.85f;

                    lr = Chunks.GetLight(wb.X - 1, wb.Y, wb.Z, 0) / 15f;
                    lg = Chunks.GetLight(wb.X - 1, wb.Y, wb.Z, 1) / 15f;
                    lb = Chunks.GetLight(wb.X - 1, wb.Y, wb.Z, 2) / 15f;
                    ls = Chunks.GetLight(wb.X - 1, wb.Y, wb.Z, 3) / 15f;

                    lr0 = brightness / 75f * (30f * lr + Chunks.GetLight(wb.X - 1, wb.Y - 1, wb.Z - 1, 0) + Chunks.GetLight(wb.X - 1, wb.Y, wb.Z - 1, 0) + Chunks.GetLight(wb.X - 1, wb.Y - 1, wb.Z, 0));
                    lr1 = brightness / 75f * (30f * lr + Chunks.GetLight(wb.X - 1, wb.Y + 1, wb.Z + 1, 0) + Chunks.GetLight(wb.X - 1, wb.Y, wb.Z + 1, 0) + Chunks.GetLight(wb.X - 1, wb.Y + 1, wb.Z, 0));
                    lr2 = brightness / 75f * (30f * lr + Chunks.GetLight(wb.X - 1, wb.Y + 1, wb.Z - 1, 0) + Chunks.GetLight(wb.X - 1, wb.Y, wb.Z - 1, 0) + Chunks.GetLight(wb.X - 1, wb.Y + 1, wb.Z, 0));
                    lr3 = brightness / 75f * (30f * lr + Chunks.GetLight(wb.X - 1, wb.Y - 1, wb.Z + 1, 0) + Chunks.GetLight(wb.X - 1, wb.Y, wb.Z + 1, 0) + Chunks.GetLight(wb.X - 1, wb.Y - 1, wb.Z, 0));

                    lg0 = brightness / 75f * (30f * lg + Chunks.GetLight(wb.X - 1, wb.Y - 1, wb.Z - 1, 1) + Chunks.GetLight(wb.X - 1, wb.Y, wb.Z - 1, 1) + Chunks.GetLight(wb.X - 1, wb.Y - 1, wb.Z, 1));
                    lg1 = brightness / 75f * (30f * lg + Chunks.GetLight(wb.X - 1, wb.Y + 1, wb.Z + 1, 1) + Chunks.GetLight(wb.X - 1, wb.Y, wb.Z + 1, 1) + Chunks.GetLight(wb.X - 1, wb.Y + 1, wb.Z, 1));
                    lg2 = brightness / 75f * (30f * lg + Chunks.GetLight(wb.X - 1, wb.Y + 1, wb.Z - 1, 1) + Chunks.GetLight(wb.X - 1, wb.Y, wb.Z - 1, 1) + Chunks.GetLight(wb.X - 1, wb.Y + 1, wb.Z, 1));
                    lg3 = brightness / 75f * (30f * lg + Chunks.GetLight(wb.X - 1, wb.Y - 1, wb.Z + 1, 1) + Chunks.GetLight(wb.X - 1, wb.Y, wb.Z + 1, 1) + Chunks.GetLight(wb.X - 1, wb.Y - 1, wb.Z, 1));

                    lb0 = brightness / 75f * (30f * lb + Chunks.GetLight(wb.X - 1, wb.Y - 1, wb.Z - 1, 2) + Chunks.GetLight(wb.X - 1, wb.Y, wb.Z - 1, 2) + Chunks.GetLight(wb.X - 1, wb.Y - 1, wb.Z, 2));
                    lb1 = brightness / 75f * (30f * lb + Chunks.GetLight(wb.X - 1, wb.Y + 1, wb.Z + 1, 2) + Chunks.GetLight(wb.X - 1, wb.Y, wb.Z + 1, 2) + Chunks.GetLight(wb.X - 1, wb.Y + 1, wb.Z, 2));
                    lb2 = brightness / 75f * (30f * lb + Chunks.GetLight(wb.X - 1, wb.Y + 1, wb.Z - 1, 2) + Chunks.GetLight(wb.X - 1, wb.Y, wb.Z - 1, 2) + Chunks.GetLight(wb.X - 1, wb.Y + 1, wb.Z, 2));
                    lb3 = brightness / 75f * (30f * lb + Chunks.GetLight(wb.X - 1, wb.Y - 1, wb.Z + 1, 2) + Chunks.GetLight(wb.X - 1, wb.Y, wb.Z + 1, 2) + Chunks.GetLight(wb.X - 1, wb.Y - 1, wb.Z, 2));

                    ls0 = brightness / 75f * (30f * ls + Chunks.GetLight(wb.X - 1, wb.Y - 1, wb.Z - 1, 3) + Chunks.GetLight(wb.X - 1, wb.Y, wb.Z - 1, 3) + Chunks.GetLight(wb.X - 1, wb.Y - 1, wb.Z, 3));
                    ls1 = brightness / 75f * (30f * ls + Chunks.GetLight(wb.X - 1, wb.Y + 1, wb.Z + 1, 3) + Chunks.GetLight(wb.X - 1, wb.Y, wb.Z + 1, 3) + Chunks.GetLight(wb.X - 1, wb.Y + 1, wb.Z, 3));
                    ls2 = brightness / 75f * (30f * ls + Chunks.GetLight(wb.X - 1, wb.Y + 1, wb.Z - 1, 3) + Chunks.GetLight(wb.X - 1, wb.Y, wb.Z - 1, 3) + Chunks.GetLight(wb.X - 1, wb.Y + 1, wb.Z, 3));
                    ls3 = brightness / 75f * (30f * ls + Chunks.GetLight(wb.X - 1, wb.Y - 1, wb.Z + 1, 3) + Chunks.GetLight(wb.X - 1, wb.Y, wb.Z + 1, 3) + Chunks.GetLight(wb.X - 1, wb.Y - 1, wb.Z, 3));

                    Info[block.Name].Lights.Add((lr0, lg0, lb0, ls0));
                    Info[block.Name].Lights.Add((lr3, lg3, lb3, ls3));
                    Info[block.Name].Lights.Add((lr1, lg1, lb1, ls1));
                    Info[block.Name].Lights.Add((lr2, lg2, lb2, ls2));

                    AddFaceIndices(block.Name);
                    break;

                case Face.Top:
                    brightness = 1f;

                    lr = Chunks.GetLight(wb.X, wb.Y + 1, wb.Z, 0) / 15f;
                    lg = Chunks.GetLight(wb.X, wb.Y + 1, wb.Z, 1) / 15f;
                    lb = Chunks.GetLight(wb.X, wb.Y + 1, wb.Z, 2) / 15f;
                    ls = Chunks.GetLight(wb.X, wb.Y + 1, wb.Z, 3) / 15f;

                    lr0 = brightness / 75f * (30f * lr + Chunks.GetLight(wb.X - 1, wb.Y + 1, wb.Z, 0) + Chunks.GetLight(wb.X - 1, wb.Y + 1, wb.Z - 1, 0) + Chunks.GetLight(wb.X, wb.Y + 1, wb.Z - 1, 0));
                    lr1 = brightness / 75f * (30f * lr + Chunks.GetLight(wb.X - 1, wb.Y + 1, wb.Z, 0) + Chunks.GetLight(wb.X - 1, wb.Y + 1, wb.Z + 1, 0) + Chunks.GetLight(wb.X, wb.Y + 1, wb.Z + 1, 0));
                    lr2 = brightness / 75f * (30f * lr + Chunks.GetLight(wb.X + 1, wb.Y + 1, wb.Z, 0) + Chunks.GetLight(wb.X + 1, wb.Y + 1, wb.Z + 1, 0) + Chunks.GetLight(wb.X, wb.Y + 1, wb.Z + 1, 0));
                    lr3 = brightness / 75f * (30f * lr + Chunks.GetLight(wb.X + 1, wb.Y + 1, wb.Z, 0) + Chunks.GetLight(wb.X + 1, wb.Y + 1, wb.Z - 1, 0) + Chunks.GetLight(wb.X, wb.Y + 1, wb.Z - 1, 0));

                    lg0 = brightness / 75f * (30f * lg + Chunks.GetLight(wb.X - 1, wb.Y + 1, wb.Z, 1) + Chunks.GetLight(wb.X - 1, wb.Y + 1, wb.Z - 1, 1) + Chunks.GetLight(wb.X, wb.Y + 1, wb.Z - 1, 1));
                    lg1 = brightness / 75f * (30f * lg + Chunks.GetLight(wb.X - 1, wb.Y + 1, wb.Z, 1) + Chunks.GetLight(wb.X - 1, wb.Y + 1, wb.Z + 1, 1) + Chunks.GetLight(wb.X, wb.Y + 1, wb.Z + 1, 1));
                    lg2 = brightness / 75f * (30f * lg + Chunks.GetLight(wb.X + 1, wb.Y + 1, wb.Z, 1) + Chunks.GetLight(wb.X + 1, wb.Y + 1, wb.Z + 1, 1) + Chunks.GetLight(wb.X, wb.Y + 1, wb.Z + 1, 1));
                    lg3 = brightness / 75f * (30f * lg + Chunks.GetLight(wb.X + 1, wb.Y + 1, wb.Z, 1) + Chunks.GetLight(wb.X + 1, wb.Y + 1, wb.Z - 1, 1) + Chunks.GetLight(wb.X, wb.Y + 1, wb.Z - 1, 1));

                    lb0 = brightness / 75f * (30f * lb + Chunks.GetLight(wb.X - 1, wb.Y + 1, wb.Z, 2) + Chunks.GetLight(wb.X - 1, wb.Y + 1, wb.Z - 1, 2) + Chunks.GetLight(wb.X, wb.Y + 1, wb.Z - 1, 2));
                    lb1 = brightness / 75f * (30f * lb + Chunks.GetLight(wb.X - 1, wb.Y + 1, wb.Z, 2) + Chunks.GetLight(wb.X - 1, wb.Y + 1, wb.Z + 1, 2) + Chunks.GetLight(wb.X, wb.Y + 1, wb.Z + 1, 2));
                    lb2 = brightness / 75f * (30f * lb + Chunks.GetLight(wb.X + 1, wb.Y + 1, wb.Z, 2) + Chunks.GetLight(wb.X + 1, wb.Y + 1, wb.Z + 1, 2) + Chunks.GetLight(wb.X, wb.Y + 1, wb.Z + 1, 2));
                    lb3 = brightness / 75f * (30f * lb + Chunks.GetLight(wb.X + 1, wb.Y + 1, wb.Z, 2) + Chunks.GetLight(wb.X + 1, wb.Y + 1, wb.Z - 1, 2) + Chunks.GetLight(wb.X, wb.Y + 1, wb.Z - 1, 2));

                    ls0 = brightness / 75f * (30f * ls + Chunks.GetLight(wb.X - 1, wb.Y + 1, wb.Z, 3) + Chunks.GetLight(wb.X - 1, wb.Y + 1, wb.Z - 1, 3) + Chunks.GetLight(wb.X, wb.Y + 1, wb.Z - 1, 3));
                    ls1 = brightness / 75f * (30f * ls + Chunks.GetLight(wb.X - 1, wb.Y + 1, wb.Z, 3) + Chunks.GetLight(wb.X - 1, wb.Y + 1, wb.Z + 1, 3) + Chunks.GetLight(wb.X, wb.Y + 1, wb.Z + 1, 3));
                    ls2 = brightness / 75f * (30f * ls + Chunks.GetLight(wb.X + 1, wb.Y + 1, wb.Z, 3) + Chunks.GetLight(wb.X + 1, wb.Y + 1, wb.Z + 1, 3) + Chunks.GetLight(wb.X, wb.Y + 1, wb.Z + 1, 3));
                    ls3 = brightness / 75f * (30f * ls + Chunks.GetLight(wb.X + 1, wb.Y + 1, wb.Z, 3) + Chunks.GetLight(wb.X + 1, wb.Y + 1, wb.Z - 1, 3) + Chunks.GetLight(wb.X, wb.Y + 1, wb.Z - 1, 3));

                    Info[block.Name].Lights.Add((lr0, lg0, lb0, ls0));
                    Info[block.Name].Lights.Add((lr1, lg1, lb1, ls1));
                    Info[block.Name].Lights.Add((lr2, lg2, lb2, ls2));
                    Info[block.Name].Lights.Add((lr3, lg3, lb3, ls3));

                    AddFaceIndices(block.Name);
                    break;

                case Face.Bottom:
                    brightness = 0.75f;

                    lr = Chunks.GetLight(wb.X, wb.Y - 1, wb.Z, 0) / 15f;
                    lg = Chunks.GetLight(wb.X, wb.Y - 1, wb.Z, 1) / 15f;
                    lb = Chunks.GetLight(wb.X, wb.Y - 1, wb.Z, 2) / 15f;
                    ls = Chunks.GetLight(wb.X, wb.Y - 1, wb.Z, 3) / 15f;

                    lr0 = brightness / 75f * (30f * lr + Chunks.GetLight(wb.X - 1, wb.Y - 1, wb.Z - 1, 0) + Chunks.GetLight(wb.X - 1, wb.Y - 1, wb.Z, 0) + Chunks.GetLight(wb.X, wb.Y - 1, wb.Z - 1, 0));
                    lr1 = brightness / 75f * (30f * lr + Chunks.GetLight(wb.X + 1, wb.Y - 1, wb.Z + 1, 0) + Chunks.GetLight(wb.X + 1, wb.Y - 1, wb.Z, 0) + Chunks.GetLight(wb.X, wb.Y - 1, wb.Z + 1, 0));
                    lr2 = brightness / 75f * (30f * lr + Chunks.GetLight(wb.X - 1, wb.Y - 1, wb.Z + 1, 0) + Chunks.GetLight(wb.X - 1, wb.Y - 1, wb.Z, 0) + Chunks.GetLight(wb.X, wb.Y - 1, wb.Z + 1, 0));
                    lr3 = brightness / 75f * (30f * lr + Chunks.GetLight(wb.X + 1, wb.Y - 1, wb.Z - 1, 0) + Chunks.GetLight(wb.X + 1, wb.Y - 1, wb.Z, 0) + Chunks.GetLight(wb.X, wb.Y - 1, wb.Z - 1, 0));

                    lg0 = brightness / 75f * (30f * lg + Chunks.GetLight(wb.X - 1, wb.Y - 1, wb.Z - 1, 1) + Chunks.GetLight(wb.X - 1, wb.Y - 1, wb.Z, 1) + Chunks.GetLight(wb.X, wb.Y - 1, wb.Z - 1, 1));
                    lg1 = brightness / 75f * (30f * lg + Chunks.GetLight(wb.X + 1, wb.Y - 1, wb.Z + 1, 1) + Chunks.GetLight(wb.X + 1, wb.Y - 1, wb.Z, 1) + Chunks.GetLight(wb.X, wb.Y - 1, wb.Z + 1, 1));
                    lg2 = brightness / 75f * (30f * lg + Chunks.GetLight(wb.X - 1, wb.Y - 1, wb.Z + 1, 1) + Chunks.GetLight(wb.X - 1, wb.Y - 1, wb.Z, 1) + Chunks.GetLight(wb.X, wb.Y - 1, wb.Z + 1, 1));
                    lg3 = brightness / 75f * (30f * lg + Chunks.GetLight(wb.X + 1, wb.Y - 1, wb.Z - 1, 1) + Chunks.GetLight(wb.X + 1, wb.Y - 1, wb.Z, 1) + Chunks.GetLight(wb.X, wb.Y - 1, wb.Z - 1, 1));

                    lb0 = brightness / 75f * (30f * lb + Chunks.GetLight(wb.X - 1, wb.Y - 1, wb.Z - 1, 2) + Chunks.GetLight(wb.X - 1, wb.Y - 1, wb.Z, 2) + Chunks.GetLight(wb.X, wb.Y - 1, wb.Z - 1, 2));
                    lb1 = brightness / 75f * (30f * lb + Chunks.GetLight(wb.X + 1, wb.Y - 1, wb.Z + 1, 2) + Chunks.GetLight(wb.X + 1, wb.Y - 1, wb.Z, 2) + Chunks.GetLight(wb.X, wb.Y - 1, wb.Z + 1, 2));
                    lb2 = brightness / 75f * (30f * lb + Chunks.GetLight(wb.X - 1, wb.Y - 1, wb.Z + 1, 2) + Chunks.GetLight(wb.X - 1, wb.Y - 1, wb.Z, 2) + Chunks.GetLight(wb.X, wb.Y - 1, wb.Z + 1, 2));
                    lb3 = brightness / 75f * (30f * lb + Chunks.GetLight(wb.X + 1, wb.Y - 1, wb.Z - 1, 2) + Chunks.GetLight(wb.X + 1, wb.Y - 1, wb.Z, 2) + Chunks.GetLight(wb.X, wb.Y - 1, wb.Z - 1, 2));

                    ls0 = brightness / 75f * (30f * ls + Chunks.GetLight(wb.X - 1, wb.Y - 1, wb.Z - 1, 3) + Chunks.GetLight(wb.X - 1, wb.Y - 1, wb.Z, 3) + Chunks.GetLight(wb.X, wb.Y - 1, wb.Z - 1, 3));
                    ls1 = brightness / 75f * (30f * ls + Chunks.GetLight(wb.X + 1, wb.Y - 1, wb.Z + 1, 3) + Chunks.GetLight(wb.X + 1, wb.Y - 1, wb.Z, 3) + Chunks.GetLight(wb.X, wb.Y - 1, wb.Z + 1, 3));
                    ls2 = brightness / 75f * (30f * ls + Chunks.GetLight(wb.X - 1, wb.Y - 1, wb.Z + 1, 3) + Chunks.GetLight(wb.X - 1, wb.Y - 1, wb.Z, 3) + Chunks.GetLight(wb.X, wb.Y - 1, wb.Z + 1, 3));
                    ls3 = brightness / 75f * (30f * ls + Chunks.GetLight(wb.X + 1, wb.Y - 1, wb.Z - 1, 3) + Chunks.GetLight(wb.X + 1, wb.Y - 1, wb.Z, 3) + Chunks.GetLight(wb.X, wb.Y - 1, wb.Z - 1, 3));

                    Info[block.Name].Lights.Add((lr0, lg0, lb0, ls0));
                    Info[block.Name].Lights.Add((lr3, lg3, lb3, ls3));
                    Info[block.Name].Lights.Add((lr1, lg1, lb1, ls1));
                    Info[block.Name].Lights.Add((lr2, lg2, lb2, ls2));

                    AddFaceIndices(block.Name);
                    break;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="block"></param>
        /// <param name="face"></param>
        /// <returns></returns>
        private static List<Vector3> TransformedVertices(Block block, Face face) =>
            GetBlockVertices(face)
                .Select(vertex => vertex + block.Position)
                .ToList();
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        private void AddFaceIndices(string name)
        {
            uint indexCount = Info[name].IndexCount;

            Info[name].Indices.Add(0 + indexCount);
            Info[name].Indices.Add(1 + indexCount);
            Info[name].Indices.Add(2 + indexCount);
            Info[name].Indices.Add(2 + indexCount);
            Info[name].Indices.Add(3 + indexCount);
            Info[name].Indices.Add(0 + indexCount);
            
            if (Info.TryGetValue(name, out var data))
            {
                data.IndexCount += 4;
                Info[name] = data;
            } 
        }
    }
}
