namespace TphdCemuTrainer.ViewModels;

public sealed class OwnershipCorrelationRowViewModel
{
    public OwnershipCorrelationRowViewModel(
        uint offset,
        int changedCount,
        IEnumerable<string> associatedItemGains,
        IEnumerable<string> reportNames,
        IEnumerable<string> bitChanges)
    {
        OffsetValue = offset;
        ChangedCount = changedCount;
        AssociatedItemGains = FormatList(associatedItemGains);
        Reports = FormatList(reportNames);
        BitChanges = FormatList(bitChanges);
    }

    public uint OffsetValue { get; }

    public string Offset => $"0x{OffsetValue:X}";

    public int ChangedCount { get; }

    public string AssociatedItemGains { get; }

    public string Reports { get; }

    public string BitChanges { get; }

    private static string FormatList(IEnumerable<string> values)
    {
        var distinctValues = values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return distinctValues.Count == 0
            ? "-"
            : string.Join("; ", distinctValues);
    }
}
