namespace TphdCemuTrainer.Memory;

internal readonly record struct MemoryRegion(ulong BaseAddress, ulong Size, uint State, uint Protect)
{
    private const uint MemCommit = 0x1000;
    private const uint PageNoAccess = 0x01;
    private const uint PageGuard = 0x100;

    public bool IsReadable =>
        State == MemCommit &&
        (Protect & PageNoAccess) == 0 &&
        (Protect & PageGuard) == 0;
}
