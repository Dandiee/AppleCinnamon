float4x4 WorldViewProjection;
float3 Normal;
Texture2D Textures;

float FogStart = 150;
float FogEnd = 400;
float FogEnabled = 1;

float4 ambientColor = float4(0.5, 0.5, 0.5, 1.0);
float4 diffuseColor = float4(1.0, 1.0, 1.0, 1.0);
float3 lightDirection = float3(-.3, -1, -0.2);
float4 SunDirection;
float4 SunColor;

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
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
	
    VertexShaderOutput output = (VertexShaderOutput)0;
	float4 position = float4(input.Position.xyz, 1);
	
    output.Position = mul(position, WorldViewProjection);
	output.TexCoords = input.TexCoords;
	output.AmbientOcclusion = input.AmbientOcclusion;
	
    return output;
}

float ComputeFogFactor(float d)
{
    return clamp((d - FogStart) / (FogEnd - FogStart), 0, 1) * FogEnabled;
}


float4 PixelShaderFunction(VertexShaderOutput input) : SV_Target
{
    return Textures.Sample(SampleType, input.TexCoords) * input.AmbientOcclusion;
}

technique10 Render
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VertexShaderFunction() ) );
		SetPixelShader( CompileShader( ps_4_0, PixelShaderFunction() ) );
    }
}
