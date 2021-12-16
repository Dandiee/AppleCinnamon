using System;

namespace AppleCinnamon
{
    public static class Rnd
    {
        private static readonly Random Random = new(3216);

        public static int Next()
        {
                return Random.Next();
        }

        public static int Next(int inclusiveFrom, int exclusiveTo)
        {
                return Random.Next(inclusiveFrom, exclusiveTo);
        }
    }
}
