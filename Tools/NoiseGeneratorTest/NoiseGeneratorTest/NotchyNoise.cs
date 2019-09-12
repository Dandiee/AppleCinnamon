using System;

namespace NoiseGeneratorTest
{

    public sealed class ImprovedNoise
    {
        private readonly byte[] _p;

        public ImprovedNoise(Random random)
        {
            _p = new byte[512];

            for (int i = 0; i < 256; i++)
            {
                _p[i] = (byte) i;
            }

            for (int i = 0; i < 256; i++)
            {
                int j = random.Next(i, 256);
                byte temp = _p[i]; _p[i] = _p[j]; _p[j] = temp;
            }

            for (int i = 0; i < 256; i++)
            {
                _p[i + 256] = _p[i];
            }
        }

        public double Compute(double x, double y)
        {
            const int xFlags = 0x46552222;
            const int yFlags = 0x2222550A;

            var xFloor = x >= 0 
                ? (int)x 
                : (int)x - 1;


            var yFloor = y >= 0 
                ? (int)y 
                : (int)y - 1;

            var X = xFloor & 0xFF;
            var Y = yFloor & 0xFF;

            x -= xFloor;
            y -= yFloor;

            var u = x * x * x * (x * (x * 6 - 15) + 10);
            var v = y * y * y * (y * (y * 6 - 15) + 10);

            var A = _p[X] + Y;
            var B = _p[X + 1] + Y;

            
            var hash = (_p[_p[A]] & 0xF) << 1;
            var g22 = (((xFlags >> hash) & 3) - 1) * x + (((yFlags >> hash) & 3) - 1) * y;

            hash = (_p[_p[B]] & 0xF) << 1;
            var g12 = (((xFlags >> hash) & 3) - 1) * (x - 1) + (((yFlags >> hash) & 3) - 1) * y;
            var c1 = g22 + u * (g12 - g22);

            hash = (_p[_p[A + 1]] & 0xF) << 1;
            var g21 = (((xFlags >> hash) & 3) - 1) * x + (((yFlags >> hash) & 3) - 1) * (y - 1);

            hash = (_p[_p[B + 1]] & 0xF) << 1;
            var g11 = (((xFlags >> hash) & 3) - 1) * (x - 1) + (((yFlags >> hash) & 3) - 1) * (y - 1);
            var c2 = g21 + u * (g11 - g21);

            return c1 + v * (c2 - c1);
        }

        
    }

    public sealed class OctaveNoise
    {
        private readonly double _baseAmplitude;
        private readonly double _baseFrequency;
        private readonly ImprovedNoise[] _baseNoise;

        public OctaveNoise(int octaves, Random random, double baseAmplitude, double baseFrequency)
        {
            _baseAmplitude = baseAmplitude;
            _baseFrequency = baseFrequency;
            _baseNoise = new ImprovedNoise[octaves];
            for (int i = 0; i < octaves; i++)
            {
                _baseNoise[i] = new ImprovedNoise(random);
            }
        }

        public double Compute(double x, double y)
        {
            var amplitude = 1.0; //_baseAmplitude;
            var frequency = 1.0; //baseFrequency;

            double sum = 0;
            for (var i = 0; i < _baseNoise.Length; i++)
            {
                sum += _baseNoise[i].Compute(x * frequency, y * frequency) * amplitude;
                amplitude *= .5f;
                frequency *= 2;
            }
            return sum;
        }
    }

}
