namespace NoiseGeneratorTest
{
    public partial class PerlinMixer
    {
        public PerlinViewModel PerlinViewModel { get; }

        public PerlinMixer()
        {
            InitializeComponent();

            PerlinViewModel = new PerlinViewModel(RawNoise, false);

            MySplineEditor.OnUpdated += (sender, _) =>
            {
                Mix((SplineEditor)sender);
            };
        }

        public void SetSize(int size)
        {
            PerlinViewModel.SupressRender = true;
            PerlinViewModel.Width = size;
            PerlinViewModel.Height = size;
            PerlinViewModel.SupressRender = false;
            PerlinViewModel.Render();
        }

        private void Mix(SplineEditor splineEditor)
        {
            var bytes = new byte[PerlinViewModel.Bytes.Length];

            var canvasHeight = splineEditor.MyCanvas.Height;

            Parallel.For(0, PerlinViewModel.ScaledValues.Length, ij =>
            {
                var offset = ij * 4;

                var rawValue = PerlinViewModel.ScaledValues[ij] * PerlinViewModel.Factor + PerlinViewModel.Offset;
                var splinedValue = splineEditor.GetValue(rawValue, canvasHeight);
                var byteValue = (byte)(255 * splinedValue);

                bytes[offset + 0] = byteValue; // BLUE
                bytes[offset + 1] = byteValue; // GREEN
                bytes[offset + 2] = byteValue; // RED
                bytes[offset + 3] = 255; // ALPHA
            });

            MixedImage.Draw(ref bytes, PerlinViewModel.Width, PerlinViewModel.Height);
        }
    }
}
