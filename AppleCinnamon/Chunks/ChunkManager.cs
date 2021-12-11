using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using AppleCinnamon.Chunks;
using AppleCinnamon.Helper;
using AppleCinnamon.Pipeline;
using SharpDX;

namespace AppleCinnamon
{
    public sealed class ChunkManager
    {
        public static readonly int InitialDegreeOfParallelism = 1; //Environment.ProcessorCount / 2;
        private int _finalizedChunks;
        public bool IsInitialized { get; private set; }


        public readonly ConcurrentDictionary<Int2, Chunk> Chunks;
        private readonly ConcurrentDictionary<Int2, object> _queuedChunks;
        private readonly ChunkUpdater _chunkUpdater;
        private readonly PipelineProvider _pipelineProvider;
        private readonly ChunkDrawer _chunkDrawer;
        private Int2? _lastQueueIndex;
        private TransformBlock<Int2, Chunk> _pipeline;

        public ChunkManager(Graphics graphics)
        {
            _pipelineProvider = new PipelineProvider(graphics.Device);
            _chunkDrawer = new ChunkDrawer(graphics.Device);
            Chunks = new ConcurrentDictionary<Int2, Chunk>();
            _queuedChunks = new ConcurrentDictionary<Int2, object>();
            _pipeline = _pipelineProvider.CreatePipeline(InitialDegreeOfParallelism, Finalize);
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

        public Voxel? GetVoxel(Int3 absoluteIndex)
        {
            var address = Chunk.GetVoxelAddress(absoluteIndex);
            if (address == null)
            {
                return null;
            }

            if (!Chunks.TryGetValue(address.Value.ChunkIndex, out var chunk))
            {
                return null;
            }

            return chunk.CurrentHeight <= address.Value.RelativeVoxelIndex.Y
                ? Voxel.Air
                : chunk.GetVoxel(address.Value.RelativeVoxelIndex.ToFlatIndex(chunk.CurrentHeight));
        }

        public bool TryGetChunk(Int2 chunkIndex, out Chunk chunk)
        {
            if (Chunks.TryGetValue(chunkIndex, out var currentChunk))
            {
                chunk = currentChunk;
                return true;
            }


            chunk = null;
            return false;
        }

        private void Finalize(Chunk chunk)
        {
            Chunks.TryAdd(chunk.ChunkIndex, chunk);
            _queuedChunks.TryRemove(chunk.ChunkIndex, out _);

            Interlocked.Increment(ref _finalizedChunks);
            var root = (Game.ViewDistance - 1) * 2;

            if (!IsInitialized && _finalizedChunks == root)
            {
                IsInitialized = true;
                // _pipeline.Complete();
                // _pipeline = _pipelineProvider.CreatePipeline(1, Finalize);
            }
        }

  
        private static readonly IReadOnlyCollection<Int2> Directions = new[]
        {
            new Int2(1, 0),
            new Int2(0, 1),
            new Int2(-1, 0),
            new Int2(0, -1)
        };


        public static IEnumerable<Int2> GetSurroundingChunks(int size)
        {
            yield return new Int2();

            for (var i = 1; i < size + 2; i++)
            {
                var cursor = new Int2(i * -1);

                foreach (var direction in Directions)
                {
                    for (var j = 1; j < i * 2 + 1; j++)
                    {
                        cursor = cursor + direction;
                        yield return cursor;
                    }
                }
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

        public void SetBlock(Int3 absoluteIndex, byte voxel) => _chunkUpdater.SetVoxel(absoluteIndex, voxel);
    }
}
