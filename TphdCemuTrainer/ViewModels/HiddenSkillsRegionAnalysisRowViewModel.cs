namespace TphdCemuTrainer.ViewModels;

public sealed class HiddenSkillsRegionAnalysisRowViewModel
{
    public HiddenSkillsRegionAnalysisRowViewModel(
        int rank,
        uint regionStartValue,
        uint regionEndValue,
        int changedBytes,
        int changedBits,
        double density,
        int largestChange,
        int candidateScore)
    {
        Rank = rank;
        RegionStartValue = regionStartValue;
        RegionEndValue = regionEndValue;
        ChangedBytes = changedBytes;
        ChangedBits = changedBits;
        Density = density;
        LargestChange = largestChange;
        CandidateScore = candidateScore;
    }

    public int Rank { get; }

    public uint RegionStartValue { get; }

    public uint RegionEndValue { get; }

    public string Region => $"0x{RegionStartValue:X}-0x{RegionEndValue:X}";

    public int ChangedBytes { get; }

    public int ChangedBits { get; }

    public double Density { get; }

    public string DensityPercent => $"{Density:0.0}%";

    public int LargestChange { get; }

    public int CandidateScore { get; }

    public bool IsSingleBitRegion => ChangedBytes == 1 && ChangedBits == 1;

    public bool IsSparseRegion => Density <= 20.0;

    public string HighlightLabel
    {
        get
        {
            var labels = new List<string>();
            if (IsSingleBitRegion)
            {
                labels.Add("single bit");
            }

            if (IsSparseRegion)
            {
                labels.Add("sparse");
            }

            if (ChangedBytes <= 3)
            {
                labels.Add("low byte count");
            }

            return labels.Count == 0 ? "-" : string.Join(", ", labels);
        }
    }
}
