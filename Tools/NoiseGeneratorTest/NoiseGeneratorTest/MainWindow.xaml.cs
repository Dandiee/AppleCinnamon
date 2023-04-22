using SharpDX.Direct3D9;
using System.Windows.Interop;
using System.Windows;

namespace NoiseGeneratorTest
{
    public partial class MainWindow
    {
        private readonly Direct3D _d3d;
        private readonly IntPtr _handle;
        private readonly Device _device;
        private Surface _target;

        public MainWindow()
        {
            InitializeComponent();

            _d3d = new Direct3D();

            //Get a handle to the WPF window. This is required to create a device.
            _handle = new WindowInteropHelper(this).Handle;

            //Create a device. Using standard creation param. 
            //Width and height have been set to 1 because we wont be using the backbuffer.
            //Adapter 0 = default adapter.
            _device = new Device(_d3d, 0, DeviceType.Hardware, _handle, CreateFlags.HardwareVertexProcessing, new PresentParameters(1, 1));

            _target = Surface.CreateRenderTarget(_device, _width, _height, Format.A8R8G8B8, MultisampleType.None, 0, true);

            DataContext = new MainWindowViewModel(this, d3dimg, _device);
        }

        private int _width = 256;
        private int _height = 256;

        public void Draw(Surface imageSurface, int width, int height)
        {
            if (_width != width || _height != height)
            {
                _width = width;
                _height = height;
                _target.Dispose();
                _target = Surface.CreateRenderTarget(_device, _width, _height, Format.A8R8G8B8, MultisampleType.None, 0, true);
            }

            _device.UpdateSurface(imageSurface, _target);

            d3dimg.Lock();
            d3dimg.SetBackBuffer(D3DResourceType.IDirect3DSurface9, _target.NativePointer);
            d3dimg.AddDirtyRect(new Int32Rect(0, 0, d3dimg.PixelWidth, d3dimg.PixelHeight));
            d3dimg.Unlock();
        }
    }
}
