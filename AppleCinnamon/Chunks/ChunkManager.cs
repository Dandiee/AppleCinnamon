using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using AppleCinnamon.Chunks;
using AppleCinnamon.Helper;
using AppleCinnamon.Pipeline;
using AppleCinnamon.Pipeline.Context;
using AppleCinnamon.Settings;
using SharpDX;

namespace AppleCinnamon
{
    public sealed partial class ChunkManager
    {
        public static readonly int InitialDegreeOfParallelism = 2;//Environment.ProcessorCount;
        private int _finalizedChunks;
        public bool IsInitialized { get; private set; }

        public readonly Dictionary<Int2, Chunk> Chunks;
        private readonly HashSet<Int2> _queuedChunks;
        private readonly ChunkUpdater _chunkUpdater;
        private readonly ChunkDrawer _chunkDrawer;
        private Int2? _lastQueueIndex;
        public readonly TransformPipelineBlock<Int2, Chunk> Pipeline;
        public readonly NeighborAssigner _neighborAssignerPipelineBlock;

        public ChunkManager(Graphics graphics)
        {
            _chunkDrawer = new ChunkDrawer(graphics.Device);
            Chunks = new Dictionary<Int2, Chunk>();
            _queuedChunks = new HashSet<Int2>();
            Pipeline = new PipelineProvider(graphics.Device).CreatePipeline(InitialDegreeOfParallelism, this, out _neighborAssignerPipelineBlock);
            _chunkUpdater = new ChunkUpdater(graphics, this);

            QueueChunksByIndex(Int2.Zero);
        }

        [SuppressMessage("ReSharper.DPA", "DPA0002: Excessive memory allocations in SOH", MessageId = "type: System.Collections.Generic.KeyValuePair`2[AppleCinnamon.Helper.Int2,AppleCinnamon.Chunk][]")]
        public void Draw(Camera camera)
        {
            var now = DateTime.Now;
            var chunksToRender = NeighborAssigner.Chunks.Values.Select(s =>
                {
                    s.IsRendered = false;

                    if (!s.CheckForValidity(camera, now))
                    {
                        KillChunk(s);
                    }

                    return s;
                })
                .Where(chunk => chunk.IsFinalized && (!Game.IsViewFrustumCullingEnabled || camera.BoundingFrustum.Contains(ref chunk.BoundingBox) != ContainmentType.Disjoint))
                .Select(s =>
                {
                    s.IsRendered = true;
                    return s;
                })
                .ToList();

            _chunkDrawer.Draw(chunksToRender, camera);
        }

        private void KillChunk(Chunk chunk)
        {
            if (chunk.IsFinalized)
            {
                Chunks.Remove(chunk.ChunkIndex, out _);
            }
            else
            {
                _queuedChunks.Remove(chunk.ChunkIndex);
            }

            NeighborAssigner.Chunks.TryRemove(chunk.ChunkIndex, out _);
            NeighborAssigner.DispatchedChunks.Remove(chunk.ChunkIndex);
        }

        public void Finalize(Chunk chunk)
        {
            _queuedChunks.Remove(chunk.ChunkIndex);
            Chunks.TryAdd(chunk.ChunkIndex, chunk);
            
            chunk.IsFinalized = true;

            Interlocked.Increment(ref _finalizedChunks);
            const int root = (Game.ViewDistance - 1) * 2;

            if (!IsInitialized && _finalizedChunks == root)
            {
                IsInitialized = true;
            }
        }


        public void Update(Camera camera, World world)
        {
            if (IsInitialized)
            {
                _chunkDrawer.Update(camera, world);
                var currentChunkIndex = new Int2((int)camera.Position.X / WorldSettings.ChunkSize, (int)camera.Position.Z / WorldSettings.ChunkSize);
                QueueChunksByIndex(currentChunkIndex);
            }
        }

        private void QueueChunksByIndex(Int2 currentChunkIndex)
        {
            if (!_lastQueueIndex.HasValue || _lastQueueIndex != currentChunkIndex)
            {
                foreach (var relativeChunkIndex in GetSurroundingChunks(Game.ViewDistance + Game.NumberOfPools - 1))
                {
                    var chunkIndex = currentChunkIndex + relativeChunkIndex;
                    if (!_queuedChunks.Contains(chunkIndex) && !Chunks.ContainsKey(chunkIndex))
                    {
                        _queuedChunks.Add(chunkIndex);
                        Pipeline.TransformBlock.Post(chunkIndex);
                    }
                }

                _lastQueueIndex = currentChunkIndex;
            }
        }

    }
}
