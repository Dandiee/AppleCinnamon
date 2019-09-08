float4x4 World;
float4x4 View;
float4x4 Projection;
float3 Normal = float3(0, 1, 0);
float TextureOffset;
Texture Textures;

sampler TextureSampler = sampler_state { texture = <Textures> ; magfilter = LINEAR; minfilter = LINEAR; mipfilter=LINEAR; AddressU = mirror; AddressV = mirror;};
// TODO: add effect parameters here.

struct VertexShaderInput
{
    float4 Position : POSITION0;
	float2 TexCoords    : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
	float2 TexCoords    : TEXCOORD0;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;

    float4 worldPosition = mul(input.Position, World);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);
    output.TexCoords = float2(input.TexCoords.x, input.TexCoords.y + TextureOffset);

    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
	float4 c = tex2D(TextureSampler, input.TexCoords); 
    return float4(c.xyz, 0.5f);
}

technique Technique1
{
    pass Pass1
    {
        // TODO: set renderstates here.

        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
