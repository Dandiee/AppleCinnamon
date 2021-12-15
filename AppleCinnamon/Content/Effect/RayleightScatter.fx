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

float3 groundCursorPosition;
bool showGroundCursor;

texture terrainAlpha, terrainBreakup, terrainOne, terrainTwo, terrainThree, terrainFour, groundCursor;

sampler samplergroundCursor = sampler_state
{
	Texture = <groundCursor>;
	ADDRESSU = CLAMP;
	ADDRESSV = CLAMP;
	FILTER = MIN_MAG_POINT_MIP_LINEAR;
};

sampler samplerAlpha = sampler_state
{
	texture = <terrainAlpha>;
	FILTER = ANISOTROPIC;
};

sampler samplerOne = sampler_state
{
	texture = <terrainOne>;
	FILTER = ANISOTROPIC;
	ADDRESSU = WRAP;
	ADDRESSV = WRAP;
};

sampler samplerTwo = sampler_state
{
	texture = <terrainTwo>;
	FILTER = ANISOTROPIC;
	ADDRESSU = WRAP;
	ADDRESSV = WRAP;
};

sampler samplerThree = sampler_state
{
	texture = <terrainThree>;
	FILTER = ANISOTROPIC;
	ADDRESSU = WRAP;
	ADDRESSV = WRAP;
};

sampler samplerFour = sampler_state
{
	texture = <terrainFour>;
	FILTER = ANISOTROPIC;
	ADDRESSU = WRAP;
	ADDRESSV = WRAP;
};

float4 constants = { 0.25f, 1.4426950f, 0.5f, 0.0f };

struct VS_INPUT
{
	float4 Position : POSITION0;
	float3 Normal : NORMAL;
	float2 TexCoord : TEXCOORD0;
};

struct VS_OUTPUT
{
	float4 Position     : SV_POSITION;
	float2 TerrainCoord : TEXCOORD0;
	float3 Normal  : TEXCOORD1;
	float3 Lin          : COLOR0;
	float3 Fex          : COLOR1;
};

VS_OUTPUT HoffmanShader(VS_INPUT Input)
{
	float4 worldPos = mul(Input.Position, worldView);
	float3 viewDir = normalize(worldPos.xyz);
	float distance = length(worldPos.xyz);

	float3 sunDir = normalize(mul(float4(sunDirection, 0.0), worldView).xyz);

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

	VS_OUTPUT Output;
	Output.Position = mul(Input.Position, worldViewProject);
	Output.TerrainCoord = Input.TexCoord.xy;
	Output.Normal = Input.Normal;
	Output.Lin = inscatter;
	Output.Fex = totalExtinction;

	return Output;
};

struct PS_INPUT
{
	float4 Position : SV_POSITION;
	float2 TerrainCoord : TEXCOORD0;
	float3 Normal : TEXCOORD1;
	float3 Lin : COLOR0;
	float3 Fex : COLOR1;
};

float4 SkyShader(PS_INPUT Input) : SV_Target
{
 return float4(Input.Lin, 1.0f);
};

float4 TerrainShader(PS_INPUT Input) : SV_Target
{
 Input.Normal = normalize(Input.Normal);

 vector alphaSamp = tex2D(samplerAlpha, Input.TerrainCoord);
 vector oneSamp = tex2D(samplerOne, float2(Input.TerrainCoord.x * 2048, Input.TerrainCoord.y * 2048));
 vector twoSamp = tex2D(samplerTwo, float2(Input.TerrainCoord.x * 2048, Input.TerrainCoord.y * 2048));
 vector threeSamp = tex2D(samplerThree, float2(Input.TerrainCoord.x * 2048, Input.TerrainCoord.y * 2048));
 vector fourSamp = tex2D(samplerFour, float2(Input.TerrainCoord.x * 2048, Input.TerrainCoord.y * 2048));

 float4 tester1 = 1.0 - alphaSamp.a;
 float4 tester2 = 1.0 - alphaSamp.b;
 float4 tester3 = 1.0 - alphaSamp.g;
 float4 tester4 = 1.0 - alphaSamp.r;

 float4 tester = lerp(threeSamp, oneSamp, saturate(dot(float3(0, 1, 0), Input.Normal) * 2));

 vector l = alphaSamp.a * oneSamp + tester1 * tester;
 vector m = alphaSamp.b * twoSamp + tester2 * l;
 vector o = alphaSamp.g * threeSamp + tester3 * m;
 vector p = alphaSamp.r * fourSamp + tester4 * o;

 float4 albedo = saturate((dot(normalize(sunDirection), Input.Normal) + .9f)) * p;

 albedo *= float4(Input.Fex, 1.0f);
 albedo += float4(Input.Lin, 1.0f);

 return albedo;
};

float4 TerrainShaderWithCursor(PS_INPUT Input) : SV_Target
{
 Input.Normal = normalize(Input.Normal);

 vector alphaSamp = tex2D(samplerAlpha, Input.TerrainCoord);
 vector oneSamp = tex2D(samplerOne, float2(Input.TerrainCoord.x * 2048, Input.TerrainCoord.y * 2048));
 vector twoSamp = tex2D(samplerTwo, float2(Input.TerrainCoord.x * 2048, Input.TerrainCoord.y * 2048));
 vector threeSamp = tex2D(samplerThree, float2(Input.TerrainCoord.x * 2048, Input.TerrainCoord.y * 2048));
 vector fourSamp = tex2D(samplerFour, float2(Input.TerrainCoord.x * 2048, Input.TerrainCoord.y * 2048));

 float4 tester1 = 1.0 - alphaSamp.a;
 float4 tester2 = 1.0 - alphaSamp.b;
 float4 tester3 = 1.0 - alphaSamp.g;
 float4 tester4 = 1.0 - alphaSamp.r;

 float4 tester0 = lerp(threeSamp, oneSamp, saturate(dot(float3(0, 1, 0), Input.Normal) * 2));

 vector l = alphaSamp.a * oneSamp + tester1 * tester0;
 vector m = alphaSamp.b * twoSamp + tester2 * l;
 vector o = alphaSamp.g * threeSamp + tester3 * m;
 vector p = alphaSamp.r * fourSamp + tester4 * o;

 float4 albedo = saturate((dot(normalize(sunDirection), Input.Normal) + .9f)) * p;

 albedo *= float4(Input.Fex, 1.0f);
 albedo += float4(Input.Lin, 1.0f);

 if (showGroundCursor)
 {
  float cursorScale = 40.0f;
  albedo += tex2D(samplergroundCursor,
   (Input.TerrainCoord * (cursorScale)) -
   (groundCursorPosition.xz * (cursorScale)) + 0.5f);
 }

 return albedo;
};

float4 Wireframe(PS_INPUT Input) : SV_Target
{
	return float4(1, 1, 1, 1);
};

technique10 Render
{
	pass P0
	{
		SetVertexShader(CompileShader(vs_5_0, HoffmanShader()));
		SetPixelShader(CompileShader(ps_5_0, SkyShader()));
	}
}
