float4x4 WorldViewProjection;

float4 FogColor = float4(.5, .5, .5, 1);
float FogStart = 64;
float FogEnd = 256;
float3 EyePosition;

Texture2D Textures;

SamplerState SampleType;


//float4(0.880,0.675,1,1),
struct VertexShaderInput
{
	float3 Position : POSITION0;
	uint Asd: VISIBILITY;
};


struct VertexShaderOutput
{
	float4 Position : SV_POSITION; // GECIFONTOS
	float2 TexCoords : TEXCOORD0;
	float AmbientOcclusion : COLOR0;
	float FogFactor : COLOR1;
	float4 HueColor : COLOR2;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{

	VertexShaderOutput output = (VertexShaderOutput)0;
	float4 position = float4(input.Position.xyz, 1);

	float u = (input.Asd & 15) * 1.0 / 16.0;
	float v = ((input.Asd & 240) >> 4) * 1.0 / 16.0;
	float l = ((input.Asd & 16128) >> 8) / 60.0f + 0.2f;
	
	output.Position = mul(position, WorldViewProjection);
	output.TexCoords = float2(u, v);
	output.AmbientOcclusion = l;

	return output;
}




float4 PixelShaderFunction(VertexShaderOutput input) : SV_Target
{
	float4 textureColor = Textures.Sample(SampleType, input.TexCoords) * input.AmbientOcclusion;
	clip(textureColor.a == 0 ? -1 : 1);
	return textureColor;
}

technique10 Render
{
	pass P0
	{
		SetVertexShader(CompileShader(vs_5_0, VertexShaderFunction()));
		SetPixelShader(CompileShader(ps_5_0, PixelShaderFunction()));
	}
}
