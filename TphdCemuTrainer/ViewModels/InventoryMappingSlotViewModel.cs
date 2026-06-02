using TphdCemuTrainer.Cheats;

namespace TphdCemuTrainer.ViewModels;

public sealed class InventoryMappingSlotViewModel : ObservableObject
{
    private byte _rawValue = InventoryDefinitions.EmptyItemId;
    private string _selectedResearchGroup;
    private string _rowNote = string.Empty;
    private string _columnNote = string.Empty;
    private string _notes = string.Empty;

    public InventoryMappingSlotViewModel(int slotIndex)
    {
        SlotIndex = slotIndex;
        SlotNumber = slotIndex + 1;
        OffsetValue = InventoryDefinitions.FirstSlotOffset + (uint)slotIndex;
        DetectedVisualGroup = InferDetectedVisualGroup(OffsetValue);
        _selectedResearchGroup = InferDefaultResearchGroup(OffsetValue);
    }

    public static IReadOnlyList<string> ResearchGroups { get; } =
    [
        "Primary / Progression Item",
        "Bomb",
        "Bottle",
        "Special",
        "Quest",
        "Unknown"
    ];

    public int SlotIndex { get; }

    public int SlotNumber { get; }

    public uint OffsetValue { get; }

    public string Offset => InventoryDefinitions.GetSlotOffset(SlotIndex);

    public byte RawValue
    {
        get => _rawValue;
        private set
        {
            if (SetField(ref _rawValue, value))
            {
                OnPropertyChanged(nameof(RawValueText));
                OnPropertyChanged(nameof(DecodedItem));
            }
        }
    }

    public string RawValueText => $"{RawValue} / 0x{RawValue:X2}";

    public string DecodedItem => InventoryDefinitions.GetItemName(RawValue);

    public string DetectedVisualGroup { get; }

    public IReadOnlyList<string> AvailableResearchGroups => ResearchGroups;

    public string SelectedResearchGroup
    {
        get => _selectedResearchGroup;
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                SetField(ref _selectedResearchGroup, value);
            }
        }
    }

    public string RowNote
    {
        get => _rowNote;
        set => SetField(ref _rowNote, value ?? string.Empty);
    }

    public string ColumnNote
    {
        get => _columnNote;
        set => SetField(ref _columnNote, value ?? string.Empty);
    }

    public string Notes
    {
        get => _notes;
        set => SetField(ref _notes, value ?? string.Empty);
    }

    public void SetCurrentItem(byte itemId)
    {
        RawValue = itemId;
    }

    public void MarkNotRead()
    {
        RawValue = InventoryDefinitions.EmptyItemId;
    }

    private static string InferDetectedVisualGroup(uint offset)
    {
        return offset switch
        {
            >= 0x258 and <= 0x261 => "Primary / Progression Item",
            >= 0x263 and <= 0x266 => "Bottle",
            >= 0x267 and <= 0x269 => "Bomb",
            0x26A => "Quest/Special",
            0x26C => "Special - Fishing Rod",
            0x26D => "Special - Horse Call",
            0x26F => "Special - Slingshot",
            _ => "Unknown"
        };
    }

    private static string InferDefaultResearchGroup(uint offset)
    {
        return offset switch
        {
            >= 0x258 and <= 0x261 => "Primary / Progression Item",
            >= 0x263 and <= 0x266 => "Bottle",
            >= 0x267 and <= 0x269 => "Bomb",
            0x26A => "Quest",
            0x26C or 0x26D or 0x26F => "Special",
            _ => "Unknown"
        };
    }
}
