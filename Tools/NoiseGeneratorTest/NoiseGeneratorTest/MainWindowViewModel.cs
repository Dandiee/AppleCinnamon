using System.CodeDom;
using System.Windows.Media;
using Prism.Mvvm;
using SharpDX.Direct3D9;

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

        private void SetColor(ref byte[] bytes, int offset, Color color, float scale)
        {
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

                    var leftValue = PerlinLeft.ScaledValues[ij];
                    var rightValue = PerlinRight.ScaledValues[ij];

                    // water
                    if (leftValue <= PerlinLeft.SetPoint)
                    {
                        var value = leftValue;
                        var color = PerlinLeft.UnderColor;

                        SetColor(ref bytes, offset, color, value);
                    }
                    //// mountain
                    //else if (rightValue >= PerlinRight.SetPoint)
                    //{
                    //    var value = leftValue * leftValue * rightValue * rightValue;
                    //    var color = PerlinLeft.OverColor;
                    //
                    //    SetColor(ref bytes, offset, color, value);
                    //}
                    //// flat land
                    //else
                    //{
                    //    var value = (float)Math.Pow(leftValue, 3) * rightValue;
                    //    var color = PerlinLeft.OverColor;
                    //
                    //    SetColor(ref bytes, offset, color, value);
                    //}
                    else
                    {
                        var value = leftValue * leftValue * rightValue * rightValue;
                        var color = PerlinLeft.OverColor;
                        
                        SetColor(ref bytes, offset, color, value);
                    }
                }

                

                _window.ImageSum.Draw(ref bytes, width, height);
            }
        }
    }
}
