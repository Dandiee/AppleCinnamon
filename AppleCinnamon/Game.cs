using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AppleCinnamon.Drawers;
using AppleCinnamon.Graphics;
using AppleCinnamon.Options;
using DaniDx.Desktop;

namespace AppleCinnamon
{
    public class Game
    {
        public readonly ChunkManager ChunkManager;
        public readonly Camera Camera;
        private readonly DebugLayout _debugLayout;

        private readonly GraphicsContext _graphicsContext;
        private readonly Crosshair _crosshair;
        public readonly SkyDome SkyDome;
        private readonly PipelineVisualizer _pipelineVisualizer;

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


                Update(TimeSpan.FromMilliseconds(_avgRenderTime));
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

                _totalRenderTime += elapsedTime.TotalMilliseconds - _lastRenderTimes[_lastRenderTimeIndex];
                _avgRenderTime = _totalRenderTime / _lastRenderTimes.Length;
                _lastRenderTimes[_lastRenderTimeIndex] = elapsedTime.TotalMilliseconds;
                _lastRenderTimeIndex = _lastRenderTimeIndex == _lastRenderTimes.Length - 1 ? 0 : _lastRenderTimeIndex + 1;

                ArrayFps = (int)(1000 / _avgRenderTime);
            });
        }


        private void Update(TimeSpan elapsedTime)
        {
            Camera.Update(elapsedTime, ChunkManager);
            ChunkManager.Update(Camera);
            ChunkManager.CleanUp();
            SkyDome.Update(Camera);
            
        }

    }
}
