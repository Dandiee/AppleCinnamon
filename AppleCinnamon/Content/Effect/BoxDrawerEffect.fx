float4x4 WorldViewProjection;

struct VS_IN
{
	float3 Minimum : POSITION0;
	float3 Maximum : POSITION1;
	float4 Color : COLOR;
};

struct GS_IN
{
	float3 Minimum : POSITION0;
	float3 Maximum : POSITION1;
	float4 Color : COLOR;
};

struct PS_IN
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR;
};


GS_IN VS(VS_IN input)
{
	GS_IN output = (GS_IN)0;

	// output.Minimum = mul(float4(input.Minimum.xyz, 1), WorldViewProjection);
	// output.Maximum = mul(float4(input.Maximum.xyz, 1), WorldViewProjection);

	output.Minimum = input.Minimum;
	output.Maximum = input.Maximum;

	output.Color = input.Color;

	return output;
}

[maxvertexcount(16)]
void GS(point GS_IN input[1], inout LineStream<PS_IN> lineStream)
{
	PS_IN output = (PS_IN)0;
	output.Color = input[0].Color;

	float3 min = input[0].Minimum;
	float3 max = input[0].Maximum;

	// output.Position = float4(min.x, max.y, max.z, 1);
	// lineStream.Append(output);
	// output.Position = float4(max.x, max.y, max.z, 1);
	// lineStream.Append(output);
	// output.Position = float4(max.x, max.y, min.z, 1);
	// lineStream.Append(output);
	// output.Position = float4(min.x, max.y, min.z, 1);
	// lineStream.Append(output);

	output.Position = mul(float4(min.x, max.y, max.z, 1), WorldViewProjection);
	lineStream.Append(output);
	output.Position = mul(float4(max.x, max.y, max.z, 1), WorldViewProjection);
	lineStream.Append(output);
	output.Position = mul(float4(max.x, max.y, min.z, 1), WorldViewProjection);
	lineStream.Append(output);
	output.Position = mul(float4(min.x, max.y, min.z, 1), WorldViewProjection);
	lineStream.Append(output);
	output.Position = mul(float4(min.x, max.y, max.z, 1), WorldViewProjection);
	lineStream.Append(output);

	output.Position = mul(float4(min.x, min.y, max.z, 1), WorldViewProjection);
	lineStream.Append(output);
	output.Position = mul(float4(max.x, min.y, max.z, 1), WorldViewProjection);
	lineStream.Append(output);
	output.Position = mul(float4(max.x, min.y, min.z, 1), WorldViewProjection);
	lineStream.Append(output);
	output.Position = mul(float4(min.x, min.y, min.z, 1), WorldViewProjection);
	lineStream.Append(output);
	output.Position = mul(float4(min.x, min.y, max.z, 1), WorldViewProjection);
	lineStream.Append(output);

	lineStream.RestartStrip();

	output.Position = mul(float4(max.x, min.y, max.z, 1), WorldViewProjection);
	lineStream.Append(output);
	output.Position = mul(float4(max.x, max.y, max.z, 1), WorldViewProjection);
	lineStream.Append(output);

	lineStream.RestartStrip();

	output.Position = mul(float4(min.x, min.y, min.z, 1), WorldViewProjection);
	lineStream.Append(output);
	output.Position = mul(float4(min.x, max.y, min.z, 1), WorldViewProjection);
	lineStream.Append(output);
	lineStream.RestartStrip();

	output.Position = mul(float4(max.x, min.y, min.z, 1), WorldViewProjection);
	lineStream.Append(output);
	output.Position = mul(float4(max.x, max.y, min.z, 1), WorldViewProjection);
	lineStream.Append(output);
}

float4 PS(PS_IN input) : SV_Target
{
	return input.Color;
}

technique10 Render
{
	pass P0
	{

		SetVertexShader(CompileShader(vs_4_0, VS()));
		SetGeometryShader(CompileShader(gs_4_0, GS()));
		SetPixelShader(CompileShader(ps_4_0, PS()));
	}
}