using System.Numerics;
using SharpDX.DirectInput;

namespace AppleCinnamon.Drawers
{
    public partial class SkyDome
    {
        public DebugContext Debug { get; private set; }

        private void SetupDebug()
        {
            Debug = new DebugContext(
                    new DebugIncDecAction<float>(Key.F1, "SunIntensity", 0.01f, step => UpdateDebug(ref SkyDomeOptions.SunIntensity, step)),
                    new DebugIncDecAction<float>(Key.F2, "Turbitity", 0.01f, step => UpdateDebug(ref SkyDomeOptions.Turbitity, step)),
                    new DebugIncDecAction<float>(Key.F3, "InscatteringMultiplier", 0.001f, step => UpdateDebug(ref SkyDomeOptions.InscatteringMultiplier, step)),
                    new DebugIncDecAction<float>(Key.F4, "BetaRayMultiplier", 0.01f, step => UpdateDebug(ref SkyDomeOptions.BetaRayMultiplier, step)),
                    new DebugIncDecAction<float>(Key.F5, "BetaMieMultiplier", 0.000001f, step => UpdateDebug(ref SkyDomeOptions.BetaMieMultiplier, step)),
                    new DebugIncDecAction<float>(Key.F6, "TimeOfDay", 0.0001f, step => UpdateDebug(ref SkyDomeOptions.TimeOfDay, step)),
                    new DebugIncDecAction<int>(Key.F7, "Resolution", 1, step => UpdateSkyDome(ref SkyDomeOptions.Resolution, step), false),
                    new DebugIncDecAction<float>(Key.F8, "Radius", 0.1f, step => UpdateSkyDome(ref SkyDomeOptions.Radius, step)));
        }

        private float UpdateDebug(ref float field, float step)
        {
            field += step;
            if (step != 0)
            {
                _skyDomeEffectEffect.UpdateDetails();
            }

            return field;
        }

        private T UpdateSkyDome<T>(ref T field, T step)
            where T : INumber<T>
        {
            field += step;
            if (step != default)
            {
                UpdateSkyDome();
            }

            return field;
        }
        
    }
}
