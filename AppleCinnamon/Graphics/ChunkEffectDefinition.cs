﻿using System;
using AppleCinnamon.Drawers;
using AppleCinnamon.Settings;
using AppleCinnamon.Vertices;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;

namespace AppleCinnamon.Grfx
{
    public sealed class ChunkEffectDefinition<TVertex> : EffectDefinition<TVertex>
        where TVertex : struct, IVertex
    {
        private readonly EffectMatrixVariable _worldViewProjectionVar;
        private readonly EffectVectorVariable _eyePositionVar;
        private readonly EffectScalarVariable _lightFactorVar;
        private readonly EffectScalarVariable _fogStartVar;
        private readonly EffectScalarVariable _fogEndVar;
        private readonly EffectVectorVariable _fogColorVar;

        public ChunkEffectDefinition(Device device, string shaderFilePath, PrimitiveTopology primitiveTopology, string textureFilePath = default)
         : base(device, shaderFilePath, primitiveTopology, textureFilePath)
        {
            _worldViewProjectionVar = Effect.GetVariableByName("WorldViewProjection").AsMatrix();
            _eyePositionVar = Effect.GetVariableByName("EyePosition").AsVector();
            _lightFactorVar = Effect.GetVariableByName("lightFactor").AsScalar();
            _fogStartVar = Effect.GetVariableByName("FogStart").AsScalar();
            _fogEndVar = Effect.GetVariableByName("FogEnd").AsScalar();
            _fogColorVar = Effect.GetVariableByName("FogColor").AsVector();
        }

        public void Update(Camera camera)
        {
            var lightFactor = SkyDomeOptions.TimeOfDay < 0 
                ? 0 
                : MathUtil.PiOverTwo - (Math.Abs(SkyDomeOptions.TimeOfDay - 0.5f) * MathUtil.PiOverTwo);

            _worldViewProjectionVar.SetMatrix(camera.WorldViewProjection);
            _eyePositionVar.Set(camera.Position);
            _lightFactorVar.Set((float)Math.Sin(lightFactor));

            if (camera.IsInWater)
            {
                _fogStartVar.Set(8);
                _fogEndVar.Set(64);
                _fogColorVar.Set(new Vector4(0, 0.2f, 1, 0));
            }
            else
            {
                _fogStartVar.Set(Game.ViewDistance * WorldSettings.ChunkSize);
                _fogEndVar.Set(10* Game.ViewDistance * WorldSettings.ChunkSize);
                _fogColorVar.Set(new Vector4(0, 0, 0, 1));
            }
        }
    }
}