namespace AppleCinnamon.Common;

public enum Face
{
    Top,
    Bottom,
    Left,
    Right,
    Front,
    Back
}

public static class FaceExtensions
{
    private static readonly IReadOnlyDictionary<Face, Face> OppositeMapping =
        new Dictionary<Face, Face>
        {
            [Face.Top] = Face.Bottom,
            [Face.Bottom] = Face.Top,
            [Face.Left] = Face.Right,
            [Face.Right] = Face.Left,
            [Face.Front] = Face.Back,
            [Face.Back] = Face.Bottom,
        };

    public static Face GetOpposite(this Face flag) => OppositeMapping[flag];
}