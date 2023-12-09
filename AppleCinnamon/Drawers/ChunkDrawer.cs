using System.Collections.Generic;
using System.Threading.Tasks;
using AppleCinnamon.Graphics;
using AppleCinnamon.Graphics.Verticies;
using AppleCinnamon.Options;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace AppleCinnamon.Drawers;

public sealed class ChunkDrawer
{
    private readonly Device _device;

    private readonly ChunkEffectDefinition<VertexSolidBlock> _solidEffectDefinition;
    private readonly ChunkEffectDefinition<VertexWater> _waterEffectDefinition;
    private readonly ChunkEffectDefinition<VertexSprite> _spriteEffectDefinition;
    private readonly EffectDefinition<VertexBox> _boxEffectDefinition;
    private readonly BlendState _waterBlendState;

    private readonly EffectVectorVariable _waterTextureOffsetVar;
    private readonly EffectMatrixVariable _boxEffectWorldViewProjectionVar;

    private int _currentWaterTextureOffsetIndex;

    public ChunkDrawer(Device device)
    {
        _device = device;

        _solidEffectDefinition = new(_device, "Content/Effect/SolidBlockEffect.fx", PrimitiveTopology.TriangleList, "Content/Texture/terrain3.png");
        _waterEffectDefinition = new(_device, "Content/Effect/WaterEffect.fx", PrimitiveTopology.TriangleList, "Content/Texture/custom_water_still.png");
        _spriteEffectDefinition = new(_device, "Content/Effect/SpriteEffetct.fx", PrimitiveTopology.TriangleList, "Content/Texture/terrain3.png");
        _boxEffectDefinition = new(_device, "Content/Effect/BoxDrawerEffect.fx", PrimitiveTopology.PointList);

        _waterTextureOffsetVar = _waterEffectDefinition.Effect.GetVariableByName("TextureOffset").AsVector();
        _boxEffectWorldViewProjectionVar = _boxEffectDefinition.Effect.GetVariableByName("WorldViewProjection").AsMatrix();

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

    public int SolidVertexCountDrawn = 0;
    public int SolidIndexCountDrawn = 0;

    public void Draw(IList<Chunk> chunks, Camera camera)
    {
        SolidVertexCountDrawn = 0;
        SolidIndexCountDrawn = 0;

        if (chunks.Count > 0)
        {
            if (GameOptions.RenderSolid)
            {
                 
                _solidEffectDefinition.Use(_device);
                foreach (var chunk in chunks)
                {
                    SolidIndexCountDrawn += chunk.Buffers.BufferSolid.IndexCount;
                    SolidVertexCountDrawn += chunk.Buffers.BufferSolid.VertexCount;
                    chunk.Buffers.BufferSolid?.Draw(_device);
                }
            }

            if (GameOptions.RenderSprites)
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

            if (GameOptions.RenderWater)
            {
                    
                _device.ImmediateContext.OutputMerger.SetBlendState(_waterBlendState);
                _waterEffectDefinition.Use(_device);
                foreach (var chunk in chunks)
                {
                    chunk.Buffers.BufferWater?.Draw(_device);
                }

                _device.ImmediateContext.OutputMerger.SetBlendState(null);
            }

            if (GameOptions.RenderBoxes)
            {
                var boxVertices = new VertexBox[(camera.CurrentCursor != null ? 1 : 0) + (GameOptions.RenderChunkBoundingBoxes ? chunks.Count : 0)];
                if (camera.CurrentCursor != null)
                {
                    boxVertices[^1] = new VertexBox(camera.CurrentCursor.BoundingBox, new Color3(0.713f, 0.125f, 0.878f));
                }

                if (GameOptions.RenderChunkBoundingBoxes)
                {
                    for (var i = 0; i < chunks.Count; i++)
                    {
                        boxVertices[i] = new VertexBox(ref chunks[i].BoundingBox,
                            new Color3(0.713f, 0.125f, 0.878f));
                    }
                }

                if (boxVertices.Length > 0)
                {
                    using var vertexBuffer = Buffer.Create(_device, BindFlags.VertexBuffer, boxVertices);
                    var binding = new VertexBufferBinding(vertexBuffer, default(VertexBox).Size, 0);
                    _boxEffectDefinition.Use(_device);
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
            _waterTextureOffsetVar.Set(new Vector2(0, _currentWaterTextureOffsetIndex * 1 / 32f));
            await Task.Delay(40);
        }
    }

    public void Update(Camera camera)
    {
        _solidEffectDefinition.Update(camera);
        _waterEffectDefinition.Update(camera);
        _spriteEffectDefinition.Update(camera);

        _boxEffectWorldViewProjectionVar.SetMatrix(camera.WorldViewProjection);
    }
}