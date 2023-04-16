using System;
using System.Linq;
using System.Windows.Forms;
using AppleCinnamon.Common;
using AppleCinnamon.Drawers;
using AppleCinnamon.Helper;
using SharpDX;
using SharpDX.Direct3D11;
using Point = System.Drawing.Point;

namespace AppleCinnamon
{
    public sealed class Game
    {
        public static readonly Vector3 StartPosition = new(0, 140, 0);

        public const int ViewDistance = 32;
        public const int NumberOfPools = 4;
        public static readonly TimeSpan ChunkDespawnCooldown = TimeSpan.FromMilliseconds(10);
        public static bool IsBackFaceCullingEnabled { get; set; }

        public static bool IsViewFrustumCullingEnabled { get; set; } = true;
        public static bool ShowChunkBoundingBoxes { get; set; } = false;
        public static bool RenderSky { get; set; } = true;
        public static bool RenderDebugLayout { get; set; } = true;
        public static bool RenderCrosshair { get; set; } = true;
        public static bool RenderWater { get; set; } = true;
        public static bool IsPaused { get; set; } = false;
        public static bool RenderSolid { get; set; } = true;
        public static bool RenderSprites { get; set; } = true;
        public static bool RenderBoxes { get; set; } = true;
        public static bool ShowPipelineVisualization { get; set; } = true;
        public static bool Debug { get; set; } = true;
        //public static bool Debug2 { get; set; } = true;

        private readonly ChunkManager _chunkManager;
        private readonly Camera _camera;
        private readonly DebugLayout _debugLayout;

        private readonly Graphics _graphics;
        private readonly Crosshair _crosshair;
        public readonly SkyDome SkyDome;
        private readonly PipelineVisualizer _pipelineVisualizer;
        private readonly double[] _lastRenderTimes;
        private DateTime _lastTick;
        private int _lastRenderTimeIndex;
        public double AverageRenderTime { get; private set; }
        public double PeekRenderTime { get; private set; }
        public double AverageFps { get; private set; }
        public World World = new();

        public static Graphics Grfx;

        public Game()
        {
            _graphics = new Graphics();
            Grfx = _graphics;
            SkyDome = new SkyDome(_graphics.Device);
            _crosshair = new Crosshair(_graphics);
            _camera = new Camera(_graphics, SkyDome);
            _chunkManager = new ChunkManager(_graphics);
            _debugLayout = new DebugLayout(_graphics);
            

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

                if (!Game.IsPaused)
                {
                    Cursor.Position = _graphics.RenderForm.PointToScreen(new Point(_graphics.RenderForm.ClientSize.Width / 2,
                        _graphics.RenderForm.ClientSize.Height / 2));
                    Cursor.Hide();
                }
                else
                {
                    Cursor.Show();
                }


                Update(new GameTime(TimeSpan.Zero, TimeSpan.FromMilliseconds(Math.Min(AverageRenderTime, 6))), _graphics.Device);
                _graphics.Draw(() =>
                {

                    if (_chunkManager.IsInitialized)
                    {
                        _chunkManager.Draw(_camera);
                    }

                    if (RenderSky)
                    {
                        SkyDome.Draw();
                    }


                    if (RenderCrosshair)
                    {
                        _crosshair.Draw(); // leaking
                    }

                    if (RenderDebugLayout)
                    {
                        _debugLayout.Draw(_chunkManager, _camera, this); // leaking
                    }


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


        private void Update(GameTime gameTime, Device device)
        {
            _camera.Update(gameTime, _chunkManager, World);
            _chunkManager.CleanUp();



            if (Game.Debug)
            {
                SkyDome.Update(_camera);
                _chunkManager.Update(_camera, World, device);
            }
        }

    }
}
