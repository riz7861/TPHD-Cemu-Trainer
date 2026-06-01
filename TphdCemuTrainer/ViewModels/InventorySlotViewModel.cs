using TphdCemuTrainer.Cheats;

namespace TphdCemuTrainer.ViewModels;

public sealed class InventorySlotViewModel : ObservableObject
{
    private string _currentItemId = "Not read";
    private string _currentItemName = "Not read";
    private InventoryItemDefinition _selectedItem;
    private bool _canEdit = true;

    public InventorySlotViewModel(int slotIndex, IReadOnlyList<InventoryItemDefinition> availableItems)
    {
        SlotIndex = slotIndex;
        SlotNumber = slotIndex + 1;
        OffsetValue = InventoryDefinitions.FirstSlotOffset + (uint)slotIndex;
        AvailableItems = availableItems;
        _selectedItem = InventoryDefinitions.Nothing;
    }

    public int SlotIndex { get; }

    public int SlotNumber { get; }

    public uint OffsetValue { get; }

    public string Offset => InventoryDefinitions.GetSlotOffset(SlotIndex);

    public string SlotLabel => InventoryDefinitions.GetSlotLabel(SlotIndex);

    public string ManagementStatus => InventoryDefinitions.GetManagementStatus(SlotIndex);

    public string Notes => InventoryDefinitions.GetSlotNotes(SlotIndex);

    public IReadOnlyList<InventoryItemDefinition> AvailableItems { get; }

    public string CurrentItemId
    {
        get => _currentItemId;
        private set => SetField(ref _currentItemId, value);
    }

    public string CurrentItemName
    {
        get => _currentItemName;
        private set => SetField(ref _currentItemName, value);
    }

    public InventoryItemDefinition SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (value is not null)
            {
                SetField(ref _selectedItem, value);
            }
        }
    }

    public bool CanEdit
    {
        get => _canEdit;
        set => SetField(ref _canEdit, value);
    }

    public void SetCurrentItem(byte itemId)
    {
        CurrentItemId = itemId.ToString();
        CurrentItemName = InventoryDefinitions.GetItemName(itemId);
        SelectedItem = InventoryDefinitions.GetDefaultSelection(itemId);
    }

    public void MarkNotRead()
    {
        CurrentItemId = "Not read";
        CurrentItemName = "Not read";
    }
}
