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

                for(var ij = 0; ij < bytes.Length; ij++)
                {
                    var left = PerlinLeft.Bytes[ij] / 255f;
                    var right = PerlinRight.Bytes[ij];

                    //byte total = (byte)((byte)(left / 2) + (byte)(right / 2));
                    byte total = (byte)(left * right);

                    bytes[ij] = total;
                };

                _window.ImageSum.Draw(ref bytes, width, height);
            }
        }
    }
}
