using System.Globalization;
using TphdCemuTrainer.Cheats;

namespace TphdCemuTrainer.ViewModels;

public sealed class TrainerValueViewModel : ObservableObject
{
    private string _currentValue = "Not read";
    private string _targetValue = string.Empty;
    private bool _isLocked;
    private bool _targetInitialized;
    private bool _isSettingTargetInternally;

    public TrainerValueViewModel(CheatDefinition definition)
    {
        Definition = definition;
    }

    public CheatDefinition Definition { get; }

    public CapacitySelectorViewModel? Capacity { get; set; }

    public Func<int>? EffectiveMaximumProvider { get; set; }

    public int? CurrentNumericValue { get; private set; }

    public string Name => Definition.Name;

    public string Offset => $"0x{Definition.Offset:X}";

    public string Source => Definition.Source;

    public bool CanLock => Definition.CanLock;

    public string CurrentValue
    {
        get => _currentValue;
        private set => SetField(ref _currentValue, value);
    }

    public string TargetValue
    {
        get => _targetValue;
        set
        {
            if (SetField(ref _targetValue, value) && !_isSettingTargetInternally)
            {
                _targetInitialized = true;
            }
        }
    }

    public bool IsLocked
    {
        get => _isLocked;
        set
        {
            if (SetField(ref _isLocked, value))
            {
                OnPropertyChanged(nameof(LockText));
            }
        }
    }

    public string LockText => IsLocked ? "Lock on" : "Lock off";

    public int EffectiveMaximum => Math.Min(
        Capacity?.CurrentCapacity ?? Definition.HardMaximum,
        EffectiveMaximumProvider?.Invoke() ?? Definition.HardMaximum);

    public void SetCurrentValue(int value, bool initializeTarget)
    {
        CurrentNumericValue = value;
        CurrentValue = value.ToString(CultureInfo.InvariantCulture);

        if (initializeTarget && !_targetInitialized)
        {
            SetTargetValue(Definition.Clamp(value, EffectiveMaximum));
        }
    }

    public void SetCurrentDisplay(string displayValue)
    {
        CurrentNumericValue = null;
        CurrentValue = displayValue;
    }

    public void SetTargetValue(int value)
    {
        _isSettingTargetInternally = true;
        TargetValue = Definition.Clamp(value, EffectiveMaximum).ToString(CultureInfo.InvariantCulture);
        _targetInitialized = true;
        _isSettingTargetInternally = false;
    }

    public bool TryGetClampedTarget(out int value, out bool wasClamped, out string error)
    {
        var text = TargetValue.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            value = 0;
            wasClamped = false;
            error = $"{Name} needs a target value.";
            return false;
        }

        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            if (!int.TryParse(text[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value))
            {
                wasClamped = false;
                error = $"'{TargetValue}' is not a valid number for {Name}.";
                return false;
            }
        }
        else if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
        {
            wasClamped = false;
            error = $"'{TargetValue}' is not a valid number for {Name}.";
            return false;
        }

        var clampedValue = Definition.Clamp(value, EffectiveMaximum);
        wasClamped = clampedValue != value;
        value = clampedValue;
        error = string.Empty;
        return true;
    }

    public void MarkNotRead()
    {
        CurrentNumericValue = null;
        CurrentValue = "Not read";
    }
}
