using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using TphdCemuTrainer.Cheats;
using TphdCemuTrainer.Memory;
using TphdCemuTrainer.Research;
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
    private bool _inventoryOwnershipDiagnosticInProgress;
    private bool _equipmentDiagnosticInProgress;
    private bool _suppressInventoryVariantSafeguard;
    private bool _enforcingInventoryVariantSafeguard;
    private bool _holdInventoryValueAfterApply;
    private bool _allowEditingUninitializedInventory;
    private bool _allowUnsafeRawInventoryWrites;
    private bool _enableExperimentalInventoryCheckboxWrites;
    private bool _allowEditingUninitializedEquipment;
    private bool _userConfirmedPastIntroArc;
    private bool _allowOwnershipEditsBeforeIntroCompletion;
    private bool _hasPlayerData;
    private bool _inventoryInitialized;
    private bool _equipmentInitialized;
    private bool _isDarkMode;
    private byte? _researchSnapshotValue;
    private uint? _researchSnapshotOffset;
    private byte[]? _researchRangeSnapshotBytes;
    private uint _researchRangeSnapshotStart;
    private string _researchRangeSnapshotLabel = string.Empty;
    private byte? _candidatePreviousValue;
    private uint? _candidatePreviousOffset;
    private ResearchSnapshotViewModel? _snapshotA;
    private ResearchSnapshotViewModel? _snapshotB;
    private ResearchSnapshotDocument? _ownershipDiscoverySnapshotA;
    private ResearchSnapshotDocument? _ownershipDiscoverySnapshotB;
    private OwnershipDiscoveryExport? _lastOwnershipDiscoveryExport;
    private AobScanCache? _scanCache;
    private ProgressionState _progressionState = ProgressionStateService.CreateUnavailable();
    private string? _lastEnabledClawshotVariantId;

    private const string ClawshotInventoryItemId = "clawshot";
    private const string DoubleClawshotsInventoryItemId = "double-clawshots";

    private static readonly IReadOnlyList<OwnershipDiscoveryRangePreset> OwnershipDiscoveryRanges =
    [
        new("Inventory Slots", 0x258, 0x18),
        new("Equipment Ownership", 0x28D, 0x08),
        new("Candidate Ownership Region", 0x240, 0xA0),
        new("Collectibles", 0x2A1, 0x30)
    ];

    private static readonly JsonSerializerOptions ExportJsonOptions = new()
    {
        WriteIndented = true
    };

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
        BottleSlots = new ObservableCollection<BottleSlotViewModel>(
            BottleDefinitions.Slots.Select(slot => new BottleSlotViewModel(slot)));
        InventoryOwnershipItems = new ObservableCollection<InventoryOwnershipItemViewModel>(
            InventoryDefinitions.OwnershipItems.Select(item => new InventoryOwnershipItemViewModel(item)));
        foreach (var item in InventoryOwnershipItems)
        {
            item.PropertyChanged += InventoryOwnershipItem_PropertyChanged;
        }

        FixedInventoryItems = new ObservableCollection<InventoryFixedSlotViewModel>(
            InventoryDefinitions.FixedSlots.Select(slot => new InventoryFixedSlotViewModel(slot)));
        InventoryRemovalItems = [];
        InventoryMappingItems = new ObservableCollection<InventoryMappingSlotViewModel>(
            Enumerable.Range(0, InventoryDefinitions.SlotCount)
                .Select(slotIndex => new InventoryMappingSlotViewModel(slotIndex)));
        InventoryDiagnostics = [];
        InventoryOwnershipDiagnostics = [];
        InventoryRemovalDiagnostics = [];
        InventoryCheckboxTestingDiagnostics = [];
        BottleEditorDiagnostics = [];
        CollectiblesDiagnostics = [];

        EquipmentSlots = new ObservableCollection<EquipmentSlotViewModel>(
            EquipmentDefinitions.Slots.Select(slot => new EquipmentSlotViewModel(slot)));
        EquipmentFlags = new ObservableCollection<EquipmentFlagViewModel>(
            EquipmentDefinitions.OwnershipFlags.Select(flag => new EquipmentFlagViewModel(flag)));
        EquipmentDiagnostics = [];
        ResearchRangeRows = [];
        ResearchSnapshots = [];
        ResearchSnapshotCompareRows = [];
        ResearchDiscoveryReportLines = [];
        OwnershipDiscoveryRows = [];
        OwnershipDiscoveryReportLines = [];
        OwnershipCorrelationRows = [];

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
        IsDarkMode = LoadDarkModePreference();

        AobPatternText.Text = CheatCatalog.PlayerBaseAob;
        ApplyProgressionState(ProgressionStateService.CreateUnavailable());
        UpdateEquipmentEditGuard();
        RefreshSnapshotBrowser();

        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _refreshTimer.Tick += RefreshTimer_Tick;
    }

    private void InventoryOwnershipItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_suppressInventoryVariantSafeguard ||
            _enforcingInventoryVariantSafeguard ||
            e.PropertyName != nameof(InventoryOwnershipItemViewModel.IsOwnedDesired) ||
            sender is not InventoryOwnershipItemViewModel item ||
            !IsClawshotVariant(item) ||
            !item.IsOwnedDesired)
        {
            return;
        }

        _lastEnabledClawshotVariantId = item.Definition.Id;
        EnforceClawshotVariantSafeguard(item, "desired-state-change");
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private static string InventoryLogPath =>
        Path.Combine(AppContext.BaseDirectory, "logs", "inventory.log");

    private static string InventoryOwnershipLogPath =>
        Path.Combine(AppContext.BaseDirectory, "logs", "inventory-ownership.log");

    private static string InventoryRemovalLogPath =>
        Path.Combine(AppContext.BaseDirectory, "logs", "inventory-removal.log");

    private static string InventoryCheckboxTestingLogPath =>
        Path.Combine(AppContext.BaseDirectory, "logs", "inventory-checkbox-testing.log");

    private static string BottleEditorLogPath =>
        Path.Combine(AppContext.BaseDirectory, "logs", "bottle-editor.log");

    private static string EquipmentLogPath =>
        Path.Combine(AppContext.BaseDirectory, "logs", "equipment.log");

    private static string CollectiblesLogPath =>
        Path.Combine(AppContext.BaseDirectory, "logs", "collectibles.log");

    private static string ProgressionLogPath =>
        Path.Combine(AppContext.BaseDirectory, "logs", "progression.log");

    private static string ResearchLogPath =>
        Path.Combine(AppContext.BaseDirectory, "logs", "research.log");

    private static string CandidateTestingLogPath =>
        Path.Combine(AppContext.BaseDirectory, "logs", "candidate-testing.log");

    private static string ScanLogPath =>
        Path.Combine(AppContext.BaseDirectory, "logs", "scan.log");

    private static string UserSettingsDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TPHD-Cemu-Trainer");

    private static string ThemePreferencePath =>
        Path.Combine(UserSettingsDirectory, "theme.txt");

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

    public ObservableCollection<BottleSlotViewModel> BottleSlots { get; }

    public ObservableCollection<InventoryOwnershipItemViewModel> InventoryOwnershipItems { get; }

    public ObservableCollection<InventoryFixedSlotViewModel> FixedInventoryItems { get; }

    public ObservableCollection<InventoryRemovalItemViewModel> InventoryRemovalItems { get; }

    public ObservableCollection<InventoryMappingSlotViewModel> InventoryMappingItems { get; }

    public ObservableCollection<string> InventoryDiagnostics { get; }

    public ObservableCollection<string> InventoryOwnershipDiagnostics { get; }

    public ObservableCollection<string> InventoryRemovalDiagnostics { get; }

    public ObservableCollection<string> InventoryCheckboxTestingDiagnostics { get; }

    public ObservableCollection<string> BottleEditorDiagnostics { get; }

    public ObservableCollection<string> CollectiblesDiagnostics { get; }

    public ObservableCollection<EquipmentSlotViewModel> EquipmentSlots { get; }

    public ObservableCollection<EquipmentFlagViewModel> EquipmentFlags { get; }

    public IEnumerable<EquipmentFlagViewModel> SwordOwnershipFlags =>
        GetEquipmentFlags(EquipmentDefinitions.SwordFlagIds);

    public IEnumerable<EquipmentFlagViewModel> ShieldOwnershipFlags =>
        GetEquipmentFlags(EquipmentDefinitions.ShieldFlagIds);

    public IEnumerable<EquipmentFlagViewModel> ArmorOwnershipFlags =>
        GetEquipmentFlags(EquipmentDefinitions.ArmorFlagIds);

    public ObservableCollection<string> EquipmentDiagnostics { get; }

    public ObservableCollection<ResearchRangeRowViewModel> ResearchRangeRows { get; }

    public ObservableCollection<ResearchSnapshotViewModel> ResearchSnapshots { get; }

    public ObservableCollection<ResearchSnapshotCompareRowViewModel> ResearchSnapshotCompareRows { get; }

    public ObservableCollection<string> ResearchDiscoveryReportLines { get; }

    public ObservableCollection<OwnershipDiscoveryRowViewModel> OwnershipDiscoveryRows { get; }

    public ObservableCollection<string> OwnershipDiscoveryReportLines { get; }

    public ObservableCollection<OwnershipCorrelationRowViewModel> OwnershipCorrelationRows { get; }

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

    public bool AllowUnsafeRawInventoryWrites
    {
        get => _allowUnsafeRawInventoryWrites;
        set
        {
            if (_allowUnsafeRawInventoryWrites != value)
            {
                _allowUnsafeRawInventoryWrites = value;
                OnPropertyChanged();
                UpdateInventoryEditGuard();
            }
        }
    }

    public bool EnableExperimentalInventoryCheckboxWrites
    {
        get => _enableExperimentalInventoryCheckboxWrites;
        set
        {
            if (_enableExperimentalInventoryCheckboxWrites != value)
            {
                _enableExperimentalInventoryCheckboxWrites = value;
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

    public bool UserConfirmedPastIntroArc
    {
        get => _userConfirmedPastIntroArc;
        set
        {
            if (_userConfirmedPastIntroArc != value)
            {
                _userConfirmedPastIntroArc = value;
                OnPropertyChanged();
                UpdateProgressionStateDisplays();
                UpdateInventoryEditGuard();
                UpdateEquipmentEditGuard();
            }
        }
    }

    public bool AllowOwnershipEditsBeforeIntroCompletion
    {
        get => _allowOwnershipEditsBeforeIntroCompletion;
        set
        {
            if (_allowOwnershipEditsBeforeIntroCompletion != value)
            {
                _allowOwnershipEditsBeforeIntroCompletion = value;
                OnPropertyChanged();
                UpdateProgressionStateDisplays();
                UpdateInventoryEditGuard();
                UpdateEquipmentEditGuard();
            }
        }
    }

    public bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            if (_isDarkMode != value)
            {
                _isDarkMode = value;
                ApplyTheme(value);
                SaveDarkModePreference(value);
                OnPropertyChanged();
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

    private IEnumerable<EquipmentFlagViewModel> GetEquipmentFlags(IEnumerable<string> ids)
    {
        var idSet = ids.ToHashSet(StringComparer.Ordinal);
        return EquipmentFlags.Where(flag => idSet.Contains(flag.Definition.Id));
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

            if (!ProcessMemory.TryAttachToCemu(out var memory, out var attachError, out var attachDiagnostics) ||
                memory is null)
            {
                AppendScanDiagnostic(attachDiagnostics, null);
                SetStatus($"{attachError} Start Cemu, load Twilight Princess HD, then rescan.", StatusKind.Neutral);
                return;
            }

            SetStatus("Cemu found. Scanning for Twilight Princess HD player data...", StatusKind.Working);
            PidText.Text = memory.ProcessId.ToString(CultureInfo.InvariantCulture);

            var pattern = AobPattern.Parse(CheatCatalog.PlayerBaseAob);
            var scanner = new AobScanner();
            var progress = new Progress<AobScanProgress>(scanProgress =>
            {
                SetStatus(
                    $"Scanning region {scanProgress.CurrentRegion}/{scanProgress.TotalRegions} " +
                    $"({FormatAddress(scanProgress.Region.BaseAddress)}, {FormatByteCount(scanProgress.Region.Size)})...",
                    StatusKind.Working);
            });
            var scanCache = _scanCache is not null && _scanCache.ProcessId == memory.ProcessId
                ? _scanCache
                : null;
            var scanResult = await Task.Run(() =>
                scanner.FindFirstWithDiagnostics(
                    memory,
                    pattern,
                    scanCache,
                    progress,
                    CancellationToken.None));
            AppendScanDiagnostic(attachDiagnostics, scanResult);
            ScanDiagnosticsText.Text = FormatScanSummary(attachDiagnostics, scanResult);
            var foundAddress = scanResult.MatchAddress;

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
            if (scanResult.MatchRegion.HasValue)
            {
                _scanCache = new AobScanCache(
                    memory.ProcessId,
                    foundAddress.Value,
                    scanResult.MatchRegion.Value.BaseAddress,
                    scanResult.MatchRegion.Value.Size);
            }

            PlayerBaseText.Text = $"0x{foundAddress.Value:X}";
            DebugPlayerBaseText.Text = PlayerBaseText.Text;
            DetachButton.IsEnabled = true;
            _refreshTimer.Start();

            var progressionRead = RefreshProgressionState();
            if (!progressionRead)
            {
                SetStatus("Player data found, but initialization state could not be read. Load into gameplay and rescan.", StatusKind.Warning);
            }
            else if (!OwnershipEditsLikelyAccepted)
            {
                SetStatus("Player data found. This save appears to be before TPHD begins honoring ownership edits.", StatusKind.Warning);
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

    private void SetPoeSouls_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is TrainerValueViewModel value &&
            value.Definition.Id == CheatId.PoeSouls)
        {
            WritePoeSoulsRequestedValue();
        }
    }

    private void MaxPoeSouls_Click(object sender, RoutedEventArgs e)
    {
        PoeSouls.SetTargetValue(PoeSouls.EffectiveMaximum);
        WritePoeSoulsRequestedValue();
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

    private async void ApplyInventoryOwnership_Click(object sender, RoutedEventArgs e)
    {
        if (!CanWriteInventoryOwnership())
        {
            return;
        }

        var ownershipChanges = InventoryOwnershipItems
            .Where(item => item.Definition.CanWrite)
            .Select(item => new { Item = item, DesiredValue = item.IsOwnedDesired })
            .ToList();

        if (!ownershipChanges.Any(change => change.Item.IsDirty))
        {
            SetStatus("No inventory ownership changes to apply.", StatusKind.Neutral);
            return;
        }

        var successfulWrites = 0;
        foreach (var change in ownershipChanges)
        {
            if (await WriteInventoryOwnershipWithDiagnosticsAsync(
                    change.Item,
                    change.DesiredValue,
                    $"{change.Item.Name} ownership set to {(change.DesiredValue ? "Yes" : "No")}."))
            {
                successfulWrites++;
            }
        }

        RefreshInventorySlots(showStatus: false);
        AppendInventoryOwnershipStateDiagnostic("ownership-apply-refresh");
        SetStatus(
            $"Applied {successfulWrites} inventory ownership change(s). Close and reopen the in-game inventory menu if needed.",
            successfulWrites == ownershipChanges.Count ? StatusKind.Connected : StatusKind.Warning);
    }

    private async void ApplyInventorySlot_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is InventorySlotViewModel slot)
        {
            if (!CanWriteRawInventorySlot())
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
            if (!CanWriteRawInventorySlot())
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

    private async void RemoveInventoryVisibleItem_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is InventoryRemovalItemViewModel item)
        {
            await RemoveInventoryVisibleItemAsync(item);
        }
    }

    private async void RestoreInventoryVisibleItem_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is InventoryRemovalItemViewModel item)
        {
            await RestoreInventoryVisibleItemAsync(item);
        }
    }

    private async void RestoreAllInventoryRemovedItems_Click(object sender, RoutedEventArgs e)
    {
        var removedItems = InventoryRemovalItems
            .Where(item => item.CanRestore)
            .ToList();
        if (removedItems.Count == 0)
        {
            SetStatus("No inventory removal restore buffers are available.", StatusKind.Neutral);
            return;
        }

        var restoredCount = 0;
        foreach (var item in removedItems)
        {
            if (await RestoreInventoryVisibleItemAsync(item))
            {
                restoredCount++;
            }
        }

        SetStatus(
            $"Restored {restoredCount} of {removedItems.Count} removed inventory slot(s).",
            restoredCount == removedItems.Count ? StatusKind.Connected : StatusKind.Warning);
    }

    private void ClearInventoryRestoreBuffer_Click(object sender, RoutedEventArgs e)
    {
        foreach (var item in InventoryRemovalItems)
        {
            item.ClearPrevious();
            item.Status = item.CurrentItemId == InventoryDefinitions.EmptyItemId
                ? "Restore buffer cleared; slot currently empty."
                : "Restore buffer cleared.";
        }

        RefreshInventorySlots(showStatus: false);
        SetStatus("Inventory removal restore buffers cleared for this session.", StatusKind.Neutral);
    }

    private async void ApplyInventoryCheckboxTesting_Click(object sender, RoutedEventArgs e)
    {
        if (!EnableExperimentalInventoryCheckboxWrites)
        {
            SetStatus("Enable the fixed-slot inventory editor before applying fixed-slot changes.", StatusKind.Warning);
            return;
        }

        EnforceClawshotVariantSafeguard(GetPreferredClawshotVariantForApply(), "apply-backstop");

        var changedItems = InventoryOwnershipItems
            .Where(item => item.IsDirty && item.CanExperimentalCheckboxWrite)
            .ToList();
        if (changedItems.Count == 0)
        {
            SetStatus("No supported fixed-slot inventory changes to apply.", StatusKind.Neutral);
            return;
        }

        var completed = 0;
        foreach (var item in changedItems)
        {
            if (await WriteInventoryCheckboxTestAsync(item))
            {
                completed++;
            }
        }

        SetStatus(
            $"Applied {completed} of {changedItems.Count} fixed-slot inventory change(s).",
            completed == changedItems.Count ? StatusKind.Connected : StatusKind.Warning);
    }

    private InventoryOwnershipItemViewModel? GetPreferredClawshotVariantForApply()
    {
        if (_lastEnabledClawshotVariantId is not null)
        {
            var lastEnabled = InventoryOwnershipItems.FirstOrDefault(
                item => item.Definition.Id == _lastEnabledClawshotVariantId);
            if (lastEnabled is not null)
            {
                return lastEnabled;
            }
        }

        var clawshot = GetInventoryOwnershipItem(ClawshotInventoryItemId);
        var doubleClawshots = GetInventoryOwnershipItem(DoubleClawshotsInventoryItemId);
        if (doubleClawshots?.IsOwnedDesired == true && doubleClawshots.IsDirty)
        {
            return doubleClawshots;
        }

        if (clawshot?.IsOwnedDesired == true && clawshot.IsDirty)
        {
            return clawshot;
        }

        return doubleClawshots?.IsOwnedDesired == true
            ? doubleClawshots
            : clawshot;
    }

    private bool EnforceClawshotVariantSafeguard(
        InventoryOwnershipItemViewModel? preferredItem,
        string reason)
    {
        if (_suppressInventoryVariantSafeguard || _enforcingInventoryVariantSafeguard)
        {
            return false;
        }

        var clawshot = GetInventoryOwnershipItem(ClawshotInventoryItemId);
        var doubleClawshots = GetInventoryOwnershipItem(DoubleClawshotsInventoryItemId);
        if (clawshot is null ||
            doubleClawshots is null ||
            !clawshot.IsOwnedDesired ||
            !doubleClawshots.IsOwnedDesired)
        {
            return false;
        }

        var preferred = preferredItem is not null && IsClawshotVariant(preferredItem)
            ? preferredItem
            : GetPreferredClawshotVariantForApply();
        preferred ??= doubleClawshots;
        var disabled = ReferenceEquals(preferred, clawshot)
            ? doubleClawshots
            : clawshot;

        _enforcingInventoryVariantSafeguard = true;
        try
        {
            disabled.IsOwnedDesired = false;
        }
        finally
        {
            _enforcingInventoryVariantSafeguard = false;
        }

        preferred.ExperimentalStatus = $"Kept by Clawshot safeguard; {disabled.Name} disabled.";
        disabled.ExperimentalStatus = $"Disabled by Clawshot safeguard; {preferred.Name} kept.";
        AppendInventoryCheckboxSafeguardDiagnostic(reason, preferred, disabled);
        SetStatus(
            $"Clawshot safeguard kept {preferred.Name} and disabled {disabled.Name}.",
            StatusKind.Warning);
        return true;
    }

    private InventoryOwnershipItemViewModel? GetInventoryOwnershipItem(string id)
    {
        return InventoryOwnershipItems.FirstOrDefault(item => item.Definition.Id == id);
    }

    private static bool IsClawshotVariant(InventoryOwnershipItemViewModel item)
    {
        return item.Definition.Id is ClawshotInventoryItemId or DoubleClawshotsInventoryItemId;
    }

    private async void ApplyBottleSlot_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is BottleSlotViewModel slot)
        {
            await WriteBottleSlotWithDiagnosticsAsync(
                slot,
                slot.SelectedContent.ItemId,
                "apply",
                $"Bottle slot {slot.BottleSlotNumber} set to {slot.SelectedContent.Name}.");
        }
    }

    private async void ClearBottleSlot_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is BottleSlotViewModel slot)
        {
            await WriteBottleSlotWithDiagnosticsAsync(
                slot,
                InventoryDefinitions.EmptyItemId,
                "clear",
                $"Bottle slot {slot.BottleSlotNumber} set to Nothing / No Bottle.");
        }
    }

    private async void RestoreBottleSlot_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is BottleSlotViewModel slot)
        {
            if (!slot.PreviousValue.HasValue)
            {
                SetStatus($"Bottle slot {slot.BottleSlotNumber} has no previous value captured this session.", StatusKind.Neutral);
                return;
            }

            var previousValue = slot.PreviousValue.Value;
            if (await WriteBottleSlotWithDiagnosticsAsync(
                    slot,
                    previousValue,
                    "restore",
                    $"Bottle slot {slot.BottleSlotNumber} restored to {BottleDefinitions.GetBottleContentName(previousValue)}."))
            {
                slot.ClearPrevious();
            }
        }
    }

    private void ExportInventoryMapping_Click(object sender, RoutedEventArgs e)
    {
        ExportInventoryMapping();
    }

    private void RefreshEquipmentButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshEquipment(showStatus: true);
    }

    private async void ApplyEquipmentOwnership_Click(object sender, RoutedEventArgs e)
    {
        if (!CanWriteEquipmentOwnership())
        {
            return;
        }

        var ownershipChanges = EquipmentFlags
            .Select(flag => new { Flag = flag, DesiredValue = flag.IsOwnedDesired })
            .ToList();

        if (!ownershipChanges.Any(change => change.Flag.IsDirty))
        {
            SetStatus("No equipment ownership changes to apply.", StatusKind.Neutral);
            return;
        }

        var successfulWrites = 0;
        foreach (var change in ownershipChanges)
        {
            if (await WriteEquipmentFlagWithDiagnosticsAsync(
                    change.Flag,
                    change.DesiredValue,
                    $"{change.Flag.Name} ownership set to {(change.DesiredValue ? "Yes" : "No")}."))
            {
                successfulWrites++;
            }
        }

        RefreshEquipment(showStatus: false);
        AppendEquipmentStateDiagnostic("ownership-apply-refresh");
        SetStatus(
            $"Applied {successfulWrites} equipment ownership change(s). Equip items through the in-game menu.",
            successfulWrites == ownershipChanges.Count ? StatusKind.Connected : StatusKind.Warning);
    }

    private void ResearchReadByte_Click(object sender, RoutedEventArgs e)
    {
        RefreshResearchByte(showStatus: true);
    }

    private void ResearchCaptureSnapshot_Click(object sender, RoutedEventArgs e)
    {
        if (TryReadResearchByte(out var offset, out var value, out _))
        {
            _researchSnapshotOffset = offset;
            _researchSnapshotValue = value;
            ResearchSnapshotByteText.Text = FormatResearchByte(value);
            ResearchCompareText.Text = "Snapshot captured.";
            SetStatus($"Research snapshot captured at _playerbase+0x{offset:X}.", StatusKind.Connected);
        }
    }

    private void ResearchCompareSnapshot_Click(object sender, RoutedEventArgs e)
    {
        if (!_researchSnapshotValue.HasValue || !_researchSnapshotOffset.HasValue)
        {
            ResearchCompareText.Text = "Capture a snapshot first.";
            SetStatus("Capture a research snapshot before comparing.", StatusKind.Neutral);
            return;
        }

        if (!TryReadResearchByte(out var offset, out var currentValue, out _))
        {
            return;
        }

        var snapshotValue = _researchSnapshotValue.Value;
        var offsetNote = offset == _researchSnapshotOffset.Value
            ? $"_playerbase+0x{offset:X}"
            : $"current _playerbase+0x{offset:X}; snapshot _playerbase+0x{_researchSnapshotOffset.Value:X}";

        ResearchCompareText.Text = currentValue == snapshotValue
            ? $"Unchanged at {offsetNote}: {FormatResearchByte(currentValue)}"
            : $"Changed at {offsetNote}: {FormatResearchByte(snapshotValue)} -> {FormatResearchByte(currentValue)}";

        SetStatus("Research byte compared.", StatusKind.Connected);
    }

    private void CandidatePreset_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is string offset)
        {
            CandidateOffsetText.Text = offset;
            CandidateStatusText.Text = $"Loaded candidate preset {offset}.";
        }
    }

    private void CandidateReadCurrent_Click(object sender, RoutedEventArgs e)
    {
        if (!TryReadCandidateByte(out var offset, out var value, out var absoluteAddress))
        {
            return;
        }

        _candidatePreviousOffset = offset;
        _candidatePreviousValue = value;
        CandidatePreviousByteText.Text = FormatResearchByte(value);
        CandidateStatusText.Text = $"Read _playerbase+0x{offset:X}: {FormatResearchByte(value)}. Restore baseline captured.";
        SetStatus($"Read candidate byte at _playerbase+0x{offset:X}.", StatusKind.Connected);
        AppendCandidateTestingLog(
            "read",
            offset,
            absoluteAddress,
            previousValue: null,
            requestedValue: null,
            readbackValue: value,
            "read-current");
    }

    private void CandidateWriteValue_Click(object sender, RoutedEventArgs e)
    {
        if (!TryReadCandidateByte(out var offset, out var previousValue, out var absoluteAddress))
        {
            return;
        }

        if (!TryParseCandidateValue(CandidateValueText.Text, out var requestedValue, out var parseError))
        {
            CandidateStatusText.Text = parseError;
            SetStatus(parseError, StatusKind.Warning);
            return;
        }

        var memory = _memory;
        if (memory is null)
        {
            CandidateStatusText.Text = "Not attached.";
            SetStatus("Not attached. Attach to Cemu and rescan before testing candidate flags.", StatusKind.Neutral);
            return;
        }

        _candidatePreviousOffset = offset;
        _candidatePreviousValue = previousValue;
        CandidatePreviousByteText.Text = FormatResearchByte(previousValue);

        if (!memory.TryWriteBytes(absoluteAddress, [requestedValue], out var writeError))
        {
            CandidateStatusText.Text = $"Write failed: {writeError}";
            SetStatus("Candidate test write failed.", StatusKind.Warning);
            AppendCandidateTestingLog(
                "write",
                offset,
                absoluteAddress,
                previousValue,
                requestedValue,
                readbackValue: null,
                $"write-failed: {writeError}");
            return;
        }

        if (!TryReadCandidateByteAt(offset, absoluteAddress, out var readbackValue, out var readbackError))
        {
            CandidateStatusText.Text = $"Write issued, but verification failed: {readbackError}";
            SetStatus("Candidate test verification failed.", StatusKind.Warning);
            AppendCandidateTestingLog(
                "write",
                offset,
                absoluteAddress,
                previousValue,
                requestedValue,
                readbackValue: null,
                $"verification-read-failed: {readbackError}");
            return;
        }

        var verified = readbackValue == requestedValue;
        CandidateCurrentByteText.Text = FormatResearchByte(readbackValue);
        CandidateStatusText.Text = verified
            ? $"Write verified at _playerbase+0x{offset:X}: {FormatResearchByte(readbackValue)}."
            : $"Write failed: expected {FormatResearchByte(requestedValue)} but read {FormatResearchByte(readbackValue)}.";
        SetStatus(
            verified ? "Candidate test write verified." : "Candidate test write readback mismatch.",
            verified ? StatusKind.Connected : StatusKind.Warning);
        AppendCandidateTestingLog(
            "write",
            offset,
            absoluteAddress,
            previousValue,
            requestedValue,
            readbackValue,
            verified ? "verified" : "readback-mismatch");
    }

    private void CandidateRestorePrevious_Click(object sender, RoutedEventArgs e)
    {
        if (!_candidatePreviousOffset.HasValue || !_candidatePreviousValue.HasValue)
        {
            CandidateStatusText.Text = "No previous value captured. Click Read Current or Write Value first.";
            SetStatus("No candidate baseline captured.", StatusKind.Neutral);
            return;
        }

        var memory = _memory;
        if (memory is null || !_playerBaseAddress.HasValue)
        {
            CandidateStatusText.Text = "Not attached.";
            SetStatus("Not attached. Attach to Cemu and rescan before restoring candidate flags.", StatusKind.Neutral);
            return;
        }

        var offset = _candidatePreviousOffset.Value;
        var previousValue = _candidatePreviousValue.Value;
        var absoluteAddress = _playerBaseAddress.Value + offset;
        CandidateOffsetText.Text = $"0x{offset:X}";

        if (!memory.TryWriteBytes(absoluteAddress, [previousValue], out var writeError))
        {
            CandidateStatusText.Text = $"Restore failed: {writeError}";
            SetStatus("Candidate restore failed.", StatusKind.Warning);
            AppendCandidateTestingLog(
                "restore",
                offset,
                absoluteAddress,
                previousValue,
                previousValue,
                readbackValue: null,
                $"restore-failed: {writeError}");
            return;
        }

        if (!TryReadCandidateByteAt(offset, absoluteAddress, out var readbackValue, out var readbackError))
        {
            CandidateStatusText.Text = $"Restore issued, but verification failed: {readbackError}";
            SetStatus("Candidate restore verification failed.", StatusKind.Warning);
            AppendCandidateTestingLog(
                "restore",
                offset,
                absoluteAddress,
                previousValue,
                previousValue,
                readbackValue: null,
                $"restore-verification-read-failed: {readbackError}");
            return;
        }

        var verified = readbackValue == previousValue;
        CandidateCurrentByteText.Text = FormatResearchByte(readbackValue);
        CandidateStatusText.Text = verified
            ? $"Restored _playerbase+0x{offset:X} to {FormatResearchByte(readbackValue)}."
            : $"Restore mismatch: expected {FormatResearchByte(previousValue)} but read {FormatResearchByte(readbackValue)}.";
        SetStatus(
            verified ? "Candidate previous value restored." : "Candidate restore readback mismatch.",
            verified ? StatusKind.Connected : StatusKind.Warning);
        AppendCandidateTestingLog(
            "restore",
            offset,
            absoluteAddress,
            previousValue,
            previousValue,
            readbackValue,
            verified ? "verified" : "restore-readback-mismatch");
    }

    private void ResearchPresetInventorySlots_Click(object sender, RoutedEventArgs e)
    {
        SetResearchRangeInputs("0x258", "0x18", "Inventory Slots");
    }

    private void ResearchPresetEquipmentOwnership_Click(object sender, RoutedEventArgs e)
    {
        SetResearchRangeInputs("0x28D", "0x08", "Equipment/Ownership");
    }

    private void ResearchPresetEquippedGear_Click(object sender, RoutedEventArgs e)
    {
        SetResearchRangeInputs("0x1D1", "0x03", "Equipped Gear");
    }

    private void ResearchCaptureRangeSnapshot_Click(object sender, RoutedEventArgs e)
    {
        if (!TryReadResearchRange(out var startOffset, out var bytes, out _))
        {
            return;
        }

        _researchRangeSnapshotStart = startOffset;
        _researchRangeSnapshotBytes = bytes;
        _researchRangeSnapshotLabel = string.IsNullOrWhiteSpace(ResearchRangeLabelText.Text)
            ? "Snapshot"
            : ResearchRangeLabelText.Text.Trim();

        ResearchRangeRows.Clear();
        for (var index = 0; index < bytes.Length; index++)
        {
            var offset = startOffset + (uint)index;
            var value = bytes[index];
            ResearchRangeRows.Add(new ResearchRangeRowViewModel(
                offset,
                value,
                DecodeKnownResearchByte(value)));
        }

        ResearchRangeStatusText.Text =
            $"Captured \"{_researchRangeSnapshotLabel}\" at _playerbase+0x{startOffset:X}, length 0x{bytes.Length:X}.";
        SetStatus("Research range snapshot captured.", StatusKind.Connected);
    }

    private void ResearchCompareRangeSnapshot_Click(object sender, RoutedEventArgs e)
    {
        if (_researchRangeSnapshotBytes is null)
        {
            ResearchRangeStatusText.Text = "Capture a range snapshot first.";
            SetStatus("Capture a research range snapshot before comparing.", StatusKind.Neutral);
            return;
        }

        if (!TryReadResearchRange(out var currentStartOffset, out var currentBytes, out _))
        {
            return;
        }

        if (currentStartOffset != _researchRangeSnapshotStart ||
            currentBytes.Length != _researchRangeSnapshotBytes.Length)
        {
            ResearchRangeStatusText.Text =
                $"Current range must match snapshot range: start 0x{_researchRangeSnapshotStart:X}, length 0x{_researchRangeSnapshotBytes.Length:X}.";
            SetStatus("Research range does not match captured snapshot.", StatusKind.Warning);
            return;
        }

        ResearchRangeRows.Clear();
        var changedCount = 0;
        for (var index = 0; index < currentBytes.Length; index++)
        {
            var offset = _researchRangeSnapshotStart + (uint)index;
            var beforeValue = _researchRangeSnapshotBytes[index];
            var currentValue = currentBytes[index];
            var row = new ResearchRangeRowViewModel(
                offset,
                beforeValue,
                DecodeKnownResearchByte(beforeValue));

            row.SetCurrent(currentValue, DecodeKnownResearchByte(currentValue));
            if (row.IsChanged)
            {
                changedCount++;
            }

            ResearchRangeRows.Add(row);
        }

        ResearchRangeStatusText.Text =
            $"Compared \"{_researchRangeSnapshotLabel}\". Changed bytes: {changedCount}.";
        AppendResearchRangeComparisonLog(currentBytes, changedCount);
        SetStatus($"Research range compared. Changed bytes: {changedCount}.", StatusKind.Connected);
    }

    private void ResearchSaveSnapshot_Click(object sender, RoutedEventArgs e)
    {
        if (!TryCreateSnapshotDocument(out var snapshot))
        {
            return;
        }

        var path = ResearchSnapshotStore.SaveSnapshot(snapshot);
        RefreshSnapshotBrowser();
        ResearchSnapshotLibraryStatusText.Text = $"Saved snapshot: {Path.GetFileName(path)}";
        SetStatus($"Saved research snapshot \"{snapshot.Name}\".", StatusKind.Connected);
    }

    private void ResearchRefreshSnapshots_Click(object sender, RoutedEventArgs e)
    {
        RefreshSnapshotBrowser();
        ResearchSnapshotLibraryStatusText.Text = "Snapshot library refreshed.";
    }

    private void ResearchLoadSnapshotA_Click(object sender, RoutedEventArgs e)
    {
        if (ResearchSnapshotListBox.SelectedItem is ResearchSnapshotViewModel snapshot)
        {
            _snapshotA = snapshot;
            ResearchSnapshotAText.Text = $"{snapshot.Name} ({snapshot.TimestampText})";
            ResearchSnapshotLibraryStatusText.Text = $"Loaded Snapshot A: {snapshot.Name}";
        }
    }

    private void ResearchLoadSnapshotB_Click(object sender, RoutedEventArgs e)
    {
        if (ResearchSnapshotListBox.SelectedItem is ResearchSnapshotViewModel snapshot)
        {
            _snapshotB = snapshot;
            ResearchSnapshotBText.Text = $"{snapshot.Name} ({snapshot.TimestampText})";
            ResearchSnapshotLibraryStatusText.Text = $"Loaded Snapshot B: {snapshot.Name}";
        }
    }

    private void ResearchDeleteSnapshot_Click(object sender, RoutedEventArgs e)
    {
        if (ResearchSnapshotListBox.SelectedItem is not ResearchSnapshotViewModel snapshot)
        {
            ResearchSnapshotLibraryStatusText.Text = "Select a snapshot to delete.";
            return;
        }

        try
        {
            File.Delete(snapshot.FilePath);
            if (ReferenceEquals(_snapshotA, snapshot))
            {
                _snapshotA = null;
                ResearchSnapshotAText.Text = "-";
            }

            if (ReferenceEquals(_snapshotB, snapshot))
            {
                _snapshotB = null;
                ResearchSnapshotBText.Text = "-";
            }

            RefreshSnapshotBrowser();
            ResearchSnapshotLibraryStatusText.Text = $"Deleted snapshot: {snapshot.Name}";
        }
        catch (Exception ex)
        {
            ResearchSnapshotLibraryStatusText.Text = $"Delete failed: {ex.Message}";
            SetStatus("Research snapshot delete failed.", StatusKind.Warning);
        }
    }

    private void ResearchCompareSavedSnapshots_Click(object sender, RoutedEventArgs e)
    {
        if (_snapshotA is null || _snapshotB is null)
        {
            ResearchSnapshotCompareStatusText.Text = "Load Snapshot A and Snapshot B before comparing.";
            SetStatus("Load two research snapshots before comparing.", StatusKind.Neutral);
            return;
        }

        CompareSavedSnapshots(_snapshotA, _snapshotB);
    }

    private void OwnershipDiscoveryCaptureA_Click(object sender, RoutedEventArgs e)
    {
        if (!TryCaptureOwnershipDiscoverySnapshot(OwnershipDiscoverySnapshotANameText.Text, out var snapshot))
        {
            return;
        }

        _ownershipDiscoverySnapshotA = snapshot;
        _lastOwnershipDiscoveryExport = null;
        OwnershipDiscoverySnapshotAText.Text = FormatOwnershipDiscoverySnapshotLabel(snapshot);
        OwnershipDiscoveryStatusText.Text = $"Captured Save A: {snapshot.Name}.";
        OwnershipDiscoveryRows.Clear();
        OwnershipDiscoveryReportLines.Clear();
        SetStatus("Ownership Discovery Save A captured.", StatusKind.Connected);
    }

    private void OwnershipDiscoveryCaptureB_Click(object sender, RoutedEventArgs e)
    {
        if (!TryCaptureOwnershipDiscoverySnapshot(OwnershipDiscoverySnapshotBNameText.Text, out var snapshot))
        {
            return;
        }

        _ownershipDiscoverySnapshotB = snapshot;
        _lastOwnershipDiscoveryExport = null;
        OwnershipDiscoverySnapshotBText.Text = FormatOwnershipDiscoverySnapshotLabel(snapshot);
        OwnershipDiscoveryStatusText.Text = $"Captured Save B: {snapshot.Name}.";
        OwnershipDiscoveryRows.Clear();
        OwnershipDiscoveryReportLines.Clear();
        SetStatus("Ownership Discovery Save B captured.", StatusKind.Connected);
    }

    private void OwnershipDiscoveryCompare_Click(object sender, RoutedEventArgs e)
    {
        if (_ownershipDiscoverySnapshotA is null || _ownershipDiscoverySnapshotB is null)
        {
            OwnershipDiscoveryStatusText.Text = "Capture Save A and Save B before comparing.";
            SetStatus("Capture both Ownership Discovery snapshots before comparing.", StatusKind.Neutral);
            return;
        }

        CompareOwnershipDiscoverySnapshots(_ownershipDiscoverySnapshotA, _ownershipDiscoverySnapshotB);
    }

    private void OwnershipDiscoveryExport_Click(object sender, RoutedEventArgs e)
    {
        if (_lastOwnershipDiscoveryExport is null)
        {
            OwnershipDiscoveryStatusText.Text = "Compare Save A and Save B before exporting.";
            SetStatus("Compare Ownership Discovery snapshots before exporting.", StatusKind.Neutral);
            return;
        }

        try
        {
            var exported = ResearchSnapshotStore.ExportOwnershipDiscovery(_lastOwnershipDiscoveryExport);
            OwnershipDiscoveryStatusText.Text =
                $"Exported {Path.GetFileName(exported.JsonPath)} and {Path.GetFileName(exported.CsvPath)}.";
            SetStatus("Ownership Discovery report exported.", StatusKind.Connected);
        }
        catch (Exception ex)
        {
            OwnershipDiscoveryStatusText.Text = $"Export failed: {ex.Message}";
            SetStatus("Ownership Discovery export failed.", StatusKind.Warning);
        }
    }

    private void OwnershipCorrelationLoadSelected_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Ownership Discovery JSON exports",
            Filter = "Ownership Discovery JSON (*.json)|*.json",
            Multiselect = true,
            InitialDirectory = Directory.Exists(ResearchSnapshotStore.ExportDirectory)
                ? ResearchSnapshotStore.ExportDirectory
                : AppContext.BaseDirectory
        };

        if (dialog.ShowDialog(this) != true)
        {
            OwnershipCorrelationStatusText.Text = "No ownership discovery exports selected.";
            return;
        }

        var exports = ResearchSnapshotStore.LoadOwnershipDiscoveryExports(dialog.FileNames);
        BuildOwnershipCorrelationReport(exports);
    }

    private void OwnershipCorrelationAnalyzeFolder_Click(object sender, RoutedEventArgs e)
    {
        var exports = ResearchSnapshotStore.LoadOwnershipDiscoveryExports();
        BuildOwnershipCorrelationReport(exports);
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

            if (!_inventoryDiagnosticInProgress &&
                !_inventoryOwnershipDiagnosticInProgress &&
                !RefreshInventorySlots(showStatus: false))
            {
                return;
            }

            if (!_equipmentDiagnosticInProgress && !RefreshEquipment(showStatus: false))
            {
                return;
            }

            UpdateDerivedDisplays();

            if (ResearchAutoRefreshCheckBox.IsChecked == true)
            {
                RefreshResearchByte(showStatus: false);
            }

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

    private OwnershipEditAcceptance EffectiveOwnershipEditAcceptance
    {
        get
        {
            if (!HasPlayerData)
            {
                return OwnershipEditAcceptance.Unknown;
            }

            return UserConfirmedPastIntroArc
                ? OwnershipEditAcceptance.LikelyYes
                : _progressionState.GameAcceptsOwnershipEdits;
        }
    }

    private string EffectiveOwnershipEditAcceptanceText => EffectiveOwnershipEditAcceptance switch
    {
        OwnershipEditAcceptance.LikelyYes => "Likely Yes",
        OwnershipEditAcceptance.LikelyNo => "Likely No",
        _ => "Unknown"
    };

    private bool OwnershipEditsLikelyAccepted => EffectiveOwnershipEditAcceptance == OwnershipEditAcceptance.LikelyYes;

    private string EffectiveOwnershipEditDetectionReason
    {
        get
        {
            if (!HasPlayerData)
            {
                return "Player data is not available.";
            }

            return UserConfirmedPastIntroArc
                ? "User override: marked as past the Ordon Village intro arc."
                : _progressionState.OwnershipEditDetectionReason;
        }
    }

    private void UpdateProgressionStateDisplays()
    {
        PlayerDataStateText.Text = _progressionState.PlayerDataStatus;
        InventoryStateText.Text = HasPlayerData ? _progressionState.InventoryStatus : "-";
        EquipmentStateText.Text = HasPlayerData ? _progressionState.EquipmentStatus : "-";
        OwnershipEditsStateText.Text = EffectiveOwnershipEditAcceptanceText;

        ProgressionPlayerBaseText.Text = _progressionState.PlayerBaseText;
        ProgressionPlayerDataText.Text = _progressionState.PlayerDataStatus;
        ProgressionMemoryInitializedText.Text = HasPlayerData
            ? _progressionState.MemoryInitializedStatus
            : "-";
        ProgressionOwnershipAcceptanceText.Text = EffectiveOwnershipEditAcceptanceText;
        ProgressionOwnershipReasonText.Text = EffectiveOwnershipEditDetectionReason;
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

        foreach (var bottleSlot in BottleSlots)
        {
            bottleSlot.SetCurrentValue(rawBytes[bottleSlot.InventorySlotIndex]);
        }

        foreach (var item in InventoryMappingItems)
        {
            item.SetCurrentItem(rawBytes[item.SlotIndex]);
        }

        UpdateInventoryRemovalItems(rawBytes);

        if (!RefreshInventoryOwnership(rawBytes, preserveDirty: true))
        {
            return false;
        }

        SetInventoryInitialized(!allSlotsEmpty, rawBytes);

        if (showStatus)
        {
            AppendInventoryStateDiagnostic(allSlotsEmpty
                ? "manual-refresh-empty-or-uninitialized"
                : "manual-refresh-read-only-slots");
            AppendInventoryOwnershipStateDiagnostic(allSlotsEmpty
                ? "manual-refresh-empty-or-uninitialized"
                : "manual-refresh");
        }

        if (allSlotsEmpty && showStatus)
        {
            SetStatus("Inventory has not been initialized by the game yet.", StatusKind.Warning);
        }
        else if (!allSlotsEmpty && showStatus)
        {
            SetStatus("Inventory state refreshed. Raw slots are read-only outside unsafe research mode.", StatusKind.Connected);
        }

        if (showStatus)
        {
            UpdateInventoryEditGuard();
        }

        return true;
    }

    private bool RefreshInventoryOwnership(IReadOnlyList<byte> rawBytes, bool preserveDirty)
    {
        var previousSuppressInventoryVariantSafeguard = _suppressInventoryVariantSafeguard;
        _suppressInventoryVariantSafeguard = true;
        try
        {
            foreach (var item in InventoryOwnershipItems)
            {
                if (!item.Definition.CanWrite)
                {
                    item.SetDetectedFromVisibleSlots(rawBytes, preserveDirty);
                    continue;
                }

                if (_memory is null || !_playerBaseAddress.HasValue)
                {
                    item.MarkNotRead();
                    continue;
                }

                if (!InventoryMemoryService.TryReadOwnershipFlag(
                        _memory,
                        _playerBaseAddress.Value,
                        item.Definition,
                        out var isOwned,
                        out var backingValue,
                        out _))
                {
                    MarkMemoryUnavailable();
                    return false;
                }

                item.SetDetectedFlag(isOwned, backingValue, preserveDirty);
            }

            return true;
        }
        finally
        {
            _suppressInventoryVariantSafeguard = previousSuppressInventoryVariantSafeguard;
        }
    }

    private void UpdateInventoryRemovalItems(IReadOnlyList<byte> rawBytes)
    {
        var knownRowsBySlot = InventoryRemovalItems.ToDictionary(item => item.SlotIndex);

        for (var slotIndex = 0; slotIndex < rawBytes.Count; slotIndex++)
        {
            var itemId = rawBytes[slotIndex];
            if (!knownRowsBySlot.TryGetValue(slotIndex, out var row) &&
                itemId == InventoryDefinitions.EmptyItemId)
            {
                continue;
            }

            if (row is null)
            {
                row = new InventoryRemovalItemViewModel(slotIndex);
                InventoryRemovalItems.Add(row);
                knownRowsBySlot[slotIndex] = row;
            }

            row.SetCurrentItem(itemId);
        }

        for (var index = InventoryRemovalItems.Count - 1; index >= 0; index--)
        {
            var row = InventoryRemovalItems[index];
            if (row.SlotIndex >= rawBytes.Count ||
                rawBytes[row.SlotIndex] == InventoryDefinitions.EmptyItemId && !row.CanRestore)
            {
                InventoryRemovalItems.RemoveAt(index);
            }
        }
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

    private bool CanWriteRawInventorySlot()
    {
        if (!AllowUnsafeRawInventoryWrites)
        {
            SetStatus("Enable unsafe raw inventory writes before writing raw inventory bytes.", StatusKind.Warning);
            return false;
        }

        return CanWriteInventorySlot();
    }

    private bool CanWriteInventoryRemoval()
    {
        if (_memory is null || !_playerBaseAddress.HasValue)
        {
            SetStatus("Not attached. Attach to Cemu and rescan before testing inventory removal.", StatusKind.Neutral);
            return false;
        }

        if (!InventoryInitialized && !AllowEditingUninitializedInventory)
        {
            SetStatus("Inventory has not been initialized by the game yet. Removal testing needs a detected visible item.", StatusKind.Warning);
            return false;
        }

        return true;
    }

    private bool CanWriteInventoryOwnership()
    {
        if (_memory is null || !_playerBaseAddress.HasValue)
        {
            SetStatus("Not attached. Attach to Cemu and rescan before editing inventory ownership.", StatusKind.Neutral);
            return false;
        }

        if (!InventoryOwnershipItems.Any(item => item.Definition.CanWrite))
        {
            SetStatus("No mapped inventory ownership flags are available yet. Use Research tools to identify flags.", StatusKind.Neutral);
            return false;
        }

        if (!InventoryInitialized && !AllowEditingUninitializedInventory)
        {
            SetStatus("Inventory has not been initialized by the game yet. Enable the advanced override to edit mapped ownership flags.", StatusKind.Warning);
            return false;
        }

        if (!OwnershipEditsLikelyAccepted && !AllowOwnershipEditsBeforeIntroCompletion)
        {
            SetStatus("This save appears to be before TPHD begins honoring ownership edits. Progress past the Ordon Village intro arc and rescan.", StatusKind.Warning);
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
            _progressionState = RefreshOwnershipEditAcceptance(_progressionState);
        }

        UpdateProgressionStateDisplays();
        UpdateInventoryEditGuard();
    }

    private void UpdateInventoryEditGuard()
    {
        var canRawEdit =
            AllowUnsafeRawInventoryWrites &&
            HasPlayerData &&
            (InventoryInitialized || AllowEditingUninitializedInventory);
        var ownershipAcceptanceAllowsEditing =
            OwnershipEditsLikelyAccepted || AllowOwnershipEditsBeforeIntroCompletion;
        var canOwnershipEdit =
            HasPlayerData &&
            (InventoryInitialized || AllowEditingUninitializedInventory) &&
            ownershipAcceptanceAllowsEditing;
        foreach (var slot in InventorySlots)
        {
            slot.CanEdit = canRawEdit;
        }

        foreach (var item in FixedInventoryItems)
        {
            item.CanEdit = false;
        }

        foreach (var bottleSlot in BottleSlots)
        {
            bottleSlot.CanEdit = HasPlayerData;
        }

        foreach (var item in InventoryOwnershipItems)
        {
            item.CanEdit =
                EnableExperimentalInventoryCheckboxWrites
                    ? item.CanExperimentalCheckboxWrite
                    : item.Definition.CanWrite && canOwnershipEdit;
        }

        ApplyInventoryOwnershipButton.IsEnabled =
            canOwnershipEdit && InventoryOwnershipItems.Any(item => item.Definition.CanWrite);
        ApplyInventoryCheckboxTestingButton.IsEnabled =
            EnableExperimentalInventoryCheckboxWrites &&
            HasPlayerData &&
            InventoryOwnershipItems.Any(item => item.CanExperimentalCheckboxWrite);

        InventoryInitializationWarningText.Visibility = HasPlayerData && !InventoryInitialized
            ? Visibility.Visible
            : Visibility.Collapsed;
        InventoryOwnershipAcceptanceWarningText.Visibility =
            HasPlayerData &&
            EffectiveOwnershipEditAcceptance == OwnershipEditAcceptance.LikelyNo &&
            !AllowOwnershipEditsBeforeIntroCompletion
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

            flag.SetDetectedValue(isOwned, backingValue, preserveDirty: true);
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
            AppendEquipmentStateDiagnostic("manual-refresh");
            SetStatus(
                !equipmentInitialized
                    ? "Equipment has not been initialized by the game yet."
                    : "Equipment data refreshed.",
                !equipmentInitialized ? StatusKind.Warning : StatusKind.Connected);
        }

        return true;
    }

    private bool CanWriteEquipmentOwnership()
    {
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

        if (!OwnershipEditsLikelyAccepted && !AllowOwnershipEditsBeforeIntroCompletion)
        {
            SetStatus("This save appears to be before TPHD begins honoring ownership edits. Progress past the Ordon Village intro arc and rescan.", StatusKind.Warning);
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
        _progressionState = RefreshOwnershipEditAcceptance(_progressionState);

        UpdateProgressionStateDisplays();
        UpdateEquipmentEditGuard();
    }

    private static ProgressionState RefreshOwnershipEditAcceptance(ProgressionState state)
    {
        var ownershipAcceptance = ProgressionStateService.EvaluateOwnershipEditAcceptance(
            state.HasPlayerData,
            state.InventoryInitialized,
            state.EquipmentInitialized);

        return state with
        {
            GameAcceptsOwnershipEdits = ownershipAcceptance.Acceptance,
            OwnershipEditDetectionReason = ownershipAcceptance.Reason
        };
    }

    private void UpdateEquipmentEditGuard()
    {
        var ownershipAcceptanceAllowsEditing =
            OwnershipEditsLikelyAccepted || AllowOwnershipEditsBeforeIntroCompletion;
        var canEdit =
            HasPlayerData &&
            (EquipmentInitialized || AllowEditingUninitializedEquipment) &&
            ownershipAcceptanceAllowsEditing;
        foreach (var slot in EquipmentSlots)
        {
            slot.CanEdit = false;
        }

        foreach (var flag in EquipmentFlags)
        {
            flag.CanEdit = canEdit;
        }

        EquipmentInitializationWarningText.Visibility = HasPlayerData && !EquipmentInitialized
            ? Visibility.Visible
            : Visibility.Collapsed;
        EquipmentOwnershipAcceptanceWarningText.Visibility =
            HasPlayerData &&
            EffectiveOwnershipEditAcceptance == OwnershipEditAcceptance.LikelyNo &&
            !AllowOwnershipEditsBeforeIntroCompletion
                ? Visibility.Visible
                : Visibility.Collapsed;
        ApplyEquipmentOwnershipButton.IsEnabled = canEdit;
    }

    private async Task<bool> RemoveInventoryVisibleItemAsync(InventoryRemovalItemViewModel item)
    {
        if (!CanWriteInventoryRemoval())
        {
            return false;
        }

        if (!item.CurrentItemId.HasValue || item.CurrentItemId.Value == InventoryDefinitions.EmptyItemId)
        {
            SetStatus("No detected visible item is available to remove for this slot.", StatusKind.Neutral);
            return false;
        }

        item.CapturePrevious(item.CurrentItemId.Value);
        return await WriteInventoryRemovalSlotAsync(
            item,
            InventoryDefinitions.EmptyItemId,
            "remove",
            "Visible inventory slot removed for research.");
    }

    private async Task<bool> RestoreInventoryVisibleItemAsync(InventoryRemovalItemViewModel item)
    {
        if (!CanWriteInventoryRemoval())
        {
            return false;
        }

        if (!item.PreviousItemId.HasValue)
        {
            SetStatus("No previous visible inventory value is captured for this slot.", StatusKind.Neutral);
            return false;
        }

        var restored = await WriteInventoryRemovalSlotAsync(
            item,
            item.PreviousItemId.Value,
            "restore",
            "Visible inventory slot restored for research.");

        if (restored)
        {
            item.ClearPrevious();
        }

        return restored;
    }

    private async Task<bool> WriteInventoryRemovalSlotAsync(
        InventoryRemovalItemViewModel item,
        byte valueToWrite,
        string action,
        string successMessage)
    {
        var memory = _memory;
        if (memory is null || !_playerBaseAddress.HasValue)
        {
            SetStatus("Not attached. Attach to Cemu and rescan before testing inventory removal.", StatusKind.Neutral);
            return false;
        }

        byte? oldValue = null;
        byte? immediateReadback = null;
        var playerBaseAddress = _playerBaseAddress.Value;
        var absoluteAddress = playerBaseAddress + item.OffsetValue;
        var diagnosticStatus = "started";

        try
        {
            if (!InventoryMemoryService.TryReadSlot(
                    memory,
                    playerBaseAddress,
                    item.SlotIndex,
                    out var oldItemId,
                    out var oldReadError))
            {
                diagnosticStatus = $"old-read-failed: {oldReadError}";
                item.Status = oldReadError;
                SetStatus(oldReadError, StatusKind.Warning);
                return false;
            }

            oldValue = oldItemId;

            if (!InventoryMemoryService.TryWriteSlot(
                    memory,
                    playerBaseAddress,
                    item.SlotIndex,
                    valueToWrite,
                    out var writeError))
            {
                diagnosticStatus = $"write-call-failed: {writeError}";
                item.Status = $"Write failed: {writeError}";
                SetStatus($"Inventory removal write failed: {writeError}", StatusKind.Warning);
                return false;
            }

            if (!InventoryMemoryService.TryReadSlot(
                    memory,
                    playerBaseAddress,
                    item.SlotIndex,
                    out var immediateItemId,
                    out var immediateReadError))
            {
                diagnosticStatus = $"immediate-read-failed: {immediateReadError}";
                item.Status = immediateReadError;
                SetStatus(immediateReadError, StatusKind.Warning);
                return false;
            }

            immediateReadback = immediateItemId;

            if (immediateReadback.Value != valueToWrite)
            {
                diagnosticStatus = "immediate-mismatch";
                item.Status = $"Write failed: expected {valueToWrite} but read {immediateReadback.Value}.";
                SetStatus(item.Status, StatusKind.Warning);
                RefreshInventorySlots(showStatus: false);
                return false;
            }

            diagnosticStatus = "immediate-verified";
            item.Status = action == "remove"
                ? "Removed; refresh/readback verified."
                : "Restored; refresh/readback verified.";
            SetStatus(successMessage, StatusKind.Connected);
            RefreshInventorySlots(showStatus: false);
            await Task.CompletedTask;
            return true;
        }
        finally
        {
            AppendInventoryRemovalDiagnostic(
                item,
                action,
                absoluteAddress,
                oldValue,
                valueToWrite,
                immediateReadback,
                diagnosticStatus);

        }
    }

    private async Task<bool> WriteInventoryCheckboxTestAsync(InventoryOwnershipItemViewModel item)
    {
        var memory = _memory;
        if (memory is null || !_playerBaseAddress.HasValue)
        {
            SetStatus("Not attached. Attach to Cemu and rescan before testing inventory checkboxes.", StatusKind.Neutral);
            return false;
        }

        if (!item.CanExperimentalCheckboxWrite || !item.KnownSlotIndex.HasValue || !item.KnownItemId.HasValue)
        {
            item.ExperimentalStatus = "No supported visible slot captured for experimental write.";
            SetStatus($"{item.Name} has no supported captured visible slot for experimental writing.", StatusKind.Warning);
            return false;
        }

        var desiredValue = item.IsOwnedDesired
            ? item.KnownItemId.Value
            : InventoryDefinitions.EmptyItemId;
        var action = item.IsOwnedDesired ? "write-known-item-id" : "remove-visible-slot";
        var slotIndex = item.KnownSlotIndex.Value;
        var absoluteAddress = _playerBaseAddress.Value + InventoryDefinitions.FirstSlotOffset + (uint)slotIndex;

        byte? oldValue = null;
        byte? immediateReadback = null;
        byte? delayed250Readback = null;
        byte? delayed1000Readback = null;
        var diagnosticStatus = "started";

        try
        {
            if (!InventoryMemoryService.TryReadSlot(
                    memory,
                    _playerBaseAddress.Value,
                    slotIndex,
                    out var oldItemId,
                    out var oldReadError))
            {
                diagnosticStatus = $"old-read-failed: {oldReadError}";
                item.ExperimentalStatus = oldReadError;
                SetStatus(oldReadError, StatusKind.Warning);
                return false;
            }

            oldValue = oldItemId;

            if (!InventoryMemoryService.TryWriteSlot(
                    memory,
                    _playerBaseAddress.Value,
                    slotIndex,
                    desiredValue,
                    out var writeError))
            {
                diagnosticStatus = $"write-call-failed: {writeError}";
                item.ExperimentalStatus = $"Write failed: {writeError}";
                SetStatus(item.ExperimentalStatus, StatusKind.Warning);
                return false;
            }

            if (!InventoryMemoryService.TryReadSlot(
                    memory,
                    _playerBaseAddress.Value,
                    slotIndex,
                    out var immediateValue,
                    out var immediateReadError))
            {
                diagnosticStatus = $"immediate-read-failed: {immediateReadError}";
                item.ExperimentalStatus = immediateReadError;
                SetStatus(immediateReadError, StatusKind.Warning);
                return false;
            }

            immediateReadback = immediateValue;
            if (immediateReadback.Value != desiredValue)
            {
                diagnosticStatus = "immediate-mismatch";
                item.ExperimentalStatus = $"Write failed: expected {desiredValue} but read {immediateReadback.Value}.";
                SetStatus(item.ExperimentalStatus, StatusKind.Warning);
                RefreshInventorySlots(showStatus: false);
                return false;
            }

            await Task.Delay(250);
            if (InventoryMemoryService.TryReadSlot(
                    memory,
                    _playerBaseAddress.Value,
                    slotIndex,
                    out var delayed250Value,
                    out _))
            {
                delayed250Readback = delayed250Value;
            }

            await Task.Delay(750);
            if (InventoryMemoryService.TryReadSlot(
                    memory,
                    _playerBaseAddress.Value,
                    slotIndex,
                    out var delayed1000Value,
                    out _))
            {
                delayed1000Readback = delayed1000Value;
            }

            var reverted =
                delayed250Readback.HasValue && delayed250Readback.Value != desiredValue ||
                delayed1000Readback.HasValue && delayed1000Readback.Value != desiredValue;
            if (reverted)
            {
                diagnosticStatus = "reverted-by-game";
                item.ExperimentalStatus = "Reverted by game.";
                SetStatus($"{item.Name}: Reverted by game.", StatusKind.Warning);
            }
            else
            {
                diagnosticStatus = "memory-changed";
                item.ExperimentalStatus = "Memory changed; game may require menu reopen or additional flags.";
                SetStatus($"{item.Name}: Memory changed; game may require menu reopen or additional flags.", StatusKind.Connected);
            }

            RefreshInventorySlots(showStatus: false);
            return !reverted;
        }
        finally
        {
            AppendInventoryCheckboxTestingDiagnostic(
                item,
                action,
                item.KnownMappingSource,
                slotIndex,
                absoluteAddress,
                oldValue,
                desiredValue,
                immediateReadback,
                delayed250Readback,
                delayed1000Readback,
                diagnosticStatus);
        }
    }

    private async Task<bool> WriteBottleSlotWithDiagnosticsAsync(
        BottleSlotViewModel slot,
        byte desiredValue,
        string action,
        string successMessage)
    {
        var memory = _memory;
        if (memory is null || !_playerBaseAddress.HasValue)
        {
            SetStatus("Not attached. Attach to Cemu and rescan before editing bottle slots.", StatusKind.Neutral);
            return false;
        }

        var absoluteAddress = _playerBaseAddress.Value + slot.OffsetValue;
        byte? oldValue = null;
        byte? immediateReadback = null;
        byte? delayed250Readback = null;
        byte? delayed1000Readback = null;
        var diagnosticStatus = "started";

        try
        {
            if (!InventoryMemoryService.TryReadSlot(
                    memory,
                    _playerBaseAddress.Value,
                    slot.InventorySlotIndex,
                    out var oldItemId,
                    out var oldReadError))
            {
                diagnosticStatus = $"old-read-failed: {oldReadError}";
                slot.Status = oldReadError;
                slot.LastWriteResult = oldReadError;
                slot.LastVerificationResult = "Old value could not be read.";
                SetStatus(oldReadError, StatusKind.Warning);
                return false;
            }

            oldValue = oldItemId;
            if (action is not "restore")
            {
                slot.CapturePrevious(oldItemId);
            }

            if (!InventoryMemoryService.TryWriteSlot(
                    memory,
                    _playerBaseAddress.Value,
                    slot.InventorySlotIndex,
                    desiredValue,
                    out var writeError))
            {
                diagnosticStatus = $"write-call-failed: {writeError}";
                slot.Status = $"Write failed: {writeError}";
                slot.LastWriteResult = slot.Status;
                slot.LastVerificationResult = "Write call failed.";
                SetStatus(slot.Status, StatusKind.Warning);
                return false;
            }

            slot.LastWriteResult = $"Wrote {FormatEquipmentByte(desiredValue)}.";

            if (!InventoryMemoryService.TryReadSlot(
                    memory,
                    _playerBaseAddress.Value,
                    slot.InventorySlotIndex,
                    out var immediateValue,
                    out var immediateReadError))
            {
                diagnosticStatus = $"immediate-read-failed: {immediateReadError}";
                slot.Status = immediateReadError;
                slot.LastVerificationResult = "Immediate readback failed.";
                SetStatus(immediateReadError, StatusKind.Warning);
                return false;
            }

            immediateReadback = immediateValue;
            if (immediateReadback.Value != desiredValue)
            {
                diagnosticStatus = "immediate-mismatch";
                slot.Status = $"Write failed: expected {desiredValue} but read {immediateReadback.Value}.";
                slot.LastVerificationResult = slot.Status;
                SetStatus(slot.Status, StatusKind.Warning);
                RefreshInventorySlots(showStatus: false);
                return false;
            }

            await Task.Delay(250);
            if (InventoryMemoryService.TryReadSlot(
                    memory,
                    _playerBaseAddress.Value,
                    slot.InventorySlotIndex,
                    out var delayed250Value,
                    out _))
            {
                delayed250Readback = delayed250Value;
            }

            await Task.Delay(750);
            if (InventoryMemoryService.TryReadSlot(
                    memory,
                    _playerBaseAddress.Value,
                    slot.InventorySlotIndex,
                    out var delayed1000Value,
                    out _))
            {
                delayed1000Readback = delayed1000Value;
            }

            var reverted =
                delayed250Readback.HasValue && delayed250Readback.Value != desiredValue ||
                delayed1000Readback.HasValue && delayed1000Readback.Value != desiredValue;
            if (reverted)
            {
                diagnosticStatus = "reverted-by-game";
                slot.Status = "Reverted by game.";
                slot.LastVerificationResult = "Immediate readback matched, but delayed verification changed.";
                SetStatus($"Bottle slot {slot.BottleSlotNumber}: Reverted by game.", StatusKind.Warning);
            }
            else
            {
                diagnosticStatus = "verified";
                slot.Status = "Write verified.";
                slot.LastVerificationResult = "Immediate, 250ms, and 1000ms readbacks matched.";
                SetStatus(successMessage, StatusKind.Connected);
            }

            RefreshInventorySlots(showStatus: false);
            return !reverted;
        }
        finally
        {
            AppendBottleEditorDiagnostic(
                slot,
                action,
                absoluteAddress,
                oldValue,
                desiredValue,
                immediateReadback,
                delayed250Readback,
                delayed1000Readback,
                diagnosticStatus);
        }
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
                        ? "This slot appears game-managed. Raw writes are for research; use ownership/progression flags once identified."
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

    private async Task<bool> WriteInventoryOwnershipWithDiagnosticsAsync(
        InventoryOwnershipItemViewModel item,
        bool desiredValue,
        string successMessage)
    {
        var memory = _memory;
        if (memory is null || !_playerBaseAddress.HasValue)
        {
            SetStatus("Not attached. Attach to Cemu and rescan before editing inventory ownership.", StatusKind.Neutral);
            return false;
        }

        if (!item.Definition.FlagOffset.HasValue)
        {
            SetStatus($"{item.Name} ownership flag is unknown.", StatusKind.Warning);
            return false;
        }

        _inventoryOwnershipDiagnosticInProgress = true;

        byte? oldValue = null;
        byte? writtenValue = null;
        byte? immediateReadback = null;
        byte? delayed250Readback = null;
        byte? delayed1000Readback = null;
        var playerBaseAddress = _playerBaseAddress.Value;
        var offsetValue = item.Definition.FlagOffset.Value;
        var absoluteAddress = playerBaseAddress + offsetValue;
        var diagnosticStatus = "started";

        try
        {
            if (!InventoryMemoryService.TryWriteOwnershipFlag(
                    memory,
                    playerBaseAddress,
                    item.Definition,
                    desiredValue,
                    out var oldBackingValue,
                    out var writtenBackingValue,
                    out var writeError))
            {
                diagnosticStatus = $"write-call-failed: {writeError}";
                SetStatus("Inventory ownership write failed or wrong address.", StatusKind.Warning);
                return false;
            }

            oldValue = oldBackingValue;
            writtenValue = writtenBackingValue;

            if (!InventoryMemoryService.TryReadByte(
                    memory,
                    playerBaseAddress,
                    offsetValue,
                    item.Name,
                    out var immediateValue,
                    out var immediateReadError))
            {
                diagnosticStatus = $"immediate-read-failed: {immediateReadError}";
                SetStatus("Inventory ownership write failed or wrong address.", StatusKind.Warning);
                return false;
            }

            immediateReadback = immediateValue;

            if (!DoesFlagMatch(immediateValue, item.Definition.Mask, desiredValue))
            {
                diagnosticStatus = "immediate-mismatch";
                SetStatus("Inventory ownership write failed or wrong address.", StatusKind.Warning);
                return false;
            }

            await Task.Delay(250);
            if (InventoryMemoryService.TryReadByte(
                    memory,
                    playerBaseAddress,
                    offsetValue,
                    item.Name,
                    out var delayed250Value,
                    out _))
            {
                delayed250Readback = delayed250Value;
            }

            await Task.Delay(750);
            if (InventoryMemoryService.TryReadByte(
                    memory,
                    playerBaseAddress,
                    offsetValue,
                    item.Name,
                    out var delayed1000Value,
                    out _))
            {
                delayed1000Readback = delayed1000Value;
            }

            var laterReverted =
                delayed250Readback.HasValue && !DoesFlagMatch(delayed250Readback.Value, item.Definition.Mask, desiredValue) ||
                delayed1000Readback.HasValue && !DoesFlagMatch(delayed1000Readback.Value, item.Definition.Mask, desiredValue);

            if (laterReverted)
            {
                diagnosticStatus = "later-reverted";
                SetStatus("Inventory ownership write succeeded, but game reverted it.", StatusKind.Warning);
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
            AppendInventoryOwnershipDiagnostic(
                item,
                absoluteAddress,
                oldValue,
                writtenValue,
                immediateReadback,
                delayed250Readback,
                delayed1000Readback,
                desiredValue,
                diagnosticStatus);

            _inventoryOwnershipDiagnosticInProgress = false;
            UpdateInventoryEditGuard();
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

    private void AppendInventoryRemovalDiagnostic(
        InventoryRemovalItemViewModel item,
        string action,
        ulong absoluteAddress,
        byte? oldValue,
        byte writtenValue,
        byte? immediateReadback,
        string diagnosticStatus)
    {
        var entry =
            $"{DateTimeOffset.Now:O} kind=inventory-removal action={action} slot={item.SlotNumber} " +
            $"offset={item.Offset} address=0x{absoluteAddress:X} old-item=\"{FormatInventoryItemName(oldValue)}\" " +
            $"old={FormatEquipmentByte(oldValue)} wrote={FormatEquipmentByte(writtenValue)} " +
            $"immediate={FormatEquipmentByte(immediateReadback)} previous={FormatEquipmentByte(item.PreviousItemId)} " +
            $"status={diagnosticStatus}";

        AppendInventoryRemovalLogEntry(entry);
    }

    private void AppendInventoryRemovalLogEntry(string entry)
    {
        InventoryRemovalDiagnostics.Insert(0, entry);
        while (InventoryRemovalDiagnostics.Count > 100)
        {
            InventoryRemovalDiagnostics.RemoveAt(InventoryRemovalDiagnostics.Count - 1);
        }

        try
        {
            var logDirectory = Path.GetDirectoryName(InventoryRemovalLogPath);
            if (!string.IsNullOrWhiteSpace(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            File.AppendAllText(InventoryRemovalLogPath, entry + Environment.NewLine);
        }
        catch (Exception ex)
        {
            InventoryRemovalDiagnostics.Insert(0, $"{DateTimeOffset.Now:O} inventory-removal-log-write-failed: {ex.Message}");
        }
    }

    private void AppendInventoryCheckboxTestingDiagnostic(
        InventoryOwnershipItemViewModel item,
        string action,
        string mappingSource,
        int slotIndex,
        ulong absoluteAddress,
        byte? oldValue,
        byte writtenValue,
        byte? immediateReadback,
        byte? delayed250Readback,
        byte? delayed1000Readback,
        string diagnosticStatus)
    {
        var entry =
            $"{DateTimeOffset.Now:O} kind=inventory-checkbox-testing action={action} item=\"{item.Name}\" " +
            $"mapping-source={mappingSource} slot={slotIndex + 1} offset={InventoryDefinitions.GetSlotOffset(slotIndex)} " +
            $"address=0x{absoluteAddress:X} " +
            $"old={FormatEquipmentByte(oldValue)} wrote={FormatEquipmentByte(writtenValue)} " +
            $"immediate={FormatEquipmentByte(immediateReadback)} read250ms={FormatEquipmentByte(delayed250Readback)} " +
            $"read1000ms={FormatEquipmentByte(delayed1000Readback)} status={diagnosticStatus}";

        AppendInventoryCheckboxTestingLogEntry(entry);
    }

    private void AppendInventoryCheckboxSafeguardDiagnostic(
        string reason,
        InventoryOwnershipItemViewModel preferred,
        InventoryOwnershipItemViewModel disabled)
    {
        var entry =
            $"{DateTimeOffset.Now:O} kind=inventory-checkbox-safeguard reason={reason} " +
            $"preferred=\"{preferred.Name}\" disabled=\"{disabled.Name}\" " +
            "message=\"Use only one Clawshot variant. Enabling both can hide or displace another progression item such as the Dominion Rod.\"";

        AppendInventoryCheckboxTestingLogEntry(entry);
    }

    private void AppendInventoryCheckboxTestingLogEntry(string entry)
    {
        InventoryCheckboxTestingDiagnostics.Insert(0, entry);
        while (InventoryCheckboxTestingDiagnostics.Count > 100)
        {
            InventoryCheckboxTestingDiagnostics.RemoveAt(InventoryCheckboxTestingDiagnostics.Count - 1);
        }

        try
        {
            var logDirectory = Path.GetDirectoryName(InventoryCheckboxTestingLogPath);
            if (!string.IsNullOrWhiteSpace(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            File.AppendAllText(InventoryCheckboxTestingLogPath, entry + Environment.NewLine);
        }
        catch (Exception ex)
        {
            InventoryCheckboxTestingDiagnostics.Insert(0, $"{DateTimeOffset.Now:O} inventory-checkbox-testing-log-write-failed: {ex.Message}");
        }
    }

    private void AppendBottleEditorDiagnostic(
        BottleSlotViewModel slot,
        string action,
        ulong absoluteAddress,
        byte? oldValue,
        byte writtenValue,
        byte? immediateReadback,
        byte? delayed250Readback,
        byte? delayed1000Readback,
        string diagnosticStatus)
    {
        var entry =
            $"{DateTimeOffset.Now:O} kind=bottle-editor action={action} " +
            $"slot={slot.BottleSlotNumber} offset={slot.Offset} address=0x{absoluteAddress:X} " +
            $"previous={FormatEquipmentByte(oldValue)} new={FormatEquipmentByte(writtenValue)} " +
            $"immediate={FormatEquipmentByte(immediateReadback)} read250ms={FormatEquipmentByte(delayed250Readback)} " +
            $"read1000ms={FormatEquipmentByte(delayed1000Readback)} status={diagnosticStatus}";

        AppendBottleEditorLogEntry(entry);
    }

    private void AppendBottleEditorLogEntry(string entry)
    {
        BottleEditorDiagnostics.Insert(0, entry);
        while (BottleEditorDiagnostics.Count > 100)
        {
            BottleEditorDiagnostics.RemoveAt(BottleEditorDiagnostics.Count - 1);
        }

        try
        {
            var logDirectory = Path.GetDirectoryName(BottleEditorLogPath);
            if (!string.IsNullOrWhiteSpace(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            File.AppendAllText(BottleEditorLogPath, entry + Environment.NewLine);
        }
        catch (Exception ex)
        {
            BottleEditorDiagnostics.Insert(0, $"{DateTimeOffset.Now:O} bottle-editor-log-write-failed: {ex.Message}");
        }
    }

    private void AppendInventoryStateDiagnostic(string state)
    {
        var rawSlots = string.Join(
            ",",
            InventorySlots.Select(slot => $"{slot.SlotLabel}:{slot.CurrentItemId}/{slot.CurrentItemName}@{slot.Offset}"));
        var ownershipDetections = string.Join(
            ",",
            InventoryOwnershipItems.Select(item => $"{item.Name}:{item.CurrentDetectedState} flag={item.FlagLocation} writable={item.Definition.CanWrite}"));

        AppendInventoryLogEntry(
            $"{DateTimeOffset.Now:O} inventory-state={state} slots={InventoryDefinitions.SlotCount} " +
            $"empty-id={InventoryDefinitions.EmptyItemId} override={AllowEditingUninitializedInventory} " +
            $"unsafe-raw-writes={AllowUnsafeRawInventoryWrites} raw-slots=\"{rawSlots}\" " +
            $"ownership-detections=\"{ownershipDetections}\"");
    }

    private void AppendInventoryOwnershipDiagnostic(
        InventoryOwnershipItemViewModel item,
        ulong absoluteAddress,
        byte? oldValue,
        byte? writtenValue,
        byte? immediateReadback,
        byte? delayed250Readback,
        byte? delayed1000Readback,
        bool desiredValue,
        string diagnosticStatus)
    {
        var finalDetectedState = delayed1000Readback.HasValue
            ? DoesFlagMatch(delayed1000Readback.Value, item.Definition.Mask, desiredValue).ToString(CultureInfo.InvariantCulture)
            : "n/a";
        var entry =
            $"{DateTimeOffset.Now:O} kind=inventory-ownership name=\"{item.Name}\" flag={item.FlagLocation} " +
            $"address=0x{absoluteAddress:X} old={FormatEquipmentByte(oldValue)} desired-bit={desiredValue} " +
            $"desired-byte={FormatEquipmentByte(writtenValue)} immediate={FormatEquipmentByte(immediateReadback)} " +
            $"read250ms={FormatEquipmentByte(delayed250Readback)} read1000ms={FormatEquipmentByte(delayed1000Readback)} " +
            $"final-detected-matches={finalDetectedState} status={diagnosticStatus}";

        AppendInventoryOwnershipLogEntry(entry);
    }

    private void AppendInventoryOwnershipStateDiagnostic(string reason)
    {
        var ownershipValues = string.Join(
            ",",
            InventoryOwnershipItems.Select(item =>
                $"{item.Name}:detected={item.IsOwnedDetected} desired={item.IsOwnedDesired} dirty={item.IsDirty} flag={item.FlagLocation} byte={item.BackingValue} writable={item.Definition.CanWrite} state=\"{item.CurrentDetectedState}\""));

        AppendInventoryOwnershipLogEntry(
            $"{DateTimeOffset.Now:O} kind=inventory-ownership-state reason={reason} " +
            $"inventory-initialized={InventoryInitialized} ownership=\"{ownershipValues}\"");
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

    private void AppendInventoryOwnershipLogEntry(string entry)
    {
        InventoryOwnershipDiagnostics.Insert(0, entry);
        while (InventoryOwnershipDiagnostics.Count > 100)
        {
            InventoryOwnershipDiagnostics.RemoveAt(InventoryOwnershipDiagnostics.Count - 1);
        }

        try
        {
            var logDirectory = Path.GetDirectoryName(InventoryOwnershipLogPath);
            if (!string.IsNullOrWhiteSpace(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            File.AppendAllText(InventoryOwnershipLogPath, entry + Environment.NewLine);
        }
        catch (Exception ex)
        {
            InventoryOwnershipDiagnostics.Insert(0, $"{DateTimeOffset.Now:O} inventory-ownership-log-write-failed: {ex.Message}");
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

    private void AppendEquipmentStateDiagnostic(string reason)
    {
        var equippedValues = string.Join(
            ",",
            EquipmentSlots.Select(slot => $"{slot.Name}:{slot.CurrentValue}/{slot.CurrentName}@{slot.Offset}"));
        var ownershipValues = string.Join(
            ",",
            EquipmentFlags.Select(flag =>
                $"{flag.Name}:detected={flag.IsOwnedDetected} desired={flag.IsOwnedDesired} dirty={flag.IsDirty} byte={flag.BackingValue} offset={flag.Offset} bit={flag.Bit}"));

        AppendEquipmentLogEntry(
            $"{DateTimeOffset.Now:O} kind=equipment-state reason={reason} " +
            $"equipped=\"{equippedValues}\" ownership=\"{ownershipValues}\"");
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

    private void AppendCollectiblesDiagnostic(
        string name,
        int? previousValue,
        int desiredValue,
        int? readbackValue,
        string diagnosticStatus)
    {
        var addressText = _playerBaseAddress.HasValue
            ? $"0x{_playerBaseAddress.Value + PoeSouls.Definition.Offset:X}"
            : "n/a";
        var entry =
            $"{DateTimeOffset.Now:O} kind=collectible name=\"{name}\" offset=0x{PoeSouls.Definition.Offset:X} " +
            $"address={addressText} previous={FormatNullableInt(previousValue)} desired={desiredValue} " +
            $"readback={FormatNullableInt(readbackValue)} status={diagnosticStatus}";

        AppendCollectiblesLogEntry(entry);
    }

    private void AppendCollectiblesLogEntry(string entry)
    {
        CollectiblesDiagnostics.Insert(0, entry);
        while (CollectiblesDiagnostics.Count > 100)
        {
            CollectiblesDiagnostics.RemoveAt(CollectiblesDiagnostics.Count - 1);
        }

        try
        {
            var logDirectory = Path.GetDirectoryName(CollectiblesLogPath);
            if (!string.IsNullOrWhiteSpace(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            File.AppendAllText(CollectiblesLogPath, entry + Environment.NewLine);
        }
        catch (Exception ex)
        {
            CollectiblesDiagnostics.Insert(0, $"{DateTimeOffset.Now:O} collectibles-log-write-failed: {ex.Message}");
        }
    }

    private void AppendProgressionDiagnostic()
    {
        var entry =
            $"{DateTimeOffset.Now:O} player-base={_progressionState.PlayerBaseText} " +
            $"has-player-data={HasPlayerData} inventory-initialized={InventoryInitialized} " +
            $"equipment-initialized={EquipmentInitialized} memory-initialized={_progressionState.MemoryInitialized} " +
            $"ownership-edits={EffectiveOwnershipEditAcceptanceText} " +
            $"ownership-detection-reason=\"{EffectiveOwnershipEditDetectionReason}\" " +
            $"user-past-intro-override={UserConfirmedPastIntroArc} intro-write-override={AllowOwnershipEditsBeforeIntroCompletion} " +
            $"inventory-bytes=\"{_progressionState.InventoryRawBytesText}\" " +
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

    private void AppendScanDiagnostic(ProcessAttachDiagnostics attachDiagnostics, AobScanResult? scanResult)
    {
        var entry = scanResult is null
            ? $"{DateTimeOffset.Now:O} process-discovery-ms={attachDiagnostics.ProcessDiscoveryTime.TotalMilliseconds:F1} " +
              $"handle-open-ms={attachDiagnostics.HandleOpenTime.TotalMilliseconds:F1} scan=not-started"
            : $"{DateTimeOffset.Now:O} process-discovery-ms={attachDiagnostics.ProcessDiscoveryTime.TotalMilliseconds:F1} " +
              $"handle-open-ms={attachDiagnostics.HandleOpenTime.TotalMilliseconds:F1} " +
              $"region-enumeration-ms={scanResult.RegionEnumerationTime.TotalMilliseconds:F1} " +
              $"scan-ms={scanResult.ScanTime.TotalMilliseconds:F1} regions-scanned={scanResult.RegionsScanned} " +
              $"regions-skipped={scanResult.RegionsSkipped} bytes-scanned={scanResult.TotalBytesScanned} " +
              $"match-address={FormatNullableAddress(scanResult.MatchAddress)} match-source={scanResult.MatchSource} " +
              $"cache-status={scanResult.CacheStatus} cached-base-valid={scanResult.CachedBaseValidated} " +
              $"cached-region-valid={scanResult.CachedRegionValidated} " +
              $"match-region={FormatRegion(scanResult.MatchRegion)}";

        try
        {
            var logDirectory = Path.GetDirectoryName(ScanLogPath);
            if (!string.IsNullOrWhiteSpace(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            File.AppendAllText(ScanLogPath, entry + Environment.NewLine);
        }
        catch (Exception ex)
        {
            ScanDiagnosticsText.Text = $"Scan log write failed: {ex.Message}";
        }
    }

    private static string FormatScanSummary(ProcessAttachDiagnostics attachDiagnostics, AobScanResult scanResult)
    {
        return
            $"Process discovery: {FormatDuration(attachDiagnostics.ProcessDiscoveryTime)}{Environment.NewLine}" +
            $"Handle open: {FormatDuration(attachDiagnostics.HandleOpenTime)}{Environment.NewLine}" +
            $"Region enumeration: {FormatDuration(scanResult.RegionEnumerationTime)}{Environment.NewLine}" +
            $"Scan: {FormatDuration(scanResult.ScanTime)}{Environment.NewLine}" +
            $"Regions scanned/skipped: {scanResult.RegionsScanned}/{scanResult.RegionsSkipped}{Environment.NewLine}" +
            $"Bytes scanned: {FormatByteCount(scanResult.TotalBytesScanned)}{Environment.NewLine}" +
            $"Match address: {FormatNullableAddress(scanResult.MatchAddress)}{Environment.NewLine}" +
            $"Match source: {scanResult.MatchSource}{Environment.NewLine}" +
            $"Cache: {scanResult.CacheStatus}, base valid={scanResult.CachedBaseValidated}, region valid={scanResult.CachedRegionValidated}{Environment.NewLine}" +
            $"Match region: {FormatRegion(scanResult.MatchRegion)}";
    }

    private static string FormatDuration(TimeSpan duration)
    {
        return $"{duration.TotalMilliseconds:F1} ms";
    }

    private static string FormatAddress(ulong address)
    {
        return $"0x{address:X}";
    }

    private static string FormatNullableAddress(ulong? address)
    {
        return address.HasValue ? FormatAddress(address.Value) : "-";
    }

    private static string FormatByteCount(ulong bytes)
    {
        const double kib = 1024;
        const double mib = kib * 1024;
        const double gib = mib * 1024;

        return bytes switch
        {
            >= (ulong)gib => $"{bytes / gib:F2} GiB",
            >= (ulong)mib => $"{bytes / mib:F2} MiB",
            >= (ulong)kib => $"{bytes / kib:F2} KiB",
            _ => $"{bytes} B"
        };
    }

    private static string FormatRegion(MemoryRegion? region)
    {
        return region.HasValue
            ? $"{FormatAddress(region.Value.BaseAddress)}+{FormatByteCount(region.Value.Size)} {region.Value.TypeName} protect=0x{region.Value.Protect:X}"
            : "-";
    }

    private bool RefreshResearchByte(bool showStatus)
    {
        if (!TryReadResearchByte(out var offset, out var value, out _))
        {
            return false;
        }

        if (showStatus)
        {
            SetStatus($"Read research byte at _playerbase+0x{offset:X}: {FormatResearchByte(value)}.", StatusKind.Connected);
        }

        return true;
    }

    private bool TryReadResearchByte(out uint offset, out byte value, out ulong absoluteAddress)
    {
        offset = 0;
        value = 0;
        absoluteAddress = 0;

        if (_memory is null || !_playerBaseAddress.HasValue)
        {
            ResearchCurrentByteText.Text = "Not attached";
            ResearchAbsoluteAddressText.Text = "-";
            SetStatus("Not attached. Attach to Cemu and rescan before using research tools.", StatusKind.Neutral);
            return false;
        }

        if (!TryParseResearchOffset(ResearchOffsetText.Text, out offset, out var parseError))
        {
            ResearchCurrentByteText.Text = parseError;
            ResearchAbsoluteAddressText.Text = "-";
            return false;
        }

        absoluteAddress = _playerBaseAddress.Value + offset;
        if (!_memory.TryReadBytes(absoluteAddress, 1, out var bytes, out var bytesRead) || bytesRead != 1)
        {
            ResearchCurrentByteText.Text = "Read failed";
            ResearchAbsoluteAddressText.Text = $"0x{absoluteAddress:X}";
            SetStatus($"Could not read research byte at _playerbase+0x{offset:X}.", StatusKind.Warning);
            return false;
        }

        value = bytes[0];
        ResearchCurrentByteText.Text = FormatResearchByte(value);
        ResearchAbsoluteAddressText.Text = $"0x{absoluteAddress:X}";
        return true;
    }

    private bool TryReadCandidateByte(out uint offset, out byte value, out ulong absoluteAddress)
    {
        offset = 0;
        value = 0;
        absoluteAddress = 0;

        if (_memory is null || !_playerBaseAddress.HasValue)
        {
            CandidateCurrentByteText.Text = "Not attached";
            CandidateAbsoluteAddressText.Text = "-";
            CandidateStatusText.Text = "Not attached. Attach to Cemu and rescan before testing candidate flags.";
            SetStatus("Not attached. Attach to Cemu and rescan before testing candidate flags.", StatusKind.Neutral);
            return false;
        }

        if (!TryParseResearchOffset(CandidateOffsetText.Text, out offset, out var parseError))
        {
            CandidateCurrentByteText.Text = parseError;
            CandidateAbsoluteAddressText.Text = "-";
            CandidateStatusText.Text = parseError;
            return false;
        }

        absoluteAddress = _playerBaseAddress.Value + offset;
        if (!TryReadCandidateByteAt(offset, absoluteAddress, out value, out var readError))
        {
            CandidateCurrentByteText.Text = "Read failed";
            CandidateAbsoluteAddressText.Text = $"0x{absoluteAddress:X}";
            CandidateStatusText.Text = readError;
            SetStatus($"Could not read candidate byte at _playerbase+0x{offset:X}.", StatusKind.Warning);
            return false;
        }

        CandidateCurrentByteText.Text = FormatResearchByte(value);
        CandidateAbsoluteAddressText.Text = $"0x{absoluteAddress:X}";
        return true;
    }

    private bool TryReadCandidateByteAt(uint offset, ulong absoluteAddress, out byte value, out string error)
    {
        value = 0;

        if (_memory is null)
        {
            error = "Not attached.";
            return false;
        }

        if (!_memory.TryReadBytes(absoluteAddress, 1, out var bytes, out var bytesRead) || bytesRead != 1)
        {
            error = $"Could not read candidate byte at _playerbase+0x{offset:X} / 0x{absoluteAddress:X}.";
            return false;
        }

        value = bytes[0];
        error = string.Empty;
        return true;
    }

    private bool TryReadResearchRange(out uint startOffset, out byte[] bytes, out ulong absoluteAddress)
    {
        startOffset = 0;
        bytes = [];
        absoluteAddress = 0;

        if (_memory is null || !_playerBaseAddress.HasValue)
        {
            ResearchRangeStatusText.Text = "Not attached. Attach to Cemu and rescan before using range research.";
            SetStatus("Not attached. Attach to Cemu and rescan before using research tools.", StatusKind.Neutral);
            return false;
        }

        if (!TryParseResearchOffset(ResearchRangeStartOffsetText.Text, out startOffset, out var offsetError))
        {
            ResearchRangeStatusText.Text = offsetError;
            return false;
        }

        if (!TryParseResearchLength(ResearchRangeLengthText.Text, out var length, out var lengthError))
        {
            ResearchRangeStatusText.Text = lengthError;
            return false;
        }

        absoluteAddress = _playerBaseAddress.Value + startOffset;
        if (!_memory.TryReadBytes(absoluteAddress, length, out bytes, out var bytesRead) || bytesRead != length)
        {
            ResearchRangeStatusText.Text = $"Could not read 0x{length:X} byte(s) at _playerbase+0x{startOffset:X}.";
            SetStatus("Could not read research range.", StatusKind.Warning);
            return false;
        }

        return true;
    }

    private bool TryCreateSnapshotDocument(out ResearchSnapshotDocument snapshot)
    {
        snapshot = new ResearchSnapshotDocument();

        if (_memory is null || !_playerBaseAddress.HasValue)
        {
            ResearchSnapshotLibraryStatusText.Text = "Attach to Cemu and load player data before saving a snapshot.";
            SetStatus("Attach before saving a research snapshot.", StatusKind.Neutral);
            return false;
        }

        var ranges = new List<ResearchSnapshotRange>();
        if (ResearchSaveInventoryRangeCheckBox.IsChecked == true &&
            !TryReadSnapshotRange(0x258, 0x18, "Inventory Slots", ranges))
        {
            return false;
        }

        if (ResearchSaveEquipmentRangeCheckBox.IsChecked == true &&
            !TryReadSnapshotRange(0x1D1, 0x03, "Equipped Gear", ranges))
        {
            return false;
        }

        if (ResearchSaveOwnershipRangeCheckBox.IsChecked == true &&
            !TryReadSnapshotRange(0x28D, 0x08, "Ownership", ranges))
        {
            return false;
        }

        if (ResearchSaveCustomRangeCheckBox.IsChecked == true)
        {
            if (!TryParseResearchOffset(ResearchRangeStartOffsetText.Text, out var customStart, out var offsetError))
            {
                ResearchSnapshotLibraryStatusText.Text = offsetError;
                return false;
            }

            if (!TryParseResearchLength(ResearchRangeLengthText.Text, out var customLength, out var lengthError))
            {
                ResearchSnapshotLibraryStatusText.Text = lengthError;
                return false;
            }

            var customLabel = string.IsNullOrWhiteSpace(ResearchRangeLabelText.Text)
                ? "Custom"
                : ResearchRangeLabelText.Text.Trim();

            if (!TryReadSnapshotRange(customStart, customLength, customLabel, ranges))
            {
                return false;
            }
        }

        if (ranges.Count == 0)
        {
            ResearchSnapshotLibraryStatusText.Text = "Select at least one range to save.";
            SetStatus("Select at least one research range.", StatusKind.Neutral);
            return false;
        }

        var name = string.IsNullOrWhiteSpace(ResearchSnapshotNameText.Text)
            ? $"Snapshot {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}"
            : ResearchSnapshotNameText.Text.Trim();

        snapshot = new ResearchSnapshotDocument
        {
            Name = name,
            Timestamp = DateTimeOffset.Now,
            Notes = ResearchSnapshotNotesText.Text.Trim(),
            GameStateDescription = ResearchSnapshotGameStateText.Text.Trim(),
            Ranges = ranges
        };

        return true;
    }

    private bool TryReadSnapshotRange(uint startOffset, int length, string label, ICollection<ResearchSnapshotRange> ranges)
    {
        if (_memory is null || !_playerBaseAddress.HasValue)
        {
            return false;
        }

        var absoluteAddress = _playerBaseAddress.Value + startOffset;
        if (!_memory.TryReadBytes(absoluteAddress, length, out var bytes, out var bytesRead) || bytesRead != length)
        {
            ResearchSnapshotLibraryStatusText.Text =
                $"Could not read {label} at _playerbase+0x{startOffset:X}, length 0x{length:X}.";
            SetStatus("Could not read research snapshot range.", StatusKind.Warning);
            return false;
        }

        ranges.Add(new ResearchSnapshotRange
        {
            Label = label,
            StartOffset = startOffset,
            Bytes = bytes
        });

        return true;
    }

    private bool TryCaptureOwnershipDiscoverySnapshot(string requestedName, out ResearchSnapshotDocument snapshot)
    {
        snapshot = new ResearchSnapshotDocument();

        if (_memory is null || !_playerBaseAddress.HasValue)
        {
            OwnershipDiscoveryStatusText.Text = "Attach to Cemu and load player data before capturing Ownership Discovery snapshots.";
            SetStatus("Attach before capturing Ownership Discovery snapshots.", StatusKind.Neutral);
            return false;
        }

        var ranges = new List<ResearchSnapshotRange>();
        foreach (var range in OwnershipDiscoveryRanges)
        {
            if (!TryReadOwnershipDiscoveryRange(range, ranges))
            {
                return false;
            }
        }

        var name = string.IsNullOrWhiteSpace(requestedName)
            ? $"Ownership Discovery {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}"
            : requestedName.Trim();

        snapshot = new ResearchSnapshotDocument
        {
            Name = name,
            Timestamp = DateTimeOffset.Now,
            Notes = "Ownership Discovery Mode capture",
            GameStateDescription = "Inventory slots, equipment ownership, candidate ownership region, and collectibles",
            Ranges = ranges
        };

        return true;
    }

    private bool TryReadOwnershipDiscoveryRange(
        OwnershipDiscoveryRangePreset range,
        ICollection<ResearchSnapshotRange> ranges)
    {
        if (_memory is null || !_playerBaseAddress.HasValue)
        {
            return false;
        }

        var absoluteAddress = _playerBaseAddress.Value + range.StartOffset;
        if (!_memory.TryReadBytes(absoluteAddress, range.Length, out var bytes, out var bytesRead) ||
            bytesRead != range.Length)
        {
            OwnershipDiscoveryStatusText.Text =
                $"Could not read {range.Label} at _playerbase+0x{range.StartOffset:X}, length 0x{range.Length:X}.";
            SetStatus("Could not read Ownership Discovery range.", StatusKind.Warning);
            return false;
        }

        ranges.Add(new ResearchSnapshotRange
        {
            Label = range.Label,
            StartOffset = range.StartOffset,
            Bytes = bytes
        });

        return true;
    }

    private void RefreshSnapshotBrowser()
    {
        ResearchSnapshots.Clear();
        foreach (var item in ResearchSnapshotStore.LoadSnapshots())
        {
            ResearchSnapshots.Add(new ResearchSnapshotViewModel(item.Path, item.Snapshot));
        }
    }

    private void CompareSavedSnapshots(ResearchSnapshotViewModel snapshotA, ResearchSnapshotViewModel snapshotB)
    {
        var valuesA = FlattenSnapshot(snapshotA.Snapshot);
        var valuesB = FlattenSnapshot(snapshotB.Snapshot);
        var offsets = valuesA.Keys
            .Union(valuesB.Keys)
            .OrderBy(offset => offset)
            .ToList();

        ResearchSnapshotCompareRows.Clear();
        foreach (var offset in offsets)
        {
            valuesA.TryGetValue(offset, out var valueA);
            valuesB.TryGetValue(offset, out var valueB);
            ResearchSnapshotCompareRows.Add(new ResearchSnapshotCompareRowViewModel(
                offset,
                valuesA.ContainsKey(offset) ? valueA : null,
                valuesB.ContainsKey(offset) ? valueB : null));
        }

        var changedRows = ResearchSnapshotCompareRows
            .Where(row => row.IsChanged)
            .ToList();

        BuildDiscoveryReport(changedRows);
        var export = CreateComparisonExport(snapshotA, snapshotB, ResearchSnapshotCompareRows, ResearchDiscoveryReportLines);
        try
        {
            var exported = ResearchSnapshotStore.ExportComparison(export);
            ResearchSnapshotCompareStatusText.Text =
                $"Compared {snapshotA.Name} vs {snapshotB.Name}. Changed bytes: {changedRows.Count}. Exported {Path.GetFileName(exported.JsonPath)} and {Path.GetFileName(exported.CsvPath)}.";
        }
        catch (Exception ex)
        {
            ResearchSnapshotCompareStatusText.Text =
                $"Compared {snapshotA.Name} vs {snapshotB.Name}. Export failed: {ex.Message}";
        }

        SetStatus($"Compared saved snapshots. Changed bytes: {changedRows.Count}.", StatusKind.Connected);
    }

    private void CompareOwnershipDiscoverySnapshots(
        ResearchSnapshotDocument snapshotA,
        ResearchSnapshotDocument snapshotB)
    {
        var valuesA = FlattenSnapshot(snapshotA);
        var valuesB = FlattenSnapshot(snapshotB);
        var offsets = valuesA.Keys
            .Union(valuesB.Keys)
            .OrderBy(offset => offset)
            .ToList();
        var treatAfterAsPersisted = OwnershipDiscoveryPersistedAfterReloadCheckBox.IsChecked == true;
        var itemContext = InferOwnershipDiscoveryItemContext(valuesA, valuesB);

        OwnershipDiscoveryRows.Clear();
        foreach (var offset in offsets)
        {
            valuesA.TryGetValue(offset, out var valueA);
            valuesB.TryGetValue(offset, out var valueB);
            OwnershipDiscoveryRows.Add(new OwnershipDiscoveryRowViewModel(
                offset,
                valuesA.ContainsKey(offset) ? valueA : null,
                valuesB.ContainsKey(offset) ? valueB : null,
                treatAfterAsPersisted,
                itemContext));
        }

        BuildOwnershipDiscoveryReport(OwnershipDiscoveryRows);
        _lastOwnershipDiscoveryExport = CreateOwnershipDiscoveryExport(
            snapshotA,
            snapshotB,
            OwnershipDiscoveryRows,
            OwnershipDiscoveryReportLines,
            treatAfterAsPersisted);

        var changedCount = OwnershipDiscoveryRows.Count(row => row.IsChanged);
        var strongCandidates = OwnershipDiscoveryRows.Count(row => row.IsStrongCandidate);
        OwnershipDiscoveryStatusText.Text =
            $"Compared {snapshotA.Name} vs {snapshotB.Name}. Changed bytes: {changedCount}. Strong candidates: {strongCandidates}. Click Export Report to write JSON/CSV.";
        SetStatus($"Ownership Discovery compared. Strong candidates: {strongCandidates}.", StatusKind.Connected);
    }

    private static Dictionary<uint, byte> FlattenSnapshot(ResearchSnapshotDocument snapshot)
    {
        var values = new Dictionary<uint, byte>();
        foreach (var range in snapshot.Ranges)
        {
            for (var index = 0; index < range.Bytes.Length; index++)
            {
                values[range.StartOffset + (uint)index] = range.Bytes[index];
            }
        }

        return values;
    }

    private static string InferOwnershipDiscoveryItemContext(
        IReadOnlyDictionary<uint, byte> valuesA,
        IReadOnlyDictionary<uint, byte> valuesB)
    {
        var detectedItems = new List<string>();
        for (var slotIndex = 0; slotIndex < InventoryDefinitions.SlotCount; slotIndex++)
        {
            var offset = InventoryDefinitions.FirstSlotOffset + (uint)slotIndex;
            if (!valuesA.TryGetValue(offset, out var beforeValue) ||
                !valuesB.TryGetValue(offset, out var afterValue) ||
                beforeValue == afterValue ||
                afterValue == InventoryDefinitions.EmptyItemId)
            {
                continue;
            }

            var itemName = InventoryDefinitions.GetKnownItemName(afterValue);
            if (itemName != "-" && itemName != "Nothing")
            {
                detectedItems.Add(itemName);
            }
        }

        return detectedItems
            .Distinct(StringComparer.Ordinal)
            .Take(3)
            .Aggregate(string.Empty, (current, item) =>
                string.IsNullOrWhiteSpace(current) ? item : $"{current} / {item}");
    }

    private void BuildDiscoveryReport(IEnumerable<ResearchSnapshotCompareRowViewModel> changedRows)
    {
        ResearchDiscoveryReportLines.Clear();

        foreach (var row in changedRows)
        {
            if (row.SnapshotBDecode != "-")
            {
                ResearchDiscoveryReportLines.Add(
                    $"Potential item discovery: Offset {row.Offset} Before: {row.SnapshotAByte} After: {row.SnapshotBByte} Detected: {row.SnapshotBDecode}");
            }
            else
            {
                ResearchDiscoveryReportLines.Add(
                    $"Potential ownership/progression flag: Offset {row.Offset} Before: {row.SnapshotAByte} After: {row.SnapshotBByte}");
            }
        }

        if (ResearchDiscoveryReportLines.Count == 0)
        {
            ResearchDiscoveryReportLines.Add("No changed bytes found.");
        }
    }

    private void BuildOwnershipDiscoveryReport(IEnumerable<OwnershipDiscoveryRowViewModel> rows)
    {
        OwnershipDiscoveryReportLines.Clear();

        var changedRows = rows
            .Where(row => row.IsChanged)
            .OrderByDescending(row => row.Score)
            .ThenBy(row => row.OffsetValue)
            .ToList();

        foreach (var row in changedRows.Take(80))
        {
            var prefix = row.IsStrongCandidate ? "Strong candidate" : "Changed";
            OwnershipDiscoveryReportLines.Add(
                $"{prefix}: {row.Offset} {row.BeforeByte} -> {row.AfterByte}; score={row.Score}; {row.PotentialMeaning}");
        }

        if (changedRows.Count == 0)
        {
            OwnershipDiscoveryReportLines.Add("No changed bytes found.");
            return;
        }

        var visibleChanges = changedRows.Count(row => !row.IsOutsideVisibleInventorySlots);
        var strongCandidates = changedRows.Count(row => row.IsStrongCandidate);
        OwnershipDiscoveryReportLines.Insert(
            0,
            $"Summary: changed={changedRows.Count}; visible-inventory-changes={visibleChanges}; strong-candidates={strongCandidates}.");
    }

    private void BuildOwnershipCorrelationReport(
        IReadOnlyList<(string Path, OwnershipDiscoveryExport Export)> exports)
    {
        OwnershipCorrelationRows.Clear();
        if (exports.Count == 0)
        {
            OwnershipCorrelationStatusText.Text = "No ownership discovery JSON exports found.";
            SetStatus("No ownership discovery exports found for correlation.", StatusKind.Neutral);
            return;
        }

        var offsets = new Dictionary<uint, OwnershipCorrelationAccumulator>();
        foreach (var (path, export) in exports)
        {
            var itemGains = InferOwnershipDiscoveryItemGains(export);
            var context = itemGains.Count == 0
                ? $"{export.SnapshotAName} -> {export.SnapshotBName}"
                : string.Join(" / ", itemGains);
            var reportName = $"{Path.GetFileName(path)}: {export.SnapshotAName} -> {export.SnapshotBName}";
            var countedOffsets = new HashSet<uint>();

            foreach (var row in export.Rows.Where(row => row.Changed))
            {
                if (!TryParseResearchOffset(row.Offset, out var offset, out _) ||
                    !countedOffsets.Add(offset))
                {
                    continue;
                }

                if (!offsets.TryGetValue(offset, out var accumulator))
                {
                    accumulator = new OwnershipCorrelationAccumulator(offset);
                    offsets[offset] = accumulator;
                }

                accumulator.ChangedCount++;
                accumulator.ReportNames.Add(reportName);
                accumulator.AssociatedItemGains.Add(context);

                var bitChange = string.IsNullOrWhiteSpace(row.ChangedBits)
                    ? FormatChangedBits(row.BeforeValue, row.AfterValue)
                    : row.ChangedBits;
                if (bitChange != "-")
                {
                    accumulator.BitChanges.Add($"{context}: {bitChange}");
                }
            }
        }

        foreach (var row in offsets.Values
                     .OrderByDescending(offset => offset.ChangedCount)
                     .ThenBy(offset => offset.OffsetValue))
        {
            OwnershipCorrelationRows.Add(new OwnershipCorrelationRowViewModel(
                row.OffsetValue,
                row.ChangedCount,
                row.AssociatedItemGains,
                row.ReportNames,
                row.BitChanges));
        }

        OwnershipCorrelationStatusText.Text =
            $"Loaded {exports.Count} ownership discovery export(s). Correlated {OwnershipCorrelationRows.Count} changed offset(s).";
        SetStatus("Ownership correlation report built.", StatusKind.Connected);
    }

    private void ExportInventoryMapping()
    {
        try
        {
            Directory.CreateDirectory(ResearchSnapshotStore.ExportDirectory);
            var rows = InventoryMappingItems
                .Select(item => new InventoryMappingExportRow(
                    item.SlotNumber,
                    item.Offset,
                    item.RawValue,
                    item.DecodedItem,
                    item.DetectedVisualGroup,
                    item.SelectedResearchGroup,
                    item.RowNote,
                    item.ColumnNote,
                    item.Notes))
                .ToList();
            var export = new InventoryMappingExport(DateTimeOffset.Now, rows);
            var jsonPath = Path.Combine(ResearchSnapshotStore.ExportDirectory, "inventory-mapping.json");
            var csvPath = Path.Combine(ResearchSnapshotStore.ExportDirectory, "inventory-mapping.csv");

            File.WriteAllText(jsonPath, JsonSerializer.Serialize(export, ExportJsonOptions));
            File.WriteAllLines(csvPath, CreateInventoryMappingCsvLines(rows));

            SetStatus("Inventory mapping exported to logs/research.", StatusKind.Connected);
        }
        catch (Exception ex)
        {
            SetStatus($"Inventory mapping export failed: {ex.Message}", StatusKind.Warning);
        }
    }

    private static IEnumerable<string> CreateInventoryMappingCsvLines(IEnumerable<InventoryMappingExportRow> rows)
    {
        yield return "Slot,Offset,Raw Value,Decoded Item,Detected Visual Group,Research Group,Row Note,Column Note,Notes";
        foreach (var row in rows)
        {
            yield return string.Join(
                ",",
                Csv(row.SlotNumber.ToString(CultureInfo.InvariantCulture)),
                Csv(row.Offset),
                Csv(row.RawValue.ToString(CultureInfo.InvariantCulture)),
                Csv(row.DecodedItem),
                Csv(row.DetectedVisualGroup),
                Csv(row.ResearchGroup),
                Csv(row.RowNote),
                Csv(row.ColumnNote),
                Csv(row.Notes));
        }
    }

    private static string Csv(string value)
    {
        return "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }

    private static List<string> InferOwnershipDiscoveryItemGains(OwnershipDiscoveryExport export)
    {
        var itemGains = new List<string>();
        foreach (var row in export.Rows.Where(row => row.Changed && row.AfterValue.HasValue))
        {
            var afterValue = row.AfterValue.GetValueOrDefault();
            if (!TryParseResearchOffset(row.Offset, out var offset, out _) ||
                offset < InventoryDefinitions.FirstSlotOffset ||
                offset >= InventoryDefinitions.FirstSlotOffset + InventoryDefinitions.SlotCount ||
                afterValue == InventoryDefinitions.EmptyItemId ||
                afterValue is < byte.MinValue or > byte.MaxValue)
            {
                continue;
            }

            var itemName = InventoryDefinitions.GetKnownItemName((byte)afterValue);
            if (itemName != "-" && itemName != "Nothing")
            {
                itemGains.Add(itemName);
            }
        }

        return itemGains
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static string FormatChangedBits(int? beforeValue, int? afterValue)
    {
        if (!beforeValue.HasValue || !afterValue.HasValue)
        {
            return "n/a";
        }

        var changedMask = (byte)(beforeValue.Value ^ afterValue.Value);
        if (changedMask == 0)
        {
            return "-";
        }

        var bits = new List<string>();
        for (var bitIndex = 0; bitIndex < 8; bitIndex++)
        {
            if ((changedMask & (1 << bitIndex)) != 0)
            {
                bits.Add($"bit {bitIndex}");
            }
        }

        return string.Join(", ", bits);
    }

    private static ResearchComparisonExport CreateComparisonExport(
        ResearchSnapshotViewModel snapshotA,
        ResearchSnapshotViewModel snapshotB,
        IEnumerable<ResearchSnapshotCompareRowViewModel> rows,
        IEnumerable<string> discoveryReport)
    {
        return new ResearchComparisonExport
        {
            Timestamp = DateTimeOffset.Now,
            SnapshotAName = snapshotA.Name,
            SnapshotBName = snapshotB.Name,
            Rows = rows.Select(row => new ResearchComparisonExportRow
            {
                Offset = row.Offset,
                SnapshotAValue = row.SnapshotAValue,
                SnapshotBValue = row.SnapshotBValue,
                SnapshotADecode = row.SnapshotADecode,
                SnapshotBDecode = row.SnapshotBDecode,
                Difference = row.Difference,
                Changed = row.IsChanged
            }).ToList(),
            DiscoveryReport = discoveryReport.ToList()
        };
    }

    private static OwnershipDiscoveryExport CreateOwnershipDiscoveryExport(
        ResearchSnapshotDocument snapshotA,
        ResearchSnapshotDocument snapshotB,
        IEnumerable<OwnershipDiscoveryRowViewModel> rows,
        IEnumerable<string> report,
        bool treatSnapshotBAsPersistedAfterReload)
    {
        return new OwnershipDiscoveryExport
        {
            Timestamp = DateTimeOffset.Now,
            SnapshotAName = snapshotA.Name,
            SnapshotBName = snapshotB.Name,
            TreatSnapshotBAsPersistedAfterReload = treatSnapshotBAsPersistedAfterReload,
            Rows = rows.Select(row => new OwnershipDiscoveryExportRow
            {
                Offset = row.Offset,
                BeforeValue = row.BeforeValue,
                AfterValue = row.AfterValue,
                Changed = row.IsChanged,
                BeforeBinary = row.BeforeBinary,
                AfterBinary = row.AfterBinary,
                ChangedBits = row.ChangedBits,
                ChangedBitCount = row.ChangedBitCount,
                PersistedAfterReload = row.PersistedAfterReload,
                OutsideVisibleInventorySlots = row.IsOutsideVisibleInventorySlots,
                Score = row.Score,
                StrongCandidate = row.IsStrongCandidate,
                PotentialMeaning = row.PotentialMeaning
            }).ToList(),
            Report = report.ToList()
        };
    }

    private void AppendResearchRangeComparisonLog(byte[] currentBytes, int changedCount)
    {
        if (_researchRangeSnapshotBytes is null)
        {
            return;
        }

        var changedRows = Enumerable.Range(0, currentBytes.Length)
            .Where(index => _researchRangeSnapshotBytes[index] != currentBytes[index])
            .Select(index =>
            {
                var offset = _researchRangeSnapshotStart + (uint)index;
                var beforeValue = _researchRangeSnapshotBytes[index];
                var currentValue = currentBytes[index];
                return $"0x{offset:X}:{beforeValue}(0x{beforeValue:X2})->{currentValue}(0x{currentValue:X2}) " +
                       $"{DecodeKnownResearchByte(beforeValue)}=>{DecodeKnownResearchByte(currentValue)}";
            });

        var entry =
            $"{DateTimeOffset.Now:O} label=\"{_researchRangeSnapshotLabel}\" " +
            $"start=0x{_researchRangeSnapshotStart:X} length=0x{currentBytes.Length:X} " +
            $"changed={changedCount} changes=\"{string.Join("; ", changedRows)}\"";

        try
        {
            var logDirectory = Path.GetDirectoryName(ResearchLogPath);
            if (!string.IsNullOrWhiteSpace(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            File.AppendAllText(ResearchLogPath, entry + Environment.NewLine);
        }
        catch (Exception ex)
        {
            ResearchRangeStatusText.Text = $"Research log write failed: {ex.Message}";
        }
    }

    private void AppendCandidateTestingLog(
        string action,
        uint offset,
        ulong absoluteAddress,
        byte? previousValue,
        byte? requestedValue,
        byte? readbackValue,
        string status)
    {
        var entry =
            $"{DateTimeOffset.Now:O} action={action} offset=0x{offset:X} address=0x{absoluteAddress:X} " +
            $"previous={FormatCandidateLogByte(previousValue)} requested={FormatCandidateLogByte(requestedValue)} " +
            $"readback={FormatCandidateLogByte(readbackValue)} status=\"{status}\"";

        try
        {
            var logDirectory = Path.GetDirectoryName(CandidateTestingLogPath);
            if (!string.IsNullOrWhiteSpace(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            File.AppendAllText(CandidateTestingLogPath, entry + Environment.NewLine);
        }
        catch (Exception ex)
        {
            CandidateStatusText.Text = $"Candidate log write failed: {ex.Message}";
        }
    }

    private void SetResearchRangeInputs(string startOffset, string length, string label)
    {
        ResearchRangeStartOffsetText.Text = startOffset;
        ResearchRangeLengthText.Text = length;
        ResearchRangeLabelText.Text = label;
        ResearchRangeStatusText.Text = $"Loaded preset: {label}.";
    }

    private static string FormatOwnershipDiscoverySnapshotLabel(ResearchSnapshotDocument snapshot)
    {
        return $"{snapshot.Name} ({snapshot.Timestamp.ToLocalTime():g}, {snapshot.Ranges.Count} ranges)";
    }

    private static bool TryParseResearchOffset(string text, out uint offset, out string error)
    {
        offset = 0;
        error = string.Empty;

        var value = text.Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            error = "Enter offset";
            return false;
        }

        value = value
            .Replace("_playerbase", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("+", string.Empty, StringComparison.Ordinal)
            .Trim();

        var isHex =
            value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ||
            value.Any(character => character is >= 'A' and <= 'F' or >= 'a' and <= 'f');

        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            value = value[2..];
        }

        var style = isHex ? NumberStyles.HexNumber : NumberStyles.Integer;
        if (!uint.TryParse(value, style, CultureInfo.InvariantCulture, out offset))
        {
            error = "Invalid offset";
            return false;
        }

        return true;
    }

    private static bool TryParseResearchLength(string text, out int length, out string error)
    {
        length = 0;

        if (!TryParseResearchOffset(text, out var parsedLength, out error))
        {
            return false;
        }

        if (parsedLength is < 1 or > 4096)
        {
            error = "Length must be between 1 and 4096 bytes.";
            return false;
        }

        length = (int)parsedLength;
        return true;
    }

    private static bool TryParseCandidateValue(string text, out byte value, out string error)
    {
        value = 0;

        if (!TryParseResearchOffset(text, out var parsedValue, out error))
        {
            error = "Invalid candidate value";
            return false;
        }

        if (parsedValue > byte.MaxValue)
        {
            error = "Candidate value must be between 0 and 255 / 0xFF.";
            return false;
        }

        value = (byte)parsedValue;
        return true;
    }

    private static string FormatResearchByte(byte value)
    {
        return $"{value} / 0x{value:X2}";
    }

    private static string FormatCandidateLogByte(byte? value)
    {
        return value.HasValue ? $"{value.Value}(0x{value.Value:X2})" : "n/a";
    }

    private static string DecodeKnownResearchByte(byte value)
    {
        return InventoryDefinitions.GetKnownItemName(value);
    }

    private static string FormatByte(byte? value)
    {
        return value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "n/a";
    }

    private static string FormatNullableInt(int? value)
    {
        return value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "n/a";
    }

    private static string FormatEquipmentByte(byte? value)
    {
        return value.HasValue ? $"{value.Value} (0x{value.Value:X2})" : "n/a";
    }

    private static string FormatInventoryItemName(byte? value)
    {
        return value.HasValue ? InventoryDefinitions.GetItemName(value.Value) : "n/a";
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

    private void WritePoeSoulsRequestedValue()
    {
        if (!PoeSouls.TryGetClampedTarget(out var target, out var wasClamped, out var validationError))
        {
            SetStatus(validationError, StatusKind.Warning);
            return;
        }

        if (wasClamped)
        {
            PoeSouls.SetTargetValue(target);
        }

        if (_memory is null || !_playerBaseAddress.HasValue)
        {
            SetStatus("Not attached. Attach to Cemu and rescan before editing Poe Souls.", StatusKind.Neutral);
            AppendCollectiblesDiagnostic(
                PoeSouls.Name,
                previousValue: null,
                desiredValue: target,
                readbackValue: null,
                "not-attached");
            return;
        }

        int? previousValue = null;
        if (PoeSouls.CurrentNumericValue.HasValue)
        {
            previousValue = PoeSouls.CurrentNumericValue.Value;
        }
        else if (TryReadIntValue(PoeSouls.Definition, out var readPreviousValue, out _))
        {
            previousValue = readPreviousValue;
        }

        var writeSucceeded = WriteValue(PoeSouls, target, quiet: true);
        int? readbackValue = null;
        var diagnosticStatus = "write-failed";

        if (writeSucceeded && TryReadIntValue(PoeSouls.Definition, out var readValue, out var readError))
        {
            readbackValue = readValue;
            PoeSouls.SetCurrentValue(readValue, initializeTarget: false);
            if (readValue == target)
            {
                diagnosticStatus = "verified";
                SetStatus($"Poe Souls set to {target}.", StatusKind.Connected);
            }
            else
            {
                diagnosticStatus = "readback-mismatch";
                SetStatus($"Poe Souls write failed: expected {target} but read {readValue}.", StatusKind.Warning);
            }
        }
        else if (writeSucceeded)
        {
            diagnosticStatus = "readback-failed";
            SetStatus("Poe Souls was written, but readback verification failed.", StatusKind.Warning);
        }

        AppendCollectiblesDiagnostic(
            PoeSouls.Name,
            previousValue,
            target,
            readbackValue,
            diagnosticStatus);

        RefreshValue(PoeSouls);
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

        foreach (var bottleSlot in BottleSlots)
        {
            bottleSlot.MarkNotRead();
        }

        foreach (var item in InventoryOwnershipItems)
        {
            item.MarkNotRead();
        }

        foreach (var item in FixedInventoryItems)
        {
            item.MarkNotRead();
        }

        foreach (var item in InventoryRemovalItems)
        {
            item.MarkNotRead();
        }

        foreach (var item in InventoryMappingItems)
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

        foreach (var bottleSlot in BottleSlots)
        {
            bottleSlot.MarkNotRead();
        }

        foreach (var item in InventoryOwnershipItems)
        {
            item.MarkNotRead();
        }

        foreach (var item in FixedInventoryItems)
        {
            item.MarkNotRead();
        }

        foreach (var item in InventoryRemovalItems)
        {
            item.MarkNotRead();
        }

        foreach (var item in InventoryMappingItems)
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
        _candidatePreviousOffset = null;
        _candidatePreviousValue = null;
        CandidateAbsoluteAddressText.Text = "-";
        CandidateCurrentByteText.Text = "Not read";
        CandidatePreviousByteText.Text = "Not captured";
        CandidateStatusText.Text = "Ready. No automatic writes.";
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

    private void ApplyTheme(bool darkMode)
    {
        if (darkMode)
        {
            SetBrush("AppBackgroundBrush", 0x1E, 0x1F, 0x22);
            SetBrush("PanelBackgroundBrush", 0x27, 0x29, 0x2D);
            SetBrush("ControlBackgroundBrush", 0x33, 0x35, 0x3A);
            SetBrush("ReadOnlyBackgroundBrush", 0x2B, 0x2D, 0x31);
            SetBrush("TextBrush", 0xF2, 0xF2, 0xF2);
            SetBrush("SecondaryTextBrush", 0xC4, 0xC7, 0xCC);
            SetBrush("BorderBrush", 0x54, 0x58, 0x60);
            SetBrush("WarningBackgroundBrush", 0x3B, 0x31, 0x1C);
            SetBrush("WarningBorderBrush", 0xC4, 0x8A, 0x2B);
            SetBrush("WarningTextBrush", 0xF2, 0xD3, 0x92);
            SetBrush("ChangedRowBackgroundBrush", 0x4B, 0x42, 0x20);
            SetBrush("StrongCandidateBackgroundBrush", 0x1F, 0x45, 0x31);
            SetBrush("ChangedBitBackgroundBrush", 0x8A, 0x66, 0x19);
            SetBrush("StatusNeutralBrush", 0xA5, 0xA8, 0xAE);
            SetBrush("TabSelectedBackgroundBrush", 0x33, 0x35, 0x3A);
            SetBrush("TabUnselectedBackgroundBrush", 0x22, 0x24, 0x28);
            SetBrush("TabHoverBackgroundBrush", 0x2D, 0x30, 0x36);
            SetBrush("TabSelectedTextBrush", 0xFF, 0xFF, 0xFF);
            SetBrush("DisabledTextBrush", 0x8F, 0x94, 0x9B);
        }
        else
        {
            SetBrush("AppBackgroundBrush", 0xF0, 0xF0, 0xF0);
            SetBrush("PanelBackgroundBrush", 0xFA, 0xFA, 0xFA);
            SetBrush("ControlBackgroundBrush", 0xFF, 0xFF, 0xFF);
            SetBrush("ReadOnlyBackgroundBrush", 0xF7, 0xF7, 0xF7);
            SetBrush("TextBrush", 0x11, 0x11, 0x11);
            SetBrush("SecondaryTextBrush", 0x55, 0x55, 0x55);
            SetBrush("BorderBrush", 0xB8, 0xB8, 0xB8);
            SetBrush("WarningBackgroundBrush", 0xFF, 0xF8, 0xE6);
            SetBrush("WarningBorderBrush", 0xD9, 0xA2, 0x3A);
            SetBrush("WarningTextBrush", 0x6F, 0x47, 0x00);
            SetBrush("ChangedRowBackgroundBrush", 0xFF, 0xF3, 0xC4);
            SetBrush("StrongCandidateBackgroundBrush", 0xDF, 0xF4, 0xE7);
            SetBrush("ChangedBitBackgroundBrush", 0xFF, 0xC6, 0x4D);
            SetBrush("StatusNeutralBrush", 0x80, 0x80, 0x80);
            SetBrush("TabSelectedBackgroundBrush", 0xFF, 0xFF, 0xFF);
            SetBrush("TabUnselectedBackgroundBrush", 0xE8, 0xE8, 0xE8);
            SetBrush("TabHoverBackgroundBrush", 0xF4, 0xF4, 0xF4);
            SetBrush("TabSelectedTextBrush", 0x11, 0x11, 0x11);
            SetBrush("DisabledTextBrush", 0x77, 0x77, 0x77);
        }
    }

    private void SetBrush(string key, byte red, byte green, byte blue)
    {
        Resources[key] = new SolidColorBrush(Color.FromRgb(red, green, blue));
    }

    private static bool LoadDarkModePreference()
    {
        try
        {
            return File.Exists(ThemePreferencePath) &&
                string.Equals(File.ReadAllText(ThemePreferencePath).Trim(), "dark", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static void SaveDarkModePreference(bool darkMode)
    {
        try
        {
            Directory.CreateDirectory(UserSettingsDirectory);
            File.WriteAllText(ThemePreferencePath, darkMode ? "dark" : "light");
        }
        catch
        {
            // Theme persistence should never interfere with trainer startup or memory tools.
        }
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

    private readonly record struct OwnershipDiscoveryRangePreset(string Label, uint StartOffset, int Length);

    private sealed record InventoryMappingExport(
        DateTimeOffset Timestamp,
        IReadOnlyList<InventoryMappingExportRow> Rows);

    private sealed record InventoryMappingExportRow(
        int SlotNumber,
        string Offset,
        byte RawValue,
        string DecodedItem,
        string DetectedVisualGroup,
        string ResearchGroup,
        string RowNote,
        string ColumnNote,
        string Notes);

    private sealed class OwnershipCorrelationAccumulator
    {
        public OwnershipCorrelationAccumulator(uint offsetValue)
        {
            OffsetValue = offsetValue;
        }

        public uint OffsetValue { get; }

        public int ChangedCount { get; set; }

        public HashSet<string> AssociatedItemGains { get; } = new(StringComparer.Ordinal);

        public HashSet<string> ReportNames { get; } = new(StringComparer.Ordinal);

        public HashSet<string> BitChanges { get; } = new(StringComparer.Ordinal);
    }

    private enum StatusKind
    {
        Neutral,
        Working,
        Connected,
        Warning
    }
}
