cbuffer ChunkContants : register(b0)
{
    float4 ChunkOffset;
};

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
	uint Position : POSITION;
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
	
	  int i = (input.Position & 15);
      int j = (input.Position & 4080) >> 4;
      int k = (input.Position & 61440) >> 12;
      int u = (input.Position & 983040) >> 16;
      int v = (input.Position & 15728640) >> 20;
      int x = (input.Position & 16777216) >> 24;
      int y = (input.Position & 33554432) >> 25;
      int z = (input.Position & 67108864) >> 26;
	  
	  
	  int i1 = (input.Position & 15);
      int j1 = (input.Position & 4080) >> 4;
      int k1 = (input.Position & 61440) >> 12;
      int u1 = (input.Position & 983040) >> 16;
      int v1 = (input.Position & 15728640) >> 20;
      int x1 = (input.Position & 16777216) >> 24;
      int y1 = (input.Position & 33554432) >> 25;
      int z1 = (input.Position & 67108864) >> 26;

	float a = -0.5;	
	float b = -0.5;	
	float c = -0.5;	

	
	if (x == 1) { a = 0.5; } 
	if (y == 1) { b = 0.5; } 
	if (z == 1) { c = 0.5; } 
	
	float4 position = float4(
		i + ChunkOffset.x + a, 
		j + ChunkOffset.y + b, 
		k + ChunkOffset.z + c, 1);

	float2 textCoord = float2(u * (1.0 / 16.0), v * (1.0 / 16.));
	
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
