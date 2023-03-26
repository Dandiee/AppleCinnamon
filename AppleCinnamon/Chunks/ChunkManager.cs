using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using System.Windows.Forms;
using AppleCinnamon.Chunks;
using AppleCinnamon.Helper;
using AppleCinnamon.Pipeline;
using AppleCinnamon.Pipeline.Context;
using AppleCinnamon.Settings;
using SharpDX;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;

namespace AppleCinnamon
{
    public sealed partial class ChunkManager
    {
        public static readonly int InitialDegreeOfParallelism = 2;//Environment.ProcessorCount;
        private int _finalizedChunks;
        public bool IsInitialized { get; private set; }

        public static readonly ConcurrentDictionary<Int2, Chunk> Chunks = new();
        private readonly ChunkUpdater _chunkUpdater;
        private readonly ChunkDrawer _chunkDrawer;
        private Int2? _lastQueueIndex;
        public readonly TransformPipelineBlock<Chunk, Chunk> Pipeline;
        public readonly NeighborAssigner _neighborAssignerPipelineBlock;
        private List<Chunk> _chunksToDraw;

        public ChunkManager(Graphics graphics)
        {
            _chunkDrawer = new ChunkDrawer(graphics.Device);
            _chunksToDraw = new List<Chunk>();
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

        public void Finalize(Chunk chunk)
        {
            chunk.IsFinalized = true;

            Interlocked.Increment(ref _finalizedChunks);
            const int root = (Game.ViewDistance - 1) * 2;

            if (!IsInitialized && _finalizedChunks == root)
            {
                IsInitialized = true;
            }

            Interlocked.Decrement(ref InProcessChunks);
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

        public static ManualResetEvent WaitForDeletionEvent = new(true);
        public static readonly ConcurrentDictionary<Int2, Chunk> ChunksToDelete = new();
        public static readonly ConcurrentBag<Chunk> Cemetery = new(); 
        public static int InProcessChunks = 0;
        public static int CreatedChunkInstances = 0;
        public static int ChunksResurrected = 0;

        private void UpdateChunks(Camera camera)
        {
            var now = DateTime.Now;
            _chunksToDraw.Clear();
            foreach (var chunk in Chunks.Values)
            {
                chunk.IsRendered = false;

                if (!chunk.CheckForValidity(camera, now))
                {
                    // Chunks.Remove(chunk.ChunkIndex, out _);
                    chunk.IsTimeToDie = true;
                    ChunksToDelete.TryAdd(chunk.ChunkIndex, chunk);
                }

                chunk.IsRendered = !chunk.IsTimeToDie && chunk.IsFinalized && (!Game.IsViewFrustumCullingEnabled ||
                                                 camera.BoundingFrustum.Contains(ref chunk.BoundingBox) !=
                                                 ContainmentType.Disjoint);
                if (chunk.IsRendered)
                {
                    _chunksToDraw.Add(chunk);
                }
            }

            if (ChunksToDelete.Count > 30)
            {
                if (InProcessChunks == 0)
                {
                    WaitForDeletionEvent.Reset();
                    CleanUpChunkToDieQueue();
                }
            }
        }

        private void CleanUpChunkToDieQueue()
        {
            foreach (var chunk in ChunksToDelete)
            {
                chunk.Value.Kill();
                Chunks.Remove(chunk.Key, out var _);
                Cemetery.Add(chunk.Value);
            }

            ChunksToDelete.Clear();
            WaitForDeletionEvent.Set();
        }

        public Chunk CreateChunk(Int2 chunkIndex)
        {
            if (Cemetery.Count == 0)
            {
                CreatedChunkInstances++;
                return new Chunk(chunkIndex);
            }

            if (Cemetery.TryTake(out var deadChunk))
            {
                ChunksResurrected++;
                return deadChunk.Resurrect(chunkIndex);
            }
            else
            {
                CreatedChunkInstances++;
                return new Chunk(chunkIndex);
            }
        }

        private void QueueChunksByIndex(Int2 currentChunkIndex)
        {
            if (!_lastQueueIndex.HasValue || _lastQueueIndex != currentChunkIndex)
            {
                foreach (var relativeChunkIndex in GetSurroundingChunks(Game.ViewDistance + Game.NumberOfPools - 1))
                {
                    var chunkIndex = currentChunkIndex + relativeChunkIndex;

                    if (!Chunks.ContainsKey(chunkIndex))
                    {
                        var newChunk = CreateChunk(chunkIndex);// new Chunk(chunkIndex);
                        Chunks.TryAdd(chunkIndex, newChunk);
                        Pipeline.TransformBlock.Post(newChunk);
                    }
                }

                _lastQueueIndex = currentChunkIndex;
            }
        }

    }
}
