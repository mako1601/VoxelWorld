using OpenTK.Mathematics;
using OpenTK.Windowing.Common;

using VoxelWorld.World;
using VoxelWorld.Managers;

namespace VoxelWorld.Entity
{
    public struct Ray
    {
        public Block? Block { get; set; }
        public Vector3i Position { get; set; }
        public Vector3i Normal { get; set; }
        public Vector3 End { get; set; }

        public Ray()
        {
            Block    = null;
            Position = Vector3i.Zero;
            Normal   = Vector3i.Zero;
            End      = Vector3.Zero;
        }

        public Ray(Block block, Vector3i position, Vector3i normal, Vector3 end)
        {
            Block    = block;
            Position = position;
            Normal   = normal;
            End      = end;
        }
    }

    public class Camera
    {
        /// <summary>
        /// 
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public float Sensitivity { get; set; } = 0.1f;
        /// <summary>
        /// 
        /// </summary>
        public float FOV { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public bool FirstMove { get; set; } = true;
        /// <summary>
        /// 
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public Vector2 LastPosition { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Vector3 Up { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Vector3 Front { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Vector3 Right { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public float Pitch { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public float Yaw { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public float AspectRatio { get; set; } = 0f;
        /// <summary>
        /// 
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public Ray Ray { get; set; } = new Ray();

        public Camera(Vector2 cursorPosition, Camera? camera = null)
        {
            LastPosition = cursorPosition;

            FOV   = camera?.FOV   ?? 90f;
            Up    = camera?.Up    ?? Vector3.UnitY;
            Front = camera?.Front ?? -Vector3.UnitZ;
            Right = camera?.Right ?? Vector3.UnitX;
            Pitch = camera?.Pitch ?? 0f;
            Yaw   = camera?.Yaw   ?? -90f;

            UpdateVectors();
        }

        [Newtonsoft.Json.JsonConstructor]
        private Camera(float fov, Vector3 up, Vector3 front, Vector3 right, float pitch, float yaw)
        {
            FOV   = fov;
            Up    = up;
            Front = front;
            Right = right;
            Pitch = pitch;
            Yaw   = yaw;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public Matrix4 GetViewMatrix(Vector3 position) => Matrix4.LookAt(position, position + Front, Up);
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Matrix4 GetProjectionMatrix() => Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(FOV), AspectRatio, 0.001f, 1000f);
        /// <summary>
        /// 
        /// </summary>
        private void UpdateVectors()
        {
            Vector3 front;
            front.X = MathF.Cos(MathHelper.DegreesToRadians(Pitch)) * MathF.Cos(MathHelper.DegreesToRadians(Yaw));
            front.Y = MathF.Sin(MathHelper.DegreesToRadians(Pitch));
            front.Z = MathF.Cos(MathHelper.DegreesToRadians(Pitch)) * MathF.Sin(MathHelper.DegreesToRadians(Yaw));

            Front = Vector3.Normalize(front);

            Right = Vector3.Normalize(Vector3.Cross(Front, Vector3.UnitY));
            Up    = Vector3.Normalize(Vector3.Cross(Right, Front));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cursorState"></param>
        /// <param name="position"></param>
        public void Move(CursorState cursorState, Vector2 position)
        {
            if (cursorState is CursorState.Grabbed)
            {
                if (FirstMove is true)
                {
                    LastPosition = position;
                    FirstMove = false;
                }
                else
                {
                    float deltaX = position.X - LastPosition.X;
                    float deltaY = LastPosition.Y - position.Y;
                    LastPosition = position;

                    Yaw   += deltaX * Sensitivity;
                    Pitch += deltaY * Sensitivity;

                    Pitch = Math.Clamp(Pitch, -89.999f, 89.999f);

                    UpdateVectors();
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lastClickTime"></param>
        /// <returns></returns>
        public double PressedDestroyBlock(double lastClickTime)
        {
            if (Ray.Block is not null && Ray.Block.Type is not Block.TypeOfBlock.Air)
            {
                ChunkManager.SetBlock(0, Ray.Position, Movement.Destroy);
                return 0;
            }

            return lastClickTime;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lastClickTime"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        public double DownDestroyBlock(double lastClickTime, double time)
        {
            if (lastClickTime >= 0.2 && Ray.Block is not null && Ray.Block.Type is not Block.TypeOfBlock.Air)
            {
                ChunkManager.SetBlock(0, Ray.Position, Movement.Destroy);
                return 0;
            }

            return lastClickTime + time;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="lastClickTime"></param>
        /// <returns></returns>
        public double PressedPlaceBlock(int id, double lastClickTime)
        {
            if (Ray.Block is not null && Ray.Block.Type is not Block.TypeOfBlock.Air)
            {
                ChunkManager.SetBlock(id, Ray.Position + Ray.Normal, Movement.Place);
                return 0;
            }

            return lastClickTime;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="lastClickTime"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        public double DownPlaceBlock(int id, double lastClickTime, double time)
        {
            if (lastClickTime >= 0.2 && Ray.Block is not null && Ray.Block.Type is not Block.TypeOfBlock.Air)
            {
                ChunkManager.SetBlock(id, Ray.Position + Ray.Normal, Movement.Place);
                return 0;
            }

            return lastClickTime + time;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="position"></param>
        /// <param name="maxLength"></param>
        public void RayCast(Vector3 position, float maxLength)
        {
            float posX = position.X;
            float posY = position.Y;
            float posZ = position.Z;

            float frontX = Front.X;
            float frontY = Front.Y;
            float frontZ = Front.Z;

            int ix = Convert.ToInt32(MathF.Floor(posX));
            int iy = Convert.ToInt32(MathF.Floor(posY));
            int iz = Convert.ToInt32(MathF.Floor(posZ));

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
                var block = ChunkManager.GetBlock(ix, iy, iz, true);
                if (block is not null && block.Type is not Block.TypeOfBlock.Air)
                {
                    end.X = posX + t * frontX;
                    end.Y = posY + t * frontY;
                    end.Z = posZ + t * frontZ;

                    Vector3i normal = Vector3i.Zero;

                    if (steppedIndex == 0) normal.X = -stepx;
                    if (steppedIndex == 1) normal.Y = -stepy;
                    if (steppedIndex == 2) normal.Z = -stepz;

                    Ray = new Ray(block, (ix, iy, iz), normal, end);
                    return;
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

            Ray = new Ray();
            return;
        }
    }
}
