using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AppleCinnamon.Drawers;
using SharpDX;
using SharpDX.Direct3D11;
using Point = System.Drawing.Point;

namespace AppleCinnamon
{
    public class Game
    {
        public static readonly Vector3 StartPosition = new(0, 140, 0);

        public const int ViewDistance = 64;
        public const int NumberOfPools = 4;
        public static readonly TimeSpan ChunkDespawnCooldown = TimeSpan.FromMilliseconds(10);

        public readonly ChunkManager ChunkManager;
        public readonly Camera Camera;
        private readonly DebugLayout _debugLayout;

        private readonly Graphics _graphics;
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
            _graphics = new Graphics();
            SkyDome = new SkyDome(_graphics.Device);
            _crosshair = new Crosshair(_graphics);
            Camera = new Camera(_graphics);
            ChunkManager = new ChunkManager(_graphics);
            _pipelineVisualizer = new PipelineVisualizer(_graphics);
            _debugLayout = new DebugLayout(this, _graphics);

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
            SharpDX.Windows.RenderLoop.Run(_graphics.RenderForm, () =>
            {
                Interlocked.Increment(ref RenderedFramesInTheLastSecond);

                var now = DateTime.Now;
                var elapsedTime = now - _lastTick;
                _lastTick = now;

                if (!GameOptions.IsPaused)
                {
                    Cursor.Position = _graphics.RenderForm.PointToScreen(new Point(_graphics.RenderForm.ClientSize.Width / 2, _graphics.RenderForm.ClientSize.Height / 2));
                    Cursor.Hide();
                }
                else
                {
                    Cursor.Show();
                }


                Update(TimeSpan.FromMilliseconds(_avgRenderTime), _graphics.Device);
                _graphics.Draw(() =>
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


        private void Update(TimeSpan elapsedTime, Device device)
        {
            Camera.Update(elapsedTime, ChunkManager);
            ChunkManager.Update(Camera, device);
            ChunkManager.CleanUp();
            SkyDome.Update(Camera);
            
        }

    }

    public static class GameOptions
    {
        public static bool RenderSolid { get; set; } = true;
        public static bool RenderSprites { get; set; } = true;
        public static bool RenderWater { get; set; } = true;
        public static bool RenderSky { get; set; } = true;
        public static bool RenderCrosshair { get; set; } = true;
        public static bool RenderBoxes { get; set; } = true;
        public static bool RenderPipelineVisualization { get; set; } = false;
        public static bool IsViewFrustumCullingEnabled { get; set; } = true;
        public static bool RenderChunkBoundingBoxes { get; set; } = false;
        public static bool RenderDebugLayout { get; set; } = true;
        public static bool IsPaused { get; set; } = false;

    }
}
