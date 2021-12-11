using System.Diagnostics;

namespace AppleCinnamon.Pipeline
{
    public sealed class LocalLightPropagationService
    {
        public DataflowContext<Chunk> InitializeLocalLight(DataflowContext<Chunk> context)
        {
            var sw = Stopwatch.StartNew();
            var chunk = context.Payload;
            LightingService.LocalPropagate(chunk, chunk.BuildingContext.LightPropagationVoxels);
            sw.Stop();

            return new DataflowContext<Chunk>(context, context.Payload, sw.ElapsedMilliseconds, nameof(LocalLightPropagationService));
        }
        
    }
}
