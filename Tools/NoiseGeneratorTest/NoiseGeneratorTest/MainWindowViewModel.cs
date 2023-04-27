using System.CodeDom;
using System.Windows.Media;
using Prism.Mvvm;

namespace NoiseGeneratorTest
{
    public partial class MainWindowViewModel : BindableBase
    {
        private readonly MainWindow _window;
        private readonly Random _random = new();

        public PerlinViewModel PerlinLeft { get; }
        public PerlinViewModel PerlinRight { get; }

        public MainWindowViewModel(MainWindow window)
        {
            _window = window;

            InitializePresets();

            PerlinLeft = new PerlinViewModel(window.ImageLeft);
            PerlinRight = new PerlinViewModel(window.ImageRight);

            PerlinLeft.Rendered += (_, _) => SourceChanged();
            PerlinRight.Rendered += (_, _) => SourceChanged();
        }

        private void SourceChanged()
        {
            if (PerlinLeft.Width == PerlinRight.Width && PerlinLeft.Height == PerlinRight.Height)
            {
                var width = PerlinLeft.Width;
                var height = PerlinLeft.Height;

                var bytes = new byte[PerlinLeft.Bytes.Length];

                for (var ij = 0; ij < PerlinLeft.ScaledValues.Length; ij++)
                {
                    var i = ij % width;
                    var j = ij / width;

                    var value = PerlinLeft.ScaledValues[ij] * PerlinLeft.ScaledValues[ij] * PerlinRight.ScaledValues[ij];
                    var byteValue = (byte)(value * 255);

                    var offset = ij * 4;

                    var leftValue = PerlinLeft.ScaledValues[ij];
                    var rightValue = PerlinRight.ScaledValues[ij];

                    var waterHighlight = PerlinLeft.Highlights[1];
                    var waterLevel = waterHighlight.RangeFrom / 256f;

                    if (leftValue > waterLevel)
                    {
                        var offsetLeftValue = leftValue - waterLevel;
                        var landScaler = offsetLeftValue / ((waterHighlight.RangeTo - waterHighlight.RangeFrom) / 256f);

                        if (landScaler > 0.9)
                        {

                        }

                        byteValue = (byte)(PerlinLeft.ScaledValues[ij] * 255);

                        var color = Color.FromRgb(0, 255, 127);

                        var scaler = Math.Pow(landScaler, 3);

                        bytes[offset + 0] = (byte)(color.B * (scaler * rightValue)); // BLUE
                        bytes[offset + 1] = (byte)(color.G * (scaler * rightValue)); // GREEN
                        bytes[offset + 2] = (byte)(color.R * (scaler * rightValue)); // RED
                        bytes[offset + 3] = 255; // ALPHA
                    }
                    else // water dont care
                    {
                        bytes[offset + 0] = 255; // BLUE
                        bytes[offset + 1] = 0; // GREEN
                        bytes[offset + 2] = 0; // RED
                        bytes[offset + 3] = 255; // ALPHA
                    }
                }

                

                _window.ImageSum.Draw(ref bytes, width, height);
            }
        }
    }
}
