
float4x4 WorldViewProjection;

float InvOpticalDepthNLessOne = 1.0f/255.0f;
float InvOpticalDepthN = 1.0f/256.0f;
float InnerRadius = 6356.7523142;
float OuterRadius = 6356.7523142 * 1.0157313;
float PI = 3.1415159;
float NumSamples = 20;
float fScale = 1.0 / (6356.7523142 * 1.0157313 - 6356.7523142);
float2 v2dRayleighMieScaleHeight = {0.25, 0.1};

float2 InvRayleighMieNLessOne = {1.0f/255.0f, 1.0f/127.0f};
float3 v3SunDir = { 0.71, 0.71, 0 };
//float ESun = 20.0f;
//float Kr = 0.0025f;
//float Km = 0.0010f;
float KrESun = 0.0025f * 20.0f;
float KmESun = 0.0010f * 20.0f;
float Kr4PI = 0.0025f * 4.0f * 3.1415159;
float Km4PI = 0.0010f * 4.0f * 3.1415159;

float g = -0.990;
float g2 = (-0.990) * (-0.990);
float3 v3HG;
float fExposure = -2.0;

float3 InvWavelength;
float3 WavelengthMie;

float starIntensity = 0.5f;

Texture2D txRayleigh;
SamplerState rayleighSampler
{
	Texture = <txRayleigh>;
    ADDRESSU = CLAMP;
	ADDRESSV = CLAMP;
	FILTER = MIN_MAG_MIP_LINEAR;
};

Texture2D txMie;
SamplerState mieSampler
{
	Texture = <txMie>;
    ADDRESSU = CLAMP;
	ADDRESSV = CLAMP;
	FILTER = MIN_MAG_MIP_LINEAR;
};

Texture2D StarsTex;
SamplerState starSampler
{
	Texture = <StarsTex>;
    ADDRESSU = CLAMP;
	ADDRESSV = CLAMP;
	FILTER = MIN_MAG_MIP_LINEAR;
};


struct VS_INPUT_UPDATE
{
    float4 Pos		: POSITION0;
    float4 TexCoord : TEXCOORD0;
};

struct VS_INPUT
{
    float3 Pos	: POSITION0;
    float2 Tex0 : TEXCOORD0;
};

struct PS_INPUT_UPDATE
{
    float4 Position : SV_POSITION;
    float2 Pos		: TEXCOORD0;
};

struct PS_INPUT
{
    float4 Pos  : SV_POSITION;
    float2 Tex0 : TEXCOORD0;
	float3 Tex1 : TEXCOORD1;
};

PS_INPUT_UPDATE VS_UPDATE( VS_INPUT_UPDATE input )
{
    PS_INPUT_UPDATE output = (PS_INPUT_UPDATE)0;
    output.Position = input.Pos;
	output.Pos = input.TexCoord;
    return output;
}

PS_INPUT VS( VS_INPUT input )
{
    PS_INPUT output = (PS_INPUT)0;
	output.Pos =  mul( float4(input.Pos,1), WorldViewProjection );
    output.Tex0 = input.Tex0; 
    output.Tex1 = - input.Pos;
    return output;
}

float getRayleighPhase(float fCos2)
{
	return 0.75 * (1.0 + fCos2);
}

float getMiePhase(float fCos, float fCos2)
{
	float3 q;

	q.x = 1.5f * ( (1.0f - g2) / (2.0f + g2) );
	q.y = 1.0f + g2;
	q.z = 2.0f * g;
	return q.x * (1.0 + fCos2) / pow(q.y - q.z * fCos, 1.5);
}

float3 HDR( float3 LDR)
{
	return 1.0f - exp( fExposure * LDR );
}


float4 PS(PS_INPUT input) : SV_Target
{
	//return float4(1,0,0,1);
	float fCos = dot( v3SunDir, input.Tex1 ) / length( input.Tex1 );
	float fCos2 = fCos * fCos;
	
	float3 v3RayleighSamples = txRayleigh.Sample(rayleighSampler, input.Tex0 );
	float3 v3MieSamples = txMie.Sample(mieSampler, input.Tex0.xy );

	float3 Color;
	Color.rgb = getRayleighPhase(fCos2) * v3RayleighSamples.rgb + getMiePhase(fCos, fCos2) * v3MieSamples.rgb;
	Color.rgb = HDR( Color.rgb );
	
	// Hack Sky Night Color
	Color.rgb += max(0,(1 - Color.rgb)) * float3( 0.05, 0.05, 0.1 ); 

	return float4( Color.rgb, 1 ) + StarsTex.Sample(starSampler, input.Tex0) * starIntensity;
}

float HitOuterSphere( float3 O, float3 Dir ) 
{
	float3 L = -O;

	float B = dot( L, Dir );
	float C = dot( L, L );
	float D = C - B * B; 
	float q = sqrt( OuterRadius * OuterRadius - D );
	float t = B;
	t += q;

	return t;
}

float2 GetDensityRatio( float fHeight )
{
	const float fAltitude = (fHeight - InnerRadius) * fScale;
	return exp( -fAltitude / v2dRayleighMieScaleHeight.xy );
}

float2 t( float3 P, float3 Px )
{
	float2 OpticalDepth = 0;

	float3 v3Vector =  Px - P;
	float fFar = length( v3Vector );
	float3 v3Dir = v3Vector / fFar;
			
	float fSampleLength = fFar / NumSamples;
	float fScaledLength = fSampleLength * fScale;
	float3 v3SampleRay = v3Dir * fSampleLength;
	P += v3SampleRay * 0.5f;
			
	for(int i = 0; i < NumSamples; i++)
	{
		float fHeight = length( P );
		OpticalDepth += GetDensityRatio( fHeight );
		P += v3SampleRay;
	}		

	OpticalDepth *= fScaledLength;
	return OpticalDepth;
}



struct PS_OUTPUT_UPDATE
{
    float4 RayLeigh : COLOR0;
	float4 Mie		: COLOR1;
};

PS_OUTPUT_UPDATE PS_UPDATE( PS_INPUT_UPDATE input)
{
	PS_OUTPUT_UPDATE output = (PS_OUTPUT_UPDATE)0;
	
	float2 Tex0 = (input.Pos);
	 
	const float3 v3PointPv = float3( 0, InnerRadius + 1e-3, 0 );
	const float AngleY = 100.0 * Tex0.x * PI / 180.0;
	const float AngleXZ = PI * Tex0.y;
	
	float3 v3Dir;
	v3Dir.x = sin( AngleY ) * cos( AngleXZ  );
	v3Dir.y = cos( AngleY );
	v3Dir.z = sin( AngleY ) * sin( AngleXZ  );
	v3Dir = normalize( v3Dir );

	float fFarPvPa = HitOuterSphere( v3PointPv , v3Dir );
	float3 v3Ray = v3Dir;

	float3 v3PointP = v3PointPv;
	float fSampleLength = fFarPvPa / NumSamples;
	float fScaledLength = fSampleLength * fScale;
	float3 v3SampleRay = v3Ray * fSampleLength;
	v3PointP += v3SampleRay * 0.5f;
				
	float3 v3RayleighSum = 0;
	float3 v3MieSum = 0;

	for( int k = 0; k < NumSamples; k++ )
	{
		float PointPHeight = length( v3PointP );

		float2 DensityRatio = GetDensityRatio( PointPHeight );
		DensityRatio *= fScaledLength;

		float2 ViewerOpticalDepth = t( v3PointP, v3PointPv );
						
		float dFarPPc = HitOuterSphere( v3PointP, v3SunDir );
		float2 SunOpticalDepth = t( v3PointP, v3PointP + v3SunDir * dFarPPc );

		float2 OpticalDepthP = SunOpticalDepth.xy + ViewerOpticalDepth.xy;
		float3 v3Attenuation = exp( - Kr4PI * InvWavelength * OpticalDepthP.x - Km4PI * OpticalDepthP.y );

		v3RayleighSum += DensityRatio.x * v3Attenuation;
		v3MieSum += DensityRatio.y * v3Attenuation;

		v3PointP += v3SampleRay;
	}

	float3 RayLeigh = v3RayleighSum * KrESun;
	float3 Mie = v3MieSum * KmESun;
	RayLeigh *= InvWavelength;
	Mie *= WavelengthMie;
	
	output.RayLeigh = float4( RayLeigh, 1 );
	output.Mie = float4( Mie, 1 );
	return output;
}


technique10 Render
{
	pass P0
	{
		SetVertexShader(CompileShader(vs_5_0, VS()));
		SetPixelShader(CompileShader(ps_5_0, PS()));
	}
}


//technique10 Render
//{
//	pass P0
//	{
//		SetVertexShader(CompileShader(vs_5_0, VS_UPDATE()));
//		SetPixelShader(CompileShader(ps_5_0, PS_UPDATE()));
//	}
//}
