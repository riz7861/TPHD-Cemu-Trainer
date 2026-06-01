using TphdCemuTrainer.Cheats;

namespace TphdCemuTrainer.ViewModels;

public sealed class EquipmentSlotViewModel : ObservableObject
{
    private string _currentValue = "Not read";
    private string _currentName = "Not read";
    private EquipmentOptionDefinition _selectedOption;
    private bool _canEdit;

    public EquipmentSlotViewModel(EquipmentSlotDefinition definition)
    {
        Definition = definition;
        _selectedOption = definition.Options[0];
    }

    public EquipmentSlotDefinition Definition { get; }

    public string Name => Definition.Name;

    public uint OffsetValue => Definition.Offset;

    public string Offset => Definition.OffsetText;

    public string Source => Definition.Source;

    public IReadOnlyList<EquipmentOptionDefinition> Options => Definition.Options;

    public string CurrentValue
    {
        get => _currentValue;
        private set => SetField(ref _currentValue, value);
    }

    public string CurrentName
    {
        get => _currentName;
        private set => SetField(ref _currentName, value);
    }

    public EquipmentOptionDefinition SelectedOption
    {
        get => _selectedOption;
        set
        {
            if (value is not null)
            {
                SetField(ref _selectedOption, value);
            }
        }
    }

    public bool CanEdit
    {
        get => _canEdit;
        set => SetField(ref _canEdit, value);
    }

    public void SetCurrentValue(byte value)
    {
        CurrentValue = value.ToString();
        CurrentName = EquipmentDefinitions.GetSlotValueName(Definition, value);
        SelectedOption = EquipmentDefinitions.GetDefaultSelection(Definition, value);
    }

    public void MarkNotRead()
    {
        CurrentValue = "Not read";
        CurrentName = "Not read";
    }
}
