using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
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
        public static readonly Vector3 StartPosition = new Vector3(0, 256, 0);
        public Factory D2dFactory;
        public Map Map;
        public Geometry Crosshair { get; private set; }

       

        public Game()
        {
            
            
            
            SetDevice();
            SetInputs();
            Map = new Map(this);
            StartLoop();
            
        }

        private void SetDevice()
        {
            RenderForm = new RenderForm("Apple & Cinnamon")
            {
                Width = 2300,
                Height = 1300
            };

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

        private void ImprovedLoop()
        {

        }

        private void StartLoop()
        {
            RenderLoop.Run(RenderForm, () =>
            {
                if (RenderForm.Focused)
                {
                    Cursor.Position = RenderForm.PointToScreen(new Point(RenderForm.ClientSize.Width / 2,
                        RenderForm.ClientSize.Height / 2));
                    Cursor.Hide();
                }

                if (Map.Camera != null)
                {

                    var lightInfo = string.Empty;
                    if (Map.Camera.CurrentCursor != null)
                    {
                        var voxel = Map.ChunkManager.GetVoxel(
                            Map.Camera.CurrentCursor.AbsoluteVoxelIndex + Map.Camera.CurrentCursor.Direction);

                        if (voxel != null)
                        {
                            lightInfo = " / Light: " + voxel.Value.Lightness;
                        }
                    }

                    RenderForm.Text = "Targets: " + (Map.Camera.CurrentCursor?.AbsoluteVoxelIndex ?? new Int3()) +
                                      "LookAt: " + Map.Camera.LookAt + " / Position" + Map.Camera.Position +
                                      " / Rendered ChunkManager: " + Map.ChunkManager.RenderedChunks + "/" +
                                      Map.ChunkManager.ChunksCount + lightInfo;
                }

                Tick();

            });
        }



        const int BadUpdateCountTime = 2;

        private bool _forceElapsedTimeToZero;
        private TimeSpan _totalGameTime;
        private TimeSpan _inactiveSleepTime;
        private readonly TimeSpan _maximumElapsedTime = TimeSpan.FromMilliseconds(500.0);
        private TimeSpan _accumulatedElapsedGameTime;
        private TimeSpan _lastFrameElapsedGameTime;
        private readonly TimerTick _timer = new TimerTick();
        private GameTime _gameTime = new GameTime();
        private bool _isFixedTimeStep;
        private readonly TimeSpan _targetElapsedTime = TimeSpan.FromTicks(10000000 / 60);
        private readonly int[] _lastUpdateCount = new int[4];
        private int _nextLastUpdateCountIndex;
        private bool _drawRunningSlowly;
        
        private float _updateCountAverageSlowLimit = (float)((4) + (4 - 4)) / 4;

    
        public void Tick()
        {
            if (!RenderForm.Focus())
            {
                Thread.Sleep(_inactiveSleepTime);
            }

            // Update the timer
            _timer.Tick();

            var elapsedAdjustedTime = _timer.ElapsedAdjustedTime;

            if (_forceElapsedTimeToZero)
            {
                elapsedAdjustedTime = TimeSpan.Zero;
                _forceElapsedTimeToZero = false;
            }

            if (elapsedAdjustedTime > _maximumElapsedTime)
            {
                elapsedAdjustedTime = _maximumElapsedTime;
            }

            bool suppressNextDraw = true;
            int updateCount = 1;
            var singleFrameElapsedTime = elapsedAdjustedTime;

            if (_isFixedTimeStep)
            {
                // If the rounded TargetElapsedTime is equivalent to current ElapsedAdjustedTime
                // then make ElapsedAdjustedTime = TargetElapsedTime. We take the same internal rules as XNA 
                if (Math.Abs(elapsedAdjustedTime.Ticks - _targetElapsedTime.Ticks) < (_targetElapsedTime.Ticks >> 6))
                {
                    elapsedAdjustedTime = _targetElapsedTime;
                }

                // Update the accumulated time
                _accumulatedElapsedGameTime += elapsedAdjustedTime;

                // Calculate the number of update to issue
                updateCount = (int)(_accumulatedElapsedGameTime.Ticks / _targetElapsedTime.Ticks);

                // If there is no need for update, then exit
                if (updateCount == 0)
                {
                    // check if we can sleep the thread to free CPU resources
                    var sleepTime = _targetElapsedTime - _accumulatedElapsedGameTime;
                    if (sleepTime > TimeSpan.Zero)
                    {
                        Thread.Sleep(sleepTime);
                    }

                    return;
                }

                // Calculate a moving average on updateCount
                _lastUpdateCount[_nextLastUpdateCountIndex] = updateCount;
                float updateCountMean = 0;
                for (int i = 0; i < _lastUpdateCount.Length; i++)
                {
                    updateCountMean += _lastUpdateCount[i];
                }

                updateCountMean /= _lastUpdateCount.Length;
                _nextLastUpdateCountIndex = (_nextLastUpdateCountIndex + 1) % _lastUpdateCount.Length;

                // Test when we are running slowly
                _drawRunningSlowly = updateCountMean > _updateCountAverageSlowLimit;

                // We are going to call Update updateCount times, so we can subtract this from accumulated elapsed game time
                _accumulatedElapsedGameTime = new TimeSpan(_accumulatedElapsedGameTime.Ticks - (updateCount * _targetElapsedTime.Ticks));
                singleFrameElapsedTime = _targetElapsedTime;
            }
            else
            {
                Array.Clear(_lastUpdateCount, 0, _lastUpdateCount.Length);
                _nextLastUpdateCountIndex = 0;
                _drawRunningSlowly = false;
            }

            // Reset the time of the next frame
            for (_lastFrameElapsedGameTime = TimeSpan.Zero; updateCount > 0; updateCount--)
            {
                _gameTime.Update(_totalGameTime, singleFrameElapsedTime, _drawRunningSlowly);
                Update(_gameTime);
                _lastFrameElapsedGameTime += singleFrameElapsedTime;
                _totalGameTime += singleFrameElapsedTime;
            }

            Draw();
        }


        private void Draw()
        {

                _gameTime.Update(_totalGameTime, _lastFrameElapsedGameTime, _drawRunningSlowly);
                _gameTime.FrameCount++;

            Device.ImmediateContext.OutputMerger.SetTargets(DepthStencilView, RenderTargetView);
            Device.ImmediateContext.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);
            Device.ImmediateContext.ClearRenderTargetView(RenderTargetView, Color.CornflowerBlue);

            RenderTarget2D.BeginDraw();
            Map.Draw();
            RenderTarget2D.FillGeometry(Crosshair, new SolidColorBrush(RenderTarget2D, Color.White), null);
            RenderTarget2D.EndDraw();

            SwapChain.Present(0, PresentFlags.None);

            _lastFrameElapsedGameTime = TimeSpan.Zero;
        }

        private void Update(GameTime gameTime)
        {
            Map.Update(gameTime);
        }

    }

}
