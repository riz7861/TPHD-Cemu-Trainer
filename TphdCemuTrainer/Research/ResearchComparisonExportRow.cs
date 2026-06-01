namespace TphdCemuTrainer.Research;

public sealed class ResearchComparisonExportRow
{
    public string Offset { get; set; } = string.Empty;

    public int? SnapshotAValue { get; set; }

    public int? SnapshotBValue { get; set; }

    public string SnapshotADecode { get; set; } = string.Empty;

    public string SnapshotBDecode { get; set; } = string.Empty;

    public string Difference { get; set; } = string.Empty;

    public bool Changed { get; set; }
}
