using System.Linq;
using AppleCinnamon.ChunkBuilders;
using AppleCinnamon.Settings;
using SharpDX;

namespace AppleCinnamon
{
    public sealed class LocalContextBuilder
    {
        public Chunk Transform(Chunk chunk)
        {
            InitializeSunlight(chunk);
            FullScanner.FullScan(chunk);
            LightingService.LocalPropagate(chunk, chunk.BuildingContext.LightPropagationVoxels);

            return chunk;
        }

        private void InitializeSunlight(Chunk chunk)
        {
            for (var i = 0; i != WorldSettings.ChunkSize; i++)
            {
                for (var k = 0; k != WorldSettings.ChunkSize; k++)
                {
                    _ = LightingService.Sunlight(chunk, new Int3(i, chunk.CurrentHeight, k), 15, false).ToList();
                }
            }
        }
    }
}
