using OpenTK.Mathematics;

using VoxelWorld.World;

namespace VoxelWorld.Entity
{
    public struct Ray
    {
        public Block? Block { get; set; }
        public Vector3i Normal { get; set; }
        public Vector3 End { get; set; }

        public Ray()
        {
            Block = null;
            Normal = new(0, 0, 0);
            End = new(0, 0, 0);
        }

        public Ray(Block block, Vector3i normal, Vector3 end)
        {
            Block = block;
            Normal = normal;
            End = end;
        }
    }

    public class Camera
    {
        #region properties
        public float Sensitivity { get; set; } = 0.1f;
        public float FOV { get; set; } = 90f;

        public Vector3 Up { get; set; } = Vector3.UnitY;
        public Vector3 Front { get; set; } = -Vector3.UnitZ;
        public Vector3 Right { get; set; } = Vector3.UnitX;

        public float Pitch { get; set; } = 0f;
        public float Yaw { get; set; } = -90f;

        public float AspectRatio { get; set; } = 0f;

        public Ray Ray { get; set; } = new Ray();
        #endregion

        #region constructor
        public Camera() { UpdateVectors(); }
        #endregion

        #region methods
        public Matrix4 GetViewMatrix(Vector3 position) =>
            Matrix4.LookAt(position, position + Front, Up);
        public Matrix4 GetProjectionMatrix() =>
            Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(FOV),
            AspectRatio, 0.001f, 1000f);

        public void UpdateVectors()
        {
            Vector3 front;
            front.X = MathF.Cos(MathHelper.DegreesToRadians(Pitch)) * MathF.Cos(MathHelper.DegreesToRadians(Yaw));
            front.Y = MathF.Sin(MathHelper.DegreesToRadians(Pitch));
            front.Z = MathF.Cos(MathHelper.DegreesToRadians(Pitch)) * MathF.Sin(MathHelper.DegreesToRadians(Yaw));

            Front = Vector3.Normalize(front);

            Right = Vector3.Normalize(Vector3.Cross(Front, Vector3.UnitY));
            Up = Vector3.Normalize(Vector3.Cross(Right, Front));
        }

        public double PressedDestroyBlock(double lastClickTime)
        {
            if (Ray.Block != null && Ray.Block.Name != "air")
            {
                Chunks.SetBlock("air", Ray);
                return 0;
            }

            return lastClickTime;
        }
        public double DownDestroyBlock(double lastClickTime, double time)
        {
            if (lastClickTime >= 0.2 && Ray.Block != null && Ray.Block.Name != "air")
            {
                Chunks.SetBlock("air", Ray);
                return 0;
            }

            return lastClickTime + time;
        }

        public double PressedPlaceBlock(string block, double lastClickTime)
        {
            if (Ray.Block != null && Ray.Block.Name != "air")
            {
                Chunks.SetBlock(block, Ray);
                return 0;
            }

            return lastClickTime;
        }
        public double DownPlaceBlock(string block, double lastClickTime, double time)
        {
            if (lastClickTime >= 0.2 && Ray.Block != null && Ray.Block.Name != "air")
            {
                Chunks.SetBlock(block, Ray);
                return 0;
            }

            return lastClickTime + time;
        }

        public void RayCast(Vector3 position, float maxLength)
        {
            float posX = position.X;
            float posY = position.Y;
            float posZ = position.Z;

            float frontX = Front.X;
            float frontY = Front.Y;
            float frontZ = Front.Z;

            int ix = Convert.ToInt32(MathHelper.Floor(posX));
            int iy = Convert.ToInt32(MathHelper.Floor(posY));
            int iz = Convert.ToInt32(MathHelper.Floor(posZ));

            int stepx = frontX > 0f ? 1 : -1;
            int stepy = frontY > 0f ? 1 : -1;
            int stepz = frontZ > 0f ? 1 : -1;

            float inf = float.PositiveInfinity;

            float txDelta = frontX == 0f ? inf : MathF.Abs(1f / frontX);
            float tyDelta = frontY == 0f ? inf : MathF.Abs(1f / frontY);
            float tzDelta = frontZ == 0f ? inf : MathF.Abs(1f / frontZ);

            float xdist = stepx > 0 ? (ix + 1 - posX) : (posX - ix);
            float ydist = stepy > 0 ? (iy + 1 - posY) : (posY - iy);
            float zdist = stepz > 0 ? (iz + 1 - posZ) : (posZ - iz);

            float txMax = txDelta < inf ? txDelta * xdist : inf;
            float tyMax = tyDelta < inf ? tyDelta * ydist : inf;
            float tzMax = tzDelta < inf ? tzDelta * zdist : inf;

            Vector3 end = new();

            int steppedIndex = -1;
            float t = 0f;
            while (t <= maxLength)
            {
                Block? block = Chunks.GetBlock(ix, iy, iz);
                if (block != null && block.Name != "air")
                {
                    end.X = posX + t * frontX;
                    end.Y = posY + t * frontY;
                    end.Z = posZ + t * frontZ;

                    Vector3i normal = Vector3i.Zero;

                    if (steppedIndex == 0) normal.X = -stepx;
                    if (steppedIndex == 1) normal.Y = -stepy;
                    if (steppedIndex == 2) normal.Z = -stepz;

                    Ray = new Ray(block, normal, end); return;
                }
                if (txMax < tyMax)
                {
                    if (txMax < tzMax)
                    {
                        ix += stepx;
                        t = txMax;
                        txMax += txDelta;
                        steppedIndex = 0;
                    }
                    else
                    {
                        iz += stepz;
                        t = tzMax;
                        tzMax += tzDelta;
                        steppedIndex = 2;
                    }
                }
                else
                {
                    if (tyMax < tzMax)
                    {
                        iy += stepy;
                        t = tyMax;
                        tyMax += tyDelta;
                        steppedIndex = 1;
                    }
                    else
                    {
                        iz += stepz;
                        t = tzMax;
                        tzMax += tzDelta;
                        steppedIndex = 2;
                    }
                }
            }

            end.X = posX + t * frontX;
            end.Y = posY + t * frontY;
            end.Z = posZ + t * frontZ;

            Ray = new Ray(); return;
        }
        #endregion
    }
}
