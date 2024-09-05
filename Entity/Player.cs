using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

using VoxelWorld.Managers;

namespace VoxelWorld.Entity
{
    public enum Movement
    {
        Nothing = 0,
        Forward,
        Backward,
        Left,
        Right,
        Up,
        Down,

        Place = 10,
        Destroy
    }

    public class Player
    {
        /// <summary>
        /// 
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public float Speed { get; }
        /// <summary>
        /// 
        /// </summary>
        public Vector3 Position { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Vector3i RoundedPosition { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Vector2i CurrentChunk { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public bool IsMovedToAnotherChunk { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        public Camera Camera { get; set; }
        /// <summary>
        /// Default value is 1 (stone block).
        /// </summary>
        public int SelectedBlock { get; set; }

        private double _lastLeftClickTime  = 0d;
        private double _lastRightClickTime = 0d;

        public Player(Player? player)
        {
            if (player is not null)
            {
                Camera          = new Camera(player.Camera);
                Position        = player.Position;
                RoundedPosition = RoundPosition();
                CurrentChunk    = ChunkManager.GetChunkPosition(RoundedPosition.Xz);
                SelectedBlock   = player.SelectedBlock;
            }
            else
            {
                Camera          = new Camera();
                Position        = (8, 36, 8);
                RoundedPosition = RoundPosition();
                CurrentChunk    = ChunkManager.GetChunkPosition(RoundedPosition.Xz);
                SelectedBlock   = 1;
            }

            IsMovedToAnotherChunk = false;
            Speed = 20f;
        }

        [Newtonsoft.Json.JsonConstructor]
        private Player(Camera camera, Vector3 position, Vector3i roundedPosition, Vector2i currentChunk, int selectedBlock)
        {
            Camera          = camera;
            Position        = position;
            RoundedPosition = roundedPosition;
            CurrentChunk    = currentChunk;
            SelectedBlock   = selectedBlock;
        }

        public void KeyboardInput(KeyboardState input, float time)
        {
            Vector3 direction = Vector3.Zero;

            if (input.IsKeyDown(Keys.W))
            {
                direction += Vector3.Normalize(Vector3.Cross(Camera.Right, -Vector3.UnitY));
            }
            if (input.IsKeyDown(Keys.S))
            {
                direction -= Vector3.Normalize(Vector3.Cross(Camera.Right, -Vector3.UnitY));
            }
            if (input.IsKeyDown(Keys.A))
            {
                direction -= Camera.Right;
            }
            if (input.IsKeyDown(Keys.D))
            {
                direction += Camera.Right;
            }
            if (input.IsKeyDown(Keys.Space))
            {
                direction += Vector3.UnitY;
            }
            if (input.IsKeyDown(Keys.LeftShift))
            {
                direction -= Vector3.UnitY;
            }

            if (direction != Vector3.Zero)
            {
                direction.Normalize();
            }

            Position += direction * Speed * time;

            var newRoundedPosition = RoundPosition();
            if (RoundedPosition != newRoundedPosition)
            {
                RoundedPosition = newRoundedPosition;

                var newCurrentChunk = ChunkManager.GetChunkPosition(newRoundedPosition.Xz);
                if (CurrentChunk != newCurrentChunk)
                {
                    CurrentChunk = newCurrentChunk;
                    IsMovedToAnotherChunk = true;
                }
            }
            else
            {
                IsMovedToAnotherChunk = false;
            }
        }

        public void MouseInput(MouseState input, double time)
        {
            Camera.RayCast(Position);

            if (input.IsButtonPressed(MouseButton.Left)) 
                _lastLeftClickTime = Camera.PressedDestroyBlock(_lastLeftClickTime);
            if (input.IsButtonDown(MouseButton.Left)) 
                _lastLeftClickTime = Camera.DownDestroyBlock(_lastLeftClickTime, time);

            if (input.IsButtonPressed(MouseButton.Right)) 
                _lastRightClickTime = Camera.PressedPlaceBlock(SelectedBlock, _lastRightClickTime);
            if (input.IsButtonDown(MouseButton.Right))
                _lastRightClickTime = Camera.DownPlaceBlock(SelectedBlock, _lastRightClickTime, time);
        }

        private Vector3i RoundPosition() =>
            ((int)MathF.Floor(Position.X), (int)MathF.Floor(Position.Y), (int)MathF.Floor(Position.Z));
    }
}