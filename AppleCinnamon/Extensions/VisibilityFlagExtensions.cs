using System.Collections.Generic;
using AppleCinnamon.Common;

namespace AppleCinnamon.Extensions;

public static class VisibilityFlagExtensions
{
    private static readonly IReadOnlyDictionary<VisibilityFlag, VisibilityFlag> OppositeMapping =
        new Dictionary<VisibilityFlag, VisibilityFlag>
        {
            [VisibilityFlag.Top] = VisibilityFlag.Bottom,
            [VisibilityFlag.Bottom] = VisibilityFlag.Top,
            [VisibilityFlag.Left] = VisibilityFlag.Right,
            [VisibilityFlag.Right] = VisibilityFlag.Left,
            [VisibilityFlag.Front] = VisibilityFlag.Back,
            [VisibilityFlag.Back] = VisibilityFlag.Front,
        };

    public static VisibilityFlag GetOpposite(this VisibilityFlag flag)
        => OppositeMapping[flag];
}