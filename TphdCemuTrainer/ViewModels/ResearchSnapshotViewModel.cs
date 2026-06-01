using System.Globalization;
using TphdCemuTrainer.Research;

namespace TphdCemuTrainer.ViewModels;

public sealed class ResearchSnapshotViewModel
{
    public ResearchSnapshotViewModel(string filePath, ResearchSnapshotDocument snapshot)
    {
        FilePath = filePath;
        Snapshot = snapshot;
    }

    public string FilePath { get; }

    public ResearchSnapshotDocument Snapshot { get; }

    public string Name => Snapshot.Name;

    public string TimestampText => Snapshot.Timestamp.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);

    public string Notes => Snapshot.Notes;

    public string GameStateDescription => Snapshot.GameStateDescription;

    public int RangeCount => Snapshot.Ranges.Count;
}
