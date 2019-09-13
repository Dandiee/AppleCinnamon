using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using AppleCinnamon.Pipeline;
using AppleCinnamon.System;
using AppleCinnamon.Vertices;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.WIC;

namespace AppleCinnamon
{
    public interface IChunkManager
    {
        int FinalizedChunks { get; }
        int RenderedChunks { get; }

        Voxel? GetVoxel(Int3 absoluteIndex);
    }

    public sealed class ChunkManager : IChunkManager
    {
        private int _finalizedChunks;
        public int FinalizedChunks => _finalizedChunks;

        private int _renderedChunks;
        public int RenderedChunks => _renderedChunks;
        


        private readonly Graphics _graphics;
        private Effect _solidBlockEffect;

        private readonly ConcurrentDictionary<Int2, Chunk> _chunks;
        private readonly ConcurrentDictionary<Int2, object> _queuedChunks;
        
        private readonly IChunkBuilder _chunkBuilder;
        private readonly IChunkDispatcher _chunkDispatcher;
        private readonly ILightPropagationService _lightPropagationService;
        private readonly IChunkProvider _chunkProvider;
        private readonly ILightFinalizer _lightFinalizer;
        private readonly ILightUpdater _lightUpdater;
        private readonly IChunkPool _chunkPool;

        public EventHandler FirstChunkLoaded;

        public ConcurrentBag<Dictionary<string, long>> Benchmark { get; }
        public const int ViewDistance = 8;
        public bool IsInitialized { get; private set; }
        public int ChunksCount => _chunks.Count;

        

        private TransformBlock<DataflowContext<Int2>, DataflowContext<Chunk>> _pipeline;

        public ChunkManager(Graphics graphics)
        {
            Benchmark = new ConcurrentBag<Dictionary<string, long>>();
            _graphics = graphics;
            _chunks = new ConcurrentDictionary<Int2,Chunk>();
            _chunkBuilder = new ChunkBuilder();
            _chunkDispatcher = new ChunkDispatcher();
            _queuedChunks = new ConcurrentDictionary<Int2, object>();
            _lightPropagationService = new LightPropagationService();
            _chunkProvider = new ChunkProvider(921207);
            _lightFinalizer = new LightFinalizer();
            _lightUpdater = new LightUpdater();
            _chunkPool = new ChunkPool();


            //_pipeline = Create(Environment.ProcessorCount);
            _pipeline = Create(1);

            LoadContent();

            QueueChunksByIndex(Int2.Zero);
        }

        private void LoadContent()
        {
            _solidBlockEffect = new Effect(_graphics.Device,
                ShaderBytecode.CompileFromFile("Content/Effect/SolidBlockEffect.fx", "fx_5_0"));

            _solidBlockEffect.GetVariableByName("Textures").AsShaderResource().SetResource(
                new ShaderResourceView(_graphics.Device,
                    TextureLoader.CreateTexture2DFromBitmap(_graphics.Device,
                        TextureLoader.LoadBitmap(new ImagingFactory2(), "Content/Texture/terrain.png"))));
        }

        public TransformBlock<DataflowContext<Int2>, DataflowContext<Chunk>> Create(int mdop)
        {
            var dataflowOptions = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = mdop
            };

            var pipeline = new TransformBlock<DataflowContext<Int2>, DataflowContext<Chunk>>(_chunkProvider.GetChunk, dataflowOptions);
            var lighter = new TransformBlock<DataflowContext<Chunk>, DataflowContext<Chunk>>(_lightPropagationService.InitializeLocalLight, dataflowOptions);
            var pool = new TransformManyBlock<DataflowContext<Chunk>, DataflowContext<Chunk>>(_chunkPool.Process, dataflowOptions);
            var lightFinalizer = new TransformBlock<DataflowContext<Chunk>, DataflowContext<Chunk>>(_lightFinalizer.Finalize, dataflowOptions);
            var dispatcher = new TransformBlock<DataflowContext<Chunk>, DataflowContext<Chunk>>(_chunkDispatcher.Dispatch, dataflowOptions);
            var finalizer = new ActionBlock<DataflowContext<Chunk>>(Finalize, dataflowOptions);

            pipeline.LinkTo(lighter);
            lighter.LinkTo(pool);
            pool.LinkTo(lightFinalizer);
            lightFinalizer.LinkTo(dispatcher);
            dispatcher.LinkTo(finalizer);

            return pipeline;
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

            Interlocked.Increment(ref _finalizedChunks);
         
            var root = ViewDistance * 2 + 1;
            if (!IsInitialized && _finalizedChunks == root * root)
            {
                IsInitialized = true;
                _pipeline.Complete();
                _pipeline = Create( Math.Max(1, Environment.ProcessorCount / 2));

                FirstChunkLoaded?.Invoke(this, null);
                Debug.Write(_chunks.Count);
            }
        }

        public void Draw(Camera camera)
        {
            _renderedChunks = 0;

            if (_chunks.Count > 0)
            {
                using (var inputLayout = new InputLayout(_graphics.Device,
                    _solidBlockEffect.GetTechniqueByIndex(0).GetPassByIndex(0).Description.Signature,
                    VertexSolidBlock.InputElements))
                {
                    _graphics.Device.ImmediateContext.InputAssembler.InputLayout = inputLayout;
                    _graphics.Device.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

                    var pass = _solidBlockEffect.GetTechniqueByIndex(0).GetPassByIndex(0);
                    pass.Apply(_graphics.Device.ImmediateContext);

                    foreach (var chunk in _chunks.Values)
                    {
                        var bb = chunk.BoundingBox;
                        if (camera.BoundingFrustum.Contains(ref bb) != ContainmentType.Disjoint)
                        {
                            chunk.Draw(_graphics.Device, camera.CurrentChunkIndexVector);
                            _renderedChunks++;
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
            _solidBlockEffect.GetVariableByName("WorldViewProjection").AsMatrix().SetMatrix(camera.WorldViewProjection);

            if (IsInitialized)
            {
                var currentChunkIndex =
                    new Int2((int) camera.Position.X / Chunk.Size.X, (int) camera.Position.Z / Chunk.Size.Z);

                QueueChunksByIndex(currentChunkIndex);
            }
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

                    _pipeline.Post(new DataflowContext<Int2>(chunkIndex, _graphics.Device));
                }
            }
        }

        private bool _isUpdateInProgress;
        public void SetBlock(Int3 absoluteIndex, byte voxel)
        {
            if (_isUpdateInProgress)
            {
                return;
            }

            var address = Chunk.GetVoxelAddress(absoluteIndex);
            if (address == null || !_chunks.TryGetValue(address.Value.ChunkIndex, out var chunk))
            {
                return;
            }

            _isUpdateInProgress = true;

            var oldVoxel = chunk.GetLocalVoxel(address.Value.RelativeVoxelIndex);
            var newVoxel = new Voxel(voxel, 0);
            chunk.SetLocalVoxel(address.Value.RelativeVoxelIndex, newVoxel);
            _lightUpdater.UpdateLighting(chunk, address.Value.RelativeVoxelIndex, oldVoxel, newVoxel);
            _chunkBuilder.BuildChunk(_graphics.Device, chunk);

            Task.WaitAll(GetSurroundingChunks(2).Select(chunkIndex =>
            {
                if (chunkIndex != Int2.Zero &&
                    _chunks.TryGetValue(chunkIndex + chunk.ChunkIndex, out var chunkToReload))
                {
                    return Task.Run(() => _chunkBuilder.BuildChunk(_graphics.Device, chunkToReload));
                }

                return Task.CompletedTask;
            }).ToArray());


            _isUpdateInProgress = false;
        }

       
    }
}
