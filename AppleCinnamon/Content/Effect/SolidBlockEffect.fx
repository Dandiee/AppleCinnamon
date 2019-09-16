float4x4 WorldViewProjection;

float4 FogColor = float4(.5, .5, .5, 1);
float FogStart = 64;
float FogEnd = 256;
float3 EyePosition;

Texture2D Textures;

SamplerState SampleType;

struct VertexShaderInput
{
    float3 Position : POSITION0;
	float2 TexCoords : TEXCOORD0;
	float AmbientOcclusion: COLOR0;
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
	float4 position = float4(input.Position.xyz, 1);
	
    output.Position = mul(position, WorldViewProjection);
	output.TexCoords = input.TexCoords;
	output.AmbientOcclusion = input.AmbientOcclusion;
	output.FogFactor = ComputeFogFactor(distance(EyePosition.xyz, input.Position.xyz));
	
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
