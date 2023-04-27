using System;
using AppleCinnamon.Graphics.Verticies;
using AppleCinnamon.Options;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;

namespace AppleCinnamon.Graphics;

public sealed class ChunkEffectDefinition<TVertex> : EffectDefinition<TVertex>
    where TVertex : struct, IVertex
{
    private readonly EffectMatrixVariable _worldViewProjectionVar;
    private readonly EffectVectorVariable _eyePositionVar;
    private readonly EffectScalarVariable _lightFactorVar;
    private readonly EffectScalarVariable _fogStartVar;
    private readonly EffectScalarVariable _fogEndVar;
    private readonly EffectVectorVariable _fogColorVar;
    private readonly EffectScalarVariable _fogDensityVar;

    public ChunkEffectDefinition(Device device, string shaderFilePath, PrimitiveTopology primitiveTopology, string textureFilePath = default)
        : base(device, shaderFilePath, primitiveTopology, textureFilePath)
    {
        _worldViewProjectionVar = Effect.GetVariableByName("WorldViewProjection").AsMatrix();
        _eyePositionVar = Effect.GetVariableByName("EyePosition").AsVector();
        _lightFactorVar = Effect.GetVariableByName("lightFactor").AsScalar();
        _fogStartVar = Effect.GetVariableByName("FogStart").AsScalar();
        _fogEndVar = Effect.GetVariableByName("FogEnd").AsScalar();
        _fogColorVar = Effect.GetVariableByName("FogColor").AsVector();
        _fogDensityVar = Effect.GetVariableByName("FogDensity").AsScalar();

        _fogDensityVar.Set(0.001f);
    }

    public void Update(Camera camera)
    {
        var lightFactor = SkyDomeOptions.TimeOfDay < 0 
            ? 0 
            : MathUtil.PiOverTwo - Math.Abs(SkyDomeOptions.TimeOfDay - 0.5f) * MathUtil.PiOverTwo;

        _worldViewProjectionVar.SetMatrix(camera.WorldViewProjection);
        _eyePositionVar.Set(camera.Position);
        _lightFactorVar.Set((float)Math.Sin(lightFactor));

        if (camera.IsInWater)
        {
            _fogStartVar.Set(8);
            _fogEndVar.Set(64);
            _fogColorVar.Set(new Vector4(35/255f, 76/255f, 102/255f, 0));
            _fogDensityVar.Set(0.05f);
        }
        else
        {
            _fogStartVar.Set(GameOptions.VIEW_DISTANCE * GameOptions.CHUNK_SIZE);
            _fogEndVar.Set(10* GameOptions.VIEW_DISTANCE * GameOptions.CHUNK_SIZE);
            _fogColorVar.Set(new Vector4(194f/255f, 1, 1, 1));
            _fogDensityVar.Set(0.001f);
        }
    }
}