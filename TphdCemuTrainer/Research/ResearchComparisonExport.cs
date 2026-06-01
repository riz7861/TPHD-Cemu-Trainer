namespace TphdCemuTrainer.Research;

public sealed class ResearchComparisonExport
{
    public DateTimeOffset Timestamp { get; set; }

    public string SnapshotAName { get; set; } = string.Empty;

    public string SnapshotBName { get; set; } = string.Empty;

    public List<ResearchComparisonExportRow> Rows { get; set; } = [];

    public List<string> DiscoveryReport { get; set; } = [];
}
