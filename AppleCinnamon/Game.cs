using System;
using System.Linq;
using System.Windows.Forms;
using AppleCinnamon.Helper;
using AppleCinnamon.Vertices;
using SharpDX;
using Point = System.Drawing.Point;

namespace AppleCinnamon
{
    public sealed class Game
    {
        public static readonly Vector3 StartPosition = new(0, 140, 0);

        public const int ViewDistance = 12;
        public const int NumberOfPools = 4;
        public static readonly TimeSpan ChunkDespawnCooldown = TimeSpan.FromSeconds(1);
        public static bool IsBackFaceCullingEnabled { get; set; }

        public static bool IsViewFrustumCullingEnabled { get; set; } = true;
        public static bool ShowChunkBoundingBoxes { get; set; } = false;
        public static bool RenderSky { get; set; } = true;
        public static bool RenderWater { get; set; } = true;
        public static bool RenderSolid { get; set; } = true;
        public static bool RenderSprites { get; set; } = true;
        public static bool RenderBoxes { get; set; } = true;
        public static bool ShowPipelineVisualization { get; set; } = true;
        public static bool Debug { get; set; } = true;

        private readonly ChunkManager _chunkManager;
        private readonly Camera _camera;
        private readonly DebugLayout _debugLayout;

        private readonly Graphics _graphics;
        private readonly Crosshair _crosshair;
        private readonly SkyDome _skyDome;
        private readonly PipelineVisualizer _pipelineVisualizer;
        private readonly double[] _lastRenderTimes;
        private DateTime _lastTick;
        private int _lastRenderTimeIndex;
        public double AverageRenderTime { get; private set; }
        public double PeekRenderTime { get; private set; }
        public double AverageFps { get; private set; }
        public World World = new();

        public Game()
        {
            _graphics = new Graphics();

            _crosshair = new Crosshair(_graphics);
            _camera = new Camera(_graphics);
            _chunkManager = new ChunkManager(_graphics);
            _debugLayout = new DebugLayout(_graphics);
            _skyDome = new SkyDome(_graphics.Device);

            _lastRenderTimes = new double[20];
            _pipelineVisualizer = new PipelineVisualizer(_graphics);

            StartLoop();
        }

        private void StartLoop()
        {
            SharpDX.Windows.RenderLoop.Run(_graphics.RenderForm, () =>
            {
                var now = DateTime.Now;
                var elapsedTime = now - _lastTick;
                _lastTick = now;

                if (!_camera.IsPaused)
                {
                    Cursor.Position = _graphics.RenderForm.PointToScreen(new Point(_graphics.RenderForm.ClientSize.Width / 2,
                        _graphics.RenderForm.ClientSize.Height / 2));
                    Cursor.Hide();
                }
                else
                {
                    Cursor.Show();
                }


                Update(new GameTime(TimeSpan.Zero, TimeSpan.FromMilliseconds(Math.Min(AverageRenderTime, 6))));
                _graphics.Draw(() =>
                {

                    if (_chunkManager.IsInitialized)
                    {
                        _chunkManager.Draw(_camera);
                    }

                    _skyDome.Draw();

                    _crosshair.Draw(); // leaking
                    _debugLayout.Draw(_chunkManager, _camera, this); // leaking


                    if (ShowPipelineVisualization)
                    {
                        _pipelineVisualizer.Draw(_camera, _chunkManager);
                    }




                });

                _lastRenderTimes[_lastRenderTimeIndex] = elapsedTime.TotalMilliseconds;
                _lastRenderTimeIndex = _lastRenderTimeIndex == _lastRenderTimes.Length - 1 ? 0 : _lastRenderTimeIndex + 1;
                AverageRenderTime = _lastRenderTimes.Average();
                PeekRenderTime = _lastRenderTimes.Max();
                AverageFps = 1000f / AverageRenderTime;
            });
        }


        private void Update(GameTime gameTime)
        {
            _camera.Update(gameTime, _chunkManager, World);
            if (Game.Debug)
            {
                _skyDome.Update(_camera, World);
                _chunkManager.Update(_camera, World);
            }
        }

    }
}
