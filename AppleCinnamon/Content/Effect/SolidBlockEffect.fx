float4x4 WorldViewProjection;

float4 FogColor;
float FogStart = 64;
float FogEnd = 256;
float FogDensity = 0.005;
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
float lightFactor = 1.0f; // SUNLIGHT FACTOR SET BY THE EFFECT
//float totalLightness = 1.0/60.0f;


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

	float textureCoordinateU   = ((input.MetaData >>  0) & 31) * textureFactor;
	float textureCoordinateV   = ((input.MetaData >>  5) & 31) * textureFactor;
	float sunlight			   = ((input.MetaData >> 10) & 63) * lightFactor; // the light factor is the t variant for the sun
	float ambientNeighborCount = ((input.MetaData >> 16) & 15);
	int   hueIndex             = ((input.MetaData >> 20) & 15);
	float emittedLight         = ((input.MetaData >> 26) & 63);

	// each AO neighbor decreases the vertex brightness by 10 percent
	// if there's no AO this will be 1 (since: 1 - (0 * 0.1) == 1)
	// worst case all neighbors are darkening the vertex which yields a 0.7f factor
	float ambientOcclusionFactor = 1.0f - (ambientNeighborCount * 0.1);

    output.Position = mul(position, WorldViewProjection);
	output.TexCoords = float2(textureCoordinateU, textureCoordinateV);

	// 1) sunlight: changes over time, it might be the dominant light source at daylight but falls off to 0 at night
	// 2) block emitted lights: are invariant of time
	// 3) max() => we always take the brightest value from the two
	// 4) smoothFactor: there are 3 neighbors with 0-15 light data;
	//        and the normal-neighbor light data with another 0-15 value
	//        grand total 15*4 = 60 light data which we need to scale down to 0..1 range => factor it with 1/60
	// 5) AO factor:
	const float smoothingFactor = 1.0f / 60.0f;

	// the smoothing factor takes into account the 3 neighbors light data and the normal-neighbor's light data
	output.AmbientOcclusion = max(sunlight, emittedLight) * smoothingFactor * ambientOcclusionFactor;
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
		SetVertexShader( CompileShader( vs_5_0, VertexShaderFunction() ) );
		SetPixelShader( CompileShader( ps_5_0, PixelShaderFunction() ) );
    }
}
