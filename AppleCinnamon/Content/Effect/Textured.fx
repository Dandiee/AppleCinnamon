float4x4 World;
float4x4 View;
float4x4 Projection;
float4x4 WorldViewProjection;

float alpha;

texture Texture;

sampler texSampler = sampler_state
{
	Texture = <Texture>;
	MinFilter = Linear;
	MipFilter = Linear;
	MagFilter = Linear;
	ADDRESSU = WRAP;
	ADDRESSV = WRAP;
};

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;

    float4 worldPosition = mul(input.Position, World);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);

    output.TexCoord = input.TexCoord;

    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float4 color = tex2D(texSampler, input.TexCoord);
    color.a *= alpha;

	return color;
    return float4(1, 0, 0, 1);
}


technique Textured
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}



