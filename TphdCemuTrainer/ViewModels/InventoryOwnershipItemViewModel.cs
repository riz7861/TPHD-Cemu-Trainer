using TphdCemuTrainer.Cheats;

namespace TphdCemuTrainer.ViewModels;

public sealed class InventoryOwnershipItemViewModel : ObservableObject
{
    private string _currentDetectedState = "Not read";
    private bool _targetOwned;
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

    public string CurrentDetectedState
    {
        get => _currentDetectedState;
        private set => SetField(ref _currentDetectedState, value);
    }

    public bool TargetOwned
    {
        get => _targetOwned;
        set => SetField(ref _targetOwned, value);
    }

    public bool CanEdit
    {
        get => _canEdit;
        set => SetField(ref _canEdit, value);
    }

    public void SetDetectedState(IReadOnlyList<byte> rawSlots)
    {
        var matches = rawSlots
            .Select((value, index) => new { Value = value, SlotIndex = index })
            .Where(slot => Definition.DetectedItemIds.Contains(slot.Value))
            .ToList();

        TargetOwned = matches.Count > 0;
        CanEdit = Definition.CanWrite;

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
        TargetOwned = false;
        CanEdit = false;
    }
}
