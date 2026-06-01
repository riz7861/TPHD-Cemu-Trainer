using TphdCemuTrainer.Cheats;

namespace TphdCemuTrainer.ViewModels;

public sealed class InventoryOwnershipItemViewModel : ObservableObject
{
    private string _currentDetectedState = "Not read";
    private bool _isOwnedDetected;
    private bool _isOwnedDesired;
    private string _backingValue = "Not read";
    private bool _canEdit;

    public InventoryOwnershipItemViewModel(InventoryOwnershipDefinition definition)
    {
        Definition = definition;
        _canEdit = definition.CanWrite;
    }

    public InventoryOwnershipDefinition Definition { get; }

    public string Name => Definition.Name;

    public string FlagLocation => Definition.FlagLocation;

    public string Notes => Definition.Notes;

    public string EditStatus => Definition.EditStatus;

    public string Source => Definition.Source;

    public uint? FlagOffsetValue => Definition.FlagOffset;

    public string BackingValue
    {
        get => _backingValue;
        private set => SetField(ref _backingValue, value);
    }

    public string CurrentDetectedState
    {
        get => _currentDetectedState;
        private set => SetField(ref _currentDetectedState, value);
    }

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

    public bool CanEdit
    {
        get => _canEdit;
        set => SetField(ref _canEdit, value);
    }

    public void SetDetectedFlag(bool isOwned, byte backingValue, bool preserveDirty)
    {
        var wasDirty = IsDirty;
        IsOwnedDetected = isOwned;
        if (!preserveDirty || !wasDirty)
        {
            IsOwnedDesired = isOwned;
        }

        BackingValue = $"0x{backingValue:X2}";
        CurrentDetectedState = isOwned ? "Owned flag set" : "Owned flag not set";
        CanEdit = Definition.CanWrite;
        OnPropertyChanged(nameof(IsDirty));
    }

    public void SetDetectedFromVisibleSlots(IReadOnlyList<byte> rawSlots, bool preserveDirty)
    {
        var matches = rawSlots
            .Select((value, index) => new { Value = value, SlotIndex = index })
            .Where(slot => Definition.DetectedItemIds.Contains(slot.Value))
            .ToList();

        var wasDirty = IsDirty;
        IsOwnedDetected = matches.Count > 0;
        if (!preserveDirty || !wasDirty)
        {
            IsOwnedDesired = IsOwnedDetected;
        }

        BackingValue = "n/a";
        CanEdit = false;

        if (rawSlots.Count == 0)
        {
            CurrentDetectedState = "Not read";
            return;
        }

        if (rawSlots.All(value => value == InventoryDefinitions.EmptyItemId))
        {
            CurrentDetectedState = "Inventory not initialized / not detected";
            return;
        }

        if (matches.Count == 0)
        {
            CurrentDetectedState = "Not detected in visible CT slots";
            return;
        }

        CurrentDetectedState = string.Join(
            "; ",
            matches.Select(match =>
                $"Slot {match.SlotIndex + 1} ({InventoryDefinitions.GetSlotOffset(match.SlotIndex)}): {InventoryDefinitions.GetItemName(match.Value)}"));
    }

    public void MarkNotRead()
    {
        CurrentDetectedState = "Not read";
        IsOwnedDetected = false;
        IsOwnedDesired = false;
        BackingValue = "Not read";
        CanEdit = false;
        OnPropertyChanged(nameof(IsDirty));
    }
}
