using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using TphdCemuTrainer.Cheats;
using TphdCemuTrainer.Memory;
using TphdCemuTrainer.ViewModels;

namespace TphdCemuTrainer;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly DispatcherTimer _refreshTimer;
    private readonly Dictionary<CheatId, TrainerValueViewModel> _values;
    private readonly Dictionary<string, CapacitySelectorViewModel> _capacities;
    private ProcessMemory? _memory;
    private ulong? _playerBaseAddress;
    private bool _isAttaching;
    private bool _isRefreshing;

    public MainWindow()
    {
        _capacities = CheatCatalog.Capacities
            .Select(definition => new CapacitySelectorViewModel(definition))
            .ToDictionary(capacity => capacity.Definition.Id);

        _values = CheatCatalog.Values
            .Select(CreateTrainerValue)
            .ToDictionary(value => value.Definition.Id);

        CurrentHealth.EffectiveMaximumProvider = GetMaximumHealthLimit;

        RawMemoryValues = new ObservableCollection<TrainerValueViewModel>(
            CheatCatalog.Values.Select(value => _values[value.Id]));

        InventoryItems = CreateFutureFeatures(FutureFeatureCatalog.InventoryItems);
        Weapons = CreateFutureFeatures(FutureFeatureCatalog.Weapons);
        Shields = CreateFutureFeatures(FutureFeatureCatalog.Shields);
        Armor = CreateFutureFeatures(FutureFeatureCatalog.Armor);
        Equipment = CreateFutureFeatures(FutureFeatureCatalog.Equipment);
        StoryFlags = CreateFutureFeatures(FutureFeatureCatalog.StoryFlags);
        HiddenSkills = CreateFutureFeatures(FutureFeatureCatalog.HiddenSkills);
        QuestItems = CreateFutureFeatures(FutureFeatureCatalog.QuestItems);
        DebugTools = CreateFutureFeatures(FutureFeatureCatalog.DebugTools);

        InitializeComponent();
        DataContext = this;

        AobPatternText.Text = CheatCatalog.PlayerBaseAob;

        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _refreshTimer.Tick += RefreshTimer_Tick;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public TrainerValueViewModel CurrentHealth => _values[CheatId.CurrentHealth];

    public TrainerValueViewModel MaximumHealth => _values[CheatId.MaximumHealth];

    public TrainerValueViewModel LanternOil => _values[CheatId.LanternOil];

    public TrainerValueViewModel Rupees => _values[CheatId.Rupees];

    public TrainerValueViewModel Arrows => _values[CheatId.Arrows];

    public TrainerValueViewModel BombSlot1 => _values[CheatId.BombSlot1];

    public TrainerValueViewModel BombSlot2 => _values[CheatId.BombSlot2];

    public TrainerValueViewModel BombSlot3 => _values[CheatId.BombSlot3];

    public TrainerValueViewModel Seeds => _values[CheatId.Seeds];

    public TrainerValueViewModel PoeSouls => _values[CheatId.PoeSouls];

    public TrainerValueViewModel GoldenBugsFlags => _values[CheatId.GoldenBugsFlags];

    public CapacitySelectorViewModel WalletCapacity => _capacities[CheatCatalog.WalletCapacityId];

    public CapacitySelectorViewModel QuiverCapacity => _capacities[CheatCatalog.QuiverCapacityId];

    public CapacitySelectorViewModel BombBagCapacity => _capacities[CheatCatalog.BombBagCapacityId];

    public CapacitySelectorViewModel SeedBagCapacity => _capacities[CheatCatalog.SeedBagCapacityId];

    public ObservableCollection<TrainerValueViewModel> RawMemoryValues { get; }

    public ObservableCollection<FutureFeatureViewModel> InventoryItems { get; }

    public ObservableCollection<FutureFeatureViewModel> Weapons { get; }

    public ObservableCollection<FutureFeatureViewModel> Shields { get; }

    public ObservableCollection<FutureFeatureViewModel> Armor { get; }

    public ObservableCollection<FutureFeatureViewModel> Equipment { get; }

    public ObservableCollection<FutureFeatureViewModel> StoryFlags { get; }

    public ObservableCollection<FutureFeatureViewModel> HiddenSkills { get; }

    public ObservableCollection<FutureFeatureViewModel> QuestItems { get; }

    public ObservableCollection<FutureFeatureViewModel> DebugTools { get; }

    private TrainerValueViewModel CreateTrainerValue(CheatDefinition definition)
    {
        var value = new TrainerValueViewModel(definition);

        if (definition.CapacityId is not null &&
            _capacities.TryGetValue(definition.CapacityId, out var capacity))
        {
            value.Capacity = capacity;
        }

        return value;
    }

    private static ObservableCollection<FutureFeatureViewModel> CreateFutureFeatures(
        IEnumerable<FutureFeatureDefinition> definitions)
    {
        return new ObservableCollection<FutureFeatureViewModel>(
            definitions.Select(definition => new FutureFeatureViewModel(definition)));
    }

    private async void AttachButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isAttaching)
        {
            return;
        }

        _isAttaching = true;
        SetControlsEnabled(false);
        SetStatus("Not attached. Looking for Cemu.exe...", StatusKind.Working);

        try
        {
            Detach(clearStatus: false);

            if (!ProcessMemory.TryAttachToCemu(out var memory, out var attachError) || memory is null)
            {
                SetStatus($"{attachError} Start Cemu, load Twilight Princess HD, then rescan.", StatusKind.Neutral);
                return;
            }

            SetStatus("Cemu found. Scanning for Twilight Princess HD player data...", StatusKind.Working);
            PidText.Text = memory.ProcessId.ToString(CultureInfo.InvariantCulture);

            var pattern = AobPattern.Parse(CheatCatalog.PlayerBaseAob);
            var scanner = new AobScanner();
            var foundAddress = await Task.Run(() => scanner.FindFirst(memory, pattern, CancellationToken.None));

            if (!foundAddress.HasValue)
            {
                memory.Dispose();
                PidText.Text = "-";
                PlayerBaseText.Text = "-";
                DebugPlayerBaseText.Text = "-";
                SetStatus("Cemu found. Load into gameplay and rescan.", StatusKind.Warning);
                return;
            }

            _memory = memory;
            _playerBaseAddress = foundAddress.Value;
            PlayerBaseText.Text = $"0x{foundAddress.Value:X}";
            DebugPlayerBaseText.Text = PlayerBaseText.Text;
            DetachButton.IsEnabled = true;
            _refreshTimer.Start();

            SetStatus("Player data found. Game loaded.", StatusKind.Connected);
            RefreshTrainer(applyLocks: false);
        }
        catch (Exception ex)
        {
            Detach(clearStatus: false);
            SetStatus($"Attach failed: {ex.Message}", StatusKind.Warning);
        }
        finally
        {
            _isAttaching = false;
            SetControlsEnabled(true);
        }
    }

    private void DetachButton_Click(object sender, RoutedEventArgs e)
    {
        Detach(clearStatus: true);
    }

    private void SetValue_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is TrainerValueViewModel value)
        {
            WriteRequestedValue(value);
        }
    }

    private void MaxValue_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not TrainerValueViewModel value)
        {
            return;
        }

        value.SetTargetValue(value.EffectiveMaximum);
        WriteRequestedValue(value);
    }

    private void CapacityCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isRefreshing ||
            e.RemovedItems.Count == 0 ||
            (sender as FrameworkElement)?.Tag is not CapacitySelectorViewModel capacity ||
            capacity.SelectedOption is null)
        {
            return;
        }

        ClampValuesForCapacity(capacity, writeIfAttached: false);

        if (_memory is null || !_playerBaseAddress.HasValue)
        {
            SetStatus($"{capacity.Name} set to {capacity.CurrentCapacity}. Attach before writing it to Cemu.", StatusKind.Neutral);
            return;
        }

        if (!WriteCapacity(capacity))
        {
            return;
        }

        ClampValuesForCapacity(capacity, writeIfAttached: true);
        SetStatus($"{capacity.Name} set to {capacity.CurrentCapacity}.", StatusKind.Connected);
    }

    private void RefreshTimer_Tick(object? sender, EventArgs e)
    {
        RefreshTrainer(applyLocks: true);
    }

    private void RefreshTrainer(bool applyLocks)
    {
        if (_isRefreshing)
        {
            return;
        }

        if (_memory is null || !_playerBaseAddress.HasValue)
        {
            return;
        }

        if (_memory.HasExited)
        {
            Detach(clearStatus: false);
            SetStatus("Cemu.exe exited. Start Cemu and rescan.", StatusKind.Warning);
            return;
        }

        _isRefreshing = true;
        try
        {
            if (!RefreshCapacities())
            {
                return;
            }

            foreach (var value in CheatCatalog.Values.Select(definition => _values[definition.Id]))
            {
                if (!RefreshValue(value))
                {
                    return;
                }
            }

            UpdateDerivedDisplays();

            if (applyLocks)
            {
                ApplyLocks();
            }
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    private bool RefreshCapacities()
    {
        foreach (var capacity in _capacities.Values)
        {
            if (!capacity.IsMemoryBacked)
            {
                capacity.MarkNotRead();
                continue;
            }

            if (!TryReadCapacity(capacity, out var storedValue, out _))
            {
                MarkMemoryUnavailable();
                return false;
            }

            capacity.SetFromStoredValue(storedValue);
        }

        return true;
    }

    private bool RefreshValue(TrainerValueViewModel value)
    {
        if (value.Definition.ValueKind == CheatValueKind.UInt32BigEndian)
        {
            if (!TryReadUInt32Value(value.Definition, out var rawValue, out _))
            {
                MarkMemoryUnavailable();
                return false;
            }

            value.SetCurrentDisplay($"0x{rawValue:X8}");
            if (value.Definition.Id == CheatId.GoldenBugsFlags)
            {
                GoldenBugsCountText.Text = $"{CountGoldenBugs(rawValue)} / 24";
            }

            return true;
        }

        if (!TryReadIntValue(value.Definition, out var currentValue, out _))
        {
            MarkMemoryUnavailable();
            return false;
        }

        value.SetCurrentValue(currentValue, initializeTarget: true);
        return true;
    }

    private void ApplyLocks()
    {
        foreach (var value in _values.Values.Where(value => value.CanLock && value.IsLocked))
        {
            if (!value.CurrentNumericValue.HasValue ||
                !value.TryGetClampedTarget(out var target, out var wasClamped, out _))
            {
                continue;
            }

            if (wasClamped)
            {
                value.SetTargetValue(target);
            }

            var current = value.CurrentNumericValue.Value;
            var effectiveMaximum = value.EffectiveMaximum;

            if (current > effectiveMaximum)
            {
                value.SetTargetValue(effectiveMaximum);
                WriteValue(value, effectiveMaximum, quiet: true);
            }
            else if (current > target)
            {
                value.SetTargetValue(current);
            }
            else if (current < target)
            {
                WriteValue(value, target, quiet: true);
            }
        }
    }

    private void WriteRequestedValue(TrainerValueViewModel value)
    {
        if (!value.TryGetClampedTarget(out var target, out var wasClamped, out var validationError))
        {
            SetStatus(validationError, StatusKind.Warning);
            return;
        }

        if (wasClamped)
        {
            value.SetTargetValue(target);
        }

        WriteValue(value, target, quiet: false);
    }

    private bool WriteValue(TrainerValueViewModel value, int requestedValue, bool quiet)
    {
        if (_memory is null || !_playerBaseAddress.HasValue)
        {
            SetStatus("Not attached. Attach to Cemu and rescan first.", StatusKind.Neutral);
            return false;
        }

        var clampedValue = value.Definition.Clamp(requestedValue, value.EffectiveMaximum);
        var address = _playerBaseAddress.Value + value.Definition.Offset;
        string writeError;
        var writeSucceeded = value.Definition.ValueKind switch
        {
            CheatValueKind.Byte => _memory.TryWriteBytes(address, [(byte)clampedValue], out writeError),
            CheatValueKind.UInt16BigEndian => BigEndianMemory.TryWriteUInt16(_memory, address, (ushort)clampedValue, out writeError),
            CheatValueKind.UInt32BigEndian => BigEndianMemory.TryWriteUInt32(_memory, address, (uint)clampedValue, out writeError),
            _ => UnsupportedWrite(value, out writeError)
        };

        if (!writeSucceeded)
        {
            SetStatus(writeError, StatusKind.Warning);
            return false;
        }

        value.SetCurrentValue(clampedValue, initializeTarget: false);
        value.SetTargetValue(clampedValue);

        if (value.Definition.Id == CheatId.MaximumHealth)
        {
            ClampCurrentHealthAfterMaximumChange(clampedValue);
        }

        if (!quiet)
        {
            SetStatus($"{value.Name} set to {clampedValue}.", StatusKind.Connected);
        }

        UpdateDerivedDisplays();
        return true;
    }

    private bool WriteCapacity(CapacitySelectorViewModel capacity)
    {
        if (_memory is null || !_playerBaseAddress.HasValue)
        {
            SetStatus("Not attached. Attach to Cemu and rescan first.", StatusKind.Neutral);
            return false;
        }

        if (!capacity.Definition.IsMemoryBacked ||
            !capacity.Definition.Offset.HasValue ||
            !capacity.Definition.ValueKind.HasValue)
        {
            return true;
        }

        var address = _playerBaseAddress.Value + capacity.Definition.Offset.Value;
        string writeError;
        var writeSucceeded = capacity.Definition.ValueKind.Value switch
        {
            CheatValueKind.Byte => _memory.TryWriteBytes(address, [(byte)capacity.StoredValue], out writeError),
            CheatValueKind.UInt16BigEndian => BigEndianMemory.TryWriteUInt16(_memory, address, (ushort)capacity.StoredValue, out writeError),
            CheatValueKind.UInt32BigEndian => BigEndianMemory.TryWriteUInt32(_memory, address, (uint)capacity.StoredValue, out writeError),
            _ => UnsupportedCapacityWrite(capacity, out writeError)
        };

        if (!writeSucceeded)
        {
            SetStatus(writeError, StatusKind.Warning);
            return false;
        }

        capacity.SetFromStoredValue(capacity.StoredValue);
        return true;
    }

    private void ClampValuesForCapacity(CapacitySelectorViewModel capacity, bool writeIfAttached)
    {
        foreach (var value in _values.Values.Where(value => ReferenceEquals(value.Capacity, capacity)))
        {
            if (value.TryGetClampedTarget(out var target, out var wasClamped, out _) && wasClamped)
            {
                value.SetTargetValue(target);
            }

            if (writeIfAttached &&
                value.CurrentNumericValue.HasValue &&
                value.CurrentNumericValue.Value > value.EffectiveMaximum)
            {
                WriteValue(value, value.EffectiveMaximum, quiet: true);
            }
        }
    }

    private void ClampCurrentHealthAfterMaximumChange(int maximumHealth)
    {
        if (CurrentHealth.CurrentNumericValue.HasValue &&
            CurrentHealth.CurrentNumericValue.Value > maximumHealth)
        {
            CurrentHealth.SetTargetValue(maximumHealth);
            WriteValue(CurrentHealth, maximumHealth, quiet: true);
        }
        else if (CurrentHealth.TryGetClampedTarget(out var target, out var wasClamped, out _) && wasClamped)
        {
            CurrentHealth.SetTargetValue(target);
        }
    }

    private bool TryReadCapacity(CapacitySelectorViewModel capacity, out int value, out string error)
    {
        value = 0;
        error = string.Empty;

        if (!capacity.Definition.Offset.HasValue || !capacity.Definition.ValueKind.HasValue)
        {
            value = capacity.CurrentCapacity;
            return true;
        }

        var definition = new CheatDefinition(
            CheatId.Rupees,
            capacity.Name,
            capacity.Definition.Offset.Value,
            capacity.Definition.ValueKind.Value,
            int.MaxValue,
            false,
            capacity.Source);

        return TryReadIntValue(definition, out value, out error);
    }

    private bool TryReadIntValue(CheatDefinition definition, out int value, out string error)
    {
        value = 0;
        error = string.Empty;

        if (_memory is null || !_playerBaseAddress.HasValue)
        {
            error = "Not attached to Cemu.exe.";
            return false;
        }

        var address = _playerBaseAddress.Value + definition.Offset;
        switch (definition.ValueKind)
        {
            case CheatValueKind.Byte:
                if (!_memory.TryReadBytes(address, 1, out var bytes, out var bytesRead) || bytesRead != 1)
                {
                    error = $"Could not read {definition.Name} at 0x{address:X}.";
                    return false;
                }

                value = bytes[0];
                return true;

            case CheatValueKind.UInt16BigEndian:
                if (!BigEndianMemory.TryReadUInt16(_memory, address, out var beValue, out error))
                {
                    return false;
                }

                value = beValue;
                return true;

            default:
                error = $"Unsupported integer value type for {definition.Name}.";
                return false;
        }
    }

    private bool TryReadUInt32Value(CheatDefinition definition, out uint value, out string error)
    {
        value = 0;
        error = string.Empty;

        if (_memory is null || !_playerBaseAddress.HasValue)
        {
            error = "Not attached to Cemu.exe.";
            return false;
        }

        if (definition.ValueKind != CheatValueKind.UInt32BigEndian)
        {
            error = $"{definition.Name} is not a 4-byte big-endian value.";
            return false;
        }

        return BigEndianMemory.TryReadUInt32(_memory, _playerBaseAddress.Value + definition.Offset, out value, out error);
    }

    private static bool UnsupportedWrite(TrainerValueViewModel value, out string error)
    {
        error = $"Unsupported value type for {value.Name}.";
        return false;
    }

    private static bool UnsupportedCapacityWrite(CapacitySelectorViewModel capacity, out string error)
    {
        error = $"Unsupported value type for {capacity.Name}.";
        return false;
    }

    private int GetMaximumHealthLimit()
    {
        if (MaximumHealth.CurrentNumericValue.HasValue)
        {
            return MaximumHealth.CurrentNumericValue.Value;
        }

        return MaximumHealth.TryGetClampedTarget(out var target, out _, out _)
            ? target
            : MaximumHealth.Definition.HardMaximum;
    }

    private void UpdateDerivedDisplays()
    {
        var maximumHealth = MaximumHealth.CurrentNumericValue;
        HeartContainersText.Text = maximumHealth.HasValue
            ? (maximumHealth.Value / 4).ToString(CultureInfo.InvariantCulture)
            : "Not read";

        TotalHeartPiecesText.Text = "Not mapped in CT";
        CollectiblesHeartContainersText.Text = HeartContainersText.Text;
    }

    private static int CountGoldenBugs(uint flags)
    {
        var usedBits = flags & 0x00FF_FFFF;
        var count = 0;

        while (usedBits != 0)
        {
            count += (int)(usedBits & 1);
            usedBits >>= 1;
        }

        return Math.Min(count, 24);
    }

    private void MarkMemoryUnavailable()
    {
        foreach (var value in _values.Values)
        {
            value.MarkNotRead();
        }

        foreach (var capacity in _capacities.Values)
        {
            capacity.MarkNotRead();
        }

        UpdateDerivedDisplays();
        SetStatus("Player data could not be read. Load into gameplay and rescan.", StatusKind.Warning);
    }

    private void Detach(bool clearStatus)
    {
        _refreshTimer.Stop();
        _memory?.Dispose();
        _memory = null;
        _playerBaseAddress = null;
        PidText.Text = "-";
        PlayerBaseText.Text = "-";
        DebugPlayerBaseText.Text = "-";
        DetachButton.IsEnabled = false;

        foreach (var value in _values.Values)
        {
            value.MarkNotRead();
        }

        foreach (var capacity in _capacities.Values)
        {
            capacity.MarkNotRead();
        }

        GoldenBugsCountText.Text = "Not read";
        UpdateDerivedDisplays();

        if (clearStatus)
        {
            SetStatus("Not attached", StatusKind.Neutral);
        }
    }

    private void SetControlsEnabled(bool enabled)
    {
        AttachButton.IsEnabled = enabled;
        TrainerTabs.IsEnabled = enabled;
    }

    private void SetStatus(string message, StatusKind kind)
    {
        StatusText.Text = message;
        StatusDot.Fill = kind switch
        {
            StatusKind.Connected => new SolidColorBrush(Color.FromRgb(24, 163, 98)),
            StatusKind.Warning => new SolidColorBrush(Color.FromRgb(224, 158, 54)),
            StatusKind.Working => new SolidColorBrush(Color.FromRgb(45, 112, 179)),
            _ => new SolidColorBrush(Color.FromRgb(128, 128, 128))
        };
    }

    protected override void OnClosed(EventArgs e)
    {
        Detach(clearStatus: false);
        base.OnClosed(e);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private enum StatusKind
    {
        Neutral,
        Working,
        Connected,
        Warning
    }
}
