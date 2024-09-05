using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

using VoxelWorld.World;
using VoxelWorld.Managers;
using SharpFont;

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
        public float Sensitivity { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public float FOV { get; set; }
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
        public int Width { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public int Height { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public Ray Ray { get; private set; } = new Ray();
        /// <summary>
        /// For some reason that is not clear to me, the value of NativeWindow.MousePosition
        /// only changes after two frames, which makes me implement this hack in the camera
        /// rotation control, and it also helps solve the problem of cursor movement when
        /// changing the window mode.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public byte MoveCount { get; set; }

        /// <summary>
        /// Default value is 5.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        private float _rayDistance;

        public Camera()
        {
            FOV   =  90f;
            Up    =  Vector3.UnitY;
            Front = -Vector3.UnitZ;
            Right =  Vector3.UnitX;
            Pitch =   0f;
            Yaw   = -90f;

            Sensitivity  = 1f;
            _rayDistance = 12f;
            MoveCount    = 0;
        }
        public Camera(Camera camera)
        {
            FOV   = camera.FOV;
            Up    = camera.Up;
            Front = camera.Front;
            Right = camera.Right;
            Pitch = camera.Pitch;
            Yaw   = camera.Yaw;

            Sensitivity  = camera.Sensitivity;
            _rayDistance = 12f;
            MoveCount    = 0;
        }

        [Newtonsoft.Json.JsonConstructor]
        private Camera(float fov, Vector3 up, Vector3 front, Vector3 right, float pitch, float yaw, float sensitivity)
        {
            FOV   = fov;
            Up    = up;
            Front = front;
            Right = right;
            Pitch = pitch;
            Yaw   = yaw;

            Sensitivity = sensitivity;
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
        public Matrix4 GetProjectionMatrix() => Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(FOV), (float)Width / Height, 0.001f, 1000f);
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
        /// <param name="mouseState"></param>
        public void Move(CursorState cursorState, MouseState mouseState)
        {
            if (cursorState is CursorState.Grabbed)
            {
                if (MoveCount < 2)
                {
                    MoveCount++;
                    return;
                }

                if (mouseState.Delta.X != 0 || mouseState.Delta.Y != 0)
                {
                    Yaw   += mouseState.Delta.X * Sensitivity / 10f;
                    Pitch -= mouseState.Delta.Y * Sensitivity / 10f;

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
        public void RayCast(Vector3 position)
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
            while (t <= _rayDistance)
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
