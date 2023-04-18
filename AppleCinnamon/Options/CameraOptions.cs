using System;
using SharpDX;

namespace AppleCinnamon.Options;

public static class CameraOptions
{
    public static float FieldOfView = MathUtil.Pi / 2f;
    public static Vector3 Gravity = new(0, -40f, 0);
    public static readonly TimeSpan BuildCooldown = TimeSpan.FromMilliseconds(100);
    public static readonly Vector3 PlayerSize = new(.5f, 1.8f, .5f);
    public static readonly float EyeHeight = 1.7f;
    public static readonly Vector3 PlayerMin = new(PlayerSize.X / -2, -EyeHeight, PlayerSize.Z / -2);
    public static readonly Vector3 PlayerMax= new(PlayerSize.X / 2, PlayerSize.Y - EyeHeight, PlayerSize.Z / 2);
}