using TphdCemuTrainer.Cheats;

namespace TphdCemuTrainer.ViewModels;

public sealed class InventoryFixedSlotViewModel : ObservableObject
{
    private string _currentItemId = "Not read";
    private string _currentItemName = "Not read";
    private InventoryItemDefinition _selectedItem;
    private bool _canEdit;

    public InventoryFixedSlotViewModel(InventoryFixedSlotDefinition definition)
    {
        Definition = definition;
        _selectedItem = definition.AllowedItems[0];
    }

    public InventoryFixedSlotDefinition Definition { get; }

    public string Name => Definition.Name;

    public int SlotIndex => Definition.SlotIndex;

    public int SlotNumber => Definition.SlotNumber;

    public uint OffsetValue => Definition.OffsetValue;

    public string Offset => Definition.Offset;

    public string ManagementStatus => Definition.ManagementStatus;

    public string Notes => Definition.Notes;

    public IReadOnlyList<InventoryItemDefinition> AllowedItems => Definition.AllowedItems;

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
        SelectedItem = InventoryDefinitions.GetFixedSlotDefaultSelection(Definition, itemId);
    }

    public void MarkNotRead()
    {
        CurrentItemId = "Not read";
        CurrentItemName = "Not read";
    }
}
