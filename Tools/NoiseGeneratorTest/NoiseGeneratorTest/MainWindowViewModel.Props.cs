using System.Collections.ObjectModel;
using System.Windows.Media;

namespace NoiseGeneratorTest
{
    public partial class MainWindowViewModel
    {
        public ObservableCollection<HighlightViewModel> Highlights { get; }= new();

        private Color _baseColor = Color.FromRgb(255, 255, 255);
        public Color BaseColor
        {
            get => _baseColor;
            set => SetPropertyAndRender(ref _baseColor, value);
        }

        private int _width = 256;
        public int Width
        {
            get => _width;
            set
            {
                if (SetProperty(ref _width, value))
                {
                    ResizeArray();
                    Render();
                }
            }
        }

        private int _height = 256;
        public int Height
        {
            get => _height;
            set
            {
                if (SetProperty(ref _height, value))
                {
                    ResizeArray();
                    Render();
                }
            }
        }

        private int _octaves = 8;
        public int Octaves
        {
            get => _octaves;
            set
            {
                if (SetProperty(ref _octaves, value))
                {
                    RecreateNoise();
                    Render();
                }
            }
        }

        private int _seed = 1513;
        public int Seed
        {
            get => _seed;
            set
            {
                if (SetProperty(ref _seed, value))
                {
                    RecreateNoise();
                    Render();
                }
            }
        }

        private float _factor = 0.5836f;
        public float Factor
        {
            get => _factor;
            set => SetPropertyAndRender(ref _factor, value);
        }

        private float _amplitude = 1.1f;
        public float Amplitude
        {
            get => _amplitude;
            set => SetPropertyAndRender(ref _amplitude, value);
        }

        private float _frequency = 3.66f;
        public float Frequency
        {
            get => _frequency;
            set => SetPropertyAndRender(ref _frequency, value);
        }


        private int _renderTime;
        public int RenderTime
        {
            get => _renderTime;
            set => SetProperty(ref _renderTime, value);
        }


        private int _offset = 92;
        public int Offset
        {
            get => _offset;
            set => SetPropertyAndRender(ref _offset, value);
        }
    }
}
