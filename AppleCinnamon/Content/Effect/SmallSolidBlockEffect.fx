float4x4 WorldViewProjection;

float4 FogColor = float4(.5, .5, .5, 1);
float FogStart = 64;
float FogEnd = 256;
float3 EyePosition;
float3 PositionOffset;

Texture2D Textures;

SamplerState SampleType;

struct VertexShaderInput
{
	uint RawData : POSITION;
};


struct VertexShaderOutput
{
    float4 Position : SV_POSITION; // GECIFONTOS
	float2 TexCoords : TEXCOORD0;
	float AmbientOcclusion : COLOR0;
	float FogFactor : COLOR1;
};

float ComputeFogFactor(float dist)
{
    return clamp((dist - FogStart) / (FogEnd - FogStart), 0, 1);
}
VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
	
    VertexShaderOutput output = (VertexShaderOutput)0;
	
	int i = (input.RawData & 15);
	int j = (input.RawData & 4080) >> 4;
	int k = (input.RawData & 61440) >> 12;
	int u = (input.RawData & 983040) >> 16;
	int v = (input.RawData & 15728640) >> 20;
	int x = (input.RawData & 16777216) >> 24;
	int y = (input.RawData & 33554432) >> 25;
	int z = (input.RawData & 67108864) >> 26;

	int c = (input.RawData & 4160749568) >> 27;

	if (x == 0) { x = -0.5; } else { x = 0.5; }
	if (y == 0) { y = -0.5; } else { y = 0.5; }
	if (z == 0) { z = -0.5; } else { z = 0.5; }
	
	float4 position = float4(
		i + PositionOffset.x + x, 
		j + PositionOffset.y + y, 
		k + PositionOffset.z + z, 1);

	float2 textCoord = float2(u * 1 / 16.0, v * 1 / 16);
	
    output.Position = mul(position, WorldViewProjection);
	output.TexCoords = textCoord;

	// output.AmbientOcclusion = input.AmbientOcclusion;
	output.AmbientOcclusion = 1;
	output.FogFactor = ComputeFogFactor(distance(EyePosition.xyz, position.xyz));
	
    return output;
}




float4 PixelShaderFunction(VertexShaderOutput input) : SV_Target
{
	float4 textureColor = Textures.Sample(SampleType, input.TexCoords) * input.AmbientOcclusion;   
	float4 finalColor = (1.0 - input.FogFactor) * textureColor + (input.FogFactor) * FogColor;
	return finalColor;
}

technique10 Render
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VertexShaderFunction() ) );
		SetPixelShader( CompileShader( ps_4_0, PixelShaderFunction() ) );
    }
}
