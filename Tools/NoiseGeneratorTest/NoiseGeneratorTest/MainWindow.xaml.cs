﻿namespace NoiseGeneratorTest
{
    public partial class MainWindow
    {
        //private readonly Device _device;
        //private Surface _target;
        //private Surface _surface;

        //private static readonly Duration Timeout = new(TimeSpan.FromSeconds(1));

        public MainWindowViewModel ViewModel { get; }

        public MainWindow()
        {
            InitializeComponent();

            //_device = new Device(new Direct3D(), 0, DeviceType.Hardware, new WindowInteropHelper(this).Handle, CreateFlags.HardwareVertexProcessing, new PresentParameters(1, 1));
            //_target = Surface.CreateRenderTarget(_device, _width, _height, Format.A8R8G8B8, MultisampleType.None, 0, true);
            //_surface = Surface.CreateOffscreenPlain(_device, _width, _height, Format.A8R8G8B8, Pool.SystemMemory);

            ViewModel = new MainWindowViewModel(this);
            DataContext = ViewModel;
        }

        private int _width = 256;
        private int _height = 256;

        public void Draw(ref byte[] bytes, int width, int height)
        {
            //MainImage.Draw(ref bytes, width, height);
            //MainImage2.Draw(ref bytes, width, height);

            //if (!NoiseD3DImage.TryLock(Timeout))
            //{
            //    throw new Exception("Sorry mate");
            //}

            //if (_width != width || _height != height)
            //{
            //    _width = width;
            //    _height = height;

            //    _target.Dispose();
            //    _target = Surface.CreateRenderTarget(_device, _width, _height, Format.A8R8G8B8, MultisampleType.None, 0, true);

            //    _surface.Dispose();
            //    _surface = Surface.CreateOffscreenPlain(_device, _width, _height, Format.A8R8G8B8, Pool.SystemMemory);
            //}

            //Surface.FromMemory(_surface, bytes, Filter.None, 0, Format.A8R8G8B8, 4 * _width, new RawRectangle(0, 0, _width, _height));
            //_device.UpdateSurface(_surface, _target);
            //NoiseD3DImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, _target.NativePointer);
            //NoiseD3DImage.AddDirtyRect(new Int32Rect(0, 0, NoiseD3DImage.PixelWidth, NoiseD3DImage.PixelHeight));
            //NoiseD3DImage.Unlock();
        }
    }
}
