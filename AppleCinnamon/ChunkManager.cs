using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
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
        ConcurrentDictionary<string, long> PipelinePerformance { get; }

        Voxel? GetVoxel(Int3 absoluteIndex);
        bool TryGetChunk(Int2 chunkIndex, out Chunk chunk);
    }


    public sealed class ChunkManager : IChunkManager
    {
        public const int ViewDistance = 8;

        // debug fields
        private int _finalizedChunks;
        public int FinalizedChunks => _finalizedChunks;

        private int _renderedChunks;
        public int RenderedChunks => _renderedChunks;

        public bool IsInitialized { get; private set; }

        public ConcurrentDictionary<string, long> PipelinePerformance { get; }


        private readonly Graphics _graphics;
        private Effect _solidBlockEffect;
        private readonly ConcurrentDictionary<Int2, Chunk> _chunks;
        private readonly ConcurrentDictionary<Int2, object> _queuedChunks;
        private readonly IChunkUpdater _chunkUpdater;
        private readonly IPipelineProvider _pipelineProvider;

        
        

        private TransformBlock<DataflowContext<Int2>, DataflowContext<Chunk>> _pipeline;

        public ChunkManager(Graphics graphics)
        {
            _pipelineProvider = new PipelineProvider();

            _graphics = graphics;
            _chunks = new ConcurrentDictionary<Int2,Chunk>();
            _queuedChunks = new ConcurrentDictionary<Int2, object>();
            PipelinePerformance = new ConcurrentDictionary<string, long>();

            _pipeline = _pipelineProvider.CreatePipeline(1, Finalize);
            _chunkUpdater = new ChunkUpdater(graphics, this);

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

        public bool TryGetChunk(Int2 chunkIndex, out Chunk chunk)
        {
            if (_chunks.TryGetValue(chunkIndex, out var currentChunk))
            {
                chunk = currentChunk;
                return true;
            }


            chunk = null;
            return false;
        }

        private void Finalize(DataflowContext<Chunk> context)
        {
            if (!_chunks.TryAdd(context.Payload.ChunkIndex, context.Payload))
            {
                throw new Exception();
            }

            if (!_queuedChunks.TryRemove(context.Payload.ChunkIndex, out _))
            {
                throw new Exception();
            }

            foreach (var performance in context.Debug)
            {
                if (PipelinePerformance.TryGetValue(performance.Key, out var value))
                {
                    PipelinePerformance[performance.Key] = value + performance.Value;
                }
                else
                {
                    PipelinePerformance[performance.Key] = performance.Value;
                }
            }

            Interlocked.Increment(ref _finalizedChunks);
            var root = ViewDistance * 2 + 1;
            if (!IsInitialized && _finalizedChunks == root * root)
            {
                IsInitialized = true;
                _pipeline.Complete();
                _pipeline = _pipelineProvider.CreatePipeline(Math.Max(1, Environment.ProcessorCount / 2), Finalize);
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

        public void SetBlock(Int3 absoluteIndex, byte voxel) => _chunkUpdater.SetVoxel(absoluteIndex, voxel);
    }
}
