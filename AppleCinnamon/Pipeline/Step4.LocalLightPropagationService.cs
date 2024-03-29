﻿using AppleCinnamon.Pipeline.Context;

namespace AppleCinnamon.Pipeline
{
    public sealed class LocalLightPropagationService : TransformChunkPipelineBlock
    {
        public override Chunk Process(Chunk chunk)
        {
            LightingService.LocalPropagate(chunk, chunk.BuildingContext.LightPropagationVoxels);
            return chunk;
        }
    }
}
