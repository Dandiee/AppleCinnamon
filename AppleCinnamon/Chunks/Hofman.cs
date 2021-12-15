﻿using System;
using AppleCinnamon.Vertices;
using SharpDX;

namespace AppleCinnamon.Chunks
{
    class Hofman
    {
        public Vector3 Position { get; set; }

        private float _sunDirection = 0;
        public float SunDirection
        {
            get => _sunDirection;
            set
            {
                _sunDirection = value;
                if (_sunDirection > 180.0f)
                    _sunDirection = 0;
                else if (_sunDirection < 0.0f)
                    _sunDirection = 0;

                Position = new Vector3(0.0f, (float)Math.Sin(MathUtil.DegreesToRadians(value)), (float)Math.Cos(MathUtil.DegreesToRadians(value)));
            }
        }

        public float SunIntensity { get; set; } = 1.0f;

        public float Turbitity { get; set; } = 1.0f;

        private Vector3 _hGg = new(0.9f, 0.9f, 0.9f);
        public Vector3 HGg
        {
            get => _hGg;
            set => _hGg = value;
        }

        public float InscatteringMultiplier { get; set; } = 1.0f;

        public float BetaRayMultiplier { get; set; } = 8.0f;

        public float BetaMieMultiplier { get; set; } = 0.00005f;

        private Vector3 _betaRPlusBetaM;
        private Vector3 _betaDashR;
        private Vector3 _betaDashM;
        private Vector3 _oneOverBetaRPlusBetaM;
        private Vector4 _multipliers;
        private Vector4 _sunColorAndIntensity;

        private readonly Vector3 _betaRay;
        private readonly Vector3 _betaDashRay;
        private readonly Vector3 _betaMie;
        private readonly Vector3 _betaDashMie;

        public Hofman()
        {
            SunDirection = 0;

            const float n = 1.0003f;
            const float N = 2.545e25f;
            const float pn = 0.035f;

            float[] lambda = new float[3];
            float[] lambda2 = new float[3];
            float[] lambda4 = new float[3];

            lambda[0] = 1.0f / 650e-9f;   // red
            lambda[1] = 1.0f / 570e-9f;   // green
            lambda[2] = 1.0f / 475e-9f;   // blue

            for (int i = 0; i < 3; ++i)
            {
                lambda2[i] = lambda[i] * lambda[i];
                lambda4[i] = lambda2[i] * lambda2[i];
            }

            Vector3 vLambda2 = new Vector3(lambda2[0], lambda2[1], lambda2[2]);
            Vector3 vLambda4 = new Vector3(lambda4[0], lambda4[1], lambda4[2]);

            // Rayleigh scattering constants

            const float temp = (float)(Math.PI * Math.PI * (n * n - 1.0f) * (n * n - 1.0f) * (6.0f + 3.0f * pn) / (6.0f - 7.0f * pn) / N);
            const float beta = (float)(8.0f * temp * Math.PI / 3.0f);

            _betaRay = beta * vLambda4;

            const float betaDash = temp / 2.0f;

            _betaDashRay = betaDash * vLambda4;

            // Mie scattering constants

            const float T = 2.0f;
            const float c = (6.544f * T - 6.51f) * 1e-17f;
            const float temp2 = (float)(0.434f * c * (2.0f * Math.PI) * (2.0f * Math.PI) * 0.5f);

            _betaDashMie = temp2 * vLambda2;

            float[] K = new float[3] { 0.685f, 0.679f, 0.670f };
            const float temp3 = (float)(0.434f * c * Math.PI * (2.0f * Math.PI) * (2.0f * Math.PI));

            Vector3 vBetaMieTemp = new Vector3(K[0] * lambda2[0], K[1] * lambda2[1], K[2] * lambda2[2]);

            _betaMie = temp3 * vBetaMieTemp;
        }

        public void Draw()
        {
            Vector3 vZenith = new Vector3(0.0f, 1.0f, 0.0f);

            float thetaS = Vector3.Dot(Position, vZenith);
            thetaS = (float)(Math.Acos(thetaS));

            ComputeAttenuation(thetaS);
            SetMaterialProperties();
        }

        private void ComputeAttenuation(float thetaS)
        {
            float beta = 0.04608365822050f * Turbitity - 0.04586025928522f;
            float tauR, tauA;
            float[] fTau = new float[3];
            float m = (float)(1.0f / (Math.Cos(thetaS) + 0.15f * Math.Pow(93.885f - thetaS / Math.PI * 180.0f, -1.253f)));  // Relative Optical Mass
            float[] lambda = new float[3] { 0.65f, 0.57f, 0.475f };

            for (int i = 0; i < 3; ++i)
            {
                // Rayleigh Scattering
                // lambda in um.
                tauR = (float)(Math.Exp(-m * 0.008735f * Math.Pow(lambda[i], -4.08f)));

                // Aerosal (water + dust) attenuation
                // beta - amount of aerosols present
                // alpha - ratio of small to large particle sizes. (0:4,usually 1.3)
                const float alpha = 1.3f;
                tauA = (float)(Math.Exp(-m * beta * Math.Pow(lambda[i], -alpha)));  // lambda should be in um

                fTau[i] = tauR * tauA;
            }

            _sunColorAndIntensity = new Vector4(fTau[0], fTau[1], fTau[2], SunIntensity * 100.0f);

        }

        private void SetMaterialProperties()
        {
            float reflectance = 0.1f;

            Vector3 vecBetaR = _betaRay * BetaRayMultiplier;
            _betaDashR = _betaDashRay * BetaRayMultiplier;
            Vector3 vecBetaM = _betaMie * BetaMieMultiplier;
            _betaDashM = _betaDashMie * BetaMieMultiplier;
            _betaRPlusBetaM = vecBetaR + vecBetaM;
            _oneOverBetaRPlusBetaM = new Vector3(1.0f / _betaRPlusBetaM.X, 1.0f / _betaRPlusBetaM.Y, 1.0f / _betaRPlusBetaM.Z);
            Vector3 vecG = new Vector3(1.0f - _hGg.X * _hGg.X, 1.0f + _hGg.X * _hGg.X, 2.0f * _hGg.X);
            _multipliers = new Vector4(InscatteringMultiplier, 0.138f * reflectance, 0.113f * reflectance, 0.08f * reflectance);
        }

        public void UpdateEffect(ChunkEffect<VertexSkyBox> skyEffect)
        {
            SunDirection += 0.1f;

            skyEffect.Effect.GetVariableByName("sunDirection").AsVector().Set(Position);
            skyEffect.Effect.GetVariableByName("betaRPlusBetaM").AsVector().Set(_betaRPlusBetaM);
            skyEffect.Effect.GetVariableByName("hGg").AsVector().Set(HGg);
            skyEffect.Effect.GetVariableByName("betaDashR").AsVector().Set(_betaDashR);
            skyEffect.Effect.GetVariableByName("betaDashM").AsVector().Set(_betaDashM);
            skyEffect.Effect.GetVariableByName("oneOverBetaRPlusBetaM").AsVector().Set(_oneOverBetaRPlusBetaM);
            skyEffect.Effect.GetVariableByName("multipliers").AsVector().Set(_multipliers);
            skyEffect.Effect.GetVariableByName("sunColorAndIntensity").AsVector().Set(_sunColorAndIntensity);
        }
    }
}
