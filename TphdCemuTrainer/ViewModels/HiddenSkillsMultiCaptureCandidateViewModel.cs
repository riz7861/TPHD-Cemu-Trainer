namespace TphdCemuTrainer.ViewModels;

public sealed class HiddenSkillsMultiCaptureCandidateViewModel
{
    public HiddenSkillsMultiCaptureCandidateViewModel(
        int rank,
        uint offsetValue,
        string kind,
        string bit,
        string values,
        string skillCounts,
        int score,
        bool isMonotonic,
        bool onlyIncreases,
        bool progressionMatch,
        string notes)
    {
        Rank = rank;
        OffsetValue = offsetValue;
        Kind = kind;
        Bit = bit;
        Values = values;
        SkillCounts = skillCounts;
        Score = score;
        IsMonotonic = isMonotonic;
        OnlyIncreases = onlyIncreases;
        ProgressionMatch = progressionMatch;
        Notes = notes;
    }

    public int Rank { get; }

    public uint OffsetValue { get; }

    public string Offset => $"0x{OffsetValue:X}";

    public string Kind { get; }

    public string Bit { get; }

    public string Values { get; }

    public string SkillCounts { get; }

    public int Score { get; }

    public bool IsMonotonic { get; }

    public bool OnlyIncreases { get; }

    public bool ProgressionMatch { get; }

    public string Notes { get; }

    public string Flags
    {
        get
        {
            var flags = new List<string>();
            if (ProgressionMatch)
            {
                flags.Add("matches 1->2->3->5->6->7");
            }

            if (OnlyIncreases)
            {
                flags.Add("only increases");
            }

            if (IsMonotonic)
            {
                flags.Add("monotonic");
            }

            return flags.Count == 0 ? "-" : string.Join(", ", flags);
        }
    }
}
