using System;
using SharpDX;

namespace AppleCinnamon.Options;

public static class GameOptions
{
    public const int VIEW_DISTANCE = 40;
    public const int NUMBER_OF_POOLS = 4;
    public const int CHUNK_SIZE = 16;
    public const int SLICE_HEIGHT = 16;
    public const int SLICE_AREA = CHUNK_SIZE * CHUNK_SIZE * SLICE_HEIGHT;

    public const bool IS_CHANGE_TRACKING_ENABLED = true;

    public static readonly Vector3 StartPosition = new(0, 140, 0);
    public static readonly TimeSpan ChunkDespawnCooldown = TimeSpan.FromMilliseconds(10);

    public static bool RenderSolid { get; set; } = true;
    public static bool RenderSprites { get; set; } = true;
    public static bool RenderWater { get; set; } = true;
    public static bool RenderSky { get; set; } = true;
    public static bool RenderCrosshair { get; set; } = true;
    public static bool RenderBoxes { get; set; } = true;
    public static bool RenderPipelineVisualization { get; set; } = false;
    public static bool IsViewFrustumCullingEnabled { get; set; } = true;
    public static bool RenderChunkBoundingBoxes { get; set; } = false;
    public static bool RenderDebugLayout { get; set; } = true;
    public static bool IsPaused { get; set; } = false;

}