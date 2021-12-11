using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using AppleCinnamon.Helper;
using AppleCinnamon.Pipeline;
using AppleCinnamon.Vertices;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using SharpDX.WIC;
using Vector2 = SharpDX.Vector2;
using Vector4 = SharpDX.Vector4;

namespace AppleCinnamon
{
    public sealed class ChunkManager : IDisposable
    {

        //public const int ViewDistance = 8;
        public static readonly int InitialDegreeOfParallelism = 1; //Environment.ProcessorCount / 2;

        // debug fields
        private int _queuedChunksCount;
        public int QueuedChunks => _queuedChunksCount;

        private int _finalizedChunks;
        public int FinalizedChunks => _finalizedChunks;

        private int _renderedChunks;
        public int RenderedChunks => _renderedChunks;

        public int TotalVisibleFaces { get; private set; }
        public int TotalVisibleVoxels { get; private set; }
        public string QuickTest { get; private set; }

        public bool IsInitialized { get; private set; }
        public TimeSpan BootTime { get; private set; }
        public readonly DateTime StartUpTime;

        public ConcurrentDictionary<string, long> PipelinePerformance { get; }


        private readonly Graphics _graphics;
        private Effect _solidBlockEffect;
        private EffectPass _solidBlockEffectPass;
        private InputLayout _solidBlockInputLayout;

        private Effect _waterBlockEffect;
        private EffectPass _waterBlockEffectPass;
        private InputLayout _waterBlockInputLayout;

        private Effect _spriteBlockEffect;
        private EffectPass _spriteBlockEffectPass;
        private InputLayout _spriteBlockInputLayout;

        public readonly ConcurrentDictionary<Int2, Chunk> Chunks;
        public readonly List<Chunk> QuickChunks;
        private readonly ConcurrentDictionary<Int2, object> _queuedChunks;
        private readonly ChunkUpdater _chunkUpdater;
        private readonly PipelineProvider _pipelineProvider;
        private int _currentWaterTextureOffsetIndex;

        private BlendState _waterBlendState;


        private TransformBlock<DataflowContext<Int2>, DataflowContext<Chunk>> _pipeline;

        public ChunkManager(Graphics graphics)
        {
            _pipelineProvider = new PipelineProvider();

            _graphics = graphics;
            Chunks = new ConcurrentDictionary<Int2, Chunk>();
            _queuedChunks = new ConcurrentDictionary<Int2, object>();
            PipelinePerformance = new ConcurrentDictionary<string, long>();
            QuickChunks = new List<Chunk>();
            _pipeline = _pipelineProvider.CreatePipeline(InitialDegreeOfParallelism, Finalize);
            _chunkUpdater = new ChunkUpdater(graphics, this);

            LoadContent();

            StartUpTime = DateTime.Now;
            QueueChunksByIndex(Int2.Zero);
        }

        private SamplerState SS;

        private void LoadContent()
        {
            _solidBlockEffect = new Effect(_graphics.Device, ShaderBytecode.CompileFromFile("Content/Effect/SolidBlockEffect.fx", "fx_5_0"));
            _solidBlockEffectPass = _solidBlockEffect.GetTechniqueByIndex(0).GetPassByIndex(0);
            _solidBlockInputLayout = new InputLayout(_graphics.Device, _solidBlockEffectPass.Description.Signature, VertexSolidBlock.InputElements);
            _solidBlockEffect.GetVariableByName("Textures").AsShaderResource().SetResource(
                new ShaderResourceView(_graphics.Device,
                    TextureLoader.CreateTexture2DFromBitmap(_graphics.Device,
                        TextureLoader.LoadBitmap(new ImagingFactory2(), "Content/Texture/terrain3.png"))));
            //_graphics.Device.ImmediateContext.PixelShader.SetSampler();
            SamplerStateDescription description = SamplerStateDescription.Default();

            var asdasdas = new SamplerStateDescription
            {
                ComparisonFunction = Comparison.NotEqual,
                AddressU = TextureAddressMode.Border,
                AddressV = TextureAddressMode.Clamp,
                AddressW = TextureAddressMode.Border,
                BorderColor = new RawColor4(1, 1, 1, 1),
                Filter = Filter.Anisotropic,
                MaximumAnisotropy = 1,
                MaximumLod = 1,
                MinimumLod = 1,
                MipLodBias = 1
            };

            SS = new SamplerState(_graphics.Device, description);
            //_solidBlockEffect.GetVariableByName("SampleType").AsSampler().SetSampler(0, ss);

            //_solidBlockEffectPass.PixelShaderDescription.Variable.Set

            _spriteBlockEffect = new Effect(_graphics.Device, ShaderBytecode.CompileFromFile("Content/Effect/SpriteEffetct.fx", "fx_5_0"));
            _spriteBlockEffectPass = _spriteBlockEffect.GetTechniqueByIndex(0).GetPassByIndex(0);
            _spriteBlockInputLayout = new InputLayout(_graphics.Device, _spriteBlockEffectPass.Description.Signature, VertexSprite.InputElements);
            _spriteBlockEffect.GetVariableByName("Textures").AsShaderResource().SetResource(
                new ShaderResourceView(_graphics.Device,
                    TextureLoader.CreateTexture2DFromBitmap(_graphics.Device,
                        TextureLoader.LoadBitmap(new ImagingFactory2(), "Content/Texture/terrain3.png"))));

            _waterBlockEffect = new Effect(_graphics.Device, ShaderBytecode.CompileFromFile("Content/Effect/WaterEffect.fx", "fx_5_0"));
            _waterBlockEffectPass = _waterBlockEffect.GetTechniqueByIndex(0).GetPassByIndex(0);
            _waterBlockInputLayout = new InputLayout(_graphics.Device, _waterBlockEffectPass.Description.Signature, VertexWater.InputElements);
            _waterBlockEffect.GetVariableByName("Textures").AsShaderResource().SetResource(
                new ShaderResourceView(_graphics.Device,
                    TextureLoader.CreateTexture2DFromBitmap(_graphics.Device,
                        TextureLoader.LoadBitmap(new ImagingFactory2(), "Content/Texture/custom_water_still.png"))));

            

            var blendStateDescription = new BlendStateDescription { AlphaToCoverageEnable = false };

            blendStateDescription.RenderTarget[0].IsBlendEnabled = true;
            blendStateDescription.RenderTarget[0].SourceBlend = BlendOption.SourceAlpha;
            blendStateDescription.RenderTarget[0].DestinationBlend = BlendOption.InverseSourceAlpha;
            blendStateDescription.RenderTarget[0].BlendOperation = BlendOperation.Add;
            blendStateDescription.RenderTarget[0].SourceAlphaBlend = BlendOption.Zero;
            blendStateDescription.RenderTarget[0].DestinationAlphaBlend = BlendOption.Zero;
            blendStateDescription.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
            blendStateDescription.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;

            _waterBlendState = new BlendState(_graphics.Device, blendStateDescription);

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
                : chunk.GetVoxel(address.Value.RelativeVoxelIndex.ToFlatIndex(chunk.CurrentHeight));
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

            QuickChunks.Add(context.Payload);

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
            var root = Game.ViewDistance * 2 + 1;
            if (!IsInitialized && _finalizedChunks == root * root)
            {
                BootTime = DateTime.Now - StartUpTime;

                IsInitialized = true;
                TotalVisibleFaces = Chunks.Values.Sum(s => s.VisibleFacesCount);
                TotalVisibleVoxels = Chunks.Values.Sum(s => s.BuildingContext.VisibilityFlags.Count);

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

            if (QuickChunks == null)
            {
                return;
            }

            if (Chunks.Count > 0)
            {
                var chunksToRender = Chunks
                    .Where(chunk => !Game.IsViewFrustumCullingEnabled || camera.BoundingFrustum.Contains(ref chunk.Value.BoundingBox) != ContainmentType.Disjoint)
                    .ToList();

                if (Game.ShowChunkBoundingBoxes)
                {
                    var vertices = chunksToRender
                        .Select(s => new VertexBox(s.Value.BoundingBox.Minimum, s.Value.BoundingBox.Maximum, Color.Red.ToColor3()))
                        .ToArray();

                    var vertexBuffer = SharpDX.Direct3D11.Buffer.Create(_graphics.Device, BindFlags.VertexBuffer, vertices);
                    var binding = new VertexBufferBinding(vertexBuffer, VertexBox.Size, 0);
                    
                    _graphics.Device.ImmediateContext.InputAssembler.InputLayout = _spriteBlockInputLayout;
                    _graphics.Device.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.LineStrip;
                    _spriteBlockEffectPass.Apply(_graphics.Device.ImmediateContext);
                    _graphics.Device.ImmediateContext.InputAssembler.SetVertexBuffers(0, binding);
                    _graphics.Device.ImmediateContext.Draw(vertices.Length, 0);
                    _graphics.Device.ImmediateContext.GeometryShader.Set(null);
                }

                if (Game.RenderSolid)
                {
                    //_graphics.Device.ImmediateContext.PixelShader.SetSampler(0, SS);
                    _graphics.Device.ImmediateContext.InputAssembler.InputLayout = _solidBlockInputLayout;
                    _graphics.Device.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
                    _solidBlockEffectPass.Apply(_graphics.Device.ImmediateContext);

                    var sw = Stopwatch.StartNew();
                    foreach (var chunk in chunksToRender)
                    {
                        chunk.Value.DrawSmarter(_graphics.Device, camera.CurrentChunkIndexVector);
                        _renderedChunks++;
                    }

                    sw.Stop();
                    QuickTest = sw.ElapsedMilliseconds + " / " + sw.ElapsedMilliseconds;
                    
                }


                

                if (true)
                {
                    
                    _graphics.Device.ImmediateContext.InputAssembler.InputLayout = _spriteBlockInputLayout;
                    _graphics.Device.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
                    _spriteBlockEffectPass.Apply(_graphics.Device.ImmediateContext);


                    foreach (var chunk in chunksToRender)
                    {
                        if (chunk.Value.BuildingContext.SpriteBlocks.Count > 0)
                        {
                            chunk.Value.DrawSprite(_graphics.Device);
                        }
                    }

                    
                }

                if (Game.RenderWater)
                {

                    _graphics.Device.ImmediateContext.OutputMerger.SetBlendState(_waterBlendState);
                    _graphics.Device.ImmediateContext.InputAssembler.InputLayout = _waterBlockInputLayout;
                    _graphics.Device.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
                    _waterBlockEffectPass.Apply(_graphics.Device.ImmediateContext);

                    foreach (var chunk in chunksToRender)
                    {
                        chunk.Value.DrawWater(_graphics.Device);
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

            _spriteBlockEffect.GetVariableByName("WorldViewProjection").AsMatrix().SetMatrix(camera.WorldViewProjection);
            //_spriteBlockEffect.GetVariableByName("EyePosition").AsMatrix().SetMatrix(camera.WorldViewProjection);

            if (camera.IsInWater)
            {
                _solidBlockEffect.GetVariableByName("FogStart").AsScalar().Set(8);
                _solidBlockEffect.GetVariableByName("FogEnd").AsScalar().Set(64);
                _solidBlockEffect.GetVariableByName("FogColor").AsVector().Set(new Vector4(0, 0.2f, 1, 0));
            }
            else
            {
                _solidBlockEffect.GetVariableByName("FogStart").AsScalar().Set(64);
                _solidBlockEffect.GetVariableByName("FogEnd").AsScalar().Set(Game.ViewDistance * Chunk.SizeXy);
                _solidBlockEffect.GetVariableByName("FogColor").AsVector().Set(new Vector4(0.5f, 0.5f, 0.5f, 1));
            }

            if (IsInitialized)
            {
                var currentChunkIndex =
                    new Int2((int)camera.Position.X / Chunk.SizeXy, (int)camera.Position.Z / Chunk.SizeXy);

                QueueChunksByIndex(currentChunkIndex);
            }
        }

        private Int2? _lastQueueIndex;
        private void QueueChunksByIndex(Int2 currentChunkIndex)
        {
            if (!_lastQueueIndex.HasValue || _lastQueueIndex != currentChunkIndex)
            {
                foreach (var relativeChunkIndex in GetSurroundingChunks(Game.ViewDistance))
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

                _lastQueueIndex = currentChunkIndex;
            }
        }
        public void SetBlock(Int3 absoluteIndex, byte voxel) => _chunkUpdater.SetVoxel(absoluteIndex, voxel);

        public void Dispose()
        {
            _solidBlockEffect?.Dispose();
            _waterBlockEffect?.Dispose();
            _solidBlockEffectPass?.Dispose();
            _waterBlockEffectPass?.Dispose();
            _solidBlockInputLayout?.Dispose();
            _waterBlockInputLayout?.Dispose();
            _waterBlendState?.Dispose();
        }
    }
}
