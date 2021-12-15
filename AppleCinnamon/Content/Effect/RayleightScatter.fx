float4x4 worldViewProject;
float4x4 worldView;

float3 sunDirection;
float3 betaRPlusBetaM;
float3 hGg;
float3 betaDashR;
float3 betaDashM;
float3 oneOverBetaRPlusBetaM;
float4 multipliers;
float4 sunColorAndIntensity;
float4 constants = { 0.25f, 1.4426950f, 0.5f, 0.0f };

struct VS_INPUT
{
	float4 Position : POSITION0;
	float3 Normal : NORMAL;
	float2 TexCoord : TEXCOORD0;
};


struct PS_INPUT
{
	float4 Position : SV_POSITION;
	float2 TerrainCoord : TEXCOORD0;
	float3 Normal : TEXCOORD1;
	float3 Lin : COLOR0;
	float3 Fex : COLOR1;
};

PS_INPUT HoffmanShader(VS_INPUT Input)
{
	float4 worldPos = mul(Input.Position, worldView);
	float3 viewDir = normalize(worldPos.xyz);
	float distance = length(worldPos.xyz);

	float3 sunDir = normalize(mul(float4(sunDirection, 1), worldView).xyz);

	float theta = dot(sunDir, viewDir);

	// 
	// Phase1 and Phase2
	//

	float phase1 = 1.0 + theta * theta;
	float phase2 = pow(rsqrt(hGg.y - hGg.z * theta), 3) * hGg.x;

	//
	// Extinction term
	//

	float3 extinction = exp(-betaRPlusBetaM * distance * constants.x);
	float3 totalExtinction = extinction * multipliers.yzw;

	//
	// Inscattering term
	//

	float3 betaRay = betaDashR * phase1;
	float3 betaMie = betaDashM * phase2;

	float3 inscatter = (betaRay + betaMie) * oneOverBetaRPlusBetaM * (1.0 - extinction);

	//
	// Apply inscattering contribution factors
	//

	inscatter *= multipliers.x;
	//
	// Scale with sun color & intensity
	//

	inscatter *= sunColorAndIntensity.xyz * sunColorAndIntensity.w;
	totalExtinction *= sunColorAndIntensity.xyz * sunColorAndIntensity.w;

	PS_INPUT Output;
	Output.Position = mul(Input.Position, worldViewProject);
	Output.TerrainCoord = Input.TexCoord.xy;
	Output.Normal = Input.Normal;
	Output.Lin = inscatter;
	Output.Fex = totalExtinction;

	return Output;
};



float4 SkyShader(PS_INPUT Input) : SV_Target
{
 return float4(Input.Lin, 1.0f);
};

technique10 Render
{
	pass P0
	{
		SetVertexShader(CompileShader(vs_5_0, HoffmanShader()));
		SetPixelShader(CompileShader(ps_5_0, SkyShader()));
	}
}
