using TphdCemuTrainer.Cheats;

namespace TphdCemuTrainer.ViewModels;

public sealed class QuestSpecialItemViewModel : ObservableObject
{
    private bool? _isOwnedDetected;
    private bool _isOwnedDesired;
    private bool _canEdit;

    public QuestSpecialItemViewModel(QuestSpecialItemDefinition definition)
    {
        Definition = definition;
    }

    public QuestSpecialItemDefinition Definition { get; }

    public string Name => Definition.Name;

    public bool? IsOwnedDetected
    {
        get => _isOwnedDetected;
        private set
        {
            if (SetField(ref _isOwnedDetected, value))
            {
                OnPropertyChanged(nameof(CurrentText));
                OnPropertyChanged(nameof(IsDirty));
                OnPropertyChanged(nameof(StatusText));
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

    public bool CanEdit
    {
        get => _canEdit;
        set
        {
            if (SetField(ref _canEdit, value))
            {
                OnPropertyChanged(nameof(StatusText));
            }
        }
    }

    public bool IsDirty => IsOwnedDetected.HasValue && IsOwnedDesired != IsOwnedDetected.Value;

    public string CurrentText => IsOwnedDetected.HasValue
        ? IsOwnedDetected.Value ? "Owned" : "Not owned"
        : "Not read";

    public string StatusText => !IsOwnedDetected.HasValue
        ? "Not read"
        : CanEdit ? "Ready" : "Unavailable";

    public void SetDetected(bool isOwned, bool preserveDirty)
    {
        var wasDirty = IsDirty;
        IsOwnedDetected = isOwned;
        if (!preserveDirty || !wasDirty)
        {
            IsOwnedDesired = isOwned;
        }
    }

    public void MarkNotRead()
    {
        IsOwnedDetected = null;
        CanEdit = false;
    }
}
