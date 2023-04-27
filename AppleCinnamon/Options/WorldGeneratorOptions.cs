namespace AppleCinnamon.Options;

public static class WorldGeneratorOptions
{
    public const double WATER_LEVEL_VALUE = 0.35f;
    public const int WATER_LEVEL = (byte)(WATER_LEVEL_VALUE * 255);
    public const int SNOW_LEVEL = WorldGeneratorOptions.WATER_LEVEL + 90;

    public static readonly SimplexOptions HighMapNoiseOptions = new(8, 0.5, 0.8, 134, 0.40, 1248);
    public static readonly SimplexOptions RiverNoiseOptions = new(8, 0.4, 1.1, 134, 0.47, 1248);
}