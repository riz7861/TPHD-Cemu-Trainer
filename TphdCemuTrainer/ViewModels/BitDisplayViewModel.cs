namespace TphdCemuTrainer.ViewModels;

public sealed class BitDisplayViewModel
{
    public BitDisplayViewModel(int bitIndex, char value, bool isChanged)
    {
        BitIndex = bitIndex;
        Value = value.ToString();
        IsChanged = isChanged;
    }

    public int BitIndex { get; }

    public string Value { get; }

    public bool IsChanged { get; }

    public string ToolTip => $"bit {BitIndex}";
}
