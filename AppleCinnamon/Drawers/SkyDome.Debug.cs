using SharpDX.DirectInput;

namespace AppleCinnamon.Drawers
{
    public partial class SkyDome
    {
        public DebugContext Debug { get; private set; }

        private void SetupDebug()
        {
            Debug = new DebugContext(
                    new DebugIncDecAction(Key.F1, "SunIntensity", 0.01f, step => UpdateDebug(ref SkyDomeOptions.SunIntensity, step)),
                    new DebugIncDecAction(Key.F2, "Turbitity", 0.01f, step => UpdateDebug(ref SkyDomeOptions.Turbitity, step)),
                    new DebugIncDecAction(Key.F3, "InscatteringMultiplier", 0.001f, step => UpdateDebug(ref SkyDomeOptions.InscatteringMultiplier, step)),
                    new DebugIncDecAction(Key.F4, "BetaRayMultiplier", 0.01f, step => UpdateDebug(ref SkyDomeOptions.BetaRayMultiplier, step)),
                    new DebugIncDecAction(Key.F5, "BetaMieMultiplier", 0.000001f, step => UpdateDebug(ref SkyDomeOptions.BetaMieMultiplier, step)),
                    new DebugIncDecAction(Key.F6, "TimeOfDay", 0.0001f, step => UpdateDebug(ref SkyDomeOptions.TimeOfDay, step))
                    );
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
    }
}
