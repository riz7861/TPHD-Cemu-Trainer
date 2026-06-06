namespace TphdCemuTrainer.ViewModels;

public sealed class LiveCaptureCandidateViewModel
{
    public LiveCaptureCandidateViewModel(
        uint offsetValue,
        int score,
        string confidence,
        string reason,
        byte currentValue,
        int changeCount,
        string persistedStatus)
    {
        OffsetValue = offsetValue;
        Offset = $"0x{offsetValue:X}";
        Score = score;
        Confidence = confidence;
        Reason = reason;
        CurrentValue = $"0x{currentValue:X2}";
        ChangeCount = changeCount;
        PersistedStatus = persistedStatus;
    }

    public uint OffsetValue { get; }

    public string Offset { get; }

    public int Score { get; }

    public string Confidence { get; }

    public string Reason { get; }

    public string CurrentValue { get; }

    public int ChangeCount { get; }

    public string PersistedStatus { get; }
}
