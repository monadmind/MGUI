using MGUI.Core.UI;
using MGUI.Shared.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MGUI.Samples
{
    /// <summary>
    /// Deterministic capture mode (<c>--capture &lt;dir&gt; [--frames N]</c>), the parity gate of the renderer: a fixed
    /// 1280×720 back buffer, synthetic time (1/60 s per update), no mouse or keyboard input, and a fixed list of
    /// samples shown one at a time beside the Compendium. Each sample is rendered for N frames and the back buffer of
    /// its last frame is saved as <c>&lt;dir&gt;/&lt;sample&gt;.png</c>; then the game exits. The reference images
    /// are in Reference/.
    /// </summary>
    public sealed class SampleCapture
    {
        public const int Width = 1280;
        public const int Height = 720;

        /// <summary>The update after which a sample's <see cref="Shot.Setup"/> runs: the first frame has laid it out.</summary>
        private const int SetupFrame = 1;

        /// <param name="Prepare">Runs when the sample is shown.</param>
        /// <param name="Setup">Runs once the sample's first frame has laid it out.</param>
        private sealed record Shot(string Name, Func<Compendium, SampleBase> Sample, Action<Compendium, SampleBase> Prepare = null,
            Action<Compendium, SampleBase> Setup = null);

        /// <summary>Together: text, images, nine-slice brushes, borders, scroll bars, list views, progress bars, a
        /// focused text box with its caret, and an open context menu.</summary>
        private static readonly Shot[] Shots =
        {
            new("FF7Inventory", c => c.FF7Inventory),
            new("IFillBrush", c => c.IFillBrushSamples, ExpandNineSlice, ScrollToBottom),
            new("ListView", c => c.ListViewSamples),
            new("ProgressBar", c => c.ProgressBarSamples, Setup: SetProgressBars),
            new("TextBox", c => c.TextBoxSamples, Setup: FocusTextBox),
            new("ContextMenu", c => c.ContextMenuSamples, Setup: OpenContextMenu),
        };

        public string OutputDirectory { get; }
        public int Frames { get; }

        private Compendium Compendium;
        private int ShotIndex;
        private int Frame;
        private bool Shown;
        private bool SetUp;
        private long Updates;

        private SampleCapture(string OutputDirectory, int Frames)
        {
            this.OutputDirectory = OutputDirectory;
            this.Frames = Frames;
        }

        /// <returns>The capture settings, or null when <paramref name="Args"/> has no <c>--capture</c>.</returns>
        public static SampleCapture TryParse(string[] Args)
        {
            string Directory = null;
            int Frames = 10;
            for (int i = 0; i < Args.Length; i++)
            {
                if (Args[i] == "--capture" && i + 1 < Args.Length)
                    Directory = Args[++i];
                else if (Args[i] == "--frames" && i + 1 < Args.Length)
                    Frames = int.Parse(Args[++i], CultureInfo.InvariantCulture);
            }

            if (Directory == null)
                return null;
            if (Frames <= SetupFrame)
                throw new ArgumentException($"--frames must be greater than {SetupFrame}");
            return new(Path.GetFullPath(Directory), Frames);
        }

        /// <summary>Hides every sample window but the Compendium.</summary>
        public void Begin(Compendium Compendium)
        {
            this.Compendium = Compendium;
            foreach (Shot Shot in Shots)
                Shot.Sample(Compendium).Hide();
            Compendium.FF7Inventory.Hide();
            System.IO.Directory.CreateDirectory(OutputDirectory);
        }

        /// <summary>The total game time MGUI sees in this update: 1/60 s per update, whatever the wall clock does.</summary>
        public TimeSpan NextUpdateTime() => TimeSpan.FromTicks(Updates++ * TimeSpan.TicksPerSecond / 60);

        /// <summary>Before MGUI's update: shows the current sample when its first frame begins.</summary>
        public void BeforeUpdate()
        {
            if (!Shown)
            {
                Shot Shot = Shots[ShotIndex];
                SampleBase Sample = Shot.Sample(Compendium);
                Shot.Prepare?.Invoke(Compendium, Sample);
                Sample.Show();
                Shown = true;
            }
        }

        /// <summary>After MGUI's update: applies the sample's setup once its first frame has been drawn.</summary>
        public void AfterUpdate()
        {
            if (!SetUp && Frame >= SetupFrame)
            {
                Shot Shot = Shots[ShotIndex];
                Shot.Setup?.Invoke(Compendium, Shot.Sample(Compendium));
                SetUp = true;
            }
        }

        /// <summary>After MGUI has drawn: on the sample's last frame saves the back buffer and moves on.</summary>
        /// <returns>True when every sample has been captured.</returns>
        public bool AfterDraw(GraphicsDevice GraphicsDevice)
        {
            if (++Frame < Frames)
                return false;

            Shot Shot = Shots[ShotIndex];
            string FilePath = Path.Combine(OutputDirectory, Shot.Name + ".png");
            SaveBackBuffer(GraphicsDevice, FilePath);
            Console.WriteLine($"Captured {FilePath}");

            Shot.Sample(Compendium).Hide();
            Compendium.Desktop.TryCloseActiveContextMenu();
            ShotIndex++;
            Frame = 0;
            Shown = false;
            SetUp = false;
            return ShotIndex == Shots.Length;
        }

        private static void SaveBackBuffer(GraphicsDevice GraphicsDevice, string FilePath)
        {
            PresentationParameters PP = GraphicsDevice.PresentationParameters;
            int W = PP.BackBufferWidth;
            int H = PP.BackBufferHeight;
            if (W != Width || H != Height)
                Console.Error.WriteLine($"Back buffer is {W}x{H}, not {Width}x{Height}");

            Color[] Data = new Color[W * H];
            GraphicsDevice.GetBackBufferData(Data);
            using Texture2D Texture = new(GraphicsDevice, W, H);
            Texture.SetData(Data);
            using FileStream Stream = File.Create(FilePath);
            Texture.SaveAsPng(Stream, W, H);
        }

        /// <summary>Expands the last expander, MGNineSliceFillBrush's.</summary>
        private static void ExpandNineSlice(Compendium Compendium, SampleBase Sample) =>
            Sample.Window.TraverseVisualTree<MGExpander>().Last().IsExpanded = true;

        private static void ScrollToBottom(Compendium Compendium, SampleBase Sample)
        {
            MGScrollViewer ScrollViewer = Sample.Window.TraverseVisualTree<MGScrollViewer>().First();
            ScrollViewer.VerticalOffset = ScrollViewer.MaxVerticalOffset;
        }

        /// <summary>Stops the progress bars (each advances by its slider's value per update) and fills them 30 % to 86 %.</summary>
        private static void SetProgressBars(Compendium Compendium, SampleBase Sample)
        {
            for (int i = 1; i <= 8; i++)
            {
                if (Sample.Window.TryGetElementByName($"Slider{i}", out MGSlider Slider))
                    Slider.Value = 0;
                if (Sample.Window.TryGetElementByName($"ProgressBar{i}", out MGProgressBar Bar))
                    Bar.Value = Bar.Minimum + (Bar.Maximum - Bar.Minimum) * (0.22f + 0.08f * i);
            }
        }

        /// <summary>Focuses TextBox2 with its caret after "Hello", blinking off for a day, so the caret is drawn.</summary>
        private static void FocusTextBox(Compendium Compendium, SampleBase Sample)
        {
            MGTextBox TextBox = Sample.Window.GetElementByName<MGTextBox>("TextBox2");
            TextBox.Caret.BlinkRate = TimeSpan.FromDays(1);
            TextBox.Caret.MoveToOriginalCharacterIndexOrEnd(5, true);
            TextBox.RequestFocus();
        }

        /// <summary>Opens ContextMenu1 beside the button that hosts it.</summary>
        private static void OpenContextMenu(Compendium Compendium, SampleBase Sample)
        {
            MGContextMenu Menu = Sample.Window.GetElementByName<MGContextMenu>("ContextMenu1");
            Compendium.Desktop.TryOpenContextMenu(Menu, new Point(760, 300));
        }

        /// <summary>MGUI's host in capture mode: the fixed viewport, the mouse far outside the window, no keys.</summary>
        public sealed class RenderHost : IRenderHost
        {
            private readonly Game1 Game;

            public RenderHost(Game1 Game, GraphicsDevice GraphicsDevice)
            {
                this.Game = Game;
                this.GraphicsDevice = GraphicsDevice;
                Game.PreviewUpdate += (sender, e) => PreviewUpdate?.Invoke(Game, e);
                Game.EndUpdate += (sender, e) => EndUpdate?.Invoke(Game, e);
            }

            public Rectangle GetBounds() => new(0, 0, Width, Height);
            public GraphicsDevice GraphicsDevice { get; }
            public MouseState GetMouseState() => new(-10000, -10000, 0, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
            public KeyboardState GetKeyboardState() => default;
            public object GetService(Type serviceType) => Game.Services.GetService(serviceType);

            public event EventHandler<TimeSpan> PreviewUpdate;
            public event EventHandler<EventArgs> EndUpdate;
        }
    }
}
