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

        private int _size = 512;
        public int Size
        {
            get => _size;
            set
            {
                if (SetProperty(ref _size, value))
                {
                    _window.ContinentMixer.SetSize(value);
                    _window.ErosionMixer.SetSize(value);
                    _window.PeakMixer.SetSize(value);
                }
            }
        }


        public ICommand MixCommand { get; }

        public void GrandMix()
        {
            var c = _window.ContinentMixer;
            var e = _window.ErosionMixer;
            var p = _window.PeakMixer;
            var floats = new float[Size * Size];

            if (c.MixedValues != null && e.MixedValues != null && p.MixedValues != null)
            {
                var l1 = c.MixedValues.Length;
                var l2 = e.MixedValues.Length;
                var l3 = p.MixedValues.Length;

                if (l1 != l2 || l2 != l3 || l1 != l3) return;

                var bytes = new byte[_window.ContinentMixer.PerlinViewModel.Bytes.Length];

                var min = float.MaxValue;
                var max = float.MinValue;

                //Parallel.For(0, Size, ij =>
                for (var ij = 0; ij < _window.ContinentMixer.PerlinViewModel.ScaledValues.Length; ij++)
                {
                    var offset = ij * 4;

                    var n1 = c.MixedValues[ij];
                    var n2 = e.MixedValues[ij];
                    var n3 = p.MixedValues[ij];

                    var n12 = (n1 + n2 + n3) / 3f;
                    //if (Math.Sign(n12) == Math.Sign(n3))
                    //{
                    //    n12 *= n3;
                    //}
                    //
                    //if (n3 < 0)
                    //{
                    //
                    //}

                    var total = (n12);

                    if (min > total) min = total;
                    if (max < total) max = total;

                    floats[ij] = total;

                    var byteValue = (byte)((total + 1f) * 0.5f * 255);
                    
                    bytes[offset + 0] = byteValue; // BLUE
                    bytes[offset + 1] = byteValue; // GREEN
                    bytes[offset + 2] = byteValue; // RED
                    bytes[offset + 3] = 255; // ALPHA

                }

                var range = max - min;

                for (var ij = 0; ij < _window.ContinentMixer.PerlinViewModel.ScaledValues.Length; ij++)
                {
                    var offset = ij * 4;

                    var n1 = c.MixedValues[ij];
                    var n2 = e.MixedValues[ij];
                    var n3 = p.MixedValues[ij];

                    var total = n1 * n2 * n3;

                    if (min > total) min = total;
                    if (max < total) max = total;

                    floats[ij] = total;

                    //var byteValue = (byte)((total + 1f) * 0.5f * 255);
                    //
                    //bytes[offset + 0] = byteValue; // BLUE
                    //bytes[offset + 1] = byteValue; // GREEN
                    //bytes[offset + 2] = byteValue; // RED
                    //bytes[offset + 3] = 255; // ALPHA

                }
                //);

                try
                {
                    _window.GrandTotal.Draw(ref bytes, Size, Size);
                }
                catch  {}
                
            }
        }

        public MainWindowViewModel(MainWindow window)
        {
            _window = window;

            //window.ContinentMixer.Mixed += (_, _) => GrandMix();
            //window.ErosionMixer.Mixed += (_, _) => GrandMix();
            //window.PeakMixer.Mixed += (_, _) => GrandMix();

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
