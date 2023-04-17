using AppleCinnamon.Collision;
using AppleCinnamon.Common;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectInput;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;
using System.Collections.Generic;
using System.Linq;
using static System.Windows.Forms.AxHost;
using TextAlignment = SharpDX.DirectWrite.TextAlignment;

namespace AppleCinnamon.Drawers
{
    public sealed class DebugLayout
    {
        public const string FontFamilyName = "Consolas";

        private readonly Graphics _graphics;
        private readonly TextFormat _leftAlignedTextFormat;
        private readonly TextFormat _rightAlignedTextFormat;
        private readonly TextFormat _bottomCenterAlignedTextFormat;

        private TextLayout _leftLayout;
        private TextLayout _rightLayout;
        private TextLayout _bottomCenterLayout;

        private readonly SolidColorBrush _brush;

        public DebugContext LeftContext { get; private set; }
        public DebugContext RightContext { get; private set; }

        private readonly DebugContext _skyDomeContext;
        private readonly DebugContext _cameraContext;
        private readonly DebugContext _pipelineContext;
        private readonly DebugContext _gameContext;
        private readonly DebugContext _mainMenuContext;
        private readonly DebugContext _performanceContext;

        public DebugLayout(Game game, Graphics graphics)
        {
            _graphics = graphics;
            _leftAlignedTextFormat = new TextFormat(_graphics.DirectWrite, FontFamilyName, FontWeight.Black, FontStyle.Normal, 20);
            _rightAlignedTextFormat = new TextFormat(_graphics.DirectWrite, FontFamilyName, FontWeight.Black, FontStyle.Normal, 20)
            {
                TextAlignment = TextAlignment.Trailing
            };

            _bottomCenterAlignedTextFormat = new TextFormat(_graphics.DirectWrite, FontFamilyName, FontWeight.Black, FontStyle.Normal, 20)
            {
                TextAlignment = TextAlignment.Center
            };

            _brush = new SolidColorBrush(_graphics.RenderTarget2D, Color.White);


            _skyDomeContext = new DebugContext(_leftAlignedTextFormat, graphics,
                new DebugAction(Key.Back, "Back", () => LeftContext = _mainMenuContext),
                new DebugIncDecAction<float>(Key.F1, () => SkyDomeOptions.SunIntensity, 0.01f, game.SkyDome.UpdateEffect),
                new DebugIncDecAction<float>(Key.F2, () => SkyDomeOptions.Turbitity, 0.01f, game.SkyDome.UpdateEffect),
                new DebugIncDecAction<float>(Key.F3, () => SkyDomeOptions.InscatteringMultiplier, 0.001f, game.SkyDome.UpdateEffect),
                new DebugIncDecAction<float>(Key.F4, () => SkyDomeOptions.BetaRayMultiplier, 0.01f, game.SkyDome.UpdateEffect),
                new DebugIncDecAction<float>(Key.F5, () => SkyDomeOptions.BetaMieMultiplier, 0.000001f, game.SkyDome.UpdateEffect),
                new DebugIncDecAction<float>(Key.F6, () => SkyDomeOptions.TimeOfDay, 0.0001f, game.SkyDome.UpdateEffect),
                new DebugIncDecAction<int>(Key.F7, () => SkyDomeOptions.Resolution, 1, game.SkyDome.UpdateSkyDome, false),
                new DebugIncDecAction<float>(Key.F8, () => SkyDomeOptions.Radius, 0.1f, game.SkyDome.UpdateSkyDome));

            _pipelineContext = new DebugContext(_rightAlignedTextFormat, graphics,
                new DebugInfoLine<int>(() => ChunkManager.BagOfDeath.Count, "Bag of Death"),
                new DebugInfoLine<int>(() => ChunkManager.Chunks.Count, "All chunks"),
                new DebugInfoLine<int>(() => ChunkManager.Graveyard.Count, "Graveyard"),
                new DebugInfoLine<int>(() => ChunkManager.ChunkCreated),
                new DebugInfoLine<int>(() => ChunkManager.ChunkResurrected),
                new DebugInfoLine<PipelineState>(() => game._chunkManager.Pipeline.State),
                new DebugInfoLine<double>(() => game._chunkManager.Pipeline.TerrainStage.TimeSpentInTransform.TotalMilliseconds, game._chunkManager.Pipeline.TerrainStage.Name, " ms"),
                new DebugInfoLine<double>(() => game._chunkManager.Pipeline.ArtifactStage.TimeSpentInTransform.TotalMilliseconds, game._chunkManager.Pipeline.ArtifactStage.Name, " ms"),
                new DebugInfoLine<double>(() => game._chunkManager.Pipeline.LocalStage.TimeSpentInTransform.TotalMilliseconds, game._chunkManager.Pipeline.LocalStage.Name, " ms"),
                new DebugInfoLine<double>(() => game._chunkManager.Pipeline.GlobalStage.TimeSpentInTransform.TotalMilliseconds, game._chunkManager.Pipeline.GlobalStage.Name, " ms"),
                new DebugInfoLine<double>(() => game._chunkManager.Pipeline.TimeSpentInTransform.TotalMilliseconds, "Dispatcher", " ms"));

            _cameraContext = new DebugContext(_rightAlignedTextFormat, graphics,
                new DebugInfoLine<Vector3>(() => game._camera.Position),
                new DebugInfoLine<Vector3>(() => game._camera.LookAt),
                new DebugInfoLine<Int2>(() => game._camera.CurrentChunkIndex),
                new DebugInfoMultiLine<VoxelRayCollisionResult>(() => game._camera.CurrentCursor, GetCurrentCursorLines));

            _gameContext = new DebugContext(_leftAlignedTextFormat, graphics,
                new DebugAction(Key.Back, "Back", () => LeftContext = _mainMenuContext),
                new DebugToggleAction(Key.F1, () => GameOptions.RenderSolid),
                new DebugToggleAction(Key.F2, () => GameOptions.RenderSprites),
                new DebugToggleAction(Key.F3, () => GameOptions.RenderWater),
                new DebugToggleAction(Key.F4, () => GameOptions.RenderSky),
                new DebugToggleAction(Key.F5, () => GameOptions.RenderCrosshair),
                new DebugToggleAction(Key.F6, () => GameOptions.RenderBoxes),
                new DebugToggleAction(Key.F7, () => GameOptions.RenderPipelineVisualization),
                new DebugToggleAction(Key.F8, () => GameOptions.RenderChunkBoundingBoxes),
                new DebugToggleAction(Key.F9, () => GameOptions.IsViewFrustumCullingEnabled));

            _performanceContext = new DebugContext(_rightAlignedTextFormat, graphics,
                new DebugInfoLine<int>(() => game.WeirdFps, default, " FPS"));

            _mainMenuContext = new DebugContext(_leftAlignedTextFormat, graphics,
                new DebugAction(Key.F1, "Sky", () => LeftContext = _skyDomeContext),
                new DebugAction(Key.F2, "Game", () => LeftContext = _gameContext),
                new DebugAction(Key.F3, "Perf", () => RightContext = _performanceContext),
                new DebugAction(Key.F4, "Camera", () => RightContext = _cameraContext),
                new DebugAction(Key.F5, "Pipeline", () => RightContext = _pipelineContext));

            LeftContext = _mainMenuContext;
            RightContext = _performanceContext;
        }

        private static IEnumerable<string> GetCurrentCursorLines(VoxelRayCollisionResult currentCursor)
        {
            yield return "CurrentCursor";

            if (currentCursor == null)
            {
                yield return "[No target]";
                yield break;
            }

            yield return $"ChunkIndex: {currentCursor.Address.Chunk.ChunkIndex}";
            yield return $"RelVoxelAddress: {currentCursor.Address.RelativeVoxelIndex}";
            yield return $"AbsVoxelAddress: {currentCursor.AbsoluteVoxelIndex}";
            yield return $"Voxel: {currentCursor.Definition.Name}";
            yield return $"Face: {currentCursor.Direction}";
        }


        public void Draw(Camera camera, Game game)
        {
            LeftContext.Draw(camera);
            RightContext.Draw(camera);
            
            _bottomCenterLayout?.Dispose();
            _bottomCenterLayout = new TextLayout(_graphics.DirectWrite, $"{camera.VoxelInHand.Name}: [{camera.VoxelInHand.Type}]", _bottomCenterAlignedTextFormat, _graphics.RenderForm.Width - 30, _graphics.RenderForm.Height);
            _graphics.RenderTarget2D.DrawTextLayout(new RawVector2(0, _graphics.RenderForm.Height - 100), _bottomCenterLayout, _brush);
        }
    }
}
