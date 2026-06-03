using TphdCemuTrainer.Cheats;

namespace TphdCemuTrainer.ViewModels;

public sealed class HiddenSkillViewModel : ObservableObject
{
    private bool? _isOwnedDetected;
    private bool _isOwnedDesired;
    private bool _canEdit;
    private string _lastWriteStatus = "Not written";
    private string _lastVerificationStatus = "Not verified";

    public HiddenSkillViewModel(HiddenSkillDefinition definition)
    {
        Definition = definition;
    }

    public HiddenSkillDefinition Definition { get; }

    public string Name => Definition.Name;

    public uint OffsetValue => Definition.Offset;

    public string Offset => Definition.OffsetText;

    public int Bit => Definition.Bit;

    public string Evidence => Definition.Evidence;

    public bool? IsOwnedDetected
    {
        get => _isOwnedDetected;
        private set
        {
            if (SetField(ref _isOwnedDetected, value))
            {
                OnPropertyChanged(nameof(CurrentState));
                OnPropertyChanged(nameof(IsDirty));
            }
        }
    }

    public bool IsOwnedDesired
    {
        get => _isOwnedDesired;
        set
        {
            if (SetField(ref _isOwnedDesired, value))
            {
                OnPropertyChanged(nameof(IsDirty));
            }
        }
    }

    public string CurrentState => IsOwnedDetected.HasValue
        ? IsOwnedDetected.Value ? "Owned" : "Not owned"
        : "Not read";

    public bool IsDirty => IsOwnedDetected.HasValue && IsOwnedDesired != IsOwnedDetected.Value;

    public bool CanEdit
    {
        get => _canEdit;
        set => SetField(ref _canEdit, value);
    }

    public string LastWriteStatus
    {
        get => _lastWriteStatus;
        set => SetField(ref _lastWriteStatus, value);
    }

    public string LastVerificationStatus
    {
        get => _lastVerificationStatus;
        set => SetField(ref _lastVerificationStatus, value);
    }

    public void SetDetected(bool isOwned, bool preserveDirty)
    {
        var wasDirty = IsDirty;
        IsOwnedDetected = isOwned;
        if (!preserveDirty || !wasDirty)
        {
            IsOwnedDesired = isOwned;
        }

        OnPropertyChanged(nameof(IsDirty));
    }

    public void MarkNotRead()
    {
        IsOwnedDetected = null;
        IsOwnedDesired = false;
        CanEdit = false;
        LastWriteStatus = "Not read";
        LastVerificationStatus = "Not verified";
    }
}
