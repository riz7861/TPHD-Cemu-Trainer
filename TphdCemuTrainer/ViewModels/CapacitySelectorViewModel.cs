using TphdCemuTrainer.Cheats;

namespace TphdCemuTrainer.ViewModels;

public sealed class CapacitySelectorViewModel : ObservableObject
{
    private CapacityOption _selectedOption;
    private string _currentStoredValue = "Not read";
    private int? _currentStoredNumericValue;

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

    public string FriendlyCurrentStatus
    {
        get
        {
            if (!IsMemoryBacked)
            {
                return "Fixed capacity";
            }

            if (!_currentStoredNumericValue.HasValue)
            {
                return "Not read";
            }

            if (_currentStoredNumericValue.Value == 0)
            {
                return Definition.Id switch
                {
                    CheatCatalog.QuiverCapacityId => "Bow not obtained",
                    CheatCatalog.BombBagCapacityId => "Bomb Bag not acquired",
                    _ => GetKnownOrUnknownStatus(_currentStoredNumericValue.Value)
                };
            }

            return GetKnownOrUnknownStatus(_currentStoredNumericValue.Value);
        }
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
        _currentStoredNumericValue = storedValue;
        CurrentStoredValue = storedValue.ToString();
        OnPropertyChanged(nameof(FriendlyCurrentStatus));

        var matchingOption = Options.FirstOrDefault(option => option.StoredValue == storedValue)
            ?? Options.FirstOrDefault(option => option.Capacity == storedValue);

        if (matchingOption is not null)
        {
            SelectedOption = matchingOption;
        }
    }

    public void MarkNotRead()
    {
        _currentStoredNumericValue = null;
        CurrentStoredValue = IsMemoryBacked ? "Not read" : "Fixed";
        OnPropertyChanged(nameof(FriendlyCurrentStatus));
    }

    private string GetKnownOrUnknownStatus(int storedValue)
    {
        var matchingOption = Options.FirstOrDefault(option => option.StoredValue == storedValue)
            ?? Options.FirstOrDefault(option => option.Capacity == storedValue);

        return matchingOption is not null
            ? $"Current: {matchingOption.Label}"
            : $"Unknown value (0x{storedValue:X2})";
    }
}
