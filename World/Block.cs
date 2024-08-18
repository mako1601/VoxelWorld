using OpenTK.Mathematics;

using VoxelWorld.Managers;

namespace VoxelWorld.World
{
    public class Block
    {
        public enum TypeOfBlock
        {
            Air = 0,
            Cube = 1,
            Log = 2,
            Leaves = 3,
            Glass = 4
        };

        public enum Face
        {
            Top = 0,
            Front = 1,
            Back = 2,
            Left = 3,
            Right = 4,
            Bottom = 5
        }
        public static List<Block> Blocks { get; set; } = [];

        private static int _id = 0;
        public int ID { get; private set; }
        public string Name { get; set; }
        /// <summary>
        /// Local coordinates of the block.
        /// </summary>
        public Vector3i Position { get; set; }
        public TypeOfBlock Type { get; private set; }
        public bool IsLightSource { get; private set; }
        public bool IsLightPassing { get; private set; }

        private string[]? Textures { get; set; }

        [Newtonsoft.Json.JsonConstructor]
        public Block(string name, TypeOfBlock type, bool isLightSource, bool isLightPassing, string[]? textures)
        {
            ID             = _id++;
            Name           = name;
            Position       = Vector3i.Zero;
            Type           = type;
            IsLightSource  = isLightSource;
            IsLightPassing = isLightPassing;
            Textures       = textures;
        }
        public Block(int id, Vector3i lb)
        {
            Block block = Blocks[id];

            ID             = block.ID;
            Name           = block.Name;
            Position       = lb;
            Type           = block.Type;
            IsLightSource  = block.IsLightSource;
            IsLightPassing = block.IsLightPassing;
        }
        public Block(int id, int lx, int ly, int lz) : this(id, (lx, ly, lz)) { }

        public static List<Vector3> GetBlockVertices(Face face) => blockVertexData[face];
        public static List<Vector2> GetBlockUV(int id, Face face)
        {
            float[] tex = TextureManager.Instance.Textures[Blocks[id].Textures![(int)face]];

            return [ (tex[0], tex[3]), (tex[2], tex[3]), (tex[2], tex[1]), (tex[0], tex[1]) ];
        }
        public override string ToString() => $"'{Name}', {Position}";

        private static readonly Dictionary<Face, List<Vector3>> blockVertexData = new()
        {
            { Face.Top,    [ (0f, 1f, 0f), (0f, 1f, 1f), (1f, 1f, 1f), (1f, 1f, 0f) ] },
            { Face.Front,  [ (0f, 0f, 1f), (1f, 0f, 1f), (1f, 1f, 1f), (0f, 1f, 1f) ] },
            { Face.Back,   [ (1f, 0f, 0f), (0f, 0f, 0f), (0f, 1f, 0f), (1f, 1f, 0f) ] },
            { Face.Left,   [ (0f, 0f, 0f), (0f, 0f, 1f), (0f, 1f, 1f), (0f, 1f, 0f) ] },
            { Face.Right,  [ (1f, 0f, 1f), (1f, 0f, 0f), (1f, 1f, 0f), (1f, 1f, 1f) ] },
            { Face.Bottom, [ (0f, 0f, 1f), (0f, 0f, 0f), (1f, 0f, 0f), (1f, 0f, 1f) ] }
        };
    }
}
