using System;
using System.Collections.Generic;
using System.Runtime;
using System.Runtime.CompilerServices;
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
    public partial class Camera
    {
        public DebugContext DebugContext { get; private set; }

        public void SetupContext()
        {
            var lines = new DebugLine[]
            {
                new DebugInfoLine<Vector3>(() => Position),
                new DebugInfoLine<Vector3>(() => LookAt),
                new DebugInfoLine<Int2>(() => CurrentChunkIndex),
                new DebugInfoMultiLine<VoxelRayCollisionResult>(() => CurrentCursor, GetCurrentCursorLines),
            };

            DebugContext = new DebugContext(lines);
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
    }
}
