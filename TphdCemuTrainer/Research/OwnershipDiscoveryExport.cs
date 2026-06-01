namespace TphdCemuTrainer.Research;

public sealed class OwnershipDiscoveryExport
{
    public DateTimeOffset Timestamp { get; set; }

    public string SnapshotAName { get; set; } = string.Empty;

    public string SnapshotBName { get; set; } = string.Empty;

    public bool TreatSnapshotBAsPersistedAfterReload { get; set; }

    public List<OwnershipDiscoveryExportRow> Rows { get; set; } = [];

    public List<string> Report { get; set; } = [];
}
