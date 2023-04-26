using System.Windows.Media;

namespace NoiseGeneratorTest
{
    public partial class MainWindowViewModel
    {
        public Preset[] Presets { get; private set; }

        private void InitializePresets()
        {
            Presets = new[]
            {
                new Preset("Lakes", 512, 512, 8, 3145, 0.4098f, 2.371f, 1f, -144, Colors.White)
                {
                    Highlights = new HighlightViewModel[]
                    {
                        new () { Color = Color.FromRgb(0, 255, 0), IsSolid = false, Value = 101, Range = 79 },
                        new () { Color = Color.FromRgb(0, 255, 255), IsSolid = false, Value = 0, Range = 74 },
                        new () { Color = Color.FromRgb(255, 235, 205), IsSolid = true, Value = 74, Range = 4 },
                        new () { Color = Color.FromRgb(255, 255, 255), IsSolid = true, Value = 255, Range = 62 }
                    }
                },
                new Preset("Wood", 512, 512, 12, 3145, 0.4499f, 4.2637f, 4.5313f, -144, Color.FromRgb(139, 69, 19)),
                new Preset("River", 512, 512, 12, 3145, 0.0920f, 0.81548f, 5, 41, Colors.White)
                {
                    Highlights = new HighlightViewModel[]
                    {
                        new () { Color = Color.FromRgb(0, 255, 255), IsSolid = false, Value = 222, Range = 63 },
                        new () { Color = Color.FromRgb(0, 255, 0), IsSolid = false, Value = 7, Range = 149 },
                        new () { Color = Color.FromRgb(255, 235, 205), IsSolid = true, Value = 0, Range = 7 },
                    }
                },
                new Preset("FlipFlop", 512, 512, 11, 3145, 0.918077f, 3.375184f, 1.56903f, 300, Colors.White)
                {
                    Highlights = new HighlightViewModel[]
                    {
                        new () { Color = Colors.Fuchsia, IsSolid = false, Value = 132, Range = 19 },
                        new () { Color = Colors.Aqua, IsSolid = false, Value = 86, Range = 29 },
                    }
                },
            };

            _selectedPreset = Presets[0];
        }

        private void ApplyPreset()
        {
            //_supressRender = true;

            //Width = SelectedPreset.Width;
            //Height = SelectedPreset.Height;
            //Octaves = SelectedPreset.Octaves;
            //Seed = SelectedPreset.Seed;
            //Factor = SelectedPreset.Factor;
            //Amplitude = SelectedPreset.Amplitude;
            //Frequency = SelectedPreset.Frequency;
            //Offset = SelectedPreset.Offset;
            //BaseColor = SelectedPreset.BaseColor;

            //Highlights.Clear();
            //foreach (var highlight in SelectedPreset.Highlights)
            //{
            //    Highlights.Add(highlight);
            //}

            //_supressRender = false;
            //Render();
        }

        private Preset _selectedPreset;
        public Preset SelectedPreset
        {
            get => _selectedPreset;
            set => SetProperty(ref _selectedPreset, value);
        }
    }

    public sealed class Preset
    {
        public string Name { get; }
        public HighlightViewModel[] Highlights { get; init; } = Array.Empty<HighlightViewModel>();

        public int Width { get;  }
        public int Height { get; }
        public int Octaves { get; }
        public int Seed { get; }
        public float Factor { get; }
        public float Amplitude { get; }
        public float Frequency { get; }
        public int Offset { get; }
        public Color BaseColor { get; }

        public Preset(string name, int width, int height, int octaves, int seed, float factor, float amplitude, float frequency, int offset, Color baseColor)
        {
            Name = name;
            Width = width;
            Height = height;
            Octaves = octaves;
            Seed = seed;
            Factor = factor;
            Amplitude = amplitude;
            Frequency = frequency;
            Offset = offset;
            BaseColor = baseColor;
        }
    }
}
