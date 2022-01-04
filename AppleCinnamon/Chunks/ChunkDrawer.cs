using System.Collections.Generic;
using System.Threading.Tasks;
using AppleCinnamon.Helper;
using AppleCinnamon.Vertices;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace AppleCinnamon.Chunks
{
    public sealed class ChunkDrawer
    {
        private readonly Device _device;

        private EffectDefinition<VertexSolidBlock> _solidEffectDefinition;
        private EffectDefinition<VertexWater> _waterEffectDefinition;
        private EffectDefinition<VertexSprite> _spriteEffectDefinition;
        private EffectDefinition<VertexBox> _boxEffectDefinition;
        private int _currentWaterTextureOffsetIndex;
        private BlendState _waterBlendState;

        public ChunkDrawer(Device device)
        {
            _device = device;

            LoadContent();
        }

        private void LoadContent()
        {
            _solidEffectDefinition = new(_device, "Content/Effect/SolidBlockEffect.fx", PrimitiveTopology.TriangleList, "Content/Texture/terrain3.png");
            _waterEffectDefinition = new(_device, "Content/Effect/WaterEffect.fx", PrimitiveTopology.TriangleList, "Content/Texture/custom_water_still.png");
            _spriteEffectDefinition = new(_device, "Content/Effect/SpriteEffetct.fx", PrimitiveTopology.TriangleList, "Content/Texture/terrain3.png");
            _boxEffectDefinition = new(_device, "Content/Effect/BoxDrawerEffect.fx", PrimitiveTopology.PointList);

            var blendStateDescription = new BlendStateDescription { AlphaToCoverageEnable = false };

            blendStateDescription.RenderTarget[0].IsBlendEnabled = true;
            blendStateDescription.RenderTarget[0].SourceBlend = BlendOption.SourceAlpha;
            blendStateDescription.RenderTarget[0].DestinationBlend = BlendOption.InverseSourceAlpha;
            blendStateDescription.RenderTarget[0].BlendOperation = BlendOperation.Add;
            blendStateDescription.RenderTarget[0].SourceAlphaBlend = BlendOption.Zero;
            blendStateDescription.RenderTarget[0].DestinationAlphaBlend = BlendOption.Zero;
            blendStateDescription.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
            blendStateDescription.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;

            _waterBlendState = new BlendState(_device, blendStateDescription);

            Task.Run(UpdateWaterTexture);
        }

        public void Draw(IList<Chunk> chunks, Camera camera)
        {
            return;
            if (chunks.Count > 0)
            {
                if (Game.RenderSolid)
                {
                    _solidEffectDefinition.Use(_device);
                    foreach (var chunk in chunks)
                    {
                        chunk.Buffers.BufferSolid?.Draw(_device);
                    }
                }


                if (Game.RenderSprites)
                {
                    _spriteEffectDefinition.Use(_device);
                    foreach (var chunk in chunks)
                    {
                        if (chunk.BuildingContext.SpriteBlocks.Count > 0)
                        {
                            chunk.Buffers.BufferSprite?.Draw(_device);
                        }
                    }
                }

                if (Game.RenderWater)
                {

                    _device.ImmediateContext.OutputMerger.SetBlendState(_waterBlendState);
                    _waterEffectDefinition.Use(_device);
                    foreach (var chunk in chunks)
                    {
                        chunk.Buffers.BufferWater?.Draw(_device);
                    }

                    _device.ImmediateContext.OutputMerger.SetBlendState(null);
                }

                if (Game.RenderBoxes)
                {
                    var boxVertices = new VertexBox[(camera.CurrentCursor != null ? 1 : 0) +
                                                    (Game.ShowChunkBoundingBoxes ? chunks.Count : 0)];
                    if (camera.CurrentCursor != null)
                    {
                        boxVertices[^1] = new VertexBox(camera.CurrentCursor.BoundingBox,
                            new Color3(0.713f, 0.125f, 0.878f));
                    }

                    if (Game.ShowChunkBoundingBoxes)
                    {
                        for (var i = 0; i < chunks.Count; i++)
                        {
                            boxVertices[i] = new VertexBox(ref chunks[i].BoundingBox,
                                new Color3(0.713f, 0.125f, 0.878f));
                        }
                    }

                    if (boxVertices.Length > 0)
                    {
                        using (var vertexBuffer = Buffer.Create(_device, BindFlags.VertexBuffer, boxVertices))
                        {
                            var binding = new VertexBufferBinding(vertexBuffer, default(VertexBox).Size, 0);
                            _boxEffectDefinition.Use(_device);
                            _device.ImmediateContext.InputAssembler.SetVertexBuffers(0, binding);
                            _device.ImmediateContext.Draw(boxVertices.Length, 0);
                            _device.ImmediateContext.GeometryShader.Set(null);
                        }
                    }
                }
            }
        }


        private async Task UpdateWaterTexture()
        {
            while (true)
            {
                _currentWaterTextureOffsetIndex = (_currentWaterTextureOffsetIndex + 1) % 32;
                _waterEffectDefinition.Effect.GetVariableByName("TextureOffset").AsVector().Set(new Vector2(0, _currentWaterTextureOffsetIndex * 1 / 32f));
                await Task.Delay(80);
            }
        }


        public void Update(Camera camera, World world)
        {
            _solidEffectDefinition.Update(camera, world);
            _waterEffectDefinition.Update(camera, world);
            _spriteEffectDefinition.Update(camera, world);
            _boxEffectDefinition.Effect.GetVariableByName("WorldViewProjection").AsMatrix().SetMatrix(camera.WorldViewProjection);
        }
    }
}
