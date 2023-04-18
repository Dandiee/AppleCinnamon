namespace AppleCinnamon.Options;

public static class SkyDomeOptions
{
    public static float SunIntensity = 1.0f;
    public static float Turbitity = 1.0f;
    public static float InscatteringMultiplier = 1.0f;
    public static float BetaRayMultiplier = 8.0f;
    public static float BetaMieMultiplier = 0.00005f;
    public static float TimeOfDay = 0.5f;
    public static int Resolution = 64;
    public static float Radius = 100;

    public static void IncrementTime(float step)
    {
        var overflow = 1.0f - (TimeOfDay + step);
        if (overflow < 0)
        {
            TimeOfDay = -1.0f - overflow;
        }
        else
        {
            TimeOfDay += step;
        }
    }

    public static void DecrementTime(float step)
    {
        var overflow = 1.0f + (TimeOfDay + step);
        if (overflow < 0)
        {
            TimeOfDay = 1.0f + overflow;
        }
        else
        {
            TimeOfDay -= step;
        }
    }
}