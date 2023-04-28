using System;
using System.Collections.Generic;
using System.Windows.Forms;
using AppleCinnamon.ChunkBuilder;
using AppleCinnamon.Graphics;
using AppleCinnamon.Graphics.Verticies;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;

using SharpDX.Mathematics.Interop;
using BlendOperation = SharpDX.Direct2D1.BlendOperation;
using Image = System.Drawing.Image;

namespace AppleCinnamon.Drawers;

public sealed class CloudDrawer
{
    private readonly GraphicsContext _graphicsContext;
    private readonly Geometry _geometry;
    private readonly SolidColorBrush _brush;

    private readonly EffectDefinition<VertexCloud> _effectDefinition;
    private readonly BufferDefinition<VertexCloud> _buffer;
    
    private readonly EffectMatrixVariable _worldViewProjectionVar;
    private readonly EffectVectorVariable _sunAmbientColorVar;
    private readonly EffectVectorVariable _sunDirectionVar;
    private readonly EffectVectorVariable _sunDiffuseColorVar;

    private readonly BlendState _waterBlendState;

    public CloudDrawer(GraphicsContext graphicsContext)
    {
        _graphicsContext = graphicsContext;
        var bitmap = new System.Drawing.Bitmap(Image.FromFile("Content/Texture/clouds.png"));

        var ones = 0;

        _effectDefinition = new EffectDefinition<VertexCloud>(_graphicsContext.Device, "Content/Effect/CloudEffect.fx", PrimitiveTopology.TriangleList);
        _worldViewProjectionVar = _effectDefinition.Effect.GetVariableByName("WorldViewProjection").AsMatrix();
        
        _sunAmbientColorVar = _effectDefinition.Effect.GetVariableByName("SunAmbientColor").AsVector();
        _sunDirectionVar = _effectDefinition.Effect.GetVariableByName("SunDirection").AsVector();
        _sunDiffuseColorVar = _effectDefinition.Effect.GetVariableByName("SunDiffuseColor").AsVector();

        _sunAmbientColorVar.Set(new Vector4(.5f, .5f, .5f, 1));
        _sunDirectionVar.Set(Vector3.Normalize(new Vector3(-15, -50, -30)));
        _sunDiffuseColorVar.Set(new Vector3(1, 1, 1));


        var blendStateDescription = new BlendStateDescription { AlphaToCoverageEnable = false };

        blendStateDescription.RenderTarget[0].IsBlendEnabled = true;
        blendStateDescription.RenderTarget[0].SourceBlend = BlendOption.SourceAlpha;
        blendStateDescription.RenderTarget[0].DestinationBlend = BlendOption.InverseSourceAlpha;
        blendStateDescription.RenderTarget[0].BlendOperation = SharpDX.Direct3D11.BlendOperation.Add;
        blendStateDescription.RenderTarget[0].SourceAlphaBlend = BlendOption.Zero;
        blendStateDescription.RenderTarget[0].DestinationAlphaBlend = BlendOption.Zero;
        blendStateDescription.RenderTarget[0].AlphaBlendOperation = SharpDX.Direct3D11.BlendOperation.Add;
        blendStateDescription.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;

        _waterBlendState = new BlendState(_graphicsContext.Device, blendStateDescription);


        var vertices = new List<VertexCloud>();
        var indexes = new List<uint>();

        var c = 0;
        for (var i = 0; i < bitmap.Width; i++)
        {
            for (var k = 0; k < bitmap.Height; k++)
            {
                var color = bitmap.GetPixel(i, k);
                if (color.A == 255)
                {
                    AddFace(ref vertices, ref indexes, ref FaceBuildInfo.FaceVertices.Bottom, i, k, -Vector3.UnitY);
                    AddFace(ref vertices, ref indexes, ref FaceBuildInfo.FaceVertices.Top, i, k, Vector3.UnitY);

                    if (i > 0 && bitmap.GetPixel(i - 1, k).A != 255)
                    {
                        AddFace(ref vertices, ref indexes, ref FaceBuildInfo.FaceVertices.Left, i, k, -Vector3.UnitX);
                    }

                    if (i < bitmap.Width - 1 && bitmap.GetPixel(i + 1, k).A != 255)
                    {
                        AddFace(ref vertices, ref indexes, ref FaceBuildInfo.FaceVertices.Right, i, k, Vector3.UnitX);
                    }

                    if (k > 0 && bitmap.GetPixel(i, k - 1).A != 255)
                    {
                        AddFace(ref vertices, ref indexes, ref FaceBuildInfo.FaceVertices.Front, i, k, -Vector3.UnitZ);
                    }

                    if (k < bitmap.Height - 1 && bitmap.GetPixel(i, k + 1).A != 255)
                    {
                        AddFace(ref vertices, ref indexes, ref FaceBuildInfo.FaceVertices.Back, i, k, Vector3.UnitZ);
                    }
                }
            }
        }

        var verticesArray = vertices.ToArray();
        var indexesArray = indexes.ToArray();
        _buffer = new BufferDefinition<VertexCloud>(graphicsContext.Device, ref verticesArray, ref indexesArray);
    }

    private void AddFace(ref List<VertexCloud> vertices, ref List<uint> indexes, ref Vector3[] faceVertices, int i, int k, Vector3 normal)
    {
        var count = vertices.Count;

        var scaler = new Vector3(12, 4, 12);
        var offset = new Vector3(-255 * 6, 0, -255 * 6);

        foreach (var b in faceVertices)
        {
            vertices.Add(new VertexCloud(scaler * b + scaler * new Vector3(i, 1, k) + 264 * Vector3.UnitY + offset, normal)); //, -Vector3.UnitY));
        }

        indexes.Add((uint)(count + 0));
        indexes.Add((uint)(count + 2));
        indexes.Add((uint)(count + 3));
        indexes.Add((uint)(count + 0));
        indexes.Add((uint)(count + 1));
        indexes.Add((uint)(count + 2));
    }


    public void Update(Camera camera, Vector3 sunPosition)
    {
        var sunDir = Vector3.Normalize(sunPosition);
        _sunDirectionVar.Set(Vector3.Normalize(sunDir));
        _worldViewProjectionVar.SetMatrix(camera.WorldViewProjection);
    }

    public void Draw()
    {
        _graphicsContext.Device.ImmediateContext.OutputMerger.SetBlendState(_waterBlendState);
        _effectDefinition.Use(_graphicsContext.Device);
        _buffer.Draw(_graphicsContext.Device);
        _graphicsContext.Device.ImmediateContext.OutputMerger.SetBlendState(null);
    }
}