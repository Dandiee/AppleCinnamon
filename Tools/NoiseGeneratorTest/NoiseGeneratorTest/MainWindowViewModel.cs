using System.Diagnostics;
using System.Drawing;
using System.IO;
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


        private int _renderTime;
        public int RenderTime
        {
            get => _renderTime;
            set => SetProperty(ref _renderTime, value);
        }

        public ICommand RenderCommand { get; }

        public MainWindowViewModel()
        {
            Width = 64;
            Height = 64;
            Octaves = 8;
            Factor = 0.2f;

            RenderCommand = new DelegateCommand(Render);
        }

        private byte[,] GenerateNoise()
        {
            var result = new byte[Width, Height];

            var noise = new OctaveNoise(Octaves, new JavaRandom(9212107));
            for (var i = 0; i < Width; i++)
            {
                for (var j = 0; j < Height; j++)
                {
                    result[i, j] = (byte) (noise.Compute(i, j) + 128 * Factor);
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
