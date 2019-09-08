using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using AppleCinnamon.System;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DirectInput;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using SharpDX.Windows;
using AlphaMode = SharpDX.Direct2D1.AlphaMode;
using Device = SharpDX.Direct3D11.Device;
using Factory = SharpDX.Direct2D1.Factory;
using FillMode = SharpDX.Direct2D1.FillMode;
using Point = System.Drawing.Point;

namespace AppleCinnamon
{
    public class Game
    {
        public RenderForm RenderForm { get; private set; }
        public RenderTargetView RenderTargetView { get; private set; }
        public DepthStencilView DepthStencilView { get; private set; }
        public Device Device { get; private set; }
        public Keyboard Keyboard { get; set; }
        public Mouse Mouse { get; set; }
        public SwapChain SwapChain { get; private set; }
        public RenderTarget RenderTarget2D { get; private set; }
        public RoundedRectangleGeometry RoundedRectangleGeometry { get; private set; }
        public static readonly Vector3 StartPosition = new Vector3(0, 120, 0);
        public Factory D2dFactory;
        public Map Map;
        public Geometry Crosshair { get; private set; }
        public Game()
        {
            SetDevice();
            Program.WriteLine("Device loaded...");
            SetInputs();
            Program.WriteLine("Keyboard loaded...");
            SetComponents();
            Program.WriteLine("Components loaded...");
            Program.WriteLine("Loop starting...");
            StartLoop();

            Cursor.Current = null;

        }

        private void SetDevice()
        {
            RenderForm = new RenderForm("SharpDX - Basics");
            RenderForm.Width = 1600;
            RenderForm.Height = 900;

            // SwapChain description
            var desc = new SwapChainDescription
            {
                BufferCount = 1,
                ModeDescription =
                    new ModeDescription(RenderForm.ClientSize.Width, RenderForm.ClientSize.Height,
                        new Rational(60, 1), Format.R8G8B8A8_UNorm),
                IsWindowed = true,
                OutputHandle = RenderForm.Handle,
                SampleDescription = new SampleDescription(1, 0),
                SwapEffect = SwapEffect.Discard,
                Usage = Usage.RenderTargetOutput
            };

            // Create Device and SwapChain
            {
                Device device;
                SwapChain swapChain;

                // [] { SharpDX.Direct3D.FeatureLevel.Level_10_0 }
                Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.BgraSupport, desc, out device, out swapChain);

                Device = device;
                SwapChain = swapChain;
                D2dFactory = new Factory();
            }

            
            // Ignore all windows events
            var factory = SwapChain.GetParent<SharpDX.DXGI.Factory>();
            factory.MakeWindowAssociation(RenderForm.Handle, WindowAssociationFlags.IgnoreAll);

            // New RenderTargetView from the backbuffer
            var backBuffer = Texture2D.FromSwapChain<Texture2D>(SwapChain, 0);
            RenderTargetView = new RenderTargetView(Device, backBuffer);

            Surface surface = backBuffer.QueryInterface<Surface>();


            RenderTarget2D = new RenderTarget(D2dFactory, surface,
                new RenderTargetProperties(new PixelFormat(Format.Unknown, AlphaMode.Premultiplied)));

          
            
            RoundedRectangleGeometry = new RoundedRectangleGeometry(D2dFactory,
                new RoundedRectangle
                {
                    RadiusX = 32,
                    RadiusY = 32,
                    Rect = new RectangleF(128, 128, RenderForm.ClientSize.Width - 128 * 2,
                        RenderForm.ClientSize.Height - 128 * 2)
                });



            var midX = RenderForm.ClientSize.Width / 2f;
            var midY = RenderForm.ClientSize.Height / 2f;
            var thickness = 3f;
            Crosshair = new GeometryGroup(D2dFactory, FillMode.Alternate,
                new[]
                {
                    new RectangleGeometry(D2dFactory, new RawRectangleF(midX - 20, midY - thickness/2, midX + 20, midY + thickness/2)),
                    new RectangleGeometry(D2dFactory, new RawRectangleF(midX - thickness/2, midY - 20, midX + thickness/2, midY + 20))
                });
            

            // Create Constant Buffer
            //var ConstantBuffer = new Buffer(Device, Utilities.SizeOf<Matrix>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None);

            // Create Depth Buffer & View
            var depthBuffer = new Texture2D(Device, new Texture2DDescription
            {
                Format = Format.D32_Float_S8X24_UInt,
                ArraySize = 1,
                MipLevels = 1,
                Width = RenderForm.ClientSize.Width,
                Height = RenderForm.ClientSize.Height,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            });

            DepthStencilView = new DepthStencilView(Device, depthBuffer);
            
            //Device.VertexShader.SetConstantBuffer(0, ConstantBuffer);
            Device.ImmediateContext.Rasterizer.SetViewport(new Viewport(0, 0, RenderForm.ClientSize.Width, RenderForm.ClientSize.Height, 0.0f, 1.0f));

            
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

        private void SetComponents()
        {
            Map = new Map(this);
        }

        private void StartLoop()
        {
            
            var gameTime = new GameTime();
            var sw = Stopwatch.StartNew();

            RenderLoop.Run(RenderForm, () =>
            {
                Update(gameTime);
                Draw(gameTime);
                if (RenderForm.Focused)
                {
                    Cursor.Position = RenderForm.PointToScreen(new Point(RenderForm.ClientSize.Width / 2,
                        RenderForm.ClientSize.Height / 2));
                    Cursor.Hide();
                }


                var newTS = sw.Elapsed;
                var diff = newTS - gameTime.TotalGameTime;
                gameTime.TotalGameTime = sw.Elapsed;
                gameTime.ElapsedGameTime = diff;
                RenderForm.Text = "Targets: " + (Map.Camera.CurrentCursor?.AbsoluteVoxelIndex?? new Int3()) + "LookAt: " + Map.Camera.LookAt + " / Position" + Map.Camera.Position +
                                  " / Rendered ChunkManager: " + Map.ChunkManager.RenderedChunks+"/"+Map.ChunkManager.ChunksCount;

            });
        }

        private void Draw(GameTime gameTime)
        {
            Device.ImmediateContext.OutputMerger.SetTargets(DepthStencilView, RenderTargetView);
            Device.ImmediateContext.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);
            Device.ImmediateContext.ClearRenderTargetView(RenderTargetView, Color.Black);

            RenderTarget2D.BeginDraw();
            Map.Draw();
            RenderTarget2D.FillGeometry(Crosshair, new SolidColorBrush(RenderTarget2D, Color.White), null);
            RenderTarget2D.EndDraw();


           
            

            
            //if (this.Camera.CurrentCursor != null)
            //{
            //    this.selection.Draw(gameTime);
            //}

            SwapChain.Present(0, PresentFlags.None);
        }

        private void Update(GameTime gameTime)
        {
            
            // this.Camera.Update(gameTime);
            Map.Update(gameTime);

            // if (this.Camera.CurrentCursor != null)
            // {
            //     this.selection.AbsoluteBlockIndex = this.Camera.CurrentCursor.AbsoluteBlockIndex;
            //     this.selection.SelectedBlockSize = new Vector3(this.Camera.CurrentCursor.VoxelDefinition.WidthLength, this.Camera.CurrentCursor.VoxelDefinition.Height, this.Camera.CurrentCursor.VoxelDefinition.WidthLength);
            //     this.selection.Update(gameTime);
            // }
        }

        public static IEnumerable<T> Go<T>()
        {
            return Enum.GetValues(typeof(T)).Cast<T>().ToList();
        }
    }

}
