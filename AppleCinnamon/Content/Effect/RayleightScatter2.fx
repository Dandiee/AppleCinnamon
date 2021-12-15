float4x4 WorldViewProjection;
float3 _CameraPos;

float _AtmosphereHeight;
float _PlanetRadius;
float2 _DensityScaleHeight;

float3 _ScatteringR;
float3 _ScatteringM;
float3 _ExtinctionR;
float3 _ExtinctionM;

float4 _IncomingLight;
float _MieG;

float4 _FrustumCorners[4];

float _SunIntensity;

struct appdata
{
	float4 vertex : POSITION;
};

struct v2f
{
	float4	pos		: SV_POSITION;
	float3	vertex	: TEXCOORD0;
};

v2f vert(appdata v)
{
	v2f o;
	o.pos = mul(WorldViewProjection, v.vertex);
	o.vertex = v.vertex;
	return o;
}

float4 frag(v2f i) : SV_Target
{
				float3 rayStart = _CameraPos;
				float3 rayDir = normalize(i.vertex);
				//float3 rayDir = normalize(mul((float3x3)_Object2World, i.vertex));

				float3 lightDir = -_WorldSpaceLightPos0.xyz;

				float3 planetCenter = _CameraPos;
				planetCenter = float3(0, -_PlanetRadius, 0);

				float2 intersection = RaySphereIntersection(rayStart, rayDir, planetCenter, _PlanetRadius + _AtmosphereHeight);
				float rayLength = intersection.y;

				intersection = RaySphereIntersection(rayStart, rayDir, planetCenter, _PlanetRadius);
				if (intersection.x > 0)
					rayLength = min(rayLength, intersection.x);

				float4 extinction;
				float4 inscattering = IntegrateInscattering(rayStart, rayDir, rayLength, planetCenter, 1, lightDir, 16, extinction);
				return float4(inscattering.xyz, 1);


}


technique10 Render
{
	pass P0
	{
		SetVertexShader(CompileShader(vs_5_0, vert()));
		SetPixelShader(CompileShader(ps_5_0, frag()));
	}
}
