using AppleCinnamon.Extensions;
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

        public void Update(Camera camera)
        {
            Effect.GetVariableByName("WorldViewProjection").AsMatrix().SetMatrix(camera.WorldViewProjection);
            Effect.GetVariableByName("EyePosition").AsVector().Set(camera.Position.ToVector3());
            Effect.GetVariableByName("lightFactor").AsScalar().Set(Hofman.SunlightFactor);

            if (camera.IsInWater)
            {
                Effect.GetVariableByName("FogStart").AsScalar().Set(8);
                Effect.GetVariableByName("FogEnd").AsScalar().Set(64);
                Effect.GetVariableByName("FogColor").AsVector().Set(new Vector4(0, 0.2f, 1, 0));
            }
            else
            {
                Effect.GetVariableByName("FogStart").AsScalar().Set(64);
                Effect.GetVariableByName("FogEnd").AsScalar().Set(Game.ViewDistance * Chunk.SizeXy);
                Effect.GetVariableByName("FogColor").AsVector().Set(new Vector4(0.5f, 0.5f, 0.5f, 1));
            }
        }
    }
}
