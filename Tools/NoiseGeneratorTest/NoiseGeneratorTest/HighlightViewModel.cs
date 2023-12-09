using System.Windows.Media;
using Prism.Mvvm;
using Color = System.Windows.Media.Color;

namespace NoiseGeneratorTest
{
    public sealed class HighlightViewModel : BindableBase
    {
        private float _value;
        public float Value
        {
            get => _value;
            set => SetPropertyAndRages(ref _value, value);
        }

        private float _range;
        public float Range
        {
            get => _range;
            set => SetPropertyAndRages(ref _range, value);
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

        private float _rangeFrom;
        public float RangeFrom
        {
            get => _rangeFrom;
            set => SetProperty(ref _rangeFrom, value);
        }

        private float _rangeTo;
        public float RangeTo
        {
            get => _rangeTo;
            set => SetProperty(ref _rangeTo, value);
        }

        private void SetPropertyAndRages<T>(ref T storage, T value, string propertyName = null)
        {
            if (SetProperty(ref storage, value, propertyName))
            {
                RangeFrom = (byte)Math.Max(0, Value - Range);
                RangeTo = (byte)Math.Min(255, Value + Range);
            }
        }
    }
}
