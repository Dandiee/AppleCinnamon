using System;
using System.Linq;
using System.Windows.Forms;
using AppleCinnamon.Common;
using AppleCinnamon.Drawers;
using AppleCinnamon.Helper;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DirectInput;
using Point = System.Drawing.Point;

namespace AppleCinnamon
{
    public partial class Game
    {
        public DebugContext DebugContext { get; private set; }

        private void SetupDebug()
        {
            DebugContext = new DebugContext(
                new DebugToggleAction(Key.F1, () => GameOptions.RenderSolid),
                new DebugToggleAction(Key.F2, () => GameOptions.RenderSprites),
                new DebugToggleAction(Key.F3, () => GameOptions.RenderWater),
                new DebugToggleAction(Key.F4, () => GameOptions.RenderSky),
                new DebugToggleAction(Key.F5, () => GameOptions.RenderCrosshair),
                new DebugToggleAction(Key.F6, () => GameOptions.RenderBoxes),
                new DebugToggleAction(Key.F7, () => GameOptions.RenderPipelineVisualization),
                new DebugToggleAction(Key.F8, () => GameOptions.RenderChunkBoundingBoxes),
                new DebugToggleAction(Key.F9, () => GameOptions.IsViewFrustumCullingEnabled));


            PerformanceContext = new DebugContext(
                new DebugInfoLine<int>(() => WeirdFps, default,  " FPS"));
        }

        public DebugContext PerformanceContext { get; private set; }
    }
}
