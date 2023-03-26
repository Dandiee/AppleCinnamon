using AppleCinnamon.Pipeline.Context;
using SharpDX.Mathematics.Interop;
using System.Threading;

namespace AppleCinnamon.Pipeline
{
    public sealed class GlobalFinalizer : IChunkTransformer
    {
        public Chunk Transform(Chunk chunk)
        {
            // if (Game.Debug) Thread.Sleep(100);
            GlobalVisibilityFinalizer.FinalizeGlobalVisibility(chunk);
            GlobalLightFinalizer.FinalizeGlobalLighting(chunk);

            return chunk;
        }
    }
}
