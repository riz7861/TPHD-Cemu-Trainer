namespace TphdCemuTrainer.ViewModels;

public sealed class QuestItemsCandidateRowViewModel
{
    public QuestItemsCandidateRowViewModel(
        uint offsetValue,
        byte beforeValue,
        byte afterValue,
        string changedBits,
        int changedBitCount,
        bool isChanged,
        bool isSingleBitChange,
        bool isPersisted,
        bool isGrouped,
        int score,
        string confidence,
        string groupName,
        string reasons)
    {
        OffsetValue = offsetValue;
        Offset = $"0x{offsetValue:X}";
        BeforeValue = beforeValue;
        AfterValue = afterValue;
        BeforeHex = $"0x{beforeValue:X2}";
        AfterHex = $"0x{afterValue:X2}";
        ChangedBits = changedBits;
        ChangedBitCount = changedBitCount;
        IsChanged = isChanged;
        IsSingleBitChange = isSingleBitChange;
        IsPersisted = isPersisted;
        IsGrouped = isGrouped;
        CandidateScore = score;
        Confidence = confidence;
        GroupName = groupName;
        Reasons = reasons;
    }

    public uint OffsetValue { get; }

    public string Offset { get; }

    public byte BeforeValue { get; }

    public byte AfterValue { get; }

    public string BeforeHex { get; }

    public string AfterHex { get; }

    public string ChangedBits { get; }

    public int ChangedBitCount { get; }

    public bool IsChanged { get; }

    public bool IsSingleBitChange { get; }

    public bool IsPersisted { get; }

    public bool IsGrouped { get; }

    public int CandidateScore { get; }

    public string Confidence { get; }

    public string GroupName { get; }

    public string Reasons { get; }
}
