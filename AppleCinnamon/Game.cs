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
        

        public ChunkManager ChunkManager { get; set; }
        public Camera Camera { get; set; }
        public BoxDrawer BoxDrawer { get; }

        private readonly Graphics _graphics;
        private readonly Crosshair _crosshair;

        public Game()
        {
            _graphics = new Graphics();

            _crosshair = new Crosshair(_graphics);
            BoxDrawer = new BoxDrawer(_graphics);
            Camera = new Camera(BoxDrawer);
            ChunkManager = new ChunkManager(_graphics, BoxDrawer);

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

                if (Camera != null)
                {

                    var lightInfo = string.Empty;
                    if (Camera.CurrentCursor != null)
                    {
                        var voxel = ChunkManager.GetVoxel(
                            Camera.CurrentCursor.AbsoluteVoxelIndex + Camera.CurrentCursor.Direction);

                        if (voxel != null)
                        {
                            lightInfo = " / Light: " + voxel.Value.Lightness;
                        }
                    }

                    _graphics.RenderForm.Text = "Targets: " + (Camera.CurrentCursor?.AbsoluteVoxelIndex ?? new Int3()) +
                                      "LookAt: " + Camera.LookAt + " / Position" + Camera.Position +
                                      " / Rendered ChunkManager: " + ChunkManager.RenderedChunks + "/" +
                                      ChunkManager.ChunksCount + lightInfo;
                }

                sw.Stop();
                var gt = new GameTime(TimeSpan.Zero, sw.Elapsed);
                Update(gt);


                _graphics.Draw(() =>
                {
                    ChunkManager.Draw(Camera);
                    BoxDrawer.Draw();
                    _crosshair.Draw();
                });

            });
        }


        private void Update(GameTime gameTime)
        {
            BoxDrawer.Update(Camera);
            Camera.Update(gameTime, _graphics.RenderForm, ChunkManager);
            ChunkManager.Update(Camera);
        }

    }

}
