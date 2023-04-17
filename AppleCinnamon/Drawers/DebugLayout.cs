using AppleCinnamon.Collision;
using AppleCinnamon.Common;
using SharpDX;
using SharpDX.DirectInput;
using SharpDX.DirectWrite;
using System.Collections.Generic;
using AppleCinnamon.Settings;
using TextAlignment = SharpDX.DirectWrite.TextAlignment;
using SharpDX.Mathematics.Interop;

namespace AppleCinnamon.Drawers
{
    public sealed class DebugLayout
    {
        public const string FONT_FAMILY_NAME = "Consolas";

        private readonly Game _game;

        public DebugContext LeftContext { get; private set; }
        public DebugContext RightContext { get; private set; }
        public DebugContext BottomCenterContext { get; private set; }

        private readonly DebugContext _mainMenuContext;

        public DebugLayout(Game game, Graphics graphics)
        {
            _game = game;

            var leftAlignedTextFormat = new TextFormat(graphics.DirectWrite, FONT_FAMILY_NAME, FontWeight.Black, FontStyle.Normal, 20);
            var rightAlignedTextFormat = new TextFormat(graphics.DirectWrite, FONT_FAMILY_NAME, FontWeight.Black, FontStyle.Normal, 20)
            {
                TextAlignment = TextAlignment.Trailing
            };
            var bottomCenterAlignedTextFormat = new TextFormat(graphics.DirectWrite, FONT_FAMILY_NAME, FontWeight.Black, FontStyle.Normal, 20)
            {
                TextAlignment = TextAlignment.Center
            };

            var leftOrigin = new RawVector2(10, 10);
            var rightOrigin = new RawVector2(0, 10);
            var bottomOrigin = new RawVector2(0, graphics.RenderForm.Height - 100);

            var skyDomeContext = new DebugContext(leftAlignedTextFormat, graphics, leftOrigin,
                new DebugAction(Key.Back, "Back", () => LeftContext = _mainMenuContext),
                new DebugIncDecAction<float>(Key.F1, () => SkyDomeOptions.SunIntensity, 0.001f, game.SkyDome.UpdateEffect),
                new DebugIncDecAction<float>(Key.F2, () => SkyDomeOptions.Turbitity, 0.01f, game.SkyDome.UpdateEffect),
                new DebugIncDecAction<float>(Key.F3, () => SkyDomeOptions.InscatteringMultiplier, 0.001f, game.SkyDome.UpdateEffect),
                new DebugIncDecAction<float>(Key.F4, () => SkyDomeOptions.BetaRayMultiplier, 0.01f, game.SkyDome.UpdateEffect),
                new DebugIncDecAction<float>(Key.F5, () => SkyDomeOptions.BetaMieMultiplier, 0.000001f, game.SkyDome.UpdateEffect),
                new DebugIncDecAction<float>(Key.F6, () => SkyDomeOptions.TimeOfDay, 0.001f, game.SkyDome.UpdateEffect, increment: SkyDomeOptions.IncrementTime, decrement: SkyDomeOptions.DecrementTime),
                new DebugIncDecAction<int>(Key.F7, () => SkyDomeOptions.Resolution, 1, game.SkyDome.UpdateSkyDome, false),
                new DebugIncDecAction<float>(Key.F8, () => SkyDomeOptions.Radius, 0.1f, game.SkyDome.UpdateSkyDome),
                new DebugIncDecAction<float>(Key.F9, () => CameraOptions.FieldOfView, 0.001f));

            var pipelineContext = new DebugContext(rightAlignedTextFormat, graphics, rightOrigin,
                new DebugInfoLine<int>(() => ChunkManager.BagOfDeath.Count, "Bag of Death"),
                new DebugInfoLine<int>(() => ChunkManager.Chunks.Count, "All chunks"),
                new DebugInfoLine<int>(() => ChunkManager.Graveyard.Count, "Graveyard"),
                new DebugInfoLine<int>(() => ChunkManager.ChunkCreated),
                new DebugInfoLine<int>(() => ChunkManager.ChunkResurrected),
                new DebugInfoLine<PipelineState>(() => game.ChunkManager.Pipeline.State),
                new DebugInfoLine<double>(() => game.ChunkManager.Pipeline.TerrainStage.TimeSpentInTransform.TotalMilliseconds, game.ChunkManager.Pipeline.TerrainStage.Name, " ms"),
                new DebugInfoLine<double>(() => game.ChunkManager.Pipeline.ArtifactStage.TimeSpentInTransform.TotalMilliseconds, game.ChunkManager.Pipeline.ArtifactStage.Name, " ms"),
                new DebugInfoLine<double>(() => game.ChunkManager.Pipeline.LocalStage.TimeSpentInTransform.TotalMilliseconds, game.ChunkManager.Pipeline.LocalStage.Name, " ms"),
                new DebugInfoLine<double>(() => game.ChunkManager.Pipeline.GlobalStage.TimeSpentInTransform.TotalMilliseconds, game.ChunkManager.Pipeline.GlobalStage.Name, " ms"),
                new DebugInfoLine<double>(() => game.ChunkManager.Pipeline.TimeSpentInTransform.TotalMilliseconds, "Dispatcher", " ms"));

            var cameraContext = new DebugContext(rightAlignedTextFormat, graphics, rightOrigin,
                
                new DebugInfoLine<Vector3>(() => game.Camera.Position),
                new DebugInfoLine<Vector3>(() => game.Camera.LookAt),
                new DebugInfoLine<Int2>(() => game.Camera.CurrentChunkIndex),
                new DebugInfoMultiLine<VoxelRayCollisionResult>(() => game.Camera.CurrentCursor, GetCurrentCursorLines));

            var gameContext = new DebugContext(leftAlignedTextFormat, graphics, leftOrigin,
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

            var performanceContext = new DebugContext(rightAlignedTextFormat, graphics, rightOrigin,
                new DebugInfoLine<int>(() => Game.ViewDistance, "ViewDistance"),
                new DebugInfoLine<int>(() => game.WeirdFps, default, " FPS"));

            _mainMenuContext = new DebugContext(leftAlignedTextFormat, graphics, leftOrigin,
                new DebugAction(Key.F1, "Sky", () => LeftContext = skyDomeContext),
                new DebugAction(Key.F2, "Game", () => LeftContext = gameContext),
                new DebugAction(Key.F3, "Perf", () => RightContext = performanceContext),
                new DebugAction(Key.F4, "Camera", () => RightContext = cameraContext),
                new DebugAction(Key.F5, "Pipeline", () => RightContext = pipelineContext));

            BottomCenterContext = new DebugContext(bottomCenterAlignedTextFormat, graphics, bottomOrigin,
                new DebugInfoLine<VoxelDefinition>(() => game.Camera.VoxelInHand, "", textFactory: vox => $"{vox.Name}: [{vox.Type}]"));

            LeftContext = _mainMenuContext;
            RightContext = performanceContext;
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


        public void Draw()
        {
            LeftContext.Draw(_game.Camera);
            RightContext.Draw(_game.Camera);
            BottomCenterContext.Draw(_game.Camera);
        }
    }
}
