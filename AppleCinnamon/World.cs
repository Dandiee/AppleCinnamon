namespace AppleCinnamon
{
    public sealed class World
    {
        public float Time { get; private set; }

        private const float TimeStep = 0.002f;

        public void IncreaseTime()
        {
            var overflow = 1.0f - (Time + TimeStep);
            if (overflow < 0)
            {
                Time = -1.0f - overflow;
            }
            else
            {
                Time += TimeStep;
            }
        }

        public void DecreaseTime()
        {
            var overflow = 1.0f + (Time + TimeStep);
            if (overflow < 0)
            {
                Time = 1.0f + overflow;
            }
            else
            {
                Time -= TimeStep;
            }
        }

        public void Update()
        {
            Time += 0.001f;
        }
    }
}