using TphdCemuTrainer.Cheats;

namespace TphdCemuTrainer.ViewModels;

public sealed class StampBitViewModel : ObservableObject
{
    private bool? _isCollectedDetected;
    private bool _isCollectedDesired;
    private bool _canEdit;
    private byte? _currentByte;
    private string _lastWriteStatus = "Not written";

    public StampBitViewModel(StampBitDefinition definition)
    {
        Definition = definition;
    }

    public StampBitDefinition Definition { get; }

    public string Name => Definition.Name;

    public uint OffsetValue => Definition.Offset;

    public string Offset => Definition.OffsetText;

    public int Bit => Definition.Bit;

    public string Notes => Definition.Notes;

    public bool? IsCollectedDetected
    {
        get => _isCollectedDetected;
        private set
        {
            if (SetField(ref _isCollectedDetected, value))
            {
                OnPropertyChanged(nameof(CurrentStatus));
                OnPropertyChanged(nameof(IsDirty));
            }
        }
    }

    public bool IsCollectedDesired
    {
        get => _isCollectedDesired;
        set
        {
            if (SetField(ref _isCollectedDesired, value))
            {
                OnPropertyChanged(nameof(IsDirty));
            }
        }
    }

    public string CurrentStatus => IsCollectedDetected.HasValue
        ? IsCollectedDetected.Value ? "Collected" : "Not collected"
        : "Not read";

    public bool IsDirty =>
        IsCollectedDetected.HasValue &&
        IsCollectedDesired != IsCollectedDetected.Value;

    public bool CanEdit
    {
        get => _canEdit;
        set => SetField(ref _canEdit, value);
    }

    public string CurrentRawByte => _currentByte.HasValue
        ? $"{_currentByte.Value} / 0x{_currentByte.Value:X2}"
        : "Not read";

    public string LastWriteStatus
    {
        get => _lastWriteStatus;
        set => SetField(ref _lastWriteStatus, value);
    }

    public void SetDetected(bool isCollected, byte currentByte, bool preserveDirty)
    {
        var wasDirty = IsDirty;
        _currentByte = currentByte;
        OnPropertyChanged(nameof(CurrentRawByte));
        IsCollectedDetected = isCollected;
        if (!preserveDirty || !wasDirty)
        {
            IsCollectedDesired = isCollected;
        }

        OnPropertyChanged(nameof(IsDirty));
    }

    public void MarkNotRead()
    {
        _currentByte = null;
        OnPropertyChanged(nameof(CurrentRawByte));
        IsCollectedDetected = null;
        IsCollectedDesired = false;
        CanEdit = false;
        LastWriteStatus = "Not read";
    }
}
