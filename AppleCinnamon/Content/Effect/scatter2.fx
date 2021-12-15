//Copyright � 2014 Sony Computer Entertainment America LLC. See License.txt.

//-----------------------------------------------             
// atmospheric scattering
// todo use either cpu or gpu to compute Mie and Rayleigh lookup texture on demand.
//      for example when sun position changes.
//-----------------------------------------------             

cbuffer cbRender  : register(b0)
{
	float4x4 WorldViewProjection;
	float4x4 proj;
	float3 v3SunDir;
	float fExposure;
};

cbuffer cbFixed : register(b1)
{
	float PI = 3.141592653;
	float InnerRadius = 6356.7523142;
	float OuterRadius = 6356.7523142 * 1.015;
	float fScale = 1.0 / (6452.103598913 - 6356.7523142);
	float KrESun = 0.0025 * 30.0;
	float KmESun = 0.0010 * 30.0;
	float Kr4PI = 0.0025 * 4.0 * 3.141592653;
	float Km4PI = 0.0010 * 4.0 * 3.141592653;
	int tNumSamples = 50;
	int iNumSamples = 20;
	float2 v2dRayleighMieScaleHeight = float2(0.25, 0.1);
	float3 WavelengthMie = float3(pow(0.650, -0.84), pow(0.570, -0.84), pow(0.475, -0.84));
	float InvOpticalDepthN = 1.0 / 128.0;
	float3 v3HG = float3(1.5f * ((1.0f - (-0.995 * -0.995)) / (2.0f + (-0.995 * -0.995))), 1.0f + (-0.995 * -0.995), 2.0f * -0.995);
	float InvOpticalDepthNLessOne = 1.0 / (128.0 - 1.0);
	float2 InvRayleighMieN = float2(1.0 / 128.0, 1.0 / 64.0);
	float2 InvRayleighMieNLessOne = float2(1.0 / (128.0 - 1.0), 1.0 / (64.0 - 1.0));
	float HalfTexelOpticalDepthN = float(0.5 / 128.0);
	float3 InvWavelength4 = float3(1.0 / pow(0.650, 4), 1.0 / pow(0.570, 4), 1.0 / pow(0.475, 4));

}

struct VS_RenderOut
{
	float4 posH : SV_POSITION;
	float2 tex0 : TEXCOORD0;
	float3 tex1 : TEXCOORD1;
};

struct VS_RenderIn
{
	float4 pos  : POSITION;
	float2 tex0 : TEXCOORD0;
};


Texture2D	txMie : register(t0);
Texture2D	txRayleigh : register(t1);

SamplerState samplinearClampMie : register(s0)
{
	Texture = <txMie>;
	AddressU = Clamp;
	AddressV = Clamp;
	AddressW = Clamp;
	Filter = MIN_MAG_MIP_LINEAR;
};

SamplerState samplinearClampRay : register(s1)
{
	Texture = <txRayleigh>;
	AddressU = Clamp;
	AddressV = Clamp;
	AddressW = Clamp;
	Filter = MIN_MAG_MIP_LINEAR;
};


float3 HDR(float3 LDR)
{
	return 1.0f - exp(fExposure * LDR);
}
float3 ToneMap(float3 HDR)
{
	return (HDR / (HDR + 1.0f));
}



float getMiePhase(float fCos, float fCos2)
{
	return v3HG.x * (1.0 + fCos2) / pow(v3HG.y - v3HG.z * fCos, 1.5);
}

float getRayleighPhase(float fCos2)
{
	return 0.75 * (1.0 + fCos2);
}

VS_RenderOut VS_Render(VS_RenderIn input)
{
	VS_RenderOut  output;
	output.posH = mul(float4(input.pos.xyz, 1), WorldViewProjection);
	output.tex0 = input.tex0;
	output.tex1 = -input.pos.xyz;
	return output;
}

float4 PS_Render(VS_RenderOut input) : SV_Target
{
	//return float4(1,0,0,1);

   float fCos = dot(v3SunDir, input.tex1) / length(input.tex1);
   float fCos2 = fCos * fCos;

   float3 v3RayleighSamples = txRayleigh.Sample(samplinearClampRay, input.tex0).xyz;
   float3 v3MieSamples = txMie.Sample(samplinearClampMie, input.tex0).xyz;

   float3 Color;
   Color.rgb = getRayleighPhase(fCos2) * v3RayleighSamples.rgb + getMiePhase(fCos, fCos2) * v3MieSamples.rgb;
   Color.rgb = HDR(Color.rgb);

   // Soft Night
   Color.rgb += max(0,(1 - Color.rgb)) * float3(0, 0, 0.2);
   return float4(Color.rgb, 1);
}

technique10 Render
{
	pass P0
	{
		SetVertexShader(CompileShader(vs_5_0, VS_Render()));
		SetPixelShader(CompileShader(ps_5_0, PS_Render()));
	}
}
