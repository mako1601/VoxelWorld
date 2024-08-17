using System.Diagnostics;

using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using static OpenTK.Graphics.OpenGL.GL;

using StbImageSharp;

using Newtonsoft.Json;

using VoxelWorld.Entity;
using VoxelWorld.World;
using VoxelWorld.Graphics;

namespace VoxelWorld.Window
{
    public struct WindowSettings
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width  { get; set; }
        public int Height { get; set; }
        public WindowState WindowState { get; set; }
        public static WindowSettings Default => new()
        {
            X = 100,
            Y = 100,
            Width = 1280,
            Height = 768,
            WindowState = WindowState.Normal
        };
    }

    public class Game : GameWindow
    {
        private Chunks _chunks;
        //private Skybox _skybox;
        private TextureManager _textureManager;

        private Player Player { get; set; }
        private Interface Interface { get; set; }
        private Color4<Rgba> BackgroundColor { get; set; } = new(0.37f, 0.78f, 1f, 1f);

        private double Time { get; set; } = 0;
        private double Timer { get; set; } = 0;
        private uint FrameCount { get; set; } = 0;
        private uint FPS { get; set; } = 0;
        private bool IsWhiteWorld  { get; set; } = false;
        private bool IsPolygonMode { get; set; } = false;
        private WindowState PreviousWindowState { get; set; }

        public Game(WindowSettings settings, string title = "VoxelWorld") :
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
                    WindowState = settings.WindowState,
                    // WindowBorder
                    Location = (settings.X, settings.Y),
                    ClientSize = (settings.Width, settings.Height),
                    MinimumClientSize = (480, 360),
                    // MaximumClientSize
                    // AspectRatio
                    //NumberOfSamples = 16, // MSAA x2, x4, x8, x16
                    // TransparentFramebuffer
                    //Vsync = VSyncMode.On,
                    // AutoIconify
                }
            )
        {
            var texture = ImageResult.FromStream(File.OpenRead($"resources/textures/utilities/logo.png"), ColorComponents.RedGreenBlueAlpha);
            Icon = new OpenTK.Windowing.Common.Input.WindowIcon(new OpenTK.Windowing.Common.Input.Image(texture.Width, texture.Height, texture.Data));

            MousePosition = (ClientSize.X / 2f, ClientSize.Y / 2f);
            CursorState = CursorState.Grabbed;
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            // OpenGL settings
            Enable(EnableCap.DepthTest);
            DepthFunc(DepthFunction.Less);

            // init
            _textureManager = new TextureManager();
            Player = new Player((8, 32, 8), MousePosition);
            //_skybox = new Skybox();
            _chunks = new Chunks();
            Interface = new Interface(Player.SelectedBlock);
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            if (IsFocused is false) return;

            FrameCount++;
            Time  += args.Time;
            Timer += args.Time;

            if (Timer > 0.2)
            {
                FPS = FrameCount * 5;
                Process curPorcess = Process.GetCurrentProcess();
                Title = $"VoxelWorld FPS: {FPS}, {args.Time * 1000d:0.0000}ms, "
                    + $"RAM: {curPorcess.WorkingSet64 / (1024f * 1024f):0.000}Mb";
                curPorcess.Dispose();
                FrameCount = 0;
                Timer -= 0.2;
            }

            Player.KeyboardInput(KeyboardState, (float)args.Time);
            Player.MouseInput(MouseState, args.Time);
            Player.Camera.Move(CursorState, MousePosition);
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            if (IsFocused is false) return;

            ClearColor(BackgroundColor);
            Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            //_skybox.Draw(Player);
            _chunks.Draw(Player, Time, BackgroundColor.ToRgb(), IsWhiteWorld);
            Interface.Draw(Color3.Yellow, new Interface.Info { Player = Player, FPS = FPS, WindowSize = ClientSize, Time = Time } );

            Context.SwapBuffers();
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);

            Viewport(0, 0, ClientSize.X, ClientSize.Y);
            Player.Camera.AspectRatio = (float)ClientSize.X / ClientSize.Y;
        }

        protected override void OnUnload()
        {
            base.OnUnload();

            Interface.Delete();
            //_skybox.Delete();
            _chunks.Delete();

            var windowSettings = new WindowSettings
            {
                X = this.WindowState is WindowState.Minimized ? 0 : this.Location.X,
                Y = this.WindowState is WindowState.Minimized ? 0 : this.Location.Y,
                Width = this.ClientSize.X,
                Height = this.ClientSize.Y,
                WindowState = this.WindowState is WindowState.Minimized ? WindowState.Normal : this.WindowState,
            };

            string json = JsonConvert.SerializeObject(windowSettings, Formatting.Indented);
            File.WriteAllText("WindowSettings.json", json);
        }

        #region Input

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key is Keys.Escape) Close();

            if (e.Key is Keys.F11 || e.Key is Keys.Enter && KeyboardState.IsKeyDown(Keys.LeftAlt))
            {
                // FIXME: MousePosition changes when changing WindowState to
                // Fullscreen and vice versa with CursorState.Grabbed, which should not be the case.
                // It is not critical, but it is very conspicuous and annoying.
                // If you know how to fix it, I will be very glad.
                if (WindowState is WindowState.Fullscreen)
                {
                    WindowState = PreviousWindowState;
                    if (Location.X < 0 && Location.Y < 0)
                    {
                        Location = (0, 0);
                        WindowState = WindowState.Maximized;
                    }
                }
                else
                {
                    PreviousWindowState = WindowState;
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

            if (e.Key is Keys.E) IsWhiteWorld = !IsWhiteWorld;
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

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button is MouseButton.Middle)
            {
                if (CursorState is CursorState.Grabbed)
                {
                    CursorState = CursorState.Normal;
                    Player.Camera.FirstMove = true;
                }
                else
                {
                    CursorState = CursorState.Grabbed;
                }
            }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            Player.Camera.FOV -= e.OffsetY * 10;
            Player.Camera.FOV = Math.Clamp(Player.Camera.FOV, 20f, 140f);
        }

        #endregion Input
    }
}
