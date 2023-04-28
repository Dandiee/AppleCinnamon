using System;
using System.Windows.Forms;
using AppleCinnamon.Extensions;
using DaniDx.Desktop;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using static SharpDX.Direct3D11.Resource;
using AlphaMode = SharpDX.Direct2D1.AlphaMode;
using Device = SharpDX.Direct3D11.Device;
using Factory = SharpDX.Direct2D1.Factory;
using PixelFormat = SharpDX.Direct2D1.PixelFormat;

using _d2d = SharpDX.Direct2D1;
using _d3d = SharpDX.Direct3D;
using _d3d11 = SharpDX.Direct3D11;
using _dxgi = SharpDX.DXGI;
using SharpDX.DirectInput;


namespace AppleCinnamon.Graphics;

public sealed class GraphicsContext
{

    public const int MSAA = 1;

    public readonly RenderForm RenderForm;
    public readonly RenderTargetView RenderTargetView;
    public readonly DepthStencilView DepthStencilView;
    public readonly Device Device;
    public readonly SwapChain SwapChain;
    public readonly RenderTarget RenderTarget2D;
    public readonly Factory D2dFactory;
    public readonly SharpDX.DirectWrite.Factory DirectWrite;



    public readonly _d2d.DeviceContext3 D2DeviceContext;
    public readonly SpriteBatch SpriteBatch;

    public GraphicsContext()
    {
        Configuration.EnableReleaseOnFinalizer = true;

        const float screenSizeScale = 0.8f;

        RenderForm = new RenderForm("Apple & Cinnamon")
        {
            Width = (int)(Screen.PrimaryScreen.Bounds.Width * screenSizeScale),
            Height = (int)(Screen.PrimaryScreen.Bounds.Height * screenSizeScale),
            StartPosition = FormStartPosition.CenterScreen
        };


        
        _d3d11.Device.CreateWithSwapChain(_d3d.DriverType.Hardware, DeviceCreationFlags.BgraSupport 
            //| DeviceCreationFlags.Debug
            , new SwapChainDescription
        {
            BufferCount = 1,
            ModeDescription = new ModeDescription(RenderForm.ClientSize.Width, RenderForm.ClientSize.Height, 
                new Rational(60, 1), Format.R8G8B8A8_UNorm),
            IsWindowed = true,
            OutputHandle = RenderForm.Handle,
            SampleDescription = new SampleDescription(MSAA, 0),
            SwapEffect = SwapEffect.Discard,
            Usage = Usage.RenderTargetOutput
        }, out var device, out var swapChain);

        Device = device;
        var asd = Device.CheckMultisampleQualityLevels(Format.R8G8B8A8_UNorm, 8);
        SwapChain = swapChain;

        D2dFactory = new Factory();

        D2DeviceContext =
            new _d2d.DeviceContext3(
                new _d2d.Device3(new Factory().QueryInterface<_d2d.Factory4>(),
                    device.QueryInterface<_dxgi.Device>()), DeviceContextOptions.None);
        D2DeviceContext.Target = D2DeviceContext.CreateBitmapRenderTarget(swapChain);
        SpriteBatch = new SpriteBatch(D2DeviceContext);

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
            SampleDescription = new SampleDescription(MSAA, 0),
            Usage = ResourceUsage.Default,
            BindFlags = BindFlags.DepthStencil,
            CpuAccessFlags = CpuAccessFlags.None,
            OptionFlags = ResourceOptionFlags.None
        }));

        DirectWrite = new SharpDX.DirectWrite.Factory();
        Device.ImmediateContext.Rasterizer.SetViewport(new Viewport(0, 0, RenderForm.ClientSize.Width, RenderForm.ClientSize.Height, 0.0f, 1.0f));

        Device.ImmediateContext.Rasterizer.State = new RasterizerState(Device, new RasterizerStateDescription
        {
            CullMode = SharpDX.Direct3D11.CullMode.Back,
            FillMode = SharpDX.Direct3D11.FillMode.Solid,
            IsFrontCounterClockwise = false,
            IsAntialiasedLineEnabled = true,
            IsMultisampleEnabled = true,
        });

    }

    public void Draw(Action drawActions)
    {
        //Device.ImmediateContext.ClearState();
        //Device.ImmediateContext.Flush();

        Device.ImmediateContext.OutputMerger.SetTargets(DepthStencilView, RenderTargetView);
        Device.ImmediateContext.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);
        Device.ImmediateContext.ClearRenderTargetView(RenderTargetView, Color.CornflowerBlue);
        RenderTarget2D.BeginDraw();
        drawActions();
        RenderTarget2D.EndDraw();
        SwapChain.Present(0, PresentFlags.None);

    }
}