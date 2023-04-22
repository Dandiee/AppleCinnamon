using System.Windows.Media;
using Prism.Mvvm;

namespace NoiseGeneratorTest
{
    public sealed class HighlightViewModel : BindableBase
    {
        private byte _value;
        public byte Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }

        private byte _range;
        public byte Range
        {
            get => _range;
            set => SetProperty(ref _range, value);
        }

        private Color _color;
        public Color Color
        {
            get => _color;
            set => SetProperty(ref _color, value);
        }

        private bool _isSolid = true;
        public bool IsSolid
        {
            get => _isSolid;
            set => SetProperty(ref _isSolid, value);
        }
    }
}
