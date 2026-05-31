using TphdCemuTrainer.Cheats;

namespace TphdCemuTrainer.ViewModels;

public sealed class CapacitySelectorViewModel : ObservableObject
{
    private CapacityOption _selectedOption;
    private string _currentStoredValue = "Not read";

    public CapacitySelectorViewModel(CapacityDefinition definition)
    {
        Definition = definition;
        _selectedOption = definition.Options.FirstOrDefault(option => option.Capacity == definition.DefaultCapacity)
            ?? definition.Options[0];
    }

    public CapacityDefinition Definition { get; }

    public IReadOnlyList<CapacityOption> Options => Definition.Options;

    public string Name => Definition.Name;

    public string Source => Definition.Source;

    public bool IsMemoryBacked => Definition.IsMemoryBacked;

    public string Offset => Definition.Offset.HasValue ? $"0x{Definition.Offset.Value:X}" : "Fixed";

    public int CurrentCapacity => SelectedOption.Capacity;

    public int StoredValue => SelectedOption.StoredValue;

    public string CurrentStoredValue
    {
        get => _currentStoredValue;
        private set => SetField(ref _currentStoredValue, value);
    }

    public CapacityOption SelectedOption
    {
        get => _selectedOption;
        set
        {
            if (value is null)
            {
                return;
            }

            if (SetField(ref _selectedOption, value))
            {
                OnPropertyChanged(nameof(CurrentCapacity));
                OnPropertyChanged(nameof(StoredValue));
            }
        }
    }

    public void SetFromStoredValue(int storedValue)
    {
        CurrentStoredValue = storedValue.ToString();

        var matchingOption = Options.FirstOrDefault(option => option.StoredValue == storedValue)
            ?? Options.FirstOrDefault(option => option.Capacity == storedValue);

        if (matchingOption is not null)
        {
            SelectedOption = matchingOption;
        }
    }

    public void MarkNotRead()
    {
        CurrentStoredValue = IsMemoryBacked ? "Not read" : "Fixed";
    }
}
