namespace TphdCemuTrainer.Research;

public sealed class ResearchSnapshotDocument
{
    public string Name { get; set; } = string.Empty;

    public DateTimeOffset Timestamp { get; set; }

    public string Notes { get; set; } = string.Empty;

    public string GameStateDescription { get; set; } = string.Empty;

    public List<ResearchSnapshotRange> Ranges { get; set; } = [];
}
