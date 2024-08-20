using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

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
        /// Default value is 8.
        /// </summary>
        public float Speed { get; set; } = 20f;
        public Vector3 Position { get; set; }
        public Vector3i RoundedPosition { get; set; }
        public Vector2i? CurrentChunk { get; set; }
        public bool IsMoved { get; private set; } = true;
        public Camera Camera { get; set; }
        /// <summary>
        /// Default value is 1 (stone block).
        /// </summary>
        public int SelectedBlock { get; set; } = 1;
        /// <summary>
        /// Default value is 5.
        /// </summary>
        public float RayDistance { get; set; } = 12f;

        private double _lastLeftClickTime  = 0d;
        private double _lastRightClickTime = 0d;

        public Player(Vector3 playerPosition, Vector2 cursorPosition)
        {
            Position        = playerPosition;
            RoundedPosition = RoundPosition();
            Camera          = new Camera(cursorPosition);
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
                IsMoved = true;
            }
            else
            {
                IsMoved = false;
            }
        }

        public void MouseInput(MouseState input, double time)
        {
            Camera.RayCast(Position, RayDistance);

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