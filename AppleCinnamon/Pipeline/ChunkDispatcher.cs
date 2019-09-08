using System.Diagnostics;

namespace AppleCinnamon.Pipeline
{
    public interface IChunkDispatcher
    {
        DataflowContext<Chunk> Dispatch(DataflowContext<Chunk> context);
    }

    public sealed class ChunkDispatcher : IChunkDispatcher
    {
        private readonly IChunkBuilder _chunkBuilder;

        public ChunkDispatcher()
        {
            _chunkBuilder = new ChunkBuilder();
        }

        public DataflowContext<Chunk> Dispatch(DataflowContext<Chunk> context)
        {
            var sw = Stopwatch.StartNew();
            _chunkBuilder.BuildChunk(context.Device, context.Payload);

            context.Payload.State = ChunkState.Displayed;
            sw.Stop();

            //context.Debug.Add(nameof(ChunkDispatcher), sw.ElapsedMilliseconds);
            return new DataflowContext<Chunk>(context, context.Payload, sw.ElapsedMilliseconds, nameof(ChunkDispatcher));
        }
    }
}
