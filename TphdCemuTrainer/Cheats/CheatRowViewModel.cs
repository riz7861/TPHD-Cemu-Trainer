using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace TphdCemuTrainer.Cheats;

public sealed class CheatRowViewModel : INotifyPropertyChanged
{
    private string _currentValue = "Not read";
    private string _desiredValue;
    private bool _isLocked;

    public CheatRowViewModel(CheatDefinition definition)
    {
        Definition = definition;
        _desiredValue = definition.DefaultValue.ToString(CultureInfo.InvariantCulture);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public CheatDefinition Definition { get; }

    public string Name => Definition.Name;

    public string Offset => $"0x{Definition.Offset:X}";

    public string Source => Definition.Source;

    public string CurrentValue
    {
        get => _currentValue;
        private set => SetField(ref _currentValue, value);
    }

    public string DesiredValue
    {
        get => _desiredValue;
        set => SetField(ref _desiredValue, value);
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

    public bool CanLock => Definition.CanLock;

    public bool HasOptions => Definition.Options is { Count: > 0 };

    public string OptionHint =>
        Definition.Options is { Count: > 0 } options
            ? string.Join(", ", options.Select(option => $"{option.Value} {option.Label}"))
            : string.Empty;

    public string LockText => IsLocked ? "On" : "Off";

    public void SetCurrentValue(int value)
    {
        CurrentValue = value.ToString(CultureInfo.InvariantCulture);
    }

    public void MarkNotRead()
    {
        CurrentValue = "Not read";
    }

    public bool TryGetDesiredValue(out int value, out string error)
    {
        var text = DesiredValue.Trim();
        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            if (int.TryParse(text[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value))
            {
                value = Definition.Clamp(value);
                error = string.Empty;
                return true;
            }
        }
        else if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
        {
            value = Definition.Clamp(value);
            error = string.Empty;
            return true;
        }

        value = 0;
        error = $"'{DesiredValue}' is not a valid number for {Name}.";
        return false;
    }

    public void UseDefaultValue()
    {
        DesiredValue = Definition.DefaultValue.ToString(CultureInfo.InvariantCulture);
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
