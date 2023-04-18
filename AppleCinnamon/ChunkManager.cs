using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using AppleCinnamon.Common;
using AppleCinnamon.Drawers;
using AppleCinnamon.Graphics;
using AppleCinnamon.Options;
using SharpDX;
using SharpDX.Direct3D11;

namespace AppleCinnamon
{
    public sealed partial class ChunkManager
    {
        private readonly GraphicsContext _graphicsContext;
        
        public bool IsInitialized { get; private set; }

        public readonly ConcurrentDictionary<Int2, Chunk> BagOfDeath = new();
        public readonly ConcurrentBag<Chunk> Graveyard = new();
        public readonly ConcurrentDictionary<Int2, Chunk> Chunks = new();
        public readonly Pipeline Pipeline;

        private readonly ChunkDrawer _chunkDrawer;

        private Int2? _lastQueueIndex;
        private List<Chunk> _chunksToDraw;
        private int _finishedChunks;

        public int ChunkCreated;
        public int ChunkResurrected;

        public ChunkManager(GraphicsContext graphicsContext)
        {
            _graphicsContext = graphicsContext;
            _chunkDrawer = new ChunkDrawer(graphicsContext.Device);
            _chunksToDraw = new List<Chunk>();
            Pipeline = new Pipeline(FinishChunk, _graphicsContext.Device, this);

            QueueChunksByIndex(Int2.Zero);
        }

        public void Draw(Camera camera)
        {
            _chunkDrawer.Draw(_chunksToDraw, camera);
        }

        public void FinishChunk(Chunk chunk)
        {
            Interlocked.Increment(ref _finishedChunks);
            const int root = (GameOptions.ViewDistance - 1) * 2;

            if (!IsInitialized && _finishedChunks == root)
            {
                IsInitialized = true;
            }

            chunk.State = ChunkState.Finished;
        }


        public void Update(Camera camera, Device device)
        {
            if (IsInitialized)
            {
                _chunkDrawer.Update(camera);
                var currentChunkIndex = new Int2((int)camera.Position.X / GameOptions.ChunkSize, (int)camera.Position.Z / GameOptions.ChunkSize);

                UpdateChunks(camera);
                QueueChunksByIndex(currentChunkIndex);
            }
        }

        

        private void UpdateChunks(Camera camera)
        {
            var now = DateTime.Now;
            _chunksToDraw = new();
            foreach (var chunk in Chunks.Values)
            {
                chunk.IsRendered = false;

                if (!chunk.UpdateDeletion(camera, now))
                {
                    BagOfDeath.TryAdd(chunk.ChunkIndex, chunk);
                }

                chunk.IsRendered = chunk.Deletion != ChunkDeletionState.Deletion &&
                                   chunk.State == ChunkState.Finished && 
                                   (!GameOptions.IsViewFrustumCullingEnabled || 
                                        camera.BoundingFrustum.Contains(ref chunk.BoundingBox) !=  ContainmentType.Disjoint);

                if (chunk.IsRendered)
                {
                    _chunksToDraw.Add(chunk);
                }
            }
        }

        public void CleanUp()
        {
            if (Pipeline.State == PipelineState.Running && BagOfDeath.Count > GameOptions.ViewDistance * 2)
            {
                Pipeline.Suspend();
            }

            if (Pipeline.State == PipelineState.Stopped)
            {
                Massacre();
            }
        }


        private void Massacre()
        {
            foreach (var chunk in BagOfDeath)
            {
                Chunks.Remove(chunk.Key, out var _);
                if (chunk.Value.Buffers == null)
                {
                    Graveyard.Add(chunk.Value);
                }
                chunk.Value.Kill();
                Pipeline.RemoveItem(chunk.Key);
            }

            BagOfDeath.Clear();
            Pipeline.Resume();
        }

        
        private Chunk CreateChunk(Int2 chunkIndex)
        {
            if (Graveyard.Count > 0)
            {
                if (Graveyard.TryTake(out var chunk))
                {
                    ChunkResurrected++;
                    return chunk.Resurrect(chunkIndex);
                }
            }

            ChunkCreated++;
            return new Chunk(chunkIndex);
        }

        private void QueueChunksByIndex(Int2 currentChunkIndex)
        {
            if (!_lastQueueIndex.HasValue || _lastQueueIndex != currentChunkIndex)
            {
                if (Pipeline.State == PipelineState.Running)
                {
                    foreach (var relativeChunkIndex in GetSurroundingChunks(GameOptions.ViewDistance + GameOptions.NumberOfPools - 1))
                    {
                        var chunkIndex = currentChunkIndex + relativeChunkIndex;

                        if (!Chunks.ContainsKey(chunkIndex))
                        {
                            var newChunk = CreateChunk(chunkIndex);
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
