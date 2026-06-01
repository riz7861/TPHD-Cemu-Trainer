using TphdCemuTrainer.Cheats;

namespace TphdCemuTrainer.ViewModels;

public sealed class EquipmentFlagViewModel : ObservableObject
{
    private bool _isOwnedDetected;
    private bool _isOwnedDesired;
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

    public bool IsOwnedDetected
    {
        get => _isOwnedDetected;
        private set
        {
            if (SetField(ref _isOwnedDetected, value))
            {
                OnPropertyChanged(nameof(IsDirty));
            }
        }
    }

    public bool IsOwnedDesired
    {
        get => _isOwnedDesired;
        set
        {
            if (SetField(ref _isOwnedDesired, value))
            {
                OnPropertyChanged(nameof(IsDirty));
            }
        }
    }

    public bool IsDirty => IsOwnedDesired != IsOwnedDetected;

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

    public void SetDetectedValue(bool isOwned, byte backingValue, bool preserveDirty)
    {
        var wasDirty = IsDirty;
        IsOwnedDetected = isOwned;
        if (!preserveDirty || !wasDirty)
        {
            IsOwnedDesired = isOwned;
        }

        BackingValue = $"0x{backingValue:X2}";
        OnPropertyChanged(nameof(IsDirty));
    }

    public void MarkNotRead()
    {
        IsOwnedDetected = false;
        IsOwnedDesired = false;
        BackingValue = "Not read";
        OnPropertyChanged(nameof(IsDirty));
    }
}
