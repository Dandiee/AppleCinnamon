using AppleCinnamon.ChunkBuilders;

namespace AppleCinnamon
{
    public sealed class GlobalContextBuilder
    {
        public Chunk Transform(Chunk chunk)
        {
            GlobalVisibilityFinalizer.FinalizeGlobalVisibility(chunk);
            GlobalLightFinalizer.FinalizeGlobalLighting(chunk);

            return chunk;
        }
    }
}
