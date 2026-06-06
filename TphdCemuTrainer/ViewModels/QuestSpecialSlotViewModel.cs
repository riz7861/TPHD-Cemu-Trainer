using TphdCemuTrainer.Cheats;

namespace TphdCemuTrainer.ViewModels;

public sealed class QuestSpecialSlotViewModel : ObservableObject
{
    private byte? _currentValue;
    private QuestSpecialOptionDefinition _selectedOption;
    private bool _canEdit;
    private string _lastWriteResult = "No writes yet";
    private string _lastVerificationResult = "No verification yet";

    public QuestSpecialSlotViewModel(QuestSpecialSlotDefinition definition)
    {
        Definition = definition;
        _selectedOption = definition.Options[0];
    }

    public QuestSpecialSlotDefinition Definition { get; }

    public string Name => Definition.Name;

    public string Notes => Definition.Notes;

    public string ToolTip => $"{Definition.Notes} Offset: {Definition.OffsetText}.";

    public uint OffsetValue => Definition.Offset;

    public IReadOnlyList<QuestSpecialOptionDefinition> Options => Definition.Options;

    public QuestSpecialOptionDefinition SelectedOption
    {
        get => _selectedOption;
        set
        {
            if (value is not null && SetField(ref _selectedOption, value))
            {
                OnPropertyChanged(nameof(IsDirty));
            }
        }
    }

    public byte? CurrentValue
    {
        get => _currentValue;
        private set
        {
            if (SetField(ref _currentValue, value))
            {
                OnPropertyChanged(nameof(CurrentText));
                OnPropertyChanged(nameof(CurrentRawText));
                OnPropertyChanged(nameof(IsDirty));
            }
        }
    }

    public string CurrentText => CurrentValue.HasValue
        ? QuestSpecialDefinitions.GetSlotValueName(Definition, CurrentValue.Value)
        : "Not read";

    public string CurrentRawText => CurrentValue.HasValue
        ? $"0x{CurrentValue.Value:X2}"
        : "-";

    public bool IsDirty => CurrentValue.HasValue && CurrentValue.Value != SelectedOption.Value;

    public bool CanEdit
    {
        get => _canEdit;
        set => SetField(ref _canEdit, value);
    }

    public string LastWriteResult
    {
        get => _lastWriteResult;
        set => SetField(ref _lastWriteResult, value);
    }

    public string LastVerificationResult
    {
        get => _lastVerificationResult;
        set => SetField(ref _lastVerificationResult, value);
    }

    public void SetCurrentValue(byte value)
    {
        var shouldInitializeSelection = !CurrentValue.HasValue || !IsDirty;
        CurrentValue = value;
        if (shouldInitializeSelection)
        {
            SelectedOption = QuestSpecialDefinitions.GetDefaultSelection(Definition, value);
        }
    }

    public void MarkNotRead()
    {
        CurrentValue = null;
        CanEdit = false;
        LastVerificationResult = "No verification yet";
    }
}
