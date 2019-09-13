using System;
using System.Diagnostics;
using System.Windows.Forms;
using AppleCinnamon.System;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SharpDX.DirectInput;
using SharpDX.DXGI;
using SharpDX.WIC;
using SharpDX.Windows;
using Device = SharpDX.Direct3D11.Device;
using Effect = SharpDX.Direct3D11.Effect;
using Point = System.Drawing.Point;

namespace AppleCinnamon
{
    public class Game
    {
        public Keyboard Keyboard { get; set; }
        public Mouse Mouse { get; set; }
        public static readonly Vector3 StartPosition = new Vector3(0, 256, 0);

        private Effect _solidBlockEffect;
        private Effect _basicColorEffect;

        public ChunkManager ChunkManager { get; set; }
        public Camera Camera { get; set; }
        public BoxDrawer BoxDrawer { get; }

        

        private readonly Graphics _graphics;
        private readonly Crosshair _crosshair;

        public Game()
        {
            _graphics = new Graphics();
            _crosshair = new Crosshair(_graphics);


            LoadContent();
            SetInputs();
            BoxDrawer = new BoxDrawer();
            Camera = new Camera(BoxDrawer);
            ChunkManager = new ChunkManager(_graphics, BoxDrawer);
            StartLoop();

        }

        public void UpdateSolidEffect()
        {
            if (Camera != null)
            {
                _solidBlockEffect.GetVariableByName("WorldViewProjection").AsMatrix().SetMatrix(Camera.WorldViewProjection);
                _basicColorEffect.GetVariableByName("WorldViewProjection").AsMatrix().SetMatrix(Camera.WorldViewProjection);
            }
        }

        protected void LoadContent()
        {

            var basicColorEffectByteCode = ShaderBytecode.CompileFromFile("Content/Effect/BasicEffect.fx", "fx_5_0", ShaderFlags.Debug, SharpDX.D3DCompiler.EffectFlags.AllowSlowOperations);
            _basicColorEffect = new Effect(_graphics.Device, basicColorEffectByteCode);
            var effectByteCode = ShaderBytecode.CompileFromFile("Content/Effect/SolidBlockEffect.fx", "fx_5_0");

            _solidBlockEffect = new Effect(_graphics.Device, effectByteCode);
            var blocksTexture = TextureLoader.CreateTexture2DFromBitmap(_graphics.Device, TextureLoader.LoadBitmap(new ImagingFactory2(), "Content/Texture/terrain.png"));
            _solidBlockEffect.GetVariableByName("Textures").AsShaderResource().SetResource(new ShaderResourceView(_graphics.Device, blocksTexture));

        }

        private void SetInputs()
        {
            var directInput = new DirectInput();
            Keyboard = new Keyboard(directInput);
            Keyboard.Properties.BufferSize = 128;
            Keyboard.Acquire();

            Mouse = new Mouse(directInput);
            Mouse.Properties.AxisMode = DeviceAxisMode.Relative;
            Mouse.Properties.BufferSize = 128;

            Mouse.Acquire();

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

                    ChunkManager.Draw(_solidBlockEffect, Camera);
                    BoxDrawer.Draw(_graphics.Device, _basicColorEffect);

                    _crosshair.Draw();
                });

            });
        }


        private void Update(GameTime gameTime)
        {
            UpdateSolidEffect();

            Camera.Update(gameTime, _graphics.RenderForm, ChunkManager);
            ChunkManager.Update(gameTime, Camera);
        }

    }

}
