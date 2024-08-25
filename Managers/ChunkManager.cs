using System.IO.Compression;

using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;
using static OpenTK.Graphics.OpenGL.GL;

using VoxelWorld.World;
using VoxelWorld.Entity;
using VoxelWorld.Graphics;
using VoxelWorld.Graphics.Renderer;

namespace VoxelWorld.Managers
{
    public class ChunkManager
    {
        private static readonly Lazy<ChunkManager> _instance = new(() => new ChunkManager());
        public static ChunkManager Instance => _instance.Value;

        public ShaderProgram Shader { get; private set; }
        public Texture TextureAtlas { get; private set; }

        public Dictionary<Vector2i, Chunk?> Chunks { get; private set; }
        public Dictionary<Vector2i, Lightmap?> Lightmaps { get; private set; }

        public LightSolver SolverR { get; private set; }
        public LightSolver SolverG { get; private set; }
        public LightSolver SolverB { get; private set; }
        public LightSolver SolverS { get; private set; }

        public Queue<Vector2i> AddChunkQueue { get; private set; }
        public Queue<Vector2i> RemoveChunkQueue { get; private set; }
        public Queue<Vector2i> AddLightmapQueue { get; private set; }
        public Queue<Vector2i> RemoveLightmapQueue { get; private set; }
        public HashSet<Vector2i> UpdateMesh { get; set; }
        public HashSet<Vector2i> VisibleChunks { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public int Seed { get; private set; }
        /// <summary>
        /// Default value is 12.
        /// </summary>
        public static byte RenderDistance { get; set; } = 8;

        private ChunkManager()
        {
            Shader       = new ShaderProgram("main.glslv", "main.glslf");
            TextureAtlas = new Texture(TextureManager.Instance.Atlas, false);
            Block.Blocks = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Block>>(File.ReadAllText("resources/data/blocks.json")) ?? throw new Exception("[CRITICAL] Blocks is null!");

            Chunks    = [];
            Lightmaps = [];

            SolverR = new LightSolver(0);
            SolverG = new LightSolver(1);
            SolverB = new LightSolver(2);
            SolverS = new LightSolver(3);

            AddChunkQueue       = [];
            RemoveChunkQueue    = [];
            AddLightmapQueue    = [];
            RemoveLightmapQueue = [];

            UpdateMesh    = [];
            VisibleChunks = [];
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
                if (Chunks.TryAdd(position, null))
                {
                    AddChunkQueue.Enqueue(position);
                }
                if (Lightmaps.TryAdd(position, null))
                {
                    AddLightmapQueue.Enqueue(position);

                    // re-mesh an existing one to avoid holes in the mesh
                    if (Chunks.TryGetValue(position + (1, 0), out var chunk) && chunk is not null && chunk.IsMeshCreated is true)
                    {
                        UpdateMesh.Add(position + (1, 0));
                    }
                    if (Chunks.TryGetValue(position + (-1, 0), out chunk) && chunk is not null && chunk.IsMeshCreated is true)
                    {
                        UpdateMesh.Add(position + (-1, 0));
                    }
                    if (Chunks.TryGetValue(position + (0, 1), out chunk) && chunk is not null && chunk.IsMeshCreated is true)
                    {
                        UpdateMesh.Add(position + (0, 1));
                    }
                    if (Chunks.TryGetValue(position + (0, -1), out chunk) && chunk is not null && chunk.IsMeshCreated is true)
                    {
                        UpdateMesh.Add(position + (0, -1));
                    }
                }
            }

            // removing old
            foreach (var (position, _) in Chunks)
            {
                if (!VisibleChunks.Contains(position))
                {
                    RemoveChunkQueue.Enqueue(position);
                    RemoveLightmapQueue.Enqueue(position);
                    UpdateMesh.Remove(position);
                }
            }
        }
        public void CreateChunk()
        {
            if (AddChunkQueue.Count > 0)
            {
                var c = AddChunkQueue.Dequeue();
                Chunks[c] = new(c);
            }
            if (AddLightmapQueue.Count > 0)
            {
                var c = AddLightmapQueue.Dequeue();

                Lightmaps[c] = new(c);

                Lightmaps[c]!.Create();

                SolverR.Solve();
                SolverG.Solve();
                SolverB.Solve();
                SolverS.Solve();

                UpdateMesh.Add(c);
            }
        }
        public void RemoveChunk()
        {
            if (RemoveChunkQueue.Count > 0)
            {
                var c = RemoveChunkQueue.Dequeue();
                Chunks.TryGetValue(c, out var chunk);
                if (chunk is null) return;
                // here is a save to memory
                Chunks[c].Delete();
                Chunks.Remove(c);
            }
            if (RemoveLightmapQueue.Count > 0)
            {
                var c = RemoveLightmapQueue.Dequeue();
                Lightmaps.TryGetValue(c, out var lightmap);
                if (lightmap is null) return;
                // here is a save to memory
                Lightmaps.Remove(c);
            }
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


            foreach (var chunk in Chunks)
            {
                Shader.SetMatrix4("uModel", Matrix4.CreateTranslation(chunk.Key.X * Chunk.Size.X, 0f, chunk.Key.Y * Chunk.Size.Z));
                chunk.Value?.Draw();
            }

            Disable(EnableCap.CullFace);
            Disable(EnableCap.Blend);
        }
        /// <summary>
        /// Deallocates all resources.
        /// </summary>
        public void Delete()
        {
            TextureAtlas.Delete();

            File.WriteAllTextAsync("saves/world/world.json", Newtonsoft.Json.JsonConvert.SerializeObject(Seed, Newtonsoft.Json.Formatting.Indented));
            SaveChunksToMemory(Chunks, "saves/world/chunks.bin");
            SaveLightmapsToMemory(Lightmaps, "saves/world/lightmaps.bin");

            foreach (var chunk in Chunks)
            {
                chunk.Value.Delete();
            }

            Shader.Delete();
        }

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
                if (Instance.Chunks.TryGetValue(newC, out var chunk))
                {
                    chunk.UpdateMesh();
                }
            }
        }
        /// <summary>
        /// Gets the Block by its world coordinates of the block.
        /// </summary>
        /// <param name="wb">World coordinates of the block</param>
        /// <returns>The Block, if there is no block - null.</returns>
        public static Block? GetBlock(Vector3i wb)
        {
            if (wb.Y > Chunk.Size.Y - 1 || wb.Y < 0) return null;

            var c = GetChunkPosition(wb.Xz); // c - chunk coordinates
            if (Instance.Chunks.TryGetValue(c, out var chunk) && chunk is not null)
            {
                return chunk[ConvertWorldToLocal(wb, c)];
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
            if (Instance.Chunks.TryGetValue(c, out var chunk) && chunk is not null)
            {
                if (isRayCast is true)
                {
                    return new Block(chunk[ConvertWorldToLocal(wx, wy, wz, c)].ID);
                }
                else
                {
                    return chunk[ConvertWorldToLocal(wx, wy, wz, c)];
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

            Instance.Chunks[c][lb] = new Block(id);
            UpdateLight(GetBlock(wb)!, wb);
            Instance.Chunks[c].UpdateMesh();
            UpdateNearestChunks(c);
        }

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

            Instance.Lightmaps.TryGetValue(c, out var lightmap);

            var lb = ConvertWorldToLocal(wb, c);

            return lightmap?.GetLight(lb.X, lb.Y, lb.Z) ?? 0;
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

            Instance.Lightmaps.TryGetValue(c, out var lightmap);

            if (lightmap is null && channel == 3)
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

                    return lightmap?.GetLight(lb.X, lb.Y, lb.Z, channel) ?? 0x0;
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

            Instance.Lightmaps.TryGetValue(c, out var lightmap);

            if (lightmap is null && channel == 3)
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

                    return lightmap?.GetLight(lb.X, lb.Y, lb.Z, channel) ?? 0x0;
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

            Instance.Lightmaps.TryGetValue(c, out var lightmap);

            var lb = ConvertWorldToLocal(wb, c);

            lightmap?.SetLight(lb.X, lb.Y, lb.Z, channel, value);
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

            Instance.Lightmaps.TryGetValue(c, out var lightmap);

            var lb = ConvertWorldToLocal(wx, wy, wz, c);

            lightmap?.SetLight(lb.X, lb.Y, lb.Z, channel, value);
        }

        #endregion Light

        #region Load&Save

        /// <summary>
        /// 
        /// </summary>
        public void Load(Vector2i currentChunk)
        {
            for (int x = -RenderDistance + currentChunk.X; x <= RenderDistance + currentChunk.X; x++)
            {
                for (int z = -RenderDistance + currentChunk.Y; z <= RenderDistance + currentChunk.Y; z++)
                {
                    VisibleChunks.Add((x, z));
                }
            }
            var filepath = "saves/world/chunks.bin";

            if (File.Exists(filepath))
            {
                foreach (var (position, chunk) in LoadChunksFromMemory(filepath))
                {
                    if (VisibleChunks.Contains(position))
                    {
                        Chunks.Add(position, chunk);
                    }
                }
            }

            filepath = "saves/world/lightmaps.bin";
            if (File.Exists(filepath))
            {
                foreach (var (position, lightmap) in LoadLightmapsFromMemory(filepath))
                {
                    if (VisibleChunks.Contains(position))
                    {
                        Lightmaps.Add(position, lightmap);
                        UpdateMesh.Add(position);
                    }
                }
            }

            filepath = "saves/world/world.json";
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
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static Dictionary<Vector2i, Chunk> LoadChunksFromMemory(string filepath)
        {
            Dictionary<Vector2i, Chunk> chunks = [];

            using var fs  = new FileStream(filepath, FileMode.Open, FileAccess.Read);
            using var gzs = new GZipStream(fs, CompressionMode.Decompress);
            using var br  = new BinaryReader(gzs);

            int chunkCount = br.ReadInt32();

            for (int i = 0; i < chunkCount; i++)
            {
                Vector2i position = (br.ReadInt32(), br.ReadInt32());
                int compressedBlockCount = br.ReadInt32();
                List<Vector2i> compressedBlocks = [];

                for (int j = 0; j < compressedBlockCount; j++)
                {
                    compressedBlocks.Add((br.ReadInt32(), br.ReadInt32()));
                }

                Chunk chunk = new(position)
                {
                    Blocks = DecompressBlocks(compressedBlocks)
                };

                chunks[position] = chunk;
            }

            return chunks;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="chunks"></param>
        public static void SaveChunksToMemory(Dictionary<Vector2i, Chunk> chunks, string filepath)
        {
            if (!Directory.Exists("saves/world")) Directory.CreateDirectory("saves/world");

            using var fs  = new FileStream(filepath, FileMode.Create, FileAccess.Write);
            using var gzs = new GZipStream(fs, CompressionLevel.SmallestSize);
            using var bw  = new BinaryWriter(gzs);

            bw.Write(chunks.Count);

            foreach (var (position, chunk) in chunks)
            {
                bw.Write(position.X);
                bw.Write(position.Y);

                var compressedBlocks = CompressBlocks(chunk.Blocks);
                bw.Write(compressedBlocks.Count);

                foreach (var (id, count) in compressedBlocks)
                {
                    bw.Write(id);
                    bw.Write(count);
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="blocks"></param>
        /// <returns></returns>
        private static List<Vector2i> CompressBlocks(Block[] blocks)
        {
            List<Vector2i> compressedData = [];

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
                    compressedData.Add((currentID, count));
                    currentID = blocks[i].ID;
                    count = 1;
                }
            }

            compressedData.Add((currentID, count));

            return compressedData;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="compressedData"></param>
        /// <returns></returns>
        private static Block[] DecompressBlocks(List<Vector2i> compressedData)
        {
            var blocks = new Block[Chunk.Size.X * Chunk.Size.Y * Chunk.Size.Z];
            int index = 0;

            foreach (var (id, count) in compressedData)
            {
                for (int i = 0; i < count; i++)
                {
                    blocks[index++] = new Block(id);
                }
            }

            return blocks;
        }

        public static Dictionary<Vector2i, Lightmap> LoadLightmapsFromMemory(string filepath)
        {
            Dictionary<Vector2i, Lightmap> lightmaps = [];

            using var fs  = new FileStream(filepath, FileMode.Open, FileAccess.Read);
            using var gzs = new GZipStream(fs, CompressionMode.Decompress);
            using var br  = new BinaryReader(gzs);

            int lightmapCount = br.ReadInt32();

            for (int i = 0; i < lightmapCount; i++)
            {
                Vector2i position = (br.ReadInt32(), br.ReadInt32());
                int compressedLightmapCount = br.ReadInt32();
                List<(ushort, int)> compressedLightmaps = [];

                for (int j = 0; j < compressedLightmapCount; j++)
                {
                    compressedLightmaps.Add((br.ReadUInt16(), br.ReadInt32()));
                }

                Lightmap lightmap = new(position)
                {
                    Map = DecompressLightmap(compressedLightmaps)
                };

                lightmaps[position] = lightmap;
            }

            return lightmaps;
        }
        public static void SaveLightmapsToMemory(Dictionary<Vector2i, Lightmap> lightmaps, string filepath)
        {
            if (!Directory.Exists("saves/world")) Directory.CreateDirectory("saves/world");

            using var fs  = new FileStream(filepath, FileMode.Create, FileAccess.Write);
            using var gzs = new GZipStream(fs, CompressionLevel.SmallestSize);
            using var bw  = new BinaryWriter(gzs);

            bw.Write(lightmaps.Count);

            foreach (var (position, lightmap) in lightmaps)
            {
                bw.Write(position.X);
                bw.Write(position.Y);

                var compressedLightmap = CompressLightmap(lightmap.Map);
                bw.Write(compressedLightmap.Count);

                foreach (var (light, count) in compressedLightmap)
                {
                    bw.Write(light);
                    bw.Write(count);
                }
            }
        }
        private static List<(ushort, int)> CompressLightmap(ushort[] lightmap)
        {
            List<(ushort, int)> compressedData = [];

            ushort light = lightmap[0];
            int count = 1;

            for (int i = count; i < lightmap.Length; i++)
            {
                if (lightmap[i] == light)
                {
                    count++;
                }
                else
                {
                    compressedData.Add((light, count));
                    light = lightmap[i];
                    count = 1;
                }
            }

            compressedData.Add((light, count));

            return compressedData;
        }
        private static ushort[] DecompressLightmap(List<(ushort, int)> compressedData)
        {
            var lightmap = new ushort[Chunk.Size.X * Chunk.Size.Y * Chunk.Size.Z];
            int index = 0;

            foreach (var (light, count) in compressedData)
            {
                for (int i = 0; i < count; i++)
                {
                    lightmap[index++] = light;
                }
            }

            return lightmap;
        }

        #endregion Load&Save

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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="chunkA"></param>
        /// <param name="chunkB"></param>
        /// <returns></returns>
        public static double GetDistance(Vector2i chunkA, Vector2i chunkB) =>
            Math.Sqrt((chunkA.X - chunkB.X) * (chunkA.X - chunkB.X) + (chunkA.Y - chunkB.Y) * (chunkA.Y - chunkB.Y));

        #endregion Inline
    }
}
