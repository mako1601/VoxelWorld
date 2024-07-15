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
        #region properties
        /// <summary>
        /// default: 8f (8 blocks per second)
        /// </summary>
        public float Speed { get; set; } = 8f;
        public Vector3 Position { get; set; }
        public Camera Camera { get; set; }
        public string SelectedBlock { get; set; } = "stone";
        /// <summary>
        /// default: 5f (5 blocks)
        /// </summary>
        public float RayDistance { get; set; } = 5f;
        #endregion

        #region fields
        private bool _isGrabbed = false;
        private bool _firstMove = true;
        private Vector2 _lastPos;

        private double _lastLeftClickTime = 0d;
        private double _lastRightClickTime = 0d;
        #endregion

        #region constructor
        public Player(Vector3 position)
        {
            Position = position;
            Camera = new Camera();
        }
        #endregion

        #region methods
        public void KeyboardInput(KeyboardState input, float time)
        {
            #region Movement
            float velocity = Speed * time;
            if (input.IsKeyDown(Keys.W))
            {
                Position += Vector3.Normalize(Vector3.Cross(Camera.Right, -Vector3.UnitY)) * velocity;
            }
            if (input.IsKeyDown(Keys.S))
            {
                Position -= Vector3.Normalize(Vector3.Cross(Camera.Right, -Vector3.UnitY)) * velocity;
            }
            if (input.IsKeyDown(Keys.A))
            {
                Position -= Camera.Right * velocity;
            }
            if (input.IsKeyDown(Keys.D))
            {
                Position += Camera.Right * velocity;
            }
            if (input.IsKeyDown(Keys.Space))
            {
                Position += Vector3.UnitY * velocity;
            }
            if (input.IsKeyDown(Keys.LeftShift))
            {
                Position -= Vector3.UnitY * velocity;
            }
            #endregion

            #region Block selection 1-8
            if (input.IsKeyPressed(Keys.D1))
            {
                SelectedBlock = "stone";
                Console.WriteLine($"SelectedBlock = \'{SelectedBlock}\'");
            }
            if (input.IsKeyPressed(Keys.D2))
            {
                SelectedBlock = "dirt";
                Console.WriteLine($"SelectedBlock = \'{SelectedBlock}\'");
            }
            if (input.IsKeyPressed(Keys.D3))
            {
                SelectedBlock = "grass";
                Console.WriteLine($"SelectedBlock = \'{SelectedBlock}\'");
            }
            if (input.IsKeyPressed(Keys.D4))
            {
                SelectedBlock = "sand";
                Console.WriteLine($"SelectedBlock = \'{SelectedBlock}\'");
            }
            if (input.IsKeyPressed(Keys.D5))
            {
                SelectedBlock = "gravel";
                Console.WriteLine($"SelectedBlock = \'{SelectedBlock}\'");
            }
            if (input.IsKeyPressed(Keys.D6))
            {
                SelectedBlock = "oak_log";
                Console.WriteLine($"SelectedBlock = \'{SelectedBlock}\'");
            }
            if (input.IsKeyPressed(Keys.D7))
            {
                SelectedBlock = "oak_leaves";
                Console.WriteLine($"SelectedBlock = \'{SelectedBlock}\'");
            }
            if (input.IsKeyPressed(Keys.D8))
            {
                SelectedBlock = "glass";
                Console.WriteLine($"SelectedBlock = \'{SelectedBlock}\'");
            }
            #endregion
        }

        public CursorState MouseInput(MouseState input, CursorState state, double time)
        {
            if (input.IsButtonPressed(MouseButton.Middle))
            {
                if (_isGrabbed == true)
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

            if (_isGrabbed == true)
            {
                if (_firstMove == true)
                {
                    _lastPos = new Vector2(input.X, input.Y);
                    _firstMove = false;
                }
                else
                {
                    float deltaX = input.X - _lastPos.X;
                    float deltaY = _lastPos.Y - input.Y;
                    _lastPos = new Vector2(input.X, input.Y);

                    Camera.Yaw += deltaX * Camera.Sensitivity;
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
        #endregion
    }
}