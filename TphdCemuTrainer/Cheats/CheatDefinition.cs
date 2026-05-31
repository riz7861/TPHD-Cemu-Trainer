namespace TphdCemuTrainer.Cheats;

public enum CheatValueKind
{
    Byte,
    UInt16BigEndian
}

public sealed record CheatOption(int Value, string Label);

public sealed record CheatDefinition(
    string Name,
    uint Offset,
    CheatValueKind ValueKind,
    int DefaultValue,
    int? MaxValue,
    bool CanLock,
    string Source,
    IReadOnlyList<CheatOption>? Options = null)
{
    public int MinimumValue => 0;

    public int MaximumValue => ValueKind switch
    {
        CheatValueKind.Byte => byte.MaxValue,
        CheatValueKind.UInt16BigEndian => ushort.MaxValue,
        _ => int.MaxValue
    };

    public int Clamp(int value)
    {
        var upperBound = Math.Min(MaxValue ?? MaximumValue, MaximumValue);
        return Math.Clamp(value, MinimumValue, upperBound);
    }
}
