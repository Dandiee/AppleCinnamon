struct VS_IN
{
	uint KEdvencedSzined : POSITION0;
	uint KJEdvencDoggo : POSITION1;
};

struct PS_IN
{
	float4 TorpPosition : SV_POSITION;
};


PS_IN Torp_VS()
{
	PS_IN output = (PS_IN)0;

	output.TorpPosition = float4(1, 1, 1, 1);

	return output;
}

float4 Torp_PS(PS_IN input) : SV_Target
{
	return float(1, 0, 0, 1);
}

technique Render
{
	pass P0
	{
		SetVertexShader(CompileShader(vs_5_0, Torp_VS())); // entry point
		SetPixelShader(CompileShader(ps_5_0, Torp_PS()));
	}
}
