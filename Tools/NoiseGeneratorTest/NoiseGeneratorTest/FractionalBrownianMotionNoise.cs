using NoiseGeneratorTest.Extensions;
using SharpDX;

namespace NoiseGeneratorTest
{
    public sealed class FractionalBrownianMotionNoise
    {
        private static Random Rnd = new();

        public int Octaves { get; set; }
        public float Amplitude { get; set; }

        public float GetValue(Vector2 input)
        {
            float value = 0;
            var amplitude = Amplitude;
            //var shift = new Vector2(100);
            // Loop of octaves
            for (var i = 0; i < Octaves; i++)
            {
                value += amplitude * Noise(input);
                //input = input.Rotate(0.5f) * 2 + shift;
                //input *= 2;
                amplitude *= .5f;
            }
            return value;
        }

        private static readonly Vector2 RandomVector = new(64.9898f, 78.233f);
        private static readonly float RandomScalar = 43758.5453123f;

        
        private static float Noise(Vector2 input)
        {
            var i = input.Floor();
            var f = input.Fract();

            // Four corners in 2D of a tile
            var a = Random(i);
            var b = Random(i + Vector2.UnitX);
            var c = Random(i + Vector2.UnitY);
            var d = Random(i + Vector2.One);

            var u = f * f * (3.0f - 2.0f * f);

            return u.X.Mix(a, b) +
                   (c - a) * u.Y * (1.0f - u.X) +
                   (d - b) * u.X * u.Y;
        }

        private static float Random(Vector2 input)
            => (Math.Sin(Vector2.Dot(input, RandomVector)) * RandomScalar).Fract();

    }
}
