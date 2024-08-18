using OpenTK.Mathematics;

using VoxelWorld.Managers;

namespace VoxelWorld.World
{
    public class Lightmap
    {
        public Dictionary<Vector3i, ushort> Map { get; set; }
        public Vector2i Position { get; }

        public Lightmap(Vector2i position)
        {
            Map = [];
            FillMap();
            Position = position;
        }

        private void FillMap()
        {
            for (int x = 0; x < Chunk.Size.X; x++)
            {
                for (int z = 0; z < Chunk.Size.Z; z++)
                {
                    for (int y = Chunk.Size.Y; y > -1; y--)
                    {
                        Map.Add((x, y, z), 0x0000);
                    }
                }
            }
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
            if(Map.TryGetValue((lx, ly, lz), out var value))
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
            if (Map.TryGetValue((lx, ly, lz), out var value))
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
            if (Map.TryGetValue((lx, ly, lz), out var value))
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
            if (Map.TryGetValue((lx, ly, lz), out var value))
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
            if (Map.TryGetValue((lx, ly, lz), out var value))
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
            if (Map.TryGetValue((lx, ly, lz), out var oldValue))
            {
                Map[(lx, ly, lz)] = (ushort)(oldValue & (0xFFFF & ~(0xF << 12 - channel * 4)) | value << 12 - channel * 4);
            }
            else
            {
                throw new Exception();
            }
        }
        public void SetLightR(int lx, int ly, int lz, int value) 
        {
            if (Map.TryGetValue((lx, ly, lz), out var oldValue))
            {
                Map[(lx, ly, lz)] = (ushort)(oldValue & 0x0FFF | value << 12);
            }
            else
            {
                throw new Exception();
            }
        }
        public void SetLightG(int lx, int ly, int lz, int value)
        {
            if (Map.TryGetValue((lx, ly, lz), out var oldValue))
            {
                Map[(lx, ly, lz)] = (ushort)(oldValue & 0xF0FF | value << 8);
            }
            else
            {
                throw new Exception();
            }
        }
        public void SetLightB(int lx, int ly, int lz, int value)
        {
            if (Map.TryGetValue((lx, ly, lz), out var oldValue))
            {
                Map[(lx, ly, lz)] = (ushort)(oldValue & 0xFF0F | value << 4);
            }
            else
            {
                throw new Exception();
            }
        }
        public void SetLightS(int lx, int ly, int lz, int value)
        {
            if (Map.TryGetValue((lx, ly, lz), out var oldValue))
            {
                Map[(lx, ly, lz)] = (ushort)(oldValue & 0xFFF0 | value);
            }
            else
            {
                throw new Exception();
            }
        }

        public Vector3i ConvertLocalToWorld(int lx, int ly, int lz) =>
            (lx + Position.X * Chunk.Size.X, ly, lz + Position.Y * Chunk.Size.Z);
        public Vector3i ConvertLocalToWorld(Vector3i lb) =>
            (lb.X + Position.X * Chunk.Size.X, lb.Y, lb.Z + Position.Y * Chunk.Size.Z);
    }
}
