struct VS_IN
{
	float3 pos : POSITION;
	float4 col : COLOR;
	uint vis : VISIBLITY;
};

struct PS_IN
{
	float4 pos : SV_POSITION;
	float4 col : COLOR;
	uint vis : VISIBLITY;
};

float4x4 ViewMatrix;
float4x4 ProjectionMatrix;
float4x4 WorldMatrix;
float4x4 VPWMatrix;

PS_IN VS( VS_IN input )
{
	PS_IN output = (PS_IN)0;

	float4 pos = float4(input.pos.xyz, 1);

	output.pos = pos;// mul(pos, worldViewProj);
	output.col = input.col;
	output.vis = input.vis;

	return output;
}

[maxvertexcount(8)]
void GS(point PS_IN input[1], inout LineStream<PS_IN> triStream )
{
	PS_IN q = (PS_IN)0;
	q.col = input[0].col;

	uint visibility = input[0].vis;

	float4 position = input[0].pos;
	
	float4 topLeftFro = position + float4(-0.5,  0.5, -0.5, 1);
    float4 topLeftBac = position + float4(-0.5,  0.5,  0.5, 1);
    float4 topRigtFro = position + float4( 0.5,  0.5, -0.5, 1);
    float4 topRigtBac = position + float4( 0.5,  0.5,  0.5, 1);

	float4 botLeftFro = position + float4(-0.5, -0.5, -0.5, 1);
    float4 botLeftBac = position + float4(-0.5, -0.5,  0.5, 1);
    float4 botRigtFro = position + float4( 0.5, -0.5, -0.5, 1);
    float4 botRigtBac = position + float4( 0.5, -0.5,  0.5, 1);

	uint a0 = (visibility & 4);
	uint a1 = (visibility & 4);
	uint a2 = (visibility & 4);
	uint a3 = (visibility & 4);
	uint a4 = (visibility & 4);
	uint a5 = (visibility & 4);
	uint a6 = (visibility & 4);
	uint a7 = (visibility & 4);

	if (true && a7 > 0)
	{
		// FRONT
		q.pos = mul(topLeftFro, VPWMatrix);
		triStream.Append(q);

		q.pos = mul(topRigtFro, VPWMatrix);
		triStream.Append(q);

		// RIGHT
		q.pos = mul(topRigtFro, VPWMatrix);
		triStream.Append(q);

		q.pos = mul(topRigtBac, VPWMatrix);
		triStream.Append(q);

		// BACK
		q.pos = mul(topRigtBac, VPWMatrix);
		triStream.Append(q);

		q.pos = mul(topLeftBac, VPWMatrix);
		triStream.Append(q);

		// LEFT
		q.pos = mul(topLeftBac, VPWMatrix);
		triStream.Append(q);

		q.pos = mul(topLeftFro, VPWMatrix);
		triStream.Append(q);
	}
	

}

float4 PS( PS_IN input ) : SV_Target
{
	return input.col;
}

technique10 Render
{
	pass P0
	{
		SetGeometryShader( CompileShader( gs_4_0, GS() ) );
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}