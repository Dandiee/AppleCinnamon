using System;
using AppleCinnamon.Extensions;
using AppleCinnamon.Settings;
using AppleCinnamon.Vertices;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;

namespace AppleCinnamon.Chunks
{
    public sealed class EffectDefinition<TVertex>
        where TVertex : struct, IVertex
    {
        public readonly PrimitiveTopology PrimitiveTopology;
        public readonly Effect Effect;
        public readonly EffectPass Pass;
        public readonly InputLayout InputLayout;

        public EffectDefinition(Device device, string shaderFilePath, PrimitiveTopology primitiveTopology, string textureFilePath = default)
        {
            PrimitiveTopology = primitiveTopology;
            Effect = new Effect(device, ShaderBytecode.CompileFromFile(shaderFilePath, "fx_5_0"));
            Pass = Effect.GetTechniqueByIndex(0).GetPassByIndex(0);
            InputLayout = new InputLayout(device, Pass.Description.Signature, default(TVertex).InputElements);

            if (!string.IsNullOrEmpty(textureFilePath))
            {
                Effect.GetVariableByName("Textures").AsShaderResource().SetResource(new ShaderResourceView(device, device.CreateTexture2DFromBitmap(textureFilePath)));
            }
        }


        public void Use(Device device)
        {
            device.ImmediateContext.InputAssembler.InputLayout = InputLayout;
            device.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology;
            Pass.Apply(device.ImmediateContext);
        }

        public void Update(Camera camera, World world)
        {
            var lightFactor = world.Time < 0 
                ? 0 
                : MathUtil.PiOverTwo - (Math.Abs(world.Time - 0.5f) * MathUtil.PiOverTwo);

            Effect.GetVariableByName("WorldViewProjection").AsMatrix().SetMatrix(camera.WorldViewProjection);
            Effect.GetVariableByName("EyePosition").AsVector().Set(camera.Position);
            Effect.GetVariableByName("lightFactor").AsScalar().Set((float)Math.Sin(lightFactor));

            if (camera.IsInWater)
            {
                Effect.GetVariableByName("FogStart").AsScalar().Set(8);
                Effect.GetVariableByName("FogEnd").AsScalar().Set(64);
                Effect.GetVariableByName("FogColor").AsVector().Set(new Vector4(0, 0.2f, 1, 0));
            }
            else
            {
                Effect.GetVariableByName("FogStart").AsScalar().Set(64);
                Effect.GetVariableByName("FogEnd").AsScalar().Set(Game.ViewDistance * WorldSettings.ChunkSize);
                Effect.GetVariableByName("FogColor").AsVector().Set(new Vector4(0, 0, 0, 1));
            }
        }
    }
}
