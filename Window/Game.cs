﻿using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using static OpenTK.Graphics.OpenGL.GL;

using VoxelWorld.Entity;
using VoxelWorld.Managers;

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
        TextureManager _textureManager;
        ChunkManager _chunkManager;
        //private Skybox _skybox;

        private Player Player { get; set; }
        private UI Interface { get; set; }
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
            var texture = StbImageSharp.ImageResult.FromStream(File.OpenRead($"resources/textures/utilities/logo.png"), StbImageSharp.ColorComponents.RedGreenBlueAlpha);
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
            _textureManager = TextureManager.Instance;
            //_skybox = new Skybox();
            Player = new Player((8f, 30.8f, 8f), MousePosition);
            _chunkManager = ChunkManager.Instance;
            Interface = new UI(Player.SelectedBlock);
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            if (Player.IsMoved is true)
            {
                var newCurrentChunk = ChunkManager.GetChunkPosition(Player.RoundedPosition.Xz);
                if (Player.CurrentChunk != newCurrentChunk)
                {
                    _chunkManager.ManageQueues(newCurrentChunk);
                    Player.CurrentChunk = newCurrentChunk;
                }
            }

            if (_chunkManager.AddQueue.Count > 0) _chunkManager.Create();
            if (_chunkManager.RemoveQueue.Count > 0) _chunkManager.Remove();

            if (IsFocused is false) return;

            FrameCount++;
            Time  += args.Time;
            Timer += args.Time;

            if (Timer > 0.2)
            {
                FPS = FrameCount * 5;
                System.Diagnostics.Process curPorcess = System.Diagnostics.Process.GetCurrentProcess();
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
            _chunkManager.Draw(Player, Time, BackgroundColor.ToRgb(), IsWhiteWorld);
            Interface.Draw(Color3.Yellow, new UI.Info { Player = Player, FPS = FPS, WindowSize = ClientSize, Time = Time } );

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
            _chunkManager.Delete();

            var windowSettings = new WindowSettings
            {
                X = this.WindowState is WindowState.Minimized ? 0 : this.Location.X,
                Y = this.WindowState is WindowState.Minimized ? 0 : this.Location.Y,
                Width = this.ClientSize.X,
                Height = this.ClientSize.Y,
                WindowState = this.WindowState is WindowState.Minimized ? WindowState.Normal : this.WindowState,
            };

            File.WriteAllText("WindowSettings.json", Newtonsoft.Json.JsonConvert.SerializeObject(windowSettings, Newtonsoft.Json.Formatting.Indented));
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
                    PolygonMode(TriangleFace.FrontAndBack, OpenTK.Graphics.OpenGL.PolygonMode.Fill);
                    IsPolygonMode = false;
                }
                else
                {
                    PolygonMode(TriangleFace.FrontAndBack, OpenTK.Graphics.OpenGL.PolygonMode.Line);
                    IsPolygonMode = true;
                }
            }

            if (e.Key is Keys.E) IsWhiteWorld = !IsWhiteWorld;
            if (e.Key is Keys.F3) Interface.DebugInfo = !Interface.DebugInfo;

            if (e.Key is Keys.D1) Player.SelectedBlock = 1;
            if (e.Key is Keys.D2) Player.SelectedBlock = 2;
            if (e.Key is Keys.D3) Player.SelectedBlock = 3;
            if (e.Key is Keys.D4) Player.SelectedBlock = 4;
            if (e.Key is Keys.D5) Player.SelectedBlock = 5;
            if (e.Key is Keys.D6) Player.SelectedBlock = 6;
            if (e.Key is Keys.D7) Player.SelectedBlock = 7;
            if (e.Key is Keys.D8) Player.SelectedBlock = 8;
            if (e.Key is Keys.R)  Player.SelectedBlock = 9;
            if (e.Key is Keys.G)  Player.SelectedBlock = 10;
            if (e.Key is Keys.B)  Player.SelectedBlock = 11;
            if (e.Key is Keys.Q)  Player.SelectedBlock = 12;
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
