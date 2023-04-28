float4x4 WorldViewProjection;

float FogEnabled = 1;
float3 EyePosition;
float4 FogColor;
float FogStart = 64;
float FogEnd = 256;
float FogDensity = 0.005;


float lightFactor = 1.0f; // SUNLIGHT FACTOR SET BY THE EFFECT

Texture2D Textures;

float4 HueColors[] =
{
	float4(1,1,1,1),
	float4(0,0.78,0.277,1),
	//float4(152 / 255.0, 245 / 255.0, 95 / 255.0,1),
	  float4(152 / 255.0, 245 / 255.0, 95 / 255.0,1),
	float4(144/255.0,191 / 255.0,96/255.0,1),
	float4(0.650,0.780,0,1),
	float4(0,0.880,0.0440,1),
	float4(0.483,0.810,0.0243,1),
	float4(0.0374,0.810, 0.0243,1),
	float4(0.610, 0.760, 0.198,1),
	float4(0.698,  0.870, 0.226,1)
};

SamplerState SS
{
	Texture = <Textures>;
	AddressU = Clamp;
	AddressV = Clamp;
	AddressW = Clamp;
	Filter = MIN_MAG_MIP_POINT;
	MaxAnisotropy = 16;
};

//float4(0.880,0.675,1,1),
struct VertexShaderInput
{
	float3 Position : POSITION0;
	uint MetaData: VISIBILITY;
};


struct VertexShaderOutput
{
	float4 Position : SV_POSITION; // GECIFONTOS
	float2 TexCoords : TEXCOORD0;
	float AmbientOcclusion : COLOR0;
	float FogFactor : COLOR1;
	float4 HueColor : COLOR2;
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

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
	VertexShaderOutput output = (VertexShaderOutput)0;
	float4 position = float4(input.Position.xyz, 1);

	// since theres no real light smoothing going on we only need to scale down everything to 0..1
	// the maximum value is 15 so our factor is 1/15
	const float smoothingFactor = 1.0f / 15.0f;

	// we also happen to have 16*16 voxel texture on the texture bitmap
	const float textureFactor = 1.0f / 16.0f;

	float textureCoordinateU = ((input.MetaData >>  0) & 31) * textureFactor;	// 5 bits
	float textureCoordinateV = ((input.MetaData >>  5) & 31) * textureFactor;	// 5 bits
	float sunlight           = ((input.MetaData >> 10) & 15) * lightFactor;		// 4 bits
	float emittedLight       = ((input.MetaData >> 14) & 15);					// 4 bits
	int   hueIndex           = ((input.MetaData >> 18) & 15);					// 4 bits

	output.Position = mul(position, WorldViewProjection);
	output.TexCoords = float2(textureCoordinateU, textureCoordinateV);
	output.AmbientOcclusion = max(sunlight, emittedLight) * smoothingFactor;
	output.FogFactor = ComputeFogFactor(distance(EyePosition.xyz, input.Position.xyz));
	output.HueColor = HueColors[hueIndex];

	return output;
}



float4 PixelShaderFunction(VertexShaderOutput input) : SV_Target
{
	float4 textureColor = Textures.Sample(SS, input.TexCoords) * input.AmbientOcclusion * input.HueColor;
	clip(textureColor.a == 0 ? -1 : 1);
	return lerp(textureColor, FogColor, input.FogFactor);
}

technique10 Render
{
	pass P0
	{
		SetVertexShader(CompileShader(vs_5_0, VertexShaderFunction()));
		SetPixelShader(CompileShader(ps_5_0, PixelShaderFunction()));
	}
}
