using AppleCinnamon.Extensions;
using AppleCinnamon.Vertices;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;

namespace AppleCinnamon.Chunks
{
    public sealed class ChunkEffect<TVertex>
        where TVertex : struct, IVertex
    {
        public readonly PrimitiveTopology PrimitiveTopology;
        public readonly Effect Effect;
        public readonly EffectPass Pass;
        public readonly InputLayout InputLayout;

        public ChunkEffect(Device device, string shaderFilePath, PrimitiveTopology primitiveTopology, string textureFilePath = default)
        {
            PrimitiveTopology = primitiveTopology;
            Effect = new Effect(device, ShaderBytecode.CompileFromFile(shaderFilePath, "fx_5_0"));
            Pass = Effect.GetTechniqueByIndex(0).GetPassByIndex(0);
            InputLayout = new InputLayout(device, Pass.Description.Signature, default(TVertex).InputElements);

            if (!string.IsNullOrEmpty(textureFilePath))
            {
                if (typeof(TVertex) == typeof(VertexSkyBox))
                {
                    Effect.GetVariableByName("txMie").AsShaderResource().SetResource(
                        new ShaderResourceView(device, device.CreateTexture2DFromBitmap(textureFilePath)));

                    Effect.GetVariableByName("txRayleigh").AsShaderResource().SetResource(
                        new ShaderResourceView(device, device.CreateTexture2DFromBitmap(textureFilePath)));
                }
                else
                {
                    Effect.GetVariableByName("Textures").AsShaderResource().SetResource(
                        new ShaderResourceView(device, device.CreateTexture2DFromBitmap(textureFilePath)));
                }
            }
        }


        public void Use(Device device)
        {
            device.ImmediateContext.InputAssembler.InputLayout = InputLayout;
            device.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology;
            Pass.Apply(device.ImmediateContext);
        }
    }
}
