using System;
using System.Runtime;
using AppleCinnamon.Chunks;
using AppleCinnamon.Collision;
using AppleCinnamon.Common;
using AppleCinnamon.Extensions;
using AppleCinnamon.Helper;
using AppleCinnamon.Settings;
using SharpDX;
using SharpDX.DirectInput;

namespace AppleCinnamon
{
    public sealed partial class Camera
    {
        public CameraAction[] Actions;

        private void SetupActions()
        {
            Actions = new[]
            {
                new CameraAction(Key.Escape, "Pause", () => Game.IsPaused = !Game.IsPaused),
                new CameraAction(Key.F1, "RenderDebugLayout", () => Game.RenderDebugLayout = !Game.RenderDebugLayout),
                new CameraAction(Key.F2, "RenderSolid", () => Game.RenderSolid = !Game.RenderSolid),
                new CameraAction(Key.F3, "RenderWater", () => Game.RenderWater= !Game.RenderWater),
                new CameraAction(Key.F4, "RenderSprites", () => Game.RenderSprites = !Game.RenderSprites),
                new CameraAction(Key.F5, "RenderBoxes", () => Game.RenderBoxes = !Game.RenderBoxes),
                new CameraAction(Key.F6, "ShowChunkBoundingBoxes", () => Game.ShowChunkBoundingBoxes = !Game.ShowChunkBoundingBoxes),
                new CameraAction(Key.F7, "RenderSky", () => Game.RenderSky = !Game.RenderSky),
                new CameraAction(Key.F8, "ShowPipelineVisualization", () => Game.ShowPipelineVisualization = !Game.ShowPipelineVisualization),
                new CameraAction(Key.F9, "RenderCrosshair", () => Game.RenderCrosshair = !Game.RenderCrosshair),
                new CameraAction(Key.F10, "Debug", () => Game.Debug = !Game.Debug),

            };
        }
    }

    public sealed class CameraAction
    {
        public Key Key { get; set; }
        public string Name { get; set; }
        public Action Action { get; set; }

        public CameraAction(Key key, string name, Action action)
        {
            Key = key;
            Name = name;
            Action = action;
        }

        public bool IsFired(KeyboardState prevState, KeyboardState nextState)
            => !nextState.IsPressed(Key) && prevState.IsPressed(Key);
    }
}
