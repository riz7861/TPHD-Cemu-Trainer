using TphdCemuTrainer.Cheats;

namespace TphdCemuTrainer.ViewModels;

public sealed class BottleSlotViewModel : ObservableObject
{
    private byte? _currentValue;
    private byte? _previousValue;
    private BottleContentDefinition _selectedContent;
    private bool _canEdit;
    private string _status = "Not read";
    private string _lastWriteResult = "No writes yet";
    private string _lastVerificationResult = "No verification yet";

    public BottleSlotViewModel(BottleSlotDefinition definition)
    {
        Definition = definition;
        _selectedContent = BottleDefinitions.Nothing;
    }

    public BottleSlotDefinition Definition { get; }

    public int BottleSlotNumber => Definition.BottleSlotNumber;

    public int InventorySlotIndex => Definition.InventorySlotIndex;

    public uint OffsetValue => Definition.Offset;

    public string Offset => Definition.OffsetText;

    public IReadOnlyList<BottleContentDefinition> AvailableContents => BottleDefinitions.ConfirmedContents;

    public BottleContentDefinition SelectedContent
    {
        get => _selectedContent;
        set
        {
            if (value is not null)
            {
                SetField(ref _selectedContent, value);
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
        ? BottleDefinitions.GetBottleContentName(CurrentValue.Value)
        : "Not read";

    public string PreviousRawValue => FormatValue(PreviousValue);

    public string PreviousDecodedContent => PreviousValue.HasValue
        ? BottleDefinitions.GetBottleContentName(PreviousValue.Value)
        : "-";

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

    public bool CanRestore => PreviousValue.HasValue && CanEdit;

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
        var shouldInitializeSelection = !CurrentValue.HasValue;
        CurrentValue = value;
        if (shouldInitializeSelection)
        {
            SelectedContent = BottleDefinitions.GetDefaultSelection(value);
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
