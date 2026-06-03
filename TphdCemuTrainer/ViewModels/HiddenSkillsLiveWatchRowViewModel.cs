namespace TphdCemuTrainer.ViewModels;

public sealed class HiddenSkillsLiveWatchRowViewModel
{
    public HiddenSkillsLiveWatchRowViewModel(
        DateTimeOffset timestamp,
        uint offsetValue,
        byte beforeValue,
        byte afterValue,
        int changeCount)
    {
        Timestamp = timestamp;
        OffsetValue = offsetValue;
        BeforeValue = beforeValue;
        AfterValue = afterValue;
        ChangeCount = changeCount;
        BeforeBinary = FormatBinary(beforeValue);
        AfterBinary = FormatBinary(afterValue);
        ChangedBitCount = CountChangedBits(beforeValue, afterValue);
        ChangedBits = FormatChangedBits(beforeValue, afterValue);
    }

    public DateTimeOffset Timestamp { get; }

    public string TimestampText => Timestamp.ToLocalTime().ToString("HH:mm:ss.fff");

    public uint OffsetValue { get; }

    public string Offset => $"0x{OffsetValue:X}";

    public byte BeforeValue { get; }

    public byte AfterValue { get; }

    public string BeforeByte => FormatByte(BeforeValue);

    public string AfterByte => FormatByte(AfterValue);

    public string BeforeBinary { get; }

    public string AfterBinary { get; }

    public string ChangedBits { get; }

    public int ChangedBitCount { get; }

    public int ChangeCount { get; }

    public bool IsSingleBitChange => ChangedBitCount == 1;

    public bool IsRepeatedChange => ChangeCount > 1;

    public bool IsMonotonicChange => AfterValue >= BeforeValue;

    public string HighlightLabel
    {
        get
        {
            var labels = new List<string>();
            if (IsSingleBitChange)
            {
                labels.Add("single bit");
            }

            if (IsRepeatedChange)
            {
                labels.Add($"repeated x{ChangeCount}");
            }

            if (IsMonotonicChange)
            {
                labels.Add("monotonic");
            }

            return labels.Count == 0 ? "-" : string.Join(", ", labels);
        }
    }

    private static string FormatByte(byte value)
    {
        return $"{value} / 0x{value:X2}";
    }

    private static string FormatBinary(byte value)
    {
        return Convert.ToString(value, 2).PadLeft(8, '0');
    }

    private static string FormatChangedBits(byte beforeValue, byte afterValue)
    {
        var changedMask = beforeValue ^ afterValue;
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

    private static int CountChangedBits(byte beforeValue, byte afterValue)
    {
        var changedMask = beforeValue ^ afterValue;
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
