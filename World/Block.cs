using OpenTK.Mathematics;

namespace VoxelWorld.World
{
    public class Block
    {
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
            "glass",
            "red_light_source",
            "green_light_source",
            "blue_light_source",
        ];

        public string Name         { get; set; }
        public Vector3i Position   { get; set; }
        public TypeOfBlock Type    { get; private set; }
        /// <summary>
        /// 0xRGBS
        /// </summary>
        public ushort Light        { get; private set; }
        public bool IsLightSource  { get; private set; }
        public bool IsLightPassing { get; private set; }

        public Block(string name, Vector3i lb, ushort light = 0x0000)
        {
            Name           = name;
            Position       = lb;
            Type           = GetBlockType(name);
            Light          = light;
            IsLightSource  = GetIsLightSource(name);
            IsLightPassing = GetIsLightPassing(name);
        }
        public Block(string name, int lx, int ly, int lz, ushort light = 0x0000) : this(name, (lx, ly, lz), light) { }

        public static List<Vector3> GetBlockVertices(Face face) => blockVertexData[face];
        public static List<Vector2> GetBlockUV(Face face) => blockUVData[face];
        public static TypeOfBlock GetBlockType(string name) => blockTypeData[name];
        public static List<string> GetTextureFilepath(string name) => textureFilepathData[name];
        public static List<uint> GetTextureIndecies(string name) => textureIndecies[name];
        public static bool GetIsLightSource(string name) => blockIsLightSourceData[name];
        public static bool GetIsLightPassing(string name) => blockIsLightPassingData[name];
        public override string ToString() => $"'{Name}', {Position}";

        public byte GetLight(int channel) => (byte)((Light >> (12 - channel * 4)) & 0xF);
        public byte GetLightR() => (byte)((Light >> 12) & 0xF);
        public byte GetLightG() => (byte)((Light >> 8) & 0xF);
        public byte GetLightB() => (byte)((Light >> 4) & 0xF);
        public byte GetLightS() => (byte)(Light & 0xF);

        public void SetLight(int channel, int value) => Light = (ushort)((Light & (0xFFFF & (~(0xF << (12 - channel * 4))))) | (value << (12 - channel * 4)));
        public void SetLightR(int value) => Light = (ushort)((Light & 0x0FFF) | (value << 12));
        public void SetLightG(int value) => Light = (ushort)((Light & 0xF0FF) | (value << 8));
        public void SetLightB(int value) => Light = (ushort)((Light & 0xFF0F) | (value << 4));
        public void SetLightS(int value) => Light = (ushort)((Light & 0xFFF0) | value);

        // perhaps it is better to store and use some things in a shader, and some in a json file
        private static readonly Dictionary<Face, List<Vector3>> blockVertexData = new()
        {
            { Face.Front,  [ (0f, 0f, 1f), (1f, 0f, 1f), (1f, 1f, 1f), (0f, 1f, 1f) ] },
            { Face.Back,   [ (0f, 0f, 0f), (0f, 1f, 0f), (1f, 1f, 0f), (1f, 0f, 0f) ] },
            { Face.Left,   [ (0f, 0f, 0f), (0f, 0f, 1f), (0f, 1f, 1f), (0f, 1f, 0f) ] },
            { Face.Right,  [ (1f, 0f, 0f), (1f, 1f, 0f), (1f, 1f, 1f), (1f, 0f, 1f) ] },
            { Face.Top,    [ (0f, 1f, 0f), (0f, 1f, 1f), (1f, 1f, 1f), (1f, 1f, 0f) ] },
            { Face.Bottom, [ (0f, 0f, 0f), (1f, 0f, 0f), (1f, 0f, 1f), (0f, 0f, 1f) ] }
        };
        private static readonly Dictionary<Face, List<Vector2>> blockUVData = new()
        {
            { Face.Front,  [ (0f, 0f), (1f, 0f), (1f, 1f), (0f, 1f) ] },
            { Face.Back,   [ (1f, 0f), (1f, 1f), (0f, 1f), (0f, 0f) ] },
            { Face.Left,   [ (0f, 0f), (1f, 0f), (1f, 1f), (0f, 1f) ] },
            { Face.Right,  [ (1f, 0f), (1f, 1f), (0f, 1f), (0f, 0f) ] },
            { Face.Top,    [ (0f, 1f), (0f, 0f), (1f, 0f), (1f, 1f) ] },
            { Face.Bottom, [ (0f, 0f), (1f, 0f), (1f, 1f), (0f, 1f) ] }
        };
        private static readonly Dictionary<string, TypeOfBlock> blockTypeData = new()
        {
            { "air", TypeOfBlock.Air },

            { "stone",  TypeOfBlock.Cube },
            { "dirt",   TypeOfBlock.Cube },
            { "grass",  TypeOfBlock.Cube },
            { "sand",   TypeOfBlock.Cube },
            { "gravel", TypeOfBlock.Cube },

            { "oak_log",    TypeOfBlock.Log },
            { "oak_leaves", TypeOfBlock.Leaves },

            { "glass", TypeOfBlock.Glass },

            { "red_light_source",   TypeOfBlock.Cube },
            { "green_light_source", TypeOfBlock.Cube },
            { "blue_light_source",  TypeOfBlock.Cube }
        };
        private static readonly Dictionary<string, List<string>> textureFilepathData = new()
        {
            { "stone",  [ "blocks/stone.png" ] },
            { "dirt",   [ "blocks/dirt.png" ] },
            { "grass",  [ "blocks/grass_block.png", "blocks/grass_block_side.png", "blocks/dirt.png" ] },
            { "sand",   [ "blocks/sand.png" ] },
            { "gravel", [ "blocks/gravel.png" ] },

            { "oak_log",    [ "blocks/oak_log.png", "blocks/oak_log_top.png" ] },
            { "oak_leaves", [ "blocks/oak_leaves.png" ] },

            { "glass", [ "blocks/glass.png" ] },

            { "red_light_source",   [ "blocks/red_light_source.png" ] },
            { "green_light_source", [ "blocks/green_light_source.png" ] },
            { "blue_light_source",  [ "blocks/blue_light_source.png" ] }
        };
        private static readonly Dictionary<string, List<uint>> textureIndecies = new()
        {
            { "stone",  [ 0, 0, 0, 0, 0, 0 ] },
            { "dirt",   [ 0, 0, 0, 0, 0, 0 ] },
            { "grass",  [ 0, 1, 1, 1, 1, 2 ] },
            { "sand",   [ 0, 0, 0, 0, 0, 0 ] },
            { "gravel", [ 0, 0, 0, 0, 0, 0 ] },

            { "oak_log",    [ 1, 0, 0, 0, 0, 1 ] },
            { "oak_leaves", [ 0, 0, 0, 0, 0, 0 ] },

            { "glass", [ 0, 0, 0, 0, 0, 0 ] },

            { "red_light_source",   [ 0, 0, 0, 0, 0, 0 ] },
            { "green_light_source", [ 0, 0, 0, 0, 0, 0 ] },
            { "blue_light_source",  [ 0, 0, 0, 0, 0, 0 ] },
        };
        private static readonly Dictionary<string, bool> blockIsLightSourceData = new()
        {
            { "air", false },

            { "stone",  false },
            { "dirt",   false },
            { "grass",  false },
            { "sand",   false },
            { "gravel", false },

            { "oak_log",    false },
            { "oak_leaves", false },

            { "glass", false },

            { "red_light_source",   true },
            { "green_light_source", true },
            { "blue_light_source",  true },
        };
        private static readonly Dictionary<string, bool> blockIsLightPassingData = new()
        {
            { "air", true },

            { "stone",  false },
            { "dirt",   false },
            { "grass",  false },
            { "sand",   false },
            { "gravel", false },

            { "oak_log",    false },
            { "oak_leaves", false },

            { "glass", true },

            { "red_light_source",   false },
            { "green_light_source", false },
            { "blue_light_source",  false },
        };
    }
}
