using AppleCinnamon.Pipeline.Context;
using SharpDX.Mathematics.Interop;

namespace AppleCinnamon.Pipeline
{
    public sealed class GlobalFinalizer
    {
        public Chunk Process(Chunk chunk)
        {
            GlobalVisibilityFinalizer.FinalizeGlobalVisibility(chunk);
            GlobalLightFinalizer.FinalizeGlobalLighting(chunk);

            return chunk;
        }
    }
}
