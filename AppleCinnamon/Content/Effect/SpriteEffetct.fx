float4x4 WorldViewProjection;

float FogEnabled = 1;
float3 EyePosition;
float4 FogColor = float4(.5, .5, .5, 1);
float FogStart = 64;
float FogEnd = 256;

Texture2D Textures;

float4 HueColors[] =
{
	float4(1,1,1,1),
	float4(0,0.78,0.277,1),
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


float ComputeFogFactor(float d)
{
	return clamp((d - FogStart) / (FogEnd - FogStart), 0, 1) * FogEnabled;
}

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{

	VertexShaderOutput output = (VertexShaderOutput)0;
	float4 position = float4(input.Position.xyz, 1);

	float u = (input.Asd & 15) * 1.0 / 16.0;                 // 4 bits
	float v = ((input.Asd & 240) >> 4) * 1.0 / 16.0;         // 4 bits
	float l = ((input.Asd & 16128) >> 8) / 60.0f + 0.2f;     // 4 bits
	int h = ((input.Asd >> 12) & 15);
	
	output.Position = mul(position, WorldViewProjection);
	output.TexCoords = float2(u, v);
	output.AmbientOcclusion = l;
	output.FogFactor = ComputeFogFactor(distance(EyePosition.xyz, input.Position.xyz));
	output.HueColor = HueColors[h];

	return output;
}



float4 PixelShaderFunction(VertexShaderOutput input) : SV_Target
{
	float4 textureColor = Textures.Sample(SS, input.TexCoords) * input.AmbientOcclusion * /*float4(1.8, 1.8, 1.8, 1) * */input.HueColor;
	clip(textureColor.a == 0 ? -1 : 1);
	float4 finalColor = (1.0 - input.FogFactor) * textureColor + (input.FogFactor) * FogColor;
	return finalColor;

}

technique10 Render
{
	pass P0
	{
		SetVertexShader(CompileShader(vs_5_0, VertexShaderFunction()));
		SetPixelShader(CompileShader(ps_5_0, PixelShaderFunction()));
	}
}
