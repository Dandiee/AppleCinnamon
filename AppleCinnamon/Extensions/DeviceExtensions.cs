using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.IO;
using SharpDX.WIC;
using Device = SharpDX.Direct3D11.Device;
using DeviceContext = SharpDX.Direct2D1.DeviceContext;
using PixelFormat = SharpDX.WIC.PixelFormat;
using _d2d = SharpDX.Direct2D1;
namespace AppleCinnamon.Extensions
{
    public static class DeviceExtensions
    {
        public static Texture2D CreateTexture2DFromBitmap(this Device device, string fileName)
        {
            var bitmapSource = LoadBitmap(new ImagingFactory2(), fileName);
            // Allocate DataStream to receive the WIC image pixels
            int stride = bitmapSource.Size.Width * 4;
            using var buffer = new DataStream(bitmapSource.Size.Height * stride, true, true);

            // Copy the content of the WIC to the buffer
            bitmapSource.CopyPixels(stride, buffer);
            return new Texture2D(device, new Texture2DDescription
            {
                Width = bitmapSource.Size.Width,
                Height = bitmapSource.Size.Height,
                ArraySize = 1,
                BindFlags = BindFlags.ShaderResource,
                Usage = ResourceUsage.Immutable,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = Format.R8G8B8A8_UNorm,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                SampleDescription = new SampleDescription(1, 0)
            }, new DataRectangle(buffer.DataPointer, stride));
        }

        public static BitmapSource LoadBitmap(ImagingFactory2 factory, string filename)
        {
            var formatConverter = new FormatConverter(factory);
            formatConverter.Initialize(new BitmapDecoder(factory, filename, DecodeOptions.CacheOnDemand).GetFrame(0),
                PixelFormat.Format32bppPRGBA, BitmapDitherType.None, null, 0.0, BitmapPaletteType.Custom);
            return formatConverter;
        }

        public static Bitmap1 CreateD2DBitmap(this DeviceContext deviceContext, string filePath)
        {
            var imagingFactory = new ImagingFactory();

            var fileStream = new NativeFileStream(
                filePath,
                NativeFileMode.Open,
                NativeFileAccess.Read);

            var bitmapDecoder = new BitmapDecoder(imagingFactory, fileStream, DecodeOptions.CacheOnDemand);
            var frame = bitmapDecoder.GetFrame(0);

            var converter = new FormatConverter(imagingFactory);
            converter.Initialize(frame, PixelFormat.Format32bppPRGBA);

            var newBitmap = Bitmap1.FromWicBitmap(deviceContext, converter);

            return newBitmap;
        }

        public static Bitmap1 CreateBitmapRenderTarget(this _d2d.DeviceContext3 d2dDeContext, SwapChain swapChain)
        {
            using var backBuffer = SharpDX.Direct3D11.Resource.FromSwapChain<Texture2D>(swapChain, 0);
            using var surface = backBuffer.QueryInterface<Surface>();

            var bmpProperties = new BitmapProperties1(
                new SharpDX.Direct2D1.PixelFormat(Format.R8G8B8A8_UNorm, SharpDX.Direct2D1.AlphaMode.Premultiplied),
                dpiX: 96,
                dpiY: 96,
                bitmapOptions: BitmapOptions.Target | BitmapOptions.CannotDraw);

            return new Bitmap1(d2dDeContext, surface, bmpProperties);
        }
    }
}
