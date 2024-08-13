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

namespace VoxelWorld.Window
{
    public struct WindowSettings
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width  { get; set; }
        public int Height { get; set; }
        public WindowState WindowState { get; set; }
    }

    public class Game : GameWindow
    {
        private Chunks _chunks;
        //private Skybox _skybox;

        private Player Player { get; }
        private Interface Interface { get; set; }
        private Color4<Rgba> BackgroundColor { get; set; } = new(0.37f, 0.78f, 1f, 1f);

        private double Time { get; set; } = 0;
        private double Timer { get; set; } = 0;
        private uint FrameCount { get; set; } = 0;
        private uint FPS { get; set; } = 0;
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
                    StartVisible = false,
                    // WindowSotate
                    WindowBorder = WindowBorder.Resizable,
                    // Location
                    // Size
                    MinimumClientSize = (480, 360),
                    // MaximumClientSize
                    // AspectRatio
                    // IsFullscreen
                    NumberOfSamples = 16, // MSAA x2, x4, x8, x16
                    // TransparentFramebuffer
                    //Vsync = VSyncMode.On,
                    // AutoIconify
                }
            )
        {
            var texture = ImageResult.FromStream(File.OpenRead($"resources/textures/utilities/logo.png"), ColorComponents.RedGreenBlueAlpha);
            Icon = new OpenTK.Windowing.Common.Input.WindowIcon(new OpenTK.Windowing.Common.Input.Image(texture.Width, texture.Height, texture.Data));

            Player = new Player((8, 32, 8));
            Player.Camera.AspectRatio = width / (float)height;

            if (File.Exists("window_settings.json"))
            {
                string json = File.ReadAllText("window_settings.json");
                var settings = JsonConvert.DeserializeObject<WindowSettings>(json);

                this.Location = (settings.X, settings.Y);
                this.Size = (settings.Width, settings.Height);
                this.WindowState = settings.WindowState;
            }
            else
            {
                CenterWindow((width, height));
            }
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            // OpenGL settings
            Enable(EnableCap.DepthTest);
            DepthFunc(DepthFunction.Less);

            // init
            //_skybox = new Skybox();
            _chunks = new Chunks();
            Interface = new Interface(Player.SelectedBlock);
        }

        protected override void OnUnload()
        {
            base.OnUnload();

            Interface.Delete();
            //_skybox.Delete();
            _chunks.Delete();

            var windowSettings = new WindowSettings
            {
                X = this.Location.X,
                Y = this.Location.Y,
                Width = this.ClientSize.X,
                Height = this.ClientSize.Y,
                WindowState = this.WindowState,
            };

            string json = JsonConvert.SerializeObject(windowSettings, Formatting.Indented);
            File.WriteAllText("window_settings.json", json);
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
            Time += args.Time;
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
            CursorState = Player.MouseInput(MouseState, CursorState, args.Time);
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
    }
}
