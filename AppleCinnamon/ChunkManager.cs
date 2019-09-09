using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using AppleCinnamon.Pipeline;
using AppleCinnamon.System;
using AppleCinnamon.Vertices;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.Windows;

namespace AppleCinnamon
{
    public interface IChunkManager { }
    public sealed class ChunkManager : IChunkManager
    {
        private readonly Device _device;
        private readonly BoxDrawer _boxDrawer;
        private readonly Map _map;

        private readonly ConcurrentDictionary<Int2, Chunk> _chunks;
        private readonly ConcurrentDictionary<Int2, object> _queuedChunks;
        private readonly TransformBlock<DataflowContext<Int2>, DataflowContext<Chunk>> _pipeline;
        private readonly IChunkBuilder _chunkBuilder;
        private readonly IChunkDispatcher _chunkDispatcher;
        private readonly ILightPropagationService _lightPropagationService;
        private readonly IChunkProvider _chunkProvider;
        private readonly ILightFinalizer _lightFinalizer;
        private readonly ILightUpdater _lightUpdater;

        public EventHandler FirstChunkLoaded;

        public ConcurrentBag<Dictionary<string, long>> Benchmark { get; }
        public const int ViewDistance = 8;
        public bool IsFirstChunkInitialized { get; private set; }
        public int ChunksCount => _chunks.Count;

        public int RenderedChunks;

        public ChunkManager(Device device, BoxDrawer boxDrawer, Map map)
        {
            _device = device;
            _boxDrawer = boxDrawer;
            _map = map;
            _chunks = new ConcurrentDictionary<Int2,Chunk>();
            _chunkBuilder = new ChunkBuilder();
            _chunkDispatcher = new ChunkDispatcher();
            _queuedChunks = new ConcurrentDictionary<Int2, object>();
            _lightPropagationService = new LightPropagationService();
            _chunkProvider = new ChunkProvider(921207);
            Benchmark = new ConcurrentBag<Dictionary<string, long>>();
            _lightFinalizer = new LightFinalizer();
            _lightUpdater = new LightUpdater();

            var chunkPool = new ChunkPool();

            var dataflowOptions = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = 1 // Environment.ProcessorCount
            };

         
            _pipeline = new TransformBlock<DataflowContext<Int2>, DataflowContext<Chunk>>(_chunkProvider.GetChunk, dataflowOptions);
            var lighter = new TransformBlock<DataflowContext<Chunk>, DataflowContext<Chunk>>(_lightPropagationService.InitializeLocalLight, dataflowOptions);
            var pool = new TransformManyBlock<DataflowContext<Chunk>, DataflowContext<Chunk>>(chunkPool.Process, dataflowOptions);
            var lightFinalizer = new TransformBlock<DataflowContext<Chunk>, DataflowContext<Chunk>>(_lightFinalizer.Finalize, dataflowOptions);
            var dispatcher = new TransformBlock<DataflowContext<Chunk>, DataflowContext<Chunk>>(_chunkDispatcher.Dispatch, dataflowOptions);
            var finalizer = new ActionBlock<DataflowContext<Chunk>>(Finalize, dataflowOptions);
            
            _pipeline.LinkTo(lighter);
            lighter.LinkTo(pool);
            pool.LinkTo(lightFinalizer);
            lightFinalizer.LinkTo(dispatcher);
            dispatcher.LinkTo(finalizer);

            QueueChunksByIndex(Int2.Zero);
        }

        public Voxel? GetVoxel(Int3 absoluteIndex)
        {
            var address = Chunk.GetVoxelAddress(absoluteIndex);
            if (address == null)
            {
                return null;
            }

            if (!_chunks.TryGetValue(address.Value.ChunkIndex, out var chunk))
            {
                return null;
            }

            return chunk.GetLocalVoxel(address.Value.RelativeVoxelIndex);
        }

        private void Finalize(DataflowContext<Chunk> context)
        {
            Benchmark.Add(context.Debug);

            if (!_chunks.TryAdd(context.Payload.ChunkIndex, context.Payload))
            {
                throw new Exception();
            }

            if (!_queuedChunks.TryRemove(context.Payload.ChunkIndex, out _))
            {
                throw new Exception();
            }

            var position = new Vector3(
                context.Payload.ChunkIndex.X * Chunk.Size.X + Chunk.Size.X / 2f - .5f,
                Chunk.Size.Y / 2f,
                context.Payload.ChunkIndex.Y * Chunk.Size.Z + Chunk.Size.Z / 2f - .5f);

            // _boxDrawer.Set("chunk_" + context.Payload.ChunkIndex, new BoxDetails(Chunk.Size.ToVector3(), position, Color.Red.ToColor3()));

            if (!IsFirstChunkInitialized)
            {
                IsFirstChunkInitialized = true;
                FirstChunkLoaded?.Invoke(this, null);
            }
        }

        public void Draw(Effect effect, Device device, RenderForm renderForm)
        {
            RenderedChunks = 0;

            if (_chunks.Count > 0)
            {
                using (var inputLayout = new InputLayout(device,
                    effect.GetTechniqueByIndex(0).GetPassByIndex(0).Description.Signature,
                    VertexSolidBlock.InputElements))
                {
                    device.ImmediateContext.InputAssembler.InputLayout = inputLayout;
                    device.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

                    var pass = effect.GetTechniqueByIndex(0).GetPassByIndex(0);
                    pass.Apply(device.ImmediateContext);

                    foreach (var chunk in _chunks.Values)
                    {
                        var bb = chunk.BoundingBox;
                        if (_map.Camera.BoundingFrustum.Contains(ref bb) != ContainmentType.Disjoint)
                        {
                            chunk.Draw(device, _map.Camera.CurrentChunkIndexVector);
                            RenderedChunks++;
                        }
                    }
                }
            }
        }
       
        private static readonly IReadOnlyCollection<Int2> Directions = new[]
        {
            new Int2(1, 0),
            new Int2(0, 1),
            new Int2(-1, 0),
            new Int2(0, -1)
        };

        

        private static IEnumerable<Int2> GetSurroundingChunks(int size)
        {
            yield return new Int2();

            for (var i = 1; i < size; i++)
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

        public void Update(GameTime gameTime, Camera camera)
        {
            if (camera == null) return;

            var currentChunkIndex =
                new Int2((int)camera.Position.X / Chunk.Size.X, (int)camera.Position.Z / Chunk.Size.Z);

            QueueChunksByIndex(currentChunkIndex);
        }


        private void QueueChunksByIndex(Int2 currentChunkIndex)
        {
            foreach (var relativeChunkIndex in GetSurroundingChunks(ViewDistance))
            {
                var chunkIndex = currentChunkIndex + relativeChunkIndex;
                if (!_queuedChunks.ContainsKey(chunkIndex) && !_chunks.ContainsKey(chunkIndex))
                {
                    if (!_queuedChunks.TryAdd(chunkIndex, null))
                    {
                        throw new Exception("asdasd");
                    }
                    _pipeline.Post(new DataflowContext<Int2>(chunkIndex, _device));
                }
            }
        }

        public void SetBlock(Int3 absoluteIndex, byte voxel)
        {
            var address = Chunk.GetVoxelAddress(absoluteIndex);
            if (address == null || !_chunks.TryGetValue(address.Value.ChunkIndex, out var chunk))
            {
                return;
            }

            var oldVoxel = chunk.GetLocalVoxel(address.Value.RelativeVoxelIndex);
            var newVoxel = new Voxel(voxel, 0);
            chunk.SetLocalVoxel(address.Value.RelativeVoxelIndex, newVoxel);
            _lightUpdater.UpdateLighting(chunk, address.Value.RelativeVoxelIndex, oldVoxel, newVoxel);
            _chunkBuilder.BuildChunk(_device, chunk);

            Task.Run(() =>
            {
                foreach (var chunkIndex in GetSurroundingChunks(2))
                {
                    if (chunkIndex != Int2.Zero &&
                        _chunks.TryGetValue(chunkIndex + chunk.ChunkIndex, out var chunkToReload))
                    {
                        _chunkBuilder.BuildChunk(_device, chunkToReload);
                    }
                }
            });
        }

       
    }
}
