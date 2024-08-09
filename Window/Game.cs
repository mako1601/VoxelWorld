using System.Diagnostics;

using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using static OpenTK.Graphics.OpenGL4.GL;

using VoxelWorld.Entity;
using VoxelWorld.World;
using VoxelWorld.Graphics.Renderer;

namespace VoxelWorld.Window
{
    public struct Parameters
    {
        public bool IsWhiteWorld { get; private set; }
        public Vector3 PlayerPosition { get; private set; }
        
        public Parameters(bool isWhiteWorld, Player player)
        {
            IsWhiteWorld = isWhiteWorld;
            PlayerPosition = player.Position;
        }

        public void Update(bool isWhiteWorld, Player player)
        {
            IsWhiteWorld = isWhiteWorld;
            PlayerPosition = player.Position;
        }
    }

    public struct Matrixes
    {
        public Matrix4 View { get; private set; }
        public Matrix4 Projection { get; private set; }

        public Matrixes(Player player)
        {
            View = player.Camera.GetViewMatrix(player.Position);
            Projection = player.Camera.GetProjectionMatrix();
        }

        public void Update(Player player)
        {
            View = player.Camera.GetViewMatrix(player.Position);
            Projection = player.Camera.GetProjectionMatrix();
        }
    }

    public class Game : GameWindow
    {
        // temporary disfigurement
        private Chunks _chunks;
        private Outline _outline;
        private Skybox _skybox;
        private Interface _interface;
        private readonly Player _player;

        private Parameters _parameters;
        private Matrixes _matrixes;
        private uint _frameCount = 0;
        private uint _fps = 0;
        private double _timer = 0;
        private bool _isWhiteWorld = false;

        public Game(int width, int height)
            : base(new GameWindowSettings
            {
                UpdateFrequency = 0, // unlimited fps
            },
            new NativeWindowSettings
            {
                APIVersion = new Version(4, 6),
                API = ContextAPI.OpenGL,
                Profile = ContextProfile.Core,
                StartFocused = true,
                //Vsync = VSyncMode.Off,
                //NumberOfSamples = 16, // MSAA x2, x4, x8, x16
                Title = "VoxelWorld",
                MinimumSize = (480, 360),
            })
        {
            _player = new Player((8, 32, 8));
            _player.Camera.AspectRatio = width / (float)height;
            CenterWindow((width, height));
        }

        public Game(int width, int height, string title)
            : base(new GameWindowSettings
            {
                UpdateFrequency = 0, // unlimited fps
            },
            new NativeWindowSettings
            {
                APIVersion = new Version(4, 6),
                API = ContextAPI.OpenGL,
                Profile = ContextProfile.Core,
                //Vsync = VSyncMode.On,
                //NumberOfSamples = 16, // MSAA x2, x4, x8, x16
                Title = title,
                MinimumSize = (480, 360),
            })
        {
            _player = new Player((8, 32, 8));
            _player.Camera.AspectRatio = width / (float)height;
            CenterWindow((width, height));
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            // OpenGL settings
            Enable(EnableCap.DepthTest);
            DepthFunc(DepthFunction.Less);

            // init
            _skybox = new Skybox();
            _chunks = new Chunks();
            _outline = new Outline();
            _interface = new Interface(_player.SelectedBlock);

            _matrixes = new Matrixes(_player);
            _parameters = new Parameters(_isWhiteWorld, _player);
        }

        protected override void OnUnload()
        {
            base.OnUnload();

            _interface.Delete();
            _skybox.Delete();
            _outline.Delete();
            _chunks.Delete();
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);

            Viewport(0, 0, ClientSize.X, ClientSize.Y);
            _player.Camera.AspectRatio = ClientSize.X / (float)ClientSize.Y;
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            _frameCount++;
            _timer += args.Time;
            
            if (_timer > 1)
            {
                _fps = _frameCount;
                Process curPorcess = Process.GetCurrentProcess();
                Title = $"VoxelWorld FPS: {_fps}, {args.Time * 1000d:0.0000}ms, "
                    + $"RAM: {curPorcess.WorkingSet64 / (1024f * 1024f):0.000}Mb";
                curPorcess.Dispose();
                _frameCount = 0;
                _timer -= 1;
            }

            if (IsFocused is false) return;

            KeyboardState keyboard = KeyboardState;
            MouseState mouse = MouseState;

            if (keyboard.IsKeyPressed(Keys.Escape))
            {
                Close();
            }
            if (keyboard.IsKeyPressed(Keys.Q))
            {
                _interface.DebugInfo = !_interface.DebugInfo;
            }
            if (keyboard.IsKeyPressed(Keys.Z))
            {
                PolygonMode(MaterialFace.FrontAndBack, OpenTK.Graphics.OpenGL4.PolygonMode.Line);
            }
            if (keyboard.IsKeyPressed(Keys.X))
            {
                PolygonMode(MaterialFace.FrontAndBack, OpenTK.Graphics.OpenGL4.PolygonMode.Fill);
            }
            if (keyboard.IsKeyPressed(Keys.O))
            {
                _isWhiteWorld = !_isWhiteWorld;
            }

            _player.KeyboardInput(keyboard, (float)args.Time);
            CursorState = _player.MouseInput(mouse, CursorState, args.Time);
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            _matrixes.Update(_player);
            _parameters.Update(_isWhiteWorld, _player);

            _skybox.Draw(_matrixes);
            _chunks.Draw(_matrixes, _parameters);
            _outline.Draw(_matrixes, _player.Camera.Ray.Block);
            _interface.Draw(Color4.Yellow, new Interface.Info { Player = _player, FPS = _fps, WindowSize = ClientSize });

            Context.SwapBuffers();
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            _player.MouseScroll(e.OffsetY);
        }
    }
}
