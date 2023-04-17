using System;
using AppleCinnamon.Extensions;
using AppleCinnamon.Vertices;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using Device = SharpDX.Direct3D11.Device;
using Vector3 = SharpDX.Vector3;
using Vector4 = SharpDX.Vector4;

namespace AppleCinnamon.Drawers
{
    public class SkyDomeEffect
    {
        private static readonly Vector3 BetaRay;
        private static readonly Vector3 BetaDashRay;
        private static readonly Vector3 BetaMie;
        private static readonly Vector3 BetaDashMie;
        private static readonly Vector3 Lambda = new(0.65f, 0.57f, 0.475f);
        private static readonly Vector3 LambdaTauR;
        private static readonly Vector3 LambdaAlpha;
        private static readonly Vector3 HGg = new(0.9f, 0.9f, 0.9f);

        public readonly EffectDefinition<VertexSkyBox> EffectDefinition;

        private readonly EffectVectorVariable _sunDirectionVar;
        private readonly EffectVectorVariable _betaRPlusBetaMVar;
        private readonly EffectVectorVariable _hGgVar;
        private readonly EffectVectorVariable _betaDashRVar;
        private readonly EffectVectorVariable _betaDashMVar;
        private readonly EffectVectorVariable _oneOverBetaRPlusBetaMVar;
        private readonly EffectVectorVariable _multipliersVar;
        private readonly EffectVectorVariable _sunColorAndIntensityVar;
        private readonly EffectMatrixVariable _worldViewProjectVar;
        private readonly EffectMatrixVariable _worldViewVar;

        public SkyDomeEffect(Device device)
        {
            EffectDefinition = new(device, "Content/Effect/RayleightScatter.fx", PrimitiveTopology.TriangleList);

            _sunDirectionVar = EffectDefinition.Effect.GetVariableByName("sunDirection").AsVector();
            _betaRPlusBetaMVar = EffectDefinition.Effect.GetVariableByName("betaRPlusBetaM").AsVector();
            _hGgVar = EffectDefinition.Effect.GetVariableByName("hGg").AsVector();
            _betaDashRVar = EffectDefinition.Effect.GetVariableByName("betaDashR").AsVector();
            _betaDashMVar = EffectDefinition.Effect.GetVariableByName("betaDashM").AsVector();
            _oneOverBetaRPlusBetaMVar = EffectDefinition.Effect.GetVariableByName("oneOverBetaRPlusBetaM").AsVector();
            _multipliersVar = EffectDefinition.Effect.GetVariableByName("multipliers").AsVector();
            _sunColorAndIntensityVar = EffectDefinition.Effect.GetVariableByName("sunColorAndIntensity").AsVector();
            _worldViewProjectVar = EffectDefinition.Effect.GetVariableByName("worldViewProject").AsMatrix();
            _worldViewVar = EffectDefinition.Effect.GetVariableByName("worldView").AsMatrix();

            UpdateDetails();
        }

        

        public void Update(Camera camera)
        {
            _worldViewProjectVar.SetMatrix(camera.WorldViewProjection);
            _worldViewVar.SetMatrix(camera.View);
        }

        static SkyDomeEffect()
        {
            const float n = 1.0003f;
            const float N = 2.545e25f;
            const float pn = 0.035f;

            var lambda = new float[3];
            var lambda2 = new float[3];
            var lambda4 = new float[3];

            lambda[0] = 1.0f / 650e-9f;   // red
            lambda[1] = 1.0f / 570e-9f;   // green
            lambda[2] = 1.0f / 475e-9f;   // blue

            for (var i = 0; i < 3; ++i)
            {
                lambda2[i] = lambda[i] * lambda[i];
                lambda4[i] = lambda2[i] * lambda2[i];
            }

            var vLambda2 = new Vector3(lambda2[0], lambda2[1], lambda2[2]);
            var vLambda4 = new Vector3(lambda4[0], lambda4[1], lambda4[2]);

            // Rayleigh scattering constants
            const float temp = (float)(Math.PI * Math.PI * (n * n - 1.0f) * (n * n - 1.0f) * (6.0f + 3.0f * pn) / (6.0f - 7.0f * pn) / N);
            const float beta = (float)(8.0f * temp * Math.PI / 3.0f);

            BetaRay = beta * vLambda4;
            const float betaDash = temp / 2.0f;
            BetaDashRay = betaDash * vLambda4;

            // Mie scattering constants
            const float T = 2.0f;
            const float c = (6.544f * T - 6.51f) * 1e-17f;
            const float temp2 = (float)(0.434f * c * (2.0f * Math.PI) * (2.0f * Math.PI) * 0.5f);

            BetaDashMie = temp2 * vLambda2;
            var K = new float[3] { 0.685f, 0.679f, 0.670f };
            const float temp3 = (float)(0.434f * c * Math.PI * (2.0f * Math.PI) * (2.0f * Math.PI));

            var vBetaMieTemp = new Vector3(K[0] * lambda2[0], K[1] * lambda2[1], K[2] * lambda2[2]);
            BetaMie = temp3 * vBetaMieTemp;
            LambdaTauR = 0.008735f * Lambda.Pow(-4.08f);
            const float alpha = 1.3f;
            LambdaAlpha = Lambda.Pow(alpha);
        }

        public void UpdateDetails()
        {
            var sunPosition = Vector3.UnitZ.Rotate(-Vector3.UnitX, SkyDomeOptions.TimeOfDay * MathUtil.Pi);
            var thetaS = (float)Math.Acos(Vector3.Dot(sunPosition, Vector3.UnitY));
            var beta = 0.04608365822050f * SkyDomeOptions.Turbitity - 0.04586025928522f;

            // Relative Optical Mass
            var m = (float)(1.0f / (Math.Cos(thetaS) + 0.15f * Math.Pow(93.885f - thetaS / Math.PI * 180.0f, -1.253f)));

            // Rayleigh Scattering lambda in um.
            var fTauR = (LambdaTauR * -m).Exp();

            // Aerosal (water + dust) attenuation
            // beta - amount of aerosols present
            // alpha - ratio of small to large particle sizes. (0:4,usually 1.3)
            var fTauA = (LambdaAlpha * -m * beta).Exp();
            var fTauVector = fTauR * fTauA;

            const float reflectance = 0.1f;

            var betaRPlusBetaM = BetaRay * SkyDomeOptions.BetaRayMultiplier + BetaMie * SkyDomeOptions.BetaMieMultiplier;

            _hGgVar.Set(HGg);

            _sunDirectionVar.Set(sunPosition);
            _betaRPlusBetaMVar.Set(betaRPlusBetaM);
            _betaDashRVar.Set(BetaDashRay * SkyDomeOptions.BetaRayMultiplier);
            _betaDashMVar.Set(BetaDashMie * SkyDomeOptions.BetaMieMultiplier);
            _oneOverBetaRPlusBetaMVar.Set(1.0f / betaRPlusBetaM);
            _multipliersVar.Set(new Vector4(SkyDomeOptions.InscatteringMultiplier, 0.138f * reflectance, 0.113f * reflectance, 0.08f * reflectance));
            _sunColorAndIntensityVar.Set(new Vector4(fTauVector.X, fTauVector.Y, fTauVector.Z, SkyDomeOptions.SunIntensity * 100.0f));
        }
    }

    public static class SkyDomeOptions
    {
        public static float SunIntensity = 1.0f;
        public static float Turbitity = 1.0f;
        public static float InscatteringMultiplier = 1.0f;
        public static float BetaRayMultiplier = 8.0f;
        public static float BetaMieMultiplier = 0.00005f;
        public static float TimeOfDay = 0.5f;
        public static int Resolution = 64;
        public static float Radius = 100;

        public static void IncrementTime(float step)
        {
            var overflow = 1.0f - (TimeOfDay + step);
            if (overflow < 0)
            {
                TimeOfDay = -1.0f - overflow;
            }
            else
            {
                TimeOfDay += step;
            }
        }

        public static void DecrementTime(float step)
        {
            var overflow = 1.0f + (TimeOfDay + step);
            if (overflow < 0)
            {
                TimeOfDay = 1.0f + overflow;
            }
            else
            {
                TimeOfDay -= step;
            }
        }
    }
}
