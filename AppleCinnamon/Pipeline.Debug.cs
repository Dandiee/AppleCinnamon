namespace AppleCinnamon
{
    public sealed partial class Pipeline
    {
        public DebugContext DebugContext { get; private set; }

        public void SetupDebugContext()
        {
            var lines = new DebugLine[]
            {
                new DebugInfoLine<int>(() => ChunkManager.BagOfDeath.Count, "Bag of Death"),
                new DebugInfoLine<int>(() => ChunkManager.Chunks.Count, "All chunks"),
                new DebugInfoLine<int>(() => ChunkManager.Graveyard.Count, "Graveyard"),
                new DebugInfoLine<int>(() => ChunkManager.ChunkCreated),
                new DebugInfoLine<int>(() => ChunkManager.ChunkResurrected),
                new DebugInfoLine<PipelineState>(() => State),
                new DebugInfoLine<double>(() => TerrainStage.TimeSpentInTransform.TotalMilliseconds, TerrainStage.Name, " ms"),
                new DebugInfoLine<double>(() => ArtifactStage.TimeSpentInTransform.TotalMilliseconds, ArtifactStage.Name, " ms"),
                new DebugInfoLine<double>(() => LocalStage.TimeSpentInTransform.TotalMilliseconds, LocalStage.Name, " ms"),
                new DebugInfoLine<double>(() => GlobalStage.TimeSpentInTransform.TotalMilliseconds, GlobalStage.Name, " ms"),
                new DebugInfoLine<double>(() => TimeSpentInTransform.TotalMilliseconds, "Dispatcher", " ms")
            };

            DebugContext = new DebugContext(lines);
        }
    }
}
