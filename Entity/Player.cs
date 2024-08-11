﻿using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace VoxelWorld.Entity
{
    // for the future
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
        public float Speed { get; set; } = 8f;
        public Vector3 Position { get; set; }
        public Camera Camera { get; set; }
        public string SelectedBlock { get; set; } = "stone";
        /// <summary>
        /// Default value is 5.
        /// </summary>
        public float RayDistance { get; set; } = 5f;

        private bool _isGrabbed = false;
        private bool _firstMove = true;
        private Vector2 _lastPos;

        private double _lastLeftClickTime  = 0d;
        private double _lastRightClickTime = 0d;

        public Player(Vector3 position)
        {
            Position = position;
            Camera   = new Camera();
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
        }

        public CursorState MouseInput(MouseState input, CursorState state, double time)
        {
            if (input.IsButtonPressed(MouseButton.Middle))
            {
                if (_isGrabbed is true)
                {
                    state = CursorState.Normal;
                    _isGrabbed = false;
                    _firstMove = true;
                }
                else
                {
                    state = CursorState.Grabbed;
                    _isGrabbed = true;
                }
            }

            if (_isGrabbed is true)
            {
                if (_firstMove is true)
                {
                    _lastPos   = (input.X, input.Y);
                    _firstMove = false;
                }
                else
                {
                    float deltaX = input.X - _lastPos.X;
                    float deltaY = _lastPos.Y - input.Y;
                    _lastPos     = (input.X, input.Y);

                    Camera.Yaw   += deltaX * Camera.Sensitivity;
                    Camera.Pitch += deltaY * Camera.Sensitivity;

                    if (Camera.Pitch > 89.999f)
                    {
                        Camera.Pitch = 89.999f;
                    }
                    if (Camera.Pitch < -89.999f)
                    {
                        Camera.Pitch = -89.999f;
                    }

                    Camera.UpdateVectors();
                }
            }

            Camera.RayCast(Position, RayDistance);

            if (input.IsButtonPressed(MouseButton.Left)) 
                _lastLeftClickTime = Camera.PressedDestroyBlock(_lastLeftClickTime);
            if (input.IsButtonDown(MouseButton.Left)) 
                _lastLeftClickTime = Camera.DownDestroyBlock(_lastLeftClickTime, time);

            if (input.IsButtonPressed(MouseButton.Right)) 
                _lastRightClickTime = Camera.PressedPlaceBlock(SelectedBlock, _lastRightClickTime);
            if (input.IsButtonDown(MouseButton.Right))
                _lastRightClickTime = Camera.DownPlaceBlock(SelectedBlock, _lastRightClickTime, time);

            return state;
        }

        public void MouseScroll(float offsetY)
        {
            Camera.FOV -= offsetY * 10;
            if (Camera.FOV < 20f)
            {
                Camera.FOV = 20f;
            }
            if (Camera.FOV > 140f)
            {
                Camera.FOV = 140f;
            }
        }
    }
}