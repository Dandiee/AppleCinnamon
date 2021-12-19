using System;
using System.Linq;
using AppleCinnamon.Pipeline.Context;
using SharpDX;

namespace AppleCinnamon.Pipeline
{
    public sealed class LocalFinalizer
    {
        public Chunk Process(Chunk chunk)
        {
            InitializeSunlight(chunk);
            FullScanner.FullScan(chunk);
            LightingService.LocalPropagate(chunk, chunk.BuildingContext.LightPropagationVoxels);

            return chunk;
        }

        private void InitializeSunlight(Chunk chunk)
        {
            for (var i = 0; i != Chunk.SizeXy; i++)
            {
                for (var k = 0; k != Chunk.SizeXy; k++)
                {
                    _ = LightingService.Sunlight(chunk, new Int3(i, chunk.CurrentHeight, k), 15, false).ToList();
                }
            }
        }
    }
}
