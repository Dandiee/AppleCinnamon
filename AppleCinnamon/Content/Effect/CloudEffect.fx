float4x4 WorldViewProjection;
float4 SunAmbientColor;
float3 SunDirection;
float3 SunDiffuseColor;

struct VS_INPUT
{
	float3 Position : POSITION0;
	float3 Normal : NORMAL;
};


struct PS_INPUT
{
	float4 Position : SV_POSITION;
	float3 Normal : TEXCOORD1;
};

PS_INPUT HoffmanShader(VS_INPUT input)
{
	PS_INPUT output = (PS_INPUT)0;
	float4 position = float4(input.Position.xyz, 1);
	output.Position = mul(position, WorldViewProjection);
	output.Normal = input.Normal;
	return output;
};



float4 SkyShader(PS_INPUT input) : SV_Target
{
	float4 diffuse = float4(1.0f, 1.0f, 1.0f, 1.0f);
	float3 finalColor = diffuse * SunAmbientColor;
	float myDot = abs(dot(SunDirection, input.Normal));
	if (input.Normal.y < -0.5f)
	{
		myDot = dot(SunDirection, input.Normal);
	}
	finalColor += saturate(myDot * SunDiffuseColor * diffuse);
	return float4(finalColor, diffuse.a * .5f);
};

technique10 Render
{
	pass P0
	{
		SetVertexShader(CompileShader(vs_5_0, HoffmanShader()));
		SetPixelShader(CompileShader(ps_5_0, SkyShader()));
	}
}
