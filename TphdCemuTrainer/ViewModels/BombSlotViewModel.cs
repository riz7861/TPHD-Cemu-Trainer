using TphdCemuTrainer.Cheats;

namespace TphdCemuTrainer.ViewModels;

public sealed class BombSlotViewModel : ObservableObject
{
    private byte? _currentValue;
    private byte? _previousValue;
    private BombContentDefinition _selectedContent;
    private bool _canEdit;
    private string _status = "Not read";
    private string _lastWriteResult = "No writes yet";
    private string _lastVerificationResult = "No verification yet";

    public BombSlotViewModel(BombSlotDefinition definition)
    {
        Definition = definition;
        _selectedContent = BombSlotDefinitions.DefaultContent;
    }

    public BombSlotDefinition Definition { get; }

    public int SlotNumber => Definition.SlotNumber;

    public uint OffsetValue => Definition.Offset;

    public string Offset => Definition.OffsetText;

    public IReadOnlyList<BombContentDefinition> AvailableContents => BombSlotDefinitions.ConfirmedContents;

    public BombContentDefinition SelectedContent
    {
        get => _selectedContent;
        set
        {
            if (value is not null && SetField(ref _selectedContent, value))
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
                OnPropertyChanged(nameof(CurrentRawValue));
                OnPropertyChanged(nameof(CurrentDecodedContent));
                OnPropertyChanged(nameof(CurrentDecimal));
                OnPropertyChanged(nameof(CurrentHex));
                OnPropertyChanged(nameof(CurrentBinary));
                OnPropertyChanged(nameof(IsDirty));
            }
        }
    }

    public byte? PreviousValue
    {
        get => _previousValue;
        private set
        {
            if (SetField(ref _previousValue, value))
            {
                OnPropertyChanged(nameof(PreviousRawValue));
                OnPropertyChanged(nameof(PreviousDecodedContent));
                OnPropertyChanged(nameof(CanRestore));
            }
        }
    }

    public string CurrentRawValue => FormatValue(CurrentValue);

    public string CurrentDecodedContent => CurrentValue.HasValue
        ? BombSlotDefinitions.GetContentName(CurrentValue.Value)
        : "Not read";

    public string CurrentDecimal => CurrentValue.HasValue
        ? CurrentValue.Value.ToString()
        : "-";

    public string CurrentHex => CurrentValue.HasValue
        ? $"0x{CurrentValue.Value:X2}"
        : "-";

    public string CurrentBinary => CurrentValue.HasValue
        ? Convert.ToString(CurrentValue.Value, 2).PadLeft(8, '0')
        : "-";

    public string PreviousRawValue => FormatValue(PreviousValue);

    public string PreviousDecodedContent => PreviousValue.HasValue
        ? BombSlotDefinitions.GetContentName(PreviousValue.Value)
        : "-";

    public bool IsDirty => CurrentValue.HasValue && CurrentValue.Value != SelectedContent.Value;

    public bool CanEdit
    {
        get => _canEdit;
        set
        {
            if (SetField(ref _canEdit, value))
            {
                OnPropertyChanged(nameof(CanRestore));
            }
        }
    }

    public bool CanRestore =>
        PreviousValue.HasValue &&
        BombSlotDefinitions.IsConfirmedContent(PreviousValue.Value) &&
        CanEdit;

    public string Status
    {
        get => _status;
        set => SetField(ref _status, value);
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
            SelectedContent = BombSlotDefinitions.GetDefaultSelection(value);
        }

        if (Status is "Not read")
        {
            Status = "Detected";
        }
    }

    public void CapturePrevious(byte value)
    {
        PreviousValue = value;
    }

    public void ClearPrevious()
    {
        PreviousValue = null;
    }

    public void MarkNotRead()
    {
        CurrentValue = null;
        CanEdit = false;
        Status = "Not read";
    }

    private static string FormatValue(byte? value)
    {
        return value.HasValue
            ? $"{value.Value} / 0x{value.Value:X2}"
            : "-";
    }
}
