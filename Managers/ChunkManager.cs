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

        public Dictionary<Vector2i, Chunk> Chunks { get; private set; }
        public Dictionary<Vector2i, Lightmap> Lightmaps { get; private set; }

        public LightSolver SolverR { get; private set; } = new LightSolver(0);
        public LightSolver SolverG { get; private set; } = new LightSolver(1);
        public LightSolver SolverB { get; private set; } = new LightSolver(2);
        public LightSolver SolverS { get; private set; } = new LightSolver(3);
        /// <summary>
        /// Default value is 12.
        /// </summary>
        public static byte RenderDistance { get; set; } = 12;

        private ChunkManager()
        {
            Shader = new ShaderProgram("main.glslv", "main.glslf");
            TextureAtlas = new Texture(TextureManager.Instance.Atlas);
            Block.Blocks = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Block>>(File.ReadAllText("resources/data/blocks.json")) ?? throw new Exception("[CRITICAL] Blocks is null!");

            Chunks = [];
            for (int x = -RenderDistance; x <= RenderDistance; x++)
            {
                for (int z = -RenderDistance; z <= RenderDistance; z++)
                {
                    Chunks.Add((x, z), new Chunk((x, z)));
                }
            }

            Lightmaps = [];
            for (int x = -RenderDistance; x <= RenderDistance; x++)
            {
                for (int z = -RenderDistance; z <= RenderDistance; z++)
                {
                    Lightmaps.Add((x, z), new Lightmap((x, z)));
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public void Create()
        {
            foreach (var lightmap in Lightmaps)
            {
                lightmap.Value.Create();
            }

            SolverR.Solve();
            SolverG.Solve();
            SolverB.Solve();
            SolverS.Solve();

            foreach (var chunk in Chunks)
            {
                chunk.Value.CreateMesh();
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
                chunk.Value.Draw();
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
                    switch (block.Name)
                    {
                        case "red_light_source":
                            Instance.SolverR.Add(wb, 13);
                            break;

                        case "green_light_source":
                            Instance.SolverG.Add(wb, 13);
                            break;

                        case "blue_light_source":
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
        /// Gets the Block by its world coordinates of the block.
        /// </summary>
        /// <param name="wb">World coordinates of the block</param>
        /// <returns>The Block, if there is no block - null.</returns>
        public static Block? GetBlock(Vector3i wb)
        {
            if (wb.Y > Chunk.Size.Y - 1 || wb.Y < 0) return null;

            var c = GetChunkPosition(wb.Xz); // c - chunk coordinates
            if (Instance.Chunks.TryGetValue(c, out var chunk))
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
            if (Instance.Chunks.TryGetValue(c, out var chunk))
            {
                if (isRayCast is true)
                {
                    return new Block(chunk[ConvertWorldToLocal(wx, wy, wz, c)].ID, wx, wy, wz);
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

            Instance.Chunks[c][lb] = new Block(id, lb);
            UpdateLight(GetBlock(wb)!, wb);
            Instance.Chunks[c].UpdateMesh();
            UpdateNearestChunks(c);
        }
        public static byte GetLight(Vector3i wb, int channel)
        {
            var c = GetChunkPosition(wb.X, wb.Z);

            Instance.Lightmaps.TryGetValue(c, out var lightmap);

            var lb = ConvertWorldToLocal(wb, c);

            return lightmap?.GetLight(lb.X, lb.Y, lb.Z, channel) ?? 0;
        }
        public static byte GetLight(int wx, int wy, int wz, int channel)
        {
            var c = GetChunkPosition(wx, wz);

            Instance.Lightmaps.TryGetValue(c, out var lightmap);

            var lb = ConvertWorldToLocal(wx, wy, wz, c);

            return lightmap?.GetLight(lb.X, lb.Y, lb.Z, channel) ?? 0;
        }
        public static void SetLight(Vector3i wb, int channel, int value)
        {
            var c = GetChunkPosition(wb.X, wb.Z);

            Instance.Lightmaps.TryGetValue(c, out var lightmap);

            var lb = ConvertWorldToLocal(wb, c);

            lightmap?.SetLight(lb.X, lb.Y, lb.Z, channel, value);
        }
        public static void SetLight(int wx, int wy, int wz, int channel, int value)
        {
            var c = GetChunkPosition(wx, wz);

            Instance.Lightmaps.TryGetValue(c, out var lightmap);

            var lb = ConvertWorldToLocal(wx, wy, wz, c);

            lightmap?.SetLight(lb.X, lb.Y, lb.Z, channel, value);
        }
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
    }
}
