using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using AppleCinnamon.Chunks;
using AppleCinnamon.Helper;
using AppleCinnamon.Pipeline;
using SharpDX;

namespace AppleCinnamon
{
    public sealed partial class ChunkManager
    {
        public static readonly int InitialDegreeOfParallelism = 1; //Environment.ProcessorCount / 2;
        private int _finalizedChunks;
        public bool IsInitialized { get; private set; }


        public readonly ConcurrentDictionary<Int2, Chunk> Chunks;
        private readonly ConcurrentDictionary<Int2, object> _queuedChunks;
        private readonly ChunkUpdater _chunkUpdater;
        private readonly ChunkDrawer _chunkDrawer;
        private Int2? _lastQueueIndex;
        private readonly TransformBlock<Int2, Chunk> _pipeline;

        public ChunkManager(Graphics graphics)
        {
            _chunkDrawer = new ChunkDrawer(graphics.Device);
            Chunks = new ConcurrentDictionary<Int2, Chunk>();
            _queuedChunks = new ConcurrentDictionary<Int2, object>();
            _pipeline = new PipelineProvider(graphics.Device).CreatePipeline(InitialDegreeOfParallelism, Finalize);
            _chunkUpdater = new ChunkUpdater(graphics, this);
            
            QueueChunksByIndex(Int2.Zero);
        }

        public void Draw(Camera camera)
        {
            var chunksToRender = Chunks
                .Where(chunk => !Game.IsViewFrustumCullingEnabled || camera.BoundingFrustum.Contains(ref chunk.Value.BoundingBox) != ContainmentType.Disjoint)
                .ToList();

            _chunkDrawer.Draw(chunksToRender, camera);
        }

        private void Finalize(Chunk chunk)
        {
            Chunks.TryAdd(chunk.ChunkIndex, chunk);
            _queuedChunks.TryRemove(chunk.ChunkIndex, out _);

            Interlocked.Increment(ref _finalizedChunks);
            const int root = (Game.ViewDistance - 1) * 2;

            if (!IsInitialized && _finalizedChunks == root)
            {
                IsInitialized = true;
            }
        }

  
        public void Update(Camera camera)
        {
            if (IsInitialized)
            {
                _chunkDrawer.Update(camera);
                var currentChunkIndex = new Int2((int)camera.Position.X / Chunk.SizeXy, (int)camera.Position.Z / Chunk.SizeXy);
                QueueChunksByIndex(currentChunkIndex);
            }
        }

        private void QueueChunksByIndex(Int2 currentChunkIndex)
        {
            if (!_lastQueueIndex.HasValue || _lastQueueIndex != currentChunkIndex)
            {
                foreach (var relativeChunkIndex in GetSurroundingChunks(Game.ViewDistance))
                {
                    var chunkIndex = currentChunkIndex + relativeChunkIndex;
                    if (!_queuedChunks.ContainsKey(chunkIndex) && !Chunks.ContainsKey(chunkIndex))
                    {
                        _queuedChunks.TryAdd(chunkIndex, null);
                        _pipeline.Post(chunkIndex);
                    }
                }

                _lastQueueIndex = currentChunkIndex;
            }
        }

    }
}
