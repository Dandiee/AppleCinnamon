
float4x4 WorldIT;
float4x4 WorldViewProj;
float4x4 World;
float4x4 ViewInv;

struct VS_INPUT 
{
	float3 Position				: POSITION;
	float4 Normal				: NORMAL;
	float2 TexCoord				: TEXCOORD0;
};

struct VS_OUTPUT 
{
	float4 HPosition	 		: POSITION0;
	float3 WorldLightVec		: TEXCOORD0;
	float3 WorldNormal	    	: TEXCOORD1;
	float2 TexCoord				: TEXCOORD2;
  	half Fog 					: TEXCOORD3;
};

float4 LightDirection = {100.0f, 100.0f, 100.0f, 1.0f};
float4 LightColor = {1.0f, 1.0f, 1.0f, 1.0f};
float4 LightColorAmbient = {0.0f, 0.0f, 0.0f, 1.0f};

float4 FogColor = {1.0f, 1.0f, 1.0f, 1.0f};

float FogDensity ;

texture DiffuseTexture;

sampler SurfSamplerDiffuse = sampler_state
{
	Texture = <DiffuseTexture>;
	MinFilter = Linear;
	MipFilter = Linear;
	MagFilter = Linear;
	ADDRESSU = WRAP;
	ADDRESSV = WRAP;
};

VS_OUTPUT VS (VS_INPUT input)
{
	VS_OUTPUT output;
	float4 Po = float4(input.Position.xyz,1);
	output.HPosition = mul( Po, WorldViewProj);
	
	output.WorldNormal = mul( input.Normal, WorldIT).xyz;
	output.WorldLightVec = LightDirection;
	
	float3 Pw = mul( Po, World).xyz;
	float3 WorldEyeDirection = ViewInv[3].xyz - Pw;
	
	float4 pos = mul( input.Position, World);
  
 	float dist = length(WorldEyeDirection);
	output.Fog = (1.f/exp(pow(dist * FogDensity, 2)));

	output.TexCoord = input.TexCoord;
	return output;
}

float4 PS(VS_OUTPUT input) : COLOR0
{	
	float4 colorOutput = float4(0,0,0,1);
	float4 DiffuseColor = tex2D( SurfSamplerDiffuse, float2( input.TexCoord.x, 1-input.TexCoord.y));
	float4 colorAmbient = DiffuseColor;
		
	float3 normal = normalize(input.WorldNormal);
	float3 lightVec = normalize(input.WorldLightVec);
	
	float NdotL = max( dot( normal, -lightVec), 0);
	
	float4 colorDiffuse  = DiffuseColor * (NdotL * LightColor) + LightColorAmbient * DiffuseColor;
	colorOutput += colorDiffuse;		
	colorOutput.a = 1.0f;
    
	colorOutput = lerp(FogColor, colorOutput, input.Fog);
	
	return colorOutput;
}


technique SkyDome 
{
	pass Pass1
	{
		CullMode = None;
		VertexShader = compile vs_2_0 VS();
		PixelShader = compile ps_2_b PS();
		CullMode = CCW;
	}
}
