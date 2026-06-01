using TphdCemuTrainer.Cheats;

namespace TphdCemuTrainer.ViewModels;

public sealed class InventoryRemovalItemViewModel : ObservableObject
{
    private byte? _currentItemId;
    private byte? _previousItemId;
    private string _status = "Detected";

    public InventoryRemovalItemViewModel(int slotIndex)
    {
        SlotIndex = slotIndex;
        SlotNumber = slotIndex + 1;
        OffsetValue = InventoryDefinitions.FirstSlotOffset + (uint)slotIndex;
    }

    public int SlotIndex { get; }

    public int SlotNumber { get; }

    public uint OffsetValue { get; }

    public string SlotLabel => InventoryDefinitions.GetSlotLabel(SlotIndex);

    public string Offset => InventoryDefinitions.GetSlotOffset(SlotIndex);

    public byte? CurrentItemId
    {
        get => _currentItemId;
        private set
        {
            if (SetField(ref _currentItemId, value))
            {
                OnPropertyChanged(nameof(CurrentRawValue));
                OnPropertyChanged(nameof(CurrentItemName));
                OnPropertyChanged(nameof(CanRemove));
            }
        }
    }

    public byte? PreviousItemId
    {
        get => _previousItemId;
        private set
        {
            if (SetField(ref _previousItemId, value))
            {
                OnPropertyChanged(nameof(PreviousRawValue));
                OnPropertyChanged(nameof(PreviousItemName));
                OnPropertyChanged(nameof(CanRestore));
            }
        }
    }

    public string CurrentRawValue => FormatItemId(CurrentItemId);

    public string CurrentItemName => CurrentItemId.HasValue
        ? InventoryDefinitions.GetItemName(CurrentItemId.Value)
        : "Not read";

    public string PreviousRawValue => FormatItemId(PreviousItemId);

    public string PreviousItemName => PreviousItemId.HasValue
        ? InventoryDefinitions.GetItemName(PreviousItemId.Value)
        : "-";

    public string Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }

    public bool CanRemove => CurrentItemId.HasValue && CurrentItemId.Value != InventoryDefinitions.EmptyItemId;

    public bool CanRestore => PreviousItemId.HasValue;

    public void SetCurrentItem(byte itemId)
    {
        CurrentItemId = itemId;
        Status = itemId == InventoryDefinitions.EmptyItemId
            ? "Currently removed / empty"
            : "Detected";
    }

    public void CapturePrevious(byte itemId)
    {
        PreviousItemId = itemId;
        Status = $"Previous captured: {FormatItemId(itemId)}";
    }

    public void ClearPrevious()
    {
        PreviousItemId = null;
    }

    public void MarkNotRead()
    {
        CurrentItemId = null;
        Status = "Not read";
    }

    private static string FormatItemId(byte? itemId)
    {
        return itemId.HasValue
            ? $"{itemId.Value} / 0x{itemId.Value:X2}"
            : "-";
    }
}
