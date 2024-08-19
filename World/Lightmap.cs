using OpenTK.Mathematics;
using VoxelWorld.Managers;

namespace VoxelWorld.World
{
    public class Lightmap
    {
        //public Dictionary<Vector3i, ushort> Map { get; set; }
        private ushort[] Map { get; set; }
        public Vector2i Position { get; }

        private ushort this[Vector3i position]
        {
            get
            {
                return Map[GetIndex(position)];
            }
            set
            {
                Map[GetIndex(position)] = value;
            }
        }

        private ushort this[int x, int y, int z]
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
            Map = new ushort[Chunk.Size.X * (Chunk.Size.Y + 1) * Chunk.Size.Z];
            Array.Fill<ushort>(Map, 0x0000);
            Position = position;
        }

        public void Create()
        {
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

            //for (int y = 0; y < Size.Y; y++)
            //{
            //    for (int x = 0; x < Size.X; x++)
            //    {
            //        for (int z = 0; z < Size.Z; z++)
            //        {
            //            if (Blocks[(x, y, z)].IsLightSource)
            //            {
            //              // ...
            //            }
            //        }
            //    }
            //}

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

        public byte GetLight(int lx, int ly, int lz, int channel)
        {
            if (TryGetLight(lx, ly, lz, out var value))
            {
                return (byte)(value >> 12 - channel * 4 & 0xF);
            }
            else
            {
                return 0;
            }
        }
        public byte GetLightR(int lx, int ly, int lz) 
        {
            if (TryGetLight(lx, ly, lz, out var value))
            {
                return (byte)(value >> 12 & 0xF);
            }
            else
            {
                return 0;
            }
        }
        public byte GetLightG(int lx, int ly, int lz)
        {
            if (TryGetLight(lx, ly, lz, out var value))
            {
                return (byte)(value >> 8 & 0xF);
            }
            else
            {
                return 0;
            }
        }
        public byte GetLightB(int lx, int ly, int lz)
        {
            if (TryGetLight(lx, ly, lz, out var value))
            {
                return (byte)(value >> 4 & 0xF);
            }
            else
            {
                return 0;
            }
        }
        public byte GetLightS(int lx, int ly, int lz)
        {
            if (TryGetLight(lx, ly, lz, out var value))
            {
                return (byte)(value & 0xF);
            }
            else
            {
                return 0;
            }
        }

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
                y >= 0 && y <= Chunk.Size.Y &&
                z >= 0 && z < Chunk.Size.Z)
            {
                light = this[x, y, z];
                return true;
            }
            else
            {
                light = 0;
                return false;
            }
        }
        private bool TryGetLight(Vector3i position, out ushort light)
        {
            if (position.X >= 0 && position.X <  Chunk.Size.X &&
                position.Y >= 0 && position.Y <= Chunk.Size.Y &&
                position.Z >= 0 && position.Z <  Chunk.Size.Z)
            {
                light = this[position];
                return true;
            }
            else
            {
                light = 0;
                return false;
            }
        }
        public static int GetIndex(int x, int y, int z) =>
            x + (y * Chunk.Size.X) + (z * Chunk.Size.X * (Chunk.Size.Y + 1));
        public static int GetIndex(Vector3i position) =>
            position.X + (position.Y * Chunk.Size.X) + (position.Z * Chunk.Size.X * (Chunk.Size.Y + 1));
        public Vector3i ConvertLocalToWorld(int lx, int ly, int lz) =>
            (lx + Position.X * Chunk.Size.X, ly, lz + Position.Y * Chunk.Size.Z);
        public Vector3i ConvertLocalToWorld(Vector3i lb) =>
            (lb.X + Position.X * Chunk.Size.X, lb.Y, lb.Z + Position.Y * Chunk.Size.Z);
    }
}
