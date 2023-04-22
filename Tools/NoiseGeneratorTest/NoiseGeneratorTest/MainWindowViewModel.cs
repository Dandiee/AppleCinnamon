using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;

namespace NoiseGeneratorTest
{
    public partial class MainWindowViewModel : BindableBase
    {
        private readonly MainWindow _window;
        private readonly Random _random = new();

        private bool _supressRender;
        private byte[] _bytes;
        private OctaveNoise _octaveNoise;

        public ICommand RenderCommand { get; }
        public ICommand ReseedCommand { get; }
        public ICommand AddHighlightCommand { get; }
        public ICommand MoveHighlightUpCommand { get; }
        public ICommand MoveHighlightDownCommand { get; }
        public ICommand RemoveHighlightCommand { get; }
        public ICommand ApplyPresetCommand { get; }

        public MainWindowViewModel(MainWindow window)
        {
            _window = window;
            
            RenderCommand = new DelegateCommand(Render);
            ApplyPresetCommand = new DelegateCommand(ApplyPreset);
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

            InitializePresets();
            ResizeArray();
            RecreateNoise();
            Render();
        }

        private void OnHighlightChanged(object? sender, PropertyChangedEventArgs e) => Render();

        private void ResizeArray() => _bytes = new byte[Width * Height * 4];
        private void RecreateNoise() => _octaveNoise = new OctaveNoise(Octaves, new Random(Seed));

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

                _bytes[offset + 0] = b; // BLUE
                _bytes[offset + 1] = g; // GREEN
                _bytes[offset + 2] = r; // RED
                _bytes[offset + 3] = 255; // ALPHA
            });

            _window.Draw(ref _bytes, Width, Height);
        }

        private void SetPropertyAndRender<T>(ref T storage, T value, string propertyName = null)
        {
            if (base.SetProperty(ref storage, value, propertyName))
            {
                Render();
            }
        }
    }
}
