using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;
using static OpenTK.Graphics.OpenGL4.GL;

using VoxelWorld.Window;
using VoxelWorld.Entity;
using VoxelWorld.Graphics;
using static VoxelWorld.World.Block;

namespace VoxelWorld.World
{
    public class Chunks
    {
        #region properties
        public static Dictionary<Vector2i, Chunk>? ChunksArray { get; private set; } = null;
        public ShaderProgram Shader { get; private set; }
        public static Dictionary<string, TextureArray>? Textures { get; set; } = null;
        #endregion

        #region constructor
        public Chunks()
        {
            Shader = new ShaderProgram("shader.glslv", "shader.glslf");
            Textures = new Dictionary<string, TextureArray>();
            for (int i = 1; i < Blocks.Count; i++) // i = 0 - air, but it has no texture :)
            {
                Textures.Add(Blocks[i], new TextureArray(GetTextureFilepath(Blocks[i])));
            }

            ChunksArray = new Dictionary<Vector2i, Chunk>
            {
                {( 0,  0), new Chunk(( 0,  0))},
                {( 1,  0), new Chunk(( 1,  0))},
                {( 1,  1), new Chunk(( 1,  1))},
                {( 0,  1), new Chunk(( 0,  1))},
                {(-1,  1), new Chunk((-1,  1))},
                {(-1,  0), new Chunk((-1,  0))},
                {(-1, -1), new Chunk((-1, -1))},
                {( 0, -1), new Chunk(( 0, -1))},
                {( 1, -1), new Chunk(( 1, -1))}
            };

            foreach (var chunk in ChunksArray)
            {
                chunk.Value.Generate();
            }
        }
        #endregion

        #region methods
        /// <summary>
        /// Draws all the chunks.
        /// </summary>
        /// <param name="parameters"></param>
        public void Draw(Matrixes matrix, Parameters parameters)
        {
            if (ChunksArray == null) throw new Exception("ChunksArray is null");
            if (Textures == null) throw new Exception("Textures is null");

            Enable(EnableCap.CullFace);
            CullFace(CullFaceMode.Back);
            Enable(EnableCap.Blend);
            BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            Shader.Bind();
            Shader.SetBool("IsWhiteWorld", parameters.IsWhiteWorld);
            Shader.SetVector3("viewPos", parameters.PlayerPosition);
            Shader.SetMatrix4("view", matrix.View);
            Shader.SetMatrix4("projection", matrix.Projection);

            foreach (var chunk in ChunksArray)
            {
                Shader.SetMatrix4("model", Matrix4.CreateTranslation(chunk.Key.X * Chunk.Size.X, 0f, chunk.Key.Y * Chunk.Size.Z));
                chunk.Value.Draw(Textures);
            }

            Disable(EnableCap.CullFace);
            Disable(EnableCap.Blend);
        }

        /// <summary>
        /// Updates vertices, textures, etc. for the сhunk
        /// </summary>
        /// <param name="name">Name of the block (ID)</param>
        /// <param name="bp">World coordinates of the block</param>
        /// <param name="cp">Coordinates of the chunk</param>
        public static void Update(string name, Vector3i bp, Vector2i cp)
        {
            if (ChunksArray == null) throw new Exception("ChunksArray is null");

            ChunksArray[cp].SetBlock(name, ConvertWorldToLocal(bp, cp));
            ChunksArray[cp].Generate();
        }

        /// <summary>
        /// Deallocates all resources
        /// </summary>
        public void Delete()
        {
            if (ChunksArray == null) throw new Exception("ChunksArray is null");
            if (Textures == null) throw new Exception("Textures is null");

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
        /// Updates vertices, textures, etc. for the nearest сhunks
        ///       []
        ///       |
        /// [] - [] - []
        ///      |
        ///     []
        /// </summary>
        /// <param name="bp">Block position</param>
        /// <param name="cp">Chunk position</param>
        private static void UpdateNearestChunks(Vector3i bp, Vector2i cp)
        {
            if (ChunksArray == null) throw new Exception("ChunksArray is null");

            var updateConditions = new (Vector2i offset, Func<bool> condition)[]
            {
                (new(-1,  0), () => bp.X == 0),
                (new( 1,  0), () => bp.X == Chunk.Size.X - 1),
                (new( 0, -1), () => bp.Z == 0),
                (new( 0,  1), () => bp.Z == Chunk.Size.Z - 1),
            };

            foreach (var (offset, condition) in updateConditions)
            {
                Vector2i updatedChunkPosition = cp + offset;
                if (ChunksArray.TryGetValue(updatedChunkPosition, out Chunk? chunk) && condition())
                {
                    chunk.Generate();
                }
            }
        }

        /// <summary>
        /// Gets the Block by its world coordinates of the block.
        /// </summary>
        /// <param name="x">World coordinate x of the block</param>
        /// <param name="y">World coordinate y of the bloc</param>
        /// <param name="z">World coordinate z of the bloc</param>
        /// <returns>The Block, if there is no block - null.</returns>
        public static Block? GetBlock(int bx, int by, int bz)
        {
            if (ChunksArray == null) return null;
            if (by > Chunk.Size.Y - 1 || by < 0) return null;

            Vector2i cp = GetChunkPosition(bx, bz); // cp - chunkPosition
            if (ChunksArray.TryGetValue(cp, out Chunk? chunk))
            {
                string name = chunk.GetBlock(ConvertWorldToLocal(bx, by, bz, cp)).Name;

                return new Block(name, bx, by, bz);
            }

            return null;
        }

        /// <summary>
        /// Replaces the block with the selected block.
        /// </summary>
        /// <param name="name">Block name</param>
        /// <param name="ray">Ray from the camera</param>
        // TODO: optimize face update
        public static void SetBlock(string name, Ray ray)
        {
            if (ChunksArray == null) throw new Exception("ChunksArray is null");
            if (ray.Block == null) throw new Exception("Block is null");

            if (name == "air")
            {
                Vector2i chunkPosition = GetChunkPosition(ray.Block.Position.X, ray.Block.Position.Z);
                Update(name, ray.Block.Position, chunkPosition);
                UpdateNearestChunks(ConvertWorldToLocal(ray.Block.Position, chunkPosition), chunkPosition);
            }
            else
            {
                Vector3i position = ray.Block.Position + ray.Normal;
                if (position.Y < 0 || position.Y > Chunk.Size.Y - 1) return;

                Vector2i chunkPosition = GetChunkPosition(position.X, position.Z);
                if (ChunksArray.ContainsKey(chunkPosition) == false) return;

                Update(name, position, chunkPosition);
                UpdateNearestChunks(ConvertWorldToLocal(position, chunkPosition), chunkPosition);
            }
        }

        /// <summary>
        /// Gets the chunk position by XZ coordinates of the block.
        /// </summary>
        /// <param name="bXZ">World coordinates XZ of the block</param>
        /// <returns>The vector of the chunk coordinates.</returns>
        public static Vector2i GetChunkPosition(Vector2i bXZ)
        {
            int chunkX = bXZ.X < 0 ? bXZ.X / (Chunk.Size.X + 1) : bXZ.X / Chunk.Size.X;
            int chunkZ = bXZ.Y < 0 ? bXZ.Y / (Chunk.Size.Z + 1) : bXZ.Y / Chunk.Size.Z;

            if (bXZ.X < 0) chunkX--;
            if (bXZ.Y < 0) chunkZ--;

            return new Vector2i(chunkX, chunkZ);
        }

        /// <summary>
        /// Gets the chunk position by XZ coordinates of the block.
        /// </summary>
        /// <param name="x">World coordinate x of the block</param>
        /// <param name="z">World coordinate z of the block</param>
        /// <returns>The vector of the chunk coordinates.</returns>
        public static Vector2i GetChunkPosition(int x, int z)
        {
            int chunkX = x < 0 ? x / (Chunk.Size.X + 1) : x / Chunk.Size.X;
            int chunkZ = z < 0 ? z / (Chunk.Size.Z + 1) : z / Chunk.Size.Z;

            if (x < 0) chunkX--;
            if (z < 0) chunkZ--;

            return new Vector2i(chunkX, chunkZ);
        }

        /// <summary>
        /// Converts the world coordinates of the block to local coordinates within the chunk.
        /// </summary>
        /// <param name="bp">World coordinates of the block</param>
        /// <param name="cp">Coordinates of the chunk</param>
        /// <returns>The vector of the block's local coordinates.</returns>
        public static Vector3i ConvertWorldToLocal(Vector3i bp, Vector2i cp)
        {
            int x = bp.X - cp.X * Chunk.Size.X;
            int z = bp.Z - cp.Y * Chunk.Size.Z;

            return new(x, bp.Y, z);
        }

        /// <summary>
        /// Converts the world coordinates of the block to local coordinates within the chunk.
        /// </summary>
        /// <param name="bx">World coordinate x of the block</param>
        /// <param name="by">World coordinate y of the block</param>
        /// <param name="bz">World coordinate z of the block</param>
        /// <param name="cp">Chunk position in the world</param>
        /// <returns>The vector of the block's local coordinates.</returns>
        public static Vector3i ConvertWorldToLocal(int bx, int by, int bz, Vector2i cp)
        {
            int x = bx - cp.X * Chunk.Size.X;
            int z = bz - cp.Y * Chunk.Size.Z;

            return new(x, by, z);
        }
        #endregion
    }
}
