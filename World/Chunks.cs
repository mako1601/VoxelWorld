using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;
using static OpenTK.Graphics.OpenGL.GL;

using VoxelWorld.Entity;
using VoxelWorld.Graphics;
using static VoxelWorld.World.Block;

namespace VoxelWorld.World
{
    public class Chunks
    {
        public static Dictionary<Vector2i, Chunk>? ChunksArray { get; private set; } = null;
        public ShaderProgram Shader { get; private set; }
        public static Dictionary<string, TextureArray>? Textures { get; set; } = null;
        public static LightSolver SolverR { get; private set; } = new LightSolver(0);
        public static LightSolver SolverG { get; private set; } = new LightSolver(1);
        public static LightSolver SolverB { get; private set; } = new LightSolver(2);
        public static LightSolver SolverS { get; private set; } = new LightSolver(3);

        public Chunks()
        {
            Shader   = new ShaderProgram("shader.glslv", "shader.glslf");
            Textures = [];

            for (int i = 1; i < Blocks.Count; i++) // i = 0 - air, but it has no texture :)
            {
                Textures.Add(Blocks[i], new TextureArray(GetTextureFilepath(Blocks[i])));
            }

            ChunksArray = new Dictionary<Vector2i, Chunk>
            {
                { ( 0,  0), new Chunk(( 0,  0)) },
                { ( 1,  0), new Chunk(( 1,  0)) },
                { ( 1,  1), new Chunk(( 1,  1)) },
                { ( 0,  1), new Chunk(( 0,  1)) },
                { (-1,  1), new Chunk((-1,  1)) },
                { (-1,  0), new Chunk((-1,  0)) },
                { (-1, -1), new Chunk((-1, -1)) },
                { ( 0, -1), new Chunk(( 0, -1)) },
                { ( 1, -1), new Chunk(( 1, -1)) }
            };

            foreach (var chunk in ChunksArray)
            {
                chunk.Value.CreateLightmap();
            }

            SolverR.Solve();
            SolverG.Solve();
            SolverB.Solve();
            SolverS.Solve();

            foreach (var chunk in ChunksArray)
            {
                chunk.Value.CreateMesh();
            }
        }
        /// <summary>
        /// Draws all the chunks.
        /// </summary>
        /// <param name="player">The Player</param>
        /// <param name="isWhiteWorld"></param>
        public void Draw(Player player, bool isWhiteWorld)
        {
            if (ChunksArray is null) throw new Exception("[CRITICAL] ChunksArray is null");
            if (Textures is null)    throw new Exception("[WARNING] Textures is null");

            Enable(EnableCap.CullFace);
            CullFace(TriangleFace.Back);
            Enable(EnableCap.Blend);
            BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            Shader.Bind();
            Shader.SetBool("IsWhiteWorld", isWhiteWorld);
            Shader.SetVector3("viewPos", player.Position);
            Shader.SetMatrix4("view", player.Camera.GetViewMatrix(player.Position));
            Shader.SetMatrix4("projection", player.Camera.GetProjectionMatrix());

            foreach (var chunk in ChunksArray)
            {
                Shader.SetMatrix4("model", Matrix4.CreateTranslation(chunk.Key.X * Chunk.Size.X, 0f, chunk.Key.Y * Chunk.Size.Z));
                chunk.Value.Draw(Textures);
            }

            Disable(EnableCap.CullFace);
            Disable(EnableCap.Blend);
        }
        /// <summary>
        /// Deallocates all resources.
        /// </summary>
        public void Delete()
        {
            foreach (var texture in Textures)
            {
                texture.Value.Delete();
            }

            foreach (var chunk in ChunksArray)
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
                if (ChunksArray.TryGetValue(newC, out var chunk))
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
            if (ChunksArray.TryGetValue(c, out var chunk))
            {
                return chunk.GetBlock(ConvertWorldToLocal(wb, c));
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
            if (ChunksArray.TryGetValue(c, out var chunk))
            {
                if (isRayCast is true)
                {
                    return new Block(chunk.GetBlock(ConvertWorldToLocal(wx, wy, wz, c)).Name, wx, wy, wz);
                }
                else
                {
                    return chunk.GetBlock(ConvertWorldToLocal(wx, wy, wz, c));
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
        /// <param name="name">Block name</param>
        /// <param name="ray">Ray from the camera</param>
        // TODO: optimize face update
        public static void SetBlock(string name, Ray ray, Movement movement)
        {
            Vector3i wb, lb;
            Vector2i c;

            if (movement is Movement.Destroy)
            {
                wb = ray.Block.Position;
                c  = GetChunkPosition(wb.Xz);
                lb = ConvertWorldToLocal(wb, c);
            }
            else
            {
                wb = ray.Block.Position + ray.Normal;
                if (wb.Y < 0 || wb.Y > Chunk.Size.Y - 1) return;
                c = GetChunkPosition(wb.X, wb.Z);
                if (ChunksArray.ContainsKey(c) is false) return;
                lb = ConvertWorldToLocal(wb, c);
            }

            ChunksArray[c].SetBlock(name, lb, GetBlock(wb).Light);
            UpdateLight(GetBlock(wb), wb);
            ChunksArray[c].UpdateMesh();
            UpdateNearestChunks(c);
        }
        /// <summary>
        /// Gets the Light in the specified Channel by its block world coordinates.
        /// </summary>
        /// <param name="wb">World coordinates of the block</param>
        /// <param name="channel">Channel number</param>
        /// <returns>The Light value in the specified channel.</returns>
        public static byte GetLight(Vector3i wb, int channel) =>
            GetBlock(wb)?.GetLight(channel) ?? 0;
        /// <summary>
        /// Gets the Light in the specified Channel by its block world coordinates.
        /// </summary>
        /// <param name="wx">World coordinate x of the block</param>
        /// <param name="wy">World coordinate y of the block</param>
        /// <param name="wz">World coordinate z of the block</param>
        /// <param name="channel">Channel number</param>
        /// <returns>The Light value in the specified channel.</returns>
        public static byte GetLight(int wx, int wy, int wz, int channel) =>
            GetBlock(wx, wy, wz)?.GetLight(channel) ?? 0;
        /// <summary>
        /// Updates light values.
        /// </summary>
        /// <param name="block">The Block</param>
        /// <param name="wb">World coordinates of the block</param>
        public static void UpdateLight(Block block, Vector3i wb)
        {
            if (block.IsLightPassing is true)
            {
                SolverR.Remove(wb);
                SolverG.Remove(wb);
                SolverB.Remove(wb);
                
                SolverR.Solve();
                SolverG.Solve();
                SolverB.Solve();

                if (GetLight(wb.X, wb.Y + 1, wb.Z, 3) == 0xF)
                {
                    for (int i = wb.Y; i >= 0; i--)
                    {
                        if (GetBlock(wb.X, i, wb.Z).IsLightPassing is false) break;

                        SolverS.Add(wb.X, i, wb.Z, 0xF);
                    }
                }

                SolverR.Add(wb.X + 1, wb.Y, wb.Z); SolverG.Add(wb.X + 1, wb.Y, wb.Z); SolverB.Add(wb.X + 1, wb.Y, wb.Z); SolverS.Add(wb.X + 1, wb.Y, wb.Z);
                SolverR.Add(wb.X - 1, wb.Y, wb.Z); SolverG.Add(wb.X - 1, wb.Y, wb.Z); SolverB.Add(wb.X - 1, wb.Y, wb.Z); SolverS.Add(wb.X - 1, wb.Y, wb.Z);
                SolverR.Add(wb.X, wb.Y + 1, wb.Z); SolverG.Add(wb.X, wb.Y + 1, wb.Z); SolverB.Add(wb.X, wb.Y + 1, wb.Z); SolverS.Add(wb.X, wb.Y + 1, wb.Z);
                SolverR.Add(wb.X, wb.Y - 1, wb.Z); SolverG.Add(wb.X, wb.Y - 1, wb.Z); SolverB.Add(wb.X, wb.Y - 1, wb.Z); SolverS.Add(wb.X, wb.Y - 1, wb.Z);
                SolverR.Add(wb.X, wb.Y, wb.Z + 1); SolverG.Add(wb.X, wb.Y, wb.Z + 1); SolverB.Add(wb.X, wb.Y, wb.Z + 1); SolverS.Add(wb.X, wb.Y, wb.Z + 1);
                SolverR.Add(wb.X, wb.Y, wb.Z - 1); SolverG.Add(wb.X, wb.Y, wb.Z - 1); SolverB.Add(wb.X, wb.Y, wb.Z - 1); SolverS.Add(wb.X, wb.Y, wb.Z - 1);

                SolverR.Solve();
                SolverG.Solve();
                SolverB.Solve();
                SolverS.Solve();
            }
            else
            {
                SolverR.Remove(wb);
                SolverG.Remove(wb);
                SolverB.Remove(wb);
                SolverS.Remove(wb);

                for (int i = wb.Y - 1; i >= 0; i--)
                {
                    SolverS.Remove(wb.X, i, wb.Z);

                    if (GetBlock(wb.X, i - 1, wb.Z).IsLightPassing is false) break;
                }

                SolverR.Solve();
                SolverG.Solve();
                SolverB.Solve();
                SolverS.Solve();

                if (block?.IsLightSource is true)
                {
                    switch (block.Name)
                    {
                        case "red_light_source":
                            SolverR.Add(wb, 13);
                            break;

                        case "green_light_source":
                            SolverG.Add(wb, 13);
                            break;

                        case "blue_light_source":
                            SolverB.Add(wb, 13);
                            break;
                    }
                
                    SolverR.Solve();
                    SolverG.Solve();
                    SolverB.Solve();
                }
            }
        }
        /// <summary>
        /// Gets the chunk position by XZ coordinates of the block.
        /// </summary>
        /// <param name="wxz">World coordinates XZ of the block</param>
        /// <returns>The vector of the chunk coordinates.</returns>
        public static Vector2i GetChunkPosition(Vector2i wxz)
        {
            int cx = wxz.X < 0 ? wxz.X / (Chunk.Size.X + 1) : wxz.X / Chunk.Size.X;
            int cz = wxz.Y < 0 ? wxz.Y / (Chunk.Size.Z + 1) : wxz.Y / Chunk.Size.Z;

            if (wxz.X < 0) cx--;
            if (wxz.Y < 0) cz--;

            return (cx, cz);
        }
        /// <summary>
        /// Gets the chunk position by XZ coordinates of the block.
        /// </summary>
        /// <param name="wx">World coordinate x of the block</param>
        /// <param name="wz">World coordinate z of the block</param>
        /// <returns>The vector of the chunk coordinates.</returns>
        public static Vector2i GetChunkPosition(int wx, int wz)
        {
            int cx = wx < 0 ? wx / (Chunk.Size.X + 1) : wx / Chunk.Size.X;
            int cz = wz < 0 ? wz / (Chunk.Size.Z + 1) : wz / Chunk.Size.Z;

            if (wx < 0) cx--;
            if (wz < 0) cz--;

            return (cx, cz);
        }
        /// <summary>
        /// Converts the world coordinates of the block to local coordinates within the chunk.
        /// </summary>
        /// <param name="wb">World coordinates of the block</param>
        /// <param name="c">Coordinates of the chunk</param>
        /// <returns>The vector of the block's local coordinates.</returns>
        public static Vector3i ConvertWorldToLocal(Vector3i wb, Vector2i c)
        {
            int lx = wb.X - c.X * Chunk.Size.X;
            int lz = wb.Z - c.Y * Chunk.Size.Z;

            return (lx, wb.Y, lz);
        }
        /// <summary>
        /// Converts the world coordinates of the block to local coordinates within the chunk.
        /// </summary>
        /// <param name="wx">World coordinate x of the block</param>
        /// <param name="wy">World coordinate y of the block</param>
        /// <param name="wz">World coordinate z of the block</param>
        /// <param name="c">Coordinates of the chunk</param>
        /// <returns>The vector of the block's local coordinates.</returns>
        public static Vector3i ConvertWorldToLocal(int wx, int wy, int wz, Vector2i c)
        {
            int lx = wx - c.X * Chunk.Size.X;
            int lz = wz - c.Y * Chunk.Size.Z;

            return (lx, wy, lz);
        }
    }
}
