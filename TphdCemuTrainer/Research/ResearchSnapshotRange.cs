namespace TphdCemuTrainer.Research;

public sealed class ResearchSnapshotRange
{
    public string Label { get; set; } = string.Empty;

    public uint StartOffset { get; set; }

    public byte[] Bytes { get; set; } = [];
}
