namespace TphdCemuTrainer.ViewModels;

public sealed class HiddenSkillsEventMarkerViewModel
{
    public HiddenSkillsEventMarkerViewModel(DateTimeOffset timestamp, string label)
    {
        Timestamp = timestamp;
        Label = label;
    }

    public DateTimeOffset Timestamp { get; }

    public string TimestampText => Timestamp.ToLocalTime().ToString("HH:mm:ss.fff");

    public string Label { get; }
}
