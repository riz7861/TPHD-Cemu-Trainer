namespace TphdCemuTrainer.ViewModels;

public sealed class QuestItemsCandidateGroupViewModel
{
    public QuestItemsCandidateGroupViewModel(
        string groupName,
        uint firstOffset,
        uint lastOffset,
        int count,
        int highestCandidateScore,
        string reasons)
    {
        GroupName = groupName;
        OffsetRange = firstOffset == lastOffset
            ? $"0x{firstOffset:X}"
            : $"0x{firstOffset:X}-0x{lastOffset:X}";
        Count = count;
        HighestCandidateScore = highestCandidateScore;
        Reasons = reasons;
    }

    public string GroupName { get; }

    public string OffsetRange { get; }

    public int Count { get; }

    public int HighestCandidateScore { get; }

    public string Reasons { get; }
}
