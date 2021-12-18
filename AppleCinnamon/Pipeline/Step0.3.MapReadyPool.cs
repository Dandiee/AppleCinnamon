using AppleCinnamon.Pipeline.Context;
using SharpDX.Mathematics.Interop;

namespace AppleCinnamon.Pipeline
{
    public sealed class MapReadyPool : ChunkPoolPipelineBlock
    {
        private static readonly RawColor4 Color = new(0, 1, 1, 1);
        public override RawColor4 DebugColor => Color;
    }
}
