using System.CodeDom;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Prism.Commands;
using Prism.Mvvm;
using SharpDX.Direct2D1.Effects;
using SharpDX.Direct3D9;

namespace NoiseGeneratorTest
{
    public partial class MainWindowViewModel : BindableBase
    {
        private readonly MainWindow _window;
        private readonly Random _random = new();

        public PerlinViewModel PerlinLeft { get; }
        public PerlinViewModel PerlinRight { get; }
        public PerlinViewModel RawImageVm { get; }

        public ICommand MixCommand { get; }

        public MainWindowViewModel(MainWindow window)
        {
            _window = window;

            InitializePresets();

            MixCommand = new DelegateCommand<SplineEditor>(obj =>
            {
                Mix(obj);
            });

            PerlinLeft = new PerlinViewModel(window.ImageLeft);
            PerlinRight = new PerlinViewModel(window.ImageRight);

            RawImageVm = new PerlinViewModel(window.RawImage);

            window.MySplineEditor.OnUpdated += (sender, _) =>
            {
                Mix((SplineEditor)sender);
            };

            PerlinLeft.Rendered += (_, _) => SourceChanged();
            PerlinRight.Rendered += (_, _) => SourceChanged();
        }

        private void Mix(SplineEditor splineEditor)
        {
            var bytes = new byte[RawImageVm.Bytes.Length];

            var canvasHeight = splineEditor.MyCanvas.Height;

            Parallel.For(0, RawImageVm.ScaledValues.Length, ij =>
            {
                var offset = ij * 4;

                var rawValue = RawImageVm.ScaledValues[ij];
                var splinedValue = splineEditor.GetValue(rawValue, canvasHeight);

                bytes[offset + 0] = (byte)(255 * splinedValue); // BLUE
                bytes[offset + 1] = (byte)(255 * splinedValue); // GREEN
                bytes[offset + 2] = (byte)(255 * splinedValue); // RED
                bytes[offset + 3] = 255; // ALPHA
            });

            _window.MixImage.Draw(ref bytes, RawImageVm.Width, RawImageVm.Height);
        }

        private void SetColor(ref byte[] bytes, int offset, Color color, float scale)
        {
            if (scale < 0 || scale > 1f)
            {

            }

            color = Colors.White;

            bytes[offset + 0] = (byte)(color.B * (scale)); // BLUE
            bytes[offset + 1] = (byte)(color.G * (scale)); // GREEN
            bytes[offset + 2] = (byte)(color.R * (scale)); // RED
            bytes[offset + 3] = 255; // ALPHA
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
                    //var i = ij % width;  var j = ij / width;

                    var offset = ij * 4;

                    var leftValue = PerlinLeft.ScaledValues[ij] - PerlinLeft.SetPoint;
                    var rightValue = PerlinRight.ScaledValues[ij];

                    // water
                    if (leftValue < 0)
                    {
                        var value = leftValue + PerlinLeft.SetPoint;
                        var color = PerlinLeft.UnderColor;

                        SetColor(ref bytes, offset, color, value);
                    }
                    else
                    {
                        var value = leftValue * rightValue * rightValue + PerlinLeft.SetPoint;
                        var color = PerlinLeft.OverColor;

                        SetColor(ref bytes, offset, color, value);
                    }
                }

                _window.ImageSum.Draw(ref bytes, width, height);
            }
        }
    }
}
