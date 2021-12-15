float4x4 WorldViewProjection;

float4 FogColor = float4(.5, .5, .5, 1);
float FogStart = 64;
float FogEnd = 256;
float3 EyePosition;

float2 Resolution = float2(1827, 997);

Texture2D Textures;

SamplerState SS
{
	Texture = <Textures>;
	AddressU = Clamp;
	AddressV = Clamp;
	AddressW = Clamp;
	Filter = MIN_MAG_MIP_POINT;	
	MaxAnisotropy = 16;
};

float4 HueColors[] = 
{
	float4(1,1,1,1),
	float4(0,0.78,0.277,1), 
	float4(152 / 255.0, 245 / 255.0, 95 / 255.0,1),
	float4(0.650,0.780,0,1),
	float4(0.650,0.780,0,1), 
	float4(0,0.880,0.0440,1), 
	float4(0.483,0.810,0.0243,1), 
	float4(0.0374,0.810, 0.0243,1), 
	float4(0.610, 0.760, 0.198,1),
	float4(0.698,  0.870, 0.226,1)
};

struct VertexShaderInput
{
    float3 Position : POSITION0;
	uint MetaData: VISIBILITY;
};


struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
	float2 TexCoords : TEXCOORD0;
	float AmbientOcclusion : COLOR0;
	float FogFactor : COLOR1;
	float4 HueColor : COLOR2;
};

float textureFactor = 1.0 / 16.0;
float totalLightness = 1.0/60.0f;
float lightFactor = 1.0f;

float ComputeFogFactor(float dist)
{
    return clamp((dist - FogStart) / (FogEnd - FogStart), 0, 1);
}

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput)0;
	float4 position = float4(input.Position.xyz, 1);

	float textureCoordinateU   = ((input.MetaData >>  0) & 31) * textureFactor;
	float textureCoordinateV   = ((input.MetaData >>  5) & 31) * textureFactor;
	float sunlight             = ((input.MetaData >> 10) & 63) * totalLightness * lightFactor;
	float ambientNeighborCount = ((input.MetaData >> 16) & 15);
	int   hueIndex             = ((input.MetaData >> 20) & 15);
	float emittedLight         = ((input.MetaData >> 26) & 63) * totalLightness + 0.2f;

    output.Position = mul(position, WorldViewProjection);
	output.TexCoords = float2(textureCoordinateU, textureCoordinateV);
	output.AmbientOcclusion = max(sunlight, emittedLight) * (1.0 - ambientNeighborCount / 3.0);
	output.FogFactor = ComputeFogFactor(distance(EyePosition.xyz, input.Position.xyz));
	output.HueColor = HueColors[hueIndex];

    return output;
}


float4 PixelShaderFunction(VertexShaderOutput input) : SV_Target
{
	float4 textureColor = Textures.Sample(SS, input.TexCoords) * input.AmbientOcclusion /** float4(1.8, 1.8, 1.8, 1)*/ * input.HueColor;
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
