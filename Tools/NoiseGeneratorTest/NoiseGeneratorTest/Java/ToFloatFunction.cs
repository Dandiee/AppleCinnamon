using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoiseGeneratorTest.Java
{
    public interface ToFloatFunction<C>
    {
        float apply(C p_184786_);

        public float minValue { get; set; }
        public float maxValue { get; set; }
    }


    public class ToFloatFunction2 : ToFloatFunction<float>
    {
        public static readonly ToFloatFunction<float> IDENTITY = new ToFloatFunction2(a => a);

        private readonly Func<float, float> _lambda;

        public float minValue { get; set; }
        public float maxValue { get; set; }

        public ToFloatFunction2(Func<float, float> lambda)
        {
            _lambda = lambda;
        }

        public float apply(float p_216496_) => _lambda(p_216496_);

        public static ToFloatFunction<float> createUnlimited(Func<float, float> func)
        {
            return new ToFloatFunction2(func)
            {
                minValue = float.MinValue,
                maxValue = float.MaxValue,
            };
        }
    }
}
