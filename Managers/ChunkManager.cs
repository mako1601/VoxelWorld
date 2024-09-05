using System.IO.Compression;

using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;
using static OpenTK.Graphics.OpenGL.GL;

using VoxelWorld.World;
using VoxelWorld.Entity;
using VoxelWorld.Graphics;
using VoxelWorld.Graphics.Renderer;
using System.Diagnostics;

namespace VoxelWorld.Managers
{
    public struct ChunkInfo
    {
        public Chunk? Chunk { get; set; }
        public Lightmap? Lightmap { get; set; }
    }

    public class ChunkManager
    {
        private static readonly Lazy<ChunkManager> _instance = new(() => new ChunkManager());
        public static ChunkManager Instance => _instance.Value;

        public ShaderProgram Shader { get; private set; }
        public Texture TextureAtlas { get; private set; }

        public Dictionary<Vector2i, ChunkInfo> Chunks { get; private set; }

        public LightSolver SolverR { get; private set; }
        public LightSolver SolverG { get; private set; }
        public LightSolver SolverB { get; private set; }
        public LightSolver SolverS { get; private set; }

        public Queue<Vector2i> AddQueue { get; private set; }
        public Queue<Vector2i> RemoveQueue { get; private set; }
        public HashSet<Vector2i> CreateMesh { get; private set; }
        public HashSet<Vector2i> UpdateMesh { get; private set; }
        public HashSet<Vector2i> VisibleChunks { get; private set; }

        public int Seed { get; private set; }
        public static byte RenderDistance { get; set; } = 8;

        private ChunkManager()
        {
            Shader       = new ShaderProgram("main.glslv", "main.glslf");
            TextureAtlas = new Texture(TextureManager.Instance.Atlas, false);
            Block.Blocks = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Block>>(File.ReadAllText("resources/data/blocks.json")) ?? throw new Exception("[CRITICAL] Blocks is null!");

            Chunks    = [];

            SolverR = new LightSolver(0);
            SolverG = new LightSolver(1);
            SolverB = new LightSolver(2);
            SolverS = new LightSolver(3);

            AddQueue    = [];
            RemoveQueue = [];

            CreateMesh    = [];
            UpdateMesh    = [];
            VisibleChunks = [];
        }
        /// <summary>
        /// Draws all the chunks.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="time"></param>
        /// <param name="backgroundColor"></param>
        /// <param name="isWhiteWorld"></param>
        public void Draw(Player player, double time, Color3<Rgb> backgroundColor, bool isWhiteWorld)
        {
            Enable(EnableCap.CullFace);
            CullFace(TriangleFace.Back);
            Enable(EnableCap.Blend);
            BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            Shader.Bind();
            Shader.SetBool("uIsWhiteWorld", isWhiteWorld);
            Shader.SetVector3("uViewPos", player.Position);
            Shader.SetVector3("uFogColor", backgroundColor);
            Shader.SetFloat("uFogFactor", 0.9f); // 0 - fog off
            Shader.SetFloat("uFogCurve", 5f);
            Shader.SetFloat("uGamma", 1.6f);
            Shader.SetFloat("uTime", (float)time);
            Shader.SetMatrix4("uView", player.Camera.GetViewMatrix(player.Position));
            Shader.SetMatrix4("uProjection", player.Camera.GetProjectionMatrix());

            foreach (var (position, chunkInfo) in Chunks)
            {
                Shader.SetMatrix4("uModel", Matrix4.CreateTranslation(position.X * Chunk.Size.X, 0f, position.Y * Chunk.Size.Z));
                chunkInfo.Chunk?.Draw();
            }

            Disable(EnableCap.CullFace);
            Disable(EnableCap.Blend);
        }
        /// <summary>
        /// Deallocates all resources.
        /// </summary>
        public void Delete()
        {
            TextureAtlas.Dispose();

            File.WriteAllTextAsync("saves/world/world.json", Newtonsoft.Json.JsonConvert.SerializeObject(Seed, Newtonsoft.Json.Formatting.Indented));

            WriteChunks(Chunks);

            foreach (var (_, chunkInfo) in Chunks)
            {
                chunkInfo.Chunk.Delete();
            }

            Shader.Dispose();
        }

        #region Chunks

        /// <summary>
        /// Updates vertices, textures, etc. for the nearest сhunks.
        ///  []  []  []
        ///    \ |  /
        /// [] - [] - []
        ///    / |  \
        ///  []  []  []
        /// </summary>
        /// <param name="c">Coordinates of the chunk</param>
        // TODO: needs a lot of optimization
        private static void UpdateNearestChunks(Vector2i c)
        {
            List<Vector2i> chunkOffsets =
            [
                ( 1,  0),
                (-1,  0),
                ( 0, -1),
                ( 0,  1),

                ( 1,  1),
                ( 1, -1),
                (-1,  1),
                (-1, -1),
            ];

            foreach (var offset in chunkOffsets)
            {
                var newC = c + offset;
                if (Instance.Chunks.TryGetValue(newC, out var chunkInfo))
                {
                    chunkInfo.Chunk.UpdateMesh();
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="currentChunk"></param>
        public void UpdateVisibleChunks(Vector2i currentChunk)
        {
            HashSet<Vector2i> visibleChunks = [];

            for (int x = -RenderDistance + currentChunk.X; x <= RenderDistance + currentChunk.X; x++)
            {
                for (int z = -RenderDistance + currentChunk.Y; z <= RenderDistance + currentChunk.Y; z++)
                {
                    visibleChunks.Add((x, z));
                }
            }

            VisibleChunks = visibleChunks;

            // adding new
            foreach (var position in visibleChunks)
            {
                if (!Chunks.ContainsKey(position))
                {
                    var chunkInfo = ReadChunk(position);

                    if (chunkInfo.Chunk is null)
                    {
                        AddQueue.Enqueue(position);
                    }
                    else
                    {
                        if (chunkInfo.Lightmap is null)
                        {
                            Chunks[position] = new ChunkInfo { Chunk = chunkInfo.Chunk, Lightmap = new(position) };

                            Chunks[position].Lightmap!.Create();

                            SolverR.Solve();
                            SolverG.Solve();
                            SolverB.Solve();
                            SolverS.Solve();
                        }
                        else
                        {
                            Chunks[position] = new ChunkInfo { Chunk = chunkInfo.Chunk, Lightmap = chunkInfo.Lightmap };
                        }

                        CreateMesh.Add(position);
                    }

                    UpdateMesh.Add(position + (1, 0));
                    UpdateMesh.Add(position + (-1, 0));
                    UpdateMesh.Add(position + (0, 1));
                    UpdateMesh.Add(position + (0, -1));
                }
            }

            // removing old
            foreach (var (position, _) in Chunks)
            {
                if (!VisibleChunks.Contains(position))
                {
                    RemoveQueue.Enqueue(position);
                    CreateMesh.Remove(position);
                    UpdateMesh.Remove(position);
                }
            }
        }
        public void CreateChunk()
        {
            if (AddQueue.Count > 0)
            {
                var c = AddQueue.Dequeue();

                Chunks[c] = new ChunkInfo { Chunk = new(c), Lightmap = new(c) };

                Chunks[c].Lightmap!.Create();

                SolverR.Solve();
                SolverG.Solve();
                SolverB.Solve();
                SolverS.Solve();

                CreateMesh.Add(c);
            }
        }
        public void RemoveChunk()
        {
            if (RemoveQueue.Count > 0)
            {
                var c = RemoveQueue.Dequeue();
                if (Chunks.TryGetValue(c, out var chunkInfo) && chunkInfo.Chunk is not null && chunkInfo.Lightmap is not null)
                {
                    WriteChunk(chunkInfo);
                    chunkInfo.Chunk.Delete();
                    Chunks.Remove(c);
                }
            }
        }

        public void CreateChunkMesh(Vector2i currentChunk)
        {
            if (CreateMesh.Count > 0 && AddQueue.Count == 0)
            {
                List<Vector2i> chunks = [.. CreateMesh];
                chunks.Sort((a, b) => GetDistance(currentChunk, a).CompareTo(GetDistance(currentChunk, b)));
                CreateMesh.Remove(chunks[0]);
                Chunks.TryGetValue(chunks[0], out var chunkInfo);
                chunkInfo.Chunk?.CreateMesh();
            }
        }

        public void UpdateChunkMesh()
        {
            if (UpdateMesh.Count > 0 && CreateMesh.Count == 0 && AddQueue.Count == 0)
            {
                var c = UpdateMesh.First();
                UpdateMesh.Remove(c);
                Chunks.TryGetValue(c, out var chunkInfo);
                chunkInfo.Chunk?.UpdateMesh();
            }
        }

        #endregion Chunks

        #region Blocks

        /// <summary>
        /// Gets the Block by its world coordinates of the block.
        /// </summary>
        /// <param name="wb">World coordinates of the block</param>
        /// <returns>The Block, if there is no block - null.</returns>
        public static Block? GetBlock(Vector3i wb)
        {
            if (wb.Y > Chunk.Size.Y - 1 || wb.Y < 0) return null;

            var c = GetChunkPosition(wb.Xz); // c - chunk coordinates
            if (Instance.Chunks.TryGetValue(c, out var chunkInfo) && chunkInfo.Chunk is not null)
            {
                return chunkInfo.Chunk[ConvertWorldToLocal(wb, c)];
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// Gets the Block by its world coordinates of the block.
        /// </summary>
        /// <param name="wx">World coordinate x of the block</param>
        /// <param name="wy">World coordinate y of the block</param>
        /// <param name="wz">World coordinate z of the block</param>
        /// <param name="isRayCast"></param>
        /// <returns>The Block, if there is no block - null.</returns>
        public static Block? GetBlock(int wx, int wy, int wz, bool isRayCast = false)
        {
            if (wy > Chunk.Size.Y - 1 || wy < 0) return null;

            var c = GetChunkPosition(wx, wz); // c - chunk coordinates
            if (Instance.Chunks.TryGetValue(c, out var chunkInfo) && chunkInfo.Chunk is not null)
            {
                if (isRayCast is true)
                {
                    return new Block(chunkInfo.Chunk[ConvertWorldToLocal(wx, wy, wz, c)].ID);
                }
                else
                {
                    return chunkInfo.Chunk[ConvertWorldToLocal(wx, wy, wz, c)];
                }
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// Replaces the block with the selected block.
        /// </summary>
        /// <param name="id">Block id</param>
        /// <param name="wb"></param>
        /// <param name="movement"></param>
        // TODO: optimize face update
        public static void SetBlock(int id, Vector3i wb, Movement movement)
        {
            Vector3i lb;
            Vector2i c;

            if (movement is Movement.Destroy)
            {
                c = GetChunkPosition(wb.Xz);
                lb = ConvertWorldToLocal(wb, c);
            }
            else
            {
                if (wb.Y < 0 || wb.Y > Chunk.Size.Y - 1) return;
                c = GetChunkPosition(wb.X, wb.Z);
                if (Instance.Chunks.ContainsKey(c) is false) return;
                lb = ConvertWorldToLocal(wb, c);
            }

            Instance.Chunks[c].Chunk[lb] = new Block(id);
            UpdateLight(GetBlock(wb)!, wb);
            Instance.Chunks[c].Chunk.UpdateMesh();
            UpdateNearestChunks(c);
        }

        #endregion Blocks

        #region Light

        /// <summary>
        /// Updates light values.
        /// </summary>
        /// <param name="block">The Block</param>
        /// <param name="wb">World coordinates of the block</param>
        public static void UpdateLight(Block block, Vector3i wb)
        {
            if (block.IsLightPassing is true)
            {
                Instance.SolverR.Remove(wb);
                Instance.SolverG.Remove(wb);
                Instance.SolverB.Remove(wb);

                Instance.SolverR.Solve();
                Instance.SolverG.Solve();
                Instance.SolverB.Solve();

                if (GetLight(wb.X, wb.Y + 1, wb.Z, 3) == 0xF)
                {
                    for (int i = wb.Y; i >= 0; i--)
                    {
                        if (GetBlock(wb.X, i, wb.Z)?.IsLightPassing is false) break;

                        Instance.SolverS.Add(wb.X, i, wb.Z, 0xF);
                    }
                }

                Instance.SolverR.Add(wb.X + 1, wb.Y, wb.Z); Instance.SolverG.Add(wb.X + 1, wb.Y, wb.Z); Instance.SolverB.Add(wb.X + 1, wb.Y, wb.Z); Instance.SolverS.Add(wb.X + 1, wb.Y, wb.Z);
                Instance.SolverR.Add(wb.X - 1, wb.Y, wb.Z); Instance.SolverG.Add(wb.X - 1, wb.Y, wb.Z); Instance.SolverB.Add(wb.X - 1, wb.Y, wb.Z); Instance.SolverS.Add(wb.X - 1, wb.Y, wb.Z);
                Instance.SolverR.Add(wb.X, wb.Y + 1, wb.Z); Instance.SolverG.Add(wb.X, wb.Y + 1, wb.Z); Instance.SolverB.Add(wb.X, wb.Y + 1, wb.Z); Instance.SolverS.Add(wb.X, wb.Y + 1, wb.Z);
                Instance.SolverR.Add(wb.X, wb.Y - 1, wb.Z); Instance.SolverG.Add(wb.X, wb.Y - 1, wb.Z); Instance.SolverB.Add(wb.X, wb.Y - 1, wb.Z); Instance.SolverS.Add(wb.X, wb.Y - 1, wb.Z);
                Instance.SolverR.Add(wb.X, wb.Y, wb.Z + 1); Instance.SolverG.Add(wb.X, wb.Y, wb.Z + 1); Instance.SolverB.Add(wb.X, wb.Y, wb.Z + 1); Instance.SolverS.Add(wb.X, wb.Y, wb.Z + 1);
                Instance.SolverR.Add(wb.X, wb.Y, wb.Z - 1); Instance.SolverG.Add(wb.X, wb.Y, wb.Z - 1); Instance.SolverB.Add(wb.X, wb.Y, wb.Z - 1); Instance.SolverS.Add(wb.X, wb.Y, wb.Z - 1);

                Instance.SolverR.Solve();
                Instance.SolverG.Solve();
                Instance.SolverB.Solve();
                Instance.SolverS.Solve();
            }
            else
            {
                Instance.SolverR.Remove(wb);
                Instance.SolverG.Remove(wb);
                Instance.SolverB.Remove(wb);
                Instance.SolverS.Remove(wb);

                for (int i = wb.Y - 1; i >= 0; i--)
                {
                    Instance.SolverS.Remove(wb.X, i, wb.Z);

                    if (GetBlock(wb.X, i - 1, wb.Z)?.IsLightPassing is false) break;
                }

                Instance.SolverR.Solve();
                Instance.SolverG.Solve();
                Instance.SolverB.Solve();
                Instance.SolverS.Solve();

                if (block?.IsLightSource is true)
                {
                    switch (block.ID)
                    {
                        case 9: // red lamp
                            Instance.SolverR.Add(wb, 13);
                            break;

                        case 10: // green lamp
                            Instance.SolverG.Add(wb, 13);
                            break;

                        case 11: // blue lamp
                            Instance.SolverB.Add(wb, 13);
                            break;
                    }

                    Instance.SolverR.Solve();
                    Instance.SolverG.Solve();
                    Instance.SolverB.Solve();
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="wb"></param>
        /// <returns></returns>
        public static ushort GetLight(Vector3i wb)
        {
            var c = GetChunkPosition(wb.X, wb.Z);

            Instance.Chunks.TryGetValue(c, out var chunkInfo);

            var lb = ConvertWorldToLocal(wb, c);

            return chunkInfo.Lightmap?.GetLight(lb.X, lb.Y, lb.Z) ?? 0;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="wb"></param>
        /// <param name="channel"></param>
        /// <returns></returns>
        public static byte GetLight(Vector3i wb, int channel)
        {
            var c = GetChunkPosition(wb.X, wb.Z);

            Instance.Chunks.TryGetValue(c, out var chunkInfo);

            if (chunkInfo.Lightmap is null && channel == 3)
            {
                if (wb.Y < 0)
                {
                    return 0x0;
                }
                else
                {
                    return 0xF;
                }
            }
            else
            {
                if (wb.Y >= Chunk.Size.Y && channel == 3)
                {
                    return 0xF;
                }
                else
                {
                    var lb = ConvertWorldToLocal(wb, c);

                    return chunkInfo.Lightmap?.GetLight(lb.X, lb.Y, lb.Z, channel) ?? 0x0;
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="wx"></param>
        /// <param name="wy"></param>
        /// <param name="wz"></param>
        /// <param name="channel"></param>
        /// <returns></returns>
        public static byte GetLight(int wx, int wy, int wz, int channel)
        {
            var c = GetChunkPosition(wx, wz);

            Instance.Chunks.TryGetValue(c, out var chunkInfo);

            if (chunkInfo.Lightmap is null && channel == 3)
            {
                if (wy < 0)
                {
                    return 0x0;
                }
                else
                {
                    return 0xF;
                }
            }
            else
            {
                if (wy >= Chunk.Size.Y && channel == 3)
                {
                    return 0xF;
                }
                else
                {
                    var lb = ConvertWorldToLocal(wx, wy, wz, c);

                    return chunkInfo.Lightmap?.GetLight(lb.X, lb.Y, lb.Z, channel) ?? 0x0;
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="wb"></param>
        /// <param name="channel"></param>
        /// <param name="value"></param>
        public static void SetLight(Vector3i wb, int channel, int value)
        {
            var c = GetChunkPosition(wb.X, wb.Z);

            Instance.Chunks.TryGetValue(c, out var chunkInfo);

            var lb = ConvertWorldToLocal(wb, c);

            chunkInfo.Lightmap?.SetLight(lb.X, lb.Y, lb.Z, channel, value);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="wx"></param>
        /// <param name="wy"></param>
        /// <param name="wz"></param>
        /// <param name="channel"></param>
        /// <param name="value"></param>
        public static void SetLight(int wx, int wy, int wz, int channel, int value)
        {
            var c = GetChunkPosition(wx, wz);

            Instance.Chunks.TryGetValue(c, out var chunkInfo);

            var lb = ConvertWorldToLocal(wx, wy, wz, c);

            chunkInfo.Lightmap?.SetLight(lb.X, lb.Y, lb.Z, channel, value);
        }

        #endregion Light

        #region Read&Write

        public void Load(Vector2i currentChunk)
        {
            for (int x = -RenderDistance + currentChunk.X; x <= RenderDistance + currentChunk.X; x++)
            {
                for (int z = -RenderDistance + currentChunk.Y; z <= RenderDistance + currentChunk.Y; z++)
                {
                    VisibleChunks.Add((x, z));
                }
            }

            foreach (var position in VisibleChunks)
            {
                var chunkInfo = ReadChunk(position);
            
                if (chunkInfo.Chunk is not null)
                {
                    if (chunkInfo.Lightmap is not null)
                    {
                        Chunks[position] = new ChunkInfo { Chunk = chunkInfo.Chunk, Lightmap = chunkInfo.Lightmap };
                        CreateMesh.Add(position);
                    }
                    else
                    {
                        Chunks[position] = new ChunkInfo { Chunk = chunkInfo.Chunk, Lightmap = null };
                    }
                }
                else
                {
                    AddQueue.Enqueue(position);
                }
            }

            string filepath = "saves/world/world.json";
            // so far, there is only one parameter, this is a reserve for future expansion
            if (File.Exists(filepath))
            {
                Seed = Newtonsoft.Json.JsonConvert.DeserializeObject<int>(File.ReadAllText(filepath));
            }
            else
            {
                Random random = new();
                Seed = random.Next(Int32.MinValue, Int32.MaxValue);
            }
        }

        public static ChunkInfo ReadChunk(Vector2i chunkPosition)
        {
            if (!Directory.Exists("saves/world")) Directory.CreateDirectory("saves/world");

            Vector2i regionPosition = (chunkPosition.X >> 5, chunkPosition.Y >> 5);
            var filepath = $"saves/world/region_{regionPosition.X}_{regionPosition.Y}.bin";

            if (!File.Exists(filepath)) return new ChunkInfo { Chunk = null, Lightmap = null };

            using var fs = new FileStream(filepath, FileMode.Open, FileAccess.Read);

            int index = chunkPosition.X - (regionPosition.X << 5) + (chunkPosition.Y - (regionPosition.Y << 5)) * 32; // 32 - RegionSize

            // chunk
            var dataLength = new byte[4];
            fs.Seek(index * 4096, SeekOrigin.Begin); // 4096 - max size of the chunk data in bytes
            fs.Read(dataLength, 0, 4);
            int length = BitConverter.ToInt32(dataLength);

            if (length == 0) return new ChunkInfo { Chunk = null, Lightmap = null };

            var compressedData = new byte[length];
            fs.Read(compressedData, 0, length);
            var chunk = new Chunk(chunkPosition) { Blocks = DecompressRLEBlocks(DecompressGZip(compressedData)) };

            // lightmap
            fs.Read(dataLength, 0, 4);
            length = BitConverter.ToInt32(dataLength);

            if (length == 0) return new ChunkInfo { Chunk = chunk, Lightmap = null };

            compressedData = new byte[length];
            fs.Read(compressedData, 0, length);
            var lightmap = new Lightmap(chunkPosition) { Map = DecompressRLELightmap(DecompressGZip(compressedData)) };

            return new ChunkInfo { Chunk = chunk, Lightmap = lightmap };
        }

        public static void WriteChunk(ChunkInfo chunkInfo)
        {
            if (!Directory.Exists("saves/world")) Directory.CreateDirectory("saves/world");

            Vector2i regionPosition = (chunkInfo.Chunk.Position.X >> 5, chunkInfo.Chunk.Position.Y >> 5);
            using var fs = new FileStream($"saves/world/region_{regionPosition.X}_{regionPosition.Y}.bin", FileMode.OpenOrCreate, FileAccess.ReadWrite);

            int index = chunkInfo.Chunk.Position.X - (regionPosition.X << 5) + (chunkInfo.Chunk.Position.Y - (regionPosition.Y << 5)) * 32; // 32 - RegionSize

            fs.Seek(index * 4096, SeekOrigin.Begin); // 4096 - max size of the chunk data in bytes

            byte[] compressedBlocks = CompressGZip(CompressRLEBlocks(chunkInfo.Chunk.Blocks));
            byte[] chunkData = new byte[compressedBlocks.Length + 4]; // 4 bytes for the size of the chunk data
            BitConverter.GetBytes(compressedBlocks.Length).CopyTo(chunkData, 0);
            Array.Copy(compressedBlocks, 0, chunkData, 4, compressedBlocks.Length);
            fs.Write(chunkData, 0, chunkData.Length);

            byte[] compressedLightmap = CompressGZip(CompressRLELightmap(chunkInfo.Lightmap.Map));
            byte[] lightmapData = new byte[compressedLightmap.Length + 4]; // 4 bytes for the size of the chunk data
            BitConverter.GetBytes(compressedLightmap.Length).CopyTo(lightmapData, 0);
            Array.Copy(compressedLightmap, 0, lightmapData, 4, compressedLightmap.Length);
            fs.Write(lightmapData, 0, lightmapData.Length);
        }

        public static void WriteChunks(Dictionary<Vector2i, ChunkInfo> chunks)
        {
            if (!Directory.Exists("saves/world")) Directory.CreateDirectory("saves/world");

            Dictionary<Vector2i, List<ChunkInfo>> regions = [];

            foreach (var (_, chunkInfo) in chunks)
            {
                Vector2i regionPosition = (chunkInfo.Chunk.Position.X >> 5, chunkInfo.Chunk.Position.Y >> 5);

                if (!regions.ContainsKey(regionPosition))
                {
                    regions[regionPosition] = [];
                }

                regions[regionPosition].Add(chunkInfo);
            }

            foreach (var (regionPosition, chunkInfoList) in regions)
            {
                using var fs = new FileStream($"saves/world/region_{regionPosition.X}_{regionPosition.Y}.bin", FileMode.OpenOrCreate, FileAccess.ReadWrite);

                foreach (var chunkInfo in chunkInfoList)
                {
                    int index = chunkInfo.Chunk.Position.X - (regionPosition.X << 5) + (chunkInfo.Chunk.Position.Y - (regionPosition.Y << 5)) * 32; // 32 - RegionSize

                    fs.Seek(index * 4096, SeekOrigin.Begin); // 4096 - max size of the chunk data in bytes

                    byte[] compressedBlocks = CompressGZip(CompressRLEBlocks(chunkInfo.Chunk.Blocks));

                    byte[] chunkData = new byte[compressedBlocks.Length + 4]; // 4 bytes for the size of the chunk data
                    BitConverter.GetBytes(compressedBlocks.Length).CopyTo(chunkData, 0);
                    Array.Copy(compressedBlocks, 0, chunkData, 4, compressedBlocks.Length);

                    fs.Write(chunkData, 0, chunkData.Length);

                    byte[] compressedLightmap = CompressGZip(CompressRLELightmap(chunkInfo.Lightmap.Map));

                    byte[] lightmapData = new byte[compressedLightmap.Length + 4]; // 4 bytes for the size of the chunk data
                    BitConverter.GetBytes(compressedLightmap.Length).CopyTo(lightmapData, 0);
                    Array.Copy(compressedLightmap, 0, lightmapData, 4, compressedLightmap.Length);

                    fs.Write(lightmapData, 0, lightmapData.Length);
                }
            }
        }

        private static byte[] DecompressGZip(byte[] compressedData)
        {
            using var ms    = new MemoryStream(compressedData);
            using var gzs   = new GZipStream(ms, CompressionMode.Decompress);
            using var outms = new MemoryStream();

            gzs.CopyTo(outms);

            return outms.ToArray();
        }

        private static Block[] DecompressRLEBlocks(byte[] compressedBlocks)
        {
            using var ms = new MemoryStream(compressedBlocks);
            using var br = new BinaryReader(ms);

            var blocks = new Block[Chunk.Size.X * Chunk.Size.Y * Chunk.Size.Z];
            int index = 0;

            while (index < blocks.Length)
            {
                int id = br.ReadInt32();
                int count = br.ReadInt32();

                for (int i = 0; i < count; i++)
                {
                    blocks[index++] = new Block(id);
                }
            }

            return blocks;
        }

        private static ushort[] DecompressRLELightmap(byte[] compressedLightmap)
        {
            using var ms = new MemoryStream(compressedLightmap);
            using var br = new BinaryReader(ms);

            var lightmap = new ushort[Chunk.Size.X * Chunk.Size.Y * Chunk.Size.Z];
            int index = 0;

            while(index < lightmap.Length)
            {
                ushort light = br.ReadUInt16();
                int count = br.ReadInt32();

                for (int i = 0; i < count; i++)
                {
                    lightmap[index++] = light;
                }
            }

            return lightmap;
        }

        private static byte[] CompressGZip(byte[] data)
        {
            using var ms = new MemoryStream();
            using (var gzs = new GZipStream(ms, CompressionMode.Compress))
            {
                gzs.Write(data, 0, data.Length);
            }
            return ms.ToArray();
        }

        private static byte[] CompressRLEBlocks(Block[] blocks)
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
        
            int currentID = blocks[0].ID;
            int count = 1;
        
            for (int i = count; i < blocks.Length; i++)
            {
                if (blocks[i].ID == currentID)
                {
                    count++;
                }
                else
                {
                    bw.Write(currentID);
                    bw.Write(count);
                    currentID = blocks[i].ID;
                    count = 1;
                }
            }
        
            bw.Write(currentID);
            bw.Write(count);
        
            return ms.ToArray();
        }

        private static byte[] CompressRLELightmap(ushort[] lightmap)
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);

            ushort currentLight = lightmap[0];
            int count = 1;

            for (int i = count; i < lightmap.Length; i++)
            {
                if (lightmap[i] == currentLight)
                {
                    count++;
                }
                else
                {
                    bw.Write(currentLight);
                    bw.Write(count);
                    currentLight = lightmap[i];
                    count = 1;
                }
            }

            bw.Write(currentLight);
            bw.Write(count);

            return ms.ToArray();
        }

        #endregion Read&Write

        #region Inline

        /// <summary>
        /// Gets the chunk position by XZ coordinates of the block.
        /// </summary>
        /// <param name="wxz">World coordinates XZ of the block</param>
        /// <returns>The vector of the chunk coordinates.</returns>
        public static Vector2i GetChunkPosition(Vector2i wxz) =>
            ((int)Math.Floor((float)wxz.X / Chunk.Size.X), (int)Math.Floor((float)wxz.Y / Chunk.Size.Z));
        /// <summary>
        /// Gets the chunk position by XZ coordinates of the block.
        /// </summary>
        /// <param name="wx">World coordinate x of the block</param>
        /// <param name="wz">World coordinate z of the block</param>
        /// <returns>The vector of the chunk coordinates.</returns>
        public static Vector2i GetChunkPosition(int wx, int wz) =>
            ((int)Math.Floor((float)wx / Chunk.Size.X), (int)Math.Floor((float)wz / Chunk.Size.Z));
        /// <summary>
        /// 
        /// </summary>
        /// <param name="chunkA"></param>
        /// <param name="chunkB"></param>
        /// <returns></returns>
        public static double GetDistance(Vector2i chunkA, Vector2i chunkB) =>
            Math.Sqrt((chunkA.X - chunkB.X) * (chunkA.X - chunkB.X) + (chunkA.Y - chunkB.Y) * (chunkA.Y - chunkB.Y));
        /// <summary>
        /// Converts the world coordinates of the block to local coordinates within the chunk.
        /// </summary>
        /// <param name="wb">World coordinates of the block</param>
        /// <param name="c">Coordinates of the chunk</param>
        /// <returns>The vector of the block's local coordinates.</returns>
        public static Vector3i ConvertWorldToLocal(Vector3i wb, Vector2i c) =>
            (wb.X - c.X * Chunk.Size.X, wb.Y, wb.Z - c.Y * Chunk.Size.Z);
        /// <summary>
        /// Converts the world coordinates of the block to local coordinates within the chunk.
        /// </summary>
        /// <param name="wx">World coordinate x of the block</param>
        /// <param name="wy">World coordinate y of the block</param>
        /// <param name="wz">World coordinate z of the block</param>
        /// <param name="c">Coordinates of the chunk</param>
        /// <returns>The vector of the block's local coordinates.</returns>
        public static Vector3i ConvertWorldToLocal(int wx, int wy, int wz, Vector2i c) =>
            (wx - c.X * Chunk.Size.X, wy, wz - c.Y * Chunk.Size.Z);
        
        #endregion Inline
    }
}
