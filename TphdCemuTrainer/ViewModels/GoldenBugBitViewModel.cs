using TphdCemuTrainer.Cheats;

namespace TphdCemuTrainer.ViewModels;

public sealed class GoldenBugBitViewModel : ObservableObject
{
    private bool? _isSetDetected;
    private bool _isSetDesired;
    private bool _canEdit;
    private string? _assignedBugName;
    private string _notes;
    private string _lastWriteStatus = "Not written";

    public GoldenBugBitViewModel(GoldenBugBitDefinition definition)
    {
        Definition = definition;
        _assignedBugName = definition.ConfirmedBugName;
        _notes = definition.Notes;
    }

    public GoldenBugBitDefinition Definition { get; }

    public uint OffsetValue => Definition.Offset;

    public string Offset => Definition.OffsetText;

    public int Bit => Definition.Bit;

    public string ConfirmedBugName => Definition.ConfirmedBugName ?? "Unconfirmed Golden Bug bit";

    public string MappingStatus => Definition.MappingStatus;

    public bool IsConfirmed => Definition.IsConfirmed;

    public IReadOnlyList<string> AvailableBugNames => GoldenBugsDefinitions.BugNames;

    public bool? IsSetDetected
    {
        get => _isSetDetected;
        private set
        {
            if (SetField(ref _isSetDetected, value))
            {
                OnPropertyChanged(nameof(CurrentState));
                OnPropertyChanged(nameof(IsDirty));
            }
        }
    }

    public bool IsSetDesired
    {
        get => _isSetDesired;
        set
        {
            if (SetField(ref _isSetDesired, value))
            {
                OnPropertyChanged(nameof(IsDirty));
            }
        }
    }

    public string CurrentState => IsSetDetected.HasValue
        ? IsSetDetected.Value ? "Set" : "Clear"
        : "Not read";

    public bool IsDirty => IsSetDetected.HasValue && IsSetDesired != IsSetDetected.Value;

    public bool CanEdit
    {
        get => _canEdit;
        set => SetField(ref _canEdit, value);
    }

    public string? AssignedBugName
    {
        get => _assignedBugName;
        set => SetField(ref _assignedBugName, value);
    }

    public string Notes
    {
        get => _notes;
        set => SetField(ref _notes, value);
    }

    public string LastWriteStatus
    {
        get => _lastWriteStatus;
        set => SetField(ref _lastWriteStatus, value);
    }

    public void SetDetected(bool isSet, bool preserveDirty)
    {
        var wasDirty = IsDirty;
        IsSetDetected = isSet;
        if (!preserveDirty || !wasDirty)
        {
            IsSetDesired = isSet;
        }

        OnPropertyChanged(nameof(IsDirty));
    }

    public void MarkNotRead()
    {
        IsSetDetected = null;
        IsSetDesired = false;
        CanEdit = false;
        LastWriteStatus = "Not read";
    }
}
