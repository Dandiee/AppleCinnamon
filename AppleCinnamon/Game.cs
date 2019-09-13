using System;
using System.Diagnostics;
using System.Windows.Forms;
using AppleCinnamon.System;
using SharpDX;
using SharpDX.Windows;
using Point = System.Drawing.Point;

namespace AppleCinnamon
{
    public class Game
    {
        public static readonly Vector3 StartPosition = new Vector3(0, 256, 0);

        private readonly ChunkManager _chunkManager;
        private readonly Camera _camera;
        private readonly BoxDrawer _boxDrawer;
        private readonly DebugLayout _debugLayout;

        private readonly Graphics _graphics;
        private readonly Crosshair _crosshair;

        public Game()
        {
            _graphics = new Graphics();

            _crosshair = new Crosshair(_graphics);
            _boxDrawer = new BoxDrawer(_graphics);
            _camera = new Camera(_graphics);
            _chunkManager = new ChunkManager(_graphics);
            _debugLayout = new DebugLayout(_graphics);

            StartLoop();
        }

        private void StartLoop()
        {
            RenderLoop.Run(_graphics.RenderForm, () =>
            {
                var sw = Stopwatch.StartNew();

                if (_graphics.RenderForm.Focused)
                {
                    Cursor.Position = _graphics.RenderForm.PointToScreen(new Point(_graphics.RenderForm.ClientSize.Width / 2,
                        _graphics.RenderForm.ClientSize.Height / 2));
                    Cursor.Hide();
                }

                sw.Stop();
                var gt = new GameTime(TimeSpan.Zero, sw.Elapsed);
                Update(gt);


                _graphics.Draw(() =>
                {
                    _chunkManager.Draw(_camera);
                    _boxDrawer.Draw();
                    _crosshair.Draw();
                    _debugLayout.Draw(_chunkManager, _camera);
                });

            });
        }


        private void Update(GameTime gameTime)
        {
            _boxDrawer.Update(_camera);
            _camera.Update(gameTime, _chunkManager, _boxDrawer);
            _chunkManager.Update(_camera);
        }
    }
}
