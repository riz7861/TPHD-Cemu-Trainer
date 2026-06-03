namespace TphdCemuTrainer.ViewModels;

public sealed class QuestItemsResearchRowViewModel : ObservableObject
{
    private string _candidateGroup = "-";

    public QuestItemsResearchRowViewModel(uint offsetValue, byte beforeValue, byte afterValue)
    {
        OffsetValue = offsetValue;
        BeforeValue = beforeValue;
        AfterValue = afterValue;
        Offset = $"0x{offsetValue:X}";
        BeforeByte = FormatByte(beforeValue);
        AfterByte = FormatByte(afterValue);
        BeforeBinary = ToBinary(beforeValue);
        AfterBinary = ToBinary(afterValue);
        ChangedBits = FormatChangedBits(beforeValue, afterValue);
        ChangedBitCount = CountChangedBits(beforeValue, afterValue);
        IsChanged = beforeValue != afterValue;
    }

    public uint OffsetValue { get; }

    public byte BeforeValue { get; }

    public byte AfterValue { get; }

    public string Offset { get; }

    public string BeforeByte { get; }

    public string AfterByte { get; }

    public string BeforeBinary { get; }

    public string AfterBinary { get; }

    public string ChangedBits { get; }

    public int ChangedBitCount { get; }

    public bool IsChanged { get; }

    public bool IsSingleBitChange => IsChanged && ChangedBitCount == 1;

    public int CandidateScore =>
        (IsChanged ? 1 : 0) +
        (IsSingleBitChange ? 5 : 0) +
        (IsChanged && ChangedBitCount is > 1 and <= 3 ? 2 : 0) +
        (CandidateGroup != "-" ? 2 : 0);

    public string CandidateGroup
    {
        get => _candidateGroup;
        private set
        {
            if (SetField(ref _candidateGroup, value))
            {
                OnPropertyChanged(nameof(CandidateScore));
            }
        }
    }

    public void SetCandidateGroup(string groupName)
    {
        CandidateGroup = groupName;
    }

    private static string FormatByte(byte value)
    {
        return $"{value} / 0x{value:X2}";
    }

    private static string ToBinary(byte value)
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
