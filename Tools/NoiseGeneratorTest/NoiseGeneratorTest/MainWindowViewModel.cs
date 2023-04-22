﻿using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Prism.Commands;
using Prism.Mvvm;
using SharpDX.Direct3D9;
using SharpDX.Mathematics.Interop;

namespace NoiseGeneratorTest
{
    public class MainWindowViewModel : BindableBase
    {
        private readonly MainWindow _window;
        private readonly D3DImage _d3DImage;
        private readonly Device _device;

        private BitmapImage _image;
        public BitmapImage Image
        {
            get => _image;
            set => SetProperty(ref _image, value);
        }

        private int _width = 256;
        public int Width
        {
            get => _width;
            set => SetProperty(ref _width, value);
        }

        private int _height = 256;
        public int Height
        {
            get => _height;
            set => SetProperty(ref _height, value);
        }


        private int _octaves;
        public int Octaves
        {
            get => _octaves;
            set => SetProperty(ref _octaves, value);
        }

        private float _factor;
        public float Factor
        {
            get => _factor;
            set => SetProperty(ref _factor, value);
        }

        private float _amplitude = 1;
        public float Amplitude
        {
            get => _amplitude;
            set => SetProperty(ref _amplitude, value);
        }

        private float _frequency = 1;
        public float Frequency
        {
            get => _frequency;
            set => SetProperty(ref _frequency, value);
        }


        private int _renderTime;
        public int RenderTime
        {
            get => _renderTime;
            set => SetProperty(ref _renderTime, value);
        }


        private int _waterLevel;
        public int WaterLevel
        {
            get => _waterLevel;
            set => SetProperty(ref _waterLevel, value);
        }

        private int _snowLevel;
        public int SnowLevel
        {
            get => _snowLevel;
            set => SetProperty(ref _snowLevel, value);
        }

        private int _grassLevel;
        public int GrassLevel
        {
            get => _grassLevel;
            set => SetProperty(ref _grassLevel, value);
        }

        private bool _useGrayScale;
        public bool UseGrayScale
        {
            get => _useGrayScale;
            set => SetProperty(ref _useGrayScale, value);
        }

        private bool _isHighlightValueEnabled;
        public bool IsHighlightValueEnabled
        {
            get => _isHighlightValueEnabled;
            set => SetProperty(ref _isHighlightValueEnabled, value);
        }

        private bool _showWater = false;
        public bool ShowWater
        {
            get => _showWater;
            set => SetProperty(ref _showWater, value);
        }


        private byte _highlightedValue;
        public byte HighlightedValue
        {
            get => _highlightedValue;
            set => SetProperty(ref _highlightedValue, value);
        }

        private byte _highlightedRange;
        public byte HighlightedRange
        {
            get => _highlightedRange;
            set => SetProperty(ref _highlightedRange, value);
        }


        private float _recordedMin;
        public float RecordedMin
        {
            get => _recordedMin;
            set => SetProperty(ref _recordedMin, value);
        }

        private float _recordedMax;
        public float RecordedMax
        {
            get => _recordedMax;
            set => SetProperty(ref _recordedMax, value);
        }

        private float _recordedRange;
        public float RecordedRange
        {
            get => _recordedRange;
            set => SetProperty(ref _recordedRange, value);
        }

        private float _factoredRange;
        public float FactoredRange
        {
            get => _factoredRange;
            set => SetProperty(ref _factoredRange, value);
        }

        private int _offset;
        public int Offset
        {
            get => _offset;
            set => SetProperty(ref _offset, value);
        }

        private int _seed;
        public int Seed
        {
            get => _seed;
            set => SetProperty(ref _seed, value);
        }

        public ICommand RenderCommand { get; }
        public ICommand ReseedCommand { get; }

        private readonly Random _random = new Random();


        public MainWindowViewModel(MainWindow window, D3DImage d3DImage, Device device)
        {
            _window = window;
            _d3DImage = d3DImage;
            _device = device;
            Octaves = 8;
            Factor = 0.47f;
            Amplitude = 1.1f;
            Frequency = 0.4f;
            Offset = 134;

            WaterLevel = 119;
            SnowLevel = 128;
            GrassLevel = 65;
            Seed = 1513;// _random.Next(0, 9999);
            RenderCommand = new DelegateCommand(Render);
            ReseedCommand = new DelegateCommand(Reseed);
        }

        private void Reseed()
        {
            Seed = _random.Next(0, 9999);
            Render();
        }

        private byte[,] GenerateNoise(int seed)
        {
            var result = new byte[Width, Height];
            var values = new float[Width, Height];
            var listValues = new List<float>(Width * Height);

            var noise = new OctaveNoise(Octaves, new Random(seed), Amplitude, Frequency);

            var fromI = Width / -2;
            var fromJ = Height / -2;

            Parallel.For(0, Width, i =>
            {
                for (var j = 0; j < Height; j++)
                {
                    var value = noise.Compute(i + fromI, j + fromJ);

                    values[i, j] = (byte)(value * Factor);

                    listValues.Add(value);
                }
            });
            //for (var i = 0; i < Width; i++)
            //{
            //    for (var j = 0; j < Height; j++)
            //    {
            //        
            //    }
            //}

            var min = listValues.Min();
            var max = listValues.Max();

            RecordedMin = min;
            RecordedMax = max;

            RecordedRange = max - min;
            FactoredRange = RecordedRange * Factor;

            for (var i = 0; i < Width; i++)
            {
                for (var j = 0; j < Height; j++)
                {
                    result[i, j] = (byte)((values[i, j] + Offset));
                }
            }

            return result;
        }

        private void Render()
        {
            var sw = Stopwatch.StartNew();
            var bytes = GenerateNoise(Seed);
            DoSomething(bytes);
            sw.Stop();
            RenderTime = (int)sw.ElapsedMilliseconds;
        }

        private void DoSomething(byte[,] heatMap)
        {
            var bytes = new List<byte>();

            for (var j = 0; j < Height; j++)
            {
                for (var i = 0; i < Width; i++)
                {
                    var b = (byte)heatMap[i, j];

                    bytes.Add(b); // BLUE
                    bytes.Add(b); // GREEN
                    bytes.Add(b); // RED
                    bytes.Add(255); // ALPHA
                }
            }

            var imageSurface = Surface.CreateOffscreenPlain(_device, Width, Height, Format.A8R8G8B8, Pool.SystemMemory);
            Surface.FromMemory(imageSurface, bytes.ToArray(), Filter.None, 0, Format.A8R8G8B8, 4 * Width,
                new RawRectangle(0, 0, Width, Height));
            _window.Draw(imageSurface, Width, Height);
        }


        private static readonly Color WaterColor = Color.Aqua;
        private static readonly Color SnowColor = Color.Snow;
        private static readonly Color GrassColor = Color.Green;



        //private Bitmap GetBitmap(byte[,] heightMap)
        //{
        //    var bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);

        //    for (var i = 0; i < Width; i++)
        //    {
        //        for (var j = 0; j < Height; j++)
        //        {
        //            var height = heightMap[i, j];
        //            var color = Color.Empty;
        //            var refHeight = 0;

        //            if (UseGrayScale)
        //            {
        //                refHeight = height;
        //                color = Color.FromArgb(height, height, height);
        //            }
        //            else
        //            {
        //                if (height < WaterLevel)
        //                {
        //                    color = WaterColor;
        //                    refHeight = WaterLevel - height;
        //                }
        //                else if (height < SnowLevel)
        //                {
        //                    color = GrassColor;
        //                    refHeight = height - WaterLevel;
        //                }
        //                else
        //                {
        //                    color = SnowColor;
        //                    refHeight = 255 - height;
        //                }
        //            }

        //            if (ShowWater)
        //            {
        //                if (height <= WaterLevel)
        //                {
        //                    color = WaterColor;
        //                }
        //            }

        //            if (IsHighlightValueEnabled)
        //            {
        //                if (Math.Abs(height - HighlightedValue) <= HighlightedRange)
        //                {
        //                    color = Color.Red;
        //                }
        //            }

        //            bitmap.SetPixel(i, j, Color.FromArgb(refHeight, color));
        //        }
        //    }

        //    return bitmap;
        //}

        //private BitmapImage GetBitmapImage(Bitmap bitmap)
        //{
        //    using (var memory = new MemoryStream())
        //    {
        //        bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
        //        memory.Position = 0;
        //        var bitmapImage = new BitmapImage();
        //        bitmapImage.BeginInit();
        //        bitmapImage.StreamSource = memory;
        //        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        //        bitmapImage.EndInit();

        //        return bitmapImage;
        //    }
        //}
    }
}
