namespace AppleCinnamon
{
    public sealed class GlobalFinalizer
    {
        public Chunk Transform(Chunk chunk)
        {
            GlobalVisibilityFinalizer.FinalizeGlobalVisibility(chunk);
            GlobalLightFinalizer.FinalizeGlobalLighting(chunk);

            return chunk;
        }
    }
}
