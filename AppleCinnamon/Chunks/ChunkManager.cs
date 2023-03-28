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
using SharpDX.Direct3D11;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;

namespace AppleCinnamon
{
    public sealed partial class ChunkManager
    {
        private readonly Graphics _graphics;
        public static readonly int InitialDegreeOfParallelism = 2;//Environment.ProcessorCount;
        private int _finalizedChunks;
        public bool IsInitialized { get; private set; }

        public static readonly ConcurrentDictionary<Int2, Chunk> Chunks = new();
        private readonly ChunkUpdater _chunkUpdater;
        private readonly ChunkDrawer _chunkDrawer;
        private Int2? _lastQueueIndex;
        public readonly PipelineBlock Pipeline;
        private List<Chunk> _chunksToDraw;

        public ChunkManager(Graphics graphics)
        {
            _graphics = graphics;
            _chunkDrawer = new ChunkDrawer(graphics.Device);
            _chunksToDraw = new List<Chunk>();
            Pipeline = new PipelineProvider(graphics.Device).CreatePipeline(InitialDegreeOfParallelism, this);
            _chunkUpdater = new ChunkUpdater(graphics, this);

            QueueChunksByIndex(Int2.Zero);
        }

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


        public void Update(Camera camera, World world, Device device)
        {
            if (IsInitialized)
            {
                _chunkDrawer.Update(camera, world);
                var currentChunkIndex = new Int2((int)camera.Position.X / WorldSettings.ChunkSize, (int)camera.Position.Z / WorldSettings.ChunkSize);

                UpdateChunks(camera, device);
                QueueChunksByIndex(currentChunkIndex);
            }
        }

        public static ManualResetEvent WaitForDeletionEvent = new(true);
        public static readonly ConcurrentDictionary<Int2, Chunk> BagOfDeath = new();
        public static readonly ConcurrentBag<Chunk> Graveyard = new(); 
        public static volatile int InProcessChunks = 0;
        public static int CreatedChunkInstances = 0;
        public static int ChunksResurrected = 0;

        private void UpdateChunks(Camera camera, Device device)
        {
            var now = DateTime.Now;
            _chunksToDraw.Clear();
            foreach (var chunk in Chunks.Values)
            {
                chunk.IsRendered = false;

                if (!chunk.CheckForValidity(camera, now))
                {
                    chunk.IsTimeToDie = true;
                    BagOfDeath.TryAdd(chunk.ChunkIndex, chunk);
                }

                chunk.IsRendered = !chunk.IsTimeToDie && chunk.IsFinalized && (!Game.IsViewFrustumCullingEnabled ||
                                                 camera.BoundingFrustum.Contains(ref chunk.BoundingBox) !=
                                                 ContainmentType.Disjoint);
                if (chunk.IsRendered)
                {
                    _chunksToDraw.Add(chunk);
                }
            }

            if (BagOfDeath.Count > Game.ViewDistance * 2) // we have victims
            {
                if (InProcessChunks == 0) // its the good time for massacre
                {
                    WaitForDeletionEvent.Reset(); // suspend all pipeline process
                    Massacre();
                    device.ImmediateContext.Flush();
                    WaitForDeletionEvent.Set(); // let em go
                }
            }
        }

        private void Massacre()
        {
            foreach (var chunk in BagOfDeath)
            {
                chunk.Value.Kill(_graphics.Device);
                Chunks.Remove(chunk.Key, out var _);
                Graveyard.Add(chunk.Value);
            }

            BagOfDeath.Clear();
        }

        public Chunk CreateChunk(Int2 chunkIndex)
        {
            if (Graveyard.Count == 0)
            {
                CreatedChunkInstances++;
                return new Chunk(chunkIndex);
            }

            if (Graveyard.TryTake(out var deadChunk))
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
