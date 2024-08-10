using System.Diagnostics;

using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using static OpenTK.Graphics.OpenGL.GL;

using StbImageSharp;

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

        private Parameters _parameters;
        private Matrixes _matrixes;

        private Player Player { get; }
        private Interface Interface { get; set; }
        private uint FPS { get; set; } = 0;
        private uint FrameCount { get; set; } = 0;
        private double Timer { get; set; } = 0;
        private bool IsWhiteWorld  { get; set; } = false;
        private bool IsPolygonMode { get; set; } = false;

        public Game(int width, int height, string title = "VoxelWorld") :
            base(
                new GameWindowSettings
                {
                    // IsMultiThreaded
                    // RenderFrequency
                    UpdateFrequency = 0, // unlimited fps
                    // Win32SuspendTimerOnDrag
                },
                new NativeWindowSettings
                {
                    // SharedContext
                    // Icon
                    // IsEventDriven
                    API = ContextAPI.OpenGL,
                    Profile = ContextProfile.Core,
                    // Flags
                    // AutoLoadBindings
                    APIVersion = new Version(4, 6),
                    // CurrentMonitor
                    Title = title,
                    // StartFocused
                    // StartVisible
                    // WindowSotate
                    WindowBorder = WindowBorder.Resizable,
                    // Location
                    // Size
                    MinimumClientSize = (480, 360),
                    // MaximumClientSize
                    // AspectRatio
                    // IsFullscreen
                    //NumberOfSamples = 16 // MSAA x2, x4, x8, x16
                }
            )
        {
            var texture = ImageResult.FromStream(File.OpenRead($"resources/textures/utilities/logo.png"), ColorComponents.RedGreenBlueAlpha);
            Icon = new OpenTK.Windowing.Common.Input.WindowIcon(new OpenTK.Windowing.Common.Input.Image(texture.Width, texture.Height, texture.Data));

            Player = new Player((8, 32, 8));
            Player.Camera.AspectRatio = width / (float)height;

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
            Interface = new Interface(Player.SelectedBlock);

            _matrixes = new Matrixes(Player);
            _parameters = new Parameters(IsWhiteWorld, Player);
        }

        protected override void OnUnload()
        {
            base.OnUnload();

            Interface.Delete();
            _skybox.Delete();
            _outline.Delete();
            _chunks.Delete();
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);

            Viewport(0, 0, ClientSize.X, ClientSize.Y);
            Player.Camera.AspectRatio = ClientSize.X / (float)ClientSize.Y;
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            if (IsFocused is false) return;

            FrameCount++;
            Timer += args.Time;
            
            if (Timer > 1)
            {
                FPS = FrameCount;
                Process curPorcess = Process.GetCurrentProcess();
                Title = $"VoxelWorld FPS: {FPS}, {args.Time * 1000d:0.0000}ms, "
                    + $"RAM: {curPorcess.WorkingSet64 / (1024f * 1024f):0.000}Mb";
                curPorcess.Dispose();
                FrameCount = 0;
                Timer -= 1;
            }

            Player.KeyboardInput(KeyboardState, (float)args.Time);
            CursorState = Player.MouseInput(MouseState, CursorState, args.Time);
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            if (IsFocused is false) return;

            Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            _matrixes.Update(Player);
            _parameters.Update(IsWhiteWorld, Player);

            _skybox.Draw(_matrixes);
            _chunks.Draw(_matrixes, _parameters);
            _outline.Draw(_matrixes, Player.Camera.Ray.Block);
            Interface.Draw(Color3.Yellow, new Interface.Info { Player = Player, FPS = FPS, WindowSize = ClientSize } );

            Context.SwapBuffers();
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            Player.MouseScroll(e.OffsetY);
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key is Keys.Escape) Close();

            if (e.Key is Keys.Enter && KeyboardState.IsKeyDown(Keys.LeftAlt))
            {
                if (IsFullscreen is true)
                {
                    WindowState = WindowState.Normal;
                }
                else
                {
                    WindowState = WindowState.Fullscreen;
                }
            }

            if (e.Key is Keys.Z)
            {
                if (IsPolygonMode is true)
                {
                    PolygonMode(TriangleFace.FrontAndBack, OpenTK.Graphics.OpenGL.PolygonMode.Line);
                    IsPolygonMode = false;
                }
                else
                {
                    PolygonMode(TriangleFace.FrontAndBack, OpenTK.Graphics.OpenGL.PolygonMode.Fill);
                    IsPolygonMode = true;
                }
            }

            if (e.Key is Keys.O) IsWhiteWorld = !IsWhiteWorld;
            if (e.Key is Keys.Q) Interface.DebugInfo = !Interface.DebugInfo;

            if (e.Key is Keys.D1) Player.SelectedBlock = "stone";
            if (e.Key is Keys.D2) Player.SelectedBlock = "dirt";
            if (e.Key is Keys.D3) Player.SelectedBlock = "grass";
            if (e.Key is Keys.D4) Player.SelectedBlock = "sand";
            if (e.Key is Keys.D5) Player.SelectedBlock = "gravel";
            if (e.Key is Keys.D6) Player.SelectedBlock = "oak_log";
            if (e.Key is Keys.D7) Player.SelectedBlock = "oak_leaves";
            if (e.Key is Keys.D8) Player.SelectedBlock = "glass";
            if (e.Key is Keys.R)  Player.SelectedBlock = "red_light_source";
            if (e.Key is Keys.G)  Player.SelectedBlock = "green_light_source";
            if (e.Key is Keys.B)  Player.SelectedBlock = "blue_light_source";
        }
    }
}
