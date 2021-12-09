float4x4 WorldViewProjection;

float4 FogColor = float4(.5, .5, .5, 1);
float FogStart = 64;
float FogEnd = 256;
float3 EyePosition;

Texture2D Textures;

SamplerState SampleType;

float4 HueColors[] = 
{
	float4(1,1,1,1), float4(0,0.78,0.277,1), float4(0.328,0.720,0.0072,1), float4(0.650,0.780,0,1), float4(0,0.880,0.0440,1), float4(0.880,0.675,1,1),
	float4(0.483,0.810,0.0243,1), float4(0.0374,0.810, 0.0243,1), float4(0.610, 0.760, 0.198,1)
};

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

float ComputeFogFactor(float dist)
{
    return clamp((dist - FogStart) / (FogEnd - FogStart), 0, 1);
}

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
	
    VertexShaderOutput output = (VertexShaderOutput)0;
	float4 position = float4(input.Position.xyz, 1);

	float u = (input.Asd & 15) * 1.0/16.0;
	float v = ((input.Asd & 240) >> 4) * 1.0/16.0;
	float l = ((input.Asd & 16128) >> 8) / 60.0f  + 0.2f;
	float a = 1.0 - (((input.Asd & 49152) >> 14) / 3.0);
	int hueIndex = ((input.Asd & 3932160) >> 18);
	float4 hueColor = HueColors[hueIndex];

    output.Position = mul(position, WorldViewProjection);
	output.TexCoords = float2(u, v);
	output.AmbientOcclusion = l * a;
	output.FogFactor = ComputeFogFactor(distance(EyePosition.xyz, input.Position.xyz));
	output.HueColor = hueColor;

    return output;
}




float4 PixelShaderFunction(VertexShaderOutput input) : SV_Target
{
	float4 textureColor =  Textures.Sample(SampleType, input.TexCoords) * input.AmbientOcclusion * input.HueColor;
	// transparent solids
	clip(textureColor.a == 0 ? -1 : 1);
	float4 finalColor = (1.0 - input.FogFactor) * textureColor + (input.FogFactor) * FogColor;
	return finalColor;
}

technique10 Render
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_5_0, VertexShaderFunction() ) );
		SetPixelShader( CompileShader( ps_5_0, PixelShaderFunction() ) );
    }
}
