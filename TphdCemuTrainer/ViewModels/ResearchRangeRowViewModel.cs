namespace TphdCemuTrainer.ViewModels;

public sealed class ResearchRangeRowViewModel : ObservableObject
{
    private string _currentByte = "Not read";
    private string _currentItemName = "-";
    private bool _isChanged;

    public ResearchRangeRowViewModel(uint offset, byte beforeValue, string beforeItemName)
    {
        OffsetValue = offset;
        BeforeValue = beforeValue;
        Offset = $"0x{offset:X}";
        BeforeByte = FormatByte(beforeValue);
        BeforeItemName = beforeItemName;
        CurrentByte = BeforeByte;
        CurrentItemName = beforeItemName;
    }

    public uint OffsetValue { get; }

    public byte BeforeValue { get; }

    public string Offset { get; }

    public string BeforeByte { get; }

    public string BeforeItemName { get; }

    public string CurrentByte
    {
        get => _currentByte;
        private set => SetField(ref _currentByte, value);
    }

    public string CurrentItemName
    {
        get => _currentItemName;
        private set => SetField(ref _currentItemName, value);
    }

    public bool IsChanged
    {
        get => _isChanged;
        private set => SetField(ref _isChanged, value);
    }

    public void SetCurrent(byte currentValue, string currentItemName)
    {
        CurrentByte = FormatByte(currentValue);
        CurrentItemName = currentItemName;
        IsChanged = currentValue != BeforeValue;
    }

    private static string FormatByte(byte value)
    {
        return $"{value} / 0x{value:X2}";
    }
}
