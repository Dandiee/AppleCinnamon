namespace NoiseGeneratorTest.Java
{

    public class TerrainProvider
    {
        private static float _deepOceanContinentalness = -0.51F;
        private static float _oceanContinentalness = -0.4F;
        private static float _plainsContinentalness = 0.1F;
        private static float _beachContinentalness = -0.15F;
        private static readonly ToFloatFunction<float> NoTransform = ToFloatFunction2.IDENTITY;

        private static readonly ToFloatFunction<float> AmplifiedOffset = ToFloatFunction2.createUnlimited((p236651) => p236651 < 0.0F ? p236651 : p236651 * 2.0F);
        private static readonly ToFloatFunction<float> AmplifiedFactor = ToFloatFunction2.createUnlimited((p236649) => 1.25F - 6.25F / (p236649 + 5.0F));
        private static readonly ToFloatFunction<float> AmplifiedJaggedness = ToFloatFunction2.createUnlimited((p236641) => p236641 * 2.0F);

        public static CubicSpline<TC, T> OverworldOffset<TC, T>(T p236636, T p236637, T p236638, bool p236639)
         where T : ToFloatFunction<TC>
        {
            ToFloatFunction<float> tofloatfunction = p236639 ? AmplifiedOffset : NoTransform;
            CubicSpline<TC, T> cubicspline = BuildErosionOffsetSpline<TC, T>(p236637, p236638, -0.15F, 0.0F, 0.0F, 0.1F, 0.0F, -0.03F, false, false, tofloatfunction);
            CubicSpline<TC, T> cubicspline1 = BuildErosionOffsetSpline<TC, T>(p236637, p236638, -0.1F, 0.03F, 0.1F, 0.1F, 0.01F, -0.03F, false, false, tofloatfunction);
            CubicSpline<TC, T> cubicspline2 = BuildErosionOffsetSpline<TC, T>(p236637, p236638, -0.1F, 0.03F, 0.1F, 0.7F, 0.01F, -0.03F, true, true, tofloatfunction);
            CubicSpline<TC, T> cubicspline3 = BuildErosionOffsetSpline<TC, T>(p236637, p236638, -0.05F, 0.03F, 0.1F, 1.0F, 0.01F, 0.01F, true, true, tofloatfunction);
            return CubicSpline<TC, T>.Builder(p236636, tofloatfunction).AddPoint(-1.1F, 0.044F).AddPoint(-1.02F, -0.2222F).AddPoint(-0.51F, -0.2222F).AddPoint(-0.44F, -0.12F).AddPoint(-0.18F, -0.12F).AddPoint(-0.16F, cubicspline).AddPoint(-0.15F, cubicspline).AddPoint(-0.1F, cubicspline1).AddPoint(0.25F, cubicspline2).AddPoint(1.0F, cubicspline3).Build();
        }

        public static CubicSpline<TC, T> OverworldFactor<TC, T>(T p236630, T p236631, T p236632, T p236633, bool p236634)
    where T : ToFloatFunction<TC>
        {
            ToFloatFunction<float> tofloatfunction = p236634 ? AmplifiedFactor : NoTransform;
            return CubicSpline<TC, T>.Builder(p236630, NoTransform)
                .AddPoint(-0.19F, 3.95F)
                .AddPoint(-0.15F, GetErosionFactor<TC, T>(p236631, p236632, p236633, 6.25F, true, NoTransform))
                .AddPoint(-0.1F, GetErosionFactor<TC, T>(p236631, p236632, p236633, 5.47F, true, tofloatfunction))
                .AddPoint(0.03F, GetErosionFactor<TC, T>(p236631, p236632, p236633, 5.08F, true, tofloatfunction))
                .AddPoint(0.06F, GetErosionFactor<TC, T>(p236631, p236632, p236633, 4.69F, false, tofloatfunction))
                .Build();
        }

        public static CubicSpline<TC, T> OverworldJaggedness<TC, T>(T p236643, T p236644, T p236645, T p236646, bool p236647)
    where T : ToFloatFunction<TC>
        {
            ToFloatFunction<float> tofloatfunction = p236647 ? AmplifiedJaggedness : NoTransform;
            float f = 0.65F;
            return CubicSpline<TC, T>.Builder(p236643, tofloatfunction)
                .AddPoint(-0.11F, 0.0F)
                .AddPoint(0.03F, BuildErosionJaggednessSpline<TC, T>(p236644, p236645, p236646, 1.0F, 0.5F, 0.0F, 0.0F, tofloatfunction))
                .AddPoint(0.65F, BuildErosionJaggednessSpline<TC, T>(p236644, p236645, p236646, 1.0F, 1.0F, 1.0F, 0.0F, tofloatfunction))
                .Build();
        }

        private static CubicSpline<TC, T> BuildErosionJaggednessSpline<TC, T>(T p236614, T p236615, T p236616, float p236617, float p236618, float p236619, float p236620, ToFloatFunction<float> p236621)
    where T : ToFloatFunction<TC>
        {
            float f = -0.5775F;
            CubicSpline<TC, T> cubicspline = BuildRidgeJaggednessSpline<TC, T>(p236615, p236616, p236617, p236619, p236621);
            CubicSpline<TC, T> cubicspline1 = BuildRidgeJaggednessSpline<TC, T>(p236615, p236616, p236618, p236620, p236621);
            return CubicSpline<TC, T>.Builder(p236614, p236621)
                .AddPoint(-1.0F, cubicspline)
                .AddPoint(-0.78F, cubicspline1)
                .AddPoint(-0.5775F, cubicspline1)
                .AddPoint(-0.375F, 0.0F)
                .Build();
        }

        private static CubicSpline<TC, T> BuildRidgeJaggednessSpline<TC, T>(T p236608, T p236609, float p236610, float p236611, ToFloatFunction<float> p236612)
    where T : ToFloatFunction<TC>
        {
            float f = NoiseRouterData.PeaksAndValleys(0.4F);
            float f1 = NoiseRouterData.PeaksAndValleys(0.56666666F);
            float f2 = (f + f1) / 2.0F;
            var builder = CubicSpline<TC, T>.Builder(p236609, p236612);
            builder.AddPoint(f, 0.0F);
            if (p236611 > 0.0F)
            {
                builder.AddPoint(f2, BuildWeirdnessJaggednessSpline<TC, T>(p236608, p236611, p236612));
            }
            else
            {
                builder.AddPoint(f2, 0.0F);
            }

            if (p236610 > 0.0F)
            {
                builder.AddPoint(1.0F, BuildWeirdnessJaggednessSpline<TC, T>(p236608, p236610, p236612));
            }
            else
            {
                builder.AddPoint(1.0F, 0.0F);
            }

            return builder.Build();
        }

        private static CubicSpline<TC, T> BuildWeirdnessJaggednessSpline<TC, T>(T p236587, float p236588, ToFloatFunction<float> p236589)
    where T : ToFloatFunction<TC>
        {
            float f = 0.63F * p236588;
            float f1 = 0.3F * p236588;
            return CubicSpline<TC, T>
                .Builder(p236587, p236589)
                .AddPoint(-0.01F, f)
                .AddPoint(0.01F, f1)
                .Build();
        }

        private static CubicSpline<TC, T> GetErosionFactor<TC, T>(T p236623, T p236624, T p236625, float p236626, bool p236627, ToFloatFunction<float> p236628)
    where T : ToFloatFunction<TC>
        {
            var cubicspline = CubicSpline<TC, T>.Builder(p236624, p236628)
                .AddPoint(-0.2F, 6.3F)
                .AddPoint(0.2F, p236626)
                .Build();

            var builder = CubicSpline<TC, T>.Builder(p236623, p236628)
                .AddPoint(-0.6F, cubicspline)
                .AddPoint(-0.5F, CubicSpline<TC, T>.Builder(p236624, p236628)
                    .AddPoint(-0.05F, 6.3F)
                    .AddPoint(0.05F, 2.67F).Build())
                .AddPoint(-0.35F, cubicspline)
                .AddPoint(-0.25F, cubicspline)
                .AddPoint(-0.1F, CubicSpline<TC, T>.Builder(p236624, p236628)
                    .AddPoint(-0.05F, 2.67F)
                    .AddPoint(0.05F, 6.3F)
                    .Build())
                .AddPoint(0.03F, cubicspline);
            if (p236627)
            {
                CubicSpline<TC, T> cubicspline1 = CubicSpline<TC, T>.Builder(p236624, p236628).AddPoint(0.0F, p236626).AddPoint(0.1F, 0.625F).Build();
                CubicSpline<TC, T> cubicspline2 = CubicSpline<TC, T>.Builder(p236625, p236628).AddPoint(-0.9F, p236626).AddPoint(-0.69F, cubicspline1).Build();
                builder.AddPoint(0.35F, p236626).AddPoint(0.45F, cubicspline2).AddPoint(0.55F, cubicspline2).AddPoint(0.62F, p236626);
            }
            else
            {
                CubicSpline<TC, T> cubicspline3 = CubicSpline<TC, T>.Builder(p236625, p236628).AddPoint(-0.7F, cubicspline).AddPoint(-0.15F, 1.37F).Build();
                CubicSpline<TC, T> cubicspline4 = CubicSpline<TC, T>.Builder(p236625, p236628).AddPoint(0.45F, cubicspline).AddPoint(0.7F, 1.56F).Build();
                builder.AddPoint(0.05F, cubicspline4).AddPoint(0.4F, cubicspline4).AddPoint(0.45F, cubicspline3).AddPoint(0.55F, cubicspline3).AddPoint(0.58F, p236626);
            }

            return builder.Build();
        }

        private static float CalculateSlope(float p236573, float p236574, float p236575, float p236576)
        {
            return (p236574 - p236573) / (p236576 - p236575);
        }

        private static CubicSpline<TC, T> BuildMountainRidgeSplineWithPoints<TC, T>(T p236591, float p236592, bool p236593, ToFloatFunction<float> p236594)
    where T : ToFloatFunction<TC>
        {
            var builder = new Builder<TC, T>(p236591, p236594);
            float f = -0.7F;
            float f1 = -1.0F;
            float f2 = MountainContinentalness(-1.0F, p236592, -0.7F);
            float f3 = 1.0F;
            float f4 = MountainContinentalness(1.0F, p236592, -0.7F);
            float f5 = CalculateMountainRidgeZeroContinentalnessPoint(p236592);
            float f6 = -0.65F;
            if (-0.65F < f5 && f5 < 1.0F)
            {
                float f14 = MountainContinentalness(-0.65F, p236592, -0.7F);
                float f8 = -0.75F;
                float f9 = MountainContinentalness(-0.75F, p236592, -0.7F);
                float f10 = CalculateSlope(f2, f9, -1.0F, -0.75F);
                builder.AddPoint(-1.0F, f2, f10);
                builder.AddPoint(-0.75F, f9);
                builder.AddPoint(-0.65F, f14);
                float f11 = MountainContinentalness(f5, p236592, -0.7F);
                float f12 = CalculateSlope(f11, f4, f5, 1.0F);
                float f13 = 0.01F;
                builder.AddPoint(f5 - 0.01F, f11);
                builder.AddPoint(f5, f11, f12);
                builder.AddPoint(1.0F, f4, f12);
            }
            else
            {
                float f7 = CalculateSlope(f2, f4, -1.0F, 1.0F);
                if (p236593)
                {
                    builder.AddPoint(-1.0F, Math.Max(0.2F, f2));
                    builder.AddPoint(0.0F, Mth.Lerp(0.5F, f2, f4), f7);
                }
                else
                {
                    builder.AddPoint(-1.0F, f2, f7);
                }

                builder.AddPoint(1.0F, f4, f7);
            }

            return builder.Build();
        }

        private static float MountainContinentalness(float p236569, float p236570, float p236571)
        {
            float f = 1.17F;
            float f1 = 0.46082947F;
            float f2 = 1.0F - (1.0F - p236570) * 0.5F;
            float f3 = 0.5F * (1.0F - p236570);
            float f4 = (p236569 + 1.17F) * 0.46082947F;
            float f5 = f4 * f2 - f3;
            return p236569 < p236571 ? Math.Max(f5, -0.2222F) : Math.Max(f5, 0.0F);
        }

        private static float CalculateMountainRidgeZeroContinentalnessPoint(float p236567)
        {
            float f = 1.17F;
            float f1 = 0.46082947F;
            float f2 = 1.0F - (1.0F - p236567) * 0.5F;
            float f3 = 0.5F * (1.0F - p236567);
            return f3 / (0.46082947F * f2) - 1.17F;
        }

        public static CubicSpline<TC, T> BuildErosionOffsetSpline<TC, T>(T p236596, T p236597, float p236598, float p236599, float p236600, float p236601, float p236602, float p236603, bool p236604, bool p236605, ToFloatFunction<float> p236606)
    where T : ToFloatFunction<TC>
        {
            float f = 0.6F;
            float f1 = 0.5F;
            float f2 = 0.5F;
            CubicSpline<TC, T> cubicspline = BuildMountainRidgeSplineWithPoints<TC, T>(p236597, Mth.Lerp(p236601, 0.6F, 1.5F), p236605, p236606);
            CubicSpline<TC, T> cubicspline1 = BuildMountainRidgeSplineWithPoints<TC, T>(p236597, Mth.Lerp(p236601, 0.6F, 1.0F), p236605, p236606);
            CubicSpline<TC, T> cubicspline2 = BuildMountainRidgeSplineWithPoints<TC, T>(p236597, p236601, p236605, p236606);
            CubicSpline<TC, T> cubicspline3 = RidgeSpline<TC, T>(p236597, p236598 - 0.15F, 0.5F * p236601, Mth.Lerp(0.5F, 0.5F, 0.5F) * p236601, 0.5F * p236601, 0.6F * p236601, 0.5F, p236606);
            CubicSpline<TC, T> cubicspline4 = RidgeSpline<TC, T>(p236597, p236598, p236602 * p236601, p236599 * p236601, 0.5F * p236601, 0.6F * p236601, 0.5F, p236606);
            CubicSpline<TC, T> cubicspline5 = RidgeSpline<TC, T>(p236597, p236598, p236602, p236602, p236599, p236600, 0.5F, p236606);
            CubicSpline<TC, T> cubicspline6 = RidgeSpline<TC, T>(p236597, p236598, p236602, p236602, p236599, p236600, 0.5F, p236606);
            CubicSpline<TC, T> cubicspline7 = CubicSpline<TC, T>.Builder(p236597, p236606).AddPoint(-1.0F, p236598).AddPoint(-0.4F, cubicspline5).AddPoint(0.0F, p236600 + 0.07F).Build();
            CubicSpline<TC, T> cubicspline8 = RidgeSpline<TC, T>(p236597, -0.02F, p236603, p236603, p236599, p236600, 0.0F, p236606);
            var builder = CubicSpline<TC, T>.Builder(p236596, p236606).AddPoint(-0.85F, cubicspline).AddPoint(-0.7F, cubicspline1).AddPoint(-0.4F, cubicspline2).AddPoint(-0.35F, cubicspline3).AddPoint(-0.1F, cubicspline4).AddPoint(0.2F, cubicspline5);
            if (p236604)
            {
                builder.AddPoint(0.4F, cubicspline6).AddPoint(0.45F, cubicspline7).AddPoint(0.55F, cubicspline7).AddPoint(0.58F, cubicspline6);
            }

            builder.AddPoint(0.7F, cubicspline8);
            return builder.Build();
        }

        private static CubicSpline<TC, T> RidgeSpline<TC, T>(T p236578, float p236579, float p236580, float p236581, float p236582, float p236583, float p236584, ToFloatFunction<float> p236585)
    where T : ToFloatFunction<TC>
        {
            float f = Math.Max(0.5F * (p236580 - p236579), p236584);
            float f1 = 5.0F * (p236581 - p236580);
            return CubicSpline<TC, T>.Builder(p236578, p236585).AddPoint(-1.0F, p236579, f).AddPoint(-0.4F, p236580, Math.Min(f, f1)).AddPoint(0.0F, p236581, f1).AddPoint(0.4F, p236582, 2.0F * (p236582 - p236581)).AddPoint(1.0F, p236583, 0.7F * (p236583 - p236582)).Build();
        }
    }

    public static class NoiseRouterData
    {
        public static float PeaksAndValleys(float i) => i;
    }
}
