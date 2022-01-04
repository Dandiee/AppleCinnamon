using System;
using System.Collections.Generic;
using AppleCinnamon.Helper;
using AppleCinnamon.Pipeline;
using AppleCinnamon.Settings;
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

        public List<int> SpriteBlocks = new();
        public List<int> SingleSidedSpriteBlocks = new();

        public Dictionary<int, VisibilityFlag> VisibilityFlags = new();
        public Queue<int> LightPropagationVoxels = new(1024);
        public List<int> TopMostWaterVoxels = new();
        public List<int> TopMostLandVoxels = new();

        public bool IsChanged => IsSpriteChanged || IsWaterChanged || IsSolidChanged;
        public bool IsSpriteChanged { get; set; } = true;
        public bool IsWaterChanged { get; set; } = true;
        public bool IsSolidChanged { get; set; } = true;

        public void SetAllChanged()
        {
            IsSpriteChanged = true;
            IsWaterChanged = true;
            IsSolidChanged = true;
        }

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
                [Face.Left] = (ijk, height) => Chunk.GetFlatIndex(WorldSettings.ChunkSize - 1, ijk.Y, ijk.Z, height),
                [Face.Right] = (ijk, height) => Chunk.GetFlatIndex(0, ijk.Y, ijk.Z, height),
                [Face.Front] = (ijk, height) => Chunk.GetFlatIndex(ijk.X, ijk.Y, WorldSettings.ChunkSize - 1, height),
                [Face.Back] = (ijk, height) => Chunk.GetFlatIndex(ijk.X, ijk.Y, 0, height)
            };

        public FaceBuildingContext(Face face)
        {
            Face = face;
            OppositeFace = face.GetOpposite();
            Direction = FaceMapping[face];
            OppositeDirection = Direction.GetOpposite();
            if (NeighborIndexFuncs.TryGetValue(face, out var getNeighborIndex))
            {
                GetNeighborIndex = getNeighborIndex;
            }
        }

        public readonly Face Face;
        public readonly Face OppositeFace;
        public readonly VisibilityFlag Direction;
        public readonly VisibilityFlag OppositeDirection;
        public readonly List<int> PendingVoxels = new();
        public readonly Func<Int3, int, int> GetNeighborIndex;
        public int VoxelCount;
    }
    
}
