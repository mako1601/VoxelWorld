using OpenTK.Mathematics;

using VoxelWorld.Managers;

namespace VoxelWorld.World
{
    public class Lightmap
    {
        public ushort[] Map { get; set; }
        public Vector2i Position { get; }

        public ushort this[int x, int y, int z]
        {
            get
            {
                return Map[GetIndex(x, y, z)];
            }
            set
            {
                Map[GetIndex(x, y, z)] = value;
            }
        }

        public Lightmap(Vector2i position)
        {
            Map = new ushort[Chunk.Size.X * Chunk.Size.Y * Chunk.Size.Z];
            Array.Fill<ushort>(Map, 0x0000);
            Position = position;
        }

        public void Create()
        {
            for (int y = 0; y < Chunk.Size.Y; y++)
            {
                for (int x = 0; x < Chunk.Size.X; x++)
                {
                    for (int z = 0; z < Chunk.Size.Z; z++)
                    {
                        var wb = ConvertLocalToWorld(x, y, z);
                        var block = ChunkManager.GetBlock(wb);

                        if (block?.IsLightSource is true)
                        {
                            switch (block.ID)
                            {
                                case 9: // red lamp
                                    ChunkManager.Instance.SolverR.Add(wb, 13);
                                    break;

                                case 10: // green lamp
                                    ChunkManager.Instance.SolverG.Add(wb, 13);
                                    break;

                                case 11: // blue lamp
                                    ChunkManager.Instance.SolverB.Add(wb, 13);
                                    break;
                            }
                        }
                    }
                }
            }

            for (int x = 0; x < Chunk.Size.X; x++)
            {
                for (int z = 0; z < Chunk.Size.Z; z++)
                {
                    for (int y = Chunk.Size.Y; y > -1; y--)
                    {
                        var block = ChunkManager.GetBlock(ConvertLocalToWorld(x, y, z));

                        if (block?.IsLightPassing is false) break;

                        SetLightS(x, y, z, 0xF);
                    }
                }
            }

            for (int x = 0; x < Chunk.Size.X; x++)
            {
                for (int z = 0; z < Chunk.Size.Z; z++)
                {
                    for (int y = Chunk.Size.Y - 1; y > -1; y--)
                    {
                        var wb = ConvertLocalToWorld(x, y, z);
                        var block = ChunkManager.GetBlock(wb);

                        if (block?.IsLightPassing is false) break;

                        if (ChunkManager.GetLight(wb.X, wb.Y - 1, wb.Z, 3) == 0 ||
                            ChunkManager.GetLight(wb.X, wb.Y + 1, wb.Z, 3) == 0 ||
                            ChunkManager.GetLight(wb.X - 1, wb.Y, wb.Z, 3) == 0 ||
                            ChunkManager.GetLight(wb.X + 1, wb.Y, wb.Z, 3) == 0 ||
                            ChunkManager.GetLight(wb.X, wb.Y, wb.Z - 1, 3) == 0 ||
                            ChunkManager.GetLight(wb.X, wb.Y, wb.Z + 1, 3) == 0)
                        {
                            ChunkManager.Instance.SolverS.Add(wb);
                        }

                        SetLightS(x, y, z, 0xF);
                    }
                }
            }
        }

        public ushort GetLight(int lx, int ly, int lz) =>
            TryGetLight(lx, ly, lz, out var value) ? value : (byte)0x0;
        public byte GetLight(int lx, int ly, int lz, int channel) =>
            TryGetLight(lx, ly, lz, out var value) ? (byte)(value >> 12 - channel * 4 & 0xF) : (byte)0x0;
        public byte GetLightR(int lx, int ly, int lz) =>
            TryGetLight(lx, ly, lz, out var value) ? (byte)(value >> 12 & 0xF) : (byte)0x0;
        public byte GetLightG(int lx, int ly, int lz) =>
            TryGetLight(lx, ly, lz, out var value) ? (byte)(value >> 8 & 0xF)  : (byte)0x0;
        public byte GetLightB(int lx, int ly, int lz) =>                      
            TryGetLight(lx, ly, lz, out var value) ? (byte)(value >> 4 & 0xF)  : (byte)0x0;
        public byte GetLightS(int lx, int ly, int lz) =>                      
            TryGetLight(lx, ly, lz, out var value) ? (byte)(value & 0xF)       : (byte)0x0;

        public void SetLight(int lx, int ly, int lz, int channel, int value)
        {
            if (TryGetLight(lx, ly, lz, out var oldValue))
            {
                this[lx, ly, lz] = (ushort)(oldValue & (0xFFFF & ~(0xF << 12 - channel * 4)) | value << 12 - channel * 4);
            }
        }
        public void SetLightR(int lx, int ly, int lz, int value) 
        {
            if (TryGetLight(lx, ly, lz, out var oldValue))
            {
                this[lx, ly, lz] = (ushort)(oldValue & 0x0FFF | value << 12);
            }
        }
        public void SetLightG(int lx, int ly, int lz, int value)
        {
            if (TryGetLight(lx, ly, lz, out var oldValue))
            {
                this[lx, ly, lz] = (ushort)(oldValue & 0xF0FF | value << 8);
            }
        }
        public void SetLightB(int lx, int ly, int lz, int value)
        {
            if (TryGetLight(lx, ly, lz, out var oldValue))
            {
                this[lx, ly, lz] = (ushort)(oldValue & 0xFF0F | value << 4);
            }
        }
        public void SetLightS(int lx, int ly, int lz, int value)
        {
            if (TryGetLight(lx, ly, lz, out var oldValue))
            {
                this[lx, ly, lz] = (ushort)(oldValue & 0xFFF0 | value);
            }
        }

        private bool TryGetLight(int x, int y, int z, out ushort light)
        {
            if (x >= 0 && x < Chunk.Size.X &&
                y >= 0 && y < Chunk.Size.Y &&
                z >= 0 && z < Chunk.Size.Z)
            {
                light = this[x, y, z];
                return true;
            }
            else
            {
                light = 0x0000;
                return false;
            }
        }
        private static int GetIndex(int x, int y, int z) =>
            x + (y * Chunk.Size.X) + (z * Chunk.Size.X * Chunk.Size.Y);
        private Vector3i ConvertLocalToWorld(int lx, int ly, int lz) =>
            (lx + Position.X * Chunk.Size.X, ly, lz + Position.Y * Chunk.Size.Z);
    }
}
