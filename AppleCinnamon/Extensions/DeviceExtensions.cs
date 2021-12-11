using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.WIC;
using Device = SharpDX.Direct3D11.Device;

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
    }
}
