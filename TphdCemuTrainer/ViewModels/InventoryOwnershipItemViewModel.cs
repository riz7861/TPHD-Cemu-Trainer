using TphdCemuTrainer.Cheats;

namespace TphdCemuTrainer.ViewModels;

public sealed class InventoryOwnershipItemViewModel : ObservableObject
{
    private string _currentDetectedState = "Not read";
    private bool _isOwnedDetected;
    private bool _isOwnedDesired;
    private string _backingValue = "Not read";
    private bool _canEdit;
    private int? _knownSlotIndex;
    private byte? _knownItemId;
    private string _knownMappingSource = "None";
    private string _experimentalStatus = "Not tested";

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

    public int? KnownSlotIndex
    {
        get => _knownSlotIndex;
        private set
        {
            if (SetField(ref _knownSlotIndex, value))
            {
                OnPropertyChanged(nameof(KnownSlotText));
                OnPropertyChanged(nameof(CanExperimentalCheckboxWrite));
                OnPropertyChanged(nameof(DesiredEditStateText));
                OnPropertyChanged(nameof(FriendlyStatus));
            }
        }
    }

    public byte? KnownItemId
    {
        get => _knownItemId;
        private set
        {
            if (SetField(ref _knownItemId, value))
            {
                OnPropertyChanged(nameof(KnownItemText));
                OnPropertyChanged(nameof(CanExperimentalCheckboxWrite));
                OnPropertyChanged(nameof(DesiredEditStateText));
                OnPropertyChanged(nameof(FriendlyStatus));
            }
        }
    }

    public string KnownSlotText => KnownSlotIndex.HasValue
        ? $"{KnownMappingSource} slot {KnownSlotIndex.Value + 1} ({InventoryDefinitions.GetSlotOffset(KnownSlotIndex.Value)})"
        : "No visible slot captured";

    public string KnownItemText => KnownItemId.HasValue
        ? $"{KnownItemId.Value} / {InventoryDefinitions.GetItemName(KnownItemId.Value)}"
        : "No item ID captured";

    public bool CanExperimentalCheckboxWrite =>
        KnownSlotIndex.HasValue &&
        KnownItemId.HasValue &&
        Definition.Id is not "bottles";

    public string KnownMappingSource
    {
        get => _knownMappingSource;
        private set
        {
            if (SetField(ref _knownMappingSource, value))
            {
                OnPropertyChanged(nameof(KnownSlotText));
            }
        }
    }

    public string ExperimentalStatus
    {
        get => _experimentalStatus;
        set
        {
            if (SetField(ref _experimentalStatus, value))
            {
                OnPropertyChanged(nameof(FriendlyStatus));
            }
        }
    }

    public string BackingValue
    {
        get => _backingValue;
        private set => SetField(ref _backingValue, value);
    }

    public string CurrentDetectedState
    {
        get => _currentDetectedState;
        private set
        {
            if (SetField(ref _currentDetectedState, value))
            {
                OnPropertyChanged(nameof(FriendlyCurrentState));
                OnPropertyChanged(nameof(FriendlyStatus));
            }
        }
    }

    public bool IsOwnedDetected
    {
        get => _isOwnedDetected;
        private set
        {
            if (SetField(ref _isOwnedDetected, value))
            {
                OnPropertyChanged(nameof(IsDirty));
                OnPropertyChanged(nameof(FriendlyCurrentState));
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
                OnPropertyChanged(nameof(DesiredEditStateText));
            }
        }
    }

    public bool IsDirty => IsOwnedDesired != IsOwnedDetected;

    public string FriendlyCurrentState
    {
        get
        {
            if (string.Equals(CurrentDetectedState, "Not read", StringComparison.OrdinalIgnoreCase))
            {
                return "Not read";
            }

            if (CurrentDetectedState.Contains("not initialized", StringComparison.OrdinalIgnoreCase))
            {
                return "Not ready";
            }

            return IsOwnedDetected ? "Present" : "Not present";
        }
    }

    public string FriendlyStatus
    {
        get
        {
            if (string.Equals(CurrentDetectedState, "Not read", StringComparison.OrdinalIgnoreCase))
            {
                return "Not read";
            }

            if (ExperimentalStatus.Contains("failed", StringComparison.OrdinalIgnoreCase) ||
                ExperimentalStatus.Contains("reverted", StringComparison.OrdinalIgnoreCase) ||
                ExperimentalStatus.Contains("error", StringComparison.OrdinalIgnoreCase))
            {
                return "Failed";
            }

            if (ExperimentalStatus.Contains("memory changed", StringComparison.OrdinalIgnoreCase))
            {
                return "Applied";
            }

            if (CurrentDetectedState.Contains("not initialized", StringComparison.OrdinalIgnoreCase))
            {
                return "Requires story progress";
            }

            return CanExperimentalCheckboxWrite ? "Ready" : "Read only";
        }
    }

    public string DesiredEditStateText
    {
        get
        {
            if (CanEdit)
            {
                return IsOwnedDesired ? "Will add/keep" : "Will remove";
            }

            return CanExperimentalCheckboxWrite ? "Locked" : "Read only";
        }
    }

    public bool CanEdit
    {
        get => _canEdit;
        set
        {
            if (SetField(ref _canEdit, value))
            {
                OnPropertyChanged(nameof(DesiredEditStateText));
                OnPropertyChanged(nameof(FriendlyStatus));
            }
        }
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
            ApplyStaticVisibleSlotFallback();
            return;
        }

        if (matches.Count == 0 && Definition.HasStaticVisibleSlotMapping)
        {
            ApplyStaticVisibleSlotFallback();
            CurrentDetectedState = rawSlots.All(value => value == InventoryDefinitions.EmptyItemId)
                ? "Not detected in visible CT slots; static mapping available"
                : "Not detected in visible CT slots; static mapping available";
            return;
        }

        if (rawSlots.All(value => value == InventoryDefinitions.EmptyItemId))
        {
            ClearKnownVisibleSlot();
            CurrentDetectedState = "Inventory not initialized / not detected";
            return;
        }

        if (matches.Count == 0)
        {
            ClearKnownVisibleSlot();
            CurrentDetectedState = "Not detected in visible CT slots";
            return;
        }

        var firstMatch = matches[0];
        KnownSlotIndex = firstMatch.SlotIndex;
        KnownItemId = firstMatch.Value;
        KnownMappingSource = "Detected";
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
        ClearKnownVisibleSlot();
        ExperimentalStatus = "Not tested";
        OnPropertyChanged(nameof(IsDirty));
    }

    private void ApplyStaticVisibleSlotFallback()
    {
        if (!Definition.HasStaticVisibleSlotMapping ||
            !Definition.StaticSlotIndex.HasValue ||
            !Definition.StaticItemId.HasValue)
        {
            ClearKnownVisibleSlot();
            return;
        }

        KnownSlotIndex = Definition.StaticSlotIndex.Value;
        KnownItemId = Definition.StaticItemId.Value;
        KnownMappingSource = "Static";
    }

    private void ClearKnownVisibleSlot()
    {
        KnownSlotIndex = null;
        KnownItemId = null;
        KnownMappingSource = "None";
    }
}
