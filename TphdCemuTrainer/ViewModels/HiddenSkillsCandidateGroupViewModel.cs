namespace TphdCemuTrainer.ViewModels;

public sealed class HiddenSkillsCandidateGroupViewModel
{
    public HiddenSkillsCandidateGroupViewModel(string name, IReadOnlyList<HiddenSkillsResearchRowViewModel> rows)
    {
        Name = name;
        Rows = rows;
        Offsets = string.Join(", ", rows.Select(row => row.Offset));
        Count = rows.Count;
        HighestScore = rows.Count == 0 ? 0 : rows.Max(row => row.CandidateScore);
        Reasons = string.Join(
            ", ",
            rows.Select(row => row.HighlightLabel)
                .Where(reason => !string.IsNullOrWhiteSpace(reason) && reason != "-")
                .SelectMany(reason => reason.Split(", ", StringSplitOptions.RemoveEmptyEntries))
                .Distinct(StringComparer.Ordinal));
    }

    public string Name { get; }

    public IReadOnlyList<HiddenSkillsResearchRowViewModel> Rows { get; }

    public string Offsets { get; }

    public int Count { get; }

    public int HighestScore { get; }

    public string Reasons { get; }
}
