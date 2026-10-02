using System;

public class Program
{
    /// <param name="args"><c>--capture &lt;dir&gt; [--frames N]</c> runs the deterministic capture mode (see
    /// <see cref="MGUI.Samples.SampleCapture"/>) and exits; no arguments run the interactive samples.</param>
    [STAThread]
    static void Main(string[] args)
    {
        using var game = new MGUI.Samples.Game1(MGUI.Samples.SampleCapture.TryParse(args));
        game.Run();
    }
}
