namespace TphdCemuTrainer.Cheats;

public enum CheatValueKind
{
    Byte,
    UInt16BigEndian,
    UInt32BigEndian
}

public enum CheatId
{
    CurrentHealth,
    MaximumHealth,
    HeartProgress,
    LanternOil,
    Rupees,
    Arrows,
    BombSlot1,
    BombSlot2,
    BombSlot3,
    Seeds,
    PoeSouls,
    GoldenBugsFlags,
    CurrentDungeonSmallKeys,
    GoronMinesKeyShardState
}

public sealed record CheatDefinition(
    CheatId Id,
    string Name,
    uint Offset,
    CheatValueKind ValueKind,
    int HardMaximum,
    bool CanLock,
    string Source,
    string? CapacityId = null)
{
    public int StorageMaximum => ValueKind switch
    {
        CheatValueKind.Byte => byte.MaxValue,
        CheatValueKind.UInt16BigEndian => ushort.MaxValue,
        CheatValueKind.UInt32BigEndian => int.MaxValue,
        _ => int.MaxValue
    };

    public int Clamp(int value, int effectiveMaximum)
    {
        var upperBound = Math.Min(Math.Min(HardMaximum, StorageMaximum), effectiveMaximum);
        return Math.Clamp(value, 0, upperBound);
    }
}
