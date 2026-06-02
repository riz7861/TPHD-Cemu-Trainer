namespace TphdCemuTrainer.ViewModels;

public sealed class HiddenSkillsResearchRowViewModel : ObservableObject
{
    private bool _isClusteredChange;
    private bool _isPinned;
    private string _groupName = "-";

    public HiddenSkillsResearchRowViewModel(
        uint offset,
        byte beforeValue,
        byte afterValue,
        bool treatAfterAsPersisted)
    {
        OffsetValue = offset;
        BeforeValue = beforeValue;
        AfterValue = afterValue;
        TreatAfterAsPersisted = treatAfterAsPersisted;
        BeforeBinary = FormatBinary(beforeValue);
        AfterBinary = FormatBinary(afterValue);
        ChangedBitCount = CountChangedBits(beforeValue, afterValue);
        ChangedBits = FormatChangedBits(beforeValue, afterValue);
    }

    public uint OffsetValue { get; }

    public byte BeforeValue { get; }

    public byte AfterValue { get; }

    public bool TreatAfterAsPersisted { get; }

    public string Offset => $"0x{OffsetValue:X}";

    public string BeforeByte => FormatByte(BeforeValue);

    public string AfterByte => FormatByte(AfterValue);

    public string BeforeBinary { get; }

    public string AfterBinary { get; }

    public string ChangedBits { get; }

    public int ChangedBitCount { get; }

    public bool IsChanged => BeforeValue != AfterValue;

    public bool IsSingleBitChange => IsChanged && ChangedBitCount == 1;

    public bool IsPersistedChange => IsChanged && TreatAfterAsPersisted;

    public bool IsClusteredChange
    {
        get => _isClusteredChange;
        private set
        {
            if (SetField(ref _isClusteredChange, value))
            {
                OnPropertyChanged(nameof(CandidateScore));
                OnPropertyChanged(nameof(HighlightLabel));
                OnPropertyChanged(nameof(IsCandidate));
            }
        }
    }

    public bool IsPinned
    {
        get => _isPinned;
        set
        {
            if (SetField(ref _isPinned, value))
            {
                OnPropertyChanged(nameof(CandidateScore));
                OnPropertyChanged(nameof(HighlightLabel));
                OnPropertyChanged(nameof(IsCandidate));
            }
        }
    }

    public string GroupName
    {
        get => _groupName;
        private set => SetField(ref _groupName, value);
    }

    public int CandidateScore =>
        (IsChanged ? 1 : 0) +
        (IsSingleBitChange ? 2 : 0) +
        (IsPersistedChange ? 2 : 0) +
        (IsClusteredChange ? 1 : 0) +
        (IsPinned ? 3 : 0);

    public bool IsCandidate => IsPinned || IsSingleBitChange || IsPersistedChange || CandidateScore >= 6;

    public string HighlightLabel
    {
        get
        {
            var labels = new List<string>();
            if (IsSingleBitChange)
            {
                labels.Add("single bit");
            }

            if (IsPersistedChange)
            {
                labels.Add("persisted");
            }

            if (IsClusteredChange)
            {
                labels.Add("clustered");
            }

            if (IsPinned)
            {
                labels.Add("pinned");
            }

            return labels.Count == 0 ? "-" : string.Join(", ", labels);
        }
    }

    public void SetClusteredChange(bool isClustered)
    {
        IsClusteredChange = isClustered;
    }

    public void SetGroupName(string groupName)
    {
        GroupName = groupName;
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
