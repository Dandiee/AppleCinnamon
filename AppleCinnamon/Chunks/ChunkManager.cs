using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        public readonly Graphics _graphics;
        private int _finalizedChunks;
        public bool IsInitialized { get; private set; }

        public static readonly ConcurrentDictionary<Int2, Chunk> Chunks = new();
        private readonly ChunkUpdater _chunkUpdater;
        private readonly ChunkDrawer _chunkDrawer;
        private Int2? _lastQueueIndex;
        public readonly Pipeline Pipeline;
        private List<Chunk> _chunksToDraw;
        public bool _isSuspended;

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
            Interlocked.Increment(ref _finalizedChunks);
            const int root = (Game.ViewDistance - 1) * 2;

            if (!IsInitialized && _finalizedChunks == root)
            {
                IsInitialized = true;
            }

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

                chunk.IsRendered = !chunk.IsTimeToDie && chunk.State == ChunkState.Finished && (!Game.IsViewFrustumCullingEnabled ||
                                                                               camera.BoundingFrustum.Contains(ref chunk.BoundingBox) !=
                                                                               ContainmentType.Disjoint);
                if (chunk.IsRendered)
                {
                    _chunksToDraw.Add(chunk);
                }
            }
        }

        public void CleanUp(Device device)
        {
            if (!_isSuspended && Pipeline.State == PipelineState.Running)
            {
                if (BagOfDeath.Count > Game.ViewDistance * 2)
                {
                    Pipeline.Suspend(() =>
                    {
                        _isSuspended = true;
                    });
                }
            }

            if (_isSuspended)
            {
                //Pipeline.Resume();
                Massacre(device);
            }
        }

        

        private void Massacre(Device device)
        {
            foreach (var chunk in BagOfDeath)
            {
                Chunks.Remove(chunk.Key, out var _);
                chunk.Value.Kill(_graphics.Device);
                Pipeline.RemoveItem(chunk.Key);
            }

            BagOfDeath.Clear();
            Pipeline.Resume();
            _isSuspended = false;
        }

        public Chunk CreateChunk(Int2 chunkIndex) => new(chunkIndex);

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
