namespace TphdCemuTrainer.Memory;

public readonly record struct MemoryRegion(ulong BaseAddress, ulong Size, uint State, uint Protect, uint Type)
{
    private const uint MemCommit = 0x1000;
    private const uint PageNoAccess = 0x01;
    private const uint PageGuard = 0x100;
    private const uint PageExecute = 0x10;
    private const uint PageExecuteRead = 0x20;
    private const uint PageExecuteReadWrite = 0x40;
    private const uint PageExecuteWriteCopy = 0x80;
    private const uint MemPrivate = 0x20000;
    private const uint MemMapped = 0x40000;
    private const uint MemImage = 0x1000000;

    public bool IsReadable =>
        State == MemCommit &&
        (Protect & PageNoAccess) == 0 &&
        (Protect & PageGuard) == 0;

    public bool IsPrivate => Type == MemPrivate;

    public bool IsMapped => Type == MemMapped;

    public bool IsImage => Type == MemImage;

    public bool IsExecutable =>
        (Protect & (PageExecute | PageExecuteRead | PageExecuteReadWrite | PageExecuteWriteCopy)) != 0;

    public string TypeName => Type switch
    {
        MemPrivate => "MEM_PRIVATE",
        MemMapped => "MEM_MAPPED",
        MemImage => "MEM_IMAGE",
        _ => $"0x{Type:X}"
    };

    public bool Contains(ulong address)
    {
        return address >= BaseAddress && address < BaseAddress + Size;
    }
}
