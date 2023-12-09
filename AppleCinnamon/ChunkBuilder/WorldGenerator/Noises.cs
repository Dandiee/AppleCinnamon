﻿using AppleCinnamon.Options;

namespace AppleCinnamon.ChunkBuilder.WorldGenerator
{
    internal class Noises
    {
        public static readonly DaniNoise Continent 
            = new(new (9, 0.43957075, 0.22988401, -0.09889114, 0.02650103, 2150));

        public static readonly DaniNoise Erosion
            = new(new(10, 0.2595467, 0.08986536, -0.16380763, 0.07992246, 1833));

        public static readonly DaniNoise Peaks
            = new(new(9, 1.9397707, 0.039858673, 0.063578725, 0.09921174, 1308));
    }
}