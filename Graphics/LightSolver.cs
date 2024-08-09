using OpenTK.Mathematics;
using VoxelWorld.World;

namespace VoxelWorld.Graphics
{
    public class LightSolver
    {
        /// <summary>
        /// 0 - red,
        /// 1 - green,
        /// 2 - blue,
        /// 3 - sun
        /// </summary>
        int Channel { get; set; }
        /// <summary>
        /// X - world X,
        /// Y - world Y,
        /// Z - world Z,
        /// W - light
        /// </summary>
        Queue<Vector4i> AddQueue { get; set; }
        /// <summary>
        /// X - world X,
        /// Y - world Y,
        /// Z - world Z,
        /// W - light
        /// </summary>
        Queue<Vector4i> RemoveQueue { get; set; }

        public LightSolver(int channel)
        {
            Channel       = channel;
            AddQueue      = [];
            RemoveQueue   = [];
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="wb"></param>
        public void Add(Vector3i wb)
        {
            Add(wb, Chunks.GetLight(wb, Channel));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="wx"></param>
        /// <param name="wy"></param>
        /// <param name="wz"></param>
        public void Add(int wx, int wy, int wz)
        {
            Add(wx, wy, wz, Chunks.GetLight(wx, wy, wz, Channel));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="wb"></param>
        /// <param name="emission"></param>
        public void Add(Vector3i wb, int emission)
        {
            if (emission < 2) return;

            AddQueue.Enqueue((wb.X, wb.Y, wb.Z, emission));
            Chunks.GetBlock(wb).SetLight(Channel, emission);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="wx"></param>
        /// <param name="wy"></param>
        /// <param name="wz"></param>
        /// <param name="emission"></param>
        public void Add(int wx, int wy, int wz, int emission)
        {
            if (emission < 2) return;

            AddQueue.Enqueue((wx, wy, wz, emission));
            Chunks.GetBlock(wx, wy, wz).SetLight(Channel, emission);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="wb"></param>
        public void Remove(Vector3i wb)
        {
            int light = Chunks.GetLight(wb, Channel);

            if (light == 0) return;

            RemoveQueue.Enqueue((wb.X, wb.Y, wb.Z, light));
            Chunks.GetBlock(wb).SetLight(Channel, 0);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="wx"></param>
        /// <param name="wy"></param>
        /// <param name="wz"></param>
        public void Remove(int wx, int wy, int wz)
        {
            int light = Chunks.GetLight(wx, wy, wz, Channel);

            if (light == 0) return;

            RemoveQueue.Enqueue((wx, wy, wz, light));
            Chunks.GetBlock(wx, wy, wz).SetLight(Channel, 0);
        }
        /// <summary>
        /// 
        /// </summary>
        public void Solve()
        {
            List<int> coords =
            [
                 0,  0,  1,
                 0,  0, -1,
                 0,  1,  0,
                 0, -1,  0,
                 1,  0,  0,
                -1,  0,  0,
            ];

            while (RemoveQueue.Count > 0)
            {
                Vector4i item = RemoveQueue.Dequeue();

                for (int i = 0; i < 6; i++)
                {
                    int x = item.X + coords[i * 3];
                    int y = item.Y + coords[i * 3 + 1];
                    int z = item.Z + coords[i * 3 + 2];

                    var block = Chunks.GetBlock(x, y, z);

                    if (block is not null)
                    {
                        int light = block.GetLight(Channel);

                        if (light > 0 && light == item.W - 1)
                        {
                            RemoveQueue.Enqueue((x, y, z, light));
                            block.SetLight(Channel, 0);
                        }
                        else if (light > item.W - 1)
                        {
                            AddQueue.Enqueue((x, y, z, light));
                        }
                    }
                }
            }

            while (AddQueue.Count > 0)
            {
                Vector4i item = AddQueue.Dequeue();

                if (item.W < 2) continue;

                for (int i = 0; i < 6; i++)
                {
                    int x = item.X + coords[i * 3];
                    int y = item.Y + coords[i * 3 + 1];
                    int z = item.Z + coords[i * 3 + 2];

                    var block = Chunks.GetBlock(x, y, z);

                    if (block is not null)
                    {
                        int light = block.GetLight(Channel);

                        if (block.IsLightPassing is true && light + 1 < item.W)
                        {
                            AddQueue.Enqueue((x, y, z, item.W - 1));
                            block.SetLight(Channel, item.W - 1);
                        }
                    }
                }
            }
        }
    }
}