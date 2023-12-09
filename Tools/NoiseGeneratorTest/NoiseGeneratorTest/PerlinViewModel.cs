using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Prism.Commands;
using Prism.Mvvm;
using SharpDX;
using SharpDX.Direct3D9;
using Color = System.Windows.Media.Color;
using System.Windows.Media.Media3D;
using Clipboard = System.Windows.Clipboard;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace NoiseGeneratorTest
{
    public sealed class PerlinViewModel : BindableBase
    {
        public ObservableCollection<HighlightViewModel> Highlights { get; } = new();

        private readonly Random _random = new();

        public readonly bool IsNormalizingEnabled;

        public bool SupressRender;
        public byte[] Bytes;
        public float[] ScaledValues;
        private OctaveNoise _octaveNoise;

        public ICommand RenderCommand { get; }
        public ICommand ReseedCommand { get; }
        public ICommand SaveAsPngCommand { get; }
        public ICommand AddHighlightCommand { get; }
        public ICommand MoveHighlightUpCommand { get; }
        public ICommand MoveHighlightDownCommand { get; }
        public ICommand RemoveHighlightCommand { get; }
        public ICommand CompensateToByteCommand { get; }
        public ICommand ImportCommand { get; }
        public ICommand ExportCommand { get; }

        private D3dImageUc _image;

        public event EventHandler<PerlinViewModel> Rendered;

        public void Normalize()
        {
            SupressRender = true;

            Factor = 2f / ValueRange;

            FactoredMinimumValue = MinimumValue * Factor;
            FactoredMaximumValue = MaximumValue * Factor;
            var offset = 1 - FactoredMaximumValue;
            FactoredValueRange = FactoredMaximumValue - FactoredMinimumValue;

            Offset = offset;

            SupressRender = false;
        }

        public PerlinViewModel(D3dImageUc image, bool isNormalizingEnabled = true)
        {
            IsNormalizingEnabled = isNormalizingEnabled;
            _image = image;

            _seed = _random.Next();

            CompensateToByteCommand = new DelegateCommand(() =>
            {
                Normalize();
                Render();
            });
            ImportCommand = new DelegateCommand(Import);
            ExportCommand = new DelegateCommand(Export);
            RenderCommand = new DelegateCommand(Render);
            SaveAsPngCommand = new DelegateCommand(SaveAsPng);
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

        private void SaveAsPng()
        {

            var bmp = new Bitmap(Width, Height);

            for (var ij = 0; ij < Width * Height; ij++)
            {
                var i = ij % Width;
                var j = ij / Width;
                var offset = ij * 4;
                var blue = Bytes[offset + 0]; // BLUE
                var green = Bytes[offset + 1]; // GREEN
                var red = Bytes[offset + 2]; // RED
                var alpha = Bytes[offset + 3] = 255; // ALPHA

                bmp.SetPixel(i, j, System.Drawing.Color.FromArgb(alpha, red, green, blue));
            }

            SaveFileDialog dialof = new SaveFileDialog();
            if (dialof.ShowDialog() == DialogResult.OK)
            {
                bmp.Save(dialof.FileName, System.Drawing.Imaging.ImageFormat.Png);
            };
        }

        private void Import()
        {
            var text = Clipboard.GetText();
            if (!string.IsNullOrEmpty(text))
            {
                SupressRender = true;
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
                SupressRender = false;
                Render();
            }
        }

        private void Export()
        {
            var firstLine = string.Create(CultureInfo.InvariantCulture,
                $"{Width};{Height};{Octaves};{Amplitude};{Frequency};{Offset};{Factor};{BaseColor.ToString()};{Seed};{SetPoint};{UnderColor};{OverColor}");
            var highlightings = string.Join(Environment.NewLine, Highlights.Select(s =>
                string.Create(CultureInfo.InvariantCulture, $"{s.Value};{s.Range};{s.IsSolid};{s.Color}")));

            var codeLine = string.Create(CultureInfo.InvariantCulture,
                $"{Octaves},{Frequency},{Amplitude},{Offset},{Factor},{Seed}");
            Clipboard.SetText($"{firstLine}{Environment.NewLine}{highlightings}{Environment.NewLine}{codeLine}");
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

        private Color _underColor = Colors.White;
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

        public void Render()
        {
            if (!SupressRender)
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

                if (IsNormalizingEnabled)
                {

                    var factored = value * Factor + Offset;
                    var factoredToOne = (factored + 1f) / 2f;
                    ScaledValues[ij] = factored;

                    var r = (byte)(BaseColor.R * factoredToOne);
                    var g = (byte)(BaseColor.G * factoredToOne);
                    var b = (byte)(BaseColor.B * factoredToOne);


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
                }
                else
                {
                    ScaledValues[ij] = value;
                }

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

            if (!IsNormalizingEnabled)
            {
                Normalize();

                Parallel.For(0, ScaledValues.Length, ij =>
                {
                    var offset = ij * 4;
                    var rawValue = (byte)(255 * (((ScaledValues[ij] * Factor + Offset)) + 1) * 0.5f);

                    Bytes[offset + 0] = rawValue; // BLUE
                    Bytes[offset + 1] = rawValue; // GREEN
                    Bytes[offset + 2] = rawValue; // RED
                    Bytes[offset + 3] = 255; // ALPHA
                });
            }

            _image.Draw(ref Bytes, Width, Height);

            Rendered?.Invoke(this, this);
        }


    }
}
