using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
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
    private bool _inventoryDiagnosticInProgress;
    private bool _equipmentDiagnosticInProgress;
    private bool _holdInventoryValueAfterApply;
    private bool _allowEditingUninitializedInventory;
    private bool _allowEditingUninitializedEquipment;
    private bool _advancedEquipmentEditing;
    private bool _hasPlayerData;
    private bool _inventoryInitialized;
    private bool _equipmentInitialized;
    private ProgressionState _progressionState = ProgressionStateService.CreateUnavailable();

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

        InventorySlots = new ObservableCollection<InventorySlotViewModel>(
            Enumerable.Range(0, InventoryDefinitions.SlotCount)
                .Select(slotIndex => new InventorySlotViewModel(slotIndex, InventoryDefinitions.SafeItems)));
        FixedInventoryItems = new ObservableCollection<InventoryFixedSlotViewModel>(
            InventoryDefinitions.FixedSlots.Select(slot => new InventoryFixedSlotViewModel(slot)));
        InventoryDiagnostics = [];

        EquipmentSlots = new ObservableCollection<EquipmentSlotViewModel>(
            EquipmentDefinitions.Slots.Select(slot => new EquipmentSlotViewModel(slot)));
        EquipmentFlags = new ObservableCollection<EquipmentFlagViewModel>(
            EquipmentDefinitions.OwnershipFlags.Select(flag => new EquipmentFlagViewModel(flag)));
        EquipmentDiagnostics = [];

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
        ApplyProgressionState(ProgressionStateService.CreateUnavailable());
        UpdateEquipmentEditGuard();

        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _refreshTimer.Tick += RefreshTimer_Tick;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private static string InventoryLogPath =>
        Path.Combine(AppContext.BaseDirectory, "logs", "inventory.log");

    private static string EquipmentLogPath =>
        Path.Combine(AppContext.BaseDirectory, "logs", "equipment.log");

    private static string ProgressionLogPath =>
        Path.Combine(AppContext.BaseDirectory, "logs", "progression.log");

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

    public ObservableCollection<InventorySlotViewModel> InventorySlots { get; }

    public ObservableCollection<InventoryFixedSlotViewModel> FixedInventoryItems { get; }

    public ObservableCollection<string> InventoryDiagnostics { get; }

    public ObservableCollection<EquipmentSlotViewModel> EquipmentSlots { get; }

    public ObservableCollection<EquipmentFlagViewModel> EquipmentFlags { get; }

    public ObservableCollection<string> EquipmentDiagnostics { get; }

    public bool HoldInventoryValueAfterApply
    {
        get => _holdInventoryValueAfterApply;
        set
        {
            if (_holdInventoryValueAfterApply != value)
            {
                _holdInventoryValueAfterApply = value;
                OnPropertyChanged();
            }
        }
    }

    public bool HasPlayerData
    {
        get => _hasPlayerData;
        private set => SetMainProperty(ref _hasPlayerData, value);
    }

    public bool InventoryInitialized
    {
        get => _inventoryInitialized;
        private set => SetMainProperty(ref _inventoryInitialized, value);
    }

    public bool EquipmentInitialized
    {
        get => _equipmentInitialized;
        private set => SetMainProperty(ref _equipmentInitialized, value);
    }

    public bool AllowEditingUninitializedInventory
    {
        get => _allowEditingUninitializedInventory;
        set
        {
            if (_allowEditingUninitializedInventory != value)
            {
                _allowEditingUninitializedInventory = value;
                OnPropertyChanged();
                UpdateInventoryEditGuard();
            }
        }
    }

    public bool AllowEditingUninitializedEquipment
    {
        get => _allowEditingUninitializedEquipment;
        set
        {
            if (_allowEditingUninitializedEquipment != value)
            {
                _allowEditingUninitializedEquipment = value;
                OnPropertyChanged();
                UpdateEquipmentEditGuard();
            }
        }
    }

    public bool AdvancedEquipmentEditing
    {
        get => _advancedEquipmentEditing;
        set
        {
            if (_advancedEquipmentEditing != value)
            {
                _advancedEquipmentEditing = value;
                OnPropertyChanged();
                UpdateEquipmentEditGuard();
            }
        }
    }

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
                ApplyProgressionState(ProgressionStateService.CreateUnavailable());
                SetStatus("Cemu found. Load into gameplay and rescan.", StatusKind.Warning);
                return;
            }

            _memory = memory;
            _playerBaseAddress = foundAddress.Value;
            PlayerBaseText.Text = $"0x{foundAddress.Value:X}";
            DebugPlayerBaseText.Text = PlayerBaseText.Text;
            DetachButton.IsEnabled = true;
            _refreshTimer.Start();

            var progressionRead = RefreshProgressionState();
            if (!progressionRead)
            {
                SetStatus("Player data found, but initialization state could not be read. Load into gameplay and rescan.", StatusKind.Warning);
            }
            else if (!InventoryInitialized || !EquipmentInitialized)
            {
                SetStatus("Player data found. Inventory or equipment is not initialized yet.", StatusKind.Warning);
            }
            else
            {
                SetStatus("Player data found. Game loaded.", StatusKind.Connected);
            }

            AppendProgressionDiagnostic();
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

    private void RefreshInventoryButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshInventorySlots(showStatus: true);
    }

    private async void ApplyFixedInventoryItem_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is InventoryFixedSlotViewModel item)
        {
            if (!CanWriteInventorySlot())
            {
                return;
            }

            await WriteInventorySlotWithDiagnosticsAsync(
                item.SlotIndex,
                item.SlotNumber,
                item.OffsetValue,
                item.Offset,
                item.SelectedItem.ItemId,
                holdAfterWrite: HoldInventoryValueAfterApply,
                successMessage: $"{item.Name} set to {item.SelectedItem.Name}.");
        }
    }

    private async void ApplyInventorySlot_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is InventorySlotViewModel slot)
        {
            if (!CanWriteInventorySlot())
            {
                return;
            }

            await WriteInventorySlotWithDiagnosticsAsync(
                slot.SlotIndex,
                slot.SlotNumber,
                slot.OffsetValue,
                slot.Offset,
                slot.SelectedItem.ItemId,
                holdAfterWrite: HoldInventoryValueAfterApply,
                successMessage: $"{slot.SelectedItem.Name} applied to slot {slot.SlotNumber}.");
        }
    }

    private async void ClearInventorySlot_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is InventorySlotViewModel slot)
        {
            if (!CanWriteInventorySlot())
            {
                return;
            }

            await WriteInventorySlotWithDiagnosticsAsync(
                slot.SlotIndex,
                slot.SlotNumber,
                slot.OffsetValue,
                slot.Offset,
                InventoryDefinitions.EmptyItemId,
                holdAfterWrite: false,
                successMessage: $"Slot {slot.SlotNumber} cleared.");
        }
    }

    private void RefreshEquipmentButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshEquipment(showStatus: true);
    }

    private async void ApplyEquippedEquipment_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is EquipmentSlotViewModel slot)
        {
            if (!CanWriteEquipment())
            {
                return;
            }

            await WriteEquippedEquipmentWithDiagnosticsAsync(
                slot,
                slot.SelectedOption.Value,
                $"{slot.Name} set to {slot.SelectedOption.Name}.");
        }
    }

    private async void EquipmentFlag_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as CheckBox)?.Tag is EquipmentFlagViewModel flag)
        {
            if (!CanWriteEquipment())
            {
                RefreshEquipment(showStatus: false);
                return;
            }

            var desiredValue = ((CheckBox)sender).IsChecked == true;
            await WriteEquipmentFlagWithDiagnosticsAsync(
                flag,
                desiredValue,
                $"{flag.Name} ownership set to {(desiredValue ? "Yes" : "No")}.");
        }
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

            if (!_inventoryDiagnosticInProgress && !RefreshInventorySlots(showStatus: false))
            {
                return;
            }

            if (!_equipmentDiagnosticInProgress && !RefreshEquipment(showStatus: false))
            {
                return;
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

    private bool RefreshProgressionState()
    {
        if (_memory is null || !_playerBaseAddress.HasValue)
        {
            ApplyProgressionState(ProgressionStateService.CreateUnavailable());
            return false;
        }

        if (!ProgressionStateService.TryReadState(
                _memory,
                _playerBaseAddress.Value,
                out var state,
                out _))
        {
            ApplyProgressionState(ProgressionStateService.CreateUnavailable());
            return false;
        }

        ApplyProgressionState(state);
        return true;
    }

    private void ApplyProgressionState(ProgressionState state)
    {
        _progressionState = state;
        HasPlayerData = state.HasPlayerData;
        InventoryInitialized = state.InventoryInitialized;
        EquipmentInitialized = state.EquipmentInitialized;

        UpdateProgressionStateDisplays();
        UpdateInventoryEditGuard();
        UpdateEquipmentEditGuard();
    }

    private void UpdateProgressionStateDisplays()
    {
        PlayerDataStateText.Text = _progressionState.PlayerDataStatus;
        InventoryStateText.Text = HasPlayerData ? _progressionState.InventoryStatus : "-";
        EquipmentStateText.Text = HasPlayerData ? _progressionState.EquipmentStatus : "-";

        ProgressionPlayerBaseText.Text = _progressionState.PlayerBaseText;
        ProgressionInventoryInitializedText.Text = HasPlayerData
            ? _progressionState.InventoryStatus
            : "-";
        ProgressionEquipmentInitializedText.Text = HasPlayerData
            ? _progressionState.EquipmentStatus
            : "-";
        ProgressionInventoryRawBytesText.Text = _progressionState.InventoryRawBytesText;
        ProgressionEquipmentOwnershipBytesText.Text = HasPlayerData
            ? _progressionState.EquipmentOwnershipBytesText
            : "Not read";
        ProgressionEquipmentEquippedBytesText.Text = HasPlayerData
            ? _progressionState.EquipmentEquippedBytesText
            : "Not read";
    }

    private bool RefreshInventorySlots(bool showStatus)
    {
        if (_memory is null || !_playerBaseAddress.HasValue)
        {
            if (showStatus)
            {
                SetStatus("Not attached. Attach to Cemu and rescan before refreshing inventory.", StatusKind.Neutral);
            }

            return false;
        }

        var allSlotsEmpty = true;
        var rawBytes = new byte[InventoryDefinitions.SlotCount];

        foreach (var slot in InventorySlots)
        {
            if (!InventoryMemoryService.TryReadSlot(
                    _memory,
                    _playerBaseAddress.Value,
                    slot.SlotIndex,
                    out var itemId,
                    out _))
            {
                MarkMemoryUnavailable();
                return false;
            }

            slot.SetCurrentItem(itemId);
            rawBytes[slot.SlotIndex] = itemId;
            allSlotsEmpty &= itemId == InventoryDefinitions.EmptyItemId;
        }

        foreach (var item in FixedInventoryItems)
        {
            item.SetCurrentItem(rawBytes[item.SlotIndex]);
        }

        SetInventoryInitialized(!allSlotsEmpty, rawBytes);

        if (allSlotsEmpty && showStatus)
        {
            AppendInventoryStateDiagnostic("inventory-all-255-empty-or-uninitialized");
            SetStatus("Inventory has not been initialized by the game yet.", StatusKind.Warning);
        }
        else if (!allSlotsEmpty && showStatus)
        {
            SetStatus("Inventory slots refreshed.", StatusKind.Connected);
        }

        if (showStatus)
        {
            UpdateInventoryEditGuard();
        }

        return true;
    }

    private bool CanWriteInventorySlot()
    {
        if (!InventoryInitialized && !AllowEditingUninitializedInventory)
        {
            SetStatus("Inventory has not been initialized by the game yet. Enable the advanced override to edit it.", StatusKind.Warning);
            return false;
        }

        return true;
    }

    private void SetInventoryInitialized(bool initialized, byte[]? rawBytes)
    {
        InventoryInitialized = initialized;

        if (rawBytes is not null)
        {
            _progressionState = _progressionState with
            {
                InventoryInitialized = initialized,
                InventoryRawBytes = rawBytes
            };
        }

        UpdateProgressionStateDisplays();
        UpdateInventoryEditGuard();
    }

    private void UpdateInventoryEditGuard()
    {
        var canEdit = HasPlayerData && (InventoryInitialized || AllowEditingUninitializedInventory);
        foreach (var slot in InventorySlots)
        {
            slot.CanEdit = canEdit;
        }

        foreach (var item in FixedInventoryItems)
        {
            item.CanEdit = canEdit;
        }

        InventoryInitializationWarningText.Visibility = HasPlayerData && !InventoryInitialized
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private bool RefreshEquipment(bool showStatus)
    {
        if (_memory is null || !_playerBaseAddress.HasValue)
        {
            if (showStatus)
            {
                SetStatus("Not attached. Attach to Cemu and rescan before refreshing equipment.", StatusKind.Neutral);
            }

            UpdateEquipmentEditGuard();
            return false;
        }

        byte equippedArmor = 0;
        byte equippedSword = 0;
        byte equippedShield = 0;

        foreach (var slot in EquipmentSlots)
        {
            if (!EquipmentMemoryService.TryReadEquipped(
                    _memory,
                    _playerBaseAddress.Value,
                    slot.Definition,
                    out var value,
                    out _))
            {
                MarkMemoryUnavailable();
                return false;
            }

            slot.SetCurrentValue(value);
            if (slot.Definition == EquipmentDefinitions.ArmorSlot)
            {
                equippedArmor = value;
            }
            else if (slot.Definition == EquipmentDefinitions.SwordSlot)
            {
                equippedSword = value;
            }
            else if (slot.Definition == EquipmentDefinitions.ShieldSlot)
            {
                equippedShield = value;
            }
        }

        var backingBytes = new Dictionary<uint, byte>();
        foreach (var flag in EquipmentFlags)
        {
            if (!EquipmentMemoryService.TryReadFlag(
                    _memory,
                    _playerBaseAddress.Value,
                    flag.Definition,
                    out var isOwned,
                    out var backingValue,
                    out _))
            {
                MarkMemoryUnavailable();
                return false;
            }

            flag.SetCurrentValue(isOwned, backingValue);
            backingBytes[flag.OffsetValue] = backingValue;
        }

        backingBytes.TryGetValue(ProgressionStateService.ArmorOwnershipOffset, out var armorOwnershipByte);
        backingBytes.TryGetValue(ProgressionStateService.EquipmentOwnershipOffset, out var equipmentOwnershipByte);
        backingBytes.TryGetValue(ProgressionStateService.MasterSwordInfusedOffset, out var masterSwordInfusedByte);

        var equipmentInitialized = ProgressionStateService.IsEquipmentInitialized(
            armorOwnershipByte,
            equipmentOwnershipByte,
            masterSwordInfusedByte,
            equippedArmor,
            equippedSword,
            equippedShield);

        SetEquipmentInitialized(
            equipmentInitialized,
            armorOwnershipByte,
            equipmentOwnershipByte,
            masterSwordInfusedByte,
            equippedArmor,
            equippedSword,
            equippedShield);

        if (showStatus)
        {
            SetStatus(
                !equipmentInitialized
                    ? "Equipment has not been initialized by the game yet."
                    : "Equipment data refreshed.",
                !equipmentInitialized ? StatusKind.Warning : StatusKind.Connected);
        }

        return true;
    }

    private bool CanWriteEquipment()
    {
        if (!AdvancedEquipmentEditing)
        {
            SetStatus("Enable Advanced equipment editing before writing equipment.", StatusKind.Warning);
            return false;
        }

        if (_memory is null || !_playerBaseAddress.HasValue)
        {
            SetStatus("Not attached. Attach to Cemu and rescan before editing equipment.", StatusKind.Neutral);
            return false;
        }

        if (!EquipmentInitialized && !AllowEditingUninitializedEquipment)
        {
            SetStatus("Equipment has not been initialized by the game yet. Enable the advanced override to edit it.", StatusKind.Warning);
            return false;
        }

        return true;
    }

    private void SetEquipmentInitialized(
        bool initialized,
        byte armorOwnershipByte,
        byte equipmentOwnershipByte,
        byte masterSwordInfusedByte,
        byte equippedArmor,
        byte equippedSword,
        byte equippedShield)
    {
        EquipmentInitialized = initialized;

        _progressionState = _progressionState with
        {
            EquipmentInitialized = initialized,
            ArmorOwnershipByte = armorOwnershipByte,
            EquipmentOwnershipByte = equipmentOwnershipByte,
            MasterSwordInfusedByte = masterSwordInfusedByte,
            EquippedArmor = equippedArmor,
            EquippedSword = equippedSword,
            EquippedShield = equippedShield
        };

        UpdateProgressionStateDisplays();
        UpdateEquipmentEditGuard();
    }

    private void UpdateEquipmentEditGuard()
    {
        var canEdit =
            AdvancedEquipmentEditing &&
            HasPlayerData &&
            (EquipmentInitialized || AllowEditingUninitializedEquipment);
        foreach (var slot in EquipmentSlots)
        {
            slot.CanEdit = canEdit;
        }

        foreach (var flag in EquipmentFlags)
        {
            flag.CanEdit = canEdit;
        }

        EquipmentInitializationWarningText.Visibility = HasPlayerData && !EquipmentInitialized
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private async Task<bool> WriteInventorySlotWithDiagnosticsAsync(
        int slotIndex,
        int slotNumber,
        uint offsetValue,
        string offset,
        byte itemId,
        bool holdAfterWrite,
        string successMessage)
    {
        var memory = _memory;
        if (memory is null || !_playerBaseAddress.HasValue)
        {
            SetStatus("Not attached. Attach to Cemu and rescan before editing inventory.", StatusKind.Neutral);
            return false;
        }

        _inventoryDiagnosticInProgress = true;

        byte? oldValue = null;
        byte? immediateReadback = null;
        byte? delayed250Readback = null;
        byte? delayed1000Readback = null;
        var expectedValue = itemId;
        var playerBaseAddress = _playerBaseAddress.Value;
        var absoluteAddress = playerBaseAddress + offsetValue;
        var diagnosticStatus = "started";
        var isGameManagedSlot = InventoryDefinitions.IsGameManagedSlot(slotIndex);

        try
        {
            if (!InventoryMemoryService.TryReadSlot(
                    memory,
                    playerBaseAddress,
                    slotIndex,
                    out var oldItemId,
                    out var oldReadError))
            {
                diagnosticStatus = $"old-read-failed: {oldReadError}";
                SetStatus(oldReadError, StatusKind.Warning);
                return false;
            }

            oldValue = oldItemId;

            if (!InventoryMemoryService.TryWriteSlot(
                    memory,
                    playerBaseAddress,
                    slotIndex,
                    expectedValue,
                    out var writeError))
            {
                diagnosticStatus = $"write-call-failed: {writeError}";
                SetStatus($"Write failed: {writeError}", StatusKind.Warning);
                return false;
            }

            if (!InventoryMemoryService.TryReadSlot(
                    memory,
                    playerBaseAddress,
                    slotIndex,
                    out var immediateItemId,
                    out var immediateReadError))
            {
                diagnosticStatus = $"immediate-read-failed: {immediateReadError}";
                SetStatus(immediateReadError, StatusKind.Warning);
                return false;
            }

            immediateReadback = immediateItemId;

            if (immediateReadback.Value != expectedValue)
            {
                diagnosticStatus = "immediate-mismatch";
                SetStatus($"Write failed: expected {expectedValue} but read {immediateReadback.Value}", StatusKind.Warning);
                RefreshInventorySlots(showStatus: false);
                return false;
            }

            var holdTask = holdAfterWrite
                ? HoldInventoryValueAsync(
                    memory,
                    playerBaseAddress,
                    slotIndex,
                    expectedValue,
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromMilliseconds(50))
                : Task.CompletedTask;

            await Task.Delay(250);
            if (InventoryMemoryService.TryReadSlot(
                    memory,
                    playerBaseAddress,
                    slotIndex,
                    out var delayed250ItemId,
                    out _))
            {
                delayed250Readback = delayed250ItemId;
            }

            await Task.Delay(750);
            if (InventoryMemoryService.TryReadSlot(
                    memory,
                    playerBaseAddress,
                    slotIndex,
                    out var delayed1000ItemId,
                    out _))
            {
                delayed1000Readback = delayed1000ItemId;
            }

            await holdTask;

            var laterReverted =
                delayed250Readback.HasValue && delayed250Readback.Value != expectedValue ||
                delayed1000Readback.HasValue && delayed1000Readback.Value != expectedValue;

            if (laterReverted)
            {
                diagnosticStatus = isGameManagedSlot ? "later-reverted-game-managed" : "later-reverted";
                SetStatus(
                    isGameManagedSlot
                        ? "This slot appears game-managed. Use the specific item editor instead."
                        : "Write succeeded, but value later reverted",
                    StatusKind.Warning);
            }
            else
            {
                diagnosticStatus = holdAfterWrite ? "held-and-verified" : "verified";
                SetStatus(successMessage, StatusKind.Connected);
            }

            RefreshInventorySlots(showStatus: false);
            return !laterReverted;
        }
        finally
        {
            AppendInventoryDiagnostic(
                slotNumber,
                offset,
                absoluteAddress,
                oldValue,
                expectedValue,
                immediateReadback,
                delayed250Readback,
                delayed1000Readback,
                holdAfterWrite,
                diagnosticStatus);

            _inventoryDiagnosticInProgress = false;
        }
    }

    private async Task<bool> WriteEquippedEquipmentWithDiagnosticsAsync(
        EquipmentSlotViewModel slot,
        byte expectedValue,
        string successMessage)
    {
        var memory = _memory;
        if (memory is null || !_playerBaseAddress.HasValue)
        {
            SetStatus("Not attached. Attach to Cemu and rescan before editing equipment.", StatusKind.Neutral);
            return false;
        }

        _equipmentDiagnosticInProgress = true;

        byte? oldValue = null;
        byte? immediateReadback = null;
        byte? delayed250Readback = null;
        byte? delayed1000Readback = null;
        var playerBaseAddress = _playerBaseAddress.Value;
        var absoluteAddress = playerBaseAddress + slot.OffsetValue;
        var diagnosticStatus = "started";
        var refreshAfterWrite = false;

        try
        {
            if (!EquipmentMemoryService.TryReadEquipped(
                    memory,
                    playerBaseAddress,
                    slot.Definition,
                    out var oldEquippedValue,
                    out var oldReadError))
            {
                diagnosticStatus = $"old-read-failed: {oldReadError}";
                SetStatus(oldReadError, StatusKind.Warning);
                return false;
            }

            oldValue = oldEquippedValue;

            if (!EquipmentMemoryService.TryWriteEquipped(
                    memory,
                    playerBaseAddress,
                    slot.Definition,
                    expectedValue,
                    out var writeError))
            {
                diagnosticStatus = $"write-call-failed: {writeError}";
                SetStatus("Write failed or wrong address.", StatusKind.Warning);
                return false;
            }

            refreshAfterWrite = true;

            if (!EquipmentMemoryService.TryReadEquipped(
                    memory,
                    playerBaseAddress,
                    slot.Definition,
                    out var immediateValue,
                    out var immediateReadError))
            {
                diagnosticStatus = $"immediate-read-failed: {immediateReadError}";
                SetStatus("Write failed or wrong address.", StatusKind.Warning);
                return false;
            }

            immediateReadback = immediateValue;

            if (immediateReadback.Value != expectedValue)
            {
                diagnosticStatus = "immediate-mismatch";
                SetStatus("Write failed or wrong address.", StatusKind.Warning);
                return false;
            }

            await Task.Delay(250);
            if (EquipmentMemoryService.TryReadEquipped(
                    memory,
                    playerBaseAddress,
                    slot.Definition,
                    out var delayed250Value,
                    out _))
            {
                delayed250Readback = delayed250Value;
            }

            await Task.Delay(750);
            if (EquipmentMemoryService.TryReadEquipped(
                    memory,
                    playerBaseAddress,
                    slot.Definition,
                    out var delayed1000Value,
                    out _))
            {
                delayed1000Readback = delayed1000Value;
            }

            var laterReverted =
                delayed250Readback.HasValue && delayed250Readback.Value != expectedValue ||
                delayed1000Readback.HasValue && delayed1000Readback.Value != expectedValue;

            if (laterReverted)
            {
                diagnosticStatus = "later-reverted";
                SetStatus("Write succeeded, but game reverted it.", StatusKind.Warning);
            }
            else
            {
                diagnosticStatus = "verified";
                SetStatus(successMessage, StatusKind.Connected);
            }

            return !laterReverted;
        }
        finally
        {
            if (refreshAfterWrite)
            {
                RefreshEquipment(showStatus: false);
            }

            AppendEquipmentDiagnostic(
                "equipped",
                slot.Name,
                slot.Offset,
                absoluteAddress,
                oldValue,
                expectedValue,
                immediateReadback,
                delayed250Readback,
                delayed1000Readback,
                diagnosticStatus);

            _equipmentDiagnosticInProgress = false;
            UpdateEquipmentEditGuard();
        }
    }

    private async Task<bool> WriteEquipmentFlagWithDiagnosticsAsync(
        EquipmentFlagViewModel flag,
        bool desiredValue,
        string successMessage)
    {
        var memory = _memory;
        if (memory is null || !_playerBaseAddress.HasValue)
        {
            SetStatus("Not attached. Attach to Cemu and rescan before editing equipment.", StatusKind.Neutral);
            return false;
        }

        _equipmentDiagnosticInProgress = true;

        byte? oldValue = null;
        byte? writtenValue = null;
        byte? immediateReadback = null;
        byte? delayed250Readback = null;
        byte? delayed1000Readback = null;
        var playerBaseAddress = _playerBaseAddress.Value;
        var absoluteAddress = playerBaseAddress + flag.OffsetValue;
        var diagnosticStatus = "started";
        var refreshAfterWrite = false;

        try
        {
            if (!EquipmentMemoryService.TryWriteFlag(
                    memory,
                    playerBaseAddress,
                    flag.Definition,
                    desiredValue,
                    out var oldBackingValue,
                    out var writtenBackingValue,
                    out var writeError))
            {
                diagnosticStatus = $"write-call-failed: {writeError}";
                SetStatus("Write failed or wrong address.", StatusKind.Warning);
                return false;
            }

            oldValue = oldBackingValue;
            writtenValue = writtenBackingValue;
            refreshAfterWrite = true;

            if (!EquipmentMemoryService.TryReadByte(
                    memory,
                    playerBaseAddress,
                    flag.OffsetValue,
                    flag.Name,
                    out var immediateValue,
                    out var immediateReadError))
            {
                diagnosticStatus = $"immediate-read-failed: {immediateReadError}";
                SetStatus("Write failed or wrong address.", StatusKind.Warning);
                return false;
            }

            immediateReadback = immediateValue;

            if (!DoesFlagMatch(immediateValue, flag.Definition.Mask, desiredValue))
            {
                diagnosticStatus = "immediate-mismatch";
                SetStatus("Write failed or wrong address.", StatusKind.Warning);
                return false;
            }

            await Task.Delay(250);
            if (EquipmentMemoryService.TryReadByte(
                    memory,
                    playerBaseAddress,
                    flag.OffsetValue,
                    flag.Name,
                    out var delayed250Value,
                    out _))
            {
                delayed250Readback = delayed250Value;
            }

            await Task.Delay(750);
            if (EquipmentMemoryService.TryReadByte(
                    memory,
                    playerBaseAddress,
                    flag.OffsetValue,
                    flag.Name,
                    out var delayed1000Value,
                    out _))
            {
                delayed1000Readback = delayed1000Value;
            }

            var laterReverted =
                delayed250Readback.HasValue && !DoesFlagMatch(delayed250Readback.Value, flag.Definition.Mask, desiredValue) ||
                delayed1000Readback.HasValue && !DoesFlagMatch(delayed1000Readback.Value, flag.Definition.Mask, desiredValue);

            if (laterReverted)
            {
                diagnosticStatus = "later-reverted";
                SetStatus("Write succeeded, but game reverted it.", StatusKind.Warning);
            }
            else
            {
                diagnosticStatus = "verified";
                SetStatus(successMessage, StatusKind.Connected);
            }

            return !laterReverted;
        }
        finally
        {
            if (refreshAfterWrite)
            {
                RefreshEquipment(showStatus: false);
            }

            AppendEquipmentDiagnostic(
                "ownership-flag",
                $"{flag.Name} bit {flag.Bit}",
                flag.Offset,
                absoluteAddress,
                oldValue,
                writtenValue,
                immediateReadback,
                delayed250Readback,
                delayed1000Readback,
                diagnosticStatus);

            _equipmentDiagnosticInProgress = false;
            UpdateEquipmentEditGuard();
        }
    }

    private static bool DoesFlagMatch(byte value, byte mask, bool expected)
    {
        return ((value & mask) != 0) == expected;
    }

    private async Task HoldInventoryValueAsync(
        ProcessMemory memory,
        ulong playerBaseAddress,
        int slotIndex,
        byte itemId,
        TimeSpan duration,
        TimeSpan interval)
    {
        var stopAt = DateTimeOffset.UtcNow + duration;
        while (DateTimeOffset.UtcNow < stopAt)
        {
            if (memory.HasExited)
            {
                return;
            }

            InventoryMemoryService.TryWriteSlot(
                memory,
                playerBaseAddress,
                slotIndex,
                itemId,
                out _);

            await Task.Delay(interval);
        }
    }

    private void AppendInventoryDiagnostic(
        int slotNumber,
        string offset,
        ulong absoluteAddress,
        byte? oldValue,
        byte writtenValue,
        byte? immediateReadback,
        byte? delayed250Readback,
        byte? delayed1000Readback,
        bool holdEnabled,
        string diagnosticStatus)
    {
        var entry =
            $"{DateTimeOffset.Now:O} slot={slotNumber} offset={offset} address=0x{absoluteAddress:X} " +
            $"old={FormatByte(oldValue)} wrote={writtenValue} immediate={FormatByte(immediateReadback)} " +
            $"read250ms={FormatByte(delayed250Readback)} read1000ms={FormatByte(delayed1000Readback)} " +
            $"hold2s={holdEnabled} status={diagnosticStatus}";

        AppendInventoryLogEntry(entry);
    }

    private void AppendInventoryStateDiagnostic(string state)
    {
        AppendInventoryLogEntry(
            $"{DateTimeOffset.Now:O} inventory-state={state} slots={InventoryDefinitions.SlotCount} " +
            $"empty-id={InventoryDefinitions.EmptyItemId} override={AllowEditingUninitializedInventory}");
    }

    private void AppendInventoryLogEntry(string entry)
    {
        InventoryDiagnostics.Insert(0, entry);
        while (InventoryDiagnostics.Count > 100)
        {
            InventoryDiagnostics.RemoveAt(InventoryDiagnostics.Count - 1);
        }

        try
        {
            var logDirectory = Path.GetDirectoryName(InventoryLogPath);
            if (!string.IsNullOrWhiteSpace(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            File.AppendAllText(InventoryLogPath, entry + Environment.NewLine);
        }
        catch (Exception ex)
        {
            InventoryDiagnostics.Insert(0, $"{DateTimeOffset.Now:O} inventory-log-write-failed: {ex.Message}");
        }
    }

    private void AppendEquipmentDiagnostic(
        string kind,
        string name,
        string offset,
        ulong absoluteAddress,
        byte? oldValue,
        byte? writtenValue,
        byte? immediateReadback,
        byte? delayed250Readback,
        byte? delayed1000Readback,
        string diagnosticStatus)
    {
        var entry =
            $"{DateTimeOffset.Now:O} kind={kind} name=\"{name}\" offset={offset} address=0x{absoluteAddress:X} " +
            $"old={FormatEquipmentByte(oldValue)} wrote={FormatEquipmentByte(writtenValue)} " +
            $"immediate={FormatEquipmentByte(immediateReadback)} read250ms={FormatEquipmentByte(delayed250Readback)} " +
            $"read1000ms={FormatEquipmentByte(delayed1000Readback)} final={FormatEquipmentByte(delayed1000Readback)} " +
            $"status={diagnosticStatus}";

        AppendEquipmentLogEntry(entry);
    }

    private void AppendEquipmentLogEntry(string entry)
    {
        EquipmentDiagnostics.Insert(0, entry);
        while (EquipmentDiagnostics.Count > 100)
        {
            EquipmentDiagnostics.RemoveAt(EquipmentDiagnostics.Count - 1);
        }

        try
        {
            var logDirectory = Path.GetDirectoryName(EquipmentLogPath);
            if (!string.IsNullOrWhiteSpace(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            File.AppendAllText(EquipmentLogPath, entry + Environment.NewLine);
        }
        catch (Exception ex)
        {
            EquipmentDiagnostics.Insert(0, $"{DateTimeOffset.Now:O} equipment-log-write-failed: {ex.Message}");
        }
    }

    private void AppendProgressionDiagnostic()
    {
        var entry =
            $"{DateTimeOffset.Now:O} player-base={_progressionState.PlayerBaseText} " +
            $"has-player-data={HasPlayerData} inventory-initialized={InventoryInitialized} " +
            $"equipment-initialized={EquipmentInitialized} inventory-bytes=\"{_progressionState.InventoryRawBytesText}\" " +
            $"equipment-ownership-bytes=\"{_progressionState.EquipmentOwnershipBytesText}\" " +
            $"equipment-equipped-bytes=\"{_progressionState.EquipmentEquippedBytesText}\" " +
            $"status=\"{StatusText.Text}\"";

        try
        {
            var logDirectory = Path.GetDirectoryName(ProgressionLogPath);
            if (!string.IsNullOrWhiteSpace(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            File.AppendAllText(ProgressionLogPath, entry + Environment.NewLine);
        }
        catch (Exception ex)
        {
            EquipmentDiagnostics.Insert(0, $"{DateTimeOffset.Now:O} progression-log-write-failed: {ex.Message}");
        }
    }

    private static string FormatByte(byte? value)
    {
        return value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "n/a";
    }

    private static string FormatEquipmentByte(byte? value)
    {
        return value.HasValue ? $"{value.Value} (0x{value.Value:X2})" : "n/a";
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

        foreach (var slot in InventorySlots)
        {
            slot.MarkNotRead();
        }

        foreach (var item in FixedInventoryItems)
        {
            item.MarkNotRead();
        }

        foreach (var slot in EquipmentSlots)
        {
            slot.MarkNotRead();
        }

        foreach (var flag in EquipmentFlags)
        {
            flag.MarkNotRead();
        }

        ApplyProgressionState(ProgressionStateService.CreateUnavailable());

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

        foreach (var slot in InventorySlots)
        {
            slot.MarkNotRead();
        }

        foreach (var item in FixedInventoryItems)
        {
            item.MarkNotRead();
        }

        foreach (var slot in EquipmentSlots)
        {
            slot.MarkNotRead();
        }

        foreach (var flag in EquipmentFlags)
        {
            flag.MarkNotRead();
        }

        ApplyProgressionState(ProgressionStateService.CreateUnavailable());

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
        ProgressionStatusMessageText.Text = message;
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

    private void SetMainProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        OnPropertyChanged(propertyName);
    }

    private enum StatusKind
    {
        Neutral,
        Working,
        Connected,
        Warning
    }
}
