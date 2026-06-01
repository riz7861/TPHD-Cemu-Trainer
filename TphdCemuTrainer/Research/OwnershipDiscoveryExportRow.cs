namespace TphdCemuTrainer.Research;

public sealed class OwnershipDiscoveryExportRow
{
    public string Offset { get; set; } = string.Empty;

    public int? BeforeValue { get; set; }

    public int? AfterValue { get; set; }

    public bool Changed { get; set; }

    public string BeforeBinary { get; set; } = string.Empty;

    public string AfterBinary { get; set; } = string.Empty;

    public string ChangedBits { get; set; } = string.Empty;

    public int ChangedBitCount { get; set; }

    public bool PersistedAfterReload { get; set; }

    public bool OutsideVisibleInventorySlots { get; set; }

    public int Score { get; set; }

    public bool StrongCandidate { get; set; }

    public string PotentialMeaning { get; set; } = string.Empty;
}
