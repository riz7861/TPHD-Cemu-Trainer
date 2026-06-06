namespace TphdCemuTrainer.ViewModels;

public sealed class LiveCaptureChangeViewModel
{
    public LiveCaptureChangeViewModel(
        uint offsetValue,
        byte initialValue,
        byte previousValue,
        byte currentValue,
        int changeCount,
        DateTimeOffset firstSeen,
        DateTimeOffset lastSeen,
        string persistedStatus,
        int candidateScore,
        string confidence,
        string reasons)
    {
        OffsetValue = offsetValue;
        InitialValue = initialValue;
        PreviousValue = previousValue;
        CurrentValue = currentValue;
        ChangeCount = changeCount;
        FirstSeen = firstSeen;
        LastSeen = lastSeen;
        PersistedStatus = persistedStatus;
        CandidateScore = candidateScore;
        Confidence = confidence;
        Reasons = reasons;
        ChangedBitCount = CountChangedBits(previousValue, currentValue);
        BitChangeSummary = FormatChangedBits(previousValue, currentValue);
    }

    public uint OffsetValue { get; }

    public string Offset => $"0x{OffsetValue:X}";

    public byte InitialValue { get; }

    public byte PreviousValue { get; }

    public byte CurrentValue { get; }

    public string InitialByte => FormatByte(InitialValue);

    public string OldValue => FormatByte(PreviousValue);

    public string CurrentByte => FormatByte(CurrentValue);

    public int ChangeCount { get; }

    public DateTimeOffset FirstSeen { get; }

    public string FirstSeenText => FirstSeen.ToLocalTime().ToString("HH:mm:ss.fff");

    public DateTimeOffset LastSeen { get; }

    public string LastSeenText => LastSeen.ToLocalTime().ToString("HH:mm:ss.fff");

    public string BitChangeSummary { get; }

    public int ChangedBitCount { get; }

    public bool IsSingleBitTransition => ChangedBitCount == 1;

    public bool StayedChanged => CurrentValue != InitialValue;

    public bool Reverted => ChangeCount > 0 && CurrentValue == InitialValue;

    public string PersistedStatus { get; }

    public int CandidateScore { get; }

    public string Confidence { get; }

    public string Reasons { get; }

    private static string FormatByte(byte value)
    {
        return $"0x{value:X2}";
    }

    private static string FormatChangedBits(byte previousValue, byte currentValue)
    {
        var changedMask = previousValue ^ currentValue;
        if (changedMask == 0)
        {
            return "-";
        }

        var bits = new List<string>();
        for (var bitIndex = 0; bitIndex < 8; bitIndex++)
        {
            if ((changedMask & (1 << bitIndex)) != 0)
            {
                bits.Add($"bit {bitIndex}");
            }
        }

        return string.Join(", ", bits);
    }

    private static int CountChangedBits(byte previousValue, byte currentValue)
    {
        var changedMask = previousValue ^ currentValue;
        var count = 0;
        for (var bitIndex = 0; bitIndex < 8; bitIndex++)
        {
            if ((changedMask & (1 << bitIndex)) != 0)
            {
                count++;
            }
        }

        return count;
    }
}
