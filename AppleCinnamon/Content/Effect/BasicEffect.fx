struct VS_IN
{
	float3 Position : POSITION0;
	float3 Color : TEXCOORD;
};

struct PS_IN
{
	float4 Position : SV_POSITION;
	float3 Color : COLOR;
};

float4x4 WorldViewProjection;

PS_IN VS( VS_IN input )
{
	PS_IN output = (PS_IN)0;

	float4x4 worldViewProj = WorldViewProjection;
	float4 position = float4(input.Position.xyz, 1);
	output.Position = mul(position, worldViewProj);
	
	output.Color = input.Color;

	return output;
}

float4 PS( PS_IN input ) : SV_Target
{
	
	return float4(input.Color.xyz, 1);
}

technique10 Render
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
