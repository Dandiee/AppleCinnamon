using AppleCinnamon.Pipeline.Context;
using System.Collections.Generic;

namespace AppleCinnamon.Pipeline
{
    public interface IChunkTransformer
    {
        Chunk Transform(Chunk chunk);
    }

    public interface IChunksTransformer
    {
        PipelineBlock Owner { get; set; }
        IEnumerable<Chunk> TransformMany(Chunk chunk);
    }
}