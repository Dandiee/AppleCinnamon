using System;
using System.Windows.Forms;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Windows;
using static SharpDX.Direct3D11.Resource;
using AlphaMode = SharpDX.Direct2D1.AlphaMode;
using Device = SharpDX.Direct3D11.Device;
using Factory = SharpDX.Direct2D1.Factory;
using PixelFormat = SharpDX.Direct2D1.PixelFormat;

namespace AppleCinnamon
{
    public sealed class Graphics
    {

        public readonly RenderForm RenderForm;
        public readonly RenderTargetView RenderTargetView;
        public readonly DepthStencilView DepthStencilView;
        public readonly Device Device;
        public readonly SwapChain SwapChain;
        public readonly RenderTarget RenderTarget2D;
        public readonly Factory D2dFactory;
        public readonly SharpDX.DirectWrite.Factory DirectWrite;

        public const float ScreenSizeScale = 0.9f;

        public Graphics()
        {
            RenderForm = new RenderForm("Apple & Cinnamon")
            {
                Width = (int)(Screen.PrimaryScreen.Bounds.Width * ScreenSizeScale),
                Height = (int)(Screen.PrimaryScreen.Bounds.Height * ScreenSizeScale),
                StartPosition = FormStartPosition.CenterScreen
            };

            Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.BgraSupport, new SwapChainDescription
            {
                BufferCount = 1,
                ModeDescription = new ModeDescription(RenderForm.ClientSize.Width, RenderForm.ClientSize.Height, new Rational(60, 1), Format.R8G8B8A8_UNorm),
                IsWindowed = true,
                OutputHandle = RenderForm.Handle,
                SampleDescription = new SampleDescription(1, 0) ,
                SwapEffect = SwapEffect.Discard,
                Usage = Usage.RenderTargetOutput
            }, out var device, out var swapChain);

            Device = device;
            SwapChain = swapChain;

            D2dFactory = new Factory();

            SwapChain.GetParent<SharpDX.DXGI.Factory>().MakeWindowAssociation(RenderForm.Handle, WindowAssociationFlags.IgnoreAll);

            var backBuffer = FromSwapChain<Texture2D>(SwapChain, 0);
            RenderTargetView = new RenderTargetView(Device, backBuffer);
            RenderTarget2D = new RenderTarget(D2dFactory, backBuffer.QueryInterface<Surface>(), new RenderTargetProperties(new PixelFormat(Format.Unknown, AlphaMode.Premultiplied)));
            DepthStencilView = new DepthStencilView(Device, new Texture2D(Device, new Texture2DDescription
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
            }));

            DirectWrite = new SharpDX.DirectWrite.Factory();
            Device.ImmediateContext.Rasterizer.SetViewport(new Viewport(0, 0, RenderForm.ClientSize.Width, RenderForm.ClientSize.Height, 0.0f, 1.0f));
        }

        public void Draw(Action drawActions)
        {
            Device.ImmediateContext.OutputMerger.SetTargets(DepthStencilView, RenderTargetView);
            Device.ImmediateContext.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);
            Device.ImmediateContext.ClearRenderTargetView(RenderTargetView, Color.CornflowerBlue);
            RenderTarget2D.BeginDraw();
            drawActions();
            RenderTarget2D.EndDraw();
            SwapChain.Present(0, PresentFlags.None);
        }
    }
}
