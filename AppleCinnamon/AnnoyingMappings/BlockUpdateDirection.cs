using System.Collections.Generic;
using AppleCinnamon.Common;
using AppleCinnamon.Helper;
using SharpDX;

namespace AppleCinnamon
{
    public readonly struct BlockUpdateDirection
    {
        private static readonly IReadOnlyDictionary<VisibilityFlag, Face> Mapping = new Dictionary<VisibilityFlag, Face>
        {
            [VisibilityFlag.Top] = Face.Top,
            [VisibilityFlag.Bottom] = Face.Bottom,
            [VisibilityFlag.Left] = Face.Left,
            [VisibilityFlag.Right] = Face.Right,
            [VisibilityFlag.Front] = Face.Front,
            [VisibilityFlag.Back] = Face.Back,
        };

        public readonly Int3 Step;
        public readonly VisibilityFlag Direction;
        public readonly VisibilityFlag OppositeDirection;
        public readonly Face Face;
        public readonly Face OppositeFace;

        private BlockUpdateDirection(Int3 step, VisibilityFlag direction, VisibilityFlag oppositeDirection)
        {
            Step = step;
            Direction = direction;
            OppositeDirection = oppositeDirection;
            Face = Mapping[direction];
            OppositeFace = Mapping[oppositeDirection];
        }

        public static BlockUpdateDirection[] All = {
            new (Int3.UnitY, VisibilityFlag.Top, VisibilityFlag.Bottom),
            new ( -Int3.UnitY, VisibilityFlag.Bottom, VisibilityFlag.Top),
            new (-Int3.UnitX, VisibilityFlag.Left, VisibilityFlag.Right),
            new (Int3.UnitX, VisibilityFlag.Right, VisibilityFlag.Left),
            new (-Int3.UnitZ, VisibilityFlag.Front, VisibilityFlag.Back),
            new (Int3.UnitZ, VisibilityFlag.Back, VisibilityFlag.Front)
        };
    }

    public struct DarknessSource
    {
        public VoxelChunkAddress Address;
        public Voxel OldVoxel;

        public DarknessSource(VoxelChunkAddress address, Voxel oldVoxel)
        {
            Address = address;
            OldVoxel = oldVoxel;
        }
    }
}