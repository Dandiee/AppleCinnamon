float4x4 WorldViewProjection;

float4 FogColor = float4(.5, .5, .5, 1);
float FogStart = 64;
float FogEnd = 256;
float3 EyePosition;

Texture2D Textures;

SamplerState SampleType;


struct VS_IN
{
	float2 Position : POSITION;
	// uint IndexAndLight : RANDOM;
	// uint PositionYVisibilityAmbient : RANDOMKA;
	// uint Ambient : RANDOMOCSKA;
};

struct PS_IN
{
	float4 Position : SV_POSITION;
};


VS_IN VS(VS_IN input )
{
	VS_IN output = (VS_IN)0;

	output.Position = input.Position;
	// output.IndexAndLight = input.IndexAndLight;
	// output.PositionYVisibilityAmbient = input.PositionYVisibilityAmbient;
	// output.Ambient = input.Ambient;

	return output;
}

[maxvertexcount(3)]
void GS(point VS_IN inputs[1], inout TriangleStream<PS_IN> triStream )
{
	VS_IN input = inputs[0];

	// uint j = (input.PositionYVisibilityAmbient & 65280);
	
	float4 pos = float4(0,0,0,0);

	PS_IN output1 = (PS_IN)0;
	PS_IN output2 = (PS_IN)0;
	PS_IN output3 = (PS_IN)0;

	output1.Position = pos + float4(-.5, +.5, -.5, 0);
	output2.Position = pos + float4(+.5, +.5, +.5, 0);
	output3.Position = pos + float4(-.5, +.5, +.5, 0);

	triStream.Append(output1);
	/*triStream.Append(output2);
	triStream.Append(output3);*/

	triStream.RestartStrip();
}

float4 PS(PS_IN input) : SV_Target
{
	return float4(1,0,0,1);

	// float4 textureColor = Textures.Sample(SampleType, input.TexCoords) * input.AmbientOcclusion;   
	// float4 finalColor = (1.0 - input.FogFactor) * textureColor + (input.FogFactor) * FogColor;
	// return finalColor;
}

technique10 Render
{
	pass P0
	{
		SetVertexShader(CompileShader(vs_5_0, VS()));
		SetGeometryShader( CompileShader( gs_5_0, GS() ) );
		SetPixelShader( CompileShader( ps_5_0, PS() ) );
	}
}