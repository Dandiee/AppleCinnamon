namespace AppleCinnamon.Common;

[Flags]
public enum TransmittanceFlags : byte
{
    None = 0,
    Quarter1 = 1,
    Quarter2 = 2,
    Quarter3 = 4,
    Quarter4 = 8,

    Top = 3,
    Bottom = 12,
    All = 15
}