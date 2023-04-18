namespace AppleCinnamon.Options;

public static class WorldGeneratorOptions
{
    public const int WATER_LEVEL = 130;

    public static readonly SimplexOptions HighMapNoiseOptions = new(8, 0.2, 0.8, 134, 0.1, 1248);
    public static readonly SimplexOptions RiverNoiseOptions = new(8, 0.4, 1.1, 134, 0.47, 1248);
}