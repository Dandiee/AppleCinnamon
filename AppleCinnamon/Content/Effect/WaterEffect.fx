float4x4 WorldViewProjection;
Texture2D Textures;

float FogEnabled = 1;
float3 EyePosition;
float4 FogColor;
float FogStart = 64;
float FogEnd = 256;
float FogDensity = 0.005;

float lightFactor = 1.0f;
float2 TextureOffset;

SamplerState SampleType;

struct VertexShaderInput
{
    float3 Position : POSITION0;
	float2 TexCoords : TEXCOORD0;
	uint CompositeLight: VISIBILITY;
};


struct VertexShaderOutput
{
    float4 Position : SV_POSITION; // GECIFONTOS
	float2 TexCoords : TEXCOORD0;
	float Brightness : COLOR0;
	float FogFactor : COLOR1;
};
float fogFactorExp2(
	const float dist,
	const float density
) {
	const float LOG2 = -1.442695;
	float d = density * dist;
	return 1.0 - clamp(exp2(d * d * LOG2), 0.0, 1.0);
}

float ComputeFogFactor(float dist)
{
	return fogFactorExp2(dist, FogDensity);
	return clamp((dist - FogStart) / (FogEnd - FogStart), 0, 1);
}
float totalLightness = 1.0 / 15.0f;
VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
	
    VertexShaderOutput output = (VertexShaderOutput)0;
	float4 position = float4(input.Position.xyz, 1);
	
    output.Position = mul(position, WorldViewProjection);
	output.TexCoords = input.TexCoords + float2(TextureOffset.x, TextureOffset.y);

	float sunlight       = ((input.CompositeLight >> 0) & 15) * totalLightness * lightFactor + 2.0f;
	float compositeLight = ((input.CompositeLight >> 4) & 15) * totalLightness + 2.0f;

	output.Brightness = max(sunlight, compositeLight);
	output.FogFactor = ComputeFogFactor(distance(EyePosition.xyz, input.Position.xyz));

    return output;
}



float4 PixelShaderFunction(VertexShaderOutput input) : SV_Target
{
    float4 color = Textures.Sample(SampleType, input.TexCoords) * input.Brightness;

	float4 finalColor = lerp(color, FogColor, input.FogFactor);
	//float4 finalColor = (1.0 - input.FogFactor) * color + (input.FogFactor) * FogColor;

	return float4(finalColor.xyz, 0.7) * float4(lightFactor, lightFactor, lightFactor, 1);
}

technique10 Render
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VertexShaderFunction() ) );
		SetPixelShader( CompileShader( ps_4_0, PixelShaderFunction() ) );
    }
}
