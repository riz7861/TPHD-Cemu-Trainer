using TphdCemuTrainer.Cheats;

namespace TphdCemuTrainer.ViewModels;

public sealed class EquipmentFlagViewModel : ObservableObject
{
    private bool _isOwned;
    private string _backingValue = "Not read";
    private bool _canEdit;

    public EquipmentFlagViewModel(EquipmentFlagDefinition definition)
    {
        Definition = definition;
    }

    public EquipmentFlagDefinition Definition { get; }

    public string Name => Definition.Name;

    public uint OffsetValue => Definition.Offset;

    public string Offset => Definition.OffsetText;

    public int Bit => Definition.Bit;

    public string Source => Definition.Source;

    public bool IsOwned
    {
        get => _isOwned;
        private set => SetField(ref _isOwned, value);
    }

    public string BackingValue
    {
        get => _backingValue;
        private set => SetField(ref _backingValue, value);
    }

    public bool CanEdit
    {
        get => _canEdit;
        set => SetField(ref _canEdit, value);
    }

    public void SetCurrentValue(bool isOwned, byte backingValue)
    {
        IsOwned = isOwned;
        BackingValue = $"0x{backingValue:X2}";
    }

    public void MarkNotRead()
    {
        IsOwned = false;
        BackingValue = "Not read";
    }
}
