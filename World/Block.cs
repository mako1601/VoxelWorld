using OpenTK.Mathematics;

namespace VoxelWorld.World
{
    public class Block
    {
        #region enums
        public enum TypeOfBlock
        {
            Air = 0,
            Cube,
            Log,
            Leaves,
            Glass
        };

        public enum Face
        {
            Top = 0,
            Front,
            Back,
            Left,
            Right,
            Bottom
        }
        #endregion

        #region properties
        public static List<string> Blocks { get; } =
        [
            "air",
            "stone",
            "dirt",
            "grass",
            "sand",
            "gravel",
            "oak_log",
            "oak_leaves",
            "glass"
        ];
        public string Name { get; set; }
        public TypeOfBlock Type { get; set; }
        public Vector3i Position { get; set; }
        #endregion

        #region constructors
        public Block(string name, int x, int y, int z)
        {
            Name = name;
            Type = GetBlockType(name);
            Position = new Vector3i(x, y, z);
        }
        public Block(string name, Vector3i position)
        {
            Name = name;
            Type = GetBlockType(name);
            Position = position;
        }
        #endregion

        #region methods
        public static List<Vector3> GetBlockVertices(Face face) => blockVertexData[face];
        public static List<Vector2> GetBlockUV(Face face) => blockUVData[face];
        public static TypeOfBlock GetBlockType(string name) => blockTypeData[name];
        public static List<string> GetTextureFilepath(string name) => textureFilepathData[name];
        public static List<uint> GetTextureIndecies(string name) => textureIndecies[name];
        public override string ToString() => $"{Name} {Position}";
        #endregion

        #region data
        // perhaps it is better to store and use it in a shader
        private static readonly Dictionary<Face, List<Vector3>> blockVertexData = new()
        {
            {Face.Front,
                new List<Vector3>
                {
                    (0f, 0f, 1f),
                    (1f, 0f, 1f),
                    (1f, 1f, 1f),
                    (0f, 1f, 1f)
                }
            },
            {Face.Back,
                new List<Vector3>
                {
                    (0f, 0f, 0f),
                    (0f, 1f, 0f),
                    (1f, 1f, 0f),
                    (1f, 0f, 0f)
                }
            },
            {Face.Left,
                new List<Vector3>
                {
                    (0f, 0f, 0f),
                    (0f, 0f, 1f),
                    (0f, 1f, 1f),
                    (0f, 1f, 0f)
                }
            },
            {Face.Right,
                new List<Vector3>
                {
                    (1f, 0f, 0f),
                    (1f, 1f, 0f),
                    (1f, 1f, 1f),
                    (1f, 0f, 1f)
                }
            },
            {Face.Top,
                new List<Vector3>
                {
                    (0f, 1f, 0f),
                    (0f, 1f, 1f),
                    (1f, 1f, 1f),
                    (1f, 1f, 0f)
                }
            },
            {Face.Bottom,
                new List<Vector3>
                {
                    (0f, 0f, 0f),
                    (1f, 0f, 0f),
                    (1f, 0f, 1f),
                    (0f, 0f, 1f)
                }
            }
        };
        private static readonly Dictionary<Face, List<Vector2>> blockUVData = new()
        {
            {Face.Front, new List<Vector2>
                {
                    (0f, 0f),
                    (1f, 0f),
                    (1f, 1f),
                    (0f, 1f)
                }
            },
            {Face.Back, new List<Vector2>
                {
                    (1f, 0f),
                    (1f, 1f),
                    (0f, 1f),
                    (0f, 0f)
                }
            },
            {Face.Left, new List<Vector2>
                {
                    (0f, 0f),
                    (1f, 0f),
                    (1f, 1f),
                    (0f, 1f)
                }
            },
            {Face.Right, new List<Vector2>
                {
                    (1f, 0f),
                    (1f, 1f),
                    (0f, 1f),
                    (0f, 0f)
                }
            },
            {Face.Top, new List<Vector2>
                {
                    (0f, 1f),
                    (0f, 0f),
                    (1f, 0f),
                    (1f, 1f)
                }
            },
            {Face.Bottom, new List<Vector2>
                {
                    (0f, 0f),
                    (1f, 0f),
                    (1f, 1f),
                    (0f, 1f)
                }
            }
        };
        private static readonly Dictionary<string, TypeOfBlock> blockTypeData = new()
        {
            {"air", TypeOfBlock.Air},
            {"stone", TypeOfBlock.Cube},
            {"dirt", TypeOfBlock.Cube},
            {"grass", TypeOfBlock.Cube},
            {"sand", TypeOfBlock.Cube},
            {"gravel", TypeOfBlock.Cube},
            {"oak_log", TypeOfBlock.Log},
            {"oak_leaves", TypeOfBlock.Leaves},
            {"glass", TypeOfBlock.Glass}
        };
        private static readonly Dictionary<string, List<string>> textureFilepathData = new()
        {
            {"stone", new List<string>{ "blocks/stone.png" }},
            {"dirt", new List<string>{ "blocks/dirt.png" }},
            {"grass", new List<string>{ "blocks/grass_block.png", "blocks/grass_block_side.png", "blocks/dirt.png"}},
            {"sand", new List<string>{ "blocks/sand.png"}},
            {"gravel", new List<string>{ "blocks/gravel.png" }},
            {"oak_log", new List<string>{ "blocks/oak_log.png", "blocks/oak_log_top.png"}},
            {"oak_leaves", new List<string>{ "blocks/oak_leaves.png" }},
            {"glass", new List<string>{ "blocks/glass.png" }}
        };
        private static readonly Dictionary<string, List<uint>> textureIndecies = new()
        {
            {"stone", new List<uint>{ 0, 0, 0, 0, 0, 0 }},
            {"dirt", new List<uint>{ 0, 0, 0, 0, 0, 0 }},
            {"grass", new List<uint>{ 0, 1, 1, 1, 1, 2 }},
            {"sand", new List<uint>{ 0, 0, 0, 0, 0, 0 }},
            {"gravel", new List<uint>{ 0, 0, 0, 0, 0, 0 }},
            {"oak_log", new List<uint>{ 1, 0, 0, 0, 0, 1 }},
            {"oak_leaves", new List<uint>{ 0, 0, 0, 0, 0, 0 }},
            {"glass", new List<uint>{ 0, 0, 0, 0, 0, 0 }}
        };
        #endregion
    }
}