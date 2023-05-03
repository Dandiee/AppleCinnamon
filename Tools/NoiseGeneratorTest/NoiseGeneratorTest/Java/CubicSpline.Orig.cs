namespace NoiseGeneratorTest.Java
{

    public abstract class CubicSpline<TC, T> : ToFloatFunction<TC>
        where T : ToFloatFunction<TC>
    {


        public abstract CubicSpline<TC, T> MapAll(ICoordinateVisitor<T> p211579);
        public static CubicSpline<TC, T> Constant(float p184240) => new Constant<TC, T>(p184240);
        public static Builder<TC, T> Builder(T p184253) => new(p184253);
        public static Builder<TC, T> Builder(T p184255, ToFloatFunction<float> p184256) => new(p184255, p184256);
        public float apply(TC p184786) => throw new NotImplementedException();
        public float minValue { get; set; }
        public float maxValue { get; set; }
    }

    public class Builder<TC, T> where T : ToFloatFunction<TC>
    {
        private readonly T _coordinate;
        private readonly ToFloatFunction<float> _valueTransformer;
        private readonly List<float> _locations = new();
        private readonly List<CubicSpline<TC, T>> _values = new();
        private readonly List<float> _derivatives = new();

        public Builder(T p184293)
            : this(p184293, ToFloatFunction2.IDENTITY)
        { }

        public Builder(T p184295, ToFloatFunction<float> p184296)
        {
            _coordinate = p184295;
            _valueTransformer = p184296;
        }

        public Builder<TC, T> AddPoint(float p216115, float p216116)
            => AddPoint(p216115, new Constant<TC, T>(_valueTransformer.apply(p216116)), 0.0F);

        public Builder<TC, T> AddPoint(float p184299, float p184300, float p184301)
            => AddPoint(p184299, new Constant<TC, T>(_valueTransformer.apply(p184300)), p184301);

        public Builder<TC, T> AddPoint(float p216118, CubicSpline<TC, T> p216119)
            => AddPoint(p216118, p216119, 0.0F);

        private Builder<TC, T> AddPoint(float p184303, CubicSpline<TC, T> p184304, float p184305)
        {
            if (_locations.Count != 0 && p184303 <= _locations[^1])
            {
                throw new NotSupportedException("Please register points in ascending order");
            }

            _locations.Add(p184303);
            _values.Add(p184304);
            _derivatives.Add(p184305);
            return this;
        }

        public CubicSpline<TC, T> Build()
        {
            if (_locations.Count == 0)
            {
                throw new NotSupportedException("No elements added");
            }

            return Multipoint<TC, T>.Create(_coordinate, _locations.ToArray(), _values.ToList(), _derivatives.ToArray());
        }
    }

    public class Constant<TC, T> : CubicSpline<TC, T> where T : ToFloatFunction<TC>
    {
        public float Value;

        public Constant(float value)
        {
            Value = value;
        }

        public float Apply(TC p184313) => Value;
        public override CubicSpline<TC, T> MapAll(ICoordinateVisitor<T> p211581) => this;
    }

    public interface ICoordinateVisitor<T>
    {
        T Visit(T p216123);
    }

    public class Multipoint<TC, T> : CubicSpline<TC, T>
        where T : ToFloatFunction<TC>
    {
        public T Coordinate;
        public float[] Locations;
        public List<CubicSpline<TC, T>> Values;
        public float[] Derivatives;
        public float MinValue;
        public float MaxValue;

        public Multipoint(T coordinate, float[] locations, List<CubicSpline<TC, T>> values, float[] derivatives, float minValue, float maxValue)
        {
            Coordinate = coordinate;
            Locations = locations;
            Values = values;
            Derivatives = derivatives;
            MinValue = minValue;
            MaxValue = maxValue;

            ValidateSizes(Locations, Values, Derivatives);
        }

        public static Multipoint<TC, T> Create(T p216144, float[] p216145, List<CubicSpline<TC, T>> p216146, float[] p216147)
        {
            ValidateSizes(p216145, p216146, p216147);

            var i = p216145.Length - 1;
            var f = float.PositiveInfinity;
            var f1 = float.NegativeInfinity;
            var f2 = p216144.minValue;
            var f3 = p216144.maxValue;
            if (f2 < p216145[0])
            {
                var f4 = LinearExtend(f2, p216145, p216146[0].minValue, p216147, 0);
                var f5 = LinearExtend(f2, p216145, p216146[0].maxValue, p216147, 0);
                f = Math.Min(f, Math.Min(f4, f5));
                f1 = Math.Max(f1, Math.Max(f4, f5));
            }

            if (f3 > p216145[i])
            {
                var f24 = LinearExtend(f3, p216145, p216146[i].minValue, p216147, i);
                var f25 = LinearExtend(f3, p216145, p216146[i].maxValue, p216147, i);
                f = Math.Min(f, Math.Min(f24, f25));
                f1 = Math.Max(f1, Math.Max(f24, f25));
            }

            foreach (var cubicspline2 in p216146)
            {
                f = Math.Min(f, cubicspline2.minValue);
                f1 = Math.Max(f1, cubicspline2.maxValue);
            }

            for (var j = 0; j < i; ++j)
            {
                var f26 = p216145[j];
                var f6 = p216145[j + 1];
                var f7 = f6 - f26;
                var cubicspline = p216146[j];
                var cubicspline1 = p216146[j + 1];
                var f8 = cubicspline.minValue;
                var f9 = cubicspline.maxValue;
                var f10 = cubicspline1.minValue;
                var f11 = cubicspline1.maxValue;
                var f12 = p216147[j];
                var f13 = p216147[j + 1];
                if (f12 != 0.0F || f13 != 0.0F)
                {
                    var f14 = f12 * f7;
                    var f15 = f13 * f7;
                    var f16 = Math.Min(f8, f10);
                    var f17 = Math.Max(f9, f11);
                    var f18 = f14 - f11 + f8;
                    var f19 = f14 - f10 + f9;
                    var f20 = -f15 + f10 - f9;
                    var f21 = -f15 + f11 - f8;
                    var f22 = Math.Min(f18, f20);
                    var f23 = Math.Max(f19, f21);
                    f = Math.Min(f, f16 + 0.25F * f22);
                    f1 = Math.Max(f1, f17 + 0.25F * f23);
                }
            }

            return new Multipoint<TC, T>(p216144, p216145, p216146, p216147, f, f1);
        }

        private static float LinearExtend(float p216134, float[] p216135, float p216136, float[] p216137, int p216138)
        {
            var f = p216137[p216138];
            return f == 0.0F ? p216136 : p216136 + f * (p216134 - p216135[p216138]);
        }

        private static void ValidateSizes(float[] p216152, List<CubicSpline<TC, T>> p216153, float[] p216154)
        {
            if (p216152.Length == p216153.Count && p216152.Length == p216154.Length)
            {
                if (p216152.Length == 0)
                {
                    throw new NotSupportedException("Cannot create a multipoint spline with no points");
                }
            }
            else
            {
                throw new NotSupportedException("All lengths must be equal, got: " + p216152.Length + " " + p216153.Count + " " + p216154.Length);
            }
        }

        public float Apply(TC p184340)
        {
            var f = Coordinate.apply(p184340);
            var i = FindIntervalStart(Locations, f);
            var j = Locations.Length - 1;
            if (i < 0)
            {
                return LinearExtend(f, Locations, Values[0].apply(p184340), Derivatives, 0);
            }
            else if (i == j)
            {
                return LinearExtend(f, Locations, Values[j].apply(p184340), Derivatives, j);
            }
            else
            {
                var f1 = Locations[i];
                var f2 = Locations[i + 1];
                var f3 = (f - f1) / (f2 - f1);
                ToFloatFunction<TC> tofloatfunction = Values[i];
                ToFloatFunction<TC> tofloatfunction1 = Values[i + 1];
                var f4 = Derivatives[i];
                var f5 = Derivatives[i + 1];
                var f6 = tofloatfunction.apply(p184340);
                var f7 = tofloatfunction1.apply(p184340);
                var f8 = f4 * (f2 - f1) - (f7 - f6);
                var f9 = -f5 * (f2 - f1) + (f7 - f6);
                return Mth.Lerp(f3, f6, f7) + f3 * (1.0F - f3) * Mth.Lerp(f3, f8, f9);
            }
        }

        private static int FindIntervalStart(float[] p216149, float p216150)
        {
            return Mth.BinarySearch(0, p216149.Length, (p216142)
                =>
                {
                    return p216150 < p216149[p216142];
                }) - 1;
        }

        public override CubicSpline<TC, T> MapAll(ICoordinateVisitor<T> p211585)
        {
            return Create(p211585.Visit(Coordinate), Locations, Values.Select((p211588) => p211588.MapAll(p211585)).ToList(), Derivatives);
        }
    }

    public static class Mth
    {
        public static float Lerp(float v0, float v1, float t)
        {
            return (1 - t) * v0 + t * v1;
        }

        public static int BinarySearch(int i, int length, Func<int, bool> func)
        {
            return 1;
        }
    }
}
