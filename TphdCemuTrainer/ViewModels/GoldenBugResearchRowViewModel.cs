namespace TphdCemuTrainer.ViewModels;

public sealed class GoldenBugResearchRowViewModel : ObservableObject
{
    public GoldenBugResearchRowViewModel(
        uint offsetValue,
        byte beforeValue,
        byte afterValue,
        int byteIndex,
        int bitIndex,
        int candidateBugIndex,
        string candidateBugName)
    {
        OffsetValue = offsetValue;
        BeforeValue = beforeValue;
        AfterValue = afterValue;
        ByteIndex = byteIndex;
        BitIndex = bitIndex;
        CandidateBugIndexValue = candidateBugIndex;
        Offset = $"0x{offsetValue:X}";
        BeforeByte = FormatByte(beforeValue);
        AfterByte = FormatByte(afterValue);
        BeforeBinary = ToBinary(beforeValue);
        AfterBinary = ToBinary(afterValue);
        ChangedBits = $"bit {bitIndex}";
        CandidateBugIndex = candidateBugIndex > 0
            ? candidateBugIndex.ToString()
            : "-";
        CandidateBugName = candidateBugName;
        IsChanged = beforeValue != afterValue;
    }

    public uint OffsetValue { get; }

    public byte BeforeValue { get; }

    public byte AfterValue { get; }

    public int ByteIndex { get; }

    public int BitIndex { get; }

    public int CandidateBugIndexValue { get; }

    public string Offset { get; }

    public string BeforeByte { get; }

    public string AfterByte { get; }

    public string BeforeBinary { get; }

    public string AfterBinary { get; }

    public string ChangedBits { get; }

    public string CandidateBugIndex { get; }

    public string CandidateBugName { get; }

    public bool IsChanged { get; }

    private static string FormatByte(byte value)
    {
        return $"{value} / 0x{value:X2}";
    }

    private static string ToBinary(byte value)
    {
        return Convert.ToString(value, 2).PadLeft(8, '0');
    }
}
