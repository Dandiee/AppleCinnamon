namespace AppleCinnamon.Options;

public static class WorldGeneratorOptions
{
    public const int WATER_LEVEL = 79;
    public const int SNOW_LEVEL = 128;

    public static readonly SimplexOptions HighMapNoiseOptions = new(8, 0.5, 0.8, 134, 0.40, 1248);
    public static readonly SimplexOptions RiverNoiseOptions = new(8, 0.4, 1.1, 134, 0.47, 1248);
}