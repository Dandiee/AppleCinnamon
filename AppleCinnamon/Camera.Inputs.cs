using System;
using System.Runtime;
using AppleCinnamon.Chunks;
using AppleCinnamon.Collision;
using AppleCinnamon.Common;
using AppleCinnamon.Drawers;
using AppleCinnamon.Extensions;
using AppleCinnamon.Helper;
using AppleCinnamon.Settings;
using SharpDX;
using SharpDX.DirectInput;

namespace AppleCinnamon
{
    public sealed partial class Camera
    {
        public KeyboardAction[] Actions;

        const float LilStep = 0.001f;

        private void SetupActions()
        {
            Actions = new[]
            {
                new KeyboardAction(Key.Escape, "Pause", () => Game.IsPaused = !Game.IsPaused),
                new KeyboardAction(Key.F1, "RenderDebugLayout", () => Game.RenderDebugLayout = !Game.RenderDebugLayout),
                new KeyboardAction(Key.F2, "RenderSolid", () => Game.RenderSolid = !Game.RenderSolid),
                new KeyboardAction(Key.F3, "RenderWater", () => Game.RenderWater= !Game.RenderWater),
                new KeyboardAction(Key.F4, "RenderSprites", () => Game.RenderSprites = !Game.RenderSprites),
                new KeyboardAction(Key.F5, "RenderBoxes", () => Game.RenderBoxes = !Game.RenderBoxes),
                new KeyboardAction(Key.F6, "ShowChunkBoundingBoxes", () => Game.ShowChunkBoundingBoxes = !Game.ShowChunkBoundingBoxes),
                new KeyboardAction(Key.F7, "RenderSky", () => Game.RenderSky = !Game.RenderSky),
                new KeyboardAction(Key.F8, "ShowPipelineVisualization", () => Game.ShowPipelineVisualization = !Game.ShowPipelineVisualization),
                new KeyboardAction(Key.F9, "RenderCrosshair", () => Game.RenderCrosshair = !Game.RenderCrosshair),
                new KeyboardAction(Key.F10, "Debug", () => Game.Debug = !Game.Debug),
            };

            Actions = new[]
            {
                new KeyboardAction(Key.Add, Key.F1, nameof(SkyDomeOptions.TimeOfDay), () => _skyDome.UpdateOpts(ref SkyDomeOptions.TimeOfDay, +LilStep)),
                new KeyboardAction(Key.Minus, Key.F1, nameof(SkyDomeOptions.TimeOfDay), () => _skyDome.UpdateOpts(ref SkyDomeOptions.TimeOfDay, -LilStep)),

                new KeyboardAction(Key.Add, Key.F2, nameof(SkyDomeOptions.SunIntensity), () => _skyDome.UpdateOpts(ref SkyDomeOptions.SunIntensity, +LilStep)),
                new KeyboardAction(Key.Minus, Key.F2, nameof(SkyDomeOptions.SunIntensity), () => _skyDome.UpdateOpts(ref SkyDomeOptions.SunIntensity, -LilStep)),

                new KeyboardAction(Key.Add, Key.F3, nameof(SkyDomeOptions.Turbitity), () => _skyDome.UpdateOpts(ref SkyDomeOptions.Turbitity, +LilStep)),
                new KeyboardAction(Key.Minus, Key.F3, nameof(SkyDomeOptions.Turbitity), () => _skyDome.UpdateOpts(ref SkyDomeOptions.Turbitity, -LilStep)),

                new KeyboardAction(Key.Add, Key.F4, nameof(SkyDomeOptions.InscatteringMultiplier), () => _skyDome.UpdateOpts(ref SkyDomeOptions.InscatteringMultiplier, +LilStep)),
                new KeyboardAction(Key.Minus, Key.F4, nameof(SkyDomeOptions.InscatteringMultiplier), () => _skyDome.UpdateOpts(ref SkyDomeOptions.InscatteringMultiplier, -LilStep)),

                new KeyboardAction(Key.Add, Key.F5, nameof(SkyDomeOptions.BetaRayMultiplier), () => _skyDome.UpdateOpts(ref SkyDomeOptions.BetaRayMultiplier, +LilStep / 100f)),
                new KeyboardAction(Key.Minus, Key.F5, nameof(SkyDomeOptions.BetaRayMultiplier), () => _skyDome.UpdateOpts(ref SkyDomeOptions.BetaRayMultiplier, -LilStep / 100f)),

                new KeyboardAction(Key.Add, Key.F7, nameof(SkyDomeOptions.BetaMieMultiplier), () => _skyDome.UpdateOpts(ref SkyDomeOptions.BetaMieMultiplier, +LilStep)),
                new KeyboardAction(Key.Minus, Key.F7, nameof(SkyDomeOptions.BetaMieMultiplier), () => _skyDome.UpdateOpts(ref SkyDomeOptions.BetaMieMultiplier, -LilStep)),
            };
        }
    }

    public sealed class KeyboardActionSet
    {

    }

    public sealed class KeyboardAction
    {
        public Key Key { get; set; }
        public Key? Modifier { get; set; }
        public string Name { get; set; }
        public Action Action { get; set; }

        public KeyboardAction(Key key, string name, Action action)
        {
            Key = key;
            Name = name;
            Action = action;
        }

        public KeyboardAction(Key key, Key modifier, string name, Action action)
         : this(key, name, action)
        {
            Modifier = modifier;
        }

        public bool IsFired(KeyboardState prevState, KeyboardState nextState)
            => (!Modifier.HasValue || nextState.IsPressed(Modifier.Value)) && !nextState.IsPressed(Key) && prevState.IsPressed(Key);
    }

    public sealed class SetValueAction
    {
        private readonly Func<string> _action;
        public Key Key { get; }
        public string Name { get; }
        public string Value { get; private set; }

        public SetValueAction(Key key, string name, Func<string> action)
        {
            _action = action;
            Key = key;
            Name = name;
        }

        public void Update(KeyboardState prevState, KeyboardState nextState)
        {
            if (nextState.IsPressed(Key))
            {
                if (!nextState.IsPressed(Key.Add) && prevState.IsPressed(Key.Add))
                {
                    Value = _action();
                }
                else if (!nextState.IsPressed(Key.Minus) && prevState.IsPressed(Key.Minus))
                {
                    Value = _action();
                }
            }
        }
    }
}
