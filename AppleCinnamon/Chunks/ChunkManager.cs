using System;
using System.Collections.Concurrent;
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

        public readonly ConcurrentDictionary<Int2, Chunk> Chunks;
        private readonly ConcurrentDictionary<Int2, object> _queuedChunks;
        private readonly ChunkUpdater _chunkUpdater;
        private readonly ChunkDrawer _chunkDrawer;
        private Int2? _lastQueueIndex;
        public readonly TransformPipelineBlock<Int2, Chunk> Pipeline;
        public readonly NeighborAssigner _neighborAssignerPipelineBlock;
        private List<Chunk> _chunksToDraw;

        public ChunkManager(Graphics graphics)
        {
            _chunkDrawer = new ChunkDrawer(graphics.Device);
            Chunks = new ConcurrentDictionary<Int2, Chunk>();
            _chunksToDraw = new List<Chunk>();
            _queuedChunks = new ConcurrentDictionary<Int2, object>();
            Pipeline = new PipelineProvider(graphics.Device).CreatePipeline(InitialDegreeOfParallelism, this, out _neighborAssignerPipelineBlock);
            _chunkUpdater = new ChunkUpdater(graphics, this);

            QueueChunksByIndex(Int2.Zero);
        }

        [SuppressMessage("ReSharper.DPA", "DPA0002: Excessive memory allocations in SOH", MessageId = "type: System.Collections.Generic.KeyValuePair`2[AppleCinnamon.Helper.Int2,AppleCinnamon.Chunk][]")]
        public void Draw(Camera camera)
        {
            var now = DateTime.Now;
            _chunkDrawer.Draw(_chunksToDraw, camera);
        }

        private void KillChunk(Chunk chunk)
        {
            if (chunk.IsFinalized)
            {
                Chunks.Remove(chunk.ChunkIndex, out _);
            }
            else
            {
                _queuedChunks.TryRemove(chunk.ChunkIndex, out _);
            }

            NeighborAssigner.Chunks.TryRemove(chunk.ChunkIndex, out _);
            NeighborAssigner.DispatchedChunks.Remove(chunk.ChunkIndex);
        }

        public void Finalize(Chunk chunk)
        {
            _queuedChunks.TryRemove(chunk.ChunkIndex, out _);
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

                UpdateChunks(camera);
                QueueChunksByIndex(currentChunkIndex);
            }
        }

        private void UpdateChunks(Camera camera)
        {
            var now = DateTime.Now;
            _chunksToDraw.Clear();
            foreach (var chunk in NeighborAssigner.Chunks.Values)
            {
                chunk.IsRendered = false;

                if (!chunk.CheckForValidity(camera, now))
                {
                    KillChunk(chunk);
                }

                chunk.IsRendered = chunk.IsFinalized && (!Game.IsViewFrustumCullingEnabled ||
                                                 camera.BoundingFrustum.Contains(ref chunk.BoundingBox) !=
                                                 ContainmentType.Disjoint);
                if (chunk.IsRendered)
                {
                    _chunksToDraw.Add(chunk);
                }
            }
        }

        private void QueueChunksByIndex(Int2 currentChunkIndex)
        {
            if (!_lastQueueIndex.HasValue || _lastQueueIndex != currentChunkIndex)
            {
                foreach (var relativeChunkIndex in GetSurroundingChunks(Game.ViewDistance + Game.NumberOfPools - 1))
                {
                    var chunkIndex = currentChunkIndex + relativeChunkIndex;
                    if (!_queuedChunks.ContainsKey(chunkIndex) && !Chunks.ContainsKey(chunkIndex))
                    {
                        _queuedChunks.TryAdd(chunkIndex, null);
                        Pipeline.TransformBlock.Post(chunkIndex);
                    }
                }

                _lastQueueIndex = currentChunkIndex;
            }
        }

    }
}
