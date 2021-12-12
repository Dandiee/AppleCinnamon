using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Prism.Commands;
using Prism.Mvvm;
using Color = System.Drawing.Color;

namespace NoiseGeneratorTest
{
    public class MainWindowViewModel : BindableBase
    {


        private BitmapImage _image;
        public BitmapImage Image
        {
            get => _image;
            set => SetProperty(ref _image, value);
        }

        private int _width ;
        public int Width
        {
            get => _width;
            set => SetProperty(ref _width, value);
        }

        private int _height;
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

        private double _amplitude = 1;
        public double Amplitude
        {
            get => _amplitude;
            set => SetProperty(ref _amplitude, value);
        }

        private double _frequency = 1;
        public double Frequency
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


        private double _recordedMin;
        public double RecordedMin
        {
            get => _recordedMin;
            set => SetProperty(ref _recordedMin, value);
        }

        private double _recordedMax;
        public double RecordedMax
        {
            get => _recordedMax;
            set => SetProperty(ref _recordedMax, value);
        }

        private double _recordedRange;
        public double RecordedRange
        {
            get => _recordedRange;
            set => SetProperty(ref _recordedRange, value);
        }

        private double _factoredRange;
        public double FactoredRange
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


        public MainWindowViewModel()
        {
            Width = 128;
            Height = 128;
            Octaves = 8;
            Factor = 0.47f;
            Amplitude = 1.1;
            Frequency = 0.4;
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
            var values = new double[Width, Height];
            var listValues = new List<double>(Width * Height);

            var noise = new OctaveNoise(Octaves, new Random(seed), Amplitude, Frequency);

            var fromI = Width / -2;
            var fromJ = Height / -2;

            for (var i = 0; i < Width; i++)
            {
                for (var j = 0; j < Height; j++)
                {
                    var value = noise.Compute(i + fromI, j + fromJ);

                    values[i, j] = (byte) (value * Factor);

                    listValues.Add(value);
                }
            }

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
                    result[i, j] = (byte) ((values[i, j] + Offset));
                }
            }

            return result;
        }

        private void Render()
        {
            var sw = Stopwatch.StartNew();
            var noise = GenerateNoise(Seed);
            sw.Stop();
            RenderTime = (int)sw.ElapsedMilliseconds;

            var bitmap = GetBitmap(noise);
            Image = GetBitmapImage(bitmap);
        }


        private static readonly Color WaterColor = Color.Aqua;
        private static readonly Color SnowColor = Color.Snow;
        private static readonly Color GrassColor = Color.Green;

        private Bitmap GetBitmap(byte[,] heightMap)
        {
            var bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);

            for (var i = 0; i < Width; i++)
            {
                for (var j = 0; j < Height; j++)
                {
                    var height = heightMap[i, j];
                    var color = Color.Empty;
                    var refHeight = 0;

                    if (UseGrayScale)
                    {
                        refHeight = height;
                        color = Color.FromArgb(height, height, height);
                    }
                    else
                    {
                        if (height < WaterLevel)
                        {
                            color = WaterColor;
                            refHeight = WaterLevel - height;
                        }
                        else if (height < SnowLevel)
                        {
                            color = GrassColor;
                            refHeight = height - WaterLevel;
                        }
                        else
                        {
                            color = SnowColor;
                            refHeight = 255 - height;
                        }
                    }

                    if (ShowWater)
                    {
                        if (height <= WaterLevel)
                        {
                            color = WaterColor;
                        }
                    }

                    if (IsHighlightValueEnabled)
                    {
                        if (Math.Abs(height - HighlightedValue) <= HighlightedRange)
                        {
                            color = Color.Red;
                        }
                    }

                    bitmap.SetPixel(i, j, Color.FromArgb(refHeight, color));
                }
            }

            return bitmap;
        }

        private BitmapImage GetBitmapImage(Bitmap bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                return bitmapImage;
            }
        }
    }
}
