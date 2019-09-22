using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        int QueuedChunks { get; }
        int TotalVisibleFaces { get; }
        int TotalVisibleVoxels { get; }

        ConcurrentDictionary<string, long> PipelinePerformance { get; }

        Voxel? GetVoxel(Int3 absoluteIndex);
        bool TryGetChunk(Int2 chunkIndex, out Chunk chunk);
    }


    public sealed class ChunkManager : IChunkManager
    {
        public const int ViewDistance = 8;
        public static readonly int InitialDegreeOfParallelism = 1;

        // debug fields
        private int _queuedChunksCount;
        public int QueuedChunks => _queuedChunksCount;

        private int _finalizedChunks;
        public int FinalizedChunks => _finalizedChunks;

        private int _renderedChunks;
        public int RenderedChunks => _renderedChunks;

        public int TotalVisibleFaces { get; private set; }
        public int TotalVisibleVoxels { get; private set; }

        public bool IsInitialized { get; private set; }

        public ConcurrentDictionary<string, long> PipelinePerformance { get; }


        private readonly Graphics _graphics;
        private Effect _solidBlockEffect;
        private Effect _waterBlockEffect;
        public readonly ConcurrentDictionary<Int2, Chunk> Chunks;
        private readonly ConcurrentDictionary<Int2, object> _queuedChunks;
        private readonly IChunkUpdater _chunkUpdater;
        private readonly IPipelineProvider _pipelineProvider;
        private int _currentWaterTextureOffsetIndex;


        private TransformBlock<DataflowContext<Int2>, DataflowContext<Chunk>> _pipeline;

        public ChunkManager(Graphics graphics)
        {
            _pipelineProvider = new PipelineProvider();

            _graphics = graphics;
            Chunks = new ConcurrentDictionary<Int2, Chunk>();
            _queuedChunks = new ConcurrentDictionary<Int2, object>();
            PipelinePerformance = new ConcurrentDictionary<string, long>();

            _pipeline = _pipelineProvider.CreatePipeline(InitialDegreeOfParallelism, Finalize);
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


            _waterBlockEffect = new Effect(_graphics.Device,
                ShaderBytecode.CompileFromFile("Content/Effect/WaterEffect.fx", "fx_5_0"));

            _waterBlockEffect.GetVariableByName("Textures").AsShaderResource().SetResource(
                new ShaderResourceView(_graphics.Device,
                    TextureLoader.CreateTexture2DFromBitmap(_graphics.Device,
                        TextureLoader.LoadBitmap(new ImagingFactory2(), "Content/Texture/custom_water_still.png"))));


            Task.Run(UpdateWaterTexture);
        }

        private async Task UpdateWaterTexture()
        {
            while (true)
            {
                _currentWaterTextureOffsetIndex = (_currentWaterTextureOffsetIndex + 1) % 32;
                _waterBlockEffect.GetVariableByName("TextureOffset").AsVector().Set(new Vector2(0, _currentWaterTextureOffsetIndex * 1 / 32f));
                await Task.Delay(80);
            }
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
                : chunk.Voxels[address.Value.RelativeVoxelIndex.ToFlatIndex()];
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

        private void Finalize(DataflowContext<Chunk> context)
        {
            if (!Chunks.TryAdd(context.Payload.ChunkIndex, context.Payload))
            {
                throw new Exception();
            }

            if (!_queuedChunks.TryRemove(context.Payload.ChunkIndex, out _))
            {
                throw new Exception();
            }

            Interlocked.Decrement(ref _queuedChunksCount);

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
                TotalVisibleFaces = Chunks.Values.Sum(s => s.VisibleFacesCount);
                TotalVisibleVoxels = Chunks.Values.Sum(s => s.VisibilityFlags.Count);

                _pipeline.Complete();
                _pipeline = _pipelineProvider.CreatePipeline(Math.Max(1, Environment.ProcessorCount / 2), Finalize);
            }
        }

        private int _drawCallCounter;
        public void Draw(Camera camera)
        {
            if (IsInitialized)
            {
                if (_drawCallCounter == 300 * 10)
                {
                    //Debugger.Break();
                }
                else
                {
                    _drawCallCounter++;
                }
            }

            

            _renderedChunks = 0;

            if (Chunks.Count > 0)
            {
                using (var inputLayout = new InputLayout(_graphics.Device,
                    _solidBlockEffect.GetTechniqueByIndex(0).GetPassByIndex(0).Description.Signature,
                    VertexSolidBlock.InputElements))
                {
                    _graphics.Device.ImmediateContext.InputAssembler.InputLayout = inputLayout;
                    _graphics.Device.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

                    var pass = _solidBlockEffect.GetTechniqueByIndex(0).GetPassByIndex(0);
                    pass.Apply(_graphics.Device.ImmediateContext);

                    foreach (var chunk in Chunks.Values)
                    {
                        if (!Game.IsViewFrustumCullingEnabled || camera.BoundingFrustum.Contains(ref chunk.BoundingBox) != ContainmentType.Disjoint)
                        {
                            chunk.DrawSmarter(_graphics.Device, camera.CurrentChunkIndexVector);
                            _renderedChunks++;
                        }
                    }
                }

                using (var inputLayout = new InputLayout(_graphics.Device,
                    _waterBlockEffect.GetTechniqueByIndex(0).GetPassByIndex(0).Description.Signature,
                    VertexWater.InputElements))
                {
                    var blendStateDescription = new BlendStateDescription { AlphaToCoverageEnable = false };

                    blendStateDescription.RenderTarget[0].IsBlendEnabled = true;
                    blendStateDescription.RenderTarget[0].SourceBlend = BlendOption.SourceAlpha;
                    blendStateDescription.RenderTarget[0].DestinationBlend = BlendOption.InverseSourceAlpha;
                    blendStateDescription.RenderTarget[0].BlendOperation = BlendOperation.Add;
                    blendStateDescription.RenderTarget[0].SourceAlphaBlend = BlendOption.Zero;
                    blendStateDescription.RenderTarget[0].DestinationAlphaBlend = BlendOption.Zero;
                    blendStateDescription.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
                    blendStateDescription.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;

                    var blendState = new BlendState(_graphics.Device, blendStateDescription);
                    _graphics.Device.ImmediateContext.OutputMerger.SetBlendState(blendState);

                    _graphics.Device.ImmediateContext.InputAssembler.InputLayout = inputLayout;
                    _graphics.Device.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

                    var pass = _waterBlockEffect.GetTechniqueByIndex(0).GetPassByIndex(0);
                    pass.Apply(_graphics.Device.ImmediateContext);

                    foreach (var chunk in Chunks.Values)
                    {
                        var bb = chunk.BoundingBox;
                        if (camera.BoundingFrustum.Contains(ref bb) != ContainmentType.Disjoint)
                        {
                            chunk.DrawWater(_graphics.Device);
                        }
                    }

                    _graphics.Device.ImmediateContext.OutputMerger.SetBlendState(null);
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
            _solidBlockEffect.GetVariableByName("EyePosition").AsVector().Set(camera.Position.ToVector3());
            _waterBlockEffect.GetVariableByName("WorldViewProjection").AsMatrix().SetMatrix(camera.WorldViewProjection);

            if (camera.IsInWater)
            {
                _solidBlockEffect.GetVariableByName("FogStart").AsScalar().Set(8);
                _solidBlockEffect.GetVariableByName("FogEnd").AsScalar().Set(64);
                _solidBlockEffect.GetVariableByName("FogColor").AsVector().Set(new Vector4(0, 0.2f, 1, 0));
            }
            else
            {
                _solidBlockEffect.GetVariableByName("FogStart").AsScalar().Set(64);
                _solidBlockEffect.GetVariableByName("FogEnd").AsScalar().Set(ViewDistance*Chunk.SizeXy);
                _solidBlockEffect.GetVariableByName("FogColor").AsVector().Set(new Vector4(0.5f, 0.5f, 0.5f, 1));
            }

            if (IsInitialized)
            {
                var currentChunkIndex =
                    new Int2((int)camera.Position.X / Chunk.SizeXy, (int)camera.Position.Z / Chunk.SizeXy);

                QueueChunksByIndex(currentChunkIndex);
            }
        }


        private void QueueChunksByIndex(Int2 currentChunkIndex)
        {
            foreach (var relativeChunkIndex in GetSurroundingChunks(ViewDistance))
            {
                var chunkIndex = currentChunkIndex + relativeChunkIndex;
                if (!_queuedChunks.ContainsKey(chunkIndex) && !Chunks.ContainsKey(chunkIndex))
                {
                    if (!_queuedChunks.TryAdd(chunkIndex, null))
                    {
                        throw new Exception("asdasd");
                    }

                    Interlocked.Increment(ref _queuedChunksCount);

                    _pipeline.Post(new DataflowContext<Int2>(chunkIndex, _graphics.Device));
                }
            }
        }
        public void SetBlock(Int3 absoluteIndex, byte voxel) => _chunkUpdater.SetVoxel(absoluteIndex, voxel);
    }
}
