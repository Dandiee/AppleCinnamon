using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using SharpDX;
using SharpDX.Direct2D1.Effects;
using SharpDX.Direct3D9;

namespace NoiseGeneratorTest.Extensions
{
    public static class VectorExtensions
    {
        public static Vector2 Floor(this Vector2 v) => new((int)Math.Floor(v.X), (int)Math.Floor(v.Y));
        public static Vector2 Fract(this Vector2 v) => v - v.Floor();

        public static float Fract(this float f) => (float)(f - Math.Floor(f));
        public static float Fract(this double f) => (float)(f - Math.Floor(f));
        public static float Mix(this float value, float min, float max) => min + value * (max - min);
        public static Vector2 Rotate(this Vector2 v, float alpha)
        {
            var sin = (float)Math.Sin(alpha);
            var cos = (float)Math.Cos(alpha);

            float tx = v.X;
            float ty = v.Y;
            return new Vector2((cos * tx) - (sin * ty),  (sin * tx) + (cos * ty));
        }
    }
}
