using System.Runtime.CompilerServices;
using NoiseGeneratorTest.Extensions;
using SharpDX;

namespace NoiseGeneratorTest
{
    public sealed class DiscreteFractionalBrownianMotionNoise
    {
        public int Octaves { get; set; }
        public float Amplitude { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public float Frequency { get; set; }

        public float GetValue(int x, int y)
        {
            float value = 0;
            var amplitude = Amplitude;
            //var shift = new Vector2(100);
            // Loop of octaves
            for (var i = 0; i < Octaves; i++)
            {
                value += amplitude * Noise(x, y);
                //input = input.Rotate(0.5f) * 2 + shift;
                x *= 2;
                y *= 2;
                amplitude *= .5f;
            }
            return value;
        }

        private static readonly float RandomVectorX = 64.9898f;
        private static readonly float RandomVectorY = 78.233f;
        private static readonly float RandomScalar = 43758.5453123f;

        
        private float Noise(int i, int j)
        {
            var x = (i / Width) * Frequency;
            var y = (j / Height) * Frequency;

            var iX = (float)Math.Floor(x);
            var iY = (float)Math.Floor(y);

            var fX = x - iX;
            var fY = y - iY;

            // Four corners in 2D of a tile
            var a = Random(iX, iY);
            var b = Random(iX + 1, iY);
            var c = Random(iX, iY + 1);
            var d = Random(iX + 1, iY + 1);

            var uX = fX * fX * (3.0f - 2.0f * fX);
            var uY = fY * fY * (3.0f - 2.0f * fY);

            return uX.Mix(a, b) +
                   (c - a) * uY * (1.0f - uX) +
                   (d - b) * uX * uY;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Random(float x, float y)
        {
            var dot = x * RandomVectorX + y * RandomVectorY;
            var scalar = Math.Sin(dot) * RandomScalar;
            return (float)(scalar - Math.Floor(scalar));
        }
    }
}
