using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AppleCinnamon.Drawers;
using AppleCinnamon.Graphics;
using AppleCinnamon.Options;
using DaniDx.Desktop;

namespace AppleCinnamon;

public class Game
{
    public readonly ChunkManager ChunkManager;
    public readonly Camera Camera;
    public readonly SkyDome SkyDome;

    private readonly DebugLayout _debugLayout;
    private readonly Crosshair _crosshair;
    private readonly PipelineVisualizer _pipelineVisualizer;
    private readonly GraphicsContext _graphicsContext;

    private readonly Stopwatch _swComponents;
    private readonly Stopwatch _swTotal;
    public double TotalLoopTime;
    public double TotalCameraUpdateTime;
    public double TotalChunkUpdateTime;
    public double TotalDrawTime;
    public double TotalPreMiscTime;
    public double TotalPostMiscTime;

    public double DrawTimeRatio;
    public double CameraUpdateTimeRatio;
    public double ChunkUpdateTimeRatio;
    public double PreMiscTimeRatio;
    public double PostMiscTimeRatio;
    public double MissingTimeRatio;

    private readonly double[] _lastRenderTimes;
    private DateTime _lastTick;
    private int _lastRenderTimeIndex;
    private double _totalRenderTime;
    private double _avgRenderTime;
    

    public int RenderedFramesInTheLastSecond;
    public int WeirdFps;
    public int ArrayFps;

    public Game()
    {
        _graphicsContext = new GraphicsContext();
        SkyDome = new SkyDome(_graphicsContext.Device);
        _crosshair = new Crosshair(_graphicsContext);
        Camera = new Camera(_graphicsContext);
        ChunkManager = new ChunkManager(_graphicsContext);
        _pipelineVisualizer = new PipelineVisualizer(_graphicsContext);
        _debugLayout = new DebugLayout(this, _graphicsContext);

        _lastRenderTimes = new double[50];
        _swComponents = new Stopwatch();
        _swTotal = new Stopwatch();

        Task.Run(WeirdFpsCounter);
        StartLoop();
    }

    public async Task WeirdFpsCounter()
    {
        while (true)
        {
            WeirdFps = RenderedFramesInTheLastSecond;
            RenderedFramesInTheLastSecond = 0;
            await Task.Delay(TimeSpan.FromMilliseconds(1000));
        }
    }

    private void StartLoop()
    {
        RenderLoop.Run(_graphicsContext.RenderForm, () =>
        {
            _swTotal.Reset();
            _swComponents.Reset();
            Interlocked.Increment(ref RenderedFramesInTheLastSecond);



            var now = DateTime.Now;
            var elapsedTime = now - _lastTick;
            _lastTick = now;

            if (!GameOptions.IsPaused)
            {
                Cursor.Position = _graphicsContext.RenderForm.PointToScreen(new System.Drawing.Point(_graphicsContext.RenderForm.ClientSize.Width / 2, _graphicsContext.RenderForm.ClientSize.Height / 2));
                Cursor.Hide();
            }
            else
            {
                Cursor.Show();
            }

            var averageElapsedTime = TimeSpan.FromMilliseconds(Math.Min(_avgRenderTime, 20));
            TotalPreMiscTime += _swComponents.ElapsedMilliseconds;

            _swComponents.Restart();
            Camera.Update(averageElapsedTime, ChunkManager);
            TotalCameraUpdateTime += _swComponents.ElapsedMilliseconds;

            _swTotal.Restart();
            ChunkManager.Update(Camera);
            ChunkManager.CleanUp();
            SkyDome.Update(Camera);
            TotalChunkUpdateTime += _swComponents.ElapsedMilliseconds;

            _swComponents.Restart();
            _graphicsContext.Draw(() =>
            {
                if (ChunkManager.IsInitialized)
                {
                    ChunkManager.Draw(Camera);
                }

                if (GameOptions.RenderSky)
                {
                    SkyDome.Draw();
                }

                if (GameOptions.RenderCrosshair)
                {
                    _crosshair.Draw();
                }

                if (GameOptions.RenderDebugLayout)
                {
                    _debugLayout.Draw();
                }

                if (GameOptions.RenderPipelineVisualization)
                {
                    _pipelineVisualizer.Draw(Camera, ChunkManager);
                }
            });
            TotalDrawTime += _swComponents.ElapsedMilliseconds;

            _swComponents.Reset();
            _totalRenderTime += elapsedTime.TotalMilliseconds - _lastRenderTimes[_lastRenderTimeIndex];
            _avgRenderTime = _totalRenderTime / _lastRenderTimes.Length;
            _lastRenderTimes[_lastRenderTimeIndex] = elapsedTime.TotalMilliseconds;
            _lastRenderTimeIndex = _lastRenderTimeIndex == _lastRenderTimes.Length - 1 ? 0 : _lastRenderTimeIndex + 1;

            ArrayFps = (int)(1000 / _avgRenderTime);

            TotalPostMiscTime += _swComponents.ElapsedMilliseconds;
            TotalLoopTime += _swTotal.ElapsedMilliseconds;

            var missing = TotalLoopTime - (TotalCameraUpdateTime + TotalChunkUpdateTime + TotalDrawTime + TotalPreMiscTime + TotalPostMiscTime);
            CameraUpdateTimeRatio = TotalCameraUpdateTime / TotalLoopTime;
            ChunkUpdateTimeRatio = TotalChunkUpdateTime / TotalLoopTime;
            DrawTimeRatio = TotalDrawTime / TotalLoopTime;
            PreMiscTimeRatio = TotalPreMiscTime / TotalLoopTime;
            PostMiscTimeRatio = TotalPostMiscTime / TotalLoopTime;
            MissingTimeRatio = missing / TotalLoopTime;
        });
    }
}