using System;
using System.Collections.Generic;
using AppleCinnamon.Pipeline;
using AppleCinnamon.System;
using SharpDX;

namespace AppleCinnamon
{
    public sealed class ChunkBuildingContext
    {
        public readonly FaceBuildingContext Top;
        public readonly FaceBuildingContext Bottom;
        public readonly FaceBuildingContext Left;
        public readonly FaceBuildingContext Right;
        public readonly FaceBuildingContext Front;
        public readonly FaceBuildingContext Back;

        public readonly FaceBuildingContext[] Faces;

        public Dictionary<int, VisibilityFlag> VisibilityFlags = new();
        public Queue<int> LightPropagationVoxels = new(1024);

        public ChunkBuildingContext()
        {
            Top = new FaceBuildingContext(Face.Top);
            Bottom = new FaceBuildingContext(Face.Bottom);
            Left = new FaceBuildingContext(Face.Left);
            Right = new FaceBuildingContext(Face.Right);
            Front = new FaceBuildingContext(Face.Front);
            Back = new FaceBuildingContext(Face.Back);

            Faces = new[] {Top, Bottom, Left, Right, Front, Back};
        }
    }

    public sealed class FaceBuildingContext
    {
        private static readonly IReadOnlyDictionary<Face, VisibilityFlag> FaceMapping =
            new Dictionary<Face, VisibilityFlag>
            {
                [Face.Top] = VisibilityFlag.Top,
                [Face.Bottom] = VisibilityFlag.Bottom,
                [Face.Left] = VisibilityFlag.Left,
                [Face.Right] = VisibilityFlag.Right,
                [Face.Front] = VisibilityFlag.Front,
                [Face.Back] = VisibilityFlag.Back,
            };

        private static readonly IReadOnlyDictionary<Face, Func<Int3, int, int>> NeighborIndexFuncs =
            new Dictionary<Face, Func<Int3, int, int>>
            {
                [Face.Left] = (ijk, height) => Help.GetFlatIndex(Chunk.SizeXy - 1, ijk.Y, ijk.Z, height),
                [Face.Right] = (ijk, height) => Help.GetFlatIndex(0, ijk.Y, ijk.Z, height),
                [Face.Front] = (ijk, height) => Help.GetFlatIndex(ijk.X, ijk.Y, Chunk.SizeXy - 1, height),
                [Face.Back] = (ijk, height) => Help.GetFlatIndex(ijk.X, ijk.Y, 0, height)
            };

        public FaceBuildingContext(Face face)
        {
            Face = face;
            Direction = FaceMapping[face];
            if (NeighborIndexFuncs.TryGetValue(face, out var getNeighborIndex))
            {
                GetNeighborIndex = getNeighborIndex;
            }
        }

        public readonly Face Face;
        public readonly VisibilityFlag Direction;
        public readonly List<int> PendingVoxels = new();
        public readonly Func<Int3, int, int> GetNeighborIndex;
        public int VoxelCount;
    }
    
}
