using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AppleCinnamon.Chunks;
using AppleCinnamon.Helper;
using AppleCinnamon.Settings;
using SharpDX;
using SharpDX.Direct3D11;

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
        public readonly Pipeline Pipeline;
        private List<Chunk> _chunksToDraw;
        public static int NumberOfChanges = 0;

        public ChunkManager(Graphics graphics)
        {
            _graphics = graphics;
            _chunkDrawer = new ChunkDrawer(graphics.Device);
            _chunksToDraw = new List<Chunk>();
            Pipeline = new Pipeline(this);
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
            if (chunk.State == ChunkState.Finished)
            {

            }

            chunk.IsFinalized = true;

            Interlocked.Increment(ref _finalizedChunks);
            const int root = (Game.ViewDistance - 1) * 2;

            if (!IsInitialized && _finalizedChunks == root)
            {
                IsInitialized = true;
            }

            chunk.IsSinking = false;
            chunk.History.Add("Finalized");
            chunk.State = ChunkState.Finished;
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

        public static readonly ConcurrentDictionary<Int2, Chunk> BagOfDeath = new();
        public static readonly ConcurrentBag<Chunk> Graveyard = new();
        public static volatile int InProcessChunks = 0;
        //public static volatile int AnotherChunkCounter = 0;
        public static int CreatedChunkInstances = 0;
        public static int ChunksResurrected = 0;
        public static int Cleanups = 0;

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

            //if (BagOfDeath.Count > Game.ViewDistance * 2) // we have victims
            //{
            //    if (InProcessChunks == 0) // its the good time for massacre
            //    {
            //        WaitForDeletionEvent.Reset(); // suspend all pipeline process
            //        Massacre();
            //        device.ImmediateContext.Flush();
            //        WaitForDeletionEvent.Set(); // let em go
            //    }
            //}

            
        }

        public void CleanUp(Device device)
        {
            if (!isSuspended && Pipeline.State == PipelineState.Running)
            {
                if (BagOfDeath.Count > Game.ViewDistance * 2)
                {
                    Pipeline.Suspend(() =>
                    {
                        isSuspended = true;
                    });
                }
            }

            if (isSuspended)
            {
                Massacre(device);
            }
        }

        public bool isSuspended = false;

        private void Massacre(Device  device)
        {
            foreach (var chunk in BagOfDeath)
            {
                if (!Chunks.Remove(chunk.Key, out var _))
                {

                }

                chunk.Value.Kill(_graphics.Device);
                Graveyard.Add(chunk.Value);
                Pipeline.RemoveItem(chunk.Key);
            }

            BagOfDeath.Clear();
            device.ImmediateContext.Flush();
            Pipeline.Resume();
            isSuspended = false;
            
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
                if (Pipeline.State == PipelineState.Running)
                {
                    foreach (var relativeChunkIndex in GetSurroundingChunks(Game.ViewDistance + Game.NumberOfPools - 1))
                    {
                        var chunkIndex = currentChunkIndex + relativeChunkIndex;

                        if (!Chunks.ContainsKey(chunkIndex))
                        {
                            var newChunk = CreateChunk(chunkIndex); // new Chunk(chunkIndex);
                            Chunks.TryAdd(chunkIndex, newChunk);
                            Pipeline.Post(newChunk);
                        }
                    }

                    _lastQueueIndex = currentChunkIndex;
                }
            }
        }

    }
}
