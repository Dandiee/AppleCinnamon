using System;

namespace AppleCinnamon
{
    [Flags]
    public enum VisibilityFlag : byte
    {
        None = 0,
        Top = 1,
        Bottom = 2,
        Left = 4,
        Right = 8,
        Front = 16,
        Back = 32,
        All = 63
    }
}