using System;

namespace AppleCinnamon.Options;

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