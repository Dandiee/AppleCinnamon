using SharpDX;

namespace AppleCinnamon.Settings
{
    public static class WorldSettings
    {
        public static Vector3 Gravity = new Vector3(0, -40f, 0);
        public static readonly Vector3 PlayerSize = new Vector3(.5f, 1.8f, .5f);
        public static readonly float EyeHeight = 1.7f;
        public static readonly Vector3 PlayerMin;
        public static readonly Vector3 PlayerMax;

        static WorldSettings()
        {
            PlayerMin = new Vector3(PlayerSize.X / -2, -EyeHeight, PlayerSize.Z / -2);
            PlayerMax = new Vector3(PlayerSize.X / 2, PlayerSize.Y - EyeHeight, PlayerSize.Z / 2);
        }
    }
}
