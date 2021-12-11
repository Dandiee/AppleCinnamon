using System.Collections.Generic;
using System.Threading.Tasks;
using AppleCinnamon.Helper;
using AppleCinnamon.Vertices;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;

namespace AppleCinnamon.Chunks
{
    public sealed class ChunkDrawer
    {
        private readonly Device _device;

        private ChunkEffect<VertexSolidBlock> _solidEffect;
        private ChunkEffect<VertexWater> _waterEffect;
        private ChunkEffect<VertexSprite> _spriteEffect;
        private ChunkEffect<VertexBox> _boxEffect;

        private int _currentWaterTextureOffsetIndex;

        private BlendState _waterBlendState;

        public ChunkDrawer(Device device)
        {
            _device = device;
            LoadContent();
        }

        private void LoadContent()
        {
            _solidEffect = new(_device, "Content/Effect/SolidBlockEffect.fx", PrimitiveTopology.TriangleList, "Content/Texture/terrain3.png");
            _waterEffect = new(_device, "Content/Effect/WaterEffect.fx", PrimitiveTopology.TriangleList, "Content/Texture/custom_water_still.png");
            _spriteEffect = new(_device, "Content/Effect/SpriteEffetct.fx", PrimitiveTopology.TriangleList, "Content/Texture/terrain3.png");
            _boxEffect = new(_device, "Content/Effect/BoxDrawerEffect.fx", PrimitiveTopology.PointList);


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

        public void Draw(List<KeyValuePair<Int2, Chunk>> chunks, Camera camera)
        {
            if (chunks.Count > 0)
            {
                if (Game.RenderSolid)
                {
                    _solidEffect.Use(_device);
                    foreach (var chunk in chunks)
                    {
                        chunk.Value.DrawSmarter(_device, camera.CurrentChunkIndexVector);
                    }
                }

                if (Game.RenderSprites)
                {
                    _spriteEffect.Use(_device);
                    foreach (var chunk in chunks)
                    {
                        if (chunk.Value.BuildingContext.SpriteBlocks.Count > 0)
                        {
                            chunk.Value.DrawSprite(_device);
                        }
                    }
                }

                if (Game.RenderWater)
                {

                    _device.ImmediateContext.OutputMerger.SetBlendState(_waterBlendState);
                    _waterEffect.Use(_device);
                    foreach (var chunk in chunks)
                    {
                        chunk.Value.DrawWater(_device);
                    }
                    _device.ImmediateContext.OutputMerger.SetBlendState(null);
                }

                if (Game.RenderBoxes)
                {
                    var boxVertices = new VertexBox[(camera.CurrentCursor != null ? 1 : 0) + (Game.ShowChunkBoundingBoxes ? chunks.Count : 0)];
                    if (camera.CurrentCursor != null)
                    {
                        boxVertices[^1] = new VertexBox(camera.CurrentCursor.BoundingBox, new Color3(0.713f, 0.125f, 0.878f));
                    }

                    if (Game.ShowChunkBoundingBoxes)
                    {
                        for (var i = 0; i < chunks.Count; i++)
                        {
                            boxVertices[i] = new VertexBox(ref chunks[i].Value.BoundingBox, new Color3(0.713f, 0.125f, 0.878f));
                        }
                    }

                    if (boxVertices.Length > 0)
                    {
                        var vertexBuffer = SharpDX.Direct3D11.Buffer.Create(_device, BindFlags.VertexBuffer, boxVertices);
                        var binding = new VertexBufferBinding(vertexBuffer, default(VertexBox).Size, 0);

                        _boxEffect.Use(_device);
                        _device.ImmediateContext.InputAssembler.SetVertexBuffers(0, binding);
                        _device.ImmediateContext.Draw(boxVertices.Length, 0);
                        _device.ImmediateContext.GeometryShader.Set(null);
                    }
                }
            }
        }


        private async Task UpdateWaterTexture()
        {
            while (true)
            {
                _currentWaterTextureOffsetIndex = (_currentWaterTextureOffsetIndex + 1) % 32;
                _waterEffect.Effect.GetVariableByName("TextureOffset").AsVector().Set(new Vector2(0, _currentWaterTextureOffsetIndex * 1 / 32f));
                await Task.Delay(80);
            }
        }

        public void Update(Camera camera)
        {
            _solidEffect.Effect.GetVariableByName("WorldViewProjection").AsMatrix().SetMatrix(camera.WorldViewProjection);
            _solidEffect.Effect.GetVariableByName("EyePosition").AsVector().Set(camera.Position.ToVector3());
            _waterEffect.Effect.GetVariableByName("WorldViewProjection").AsMatrix().SetMatrix(camera.WorldViewProjection);
            _spriteEffect.Effect.GetVariableByName("WorldViewProjection").AsMatrix().SetMatrix(camera.WorldViewProjection);
            _boxEffect.Effect.GetVariableByName("WorldViewProjection").AsMatrix().SetMatrix(camera.WorldViewProjection);

            if (camera.IsInWater)
            {
                _solidEffect.Effect.GetVariableByName("FogStart").AsScalar().Set(8);
                _solidEffect.Effect.GetVariableByName("FogEnd").AsScalar().Set(64);
                _solidEffect.Effect.GetVariableByName("FogColor").AsVector().Set(new Vector4(0, 0.2f, 1, 0));
            }
            else
            {
                _solidEffect.Effect.GetVariableByName("FogStart").AsScalar().Set(64);
                _solidEffect.Effect.GetVariableByName("FogEnd").AsScalar().Set(Game.ViewDistance * Chunk.SizeXy);
                _solidEffect.Effect.GetVariableByName("FogColor").AsVector().Set(new Vector4(0.5f, 0.5f, 0.5f, 1));
            }
        }
    }
}
