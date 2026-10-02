using MGUI.Core.UI;
using MGUI.Shared.Rendering;
using Microsoft.Xna.Framework;
using Monadmind.Gfx.D3D12;
using Monadmind.Gfx.Zna;
using Monadmind.Gfx.Rhi;
using System;
using System.Collections.Generic;
using GfxDevice = Monadmind.Gfx.GfxDevice;

namespace MGUI.Samples
{
    /// <summary>
    /// The samples' host, and the model for a MonoGame game that renders MGUI through Monadmind.Gfx.Zna (D3D12).
    /// <para>MonoGame keeps the game loop, the window (GraphicsDeviceManager: size, fullscreen, vsync), input and
    /// content loading; MonoGame's own GraphicsDevice still exists, but only for the ContentManager, whose textures and
    /// fonts the Zna loaders copy. Rendering goes to the Zna <see cref="GraphicsDevice"/> (the global alias), which
    /// presents into MonoGame's SDL window through a D3D12 swapchain:</para>
    /// <list type="bullet">
    /// <item><c>Initialize</c> creates the GfxDevice and the Zna device on the window's HWND
    /// (<see cref="MonoGameHost.GetHwnd"/>: on DesktopGL <c>Window.Handle</c> is the SDL window, not an HWND).</item>
    /// <item><c>BeginDraw</c> opens the frame, <c>EndDraw</c> presents it and does not call <c>base.EndDraw()</c>,
    /// which would swap MonoGame's GL buffer.</item>
    /// <item><see cref="WindowResizeTracker"/> follows the client size (Window.ClientSizeChanged,
    /// GraphicsDeviceManager.DeviceReset, and a per-frame check).</item>
    /// </list>
    /// Inside this class the simple name <c>GraphicsDevice</c> in an expression is <see cref="Game.GraphicsDevice"/>,
    /// MonoGame's; the Zna device is <see cref="Device"/>.
    /// </summary>
    public class Game1 : Game, IObservableUpdate
    {
        private readonly GraphicsDeviceManager _graphics;

        private GfxDevice Gfx;
        /// <summary>The device MGUI draws with.</summary>
        private GraphicsDevice Device;
        private WindowResizeTracker Resize;

        private MainRenderer MGUIRenderer { get; set; }
        private MGDesktop Desktop { get; set; }

        /// <summary>The deterministic capture mode (<c>--capture</c>), or null.</summary>
        private readonly SampleCapture Capture;

        //  IObservableUpdate implementation
        public event EventHandler<TimeSpan> PreviewUpdate;
        public event EventHandler<EventArgs> EndUpdate;

        public Game1(SampleCapture Capture = null)
        {
            this.Capture = Capture;
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            Window.AllowUserResizing = Capture == null;
            if (Capture != null)
            {
                //  One update per frame, as fast as it goes; MGUI's clock is SampleCapture's.
                IsFixedTimeStep = false;
                _graphics.SynchronizeWithVerticalRetrace = false;
            }
        }

        protected override void Initialize()
        {
            _graphics.PreferredBackBufferWidth = Capture == null ? 1600 : SampleCapture.Width;
            _graphics.PreferredBackBufferHeight = Capture == null ? 900 : SampleCapture.Height;
            _graphics.ApplyChanges();

            CreateDevice();

            IRenderHost Host = Capture == null ? new GameRenderHost<Game1>(this, Device) : new SampleCapture.RenderHost(this, Device);
            MGUIRenderer = new(Host);
            Desktop = new(MGUIRenderer);

            //  This is a dialog with toggle buttons to launch other dialogs
            Compendium Compendium = new(Content, Desktop);
            Compendium.Show();
            Capture?.Begin(Compendium);

            base.Initialize();
        }

        /// <summary>The GfxDevice (D3D12 on the high-performance GPU; the debug layer in Debug builds) and the Zna device
        /// presenting into the window, with the back buffer MonoGame's GraphicsDeviceManager was configured for.</summary>
        private void CreateDevice()
        {
            nint Hwnd = MonoGameHost.GetHwnd(Window);
            (int Width, int Height) = MonoGameHost.GetClientSize(Hwnd);

            DeviceOptions Options = new() { Adapter = AdapterPreference.HighPerformance };
#if DEBUG
            Options.Debug = true;
#endif
            Gfx = new GfxDevice(Options, o => new D3D12Device(o));

            Microsoft.Xna.Framework.Graphics.PresentationParameters PP = GraphicsDevice.PresentationParameters;
            Device = new GraphicsDevice(Gfx, Hwnd, Width, Height, new GraphicsDeviceOptions
            {
                VSync = _graphics.SynchronizeWithVerticalRetrace,
                MultiSampleCount = PP.MultiSampleCount,
                RenderTargetUsage = PP.RenderTargetUsage,
                DepthStencilFormat = PP.DepthStencilFormat,
            });
            Resize = new WindowResizeTracker(Window, _graphics, Device, Hwnd);
        }

        protected override void Update(GameTime gameTime)
        {
            Capture?.BeforeUpdate();
            PreviewUpdate?.Invoke(this, Capture?.NextUpdateTime() ?? gameTime.TotalGameTime);

            Desktop.Update();
            Capture?.AfterUpdate();

            base.Update(gameTime);

            EndUpdate?.Invoke(this, EventArgs.Empty);
        }

        protected override bool BeginDraw()
        {
            Resize.Update();    //  resizes between frames only
            Device.BeginFrame();
            return true;
        }

        protected override void Draw(GameTime gameTime)
        {
            Device.Clear(Color.CornflowerBlue);

            Desktop.Draw();
            if (Capture != null && Capture.AfterDraw(Device))
                Exit();
            base.Draw(gameTime);
        }

        /// <summary>Presents through the Zna device. No <c>base.EndDraw()</c>: it would swap MonoGame's GL buffer.</summary>
        protected override void EndDraw()
        {
            Device.Present();
#if DEBUG
            ReportDebugMessages();
#endif
        }

        private readonly HashSet<string> ReportedDebugMessages = new();

        /// <summary>Writes each distinct message of the D3D12 debug layer to the console once.</summary>
        private void ReportDebugMessages()
        {
            if (Gfx.Rhi is D3D12Device D3D12)
            {
                foreach (string Message in D3D12.GetDebugMessages())
                {
                    if (ReportedDebugMessages.Add(Message))
                        Console.Error.WriteLine($"D3D12 {Message}");
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Resize?.Dispose();
                Device?.Dispose();  //  before the window goes: the swapchain is released while the HWND exists
                Gfx?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
