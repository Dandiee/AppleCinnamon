using System.Collections.Generic;
using AppleCinnamon.Pipeline;
using AppleCinnamon.System;

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
        public List<int> LightPropagationVoxels = new(1024);

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

        public FaceBuildingContext(Face face)
        {
            Face = face;
            Direction = FaceMapping[face];
        }

        public readonly Face Face;
        public readonly VisibilityFlag Direction;
        public readonly List<int> PendingVoxels = new();
        public int VoxelCount;
    }
    
}
