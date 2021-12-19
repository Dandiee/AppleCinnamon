using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using System.Windows.Forms.VisualStyles;
using AppleCinnamon.Chunks;
using AppleCinnamon.Helper;
using AppleCinnamon.Pipeline;
using AppleCinnamon.Pipeline.Context;
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

        public ChunkManager(Graphics graphics)
        {
            _chunkDrawer = new ChunkDrawer(graphics.Device);
            Chunks = new ConcurrentDictionary<Int2, Chunk>();
            _queuedChunks = new ConcurrentDictionary<Int2, object>();
            Pipeline = new PipelineProvider(graphics.Device).CreatePipeline(InitialDegreeOfParallelism, Finalize, out _neighborAssignerPipelineBlock);
            _chunkUpdater = new ChunkUpdater(graphics, this);
            
            QueueChunksByIndex(Int2.Zero);
        }



        [SuppressMessage("ReSharper.DPA", "DPA0002: Excessive memory allocations in SOH", MessageId = "type: System.Collections.Generic.KeyValuePair`2[AppleCinnamon.Helper.Int2,AppleCinnamon.Chunk][]")]
        public void Draw(Camera camera)
        {
            var now = DateTime.Now;
            var chunksToDelete = new List<Chunk>();
            var chunksToRender = NeighborAssigner.Chunks.Values.Select(s =>
                {
                    s.IsRendered = false;

                    var distanceX = Math.Abs(camera.CurrentChunkIndex.X - s.ChunkIndex.X);
                    var distanceY = Math.Abs(camera.CurrentChunkIndex.Y - s.ChunkIndex.Y);
                    var maxDistance = Math.Max(distanceX, distanceY);

                    if (maxDistance > Game.ViewDistance + Game.NumberOfPools)
                    {
                        if (s.IsMarkedForDelete)
                        {
                            if ((now - s.MarkedForDeleteAt) > Game.ChunkDespawnCooldown)
                            {
                                chunksToDelete.Add(s);
                            }
                        }
                        else
                        {
                            s.MarkedForDeleteAt = now;
                            s.IsMarkedForDelete = true;
                        }
                    }
                    else if (s.IsMarkedForDelete)
                    {
                        s.IsMarkedForDelete = false;
                    }

                    return s;
                })
                .Where(chunk => chunk.IsReadyToRender && (!Game.IsViewFrustumCullingEnabled || camera.BoundingFrustum.Contains(ref chunk.BoundingBox) != ContainmentType.Disjoint))
                .Select(s =>
                {
                    s.IsRendered = true;
                    return s;
                })
                .ToList();

            if (chunksToDelete.Count > 0)
            {
                foreach (var chunk in chunksToDelete)
                {
                    NeighborAssigner.Chunks.TryRemove(chunk.ChunkIndex, out _);
                    NeighborAssigner.DispatchedChunks.Remove(chunk.ChunkIndex);
                    _queuedChunks.TryRemove(chunk.ChunkIndex, out _);
                    Chunks.TryRemove(chunk.ChunkIndex, out _);
                    chunk.DereferenceNeighbors();
                    chunk.ShouldBeDeadByNow = true;
                }
            }

            _chunkDrawer.Draw(chunksToRender, camera);
        }

        private void Finalize(Chunk chunk)
        {
            chunk.IsReadyToRender = true;
            Chunks.TryAdd(chunk.ChunkIndex, chunk);
            _queuedChunks.TryRemove(chunk.ChunkIndex, out _);

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
                var currentChunkIndex = new Int2((int)camera.Position.X / Chunk.SizeXy, (int)camera.Position.Z / Chunk.SizeXy);
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

                    if (!Chunks.ContainsKey(chunkIndex) && _queuedChunks.ContainsKey(chunkIndex))
                    {

                    }

                    if (Chunks.ContainsKey(chunkIndex) && !_queuedChunks.ContainsKey(chunkIndex))
                    {

                    }

                    if (!_queuedChunks.ContainsKey(chunkIndex) && !Chunks.ContainsKey(chunkIndex))
                    {
                        //Debug.WriteLine($"Chunk queued: ({chunkIndex})");
                        _queuedChunks.TryAdd(chunkIndex, null);
                        Pipeline.TransformBlock.Post(chunkIndex);
                    }
                }

                _lastQueueIndex = currentChunkIndex;
            }
        }

    }
}
