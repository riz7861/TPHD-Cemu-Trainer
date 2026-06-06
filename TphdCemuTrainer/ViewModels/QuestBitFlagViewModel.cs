using TphdCemuTrainer.Cheats;

namespace TphdCemuTrainer.ViewModels;

public sealed class QuestBitFlagViewModel : ObservableObject
{
    private bool? _isSetDetected;
    private bool _isSetDesired;
    private bool _canEdit;
    private string _lastWriteResult = "No writes yet";
    private string _lastVerificationResult = "No verification yet";

    public QuestBitFlagViewModel(QuestBitFlagDefinition definition)
    {
        Definition = definition;
    }

    public QuestBitFlagDefinition Definition { get; }

    public string Name => Definition.Name;

    public string Notes => Definition.Notes;

    public string ToolTip => $"{Definition.Notes} Offset: {Definition.OffsetText}, {Definition.BitText}.";

    public uint OffsetValue => Definition.Offset;

    public int Bit => Definition.Bit;

    public bool? IsSetDetected
    {
        get => _isSetDetected;
        private set
        {
            if (SetField(ref _isSetDetected, value))
            {
                OnPropertyChanged(nameof(CurrentText));
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

    public bool IsDirty => IsSetDetected.HasValue && IsSetDetected.Value != IsSetDesired;

    public string CurrentText => IsSetDetected.HasValue
        ? IsSetDetected.Value ? "Set" : "Clear"
        : "Not read";

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

    public void SetDetected(bool isSet, bool preserveDirty)
    {
        var wasDirty = IsDirty;
        IsSetDetected = isSet;
        if (!preserveDirty || !wasDirty)
        {
            IsSetDesired = isSet;
        }
    }

    public void MarkNotRead()
    {
        IsSetDetected = null;
        CanEdit = false;
        LastVerificationResult = "No verification yet";
    }
}
