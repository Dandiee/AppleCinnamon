using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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

        private int _width;
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

        private int _amplitude;
        public int Amplitude
        {
            get => _amplitude;
            set => SetProperty(ref _amplitude, value);
        }

        private int _frequency;
        public int Frequency
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

        public ICommand RenderCommand { get; }

        public MainWindowViewModel()
        {
            Width = 32;
            Height = 32;
            Octaves = 8;
            Factor = 1f;
            Amplitude = 1;
            Frequency = 1;

            RenderCommand = new DelegateCommand(Render);
        }

        private byte[,] GenerateNoise()
        {
            var result = new byte[Width, Height];
            var values = new double[Width, Height];
            var listValues = new List<double>(Width * Height);

            var noise = new OctaveNoise(Octaves, new Random(9212107), Amplitude, Frequency);
            for (var i = 0; i < Width; i++)
            {
                for (var j = 0; j < Height; j++)
                {
                    var value = noise.Compute(i, j);

                    values[i, j] = (byte) (value * Factor);

                    listValues.Add(value);
                }
            }

            var min = listValues.Min();
            var max = listValues.Max();

            var range = (max - min) / 255f;

            for (var i = 0; i < Width; i++)
            {
                for (var j = 0; j < Height; j++)
                {
                    result[i, j] = (byte) ((values[i, j] - min) / range);
                }
            }

            return result;
        }

        private void Render()
        {
            var sw = Stopwatch.StartNew();
            var noise = GenerateNoise();
            sw.Stop();
            RenderTime = (int)sw.ElapsedMilliseconds;

            var bitmap = GetBitmap(noise);
            Image = GetBitmapImage(bitmap);
        }

        private Bitmap GetBitmap(byte[,] heightMap)
        {
            var bitmap = new Bitmap(Width, Height);

            for (var i = 0; i < Width; i++)
            {
                for (var j = 0; j < Height; j++)
                {
                    var height = heightMap[i, j];

                    bitmap.SetPixel(i, j, Color.FromArgb(height, height, height));
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
