namespace TphdCemuTrainer.ViewModels;

public sealed class QuestItemsMultiCaptureRowViewModel
{
    public QuestItemsMultiCaptureRowViewModel(
        uint offsetValue,
        string valueProgression,
        int appearances,
        int score,
        string confidence,
        string reasons)
    {
        OffsetValue = offsetValue;
        Offset = $"0x{offsetValue:X}";
        ValueProgression = valueProgression;
        AppearanceCount = appearances;
        CandidateScore = score;
        Confidence = confidence;
        Reasons = reasons;
    }

    public uint OffsetValue { get; }

    public string Offset { get; }

    public string ValueProgression { get; }

    public int AppearanceCount { get; }

    public int CandidateScore { get; }

    public string Confidence { get; }

    public string Reasons { get; }
}
