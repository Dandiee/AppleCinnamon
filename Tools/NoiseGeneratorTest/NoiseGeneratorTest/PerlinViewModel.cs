using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Media;
using Prism.Commands;
using Prism.Mvvm;

namespace NoiseGeneratorTest
{
    public sealed class PerlinViewModel : BindableBase
    {
        public ObservableCollection<HighlightViewModel> Highlights { get; } = new();

        private readonly Random _random = new();

        private bool _supressRender;
        public byte[] Bytes;
        private OctaveNoise _octaveNoise;

        public ICommand RenderCommand { get; }
        public ICommand ReseedCommand { get; }
        public ICommand AddHighlightCommand { get; }
        public ICommand MoveHighlightUpCommand { get; }
        public ICommand MoveHighlightDownCommand { get; }
        public ICommand RemoveHighlightCommand { get; }

        private D3dImageUc _image;

        public event EventHandler<PerlinViewModel> Rendered;

        public PerlinViewModel(D3dImageUc image)
        {
            _image = image;

            _seed = _random.Next();

            RenderCommand = new DelegateCommand(Render);
            ReseedCommand = new DelegateCommand(() => Seed = _random.Next(0, 9999));
            AddHighlightCommand = new DelegateCommand(() => Highlights.Add(new HighlightViewModel()));
            MoveHighlightUpCommand = new DelegateCommand<HighlightViewModel>(h =>
            {
                var index = Highlights.IndexOf(h);
                if (index > 0)
                {
                    Highlights.Move(index, index - 1);
                }
            });

            MoveHighlightDownCommand = new DelegateCommand<HighlightViewModel>(h =>
            {
                var index = Highlights.IndexOf(h);
                if (index < Highlights.Count - 1)
                {
                    Highlights.Move(index, index + 1);
                }
            });

            RemoveHighlightCommand = new DelegateCommand<HighlightViewModel>(h => Highlights.Remove(h));

            Highlights.CollectionChanged += (_, args) =>
            {
                if (args.NewItems != null)
                {
                    foreach (var item in args.NewItems.OfType<BindableBase>())
                    {
                        item.PropertyChanged += OnHighlightChanged;
                    }
                }

                if (args.OldItems != null)
                {
                    foreach (var item in args.OldItems.OfType<BindableBase>())
                    {
                        item.PropertyChanged -= OnHighlightChanged;
                    }
                }

                Render();
            };

            ResizeArray();
            RecreateNoise();
            Render();
        }

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

        private void ResizeArray() => Bytes = new byte[Width * Height * 4];
        private void RecreateNoise() => _octaveNoise = new OctaveNoise(Octaves, new Random(Seed));

        private void SetPropertyAndRender<T>(ref T storage, T value, string propertyName = null)
        {
            if (SetProperty(ref storage, value, propertyName))
            {
                Render();
            }
        }

        private void Render()
        {
            if (!_supressRender)
            {
                var sw = Stopwatch.StartNew();
                GenerateNoise();
                sw.Stop();
                RenderTime = (int)sw.ElapsedMilliseconds;
            }
        }

        private void OnHighlightChanged(object? sender, PropertyChangedEventArgs e) => Render();

        private void GenerateNoise()
        {
            var fromI = Width / -2;
            var fromJ = Height / -2;

            Parallel.For(0, Width * Height, ij =>
            {
                var i = ij % Width;
                var j = ij / Width;

                var value = _octaveNoise.Compute(i + fromI, j + fromJ, Amplitude, Frequency);
                
                var factoredByteValue = (byte)(value * Factor + Offset);
                
                var ratio = factoredByteValue / 255f;

                var offset = ij * 4;

                var r = (byte)(BaseColor.R * ratio);
                var g = (byte)(BaseColor.G * ratio);
                var b = (byte)(BaseColor.B * ratio);

                foreach (var highlight in Highlights)
                {
                    var highlightFactor = highlight.IsSolid ? 1 : ratio;

                    if (Math.Abs(factoredByteValue - highlight.Value) < highlight.Range)
                    {
                        r = (byte)(highlight.Color.R * highlightFactor);
                        g = (byte)(highlight.Color.G * highlightFactor);
                        b = (byte)(highlight.Color.B * highlightFactor);
                    }
                }

                Bytes[offset + 0] = b; // BLUE
                Bytes[offset + 1] = g; // GREEN
                Bytes[offset + 2] = r; // RED
                Bytes[offset + 3] = 255; // ALPHA
            });

            _image.Draw(ref Bytes, Width, Height);

            Rendered?.Invoke(this, this);
        }
    }
}
