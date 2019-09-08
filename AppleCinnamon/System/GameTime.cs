using System;

namespace AppleCinnamon.System
{
    public class GameTime
    {
        public TimeSpan ElapsedGameTime { get; set; }
        public TimeSpan TotalGameTime { get; set; }

        public GameTime()
        {
            ElapsedGameTime = new TimeSpan(0);
            TotalGameTime = new TimeSpan(0);
        }
    }
}
