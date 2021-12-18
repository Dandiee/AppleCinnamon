using System.Linq;
using AppleCinnamon.Pipeline.Context;
using SharpDX;

namespace AppleCinnamon.Pipeline
{
    public sealed class LocalSunlightInitializer : TransformChunkPipelineBlock<Chunk>
    {
        public override Chunk Process(Chunk chunk)
        {
            for (var i = 0; i != Chunk.SizeXy; i++)
            {
                for (var k = 0; k != Chunk.SizeXy; k++)
                {
                    _ = LightingService.Sunlight(chunk, new Int3(i, chunk.CurrentHeight, k), 15, false).ToList();
                }
            }

            return chunk;
        }
    }
}
