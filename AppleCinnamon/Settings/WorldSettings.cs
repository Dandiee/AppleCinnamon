using System;
using SharpDX;

namespace AppleCinnamon.Settings
{
    public static class WorldSettings
    {
        public static Vector3 Gravity = new(0, -40f, 0);
        public static readonly Vector3 PlayerSize = new(.5f, 1.8f, .5f);
        public static readonly float EyeHeight = 1.7f;
        public static readonly Vector3 PlayerMin;
        public static readonly Vector3 PlayerMax;
        public const int WaterLevel = 130;

        public static readonly SimplexOptions HighMapNoiseOptions = new(8, 0.2, 0.8, 134, 0.1, 1248);
        public static readonly SimplexOptions RiverNoiseOptions = new(8, 0.4, 1.1, 134, 0.47, 1248);

        static WorldSettings()
        {
            PlayerMin = new Vector3(PlayerSize.X / -2, -EyeHeight, PlayerSize.Z / -2);
            PlayerMax = new Vector3(PlayerSize.X / 2, PlayerSize.Y - EyeHeight, PlayerSize.Z / 2);
        }
    }


    public readonly struct SimplexOptions
    {
        public readonly int Seed;
        public readonly int Octaves;
        public readonly double Frequency;
        public readonly double Amplitude;
        public readonly int Offset;
        public readonly double Factor;
        public readonly Random Random;

        public SimplexOptions(int octaves, double frequency, double amplitude, int offset, double factor, int seed)
        {
            Seed = seed;
            Octaves = octaves;
            Frequency = frequency;
            Amplitude = amplitude;
            Offset = offset;
            Factor = factor;
            Seed = seed;
            Random = new Random(seed);
        }
    }
}
