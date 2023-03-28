using AppleCinnamon.Pipeline.Context;
using SharpDX.Mathematics.Interop;
using System.Threading;

namespace AppleCinnamon.Pipeline
{
    public sealed class GlobalFinalizer : IChunkTransformer
    {
        public Chunk Transform(Chunk chunk)
        {
            GlobalVisibilityFinalizer.FinalizeGlobalVisibility(chunk);
            GlobalLightFinalizer.FinalizeGlobalLighting(chunk);

            return chunk;
        }
    }
}
