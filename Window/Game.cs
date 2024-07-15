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
        #region fields
        // temporary disfigurement
        private Chunks chunks;
        private Outline outline;
        private Skybox skybox;
        private Interface gameInterface;
        private readonly Player player;

        private Parameters parameters;
        private Matrixes matrixes;
        private uint frameCount = 0;
        private uint FPS = 0;
        private double timer = 0;
        private bool _isWhiteWorld = false;
        #endregion

        #region constructors
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
                MinimumSize = new(480, 360),
            })
        {
            player = new Player(new(8, 32, 8));
            player.Camera.AspectRatio = width / (float)height;
            CenterWindow(new Vector2i(width, height));
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
                MinimumSize = new(480, 360),
            })
        {
            player = new Player(new(8, 32, 8));
            player.Camera.AspectRatio = width / (float)height;
            CenterWindow(new Vector2i(width, height));
        }
        #endregion

        #region methods
        protected override void OnLoad()
        {
            base.OnLoad();

            // OpenGL settings
            Enable(EnableCap.DepthTest);
            DepthFunc(DepthFunction.Less);

            // init
            skybox = new Skybox();
            chunks = new Chunks();
            outline = new Outline();
            gameInterface = new Interface(player.SelectedBlock);

            matrixes = new Matrixes(player);
            parameters = new Parameters(_isWhiteWorld, player);
        }

        protected override void OnUnload()
        {
            base.OnUnload();

            gameInterface.Delete();
            skybox.Delete();
            outline.Delete();
            chunks.Delete();
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);

            Viewport(0, 0, ClientSize.X, ClientSize.Y);
            player.Camera.AspectRatio = ClientSize.X / (float)ClientSize.Y;
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            frameCount++;
            timer += args.Time;
            
            if (timer > 1)
            {
                FPS = frameCount;
                Process curPorcess = Process.GetCurrentProcess();
                Title = $"VoxelWorld FPS: {FPS}, {args.Time * 1000d:0.0000}ms, "
                    + $"RAM: {curPorcess.WorkingSet64 / (1024f * 1024f):0.000}Mb";
                curPorcess.Dispose();
                frameCount = 0;
                timer -= 1;
            }

            if (IsFocused == false) return;

            KeyboardState keyboard = KeyboardState;
            MouseState mouse = MouseState;

            if (keyboard.IsKeyPressed(Keys.Escape))
            {
                Close();
            }
            if (keyboard.IsKeyPressed(Keys.Q))
            {
                gameInterface.DebugInfo = !gameInterface.DebugInfo;
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

            player.KeyboardInput(keyboard, (float)args.Time);
            CursorState = player.MouseInput(mouse, CursorState, args.Time);
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            matrixes.Update(player);
            parameters.Update(_isWhiteWorld, player);

            skybox.Draw(matrixes);
            chunks.Draw(matrixes, parameters);
            outline.Draw(matrixes, player.Camera.Ray.Block);
            gameInterface.Draw(Color4.Black, new Interface.Info { Player = player, FPS = FPS, WindowSize = ClientSize });

            Context.SwapBuffers();
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            player.MouseScroll(e.OffsetY);
        }
        #endregion
    }
}
