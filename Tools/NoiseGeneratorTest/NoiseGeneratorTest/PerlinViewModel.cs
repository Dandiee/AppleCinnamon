using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Prism.Commands;
using Prism.Mvvm;
using SharpDX;
using Color = System.Windows.Media.Color;

namespace NoiseGeneratorTest
{
    public sealed class PerlinViewModel : BindableBase
    {
        public ObservableCollection<HighlightViewModel> Highlights { get; } = new();

        private readonly Random _random = new();

        private bool _supressRender;
        public byte[] Bytes;
        public float[] ScaledValues;
        private OctaveNoise _octaveNoise;

        public ICommand RenderCommand { get; }
        public ICommand ReseedCommand { get; }
        public ICommand AddHighlightCommand { get; }
        public ICommand MoveHighlightUpCommand { get; }
        public ICommand MoveHighlightDownCommand { get; }
        public ICommand RemoveHighlightCommand { get; }
        public ICommand CompensateToByteCommand { get; }
        public ICommand ImportCommand { get; }
        public ICommand ExportCommand { get; }

        private D3dImageUc _image;

        public event EventHandler<PerlinViewModel> Rendered;

        public PerlinViewModel(D3dImageUc image)
        {
            _image = image;

            _seed = _random.Next();

            CompensateToByteCommand = new DelegateCommand(() =>
            {
                _supressRender = true;

                Factor = 2f / ValueRange;
                FactoredMinimumValue = MinimumValue * Factor;
                FactoredMaximumValue = MaximumValue * Factor;
                FactoredValueRange = FactoredMaximumValue - FactoredMinimumValue;

                Offset = (-1f - FactoredMinimumValue);
                

                _supressRender = false;
                Render();
            });
            ImportCommand = new DelegateCommand(Import);
            ExportCommand = new DelegateCommand(Export);
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

        private void Import()
        {
            var text = Clipboard.GetText();
            if (!string.IsNullOrEmpty(text))
            {
                _supressRender = true;
                try
                {
                    var lines = text.Split(Environment.NewLine);
                    var noiseData = lines[0].Split(";");
                    Width = int.Parse(noiseData[0], CultureInfo.InvariantCulture);
                    Height = int.Parse(noiseData[1], CultureInfo.InvariantCulture);
                    Octaves = int.Parse(noiseData[2], CultureInfo.InvariantCulture);
                    Amplitude = float.Parse(noiseData[3], CultureInfo.InvariantCulture);
                    Frequency = float.Parse(noiseData[4], CultureInfo.InvariantCulture);
                    Offset = float.Parse(noiseData[5], CultureInfo.InvariantCulture);
                    Factor = float.Parse(noiseData[6], CultureInfo.InvariantCulture);
                    BaseColor = (Color)ColorConverter.ConvertFromString(noiseData[7]);
                    Seed = int.Parse(noiseData[8], CultureInfo.InvariantCulture);
                    SetPoint = float.Parse(noiseData[9], CultureInfo.InvariantCulture);
                    UnderColor = (Color)ColorConverter.ConvertFromString(noiseData[10]);
                    OverColor = (Color)ColorConverter.ConvertFromString(noiseData[11]);

                    Highlights.Clear();

                    for (var i = 1; i < lines.Length; i++)
                    {
                        var highlightData = lines[i].Split(";");

                        Highlights.Add(new HighlightViewModel
                        {
                            Value = byte.Parse(highlightData[0], CultureInfo.InvariantCulture),
                            Range = byte.Parse(highlightData[1], CultureInfo.InvariantCulture),
                            IsSolid = bool.Parse(highlightData[2]),
                            Color = (Color)ColorConverter.ConvertFromString(highlightData[3]),
                        });
                    }
                }
                catch { }
                _supressRender = false;
                Render();
            }
        }

        private void Export()
        {
            var firstLine = string.Create(CultureInfo.InvariantCulture,
                $"{Width};{Height};{Octaves};{Amplitude};{Frequency};{Offset};{Factor};{BaseColor.ToString()};{Seed};{SetPoint};{UnderColor};{OverColor}");
            var highlightings = string.Join(Environment.NewLine, Highlights.Select(s =>
                string.Create(CultureInfo.InvariantCulture, $"{s.Value};{s.Range};{s.IsSolid};{s.Color}")));
            Clipboard.SetText($"{firstLine}{Environment.NewLine}{highlightings}");
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

        private float _minimumValue;
        public float MinimumValue
        {
            get => _minimumValue;
            set => SetProperty(ref _minimumValue, value);
        }

        private float _factoredMinimumValue;
        public float FactoredMinimumValue
        {
            get => _factoredMinimumValue;
            set => SetProperty(ref _factoredMinimumValue, value);
        }

        private float _maximumValue;
        public float MaximumValue
        {
            get => _maximumValue;
            set => SetProperty(ref _maximumValue, value);
        }

        private float _factoredMaximumValue;
        public float FactoredMaximumValue
        {
            get => _factoredMaximumValue;
            set => SetProperty(ref _factoredMaximumValue, value);
        }

        private float _valueRange;
        public float ValueRange
        {
            get => _valueRange;
            set => SetProperty(ref _valueRange, value);
        }

        private float _factoredValueRange;
        public float FactoredValueRange
        {
            get => _factoredValueRange;
            set => SetProperty(ref _factoredValueRange, value);
        }

        private float _setPoint;
        public float SetPoint
        {
            get => _setPoint;
            set => SetPropertyAndRender(ref _setPoint, value);
        }

        private Color _underColor  = Colors.White;
        public Color UnderColor
        {
            get => _underColor;
            set => SetPropertyAndRender(ref _underColor, value);
        }

        private Color _overColor = Colors.White;
        public Color OverColor
        {
            get => _overColor;
            set => SetPropertyAndRender(ref _overColor, value);
        }


        private float _offset = 0;
        public float Offset
        {
            get => _offset;
            set => SetPropertyAndRender(ref _offset, value);
        }

        private void ResizeArray()
        {
            Bytes = new byte[Width * Height * 4];
            ScaledValues = new float[Width * Height];
        }

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

            var lockObj = new object();

            var globalMinMax = new Vector2(9999, -9999);

            Parallel.For(0, Width * Height,
                () => new Vector2(float.MaxValue, float.MinValue),
                (ij, state, localMinMax) =>
            {
                var i = ij % Width;
                var j = ij / Width;

                var value = _octaveNoise.Compute(i + fromI, j + fromJ, Amplitude, Frequency);

                if (localMinMax.X > value) localMinMax.X = value;
                if (localMinMax.Y < value) localMinMax.Y = value;

                var signedFactored = value * Factor + Offset;

                var factored = (signedFactored + 1f) / 2;

                ScaledValues[ij] = factored;

                var r = (byte)(BaseColor.R * factored);
                var g = (byte)(BaseColor.G * factored);
                var b = (byte)(BaseColor.B * factored);

                if (signedFactored <= SetPoint)
                {
                    r = (byte)(UnderColor.R * factored);
                    g = (byte)(UnderColor.G * factored);
                    b = (byte)(UnderColor.B * factored);
                }
                else
                {
                    r = (byte)(OverColor.R * factored);
                    g = (byte)(OverColor.G * factored);
                    b = (byte)(OverColor.B * factored);
                }

                foreach (var highlight in Highlights)
                {
                    var highlightFactor = highlight.IsSolid ? 1 : factored;

                    if (Math.Abs(factored - highlight.Value) < highlight.Range)
                    {
                        r = (byte)(highlight.Color.R * highlightFactor);
                        g = (byte)(highlight.Color.G * highlightFactor);
                        b = (byte)(highlight.Color.B * highlightFactor);
                    }
                }

                var offset = ij * 4;
                Bytes[offset + 0] = b; // BLUE
                Bytes[offset + 1] = g; // GREEN
                Bytes[offset + 2] = r; // RED
                Bytes[offset + 3] = 255; // ALPHA

                return localMinMax;
            }, localMinMax =>
            {
                lock (lockObj)
                {
                    if (globalMinMax.X > localMinMax.X) globalMinMax.X = localMinMax.X;
                    if (globalMinMax.Y < localMinMax.Y) globalMinMax.Y = localMinMax.Y;
                }
            });

            MinimumValue = globalMinMax.X;
            MaximumValue = globalMinMax.Y;
            ValueRange = MaximumValue - MinimumValue;

            _image.Draw(ref Bytes, Width, Height);

            Rendered?.Invoke(this, this);
        }
    }
}
