using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Reflection;
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
    private readonly DispatcherTimer _hiddenSkillsLiveWatchTimer;
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
    private bool _suppressHiddenSkillDependencyEnforcement;
    private byte? _researchSnapshotValue;
    private uint? _researchSnapshotOffset;
    private byte[]? _researchRangeSnapshotBytes;
    private uint _researchRangeSnapshotStart;
    private string _researchRangeSnapshotLabel = string.Empty;
    private byte[]? _goldenBugsBeforeSnapshotBytes;
    private byte[]? _goldenBugsAfterSnapshotBytes;
    private uint _goldenBugsBeforeSnapshotStart;
    private uint _goldenBugsAfterSnapshotStart;
    private DateTimeOffset? _goldenBugsBeforeCapturedAt;
    private DateTimeOffset? _goldenBugsAfterCapturedAt;
    private GoldenBugsResearchExport? _lastGoldenBugsResearchExport;
    private byte[]? _questItemsBeforeSnapshotBytes;
    private byte[]? _questItemsAfterSnapshotBytes;
    private uint _questItemsBeforeSnapshotStart;
    private uint _questItemsAfterSnapshotStart;
    private DateTimeOffset? _questItemsBeforeCapturedAt;
    private DateTimeOffset? _questItemsAfterCapturedAt;
    private QuestItemsCaptureDocument? _questItemsCaptureA;
    private QuestItemsCaptureDocument? _questItemsCaptureB;
    private string _questItemsCaptureAPath = string.Empty;
    private string _questItemsCaptureBPath = string.Empty;
    private QuestItemsResearchExport? _lastQuestItemsResearchExport;
    private List<QuestItemsCandidateRowViewModel> _questItemsAllCandidateRows = [];
    private QuestItemsCandidateRankingExport? _lastQuestItemsCandidateRankingExport;
    private QuestItemsCandidateGroupsExport? _lastQuestItemsCandidateGroupsExport;
    private QuestItemsMultiCaptureAnalysisExport? _lastQuestItemsMultiCaptureAnalysisExport;
    private readonly List<QuestItemsLoadedCapture> _questItemsMultiCaptures = [];
    private readonly Dictionary<uint, int> _questItemsMultiAppearanceCounts = [];
    private byte[]? _hiddenSkillsBeforeSnapshotBytes;
    private byte[]? _hiddenSkillsAfterSnapshotBytes;
    private uint _hiddenSkillsBeforeSnapshotStart;
    private uint _hiddenSkillsAfterSnapshotStart;
    private DateTimeOffset? _hiddenSkillsBeforeCapturedAt;
    private DateTimeOffset? _hiddenSkillsAfterCapturedAt;
    private HiddenSkillsResearchExport? _lastHiddenSkillsResearchExport;
    private HiddenSkillsMultiCaptureExport? _lastHiddenSkillsMultiCaptureExport;
    private HiddenSkillsRegionAnalysisExport? _lastHiddenSkillsRegionAnalysisExport;
    private List<HiddenSkillsResearchRowViewModel> _hiddenSkillsAllResearchRows = [];
    private readonly Dictionary<uint, bool> _hiddenSkillsPinnedOffsets = [];
    private readonly HiddenSkillsLoadedCapture?[] _hiddenSkillsMultiCaptures = new HiddenSkillsLoadedCapture?[6];
    private HiddenSkillsCaptureDocument? _hiddenSkillsRegionCaptureA;
    private HiddenSkillsCaptureDocument? _hiddenSkillsRegionCaptureB;
    private byte[]? _hiddenSkillsRegionCaptureABytes;
    private byte[]? _hiddenSkillsRegionCaptureBBytes;
    private string _hiddenSkillsRegionCaptureAPath = string.Empty;
    private string _hiddenSkillsRegionCaptureBPath = string.Empty;
    private byte[]? _hiddenSkillsLiveWatchPreviousBytes;
    private uint _hiddenSkillsLiveWatchStartOffset;
    private int _hiddenSkillsLiveWatchLength;
    private DateTimeOffset? _hiddenSkillsLiveWatchStartedAt;
    private readonly Dictionary<uint, int> _hiddenSkillsLiveWatchChangeCounts = [];
    private byte[]? _goldenBugsBitfieldRestoreSnapshotBytes;
    private DateTimeOffset? _goldenBugsBitfieldRestoreSnapshotCapturedAt;
    private byte[]? _goldenBugsEditorRestoreSnapshotBytes;
    private DateTimeOffset? _goldenBugsEditorRestoreSnapshotCapturedAt;
    private byte[]? _hiddenSkillsRestoreSnapshotBytes;
    private DateTimeOffset? _hiddenSkillsRestoreSnapshotCapturedAt;
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

    private static readonly IReadOnlyList<string> GoldenBugCandidateNames = GoldenBugsDefinitions.BugNames;

    private static readonly IReadOnlyList<string> QuestItemsCaptureTypes =
    [
        "Forest Temple",
        "Goron Mines",
        "Lakebed Temple",
        "Arbiter's Grounds",
        "Snowpeak Ruins",
        "Temple of Time",
        "City in the Sky",
        "Palace of Twilight",
        "Hyrule Castle",
        "Custom"
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
        BombSlots = new ObservableCollection<BombSlotViewModel>(
            BombSlotDefinitions.Slots.Select(slot => new BombSlotViewModel(slot)));
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
        BombSlotEditorDiagnostics = [];
        CollectiblesDiagnostics = [];
        GoldenBugsResearchRows = [];
        QuestItemsResearchRows = [];
        QuestItemsCandidateRows = [];
        QuestItemsCandidateGroups = [];
        QuestItemsMultiCaptureRows = [];
        GoldenBugsCandidateList = new ObservableCollection<string>(
            GoldenBugCandidateNames.Select((name, index) => $"{index + 1}. {name}"));
        GoldenBugsBitRows = new ObservableCollection<GoldenBugBitViewModel>(
            GoldenBugsDefinitions.Bits.Select(bit => new GoldenBugBitViewModel(bit)));
        GoldenBugsEditorRows = new ObservableCollection<GoldenBugBitViewModel>(
            GoldenBugsDefinitions.OwnershipBits
                .Select(definition => GoldenBugsBitRows.First(bit =>
                    bit.OffsetValue == definition.Offset && bit.Bit == definition.Bit)));
        GoldenBugsBitfieldDiagnostics = [];
        GoldenBugsEditorDiagnostics = [];
        HiddenSkillsEditorRows = new ObservableCollection<HiddenSkillViewModel>(
            HiddenSkillsDefinitions.Skills.Select(skill => new HiddenSkillViewModel(skill)));
        foreach (var skill in HiddenSkillsEditorRows)
        {
            skill.PropertyChanged += HiddenSkill_PropertyChanged;
        }

        HiddenSkillsEditorDiagnostics = [];
        HiddenSkillsResearchRows = [];
        HiddenSkillsCandidateGroups = [];
        HiddenSkillsMultiCaptureCandidates = [];
        HiddenSkillsRegionAnalysisRows = [];
        HiddenSkillsLiveWatchRows = [];
        HiddenSkillsEventMarkers = [];

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

        _hiddenSkillsLiveWatchTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _hiddenSkillsLiveWatchTimer.Tick += HiddenSkillsLiveWatchTimer_Tick;
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

    private void HiddenSkill_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_suppressHiddenSkillDependencyEnforcement ||
            e.PropertyName != nameof(HiddenSkillViewModel.IsOwnedDesired) ||
            sender is not HiddenSkillViewModel skill)
        {
            return;
        }

        EnforceHiddenSkillProgressionFrom(skill);
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

    private static string BombSlotEditorLogPath =>
        Path.Combine(AppContext.BaseDirectory, "logs", "bomb-slot-editor.log");

    private static string QuestItemsResearchLogPath =>
        Path.Combine(AppContext.BaseDirectory, "logs", "quest-items-research.log");

    private static string GoldenBugsResearchLogPath =>
        Path.Combine(AppContext.BaseDirectory, "logs", "golden-bugs-research.log");

    private static string GoldenBugsBitfieldTestingLogPath =>
        Path.Combine(AppContext.BaseDirectory, "logs", "golden-bugs-bitfield-testing.log");

    private static string GoldenBugsEditorLogPath =>
        Path.Combine(AppContext.BaseDirectory, "logs", "golden-bugs-editor.log");

    private static string HiddenSkillsResearchLogPath =>
        Path.Combine(AppContext.BaseDirectory, "logs", "hidden-skills-research.log");

    private static string HiddenSkillsBitTestingLogPath =>
        Path.Combine(AppContext.BaseDirectory, "logs", "hidden-skills-bit-testing.log");

    private static string HiddenSkillsEditorLogPath =>
        Path.Combine(AppContext.BaseDirectory, "logs", "hidden-skills-editor.log");

    private static string HiddenSkillsLiveWatchLogPath =>
        Path.Combine(AppContext.BaseDirectory, "logs", "hidden-skills-live-watch.log");

    private static string HiddenSkillsCaptureDirectory =>
        Path.Combine(ResearchSnapshotStore.ExportDirectory, "hidden-skills-captures");

    private static string QuestSearchDirectory =>
        Path.Combine(ResearchSnapshotStore.ExportDirectory, "quest-search");

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

    public ObservableCollection<BombSlotViewModel> BombSlots { get; }

    public ObservableCollection<InventoryOwnershipItemViewModel> InventoryOwnershipItems { get; }

    public ObservableCollection<InventoryFixedSlotViewModel> FixedInventoryItems { get; }

    public ObservableCollection<InventoryRemovalItemViewModel> InventoryRemovalItems { get; }

    public ObservableCollection<InventoryMappingSlotViewModel> InventoryMappingItems { get; }

    public ObservableCollection<string> InventoryDiagnostics { get; }

    public ObservableCollection<string> InventoryOwnershipDiagnostics { get; }

    public ObservableCollection<string> InventoryRemovalDiagnostics { get; }

    public ObservableCollection<string> InventoryCheckboxTestingDiagnostics { get; }

    public ObservableCollection<string> BottleEditorDiagnostics { get; }

    public ObservableCollection<string> BombSlotEditorDiagnostics { get; }

    public ObservableCollection<string> CollectiblesDiagnostics { get; }

    public ObservableCollection<GoldenBugResearchRowViewModel> GoldenBugsResearchRows { get; }

    public ObservableCollection<string> GoldenBugsCandidateList { get; }

    public ObservableCollection<GoldenBugBitViewModel> GoldenBugsBitRows { get; }

    public ObservableCollection<GoldenBugBitViewModel> GoldenBugsEditorRows { get; }

    public ObservableCollection<string> GoldenBugsBitfieldDiagnostics { get; }

    public ObservableCollection<string> GoldenBugsEditorDiagnostics { get; }

    public ObservableCollection<QuestItemsResearchRowViewModel> QuestItemsResearchRows { get; }

    public ObservableCollection<QuestItemsCandidateRowViewModel> QuestItemsCandidateRows { get; }

    public ObservableCollection<QuestItemsCandidateGroupViewModel> QuestItemsCandidateGroups { get; }

    public ObservableCollection<QuestItemsMultiCaptureRowViewModel> QuestItemsMultiCaptureRows { get; }

    public ObservableCollection<HiddenSkillViewModel> HiddenSkillsEditorRows { get; }

    public ObservableCollection<string> HiddenSkillsEditorDiagnostics { get; }

    public ObservableCollection<HiddenSkillsResearchRowViewModel> HiddenSkillsResearchRows { get; }

    public ObservableCollection<HiddenSkillsCandidateGroupViewModel> HiddenSkillsCandidateGroups { get; }

    public ObservableCollection<HiddenSkillsMultiCaptureCandidateViewModel> HiddenSkillsMultiCaptureCandidates { get; }

    public ObservableCollection<HiddenSkillsRegionAnalysisRowViewModel> HiddenSkillsRegionAnalysisRows { get; }

    public ObservableCollection<HiddenSkillsLiveWatchRowViewModel> HiddenSkillsLiveWatchRows { get; }

    public ObservableCollection<HiddenSkillsEventMarkerViewModel> HiddenSkillsEventMarkers { get; }

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

    private void GoldenBugsCaptureBefore_Click(object sender, RoutedEventArgs e)
    {
        if (!TryReadGoldenBugsResearchRange(out var startOffset, out var bytes, out _))
        {
            return;
        }

        _goldenBugsBeforeSnapshotStart = startOffset;
        _goldenBugsBeforeSnapshotBytes = bytes;
        _goldenBugsBeforeCapturedAt = DateTimeOffset.Now;
        GoldenBugsResearchRows.Clear();
        _lastGoldenBugsResearchExport = null;
        GoldenBugsResearchStatusText.Text =
            $"Captured Before at _playerbase+0x{startOffset:X}, length 0x{bytes.Length:X}.";
        AppendGoldenBugsResearchLog(
            $"action=capture-before start=0x{startOffset:X} length=0x{bytes.Length:X} bytes=\"{FormatByteArray(bytes)}\"");
        SetStatus("Golden Bugs Before snapshot captured.", StatusKind.Connected);
    }

    private void GoldenBugsCaptureAfter_Click(object sender, RoutedEventArgs e)
    {
        if (!TryReadGoldenBugsResearchRange(out var startOffset, out var bytes, out _))
        {
            return;
        }

        _goldenBugsAfterSnapshotStart = startOffset;
        _goldenBugsAfterSnapshotBytes = bytes;
        _goldenBugsAfterCapturedAt = DateTimeOffset.Now;
        _lastGoldenBugsResearchExport = null;
        GoldenBugsResearchStatusText.Text =
            $"Captured After at _playerbase+0x{startOffset:X}, length 0x{bytes.Length:X}.";
        AppendGoldenBugsResearchLog(
            $"action=capture-after start=0x{startOffset:X} length=0x{bytes.Length:X} bytes=\"{FormatByteArray(bytes)}\"");
        SetStatus("Golden Bugs After snapshot captured.", StatusKind.Connected);
    }

    private void GoldenBugsCompare_Click(object sender, RoutedEventArgs e)
    {
        if (_goldenBugsBeforeSnapshotBytes is null || _goldenBugsAfterSnapshotBytes is null)
        {
            GoldenBugsResearchStatusText.Text = "Capture Before and After snapshots before comparing.";
            SetStatus("Capture Golden Bugs Before and After snapshots first.", StatusKind.Neutral);
            return;
        }

        if (_goldenBugsBeforeSnapshotStart != _goldenBugsAfterSnapshotStart ||
            _goldenBugsBeforeSnapshotBytes.Length != _goldenBugsAfterSnapshotBytes.Length)
        {
            GoldenBugsResearchStatusText.Text = "Before and After ranges must use the same start and length.";
            SetStatus("Golden Bugs research ranges do not match.", StatusKind.Warning);
            return;
        }

        GoldenBugsResearchRows.Clear();
        var changedByteCount = 0;
        var changedBitCount = 0;
        for (var byteIndex = 0; byteIndex < _goldenBugsBeforeSnapshotBytes.Length; byteIndex++)
        {
            var beforeValue = _goldenBugsBeforeSnapshotBytes[byteIndex];
            var afterValue = _goldenBugsAfterSnapshotBytes[byteIndex];
            if (beforeValue == afterValue)
            {
                continue;
            }

            changedByteCount++;
            var changedMask = beforeValue ^ afterValue;
            for (var bitIndex = 0; bitIndex < 8; bitIndex++)
            {
                if ((changedMask & (1 << bitIndex)) == 0)
                {
                    continue;
                }

                changedBitCount++;
                var offset = _goldenBugsBeforeSnapshotStart + (uint)byteIndex;
                var mapping = GoldenBugsDefinitions.Bits.FirstOrDefault(bit =>
                    bit.Offset == offset && bit.Bit == bitIndex);
                var candidateIndex = mapping?.ConfirmedBugName is null
                    ? 0
                    : GetGoldenBugReferenceIndex(mapping.ConfirmedBugName);
                var candidateName = mapping?.ConfirmedBugName ?? "Unmapped research bit";
                GoldenBugsResearchRows.Add(new GoldenBugResearchRowViewModel(
                    offset,
                    beforeValue,
                    afterValue,
                    byteIndex,
                    bitIndex,
                    candidateIndex,
                    candidateName));
            }
        }

        _lastGoldenBugsResearchExport = CreateGoldenBugsResearchExport(changedByteCount, changedBitCount);
        GoldenBugsResearchStatusText.Text =
            $"Compared Golden Bugs snapshots. Changed bytes: {changedByteCount}. Changed bits: {changedBitCount}. Confirmed ownership names are shown when mapped.";
        var changes = string.Join(
            "; ",
            GoldenBugsResearchRows.Select(row =>
                $"{row.Offset}:{row.BeforeValue}(0x{row.BeforeValue:X2})->{row.AfterValue}(0x{row.AfterValue:X2}) {row.ChangedBits} candidate={row.CandidateBugIndex} {row.CandidateBugName}"));
        AppendGoldenBugsResearchLog(
            $"action=compare start=0x{_goldenBugsBeforeSnapshotStart:X} length=0x{_goldenBugsBeforeSnapshotBytes.Length:X} changed-bytes={changedByteCount} changed-bits={changedBitCount} changes=\"{changes}\"");
        SetStatus("Golden Bugs research snapshots compared.", StatusKind.Connected);
    }

    private void GoldenBugsExportReport_Click(object sender, RoutedEventArgs e)
    {
        if (_lastGoldenBugsResearchExport is null)
        {
            GoldenBugsResearchStatusText.Text = "Compare snapshots before exporting a Golden Bugs report.";
            SetStatus("Compare Golden Bugs snapshots before exporting.", StatusKind.Neutral);
            return;
        }

        try
        {
            Directory.CreateDirectory(ResearchSnapshotStore.ExportDirectory);
            var baseName = $"{_lastGoldenBugsResearchExport.Timestamp:yyyyMMdd_HHmmss}_golden-bugs-research";
            var jsonPath = Path.Combine(ResearchSnapshotStore.ExportDirectory, baseName + ".json");
            var csvPath = Path.Combine(ResearchSnapshotStore.ExportDirectory, baseName + ".csv");
            File.WriteAllText(jsonPath, JsonSerializer.Serialize(_lastGoldenBugsResearchExport, ExportJsonOptions));
            File.WriteAllLines(csvPath, CreateGoldenBugsResearchCsvLines(_lastGoldenBugsResearchExport.Rows));

            GoldenBugsResearchStatusText.Text =
                $"Exported {Path.GetFileName(jsonPath)} and {Path.GetFileName(csvPath)}.";
            AppendGoldenBugsResearchLog(
                $"action=export json=\"{jsonPath}\" csv=\"{csvPath}\" rows={_lastGoldenBugsResearchExport.Rows.Count}");
            SetStatus("Golden Bugs research report exported to logs/research.", StatusKind.Connected);
        }
        catch (Exception ex)
        {
            GoldenBugsResearchStatusText.Text = $"Export failed: {ex.Message}";
            SetStatus("Golden Bugs research export failed.", StatusKind.Warning);
        }
    }

    private void QuestItemsCaptureBefore_Click(object sender, RoutedEventArgs e)
    {
        if (!TryReadQuestItemsResearchRange(out var startOffset, out var bytes, out _))
        {
            return;
        }

        _questItemsBeforeSnapshotStart = startOffset;
        _questItemsBeforeSnapshotBytes = bytes;
        _questItemsBeforeCapturedAt = DateTimeOffset.Now;
        _questItemsCaptureA = CreateQuestItemsCaptureDocument(
            _questItemsBeforeCapturedAt.Value,
            BuildQuestItemsCaptureLabel("Before"),
            startOffset,
            bytes,
            GetQuestItemsCaptureNotes());
        _questItemsCaptureAPath = string.Empty;
        QuestItemsResearchRows.Clear();
        _lastQuestItemsResearchExport = null;
        TryWriteQuestItemsSnapshotExport(
            "quest-items-before.json",
            _questItemsCaptureA.Label,
            startOffset,
            bytes,
            _questItemsBeforeCapturedAt);
        UpdateQuestItemsCaptureSummary();

        QuestItemsResearchStatusText.Text =
            $"Captured Before \"{_questItemsCaptureA.Label}\" at _playerbase+0x{startOffset:X}, length 0x{bytes.Length:X}. Saved quest-items-before.json.";
        AppendQuestItemsResearchLog(
            $"action=capture-before label=\"{_questItemsCaptureA.Label}\" start=0x{startOffset:X} length=0x{bytes.Length:X} bytes=\"{FormatByteArray(bytes)}\"");
        SetStatus("Quest Items Before snapshot captured.", StatusKind.Connected);
    }

    private void QuestItemsCaptureAfter_Click(object sender, RoutedEventArgs e)
    {
        if (!TryReadQuestItemsResearchRange(out var startOffset, out var bytes, out _))
        {
            return;
        }

        _questItemsAfterSnapshotStart = startOffset;
        _questItemsAfterSnapshotBytes = bytes;
        _questItemsAfterCapturedAt = DateTimeOffset.Now;
        _questItemsCaptureB = CreateQuestItemsCaptureDocument(
            _questItemsAfterCapturedAt.Value,
            BuildQuestItemsCaptureLabel("After"),
            startOffset,
            bytes,
            GetQuestItemsCaptureNotes());
        _questItemsCaptureBPath = string.Empty;
        _lastQuestItemsResearchExport = null;
        TryWriteQuestItemsSnapshotExport(
            "quest-items-after.json",
            _questItemsCaptureB.Label,
            startOffset,
            bytes,
            _questItemsAfterCapturedAt);
        UpdateQuestItemsCaptureSummary();

        QuestItemsResearchStatusText.Text =
            $"Captured After \"{_questItemsCaptureB.Label}\" at _playerbase+0x{startOffset:X}, length 0x{bytes.Length:X}. Saved quest-items-after.json.";
        AppendQuestItemsResearchLog(
            $"action=capture-after label=\"{_questItemsCaptureB.Label}\" start=0x{startOffset:X} length=0x{bytes.Length:X} bytes=\"{FormatByteArray(bytes)}\"");
        SetStatus("Quest Items After snapshot captured.", StatusKind.Connected);
    }

    private void QuestItemsSaveCurrentCapture_Click(object sender, RoutedEventArgs e)
    {
        if (!TryReadQuestItemsResearchRange(out var startOffset, out var bytes, out _))
        {
            return;
        }

        var timestamp = DateTimeOffset.Now;
        var label = BuildQuestItemsCaptureLabel("Current");
        var path = TrySaveQuestItemsCapture(label, startOffset, bytes, timestamp, GetQuestItemsCaptureNotes());
        if (!string.IsNullOrWhiteSpace(path))
        {
            QuestItemsResearchStatusText.Text =
                $"Saved current Quest Items capture \"{label}\" to {Path.GetFileName(path)}.";
            AppendQuestItemsResearchLog(
                $"action=save-current label=\"{label}\" path=\"{path}\" start=0x{startOffset:X} length=0x{bytes.Length:X}");
            SetStatus("Quest Items capture saved.", StatusKind.Connected);
        }
    }

    private void QuestItemsSaveBeforeCapture_Click(object sender, RoutedEventArgs e)
    {
        SaveQuestItemsCapturedSlot(
            "A",
            _questItemsBeforeSnapshotStart,
            _questItemsBeforeSnapshotBytes,
            _questItemsBeforeCapturedAt,
            _questItemsCaptureA);
    }

    private void QuestItemsSaveAfterCapture_Click(object sender, RoutedEventArgs e)
    {
        SaveQuestItemsCapturedSlot(
            "B",
            _questItemsAfterSnapshotStart,
            _questItemsAfterSnapshotBytes,
            _questItemsAfterCapturedAt,
            _questItemsCaptureB);
    }

    private void QuestItemsLoadCaptureA_Click(object sender, RoutedEventArgs e)
    {
        if (!TryLoadQuestItemsCapture("A", out var capture, out var bytes, out var path))
        {
            return;
        }

        _questItemsCaptureA = capture;
        _questItemsCaptureAPath = path;
        _questItemsBeforeSnapshotStart = capture.StartOffset;
        _questItemsBeforeSnapshotBytes = bytes;
        _questItemsBeforeCapturedAt = capture.Timestamp;
        _lastQuestItemsResearchExport = null;
        QuestItemsResearchRows.Clear();
        UpdateQuestItemsCaptureSummary();

        QuestItemsResearchStatusText.Text =
            $"Loaded Capture A \"{capture.Label}\" from {Path.GetFileName(path)}.";
        AppendQuestItemsResearchLog(
            $"action=load-capture-a label=\"{capture.Label}\" path=\"{path}\" start=0x{capture.StartOffset:X} length=0x{capture.Length:X}");
        SetStatus("Quest Items Capture A loaded.", StatusKind.Connected);
    }

    private void QuestItemsLoadCaptureB_Click(object sender, RoutedEventArgs e)
    {
        if (!TryLoadQuestItemsCapture("B", out var capture, out var bytes, out var path))
        {
            return;
        }

        _questItemsCaptureB = capture;
        _questItemsCaptureBPath = path;
        _questItemsAfterSnapshotStart = capture.StartOffset;
        _questItemsAfterSnapshotBytes = bytes;
        _questItemsAfterCapturedAt = capture.Timestamp;
        _lastQuestItemsResearchExport = null;
        QuestItemsResearchRows.Clear();
        UpdateQuestItemsCaptureSummary();

        QuestItemsResearchStatusText.Text =
            $"Loaded Capture B \"{capture.Label}\" from {Path.GetFileName(path)}.";
        AppendQuestItemsResearchLog(
            $"action=load-capture-b label=\"{capture.Label}\" path=\"{path}\" start=0x{capture.StartOffset:X} length=0x{capture.Length:X}");
        SetStatus("Quest Items Capture B loaded.", StatusKind.Connected);
    }

    private void QuestItemsCompareLoadedCaptures_Click(object sender, RoutedEventArgs e)
    {
        CompareQuestItemsSnapshots("compare-loaded");
    }

    private void QuestItemsAnalyzeLoadedCaptures_Click(object sender, RoutedEventArgs e)
    {
        if (QuestItemsResearchRows.Count == 0)
        {
            CompareQuestItemsSnapshots("analyze-loaded");
        }

        BuildQuestItemsCandidateAnalysis("analyze-loaded");
    }

    private void QuestItemsCandidateFilter_Changed(object sender, RoutedEventArgs e)
    {
        ApplyQuestItemsCandidateFilters();
    }

    private void QuestItemsExportCandidateRanking_Click(object sender, RoutedEventArgs e)
    {
        ExportQuestItemsCandidateRanking();
    }

    private void QuestItemsExportCandidateGroups_Click(object sender, RoutedEventArgs e)
    {
        ExportQuestItemsCandidateGroups();
    }

    private void QuestItemsLoadMultiCaptures_Click(object sender, RoutedEventArgs e)
    {
        LoadQuestItemsMultiCaptures();
    }

    private void QuestItemsLoadAllCaptures_Click(object sender, RoutedEventArgs e)
    {
        LoadAllQuestItemsCapturesFromSearchFolder();
    }

    private void QuestItemsAnalyzeMultiCaptures_Click(object sender, RoutedEventArgs e)
    {
        AnalyzeQuestItemsMultiCaptures();
    }

    private void QuestItemsExportMultiCaptureAnalysis_Click(object sender, RoutedEventArgs e)
    {
        ExportQuestItemsMultiCaptureAnalysis();
    }

    private void QuestItemsExportReport_Click(object sender, RoutedEventArgs e)
    {
        ExportQuestItemsNamedReport();
    }

    private void QuestItemsOpenSearchFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Directory.CreateDirectory(QuestSearchDirectory);
            Process.Start(new ProcessStartInfo
            {
                FileName = QuestSearchDirectory,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            QuestItemsResearchStatusText.Text = $"Open Quest Search folder failed: {ex.Message}";
            SetStatus("Open Quest Search folder failed.", StatusKind.Warning);
        }
    }

    private void QuestItemsCompare_Click(object sender, RoutedEventArgs e)
    {
        CompareQuestItemsSnapshots("compare");
    }

    private void CompareQuestItemsSnapshots(string action)
    {
        if (_questItemsBeforeSnapshotBytes is null || _questItemsAfterSnapshotBytes is null)
        {
            QuestItemsResearchStatusText.Text = "Capture or load Capture A and Capture B before comparing Quest Items research data.";
            SetStatus("Capture Quest Items Before and After snapshots first.", StatusKind.Neutral);
            return;
        }

        UpdateQuestItemsCaptureSummary();
        if (_questItemsBeforeSnapshotStart != _questItemsAfterSnapshotStart ||
            _questItemsBeforeSnapshotBytes.Length != _questItemsAfterSnapshotBytes.Length)
        {
            QuestItemsResearchStatusText.Text =
                $"Range mismatch. Capture A \"{GetQuestItemsCaptureALabel()}\" uses 0x{_questItemsBeforeSnapshotStart:X} length 0x{_questItemsBeforeSnapshotBytes.Length:X}; " +
                $"Capture B \"{GetQuestItemsCaptureBLabel()}\" uses 0x{_questItemsAfterSnapshotStart:X} length 0x{_questItemsAfterSnapshotBytes.Length:X}.";
            AppendQuestItemsResearchLog(
                $"action={action}-range-mismatch capture-a=\"{GetQuestItemsCaptureALabel()}\" start-a=0x{_questItemsBeforeSnapshotStart:X} length-a=0x{_questItemsBeforeSnapshotBytes.Length:X} capture-b=\"{GetQuestItemsCaptureBLabel()}\" start-b=0x{_questItemsAfterSnapshotStart:X} length-b=0x{_questItemsAfterSnapshotBytes.Length:X}");
            SetStatus("Quest Items research ranges do not match.", StatusKind.Warning);
            return;
        }

        var rows = new List<QuestItemsResearchRowViewModel>(_questItemsBeforeSnapshotBytes.Length);
        for (var index = 0; index < _questItemsBeforeSnapshotBytes.Length; index++)
        {
            rows.Add(new QuestItemsResearchRowViewModel(
                _questItemsBeforeSnapshotStart + (uint)index,
                _questItemsBeforeSnapshotBytes[index],
                _questItemsAfterSnapshotBytes[index]));
        }

        AssignQuestItemsCandidateGroups(rows);
        QuestItemsResearchRows.Clear();
        foreach (var row in rows)
        {
            QuestItemsResearchRows.Add(row);
        }

        var changedByteCount = rows.Count(row => row.IsChanged);
        var changedBitCount = rows.Sum(row => row.ChangedBitCount);
        _lastQuestItemsResearchExport = CreateQuestItemsResearchExport(changedByteCount, changedBitCount);
        BuildQuestItemsCandidateAnalysis(action);
        QuestItemsResearchStatusText.Text =
            $"Compared Quest Items captures: \"{_lastQuestItemsResearchExport.CaptureALabel}\" vs \"{_lastQuestItemsResearchExport.CaptureBLabel}\". " +
            $"Range _playerbase+0x{_lastQuestItemsResearchExport.CaptureAStartOffset:X}, length 0x{_lastQuestItemsResearchExport.CaptureALength:X}. " +
            $"Changed bytes: {changedByteCount}. Changed bits: {changedBitCount}.";
        var changes = string.Join(
            "; ",
            rows
                .Where(row => row.IsChanged)
                .Select(row =>
                    $"{row.Offset}:{row.BeforeValue}(0x{row.BeforeValue:X2})->{row.AfterValue}(0x{row.AfterValue:X2}) {row.ChangedBits} score={row.CandidateScore} group={row.CandidateGroup}"));
        AppendQuestItemsResearchLog(
            $"action={action} capture-a=\"{_lastQuestItemsResearchExport.CaptureALabel}\" capture-b=\"{_lastQuestItemsResearchExport.CaptureBLabel}\" start=0x{_questItemsBeforeSnapshotStart:X} length=0x{_questItemsBeforeSnapshotBytes.Length:X} changed-bytes={changedByteCount} changed-bits={changedBitCount} changes=\"{changes}\"");
        SetStatus("Quest Items research snapshots compared.", StatusKind.Connected);
    }

    private void QuestItemsExportJson_Click(object sender, RoutedEventArgs e)
    {
        if (_lastQuestItemsResearchExport is null)
        {
            QuestItemsResearchStatusText.Text = "Compare Quest Items snapshots before exporting JSON.";
            SetStatus("Compare Quest Items snapshots before exporting.", StatusKind.Neutral);
            return;
        }

        try
        {
            Directory.CreateDirectory(ResearchSnapshotStore.ExportDirectory);
            Directory.CreateDirectory(QuestSearchDirectory);
            if (_questItemsBeforeSnapshotBytes is not null)
            {
                TryWriteQuestItemsSnapshotExport(
                    "quest-items-before.json",
                    "Before",
                    _questItemsBeforeSnapshotStart,
                    _questItemsBeforeSnapshotBytes,
                    _questItemsBeforeCapturedAt);
            }

            if (_questItemsAfterSnapshotBytes is not null)
            {
                TryWriteQuestItemsSnapshotExport(
                    "quest-items-after.json",
                    "After",
                    _questItemsAfterSnapshotStart,
                    _questItemsAfterSnapshotBytes,
                    _questItemsAfterCapturedAt);
            }

            var reportPath = Path.Combine(ResearchSnapshotStore.ExportDirectory, "quest-items-report.json");
            File.WriteAllText(reportPath, JsonSerializer.Serialize(_lastQuestItemsResearchExport, ExportJsonOptions));
            var namedReportPath = WriteQuestItemsNamedJsonReport();
            QuestItemsResearchStatusText.Text =
                $"Exported Quest Items JSON report to logs/research and {Path.GetFileName(namedReportPath)}.";
            AppendQuestItemsResearchLog($"action=export-json report=\"{reportPath}\" rows={_lastQuestItemsResearchExport.Rows.Count}");
            SetStatus("Quest Items JSON exported.", StatusKind.Connected);
        }
        catch (Exception ex)
        {
            QuestItemsResearchStatusText.Text = $"Quest Items JSON export failed: {ex.Message}";
            SetStatus("Quest Items JSON export failed.", StatusKind.Warning);
        }
    }

    private void QuestItemsExportCsv_Click(object sender, RoutedEventArgs e)
    {
        if (_lastQuestItemsResearchExport is null)
        {
            QuestItemsResearchStatusText.Text = "Compare Quest Items snapshots before exporting CSV.";
            SetStatus("Compare Quest Items snapshots before exporting.", StatusKind.Neutral);
            return;
        }

        try
        {
            Directory.CreateDirectory(ResearchSnapshotStore.ExportDirectory);
            Directory.CreateDirectory(QuestSearchDirectory);
            var csvPath = Path.Combine(ResearchSnapshotStore.ExportDirectory, "quest-items-report.csv");
            File.WriteAllLines(csvPath, CreateQuestItemsResearchCsvLines(_lastQuestItemsResearchExport));
            var namedCsvPath = WriteQuestItemsNamedCsvReport();
            QuestItemsResearchStatusText.Text =
                $"Exported Quest Items CSV report to logs/research and {Path.GetFileName(namedCsvPath)}.";
            AppendQuestItemsResearchLog($"action=export-csv csv=\"{csvPath}\" rows={_lastQuestItemsResearchExport.Rows.Count}");
            SetStatus("Quest Items CSV exported.", StatusKind.Connected);
        }
        catch (Exception ex)
        {
            QuestItemsResearchStatusText.Text = $"Quest Items CSV export failed: {ex.Message}";
            SetStatus("Quest Items CSV export failed.", StatusKind.Warning);
        }
    }

    private void HiddenSkillsCaptureBefore_Click(object sender, RoutedEventArgs e)
    {
        if (!TryReadHiddenSkillsResearchRange(out var startOffset, out var bytes, out _))
        {
            return;
        }

        _hiddenSkillsBeforeSnapshotBytes = bytes;
        _hiddenSkillsBeforeSnapshotStart = startOffset;
        _hiddenSkillsBeforeCapturedAt = DateTimeOffset.Now;
        _lastHiddenSkillsResearchExport = null;
        ClearHiddenSkillsResearchResults();
        HiddenSkillsResearchStatusText.Text =
            $"Captured Before at _playerbase+0x{startOffset:X}, length 0x{bytes.Length:X}.";
        var savedPath = TrySaveHiddenSkillsCapture(
            "before",
            startOffset,
            bytes,
            _hiddenSkillsBeforeCapturedAt.Value,
            GetHiddenSkillsCaptureLabel());
        if (!string.IsNullOrWhiteSpace(savedPath))
        {
            HiddenSkillsResearchStatusText.Text =
                $"Captured and saved Before: {Path.GetFileName(savedPath)}.";
        }

        AppendHiddenSkillsResearchLog(
            $"action=capture-before start=0x{startOffset:X} length=0x{bytes.Length:X} saved=\"{savedPath}\" bytes=\"{FormatByteArray(bytes)}\"");
        SetStatus("Hidden Skills Before snapshot captured.", StatusKind.Connected);
    }

    private void HiddenSkillsCaptureAfter_Click(object sender, RoutedEventArgs e)
    {
        if (!TryReadHiddenSkillsResearchRange(out var startOffset, out var bytes, out _))
        {
            return;
        }

        _hiddenSkillsAfterSnapshotBytes = bytes;
        _hiddenSkillsAfterSnapshotStart = startOffset;
        _hiddenSkillsAfterCapturedAt = DateTimeOffset.Now;
        _lastHiddenSkillsResearchExport = null;
        ClearHiddenSkillsResearchResults();
        HiddenSkillsResearchStatusText.Text =
            $"Captured After at _playerbase+0x{startOffset:X}, length 0x{bytes.Length:X}.";
        var savedPath = TrySaveHiddenSkillsCapture(
            "after",
            startOffset,
            bytes,
            _hiddenSkillsAfterCapturedAt.Value,
            GetHiddenSkillsCaptureLabel());
        if (!string.IsNullOrWhiteSpace(savedPath))
        {
            HiddenSkillsResearchStatusText.Text =
                $"Captured and saved After: {Path.GetFileName(savedPath)}.";
        }

        AppendHiddenSkillsResearchLog(
            $"action=capture-after start=0x{startOffset:X} length=0x{bytes.Length:X} saved=\"{savedPath}\" bytes=\"{FormatByteArray(bytes)}\"");
        SetStatus("Hidden Skills After snapshot captured.", StatusKind.Connected);
    }

    private void HiddenSkillsLoadBeforeCapture_Click(object sender, RoutedEventArgs e)
    {
        if (!TryLoadHiddenSkillsCapture("before", out var capture))
        {
            return;
        }

        _hiddenSkillsBeforeSnapshotBytes = ParseHiddenSkillsCaptureBytes(capture);
        _hiddenSkillsBeforeSnapshotStart = capture.StartOffset;
        _hiddenSkillsBeforeCapturedAt = capture.Timestamp;
        _lastHiddenSkillsResearchExport = null;
        ClearHiddenSkillsResearchResults();
        HiddenSkillsResearchStartOffsetText.Text = $"0x{capture.StartOffset:X}";
        HiddenSkillsResearchLengthText.Text = $"0x{capture.Length:X}";
        if (!string.IsNullOrWhiteSpace(capture.Label))
        {
            HiddenSkillsCaptureLabelText.Text = capture.Label;
        }

        HiddenSkillsResearchStatusText.Text =
            $"Loaded Before capture: {capture.LabelOrDefault} ({capture.Timestamp.ToLocalTime():g}, 0x{capture.StartOffset:X}/0x{capture.Length:X}).";
        AppendHiddenSkillsResearchLog(
            $"action=load-before label=\"{capture.Label}\" start=0x{capture.StartOffset:X} length=0x{capture.Length:X}");
        SetStatus("Hidden Skills Before capture loaded.", StatusKind.Connected);
    }

    private void HiddenSkillsLoadAfterCapture_Click(object sender, RoutedEventArgs e)
    {
        if (!TryLoadHiddenSkillsCapture("after", out var capture))
        {
            return;
        }

        _hiddenSkillsAfterSnapshotBytes = ParseHiddenSkillsCaptureBytes(capture);
        _hiddenSkillsAfterSnapshotStart = capture.StartOffset;
        _hiddenSkillsAfterCapturedAt = capture.Timestamp;
        _lastHiddenSkillsResearchExport = null;
        ClearHiddenSkillsResearchResults();
        HiddenSkillsResearchStartOffsetText.Text = $"0x{capture.StartOffset:X}";
        HiddenSkillsResearchLengthText.Text = $"0x{capture.Length:X}";
        if (!string.IsNullOrWhiteSpace(capture.Label))
        {
            HiddenSkillsCaptureLabelText.Text = capture.Label;
        }

        HiddenSkillsResearchStatusText.Text =
            $"Loaded After capture: {capture.LabelOrDefault} ({capture.Timestamp.ToLocalTime():g}, 0x{capture.StartOffset:X}/0x{capture.Length:X}).";
        AppendHiddenSkillsResearchLog(
            $"action=load-after label=\"{capture.Label}\" start=0x{capture.StartOffset:X} length=0x{capture.Length:X}");
        SetStatus("Hidden Skills After capture loaded.", StatusKind.Connected);
    }

    private void HiddenSkillsCompare_Click(object sender, RoutedEventArgs e)
    {
        if (_hiddenSkillsBeforeSnapshotBytes is null || _hiddenSkillsAfterSnapshotBytes is null)
        {
            HiddenSkillsResearchStatusText.Text = "Capture Before and After snapshots before comparing.";
            SetStatus("Capture Hidden Skills Before and After snapshots first.", StatusKind.Neutral);
            return;
        }

        if (_hiddenSkillsBeforeSnapshotStart != _hiddenSkillsAfterSnapshotStart ||
            _hiddenSkillsBeforeSnapshotBytes.Length != _hiddenSkillsAfterSnapshotBytes.Length)
        {
            HiddenSkillsResearchStatusText.Text = "Before and After ranges must use the same start and length.";
            SetStatus("Hidden Skills research ranges do not match.", StatusKind.Warning);
            return;
        }

        var treatAfterAsPersisted = HiddenSkillsPersistedAfterReloadCheckBox.IsChecked == true;
        var rows = new List<HiddenSkillsResearchRowViewModel>(_hiddenSkillsBeforeSnapshotBytes.Length);
        for (var index = 0; index < _hiddenSkillsBeforeSnapshotBytes.Length; index++)
        {
            rows.Add(new HiddenSkillsResearchRowViewModel(
                _hiddenSkillsBeforeSnapshotStart + (uint)index,
                _hiddenSkillsBeforeSnapshotBytes[index],
                _hiddenSkillsAfterSnapshotBytes[index],
                treatAfterAsPersisted));
        }

        var changedOffsets = rows
            .Where(row => row.IsChanged)
            .Select(row => row.OffsetValue)
            .ToList();
        foreach (var row in rows.Where(row => row.IsChanged))
        {
            row.SetClusteredChange(changedOffsets.Any(offset =>
                offset != row.OffsetValue &&
                Math.Abs((long)offset - row.OffsetValue) <= 2));
        }

        _hiddenSkillsAllResearchRows = rows;
        ApplyHiddenSkillsPinnedOffsets();
        RebuildHiddenSkillsCandidateGroups();
        ApplyHiddenSkillsResearchFilter();

        _lastHiddenSkillsResearchExport = CreateHiddenSkillsResearchExport();
        var changedCount = _hiddenSkillsAllResearchRows.Count(row => row.IsChanged);
        var singleBitCount = _hiddenSkillsAllResearchRows.Count(row => row.IsSingleBitChange);
        var clusteredCount = _hiddenSkillsAllResearchRows.Count(row => row.IsClusteredChange);
        HiddenSkillsResearchStatusText.Text =
            $"Compared Hidden Skills snapshots. Changed bytes: {changedCount}. Single-bit: {singleBitCount}. Clustered: {clusteredCount}. Showing {HiddenSkillsResearchRows.Count} row(s). Groups: {HiddenSkillsCandidateGroups.Count}.";
        AppendHiddenSkillsResearchLog(
            $"action=compare start=0x{_hiddenSkillsBeforeSnapshotStart:X} length=0x{_hiddenSkillsBeforeSnapshotBytes.Length:X} " +
            $"changed={changedCount} single-bit={singleBitCount} clustered={clusteredCount} persisted={treatAfterAsPersisted}");
        SetStatus("Hidden Skills snapshots compared.", StatusKind.Connected);
    }

    private void HiddenSkillsExportJson_Click(object sender, RoutedEventArgs e)
    {
        if (_lastHiddenSkillsResearchExport is null)
        {
            HiddenSkillsResearchStatusText.Text = "Compare Hidden Skills snapshots before exporting JSON.";
            SetStatus("Compare Hidden Skills snapshots before exporting.", StatusKind.Neutral);
            return;
        }

        try
        {
            Directory.CreateDirectory(ResearchSnapshotStore.ExportDirectory);
            if (_hiddenSkillsBeforeSnapshotBytes is not null)
            {
                TryWriteHiddenSkillsSnapshotExport(
                    "hidden-skills-before.json",
                    "Before",
                    _hiddenSkillsBeforeSnapshotStart,
                    _hiddenSkillsBeforeSnapshotBytes,
                    _hiddenSkillsBeforeCapturedAt);
            }

            if (_hiddenSkillsAfterSnapshotBytes is not null)
            {
                TryWriteHiddenSkillsSnapshotExport(
                    "hidden-skills-after.json",
                    "After",
                    _hiddenSkillsAfterSnapshotStart,
                    _hiddenSkillsAfterSnapshotBytes,
                    _hiddenSkillsAfterCapturedAt);
            }

            var reportPath = Path.Combine(ResearchSnapshotStore.ExportDirectory, "hidden-skills-report.json");
            File.WriteAllText(reportPath, JsonSerializer.Serialize(_lastHiddenSkillsResearchExport, ExportJsonOptions));
            HiddenSkillsResearchStatusText.Text = "Exported Hidden Skills JSON files to logs/research.";
            AppendHiddenSkillsResearchLog($"action=export-json report=\"{reportPath}\" rows={_lastHiddenSkillsResearchExport.Rows.Count}");
            SetStatus("Hidden Skills JSON exported.", StatusKind.Connected);
        }
        catch (Exception ex)
        {
            HiddenSkillsResearchStatusText.Text = $"Hidden Skills JSON export failed: {ex.Message}";
            SetStatus("Hidden Skills JSON export failed.", StatusKind.Warning);
        }
    }

    private void HiddenSkillsExportCsv_Click(object sender, RoutedEventArgs e)
    {
        if (_lastHiddenSkillsResearchExport is null)
        {
            HiddenSkillsResearchStatusText.Text = "Compare Hidden Skills snapshots before exporting CSV.";
            SetStatus("Compare Hidden Skills snapshots before exporting.", StatusKind.Neutral);
            return;
        }

        try
        {
            Directory.CreateDirectory(ResearchSnapshotStore.ExportDirectory);
            var csvPath = Path.Combine(ResearchSnapshotStore.ExportDirectory, "hidden-skills-report.csv");
            File.WriteAllLines(csvPath, CreateHiddenSkillsResearchCsvLines(_lastHiddenSkillsResearchExport.Rows));
            HiddenSkillsResearchStatusText.Text = "Exported Hidden Skills CSV report to logs/research.";
            AppendHiddenSkillsResearchLog($"action=export-csv csv=\"{csvPath}\" rows={_lastHiddenSkillsResearchExport.Rows.Count}");
            SetStatus("Hidden Skills CSV exported.", StatusKind.Connected);
        }
        catch (Exception ex)
        {
            HiddenSkillsResearchStatusText.Text = $"Hidden Skills CSV export failed: {ex.Message}";
            SetStatus("Hidden Skills CSV export failed.", StatusKind.Warning);
        }
    }

    private void HiddenSkillsFilterChanged_Click(object sender, RoutedEventArgs e)
    {
        ApplyHiddenSkillsResearchFilter();
        if (_hiddenSkillsAllResearchRows.Count > 0)
        {
            HiddenSkillsResearchStatusText.Text =
                $"Filter updated. Showing {HiddenSkillsResearchRows.Count} of {_hiddenSkillsAllResearchRows.Count} Hidden Skills research row(s).";
        }
    }

    private void HiddenSkillsPinChanged_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not HiddenSkillsResearchRowViewModel row)
        {
            return;
        }

        _hiddenSkillsPinnedOffsets[row.OffsetValue] = row.IsPinned;
        RebuildHiddenSkillsCandidateGroups();
        ApplyHiddenSkillsResearchFilter();
        _lastHiddenSkillsResearchExport = CreateHiddenSkillsResearchExport();
        AppendHiddenSkillsResearchLog(
            $"action=pin offset={row.Offset} pinned={row.IsPinned} score={row.CandidateScore}");
        HiddenSkillsResearchStatusText.Text =
            $"{(row.IsPinned ? "Pinned" : "Unpinned")} {row.Offset}. Candidate groups: {HiddenSkillsCandidateGroups.Count}.";
    }

    private void HiddenSkillsExportCandidateGroups_Click(object sender, RoutedEventArgs e)
    {
        if (HiddenSkillsCandidateGroups.Count == 0)
        {
            HiddenSkillsResearchStatusText.Text = "No Hidden Skills candidate groups to export. Compare captures or pin offsets first.";
            SetStatus("No Hidden Skills candidate groups to export.", StatusKind.Neutral);
            return;
        }

        try
        {
            Directory.CreateDirectory(ResearchSnapshotStore.ExportDirectory);
            var export = CreateHiddenSkillsCandidateGroupsExport();
            var jsonPath = Path.Combine(ResearchSnapshotStore.ExportDirectory, "hidden-skills-candidate-groups.json");
            var csvPath = Path.Combine(ResearchSnapshotStore.ExportDirectory, "hidden-skills-candidate-groups.csv");
            File.WriteAllText(jsonPath, JsonSerializer.Serialize(export, ExportJsonOptions));
            File.WriteAllLines(csvPath, CreateHiddenSkillsCandidateGroupsCsvLines(export.Groups));
            HiddenSkillsResearchStatusText.Text =
                $"Exported Hidden Skills candidate groups: {Path.GetFileName(jsonPath)} and {Path.GetFileName(csvPath)}.";
            AppendHiddenSkillsResearchLog(
                $"action=export-candidate-groups json=\"{jsonPath}\" csv=\"{csvPath}\" groups={export.Groups.Count}");
            SetStatus("Hidden Skills candidate groups exported.", StatusKind.Connected);
        }
        catch (Exception ex)
        {
            HiddenSkillsResearchStatusText.Text = $"Hidden Skills candidate group export failed: {ex.Message}";
            SetStatus("Hidden Skills candidate group export failed.", StatusKind.Warning);
        }
    }

    private void HiddenSkillsMultiLoadCapture_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not string slotText ||
            !int.TryParse(slotText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var slotIndex) ||
            slotIndex is < 0 or > 5)
        {
            return;
        }

        var slotName = GetHiddenSkillsMultiCaptureSlotName(slotIndex);
        if (!TryLoadHiddenSkillsCaptureForMultiAnalyzer(slotName, out var capture, out var bytes, out var filePath))
        {
            return;
        }

        _hiddenSkillsMultiCaptures[slotIndex] = new HiddenSkillsLoadedCapture(
            slotName,
            capture,
            bytes,
            filePath);
        GetHiddenSkillsMultiCaptureFileTextBox(slotIndex).Text =
            $"{Path.GetFileName(filePath)} | {capture.LabelOrDefault} | 0x{capture.StartOffset:X}/0x{capture.Length:X}";
        HiddenSkillsMultiCaptureCandidates.Clear();
        _lastHiddenSkillsMultiCaptureExport = null;
        HiddenSkillsMultiCaptureStatusText.Text =
            $"Loaded Capture {slotName}: {capture.LabelOrDefault} ({capture.Timestamp.ToLocalTime():g}).";
        AppendHiddenSkillsResearchLog(
            $"action=multi-load slot={slotName} file=\"{filePath}\" label=\"{capture.Label}\" start=0x{capture.StartOffset:X} length=0x{capture.Length:X}");
        SetStatus($"Hidden Skills Capture {slotName} loaded.", StatusKind.Connected);
    }

    private void HiddenSkillsMultiAnalyze_Click(object sender, RoutedEventArgs e)
    {
        if (_hiddenSkillsMultiCaptures.Any(capture => capture is null))
        {
            HiddenSkillsMultiCaptureStatusText.Text = "Load captures A-F before analyzing.";
            SetStatus("Load all six Hidden Skills captures before analyzing.", StatusKind.Neutral);
            return;
        }

        if (!TryReadHiddenSkillsMultiSkillCounts(out var skillCounts))
        {
            return;
        }

        var captures = _hiddenSkillsMultiCaptures
            .OfType<HiddenSkillsLoadedCapture>()
            .ToArray();
        var firstCapture = captures[0];
        if (captures.Any(capture =>
                capture.Document.StartOffset != firstCapture.Document.StartOffset ||
                capture.Bytes.Length != firstCapture.Bytes.Length))
        {
            HiddenSkillsMultiCaptureStatusText.Text =
                "All multi-capture analyzer files must use the same playerbase-relative start offset and length.";
            SetStatus("Hidden Skills multi-capture ranges do not match.", StatusKind.Warning);
            return;
        }

        var candidates = CreateHiddenSkillsMultiCaptureCandidates(captures, skillCounts);
        HiddenSkillsMultiCaptureCandidates.Clear();
        foreach (var candidate in candidates)
        {
            HiddenSkillsMultiCaptureCandidates.Add(candidate);
        }

        _lastHiddenSkillsMultiCaptureExport = CreateHiddenSkillsMultiCaptureExport(captures, skillCounts);
        var progressionMatches = HiddenSkillsMultiCaptureCandidates.Count(candidate => candidate.ProgressionMatch);
        HiddenSkillsMultiCaptureStatusText.Text =
            $"Analyzed {captures.Length} captures over 0x{firstCapture.Bytes.Length:X} byte(s). Ranked {HiddenSkillsMultiCaptureCandidates.Count} candidate row(s); {progressionMatches} progression match(es).";
        AppendHiddenSkillsResearchLog(
            $"action=multi-analyze start=0x{firstCapture.Document.StartOffset:X} length=0x{firstCapture.Bytes.Length:X} " +
            $"counts=\"{string.Join("->", skillCounts)}\" candidates={HiddenSkillsMultiCaptureCandidates.Count} progression-matches={progressionMatches}");
        SetStatus("Hidden Skills multi-capture ranking generated.", StatusKind.Connected);
    }

    private void HiddenSkillsMultiExportRanking_Click(object sender, RoutedEventArgs e)
    {
        if (_lastHiddenSkillsMultiCaptureExport is null)
        {
            HiddenSkillsMultiCaptureStatusText.Text = "Analyze multi-capture ranking before exporting.";
            SetStatus("Analyze Hidden Skills multi-capture ranking before exporting.", StatusKind.Neutral);
            return;
        }

        try
        {
            Directory.CreateDirectory(ResearchSnapshotStore.ExportDirectory);
            var jsonPath = Path.Combine(ResearchSnapshotStore.ExportDirectory, "hidden-skills-multi-capture-ranking.json");
            var csvPath = Path.Combine(ResearchSnapshotStore.ExportDirectory, "hidden-skills-multi-capture-ranking.csv");
            File.WriteAllText(jsonPath, JsonSerializer.Serialize(_lastHiddenSkillsMultiCaptureExport, ExportJsonOptions));
            File.WriteAllLines(csvPath, CreateHiddenSkillsMultiCaptureCsvLines(_lastHiddenSkillsMultiCaptureExport.Candidates));

            HiddenSkillsMultiCaptureStatusText.Text =
                $"Exported Hidden Skills multi-capture ranking: {Path.GetFileName(jsonPath)} and {Path.GetFileName(csvPath)}.";
            AppendHiddenSkillsResearchLog(
                $"action=multi-export json=\"{jsonPath}\" csv=\"{csvPath}\" rows={_lastHiddenSkillsMultiCaptureExport.Candidates.Count}");
            SetStatus("Hidden Skills multi-capture ranking exported.", StatusKind.Connected);
        }
        catch (Exception ex)
        {
            HiddenSkillsMultiCaptureStatusText.Text = $"Hidden Skills multi-capture export failed: {ex.Message}";
            SetStatus("Hidden Skills multi-capture export failed.", StatusKind.Warning);
        }
    }

    private void HiddenSkillsRegionLoadCaptureA_Click(object sender, RoutedEventArgs e)
    {
        if (!TryLoadHiddenSkillsCaptureForRegionAnalyzer("A", out var capture, out var bytes, out var filePath))
        {
            return;
        }

        _hiddenSkillsRegionCaptureA = capture;
        _hiddenSkillsRegionCaptureABytes = bytes;
        _hiddenSkillsRegionCaptureAPath = filePath;
        HiddenSkillsRegionCaptureAFileText.Text =
            $"{Path.GetFileName(filePath)} | {capture.LabelOrDefault} | 0x{capture.StartOffset:X}/0x{capture.Length:X}";
        HiddenSkillsRegionAnalysisRows.Clear();
        _lastHiddenSkillsRegionAnalysisExport = null;
        HiddenSkillsRegionStatusText.Text = $"Loaded Region Capture A: {capture.LabelOrDefault}.";
        AppendHiddenSkillsResearchLog(
            $"action=region-load-a file=\"{filePath}\" label=\"{capture.Label}\" start=0x{capture.StartOffset:X} length=0x{capture.Length:X}");
        SetStatus("Hidden Skills Region Capture A loaded.", StatusKind.Connected);
    }

    private void HiddenSkillsRegionLoadCaptureB_Click(object sender, RoutedEventArgs e)
    {
        if (!TryLoadHiddenSkillsCaptureForRegionAnalyzer("B", out var capture, out var bytes, out var filePath))
        {
            return;
        }

        _hiddenSkillsRegionCaptureB = capture;
        _hiddenSkillsRegionCaptureBBytes = bytes;
        _hiddenSkillsRegionCaptureBPath = filePath;
        HiddenSkillsRegionCaptureBFileText.Text =
            $"{Path.GetFileName(filePath)} | {capture.LabelOrDefault} | 0x{capture.StartOffset:X}/0x{capture.Length:X}";
        HiddenSkillsRegionAnalysisRows.Clear();
        _lastHiddenSkillsRegionAnalysisExport = null;
        HiddenSkillsRegionStatusText.Text = $"Loaded Region Capture B: {capture.LabelOrDefault}.";
        AppendHiddenSkillsResearchLog(
            $"action=region-load-b file=\"{filePath}\" label=\"{capture.Label}\" start=0x{capture.StartOffset:X} length=0x{capture.Length:X}");
        SetStatus("Hidden Skills Region Capture B loaded.", StatusKind.Connected);
    }

    private void HiddenSkillsRegionPreset_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not string preset)
        {
            return;
        }

        var parts = preset.Split('|');
        if (parts.Length != 2)
        {
            return;
        }

        HiddenSkillsRegionStartOffsetText.Text = parts[0];
        HiddenSkillsRegionLengthText.Text = parts[1];
        HiddenSkillsRegionStatusText.Text = $"Loaded region preset {parts[0]} length {parts[1]}.";
    }

    private void HiddenSkillsRegionAnalyze_Click(object sender, RoutedEventArgs e)
    {
        if (_hiddenSkillsRegionCaptureA is null ||
            _hiddenSkillsRegionCaptureB is null ||
            _hiddenSkillsRegionCaptureABytes is null ||
            _hiddenSkillsRegionCaptureBBytes is null)
        {
            HiddenSkillsRegionStatusText.Text = "Load Region Capture A and Capture B before analyzing.";
            SetStatus("Load Hidden Skills region captures first.", StatusKind.Neutral);
            return;
        }

        if (!TryParseResearchOffset(HiddenSkillsRegionStartOffsetText.Text, out var startOffset, out var offsetError))
        {
            HiddenSkillsRegionStatusText.Text = offsetError;
            SetStatus("Invalid Hidden Skills region start offset.", StatusKind.Warning);
            return;
        }

        if (!TryParseResearchLength(HiddenSkillsRegionLengthText.Text, out var length, out var lengthError))
        {
            HiddenSkillsRegionStatusText.Text = lengthError;
            SetStatus("Invalid Hidden Skills region length.", StatusKind.Warning);
            return;
        }

        var captureAInRange = TryExtractHiddenSkillsCaptureRange(
            _hiddenSkillsRegionCaptureA,
            _hiddenSkillsRegionCaptureABytes,
            startOffset,
            length,
            out var captureARange,
            out var captureAError);
        var captureBInRange = TryExtractHiddenSkillsCaptureRange(
            _hiddenSkillsRegionCaptureB,
            _hiddenSkillsRegionCaptureBBytes,
            startOffset,
            length,
            out var captureBRange,
            out var captureBError);
        if (!captureAInRange || !captureBInRange)
        {
            HiddenSkillsRegionStatusText.Text = string.IsNullOrWhiteSpace(captureAError) ? captureBError : captureAError;
            SetStatus("Selected Hidden Skills region is outside a loaded capture.", StatusKind.Warning);
            return;
        }

        var rows = CreateHiddenSkillsRegionAnalysisRows(startOffset, captureARange, captureBRange);
        HiddenSkillsRegionAnalysisRows.Clear();
        foreach (var row in rows)
        {
            HiddenSkillsRegionAnalysisRows.Add(row);
        }

        _lastHiddenSkillsRegionAnalysisExport = CreateHiddenSkillsRegionAnalysisExport(startOffset, length);
        HiddenSkillsRegionStatusText.Text =
            $"Analyzed 0x{length:X} byte(s). Ranked {HiddenSkillsRegionAnalysisRows.Count} changed region(s).";
        AppendHiddenSkillsResearchLog(
            $"action=region-analyze start=0x{startOffset:X} length=0x{length:X} regions={HiddenSkillsRegionAnalysisRows.Count}");
        SetStatus("Hidden Skills region comparison completed.", StatusKind.Connected);
    }

    private void HiddenSkillsRegionExport_Click(object sender, RoutedEventArgs e)
    {
        if (_lastHiddenSkillsRegionAnalysisExport is null)
        {
            HiddenSkillsRegionStatusText.Text = "Analyze regions before exporting.";
            SetStatus("Analyze Hidden Skills regions before exporting.", StatusKind.Neutral);
            return;
        }

        try
        {
            Directory.CreateDirectory(ResearchSnapshotStore.ExportDirectory);
            var jsonPath = Path.Combine(ResearchSnapshotStore.ExportDirectory, "hidden-skills-region-analysis.json");
            var csvPath = Path.Combine(ResearchSnapshotStore.ExportDirectory, "hidden-skills-region-analysis.csv");
            File.WriteAllText(jsonPath, JsonSerializer.Serialize(_lastHiddenSkillsRegionAnalysisExport, ExportJsonOptions));
            File.WriteAllLines(csvPath, CreateHiddenSkillsRegionAnalysisCsvLines(_lastHiddenSkillsRegionAnalysisExport.Rows));
            HiddenSkillsRegionStatusText.Text =
                $"Exported Hidden Skills region analysis: {Path.GetFileName(jsonPath)} and {Path.GetFileName(csvPath)}.";
            AppendHiddenSkillsResearchLog(
                $"action=region-export json=\"{jsonPath}\" csv=\"{csvPath}\" rows={_lastHiddenSkillsRegionAnalysisExport.Rows.Count}");
            SetStatus("Hidden Skills region analysis exported.", StatusKind.Connected);
        }
        catch (Exception ex)
        {
            HiddenSkillsRegionStatusText.Text = $"Hidden Skills region export failed: {ex.Message}";
            SetStatus("Hidden Skills region export failed.", StatusKind.Warning);
        }
    }

    private void HiddenSkillsLiveWatchStart_Click(object sender, RoutedEventArgs e)
    {
        if (_hiddenSkillsLiveWatchTimer.IsEnabled)
        {
            HiddenSkillsLiveWatchStatusText.Text = "Live watch is already running.";
            return;
        }

        if (!TryReadHiddenSkillsLiveWatchRange(out var startOffset, out var bytes))
        {
            return;
        }

        _hiddenSkillsLiveWatchStartOffset = startOffset;
        _hiddenSkillsLiveWatchLength = bytes.Length;
        _hiddenSkillsLiveWatchPreviousBytes = bytes;
        _hiddenSkillsLiveWatchStartedAt = DateTimeOffset.Now;
        _hiddenSkillsLiveWatchChangeCounts.Clear();
        _hiddenSkillsLiveWatchTimer.Start();
        HiddenSkillsLiveWatchStatusText.Text =
            $"Watching _playerbase+0x{startOffset:X} length 0x{bytes.Length:X} every 250ms. Baseline captured.";
        AppendHiddenSkillsLiveWatchLog(
            $"action=start start=0x{startOffset:X} length=0x{bytes.Length:X} bytes=\"{FormatByteArray(bytes)}\"");
        SetStatus("Hidden Skills live watch started.", StatusKind.Working);
    }

    private void HiddenSkillsLiveWatchStop_Click(object sender, RoutedEventArgs e)
    {
        StopHiddenSkillsLiveWatch("Stopped Hidden Skills live watch.");
    }

    private void HiddenSkillsLiveWatchClear_Click(object sender, RoutedEventArgs e)
    {
        HiddenSkillsLiveWatchRows.Clear();
        HiddenSkillsEventMarkers.Clear();
        _hiddenSkillsLiveWatchChangeCounts.Clear();
        HiddenSkillsLiveWatchStatusText.Text = "Cleared Hidden Skills live watch rows and event markers.";
        AppendHiddenSkillsLiveWatchLog("action=clear");
        SetStatus("Hidden Skills live watch cleared.", StatusKind.Neutral);
    }

    private void HiddenSkillsLiveWatchExport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Directory.CreateDirectory(ResearchSnapshotStore.ExportDirectory);
            var export = CreateHiddenSkillsLiveWatchExport();
            var jsonPath = Path.Combine(ResearchSnapshotStore.ExportDirectory, "hidden-skills-live-watch.json");
            var csvPath = Path.Combine(ResearchSnapshotStore.ExportDirectory, "hidden-skills-live-watch.csv");
            File.WriteAllText(jsonPath, JsonSerializer.Serialize(export, ExportJsonOptions));
            File.WriteAllLines(csvPath, CreateHiddenSkillsLiveWatchCsvLines(export));
            HiddenSkillsLiveWatchStatusText.Text =
                $"Exported Hidden Skills live watch: {Path.GetFileName(jsonPath)} and {Path.GetFileName(csvPath)}.";
            AppendHiddenSkillsLiveWatchLog(
                $"action=export json=\"{jsonPath}\" csv=\"{csvPath}\" changes={export.Rows.Count} events={export.Events.Count}");
            SetStatus("Hidden Skills live watch exported.", StatusKind.Connected);
        }
        catch (Exception ex)
        {
            HiddenSkillsLiveWatchStatusText.Text = $"Hidden Skills live watch export failed: {ex.Message}";
            SetStatus("Hidden Skills live watch export failed.", StatusKind.Warning);
        }
    }

    private void HiddenSkillsMarkEvent_Click(object sender, RoutedEventArgs e)
    {
        var label = HiddenSkillsEventLabelBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(label))
        {
            label = "Hidden Skills event";
        }

        var marker = new HiddenSkillsEventMarkerViewModel(DateTimeOffset.Now, label);
        HiddenSkillsEventMarkers.Insert(0, marker);
        HiddenSkillsLiveWatchStatusText.Text = $"Marked event: {label}.";
        AppendHiddenSkillsLiveWatchLog($"action=event label=\"{label}\"");
        SetStatus("Hidden Skills event marker added.", StatusKind.Neutral);
    }

    private void HiddenSkillsBitPreset_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not string preset)
        {
            return;
        }

        var parts = preset.Split('|');
        if (parts.Length != 2)
        {
            return;
        }

        HiddenSkillsBitOffsetText.Text = parts[0];
        HiddenSkillsBitIndexText.Text = parts[1];
        HiddenSkillsBitStatusText.Text = $"Loaded Hidden Skills bit preset {parts[0]} bit {parts[1]}.";
    }

    private void HiddenSkillsBitReadCurrent_Click(object sender, RoutedEventArgs e)
    {
        if (!TryReadHiddenSkillsBitTestByte(out var offset, out var bit, out var value, out var absoluteAddress))
        {
            return;
        }

        var isSet = IsBitSet(value, bit);
        HiddenSkillsBitAbsoluteAddressText.Text = $"0x{absoluteAddress:X}";
        HiddenSkillsBitCurrentByteText.Text = FormatResearchByte(value);
        HiddenSkillsBitCurrentStateText.Text = isSet ? "Set" : "Clear";
        HiddenSkillsBitDesiredStateCheckBox.IsChecked = isSet;
        HiddenSkillsBitImmediateReadbackText.Text = "Not written";
        HiddenSkillsBit250ReadbackText.Text = "Not written";
        HiddenSkillsBit1000ReadbackText.Text = "Not written";
        HiddenSkillsBitStatusText.Text =
            $"Read _playerbase+0x{offset:X} bit {bit}: {(isSet ? "set" : "clear")}.";
        AppendHiddenSkillsBitTestingLog(
            $"action=read offset=0x{offset:X} bit={bit} address=0x{absoluteAddress:X} byte={FormatCandidateLogByte(value)} state={isSet}");
        SetStatus("Hidden Skills bit read.", StatusKind.Connected);
    }

    private void HiddenSkillsBitToggleState_Click(object sender, RoutedEventArgs e)
    {
        if (!TryReadHiddenSkillsBitTestByte(out var offset, out var bit, out var value, out _))
        {
            return;
        }

        var newDesiredState = !IsBitSet(value, bit);
        HiddenSkillsBitCurrentByteText.Text = FormatResearchByte(value);
        HiddenSkillsBitCurrentStateText.Text = newDesiredState ? "Clear -> Set" : "Set -> Clear";
        HiddenSkillsBitDesiredStateCheckBox.IsChecked = newDesiredState;
        HiddenSkillsBitStatusText.Text =
            $"Toggled desired state for _playerbase+0x{offset:X} bit {bit} to {(newDesiredState ? "set" : "clear")}. Click Apply to write.";
        SetStatus("Hidden Skills bit desired state toggled.", StatusKind.Neutral);
    }

    private async void HiddenSkillsBitApply_Click(object sender, RoutedEventArgs e)
    {
        if (!TryReadHiddenSkillsBitTestByte(out var offset, out var bit, out var beforeValue, out var absoluteAddress))
        {
            return;
        }

        var memory = _memory;
        if (memory is null)
        {
            HiddenSkillsBitStatusText.Text = "Not attached.";
            SetStatus("Not attached. Attach to Cemu and rescan before testing Hidden Skills bits.", StatusKind.Neutral);
            return;
        }

        var desiredState = HiddenSkillsBitDesiredStateCheckBox.IsChecked == true;
        var mask = (byte)(1 << bit);
        var desiredValue = desiredState
            ? (byte)(beforeValue | mask)
            : (byte)(beforeValue & ~mask);
        byte? immediateReadback = null;
        byte? delayed250Readback = null;
        byte? delayed1000Readback = null;
        var status = "started";

        try
        {
            if (!memory.TryWriteBytes(absoluteAddress, [desiredValue], out var writeError))
            {
                status = $"write-failed: {writeError}";
                HiddenSkillsBitStatusText.Text = $"Write failed: {writeError}";
                SetStatus("Hidden Skills bit write failed.", StatusKind.Warning);
                return;
            }

            if (!TryReadCandidateByteAt(offset, absoluteAddress, out var immediateValue, out var immediateError))
            {
                status = $"immediate-read-failed: {immediateError}";
                HiddenSkillsBitStatusText.Text = $"Write issued, but immediate verification failed: {immediateError}";
                SetStatus("Hidden Skills bit immediate verification failed.", StatusKind.Warning);
                return;
            }

            immediateReadback = immediateValue;
            HiddenSkillsBitImmediateReadbackText.Text = FormatResearchByte(immediateValue);
            if (immediateValue != desiredValue)
            {
                status = "immediate-mismatch";
                HiddenSkillsBitStatusText.Text =
                    $"Write failed: expected {FormatResearchByte(desiredValue)} but read {FormatResearchByte(immediateValue)}.";
                SetStatus("Hidden Skills bit immediate readback mismatch.", StatusKind.Warning);
                return;
            }

            await Task.Delay(250);
            if (TryReadCandidateByteAt(offset, absoluteAddress, out var delayed250Value, out _))
            {
                delayed250Readback = delayed250Value;
                HiddenSkillsBit250ReadbackText.Text = FormatResearchByte(delayed250Value);
            }
            else
            {
                HiddenSkillsBit250ReadbackText.Text = "Read failed";
            }

            await Task.Delay(750);
            if (TryReadCandidateByteAt(offset, absoluteAddress, out var delayed1000Value, out _))
            {
                delayed1000Readback = delayed1000Value;
                HiddenSkillsBit1000ReadbackText.Text = FormatResearchByte(delayed1000Value);
            }
            else
            {
                HiddenSkillsBit1000ReadbackText.Text = "Read failed";
            }

            var delayedMismatch =
                delayed250Readback.HasValue && delayed250Readback.Value != desiredValue ||
                delayed1000Readback.HasValue && delayed1000Readback.Value != desiredValue;
            status = delayedMismatch ? "delayed-mismatch" : "verified";
            HiddenSkillsBitCurrentByteText.Text = FormatResearchByte(delayed1000Readback ?? immediateValue);
            HiddenSkillsBitCurrentStateText.Text = IsBitSet(delayed1000Readback ?? immediateValue, bit) ? "Set" : "Clear";
            HiddenSkillsBitStatusText.Text = delayedMismatch
                ? "Write succeeded immediately, but delayed verification changed. Game may have reverted or updated the byte."
                : $"Verified _playerbase+0x{offset:X} bit {bit} {(desiredState ? "set" : "clear")}.";
            SetStatus(
                delayedMismatch ? "Hidden Skills bit changed after delayed verification." : "Hidden Skills bit write verified.",
                delayedMismatch ? StatusKind.Warning : StatusKind.Connected);
        }
        finally
        {
            AppendHiddenSkillsBitTestingLog(
                $"action=apply offset=0x{offset:X} bit={bit} address=0x{absoluteAddress:X} " +
                $"before={FormatCandidateLogByte(beforeValue)} desired-byte={FormatCandidateLogByte(desiredValue)} " +
                $"desired-state={desiredState} immediate={FormatCandidateLogByte(immediateReadback)} " +
                $"read250ms={FormatCandidateLogByte(delayed250Readback)} read1000ms={FormatCandidateLogByte(delayed1000Readback)} " +
                $"status=\"{status}\"");
        }
    }

    private async void ApplyGoldenBugChanges_Click(object sender, RoutedEventArgs e)
    {
        var dirtyBits = GoldenBugsEditorRows
            .Where(bit => bit.IsDirty && bit.CanEdit)
            .ToList();
        if (dirtyBits.Count == 0)
        {
            GoldenBugsEditorStatusText.Text = "No Golden Bug changes to apply.";
            SetStatus("No Golden Bug changes to apply.", StatusKind.Neutral);
            return;
        }

        if (!TryReadGoldenBugsEditorBytes(out var currentBytes, out _))
        {
            return;
        }

        var desiredBytes = currentBytes.ToArray();
        foreach (var bit in dirtyBits)
        {
            SetGoldenBugBitInBytes(desiredBytes, bit, bit.IsSetDesired);
        }

        var verified = await WriteGoldenBugsEditorBytesAsync("apply", desiredBytes);
        RefreshGoldenBugsBitfieldFromMemory(preserveDirty: !verified);
        GoldenBugsEditorStatusText.Text = verified
            ? $"Applied {dirtyBits.Count} Golden Bug change(s)."
            : "Golden Bugs write did not fully verify. Check diagnostics.";
        SetStatus(
            GoldenBugsEditorStatusText.Text,
            verified ? StatusKind.Connected : StatusKind.Warning);
    }

    private async void AddAllGoldenBugs_Click(object sender, RoutedEventArgs e)
    {
        if (!TryReadGoldenBugsEditorBytes(out var currentBytes, out _))
        {
            return;
        }

        foreach (var bit in GoldenBugsEditorRows)
        {
            bit.IsSetDesired = true;
        }

        var desiredBytes = currentBytes.ToArray();
        foreach (var bit in GoldenBugsEditorRows)
        {
            SetGoldenBugBitInBytes(desiredBytes, bit, isSet: true);
        }

        var verified = await WriteGoldenBugsEditorBytesAsync("add-all", desiredBytes);
        RefreshGoldenBugsBitfieldFromMemory(preserveDirty: !verified);
        GoldenBugsEditorStatusText.Text = verified
            ? "Added all Golden Bugs."
            : "Add All did not fully verify. Check diagnostics.";
        SetStatus(
            GoldenBugsEditorStatusText.Text,
            verified ? StatusKind.Connected : StatusKind.Warning);
    }

    private async void ClearAllGoldenBugs_Click(object sender, RoutedEventArgs e)
    {
        if (!TryReadGoldenBugsEditorBytes(out var currentBytes, out _))
        {
            return;
        }

        foreach (var bit in GoldenBugsEditorRows)
        {
            bit.IsSetDesired = false;
        }

        var desiredBytes = currentBytes.ToArray();
        foreach (var bit in GoldenBugsEditorRows)
        {
            SetGoldenBugBitInBytes(desiredBytes, bit, isSet: false);
        }

        var verified = await WriteGoldenBugsEditorBytesAsync("clear-all", desiredBytes);
        RefreshGoldenBugsBitfieldFromMemory(preserveDirty: !verified);
        GoldenBugsEditorStatusText.Text = verified
            ? "Cleared all Golden Bugs."
            : "Clear All did not fully verify. Check diagnostics.";
        SetStatus(
            GoldenBugsEditorStatusText.Text,
            verified ? StatusKind.Connected : StatusKind.Warning);
    }

    private async void RestoreGoldenBugState_Click(object sender, RoutedEventArgs e)
    {
        if (_goldenBugsEditorRestoreSnapshotBytes is null)
        {
            GoldenBugsEditorStatusText.Text = "No previous Golden Bug state captured this session.";
            SetStatus("No previous Golden Bug state captured this session.", StatusKind.Neutral);
            return;
        }

        var verified = await WriteGoldenBugsEditorBytesAsync(
            "restore-previous",
            _goldenBugsEditorRestoreSnapshotBytes,
            capturePreviousState: false);
        RefreshGoldenBugsBitfieldFromMemory(preserveDirty: !verified);
        GoldenBugsEditorStatusText.Text = verified
            ? $"Restored Golden Bug state captured at {_goldenBugsEditorRestoreSnapshotCapturedAt?.ToLocalTime():g}."
            : "Restore did not fully verify. Check diagnostics.";
        SetStatus(
            GoldenBugsEditorStatusText.Text,
            verified ? StatusKind.Connected : StatusKind.Warning);
    }

    private void RefreshHiddenSkills_Click(object sender, RoutedEventArgs e)
    {
        RefreshHiddenSkillsEditor(preserveDirty: false, showStatus: true);
    }

    private async void ApplyHiddenSkillsChanges_Click(object sender, RoutedEventArgs e)
    {
        var normalized = NormalizeHiddenSkillDesiredProgression("apply-normalize", out var dependencyMessage);

        var dirtySkills = HiddenSkillsEditorRows
            .Where(skill => skill.IsDirty && skill.CanEdit)
            .ToList();
        if (dirtySkills.Count == 0)
        {
            HiddenSkillsEditorStatusText.Text = "No Hidden Skills changes to apply.";
            SetStatus("No Hidden Skills changes to apply.", StatusKind.Neutral);
            return;
        }

        if (!TryReadHiddenSkillsEditorBytes(out var currentBytes, out _))
        {
            return;
        }

        var desiredBytes = currentBytes.ToArray();
        foreach (var skill in dirtySkills)
        {
            SetHiddenSkillBitInBytes(desiredBytes, skill, skill.IsOwnedDesired);
        }

        var verified = await WriteHiddenSkillsEditorBytesAsync("apply", desiredBytes);
        RefreshHiddenSkillsEditor(preserveDirty: !verified, showStatus: false);
        HiddenSkillsEditorStatusText.Text = verified
            ? normalized
                ? $"{dependencyMessage} Applied {dirtySkills.Count} Hidden Skill change(s)."
                : $"Applied {dirtySkills.Count} Hidden Skill change(s)."
            : "Hidden Skills write did not fully verify. Check diagnostics.";
        SetStatus(
            HiddenSkillsEditorStatusText.Text,
            verified ? StatusKind.Connected : StatusKind.Warning);
    }

    private async void AddAllHiddenSkills_Click(object sender, RoutedEventArgs e)
    {
        if (!TryReadHiddenSkillsEditorBytes(out var currentBytes, out _))
        {
            return;
        }

        var previousSuppressHiddenSkillDependencyEnforcement = _suppressHiddenSkillDependencyEnforcement;
        _suppressHiddenSkillDependencyEnforcement = true;
        try
        {
            foreach (var skill in HiddenSkillsEditorRows)
            {
                skill.IsOwnedDesired = true;
            }
        }
        finally
        {
            _suppressHiddenSkillDependencyEnforcement = previousSuppressHiddenSkillDependencyEnforcement;
        }

        var desiredBytes = currentBytes.ToArray();
        foreach (var skill in HiddenSkillsEditorRows)
        {
            SetHiddenSkillBitInBytes(desiredBytes, skill, isSet: true);
        }

        var verified = await WriteHiddenSkillsEditorBytesAsync("add-all", desiredBytes);
        RefreshHiddenSkillsEditor(preserveDirty: !verified, showStatus: false);
        HiddenSkillsEditorStatusText.Text = verified
            ? "Added all Hidden Skills."
            : "Add All Hidden Skills did not fully verify. Check diagnostics.";
        SetStatus(
            HiddenSkillsEditorStatusText.Text,
            verified ? StatusKind.Connected : StatusKind.Warning);
    }

    private async void ClearAllHiddenSkills_Click(object sender, RoutedEventArgs e)
    {
        if (!TryReadHiddenSkillsEditorBytes(out var currentBytes, out _))
        {
            return;
        }

        var previousSuppressHiddenSkillDependencyEnforcement = _suppressHiddenSkillDependencyEnforcement;
        _suppressHiddenSkillDependencyEnforcement = true;
        try
        {
            foreach (var skill in HiddenSkillsEditorRows)
            {
                skill.IsOwnedDesired = false;
            }
        }
        finally
        {
            _suppressHiddenSkillDependencyEnforcement = previousSuppressHiddenSkillDependencyEnforcement;
        }

        var desiredBytes = currentBytes.ToArray();
        foreach (var skill in HiddenSkillsEditorRows)
        {
            SetHiddenSkillBitInBytes(desiredBytes, skill, isSet: false);
        }

        var verified = await WriteHiddenSkillsEditorBytesAsync("clear-all", desiredBytes);
        RefreshHiddenSkillsEditor(preserveDirty: !verified, showStatus: false);
        HiddenSkillsEditorStatusText.Text = verified
            ? "Cleared all Hidden Skills."
            : "Clear All Hidden Skills did not fully verify. Check diagnostics.";
        SetStatus(
            HiddenSkillsEditorStatusText.Text,
            verified ? StatusKind.Connected : StatusKind.Warning);
    }

    private async void RestoreHiddenSkills_Click(object sender, RoutedEventArgs e)
    {
        if (_hiddenSkillsRestoreSnapshotBytes is null)
        {
            HiddenSkillsEditorStatusText.Text = "No previous Hidden Skills state captured this session.";
            SetStatus("No previous Hidden Skills state captured this session.", StatusKind.Neutral);
            return;
        }

        var verified = await WriteHiddenSkillsEditorBytesAsync(
            "restore-previous",
            _hiddenSkillsRestoreSnapshotBytes,
            capturePreviousState: false);
        RefreshHiddenSkillsEditor(preserveDirty: !verified, showStatus: false);
        HiddenSkillsEditorStatusText.Text = verified
            ? $"Restored Hidden Skills state captured at {_hiddenSkillsRestoreSnapshotCapturedAt?.ToLocalTime():g}."
            : "Restore Hidden Skills did not fully verify. Check diagnostics.";
        SetStatus(
            HiddenSkillsEditorStatusText.Text,
            verified ? StatusKind.Connected : StatusKind.Warning);
    }

    private void GoldenBugsCaptureBitfield_Click(object sender, RoutedEventArgs e)
    {
        if (!TryReadGoldenBugsBitfieldBytes(out var bytes, out _))
        {
            return;
        }

        _goldenBugsBitfieldRestoreSnapshotBytes = bytes.ToArray();
        _goldenBugsBitfieldRestoreSnapshotCapturedAt = DateTimeOffset.Now;
        UpdateGoldenBugsBitRows(bytes, preserveDirty: false);
        GoldenBugsBitfieldStatusText.Text =
            $"Captured Golden Bugs bitfield snapshot: {FormatByteArray(bytes)}.";
        AppendGoldenBugsBitfieldTestingLog(
            $"action=capture offset=0x{GoldenBugsDefinitions.FirstOffset:X} length=0x{GoldenBugsDefinitions.ByteCount:X} bytes=\"{FormatByteArray(bytes)}\"");
        SetStatus("Golden Bugs bitfield snapshot captured.", StatusKind.Connected);
    }

    private async void GoldenBugsApplyBitChanges_Click(object sender, RoutedEventArgs e)
    {
        var dirtyBits = GoldenBugsBitRows
            .Where(bit => bit.IsDirty && bit.CanEdit)
            .ToList();
        if (dirtyBits.Count == 0)
        {
            GoldenBugsBitfieldStatusText.Text = "No Golden Bugs bit changes to apply.";
            SetStatus("No Golden Bugs bit changes to apply.", StatusKind.Neutral);
            return;
        }

        var successfulWrites = 0;
        foreach (var bit in dirtyBits)
        {
            if (await WriteGoldenBugBitWithDiagnosticsAsync(bit))
            {
                successfulWrites++;
            }
        }

        RefreshGoldenBugsBitfieldFromMemory();
        GoldenBugsBitfieldStatusText.Text =
            $"Applied {successfulWrites} of {dirtyBits.Count} Golden Bugs bit change(s).";
        SetStatus(
            $"Applied {successfulWrites} of {dirtyBits.Count} Golden Bugs bit change(s).",
            successfulWrites == dirtyBits.Count ? StatusKind.Connected : StatusKind.Warning);
    }

    private async void GoldenBugsRestoreBitfield_Click(object sender, RoutedEventArgs e)
    {
        if (_goldenBugsBitfieldRestoreSnapshotBytes is null)
        {
            GoldenBugsBitfieldStatusText.Text = "Capture current bitfield before restoring.";
            SetStatus("Capture a Golden Bugs bitfield snapshot before restoring.", StatusKind.Neutral);
            return;
        }

        if (await RestoreGoldenBugsBitfieldSnapshotAsync(_goldenBugsBitfieldRestoreSnapshotBytes))
        {
            RefreshGoldenBugsBitfieldFromMemory();
            GoldenBugsBitfieldStatusText.Text =
                $"Restored Golden Bugs bitfield snapshot captured at {_goldenBugsBitfieldRestoreSnapshotCapturedAt?.ToLocalTime():g}.";
            SetStatus("Golden Bugs bitfield snapshot restored.", StatusKind.Connected);
        }
    }

    private void GoldenBugsExportBitfieldReport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Directory.CreateDirectory(ResearchSnapshotStore.ExportDirectory);
            var report = CreateGoldenBugsBitfieldReport();
            var baseName = $"{report.Timestamp:yyyyMMdd_HHmmss}_golden-bugs-bitfield-report";
            var jsonPath = Path.Combine(ResearchSnapshotStore.ExportDirectory, baseName + ".json");
            var csvPath = Path.Combine(ResearchSnapshotStore.ExportDirectory, baseName + ".csv");

            File.WriteAllText(jsonPath, JsonSerializer.Serialize(report, ExportJsonOptions));
            File.WriteAllLines(csvPath, CreateGoldenBugsBitfieldReportCsvLines(report.Rows));

            GoldenBugsBitfieldStatusText.Text =
                $"Exported {Path.GetFileName(jsonPath)} and {Path.GetFileName(csvPath)}.";
            AppendGoldenBugsBitfieldTestingLog(
                $"action=export json=\"{jsonPath}\" csv=\"{csvPath}\" rows={report.Rows.Count}");
            SetStatus("Golden Bugs bitfield report exported to logs/research.", StatusKind.Connected);
        }
        catch (Exception ex)
        {
            GoldenBugsBitfieldStatusText.Text = $"Export failed: {ex.Message}";
            SetStatus("Golden Bugs bitfield report export failed.", StatusKind.Warning);
        }
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

    private void RefreshBombSlots_Click(object sender, RoutedEventArgs e)
    {
        RefreshBombSlots(showStatus: true);
    }

    private async void ApplyBombSlotChanges_Click(object sender, RoutedEventArgs e)
    {
        if (!CanWriteBombSlots())
        {
            return;
        }

        var changedSlots = BombSlots
            .Where(slot => slot.IsDirty)
            .ToList();
        if (changedSlots.Count == 0)
        {
            BombSlotEditorStatusText.Text = "No Bomb Slot changes to apply.";
            SetStatus("No Bomb Slot changes to apply.", StatusKind.Neutral);
            return;
        }

        var verifiedCount = 0;
        foreach (var slot in changedSlots)
        {
            var verified = await WriteBombSlotWithDiagnosticsAsync(
                slot,
                slot.SelectedContent.Value,
                "apply",
                $"Bomb Slot {slot.SlotNumber} changed to {slot.SelectedContent.Name}.");
            if (verified)
            {
                verifiedCount++;
            }
        }

        RefreshBombSlots(showStatus: false);
        BombSlotEditorStatusText.Text =
            verifiedCount == changedSlots.Count
                ? $"Verification succeeded. Applied {verifiedCount} Bomb Slot change(s)."
                : $"Verification failed for {changedSlots.Count - verifiedCount} of {changedSlots.Count} Bomb Slot change(s).";
        SetStatus(BombSlotEditorStatusText.Text, verifiedCount == changedSlots.Count ? StatusKind.Connected : StatusKind.Warning);
    }

    private async void RestoreBombSlots_Click(object sender, RoutedEventArgs e)
    {
        if (!CanWriteBombSlots())
        {
            return;
        }

        var restorableSlots = BombSlots
            .Where(slot => slot.PreviousValue.HasValue && BombSlotDefinitions.IsConfirmedContent(slot.PreviousValue.Value))
            .ToList();
        if (restorableSlots.Count == 0)
        {
            BombSlotEditorStatusText.Text = "No confirmed previous Bomb Slot values are captured this session.";
            SetStatus("No confirmed previous Bomb Slot values are captured this session.", StatusKind.Neutral);
            return;
        }

        var verifiedCount = 0;
        foreach (var slot in restorableSlots)
        {
            var previousValue = slot.PreviousValue!.Value;
            var verified = await WriteBombSlotWithDiagnosticsAsync(
                slot,
                previousValue,
                "restore",
                $"Bomb Slot {slot.SlotNumber} restored to {BombSlotDefinitions.GetContentName(previousValue)}.");
            if (verified)
            {
                slot.ClearPrevious();
                verifiedCount++;
            }
        }

        RefreshBombSlots(showStatus: false);
        BombSlotEditorStatusText.Text =
            verifiedCount == restorableSlots.Count
                ? $"Verification succeeded. Restored {verifiedCount} Bomb Slot value(s)."
                : $"Verification failed for {restorableSlots.Count - verifiedCount} of {restorableSlots.Count} Bomb Slot restore(s).";
        SetStatus(BombSlotEditorStatusText.Text, verifiedCount == restorableSlots.Count ? StatusKind.Connected : StatusKind.Warning);
    }

    private void CreateSupportSnapshot_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var snapshotPath = CreateSupportSnapshot();
            SupportSnapshotStatusText.Text = $"Created support snapshot: {Path.GetFileName(snapshotPath)}";
            SetStatus("Support snapshot created.", StatusKind.Connected);
        }
        catch (Exception ex)
        {
            SupportSnapshotStatusText.Text = $"Support snapshot failed: {ex.Message}";
            SetStatus("Support snapshot failed.", StatusKind.Warning);
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

            if (!RefreshBombSlots(showStatus: false))
            {
                return;
            }

            if (!_equipmentDiagnosticInProgress && !RefreshEquipment(showStatus: false))
            {
                return;
            }

            if (!RefreshHiddenSkillsEditor(preserveDirty: true, showStatus: false))
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
                UpdateGoldenBugsResearchCurrentDisplay(rawValue);
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

    private bool RefreshBombSlots(bool showStatus)
    {
        if (_memory is null || !_playerBaseAddress.HasValue)
        {
            if (showStatus)
            {
                BombSlotEditorStatusText.Text = "Not attached. Attach to Cemu and rescan before refreshing Bomb Slots.";
                SetStatus("Not attached. Attach to Cemu and rescan before refreshing Bomb Slots.", StatusKind.Neutral);
            }

            foreach (var slot in BombSlots)
            {
                slot.CanEdit = false;
            }

            return false;
        }

        foreach (var slot in BombSlots)
        {
            if (!InventoryMemoryService.TryReadByte(
                    _memory,
                    _playerBaseAddress.Value,
                    slot.OffsetValue,
                    $"Bomb Slot {slot.SlotNumber}",
                    out var value,
                    out var error))
            {
                BombSlotEditorStatusText.Text = error;
                MarkMemoryUnavailable();
                return false;
            }

            slot.SetCurrentValue(value);
            slot.CanEdit = HasPlayerData;
        }

        if (showStatus)
        {
            BombSlotEditorStatusText.Text = "Bomb Slots refreshed. Confirmed values are Normal Bombs, Water Bombs, and Bomblings.";
            SetStatus("Bomb Slots refreshed.", StatusKind.Connected);
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

    private bool CanWriteBombSlots()
    {
        if (_memory is null || !_playerBaseAddress.HasValue)
        {
            BombSlotEditorStatusText.Text = "Not attached. Attach to Cemu and rescan before editing Bomb Slots.";
            SetStatus("Not attached. Attach to Cemu and rescan before editing Bomb Slots.", StatusKind.Neutral);
            return false;
        }

        if (!HasPlayerData)
        {
            BombSlotEditorStatusText.Text = "Player data is not available. Load into gameplay and rescan before editing Bomb Slots.";
            SetStatus("Player data is not available. Load into gameplay and rescan before editing Bomb Slots.", StatusKind.Warning);
            return false;
        }

        return true;
    }

    private async Task<bool> WriteBombSlotWithDiagnosticsAsync(
        BombSlotViewModel slot,
        byte desiredValue,
        string action,
        string successMessage)
    {
        var memory = _memory;
        if (memory is null || !_playerBaseAddress.HasValue)
        {
            BombSlotEditorStatusText.Text = "Not attached. Attach to Cemu and rescan before editing Bomb Slots.";
            SetStatus("Not attached. Attach to Cemu and rescan before editing Bomb Slots.", StatusKind.Neutral);
            return false;
        }

        if (!BombSlotDefinitions.IsConfirmedContent(desiredValue))
        {
            BombSlotEditorStatusText.Text = $"Unsupported Bomb Slot value 0x{desiredValue:X2}.";
            SetStatus("Unsupported Bomb Slot value.", StatusKind.Warning);
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
            if (!InventoryMemoryService.TryReadByte(
                    memory,
                    _playerBaseAddress.Value,
                    slot.OffsetValue,
                    $"Bomb Slot {slot.SlotNumber}",
                    out var oldSlotValue,
                    out var oldReadError))
            {
                diagnosticStatus = $"old-read-failed: {oldReadError}";
                slot.Status = oldReadError;
                slot.LastWriteResult = oldReadError;
                slot.LastVerificationResult = "Old value could not be read.";
                BombSlotEditorStatusText.Text = oldReadError;
                SetStatus(oldReadError, StatusKind.Warning);
                return false;
            }

            oldValue = oldSlotValue;
            if (action is not "restore")
            {
                slot.CapturePrevious(oldSlotValue);
            }

            if (!memory.TryWriteBytes(absoluteAddress, [desiredValue], out var writeError))
            {
                diagnosticStatus = $"write-call-failed: {writeError}";
                slot.Status = $"Write failed: {writeError}";
                slot.LastWriteResult = slot.Status;
                slot.LastVerificationResult = "Write call failed.";
                BombSlotEditorStatusText.Text = slot.Status;
                SetStatus(slot.Status, StatusKind.Warning);
                return false;
            }

            slot.LastWriteResult = $"Wrote {FormatEquipmentByte(desiredValue)}.";

            if (!InventoryMemoryService.TryReadByte(
                    memory,
                    _playerBaseAddress.Value,
                    slot.OffsetValue,
                    $"Bomb Slot {slot.SlotNumber}",
                    out var immediateValue,
                    out var immediateReadError))
            {
                diagnosticStatus = $"immediate-read-failed: {immediateReadError}";
                slot.Status = immediateReadError;
                slot.LastVerificationResult = "Immediate readback failed.";
                BombSlotEditorStatusText.Text = immediateReadError;
                SetStatus(immediateReadError, StatusKind.Warning);
                return false;
            }

            immediateReadback = immediateValue;
            if (immediateReadback.Value != desiredValue)
            {
                diagnosticStatus = "immediate-mismatch";
                slot.Status = $"Write failed: expected {desiredValue} but read {immediateReadback.Value}.";
                slot.LastVerificationResult = slot.Status;
                BombSlotEditorStatusText.Text = slot.Status;
                SetStatus(slot.Status, StatusKind.Warning);
                return false;
            }

            await Task.Delay(250);
            if (InventoryMemoryService.TryReadByte(
                    memory,
                    _playerBaseAddress.Value,
                    slot.OffsetValue,
                    $"Bomb Slot {slot.SlotNumber}",
                    out var delayed250Value,
                    out _))
            {
                delayed250Readback = delayed250Value;
            }

            await Task.Delay(750);
            if (InventoryMemoryService.TryReadByte(
                    memory,
                    _playerBaseAddress.Value,
                    slot.OffsetValue,
                    $"Bomb Slot {slot.SlotNumber}",
                    out var delayed1000Value,
                    out _))
            {
                delayed1000Readback = delayed1000Value;
            }

            var verificationFailed =
                delayed250Readback.HasValue && delayed250Readback.Value != desiredValue ||
                delayed1000Readback.HasValue && delayed1000Readback.Value != desiredValue;
            if (verificationFailed)
            {
                diagnosticStatus = "verification-failed";
                slot.Status = "Verification failed.";
                slot.LastVerificationResult = "Immediate readback matched, but delayed verification changed.";
                BombSlotEditorStatusText.Text = $"Bomb Slot {slot.SlotNumber}: Verification failed.";
                SetStatus(BombSlotEditorStatusText.Text, StatusKind.Warning);
            }
            else
            {
                diagnosticStatus = "verified";
                slot.Status = "Write verified.";
                slot.LastVerificationResult = "Immediate, 250ms, and 1000ms readbacks matched.";
                BombSlotEditorStatusText.Text = successMessage;
                SetStatus(successMessage, StatusKind.Connected);
            }

            return !verificationFailed;
        }
        finally
        {
            AppendBombSlotEditorDiagnostic(
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

    private void AppendBombSlotEditorDiagnostic(
        BombSlotViewModel slot,
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
            $"{DateTimeOffset.Now:O} kind=bomb-slot-editor action={action} " +
            $"slot={slot.SlotNumber} offset={slot.Offset} address=0x{absoluteAddress:X} " +
            $"previous={FormatEquipmentByte(oldValue)} new={FormatEquipmentByte(writtenValue)} " +
            $"immediate={FormatEquipmentByte(immediateReadback)} read250ms={FormatEquipmentByte(delayed250Readback)} " +
            $"read1000ms={FormatEquipmentByte(delayed1000Readback)} status={diagnosticStatus}";

        AppendBombSlotEditorLogEntry(entry);
    }

    private void AppendBombSlotEditorLogEntry(string entry)
    {
        BombSlotEditorDiagnostics.Insert(0, entry);
        while (BombSlotEditorDiagnostics.Count > 100)
        {
            BombSlotEditorDiagnostics.RemoveAt(BombSlotEditorDiagnostics.Count - 1);
        }

        try
        {
            var logDirectory = Path.GetDirectoryName(BombSlotEditorLogPath);
            if (!string.IsNullOrWhiteSpace(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            File.AppendAllText(BombSlotEditorLogPath, entry + Environment.NewLine);
        }
        catch (Exception ex)
        {
            BombSlotEditorDiagnostics.Insert(0, $"{DateTimeOffset.Now:O} bomb-slot-editor-log-write-failed: {ex.Message}");
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

    private bool TryReadHiddenSkillsBitTestByte(
        out uint offset,
        out int bit,
        out byte value,
        out ulong absoluteAddress)
    {
        offset = 0;
        bit = 0;
        value = 0;
        absoluteAddress = 0;

        if (_memory is null || !_playerBaseAddress.HasValue)
        {
            HiddenSkillsBitAbsoluteAddressText.Text = "-";
            HiddenSkillsBitCurrentByteText.Text = "Not attached";
            HiddenSkillsBitCurrentStateText.Text = "Not attached";
            HiddenSkillsBitStatusText.Text = "Not attached. Attach to Cemu and rescan before testing Hidden Skills bits.";
            SetStatus("Not attached. Attach to Cemu and rescan before testing Hidden Skills bits.", StatusKind.Neutral);
            return false;
        }

        if (!TryParseResearchOffset(HiddenSkillsBitOffsetText.Text, out offset, out var offsetError))
        {
            HiddenSkillsBitAbsoluteAddressText.Text = "-";
            HiddenSkillsBitStatusText.Text = offsetError;
            return false;
        }

        if (!TryParseHiddenSkillsBit(HiddenSkillsBitIndexText.Text, out bit, out var bitError))
        {
            HiddenSkillsBitStatusText.Text = bitError;
            return false;
        }

        absoluteAddress = _playerBaseAddress.Value + offset;
        if (!TryReadCandidateByteAt(offset, absoluteAddress, out value, out var readError))
        {
            HiddenSkillsBitAbsoluteAddressText.Text = $"0x{absoluteAddress:X}";
            HiddenSkillsBitCurrentByteText.Text = "Read failed";
            HiddenSkillsBitCurrentStateText.Text = "Read failed";
            HiddenSkillsBitStatusText.Text = readError;
            SetStatus("Could not read Hidden Skills bit test byte.", StatusKind.Warning);
            return false;
        }

        return true;
    }

    private static bool TryParseHiddenSkillsBit(string text, out int bit, out string error)
    {
        bit = 0;
        error = string.Empty;

        if (!int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out bit) ||
            bit is < 0 or > 7)
        {
            error = "Bit must be between 0 and 7.";
            return false;
        }

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

    private bool TryReadGoldenBugsResearchRange(out uint startOffset, out byte[] bytes, out ulong absoluteAddress)
    {
        startOffset = 0;
        bytes = [];
        absoluteAddress = 0;

        if (_memory is null || !_playerBaseAddress.HasValue)
        {
            GoldenBugsResearchStatusText.Text = "Not attached. Attach to Cemu and rescan before using Golden Bugs research.";
            SetStatus("Not attached. Attach to Cemu and rescan before using Golden Bugs research.", StatusKind.Neutral);
            return false;
        }

        if (!TryParseResearchOffset(GoldenBugsResearchStartOffsetText.Text, out startOffset, out var offsetError))
        {
            GoldenBugsResearchStatusText.Text = offsetError;
            return false;
        }

        if (!TryParseResearchLength(GoldenBugsResearchLengthText.Text, out var length, out var lengthError))
        {
            GoldenBugsResearchStatusText.Text = lengthError;
            return false;
        }

        absoluteAddress = _playerBaseAddress.Value + startOffset;
        if (!_memory.TryReadBytes(absoluteAddress, length, out bytes, out var bytesRead) || bytesRead != length)
        {
            GoldenBugsResearchStatusText.Text = $"Could not read 0x{length:X} byte(s) at _playerbase+0x{startOffset:X}.";
            SetStatus("Could not read Golden Bugs research range.", StatusKind.Warning);
            return false;
        }

        return true;
    }

    private bool TryReadQuestItemsResearchRange(out uint startOffset, out byte[] bytes, out ulong absoluteAddress)
    {
        startOffset = 0;
        bytes = [];
        absoluteAddress = 0;

        if (_memory is null || !_playerBaseAddress.HasValue)
        {
            QuestItemsResearchStatusText.Text = "Not attached. Attach to Cemu and rescan before using Quest Items research.";
            SetStatus("Not attached. Attach to Cemu and rescan before using Quest Items research.", StatusKind.Neutral);
            return false;
        }

        if (!TryParseResearchOffset(QuestItemsResearchStartOffsetText.Text, out startOffset, out var offsetError))
        {
            QuestItemsResearchStatusText.Text = offsetError;
            return false;
        }

        if (!TryParseResearchLength(QuestItemsResearchLengthText.Text, out var length, out var lengthError))
        {
            QuestItemsResearchStatusText.Text = lengthError;
            return false;
        }

        absoluteAddress = _playerBaseAddress.Value + startOffset;
        if (!_memory.TryReadBytes(absoluteAddress, length, out bytes, out var bytesRead) || bytesRead != length)
        {
            QuestItemsResearchStatusText.Text = $"Could not read 0x{length:X} byte(s) at _playerbase+0x{startOffset:X}.";
            SetStatus("Could not read Quest Items research range.", StatusKind.Warning);
            return false;
        }

        return true;
    }

    private bool TryReadHiddenSkillsResearchRange(out uint startOffset, out byte[] bytes, out ulong absoluteAddress)
    {
        startOffset = 0;
        bytes = [];
        absoluteAddress = 0;

        if (_memory is null || !_playerBaseAddress.HasValue)
        {
            HiddenSkillsResearchStatusText.Text = "Not attached. Attach to Cemu and rescan before using Hidden Skills research.";
            SetStatus("Not attached. Attach to Cemu and rescan before using Hidden Skills research.", StatusKind.Neutral);
            return false;
        }

        if (!TryParseResearchOffset(HiddenSkillsResearchStartOffsetText.Text, out startOffset, out var offsetError))
        {
            HiddenSkillsResearchStatusText.Text = offsetError;
            return false;
        }

        if (!TryParseResearchLength(HiddenSkillsResearchLengthText.Text, out var length, out var lengthError))
        {
            HiddenSkillsResearchStatusText.Text = lengthError;
            return false;
        }

        absoluteAddress = _playerBaseAddress.Value + startOffset;
        if (!_memory.TryReadBytes(absoluteAddress, length, out bytes, out var bytesRead) || bytesRead != length)
        {
            HiddenSkillsResearchStatusText.Text = $"Could not read 0x{length:X} byte(s) at _playerbase+0x{startOffset:X}.";
            SetStatus("Could not read Hidden Skills research range.", StatusKind.Warning);
            return false;
        }

        return true;
    }

    private bool TryReadGoldenBugsBitfieldBytes(out byte[] bytes, out ulong absoluteAddress)
    {
        bytes = [];
        absoluteAddress = 0;

        if (_memory is null || !_playerBaseAddress.HasValue)
        {
            GoldenBugsBitfieldStatusText.Text = "Not attached. Attach to Cemu and rescan before using the Golden Bugs bitfield tester.";
            SetStatus("Not attached. Attach to Cemu and rescan before using Golden Bugs bitfield tester.", StatusKind.Neutral);
            return false;
        }

        absoluteAddress = _playerBaseAddress.Value + GoldenBugsDefinitions.FirstOffset;
        if (!_memory.TryReadBytes(absoluteAddress, GoldenBugsDefinitions.ByteCount, out bytes, out var bytesRead) ||
            bytesRead != GoldenBugsDefinitions.ByteCount)
        {
            GoldenBugsBitfieldStatusText.Text = "Could not read Golden Bugs bitfield bytes.";
            SetStatus("Could not read Golden Bugs bitfield bytes.", StatusKind.Warning);
            return false;
        }

        return true;
    }

    private bool TryReadGoldenBugsEditorBytes(out byte[] bytes, out ulong absoluteAddress)
    {
        bytes = [];
        absoluteAddress = 0;

        if (_memory is null || !_playerBaseAddress.HasValue)
        {
            GoldenBugsEditorStatusText.Text = "Not attached. Attach to Cemu and rescan before editing Golden Bugs.";
            SetStatus("Not attached. Attach to Cemu and rescan before editing Golden Bugs.", StatusKind.Neutral);
            return false;
        }

        absoluteAddress = _playerBaseAddress.Value + GoldenBugsDefinitions.FirstOffset;
        if (!_memory.TryReadBytes(absoluteAddress, GoldenBugsDefinitions.OwnershipByteCount, out bytes, out var bytesRead) ||
            bytesRead != GoldenBugsDefinitions.OwnershipByteCount)
        {
            GoldenBugsEditorStatusText.Text = "Could not read Golden Bugs ownership bytes.";
            SetStatus("Could not read Golden Bugs ownership bytes.", StatusKind.Warning);
            return false;
        }

        return true;
    }

    private bool TryReadHiddenSkillsEditorBytes(out byte[] bytes, out ulong absoluteAddress)
    {
        bytes = [];
        absoluteAddress = 0;

        if (_memory is null || !_playerBaseAddress.HasValue)
        {
            HiddenSkillsEditorStatusText.Text = "Not attached. Attach to Cemu and rescan before editing Hidden Skills.";
            SetStatus("Not attached. Attach to Cemu and rescan before editing Hidden Skills.", StatusKind.Neutral);
            return false;
        }

        if (!HasPlayerData)
        {
            HiddenSkillsEditorStatusText.Text = "Player data is not available. Load into gameplay and rescan before editing Hidden Skills.";
            SetStatus("Load into gameplay and rescan before editing Hidden Skills.", StatusKind.Neutral);
            return false;
        }

        absoluteAddress = _playerBaseAddress.Value + HiddenSkillsDefinitions.FirstOffset;
        if (!_memory.TryReadBytes(absoluteAddress, HiddenSkillsDefinitions.OwnershipByteCount, out bytes, out var bytesRead) ||
            bytesRead != HiddenSkillsDefinitions.OwnershipByteCount)
        {
            HiddenSkillsEditorStatusText.Text = "Could not read Hidden Skills ownership bytes.";
            SetStatus("Could not read Hidden Skills ownership bytes.", StatusKind.Warning);
            return false;
        }

        return true;
    }

    private bool RefreshHiddenSkillsEditor(bool preserveDirty = true, bool showStatus = false)
    {
        if (!HasPlayerData)
        {
            MarkHiddenSkillsNotRead();
            if (showStatus)
            {
                SetStatus("Player data is not available. Load into gameplay and rescan before editing Hidden Skills.", StatusKind.Neutral);
            }

            return true;
        }

        if (!TryReadHiddenSkillsEditorBytes(out var bytes, out _))
        {
            return false;
        }

        UpdateHiddenSkillsEditorRows(bytes, preserveDirty);
        if (showStatus)
        {
            HiddenSkillsEditorStatusText.Text = "Hidden Skills refreshed.";
            SetStatus("Hidden Skills refreshed.", StatusKind.Connected);
        }

        return true;
    }

    private void RefreshGoldenBugsBitfieldFromMemory(bool preserveDirty = true)
    {
        if (!TryReadGoldenBugsBitfieldBytes(out var bytes, out _))
        {
            return;
        }

        UpdateGoldenBugsBitRows(bytes, preserveDirty);
        var rawValue =
            (uint)bytes[0] << 24 |
            (uint)bytes[1] << 16 |
            (uint)bytes[2] << 8 |
            bytes[3];
        GoldenBugsFlags.SetCurrentDisplay($"0x{rawValue:X8}");
        UpdateGoldenBugsResearchCurrentDisplay(rawValue, preserveDirty);
    }

    private async Task<bool> WriteGoldenBugsEditorBytesAsync(
        string operation,
        byte[] desiredBytes,
        bool capturePreviousState = true)
    {
        var memory = _memory;
        if (memory is null || !_playerBaseAddress.HasValue)
        {
            GoldenBugsEditorStatusText.Text = "Not attached.";
            SetStatus("Not attached. Attach to Cemu and rescan before editing Golden Bugs.", StatusKind.Neutral);
            return false;
        }

        if (desiredBytes.Length != GoldenBugsDefinitions.OwnershipByteCount)
        {
            GoldenBugsEditorStatusText.Text = "Golden Bugs desired state has an invalid length.";
            SetStatus("Golden Bugs desired state has an invalid length.", StatusKind.Warning);
            return false;
        }

        var absoluteAddress = _playerBaseAddress.Value + GoldenBugsDefinitions.FirstOffset;
        byte[]? beforeBytes = null;
        byte[]? immediateBytes = null;
        byte[]? delayed250Bytes = null;
        byte[]? delayed1000Bytes = null;
        var status = "started";

        try
        {
            if (!memory.TryReadBytes(absoluteAddress, GoldenBugsDefinitions.OwnershipByteCount, out var currentBytes, out var currentBytesRead) ||
                currentBytesRead != GoldenBugsDefinitions.OwnershipByteCount)
            {
                status = "before-read-failed";
                GoldenBugsEditorStatusText.Text = "Could not read Golden Bugs before writing.";
                SetStatus("Could not read Golden Bugs before writing.", StatusKind.Warning);
                return false;
            }

            beforeBytes = currentBytes;
            if (capturePreviousState && !beforeBytes.SequenceEqual(desiredBytes))
            {
                _goldenBugsEditorRestoreSnapshotBytes = beforeBytes.ToArray();
                _goldenBugsEditorRestoreSnapshotCapturedAt = DateTimeOffset.Now;
            }

            if (beforeBytes.SequenceEqual(desiredBytes))
            {
                immediateBytes = beforeBytes.ToArray();
                delayed250Bytes = beforeBytes.ToArray();
                delayed1000Bytes = beforeBytes.ToArray();
                status = "no-change";
                return true;
            }

            for (var index = 0; index < desiredBytes.Length; index++)
            {
                if (beforeBytes[index] == desiredBytes[index])
                {
                    continue;
                }

                if (!memory.TryWriteBytes(absoluteAddress + (uint)index, [desiredBytes[index]], out var writeError))
                {
                    status = $"write-failed-index-{index}: {writeError}";
                    GoldenBugsEditorStatusText.Text = $"Golden Bugs write failed at 0x{GoldenBugsDefinitions.FirstOffset + (uint)index:X}: {writeError}";
                    SetStatus("Golden Bugs write failed.", StatusKind.Warning);
                    return false;
                }
            }

            if (!memory.TryReadBytes(absoluteAddress, GoldenBugsDefinitions.OwnershipByteCount, out immediateBytes, out var immediateRead) ||
                immediateRead != GoldenBugsDefinitions.OwnershipByteCount)
            {
                status = "immediate-read-failed";
                SetStatus("Golden Bugs immediate verification failed.", StatusKind.Warning);
                return false;
            }

            if (!desiredBytes.SequenceEqual(immediateBytes))
            {
                status = "immediate-mismatch";
                SetStatus("Golden Bugs immediate readback mismatch.", StatusKind.Warning);
                return false;
            }

            await Task.Delay(250);
            if (memory.TryReadBytes(absoluteAddress, GoldenBugsDefinitions.OwnershipByteCount, out var read250, out var read250Count) &&
                read250Count == GoldenBugsDefinitions.OwnershipByteCount)
            {
                delayed250Bytes = read250;
            }

            await Task.Delay(750);
            if (memory.TryReadBytes(absoluteAddress, GoldenBugsDefinitions.OwnershipByteCount, out var read1000, out var read1000Count) &&
                read1000Count == GoldenBugsDefinitions.OwnershipByteCount)
            {
                delayed1000Bytes = read1000;
            }

            var delayedMismatch =
                delayed250Bytes is not null && !desiredBytes.SequenceEqual(delayed250Bytes) ||
                delayed1000Bytes is not null && !desiredBytes.SequenceEqual(delayed1000Bytes);
            status = delayedMismatch ? "delayed-mismatch" : "verified";
            if (delayedMismatch)
            {
                SetStatus("Golden Bugs changed after delayed verification.", StatusKind.Warning);
            }

            return !delayedMismatch;
        }
        finally
        {
            AppendGoldenBugsEditorDiagnostic(
                operation,
                beforeBytes,
                desiredBytes,
                immediateBytes,
                delayed250Bytes,
                delayed1000Bytes,
                status);
        }
    }

    private async Task<bool> WriteHiddenSkillsEditorBytesAsync(
        string operation,
        byte[] desiredBytes,
        bool capturePreviousState = true)
    {
        var memory = _memory;
        if (memory is null || !_playerBaseAddress.HasValue)
        {
            HiddenSkillsEditorStatusText.Text = "Not attached.";
            SetStatus("Not attached. Attach to Cemu and rescan before editing Hidden Skills.", StatusKind.Neutral);
            return false;
        }

        if (desiredBytes.Length != HiddenSkillsDefinitions.OwnershipByteCount)
        {
            HiddenSkillsEditorStatusText.Text = "Hidden Skills desired state has an invalid length.";
            SetStatus("Hidden Skills desired state has an invalid length.", StatusKind.Warning);
            return false;
        }

        var absoluteAddress = _playerBaseAddress.Value + HiddenSkillsDefinitions.FirstOffset;
        byte[]? beforeBytes = null;
        byte[]? immediateBytes = null;
        byte[]? delayed250Bytes = null;
        byte[]? delayed1000Bytes = null;
        var status = "started";

        try
        {
            if (!memory.TryReadBytes(absoluteAddress, HiddenSkillsDefinitions.OwnershipByteCount, out var currentBytes, out var currentBytesRead) ||
                currentBytesRead != HiddenSkillsDefinitions.OwnershipByteCount)
            {
                status = "before-read-failed";
                HiddenSkillsEditorStatusText.Text = "Could not read Hidden Skills before writing.";
                SetStatus("Could not read Hidden Skills before writing.", StatusKind.Warning);
                return false;
            }

            beforeBytes = currentBytes;
            if (capturePreviousState && !beforeBytes.SequenceEqual(desiredBytes))
            {
                _hiddenSkillsRestoreSnapshotBytes = beforeBytes.ToArray();
                _hiddenSkillsRestoreSnapshotCapturedAt = DateTimeOffset.Now;
            }

            if (beforeBytes.SequenceEqual(desiredBytes))
            {
                immediateBytes = beforeBytes.ToArray();
                delayed250Bytes = beforeBytes.ToArray();
                delayed1000Bytes = beforeBytes.ToArray();
                status = "no-change";
                SetHiddenSkillsRowWriteStatuses(desiredBytes, "No change", "Already matched desired state");
                return true;
            }

            for (var index = 0; index < desiredBytes.Length; index++)
            {
                if (beforeBytes[index] == desiredBytes[index])
                {
                    continue;
                }

                if (!memory.TryWriteBytes(absoluteAddress + (uint)index, [desiredBytes[index]], out var writeError))
                {
                    status = $"write-failed-index-{index}: {writeError}";
                    HiddenSkillsEditorStatusText.Text =
                        $"Hidden Skills write failed at 0x{HiddenSkillsDefinitions.FirstOffset + (uint)index:X}: {writeError}";
                    SetStatus("Hidden Skills write failed.", StatusKind.Warning);
                    SetHiddenSkillsRowWriteStatuses(desiredBytes, "Write failed", status);
                    return false;
                }
            }

            if (!memory.TryReadBytes(absoluteAddress, HiddenSkillsDefinitions.OwnershipByteCount, out immediateBytes, out var immediateRead) ||
                immediateRead != HiddenSkillsDefinitions.OwnershipByteCount)
            {
                status = "immediate-read-failed";
                SetStatus("Hidden Skills immediate verification failed.", StatusKind.Warning);
                SetHiddenSkillsRowWriteStatuses(desiredBytes, "Written", "Immediate read failed");
                return false;
            }

            if (!desiredBytes.SequenceEqual(immediateBytes))
            {
                status = "immediate-mismatch";
                SetStatus("Hidden Skills immediate readback mismatch.", StatusKind.Warning);
                SetHiddenSkillsRowWriteStatuses(desiredBytes, "Written", "Immediate readback mismatch");
                return false;
            }

            await Task.Delay(250);
            if (memory.TryReadBytes(absoluteAddress, HiddenSkillsDefinitions.OwnershipByteCount, out var read250, out var read250Count) &&
                read250Count == HiddenSkillsDefinitions.OwnershipByteCount)
            {
                delayed250Bytes = read250;
            }

            await Task.Delay(750);
            if (memory.TryReadBytes(absoluteAddress, HiddenSkillsDefinitions.OwnershipByteCount, out var read1000, out var read1000Count) &&
                read1000Count == HiddenSkillsDefinitions.OwnershipByteCount)
            {
                delayed1000Bytes = read1000;
            }

            var delayedMismatch =
                delayed250Bytes is not null && !desiredBytes.SequenceEqual(delayed250Bytes) ||
                delayed1000Bytes is not null && !desiredBytes.SequenceEqual(delayed1000Bytes);
            status = delayedMismatch ? "delayed-mismatch" : "verified";
            SetHiddenSkillsRowWriteStatuses(
                desiredBytes,
                "Written",
                delayedMismatch ? "Delayed verification mismatch" : "Verified immediate/250ms/1000ms");
            if (delayedMismatch)
            {
                SetStatus("Hidden Skills changed after delayed verification.", StatusKind.Warning);
            }

            return !delayedMismatch;
        }
        finally
        {
            AppendHiddenSkillsEditorDiagnostic(
                operation,
                beforeBytes,
                desiredBytes,
                immediateBytes,
                delayed250Bytes,
                delayed1000Bytes,
                status);
        }
    }

    private async Task<bool> WriteGoldenBugBitWithDiagnosticsAsync(GoldenBugBitViewModel bit)
    {
        var memory = _memory;
        if (memory is null || !_playerBaseAddress.HasValue)
        {
            GoldenBugsBitfieldStatusText.Text = "Not attached.";
            SetStatus("Not attached. Attach to Cemu and rescan before testing Golden Bugs bits.", StatusKind.Neutral);
            return false;
        }

        var absoluteAddress = _playerBaseAddress.Value + bit.OffsetValue;
        var mask = (byte)(1 << bit.Bit);
        byte? beforeByte = null;
        byte? afterByte = null;
        byte? immediateReadback = null;
        byte? delayed250Readback = null;
        byte? delayed1000Readback = null;
        var status = "started";

        try
        {
            if (!memory.TryReadBytes(absoluteAddress, 1, out var beforeBytes, out var beforeBytesRead) ||
                beforeBytesRead != 1)
            {
                status = "before-read-failed";
                bit.LastWriteStatus = "Before read failed.";
                GoldenBugsBitfieldStatusText.Text = $"Could not read {bit.Offset} bit {bit.Bit}.";
                SetStatus("Could not read Golden Bugs bit before write.", StatusKind.Warning);
                return false;
            }

            beforeByte = beforeBytes[0];
            afterByte = bit.IsSetDesired
                ? (byte)(beforeByte.Value | mask)
                : (byte)(beforeByte.Value & ~mask);

            if (!memory.TryWriteBytes(absoluteAddress, [afterByte.Value], out var writeError))
            {
                status = $"write-failed: {writeError}";
                bit.LastWriteStatus = "Write failed.";
                GoldenBugsBitfieldStatusText.Text = $"Golden Bugs bit write failed: {writeError}";
                SetStatus("Golden Bugs bit write failed.", StatusKind.Warning);
                return false;
            }

            if (!memory.TryReadBytes(absoluteAddress, 1, out var immediateBytes, out var immediateBytesRead) ||
                immediateBytesRead != 1)
            {
                status = "immediate-read-failed";
                bit.LastWriteStatus = "Immediate read failed.";
                SetStatus("Golden Bugs bit immediate verification failed.", StatusKind.Warning);
                return false;
            }

            immediateReadback = immediateBytes[0];
            if (immediateReadback.Value != afterByte.Value)
            {
                status = "immediate-mismatch";
                bit.LastWriteStatus = $"Immediate mismatch: expected 0x{afterByte.Value:X2}, read 0x{immediateReadback.Value:X2}.";
                SetStatus("Golden Bugs bit immediate readback mismatch.", StatusKind.Warning);
                return false;
            }

            await Task.Delay(250);
            if (memory.TryReadBytes(absoluteAddress, 1, out var delayed250Bytes, out var delayed250BytesRead) &&
                delayed250BytesRead == 1)
            {
                delayed250Readback = delayed250Bytes[0];
            }

            await Task.Delay(750);
            if (memory.TryReadBytes(absoluteAddress, 1, out var delayed1000Bytes, out var delayed1000BytesRead) &&
                delayed1000BytesRead == 1)
            {
                delayed1000Readback = delayed1000Bytes[0];
            }

            var reverted =
                delayed250Readback.HasValue && delayed250Readback.Value != afterByte.Value ||
                delayed1000Readback.HasValue && delayed1000Readback.Value != afterByte.Value;
            if (reverted)
            {
                status = "reverted-or-changed";
                bit.LastWriteStatus = "Delayed readback changed.";
                SetStatus("Golden Bugs bit write changed after delayed verification.", StatusKind.Warning);
            }
            else
            {
                status = "verified";
                bit.LastWriteStatus = "Verified.";
            }

            return !reverted;
        }
        finally
        {
            AppendGoldenBugsBitfieldTestingDiagnostic(
                "write-bit",
                bit,
                absoluteAddress,
                beforeByte,
                afterByte,
                mask,
                immediateReadback,
                delayed250Readback,
                delayed1000Readback,
                status);
        }
    }

    private async Task<bool> RestoreGoldenBugsBitfieldSnapshotAsync(byte[] snapshot)
    {
        var memory = _memory;
        if (memory is null || !_playerBaseAddress.HasValue)
        {
            GoldenBugsBitfieldStatusText.Text = "Not attached.";
            SetStatus("Not attached. Attach to Cemu and rescan before restoring Golden Bugs bitfield.", StatusKind.Neutral);
            return false;
        }

        var absoluteAddress = _playerBaseAddress.Value + GoldenBugsDefinitions.FirstOffset;
        byte[]? beforeBytes = null;
        byte[]? immediateBytes = null;
        byte[]? delayed250Bytes = null;
        byte[]? delayed1000Bytes = null;
        var status = "started";

        try
        {
            if (!memory.TryReadBytes(absoluteAddress, GoldenBugsDefinitions.ByteCount, out var currentBytes, out var currentBytesRead) ||
                currentBytesRead != GoldenBugsDefinitions.ByteCount)
            {
                status = "before-read-failed";
                GoldenBugsBitfieldStatusText.Text = "Could not read Golden Bugs bitfield before restore.";
                SetStatus("Could not read Golden Bugs bitfield before restore.", StatusKind.Warning);
                return false;
            }

            beforeBytes = currentBytes;
            if (!memory.TryWriteBytes(absoluteAddress, snapshot, out var writeError))
            {
                status = $"restore-write-failed: {writeError}";
                GoldenBugsBitfieldStatusText.Text = $"Golden Bugs bitfield restore failed: {writeError}";
                SetStatus("Golden Bugs bitfield restore failed.", StatusKind.Warning);
                return false;
            }

            if (!memory.TryReadBytes(absoluteAddress, GoldenBugsDefinitions.ByteCount, out immediateBytes, out var immediateRead) ||
                immediateRead != GoldenBugsDefinitions.ByteCount)
            {
                status = "restore-immediate-read-failed";
                SetStatus("Golden Bugs bitfield restore immediate verification failed.", StatusKind.Warning);
                return false;
            }

            if (!snapshot.SequenceEqual(immediateBytes))
            {
                status = "restore-immediate-mismatch";
                SetStatus("Golden Bugs bitfield restore immediate mismatch.", StatusKind.Warning);
                return false;
            }

            await Task.Delay(250);
            if (memory.TryReadBytes(absoluteAddress, GoldenBugsDefinitions.ByteCount, out var read250, out var read250Count) &&
                read250Count == GoldenBugsDefinitions.ByteCount)
            {
                delayed250Bytes = read250;
            }

            await Task.Delay(750);
            if (memory.TryReadBytes(absoluteAddress, GoldenBugsDefinitions.ByteCount, out var read1000, out var read1000Count) &&
                read1000Count == GoldenBugsDefinitions.ByteCount)
            {
                delayed1000Bytes = read1000;
            }

            var reverted =
                delayed250Bytes is not null && !snapshot.SequenceEqual(delayed250Bytes) ||
                delayed1000Bytes is not null && !snapshot.SequenceEqual(delayed1000Bytes);
            status = reverted ? "restore-delayed-mismatch" : "restore-verified";
            if (reverted)
            {
                SetStatus("Golden Bugs bitfield restore changed after delayed verification.", StatusKind.Warning);
            }

            return !reverted;
        }
        finally
        {
            AppendGoldenBugsBitfieldRestoreDiagnostic(
                absoluteAddress,
                beforeBytes,
                snapshot,
                immediateBytes,
                delayed250Bytes,
                delayed1000Bytes,
                status);
        }
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

    private GoldenBugsResearchExport CreateGoldenBugsResearchExport(int changedByteCount, int changedBitCount)
    {
        return new GoldenBugsResearchExport(
            DateTimeOffset.Now,
            _goldenBugsBeforeSnapshotStart,
            _goldenBugsBeforeSnapshotBytes?.Length ?? 0,
            _goldenBugsBeforeCapturedAt,
            _goldenBugsAfterCapturedAt,
            changedByteCount,
            changedBitCount,
            GoldenBugCandidateNames.Select((name, index) => $"{index + 1}. {name}").ToList(),
            GoldenBugsResearchRows.Select(row => new GoldenBugsResearchExportRow(
                row.Offset,
                row.BeforeValue,
                row.AfterValue,
                row.BeforeBinary,
                row.AfterBinary,
                row.ChangedBits,
                row.ByteIndex,
                row.BitIndex,
                row.CandidateBugIndexValue,
                row.CandidateBugName)).ToList());
    }

    private static IEnumerable<string> CreateGoldenBugsResearchCsvLines(IEnumerable<GoldenBugsResearchExportRow> rows)
    {
        yield return "Offset,Before Byte,After Byte,Before Binary,After Binary,Changed Bits,Byte Index,Bit Index,Candidate Bug Index,Candidate Bug Name";
        foreach (var row in rows)
        {
            yield return string.Join(
                ",",
                Csv(row.Offset),
                Csv(row.BeforeValue.ToString(CultureInfo.InvariantCulture)),
                Csv(row.AfterValue.ToString(CultureInfo.InvariantCulture)),
                Csv(row.BeforeBinary),
                Csv(row.AfterBinary),
                Csv(row.ChangedBits),
                Csv(row.ByteIndex.ToString(CultureInfo.InvariantCulture)),
                Csv(row.BitIndex.ToString(CultureInfo.InvariantCulture)),
                Csv(row.CandidateBugIndex.ToString(CultureInfo.InvariantCulture)),
                Csv(row.CandidateBugName));
        }
    }

    private QuestItemsResearchExport CreateQuestItemsResearchExport(int changedByteCount, int changedBitCount)
    {
        return new QuestItemsResearchExport(
            DateTimeOffset.Now,
            GetQuestItemsCaptureALabel(),
            GetQuestItemsCaptureBLabel(),
            _questItemsBeforeSnapshotStart,
            _questItemsBeforeSnapshotBytes?.Length ?? 0,
            _questItemsBeforeCapturedAt,
            _questItemsAfterSnapshotStart,
            _questItemsAfterSnapshotBytes?.Length ?? 0,
            _questItemsAfterCapturedAt,
            string.Empty,
            changedByteCount,
            changedBitCount,
            QuestItemsResearchRows.Select(row => new QuestItemsResearchExportRow(
                row.Offset,
                row.BeforeValue,
                row.AfterValue,
                row.BeforeBinary,
                row.AfterBinary,
                row.ChangedBits,
                row.ChangedBitCount,
                row.IsChanged,
                row.CandidateScore,
                row.CandidateGroup)).ToList());
    }

    private static IEnumerable<string> CreateQuestItemsResearchCsvLines(QuestItemsResearchExport export)
    {
        yield return "Field,Value";
        yield return string.Join(",", Csv("Capture A"), Csv(export.CaptureALabel));
        yield return string.Join(",", Csv("Capture B"), Csv(export.CaptureBLabel));
        yield return string.Join(",", Csv("Capture A Start Offset"), Csv($"0x{export.CaptureAStartOffset:X}"));
        yield return string.Join(",", Csv("Capture A Length"), Csv($"0x{export.CaptureALength:X}"));
        yield return string.Join(",", Csv("Capture B Start Offset"), Csv($"0x{export.CaptureBStartOffset:X}"));
        yield return string.Join(",", Csv("Capture B Length"), Csv($"0x{export.CaptureBLength:X}"));
        yield return string.Join(",", Csv("Changed Byte Count"), Csv(export.ChangedByteCount.ToString(CultureInfo.InvariantCulture)));
        yield return string.Join(",", Csv("Changed Bit Count"), Csv(export.ChangedBitCount.ToString(CultureInfo.InvariantCulture)));
        yield return string.Join(",", Csv("Range Warning"), Csv(export.RangeWarning));
        yield return string.Empty;
        yield return "Offset,Capture A Value,Capture B Value,Capture A Binary,Capture B Binary,Changed Bits,Changed Bit Count,Changed,Candidate Score,Candidate Group";
        foreach (var row in export.Rows)
        {
            yield return string.Join(
                ",",
                Csv(row.Offset),
                Csv(row.BeforeValue.ToString(CultureInfo.InvariantCulture)),
                Csv(row.AfterValue.ToString(CultureInfo.InvariantCulture)),
                Csv(row.BeforeBinary),
                Csv(row.AfterBinary),
                Csv(row.ChangedBits),
                Csv(row.ChangedBitCount.ToString(CultureInfo.InvariantCulture)),
                Csv(row.Changed.ToString(CultureInfo.InvariantCulture)),
                Csv(row.CandidateScore.ToString(CultureInfo.InvariantCulture)),
                Csv(row.CandidateGroup));
        }
    }

    private static void AssignQuestItemsCandidateGroups(IReadOnlyList<QuestItemsResearchRowViewModel> rows)
    {
        var changedRows = rows
            .Where(row => row.IsChanged)
            .OrderBy(row => row.OffsetValue)
            .ToList();
        if (changedRows.Count == 0)
        {
            return;
        }

        var groups = new List<List<QuestItemsResearchRowViewModel>>();
        List<QuestItemsResearchRowViewModel>? currentGroup = null;
        uint? previousOffset = null;
        foreach (var row in changedRows)
        {
            if (currentGroup is null || !previousOffset.HasValue || row.OffsetValue - previousOffset.Value > 4)
            {
                currentGroup = [];
                groups.Add(currentGroup);
            }

            currentGroup.Add(row);
            previousOffset = row.OffsetValue;
        }

        for (var index = 0; index < groups.Count; index++)
        {
            var groupName = GetHiddenSkillsCandidateGroupName(index);
            foreach (var row in groups[index])
            {
                row.SetCandidateGroup(groupName);
            }
        }
    }

    private void BuildQuestItemsCandidateAnalysis(string action)
    {
        _questItemsAllCandidateRows = [];
        QuestItemsCandidateGroups.Clear();
        _lastQuestItemsCandidateRankingExport = null;
        _lastQuestItemsCandidateGroupsExport = null;

        var persisted = !string.IsNullOrWhiteSpace(_questItemsCaptureAPath) &&
                        !string.IsNullOrWhiteSpace(_questItemsCaptureBPath);
        foreach (var row in QuestItemsResearchRows)
        {
            var analysis = ScoreQuestItemsCandidate(row, persisted);
            _questItemsAllCandidateRows.Add(new QuestItemsCandidateRowViewModel(
                row.OffsetValue,
                row.BeforeValue,
                row.AfterValue,
                row.ChangedBits,
                row.ChangedBitCount,
                row.IsChanged,
                row.IsSingleBitChange,
                persisted,
                row.CandidateGroup != "-",
                analysis.Score,
                GetQuestItemsConfidence(analysis.Score),
                row.CandidateGroup,
                string.Join("; ", analysis.Reasons)));
        }

        ApplyQuestItemsCandidateFilters();
        BuildQuestItemsCandidateGroups();
        _lastQuestItemsCandidateRankingExport = CreateQuestItemsCandidateRankingExport();
        _lastQuestItemsCandidateGroupsExport = CreateQuestItemsCandidateGroupsExport();
        AppendQuestItemsResearchLog(
            $"action={action}-candidate-analysis rows={QuestItemsCandidateRows.Count} groups={QuestItemsCandidateGroups.Count}");
    }

    private (int Score, List<string> Reasons) ScoreQuestItemsCandidate(
        QuestItemsResearchRowViewModel row,
        bool persisted)
    {
        var score = 0;
        var reasons = new List<string>();
        var delta = row.AfterValue - row.BeforeValue;
        var absoluteDelta = Math.Abs(delta);

        if (row.IsChanged)
        {
            score += 2;
            reasons.Add("Changed");
        }
        else
        {
            reasons.Add("Unchanged");
        }

        if (row.IsSingleBitChange)
        {
            score += 5;
            reasons.Add("Single-bit change");
        }
        else if (row.ChangedBitCount is > 1 and <= 3)
        {
            score += 2;
            reasons.Add("Small bit change");
        }
        else if (row.ChangedBitCount >= 5)
        {
            score -= 4;
            reasons.Add("Large noisy bit change");
        }

        if (row.IsChanged && absoluteDelta <= 4)
        {
            score += 2;
            reasons.Add("Small value transition");
        }
        else if (row.IsChanged && absoluteDelta > 64)
        {
            score -= 3;
            reasons.Add("Large value jump");
        }

        if (row.IsChanged && delta > 0)
        {
            score += 2;
            reasons.Add("Monotonic increase");
        }

        if (row.IsChanged && IsFlagLikeTransition(row.BeforeValue, row.AfterValue))
        {
            score += 3;
            reasons.Add("Flag-like transition");
        }

        if (row.CandidateGroup != "-")
        {
            score += 2;
            reasons.Add($"Nearby grouped candidate ({row.CandidateGroup})");
        }

        if (persisted && row.IsChanged)
        {
            score += 2;
            reasons.Add("Persisted saved-capture change");
        }

        if (_questItemsMultiAppearanceCounts.TryGetValue(row.OffsetValue, out var appearances) && appearances > 1)
        {
            score += Math.Min(4, appearances);
            reasons.Add($"Appears in {appearances} progression comparisons");
        }

        if (LooksTimerLike(row.BeforeValue, row.AfterValue, row.ChangedBitCount))
        {
            score -= 3;
            reasons.Add("Possible counter/timer noise");
        }

        return (Math.Max(0, score), reasons);
    }

    private static bool IsFlagLikeTransition(byte beforeValue, byte afterValue)
    {
        if (beforeValue == afterValue)
        {
            return false;
        }

        var newlySet = (byte)(afterValue & ~beforeValue);
        var cleared = (byte)(beforeValue & ~afterValue);
        return cleared == 0 && newlySet != 0 && IsSingleBitMask(newlySet);
    }

    private static bool IsSingleBitMask(byte value)
    {
        return value != 0 && (value & (value - 1)) == 0;
    }

    private static bool LooksTimerLike(byte beforeValue, byte afterValue, int changedBitCount)
    {
        return Math.Abs(afterValue - beforeValue) > 32 && changedBitCount >= 4;
    }

    private static string GetQuestItemsConfidence(int score)
    {
        return score >= 10 ? "High" : score >= 6 ? "Medium" : "Low";
    }

    private void ApplyQuestItemsCandidateFilters()
    {
        if (!IsInitialized)
        {
            return;
        }

        var changedOnly = QuestItemsFilterChangedOnlyCheckBox.IsChecked != false;
        var singleBitOnly = QuestItemsFilterSingleBitOnlyCheckBox.IsChecked == true;
        var groupedOnly = QuestItemsFilterGroupedOnlyCheckBox.IsChecked == true;
        var persistedOnly = QuestItemsFilterPersistedOnlyCheckBox.IsChecked == true;
        var minimumScore = 0;
        if (!int.TryParse(QuestItemsFilterMinimumScoreText.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out minimumScore))
        {
            minimumScore = 0;
        }

        QuestItemsCandidateRows.Clear();
        foreach (var row in _questItemsAllCandidateRows
                     .Where(row => !changedOnly || row.IsChanged)
                     .Where(row => !singleBitOnly || row.IsSingleBitChange)
                     .Where(row => !groupedOnly || row.IsGrouped)
                     .Where(row => !persistedOnly || row.IsPersisted)
                     .Where(row => row.CandidateScore >= minimumScore)
                     .OrderByDescending(row => row.CandidateScore)
                     .ThenBy(row => row.OffsetValue))
        {
            QuestItemsCandidateRows.Add(row);
        }
    }

    private void BuildQuestItemsCandidateGroups()
    {
        QuestItemsCandidateGroups.Clear();
        var groupedRows = _questItemsAllCandidateRows
            .Where(row => row.IsChanged && row.GroupName != "-")
            .GroupBy(row => row.GroupName)
            .OrderBy(group => group.Min(row => row.OffsetValue));

        foreach (var group in groupedRows)
        {
            var rows = group.OrderBy(row => row.OffsetValue).ToList();
            var reasons = rows
                .SelectMany(row => row.Reasons.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(5);
            QuestItemsCandidateGroups.Add(new QuestItemsCandidateGroupViewModel(
                group.Key,
                rows.First().OffsetValue,
                rows.Last().OffsetValue,
                rows.Count,
                rows.Max(row => row.CandidateScore),
                string.Join("; ", reasons)));
        }
    }

    private QuestItemsCandidateRankingExport CreateQuestItemsCandidateRankingExport()
    {
        return new QuestItemsCandidateRankingExport(
            DateTimeOffset.Now,
            GetQuestItemsCaptureALabel(),
            GetQuestItemsCaptureBLabel(),
            QuestItemsCandidateRows.Select(row => new QuestItemsCandidateRankingExportRow(
                row.Offset,
                row.BeforeHex,
                row.AfterHex,
                row.ChangedBits,
                row.ChangedBitCount,
                row.CandidateScore,
                row.Confidence,
                row.GroupName,
                row.Reasons)).ToList());
    }

    private QuestItemsCandidateGroupsExport CreateQuestItemsCandidateGroupsExport()
    {
        return new QuestItemsCandidateGroupsExport(
            DateTimeOffset.Now,
            GetQuestItemsCaptureALabel(),
            GetQuestItemsCaptureBLabel(),
            QuestItemsCandidateGroups.Select(group => new QuestItemsCandidateGroupsExportRow(
                group.GroupName,
                group.OffsetRange,
                group.Count,
                group.HighestCandidateScore,
                group.Reasons)).ToList());
    }

    private void ExportQuestItemsCandidateRanking()
    {
        if (QuestItemsCandidateRows.Count == 0 && _questItemsAllCandidateRows.Count == 0)
        {
            QuestItemsResearchStatusText.Text = "Analyze Quest Items captures before exporting candidate ranking.";
            SetStatus("Analyze Quest Items captures before exporting.", StatusKind.Neutral);
            return;
        }

        try
        {
            Directory.CreateDirectory(QuestSearchDirectory);
            _lastQuestItemsCandidateRankingExport = CreateQuestItemsCandidateRankingExport();
            var jsonPath = Path.Combine(QuestSearchDirectory, "quest-candidate-ranking.json");
            var csvPath = Path.Combine(QuestSearchDirectory, "quest-candidate-ranking.csv");
            File.WriteAllText(jsonPath, JsonSerializer.Serialize(_lastQuestItemsCandidateRankingExport, ExportJsonOptions));
            File.WriteAllLines(csvPath, CreateQuestItemsCandidateRankingCsvLines(_lastQuestItemsCandidateRankingExport));
            QuestItemsResearchStatusText.Text =
                $"Exported Quest Items candidate ranking: {Path.GetFileName(jsonPath)} and {Path.GetFileName(csvPath)}.";
            AppendQuestItemsResearchLog(
                $"action=export-candidate-ranking json=\"{jsonPath}\" csv=\"{csvPath}\" rows={_lastQuestItemsCandidateRankingExport.Rows.Count}");
            SetStatus("Quest Items candidate ranking exported.", StatusKind.Connected);
        }
        catch (Exception ex)
        {
            QuestItemsResearchStatusText.Text = $"Quest Items candidate ranking export failed: {ex.Message}";
            SetStatus("Quest Items candidate ranking export failed.", StatusKind.Warning);
        }
    }

    private void ExportQuestItemsCandidateGroups()
    {
        if (QuestItemsCandidateGroups.Count == 0)
        {
            QuestItemsResearchStatusText.Text = "Analyze Quest Items captures before exporting candidate groups.";
            SetStatus("Analyze Quest Items captures before exporting groups.", StatusKind.Neutral);
            return;
        }

        try
        {
            Directory.CreateDirectory(QuestSearchDirectory);
            _lastQuestItemsCandidateGroupsExport = CreateQuestItemsCandidateGroupsExport();
            var jsonPath = Path.Combine(QuestSearchDirectory, "quest-candidate-groups.json");
            var csvPath = Path.Combine(QuestSearchDirectory, "quest-candidate-groups.csv");
            File.WriteAllText(jsonPath, JsonSerializer.Serialize(_lastQuestItemsCandidateGroupsExport, ExportJsonOptions));
            File.WriteAllLines(csvPath, CreateQuestItemsCandidateGroupsCsvLines(_lastQuestItemsCandidateGroupsExport));
            QuestItemsResearchStatusText.Text =
                $"Exported Quest Items candidate groups: {Path.GetFileName(jsonPath)} and {Path.GetFileName(csvPath)}.";
            AppendQuestItemsResearchLog(
                $"action=export-candidate-groups json=\"{jsonPath}\" csv=\"{csvPath}\" groups={_lastQuestItemsCandidateGroupsExport.Groups.Count}");
            SetStatus("Quest Items candidate groups exported.", StatusKind.Connected);
        }
        catch (Exception ex)
        {
            QuestItemsResearchStatusText.Text = $"Quest Items candidate groups export failed: {ex.Message}";
            SetStatus("Quest Items candidate groups export failed.", StatusKind.Warning);
        }
    }

    private static IEnumerable<string> CreateQuestItemsCandidateRankingCsvLines(QuestItemsCandidateRankingExport export)
    {
        yield return "Offset,Capture A Value,Capture B Value,Changed Bits,Changed Bit Count,Score,Confidence,Group,Reasons";
        foreach (var row in export.Rows)
        {
            yield return string.Join(
                ",",
                Csv(row.Offset),
                Csv(row.CaptureAValue),
                Csv(row.CaptureBValue),
                Csv(row.ChangedBits),
                Csv(row.ChangedBitCount.ToString(CultureInfo.InvariantCulture)),
                Csv(row.CandidateScore.ToString(CultureInfo.InvariantCulture)),
                Csv(row.Confidence),
                Csv(row.GroupName),
                Csv(row.Reasons));
        }
    }

    private static IEnumerable<string> CreateQuestItemsCandidateGroupsCsvLines(QuestItemsCandidateGroupsExport export)
    {
        yield return "Group,Offset Range,Count,Highest Candidate Score,Reasons";
        foreach (var group in export.Groups)
        {
            yield return string.Join(
                ",",
                Csv(group.GroupName),
                Csv(group.OffsetRange),
                Csv(group.Count.ToString(CultureInfo.InvariantCulture)),
                Csv(group.HighestCandidateScore.ToString(CultureInfo.InvariantCulture)),
                Csv(group.Reasons));
        }
    }

    private void LoadQuestItemsMultiCaptures()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Load Quest Items progression captures",
            Filter = "Quest Items captures (*.json)|*.json|All files (*.*)|*.*",
            FileName = "quest-capture_*.json",
            Multiselect = true
        };

        if (Directory.Exists(QuestSearchDirectory))
        {
            dialog.InitialDirectory = QuestSearchDirectory;
        }

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        LoadQuestItemsMultiCapturesFromPaths(dialog.FileNames, "load-multi-captures");
    }

    private void LoadAllQuestItemsCapturesFromSearchFolder()
    {
        Directory.CreateDirectory(QuestSearchDirectory);
        var paths = Directory
            .EnumerateFiles(QuestSearchDirectory, "quest-capture_*.json")
            .OrderBy(path => path)
            .ToList();
        if (paths.Count == 0)
        {
            QuestItemsResearchStatusText.Text = "No Quest Items captures were found in logs/research/quest-search.";
            SetStatus("No Quest Items captures found.", StatusKind.Neutral);
            return;
        }

        LoadQuestItemsMultiCapturesFromPaths(paths, "load-all-multi-captures");
    }

    private void LoadQuestItemsMultiCapturesFromPaths(IEnumerable<string> paths, string action)
    {
        var loadedCaptures = new List<QuestItemsLoadedCapture>();
        foreach (var path in paths)
        {
            if (TryLoadQuestItemsCaptureFromPath(path, out var capture, out var bytes, out var loadedPath))
            {
                loadedCaptures.Add(new QuestItemsLoadedCapture(loadedPath, capture, bytes));
            }
        }

        _questItemsMultiCaptures.Clear();
        _questItemsMultiCaptures.AddRange(loadedCaptures
            .OrderBy(capture => GetQuestItemsCaptureTypeOrder(capture.Document.CaptureType))
            .ThenBy(capture => capture.Document.Timestamp));
        QuestItemsMultiCaptureRows.Clear();
        _lastQuestItemsMultiCaptureAnalysisExport = null;

        QuestItemsResearchStatusText.Text =
            $"Loaded {_questItemsMultiCaptures.Count} Quest Items progression capture(s) for multi-capture analysis.";
        AppendQuestItemsResearchLog(
            $"action={action} count={_questItemsMultiCaptures.Count} files=\"{string.Join("; ", _questItemsMultiCaptures.Select(capture => capture.Path))}\"");
        SetStatus("Quest Items multi-captures loaded.", StatusKind.Connected);
    }

    private void AnalyzeQuestItemsMultiCaptures()
    {
        if (_questItemsMultiCaptures.Count < 2)
        {
            QuestItemsResearchStatusText.Text = "Load at least two Quest Items captures before multi-capture analysis.";
            SetStatus("Load Quest Items multi-captures first.", StatusKind.Neutral);
            return;
        }

        var first = _questItemsMultiCaptures[0];
        if (_questItemsMultiCaptures.Any(capture =>
                capture.Document.StartOffset != first.Document.StartOffset ||
                capture.Bytes.Length != first.Bytes.Length))
        {
            QuestItemsResearchStatusText.Text = "All Quest Items multi-captures must use the same start offset and length.";
            SetStatus("Quest Items multi-capture ranges do not match.", StatusKind.Warning);
            return;
        }

        QuestItemsMultiCaptureRows.Clear();
        _questItemsMultiAppearanceCounts.Clear();
        for (var index = 0; index < first.Bytes.Length; index++)
        {
            var offset = first.Document.StartOffset + (uint)index;
            var values = _questItemsMultiCaptures.Select(capture => capture.Bytes[index]).ToList();
            var appearances = CountQuestItemsValueChanges(values);
            if (appearances == 0)
            {
                continue;
            }

            var analysis = ScoreQuestItemsMultiCaptureCandidate(values, appearances);
            _questItemsMultiAppearanceCounts[offset] = appearances;
            QuestItemsMultiCaptureRows.Add(new QuestItemsMultiCaptureRowViewModel(
                offset,
                string.Join(" -> ", values.Select(value => $"0x{value:X2}")),
                appearances,
                analysis.Score,
                GetQuestItemsConfidence(analysis.Score),
                string.Join("; ", analysis.Reasons)));
        }

        var sortedRows = QuestItemsMultiCaptureRows
            .OrderByDescending(row => row.CandidateScore)
            .ThenBy(row => row.OffsetValue)
            .ToList();
        QuestItemsMultiCaptureRows.Clear();
        foreach (var row in sortedRows)
        {
            QuestItemsMultiCaptureRows.Add(row);
        }

        _lastQuestItemsMultiCaptureAnalysisExport = CreateQuestItemsMultiCaptureAnalysisExport();
        if (QuestItemsResearchRows.Count > 0)
        {
            BuildQuestItemsCandidateAnalysis("multi-capture-correlation");
        }

        QuestItemsResearchStatusText.Text =
            $"Analyzed {_questItemsMultiCaptures.Count} Quest Items captures. Ranked candidates: {QuestItemsMultiCaptureRows.Count}.";
        AppendQuestItemsResearchLog(
            $"action=analyze-multi-captures captures={_questItemsMultiCaptures.Count} rows={QuestItemsMultiCaptureRows.Count}");
        SetStatus("Quest Items multi-capture analysis complete.", StatusKind.Connected);
    }

    private static int CountQuestItemsValueChanges(IReadOnlyList<byte> values)
    {
        var changes = 0;
        for (var index = 1; index < values.Count; index++)
        {
            if (values[index] != values[index - 1])
            {
                changes++;
            }
        }

        return changes;
    }

    private static (int Score, List<string> Reasons) ScoreQuestItemsMultiCaptureCandidate(
        IReadOnlyList<byte> values,
        int appearances)
    {
        var score = appearances * 2;
        var reasons = new List<string> { $"Changed in {appearances} transition(s)" };
        var onlyIncreases = true;
        var bitCountsOnlyIncrease = true;
        var largestDelta = 0;
        for (var index = 1; index < values.Count; index++)
        {
            if (values[index] < values[index - 1])
            {
                onlyIncreases = false;
            }

            if (CountSetBits(values[index]) < CountSetBits(values[index - 1]))
            {
                bitCountsOnlyIncrease = false;
            }

            largestDelta = Math.Max(largestDelta, Math.Abs(values[index] - values[index - 1]));
        }

        if (onlyIncreases)
        {
            score += 4;
            reasons.Add("Only ever increases");
        }

        if (bitCountsOnlyIncrease)
        {
            score += 3;
            reasons.Add("Set-bit count only increases");
        }

        if (values.Distinct().Count() > 1 && onlyIncreases && largestDelta <= 8)
        {
            score += 3;
            reasons.Add("Value increases in small steps");
        }

        if (HasFlagAppearsOnceAndRemainsSet(values))
        {
            score += 5;
            reasons.Add("Flag appears once and remains set");
        }

        if (largestDelta > 64)
        {
            score -= 3;
            reasons.Add("Large noisy value change");
        }

        if (appearances >= Math.Max(3, values.Count - 1) && largestDelta > 8)
        {
            score -= 3;
            reasons.Add("Possible frequently changing counter");
        }

        return (Math.Max(0, score), reasons);
    }

    private static bool HasFlagAppearsOnceAndRemainsSet(IReadOnlyList<byte> values)
    {
        for (var bit = 0; bit < 8; bit++)
        {
            var mask = (byte)(1 << bit);
            var firstSetIndex = -1;
            for (var index = 0; index < values.Count; index++)
            {
                if ((values[index] & mask) != 0)
                {
                    firstSetIndex = index;
                    break;
                }
            }

            if (firstSetIndex <= 0)
            {
                continue;
            }

            var wasUnsetBefore = values.Take(firstSetIndex).All(value => (value & mask) == 0);
            var remainsSet = values.Skip(firstSetIndex).All(value => (value & mask) != 0);
            if (wasUnsetBefore && remainsSet)
            {
                return true;
            }
        }

        return false;
    }

    private static int CountSetBits(byte value)
    {
        var count = 0;
        var remaining = value;
        while (remaining != 0)
        {
            count += remaining & 1;
            remaining >>= 1;
        }

        return count;
    }

    private static int GetQuestItemsCaptureTypeOrder(string captureType)
    {
        var index = QuestItemsCaptureTypes
            .Select((type, typeIndex) => new { type, typeIndex })
            .FirstOrDefault(item => string.Equals(item.type, captureType, StringComparison.OrdinalIgnoreCase))
            ?.typeIndex;
        return index ?? QuestItemsCaptureTypes.Count;
    }

    private QuestItemsMultiCaptureAnalysisExport CreateQuestItemsMultiCaptureAnalysisExport()
    {
        return new QuestItemsMultiCaptureAnalysisExport(
            DateTimeOffset.Now,
            _questItemsMultiCaptures.Select(capture => new QuestItemsMultiCaptureExportCapture(
                capture.Document.Label,
                capture.Document.CaptureType,
                capture.Document.Timestamp,
                capture.Document.StartOffset,
                capture.Document.Length,
                Path.GetFileName(capture.Path))).ToList(),
            QuestItemsMultiCaptureRows.Select(row => new QuestItemsMultiCaptureAnalysisExportRow(
                row.Offset,
                row.ValueProgression,
                row.AppearanceCount,
                row.CandidateScore,
                row.Confidence,
                row.Reasons)).ToList());
    }

    private void ExportQuestItemsMultiCaptureAnalysis()
    {
        if (QuestItemsMultiCaptureRows.Count == 0)
        {
            QuestItemsResearchStatusText.Text = "Analyze Quest Items multi-captures before exporting.";
            SetStatus("Analyze Quest Items multi-captures before exporting.", StatusKind.Neutral);
            return;
        }

        try
        {
            Directory.CreateDirectory(QuestSearchDirectory);
            _lastQuestItemsMultiCaptureAnalysisExport = CreateQuestItemsMultiCaptureAnalysisExport();
            var jsonPath = Path.Combine(QuestSearchDirectory, "quest-multi-capture-analysis.json");
            var csvPath = Path.Combine(QuestSearchDirectory, "quest-multi-capture-analysis.csv");
            File.WriteAllText(jsonPath, JsonSerializer.Serialize(_lastQuestItemsMultiCaptureAnalysisExport, ExportJsonOptions));
            File.WriteAllLines(csvPath, CreateQuestItemsMultiCaptureAnalysisCsvLines(_lastQuestItemsMultiCaptureAnalysisExport));
            QuestItemsResearchStatusText.Text =
                $"Exported Quest Items multi-capture analysis: {Path.GetFileName(jsonPath)} and {Path.GetFileName(csvPath)}.";
            AppendQuestItemsResearchLog(
                $"action=export-multi-capture-analysis json=\"{jsonPath}\" csv=\"{csvPath}\" rows={_lastQuestItemsMultiCaptureAnalysisExport.Rows.Count}");
            SetStatus("Quest Items multi-capture analysis exported.", StatusKind.Connected);
        }
        catch (Exception ex)
        {
            QuestItemsResearchStatusText.Text = $"Quest Items multi-capture export failed: {ex.Message}";
            SetStatus("Quest Items multi-capture export failed.", StatusKind.Warning);
        }
    }

    private static IEnumerable<string> CreateQuestItemsMultiCaptureAnalysisCsvLines(
        QuestItemsMultiCaptureAnalysisExport export)
    {
        yield return "Offset,Value Progression,Appearances,Score,Confidence,Reasons";
        foreach (var row in export.Rows)
        {
            yield return string.Join(
                ",",
                Csv(row.Offset),
                Csv(row.ValueProgression),
                Csv(row.AppearanceCount.ToString(CultureInfo.InvariantCulture)),
                Csv(row.CandidateScore.ToString(CultureInfo.InvariantCulture)),
                Csv(row.Confidence),
                Csv(row.Reasons));
        }
    }

    private void ClearHiddenSkillsResearchResults()
    {
        _hiddenSkillsAllResearchRows = [];
        HiddenSkillsResearchRows.Clear();
        HiddenSkillsCandidateGroups.Clear();
        _lastHiddenSkillsResearchExport = null;
    }

    private void ApplyHiddenSkillsPinnedOffsets()
    {
        foreach (var row in _hiddenSkillsAllResearchRows)
        {
            row.IsPinned = _hiddenSkillsPinnedOffsets.TryGetValue(row.OffsetValue, out var pinned) && pinned;
        }
    }

    private void ApplyHiddenSkillsResearchFilter()
    {
        HiddenSkillsResearchRows.Clear();

        var filterEnabled = HiddenSkillsFilterEnabledCheckBox.IsChecked == true;
        var showSingleBit = HiddenSkillsFilterSingleBitCheckBox.IsChecked == true;
        var showPersisted = HiddenSkillsFilterPersistedCheckBox.IsChecked == true;
        var showScore = HiddenSkillsFilterScoreCheckBox.IsChecked == true;

        foreach (var row in _hiddenSkillsAllResearchRows)
        {
            if (!filterEnabled ||
                row.IsPinned ||
                showSingleBit && row.IsSingleBitChange ||
                showPersisted && row.IsPersistedChange ||
                showScore && row.CandidateScore >= 6)
            {
                HiddenSkillsResearchRows.Add(row);
            }
        }
    }

    private void RebuildHiddenSkillsCandidateGroups()
    {
        foreach (var row in _hiddenSkillsAllResearchRows)
        {
            row.SetGroupName("-");
        }

        HiddenSkillsCandidateGroups.Clear();
        var candidateRows = _hiddenSkillsAllResearchRows
            .Where(row => row.IsCandidate)
            .OrderBy(row => row.OffsetValue)
            .ToList();
        if (candidateRows.Count == 0)
        {
            return;
        }

        var groups = new List<List<HiddenSkillsResearchRowViewModel>>();
        List<HiddenSkillsResearchRowViewModel>? currentGroup = null;
        uint? previousOffset = null;
        foreach (var row in candidateRows)
        {
            if (currentGroup is null || !previousOffset.HasValue || row.OffsetValue - previousOffset.Value > 4)
            {
                currentGroup = [];
                groups.Add(currentGroup);
            }

            currentGroup.Add(row);
            previousOffset = row.OffsetValue;
        }

        for (var index = 0; index < groups.Count; index++)
        {
            var groupName = GetHiddenSkillsCandidateGroupName(index);
            foreach (var row in groups[index])
            {
                row.SetGroupName(groupName);
            }

            HiddenSkillsCandidateGroups.Add(new HiddenSkillsCandidateGroupViewModel(groupName, groups[index]));
        }
    }

    private static string GetHiddenSkillsCandidateGroupName(int index)
    {
        return index < 26
            ? $"Group {(char)('A' + index)}"
            : $"Group {index + 1}";
    }

    private HiddenSkillsResearchExport CreateHiddenSkillsResearchExport()
    {
        return new HiddenSkillsResearchExport(
            DateTimeOffset.Now,
            _hiddenSkillsBeforeSnapshotStart,
            _hiddenSkillsBeforeSnapshotBytes?.Length ?? 0,
            _hiddenSkillsBeforeCapturedAt,
            _hiddenSkillsAfterCapturedAt,
            HiddenSkillsPersistedAfterReloadCheckBox.IsChecked == true,
            FutureFeatureCatalog.HiddenSkills.Select(skill => skill.Name).ToList(),
            _hiddenSkillsAllResearchRows.Select(row => new HiddenSkillsResearchExportRow(
                row.Offset,
                row.BeforeValue,
                row.AfterValue,
                row.BeforeBinary,
                row.AfterBinary,
                row.ChangedBits,
                row.ChangedBitCount,
                row.IsChanged,
                row.IsSingleBitChange,
                row.IsPersistedChange,
                row.IsClusteredChange,
                row.CandidateScore,
                row.HighlightLabel,
                row.IsPinned,
                row.GroupName)).ToList(),
            CreateHiddenSkillsCandidateGroupExports());
    }

    private static IEnumerable<string> CreateHiddenSkillsResearchCsvLines(IEnumerable<HiddenSkillsResearchExportRow> rows)
    {
        yield return "Offset,Before Byte,After Byte,Before Binary,After Binary,Changed Bits,Changed Bit Count,Changed,Single Bit,Persisted,Clustered,Candidate Score,Highlights,Pinned,Group";
        foreach (var row in rows)
        {
            yield return string.Join(
                ",",
                Csv(row.Offset),
                Csv(row.BeforeValue.ToString(CultureInfo.InvariantCulture)),
                Csv(row.AfterValue.ToString(CultureInfo.InvariantCulture)),
                Csv(row.BeforeBinary),
                Csv(row.AfterBinary),
                Csv(row.ChangedBits),
                Csv(row.ChangedBitCount.ToString(CultureInfo.InvariantCulture)),
                Csv(row.Changed.ToString()),
                Csv(row.SingleBitChange.ToString()),
                Csv(row.PersistedChange.ToString()),
                Csv(row.ClusteredChange.ToString()),
                Csv(row.CandidateScore.ToString(CultureInfo.InvariantCulture)),
                Csv(row.Highlights),
                Csv(row.Pinned.ToString()),
                Csv(row.GroupName));
        }
    }

    private HiddenSkillsCandidateGroupsExport CreateHiddenSkillsCandidateGroupsExport()
    {
        return new HiddenSkillsCandidateGroupsExport(
            DateTimeOffset.Now,
            _hiddenSkillsBeforeSnapshotStart,
            _hiddenSkillsBeforeSnapshotBytes?.Length ?? 0,
            CreateHiddenSkillsCandidateGroupExports());
    }

    private IReadOnlyList<HiddenSkillsCandidateGroupExport> CreateHiddenSkillsCandidateGroupExports()
    {
        return HiddenSkillsCandidateGroups.Select(group => new HiddenSkillsCandidateGroupExport(
            group.Name,
            group.Offsets,
            group.Count,
            group.HighestScore,
            group.Reasons,
            group.Rows.Select(row => new HiddenSkillsCandidateGroupRowExport(
                row.Offset,
                row.BeforeValue,
                row.AfterValue,
                row.ChangedBits,
                row.CandidateScore,
                row.HighlightLabel,
                row.IsPinned)).ToList())).ToList();
    }

    private static IEnumerable<string> CreateHiddenSkillsCandidateGroupsCsvLines(IEnumerable<HiddenSkillsCandidateGroupExport> groups)
    {
        yield return "Group,Offsets,Count,Highest Score,Reasons,Row Offset,Before Value,After Value,Changed Bits,Candidate Score,Highlights,Pinned";
        foreach (var group in groups)
        {
            foreach (var row in group.Rows)
            {
                yield return string.Join(
                    ",",
                    Csv(group.Name),
                    Csv(group.Offsets),
                    Csv(group.Count.ToString(CultureInfo.InvariantCulture)),
                    Csv(group.HighestScore.ToString(CultureInfo.InvariantCulture)),
                    Csv(group.Reasons),
                    Csv(row.Offset),
                    Csv(row.BeforeValue.ToString(CultureInfo.InvariantCulture)),
                    Csv(row.AfterValue.ToString(CultureInfo.InvariantCulture)),
                    Csv(row.ChangedBits),
                    Csv(row.CandidateScore.ToString(CultureInfo.InvariantCulture)),
                    Csv(row.Highlights),
                Csv(row.Pinned.ToString()));
            }
        }
    }

    private static IEnumerable<string> CreateHiddenSkillsMultiCaptureCsvLines(IEnumerable<HiddenSkillsMultiCaptureExportRow> rows)
    {
        yield return "Rank,Offset,Kind,Bit,Values A-F,Known Skill Counts,Score,Monotonic,Only Increases,Progression Match,Flags,Notes";
        foreach (var row in rows)
        {
            yield return string.Join(
                ",",
                Csv(row.Rank.ToString(CultureInfo.InvariantCulture)),
                Csv(row.Offset),
                Csv(row.Kind),
                Csv(row.Bit),
                Csv(row.Values),
                Csv(row.SkillCounts),
                Csv(row.Score.ToString(CultureInfo.InvariantCulture)),
                Csv(row.IsMonotonic.ToString(CultureInfo.InvariantCulture)),
                Csv(row.OnlyIncreases.ToString(CultureInfo.InvariantCulture)),
                Csv(row.ProgressionMatch.ToString(CultureInfo.InvariantCulture)),
                Csv(row.Flags),
                Csv(row.Notes));
        }
    }

    private static IEnumerable<string> CreateHiddenSkillsRegionAnalysisCsvLines(IEnumerable<HiddenSkillsRegionAnalysisExportRow> rows)
    {
        yield return "Rank,Region,Changed Bytes,Changed Bits,Density %,Largest Change,Candidate Score,Single Bit Region,Highlights";
        foreach (var row in rows)
        {
            yield return string.Join(
                ",",
                Csv(row.Rank.ToString(CultureInfo.InvariantCulture)),
                Csv(row.Region),
                Csv(row.ChangedBytes.ToString(CultureInfo.InvariantCulture)),
                Csv(row.ChangedBits.ToString(CultureInfo.InvariantCulture)),
                Csv(row.Density.ToString("0.0", CultureInfo.InvariantCulture)),
                Csv(row.LargestChange.ToString(CultureInfo.InvariantCulture)),
                Csv(row.CandidateScore.ToString(CultureInfo.InvariantCulture)),
                Csv(row.SingleBitRegion.ToString(CultureInfo.InvariantCulture)),
                Csv(row.Highlights));
        }
    }

    private static IEnumerable<string> CreateHiddenSkillsLiveWatchCsvLines(HiddenSkillsLiveWatchExport export)
    {
        yield return "Type,Timestamp,Label,Offset,Before,After,Binary Before,Binary After,Changed Bits,Changed Bit Count,Change Count,Single Bit,Repeated,Monotonic,Highlights";
        foreach (var marker in export.Events.OrderBy(marker => marker.Timestamp))
        {
            yield return string.Join(
                ",",
                Csv("Event"),
                Csv(marker.Timestamp.ToString("O", CultureInfo.InvariantCulture)),
                Csv(marker.Label),
                Csv(string.Empty),
                Csv(string.Empty),
                Csv(string.Empty),
                Csv(string.Empty),
                Csv(string.Empty),
                Csv(string.Empty),
                Csv(string.Empty),
                Csv(string.Empty),
                Csv(string.Empty),
                Csv(string.Empty),
                Csv(string.Empty),
                Csv(string.Empty));
        }

        foreach (var row in export.Rows.OrderBy(row => row.Timestamp))
        {
            yield return string.Join(
                ",",
                Csv("Change"),
                Csv(row.Timestamp.ToString("O", CultureInfo.InvariantCulture)),
                Csv(string.Empty),
                Csv(row.Offset),
                Csv(row.BeforeValue.ToString(CultureInfo.InvariantCulture)),
                Csv(row.AfterValue.ToString(CultureInfo.InvariantCulture)),
                Csv(row.BeforeBinary),
                Csv(row.AfterBinary),
                Csv(row.ChangedBits),
                Csv(row.ChangedBitCount.ToString(CultureInfo.InvariantCulture)),
                Csv(row.ChangeCount.ToString(CultureInfo.InvariantCulture)),
                Csv(row.SingleBitChange.ToString(CultureInfo.InvariantCulture)),
                Csv(row.RepeatedChange.ToString(CultureInfo.InvariantCulture)),
                Csv(row.MonotonicChange.ToString(CultureInfo.InvariantCulture)),
                Csv(row.Highlights));
        }
    }

    private GoldenBugsBitfieldReport CreateGoldenBugsBitfieldReport()
    {
        return new GoldenBugsBitfieldReport(
            DateTimeOffset.Now,
            _goldenBugsBitfieldRestoreSnapshotCapturedAt,
            _goldenBugsBitfieldRestoreSnapshotBytes is null
                ? []
                : _goldenBugsBitfieldRestoreSnapshotBytes.Select(value => $"0x{value:X2}").ToList(),
            GoldenBugsBitRows.Select(bit => new GoldenBugsBitfieldReportRow(
                bit.Offset,
                bit.Bit,
                bit.CurrentState,
                bit.IsSetDesired,
                bit.ConfirmedBugName,
                bit.MappingStatus,
                bit.AssignedBugName ?? string.Empty,
                bit.Notes,
                bit.LastWriteStatus)).ToList());
    }

    private static IEnumerable<string> CreateGoldenBugsBitfieldReportCsvLines(IEnumerable<GoldenBugsBitfieldReportRow> rows)
    {
        yield return "Offset,Bit,Current State,Desired,Confirmed Bug Name,Mapping Status,Assigned Bug Name,Notes,Last Write Status";
        foreach (var row in rows)
        {
            yield return string.Join(
                ",",
                Csv(row.Offset),
                Csv(row.Bit.ToString(CultureInfo.InvariantCulture)),
                Csv(row.CurrentState),
                Csv(row.Desired.ToString(CultureInfo.InvariantCulture)),
                Csv(row.ConfirmedBugName),
                Csv(row.MappingStatus),
                Csv(row.AssignedBugName),
                Csv(row.Notes),
                Csv(row.LastWriteStatus));
        }
    }

    private static string Csv(string value)
    {
        return "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }

    private static string SanitizeFileName(string value)
    {
        var fileName = string.IsNullOrWhiteSpace(value) ? "capture" : value.Trim();
        foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(invalidCharacter, '-');
        }

        return fileName.Replace(' ', '-');
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

    private string CreateSupportSnapshot()
    {
        var timestamp = DateTimeOffset.Now;
        var snapshotDirectory = Path.Combine(AppContext.BaseDirectory, "support-snapshots");
        Directory.CreateDirectory(snapshotDirectory);

        var fileName = $"TPHD-Trainer-SupportSnapshot_{timestamp:yyyyMMdd_HHmmss}.zip";
        var path = Path.Combine(snapshotDirectory, fileName);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        var includedLogs = new List<string>();
        var skippedLogs = new List<string>();
        using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
        {
            AddLogsToSupportSnapshot(archive, includedLogs, skippedLogs);
            AddTextEntry(archive, "state.txt", CreateSupportSnapshotStateText(timestamp, includedLogs, skippedLogs));
            AddJsonEntry(archive, "summary.json", CreateSupportSnapshotDocument(timestamp, includedLogs, skippedLogs));
        }

        return path;
    }

    private SupportSnapshotDocument CreateSupportSnapshotDocument(
        DateTimeOffset timestamp,
        IReadOnlyList<string> includedLogs,
        IReadOnlyList<string> skippedLogs)
    {
        return new SupportSnapshotDocument(
            ApplicationVersion: Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown",
            Timestamp: timestamp,
            ProcessName: "Cemu.exe",
            IsAttached: _memory is not null && !_memory.HasExited,
            Pid: PidText.Text,
            PlayerBaseAddress: PlayerBaseText.Text,
            ConnectionStatus: StatusText.Text,
            HasPlayerData: HasPlayerData,
            InventoryInitialized: InventoryInitialized,
            EquipmentInitialized: EquipmentInitialized,
            OwnershipEditsState: EffectiveOwnershipEditAcceptanceText,
            DarkModeEnabled: IsDarkMode,
            AobPattern: CheatCatalog.PlayerBaseAob,
            Capacities: _capacities.Values.Select(capacity => new SupportSnapshotCapacity(
                capacity.Name,
                capacity.CurrentStoredValue,
                capacity.SelectedOption?.Label ?? "-")).ToList(),
            Values: _values.Values.Select(value => new SupportSnapshotValue(
                value.Name,
                value.Offset,
                value.CurrentValue,
                value.TargetValue,
                value.IsLocked)).ToList(),
            IncludedLogFiles: includedLogs.ToList(),
            SkippedLogFiles: skippedLogs.ToList());
    }

    private string CreateSupportSnapshotStateText(
        DateTimeOffset timestamp,
        IReadOnlyList<string> includedLogs,
        IReadOnlyList<string> skippedLogs)
    {
        return string.Join(
            Environment.NewLine,
            "TPHD Cemu Trainer Support Snapshot",
            $"Timestamp: {timestamp:O}",
            $"Application version: {Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown"}",
            $"Attached: {_memory is not null && !_memory.HasExited}",
            $"PID: {PidText.Text}",
            $"Player base: {PlayerBaseText.Text}",
            $"Status: {StatusText.Text}",
            $"Player data: {HasPlayerData}",
            $"Inventory initialized: {InventoryInitialized}",
            $"Equipment initialized: {EquipmentInitialized}",
            $"Ownership edits: {EffectiveOwnershipEditAcceptanceText}",
            $"Dark mode: {IsDarkMode}",
            $"Included log files: {includedLogs.Count}",
            $"Skipped log files: {skippedLogs.Count}",
            string.Empty,
            "No save files or absolute user paths are included.");
    }

    private static void AddLogsToSupportSnapshot(
        ZipArchive archive,
        ICollection<string> includedLogs,
        ICollection<string> skippedLogs)
    {
        var logsDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
        if (!Directory.Exists(logsDirectory))
        {
            return;
        }

        foreach (var filePath in Directory.EnumerateFiles(logsDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(logsDirectory, filePath);
            if (relativePath.StartsWith("..", StringComparison.Ordinal))
            {
                skippedLogs.Add(relativePath);
                continue;
            }

            var entryName = "logs/" + relativePath.Replace('\\', '/');
            try
            {
                AddTextEntry(archive, entryName, File.ReadAllText(filePath));
                includedLogs.Add(entryName);
            }
            catch (Exception ex)
            {
                skippedLogs.Add($"{entryName}: {ex.Message}");
            }
        }
    }

    private static void AddJsonEntry<T>(ZipArchive archive, string entryName, T value)
    {
        AddTextEntry(archive, entryName, JsonSerializer.Serialize(value, ExportJsonOptions));
    }

    private static void AddTextEntry(ZipArchive archive, string entryName, string content)
    {
        var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream);
        writer.Write(SanitizeSupportSnapshotText(content));
    }

    private static string SanitizeSupportSnapshotText(string content)
    {
        var sanitized = content;
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(userProfile))
        {
            sanitized = sanitized.Replace(userProfile, "<user-profile>", StringComparison.OrdinalIgnoreCase);
            sanitized = sanitized.Replace(userProfile.Replace('\\', '/'), "<user-profile>", StringComparison.OrdinalIgnoreCase);
        }

        var userName = Environment.UserName;
        if (!string.IsNullOrWhiteSpace(userName) && userName.Length >= 3)
        {
            sanitized = sanitized.Replace(userName, "<user>", StringComparison.OrdinalIgnoreCase);
        }

        return sanitized;
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

    private void AppendGoldenBugsResearchLog(string details)
    {
        var entry = $"{DateTimeOffset.Now:O} {details}";

        try
        {
            var logDirectory = Path.GetDirectoryName(GoldenBugsResearchLogPath);
            if (!string.IsNullOrWhiteSpace(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            File.AppendAllText(GoldenBugsResearchLogPath, entry + Environment.NewLine);
        }
        catch (Exception ex)
        {
            GoldenBugsResearchStatusText.Text = $"Golden Bugs research log write failed: {ex.Message}";
        }
    }

    private void AppendQuestItemsResearchLog(string details)
    {
        var entry = $"{DateTimeOffset.Now:O} {details}";

        try
        {
            var logDirectory = Path.GetDirectoryName(QuestItemsResearchLogPath);
            if (!string.IsNullOrWhiteSpace(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            File.AppendAllText(QuestItemsResearchLogPath, entry + Environment.NewLine);
        }
        catch (Exception ex)
        {
            QuestItemsResearchStatusText.Text = $"Quest Items research log write failed: {ex.Message}";
        }
    }

    private void AppendHiddenSkillsResearchLog(string details)
    {
        var entry = $"{DateTimeOffset.Now:O} {details}";

        try
        {
            var logDirectory = Path.GetDirectoryName(HiddenSkillsResearchLogPath);
            if (!string.IsNullOrWhiteSpace(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            File.AppendAllText(HiddenSkillsResearchLogPath, entry + Environment.NewLine);
        }
        catch (Exception ex)
        {
            HiddenSkillsResearchStatusText.Text = $"Hidden Skills research log write failed: {ex.Message}";
        }
    }

    private void AppendHiddenSkillsBitTestingLog(string details)
    {
        var entry = $"{DateTimeOffset.Now:O} {details}";

        try
        {
            var logDirectory = Path.GetDirectoryName(HiddenSkillsBitTestingLogPath);
            if (!string.IsNullOrWhiteSpace(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            File.AppendAllText(HiddenSkillsBitTestingLogPath, entry + Environment.NewLine);
        }
        catch (Exception ex)
        {
            HiddenSkillsBitStatusText.Text = $"Hidden Skills bit testing log write failed: {ex.Message}";
        }
    }

    private void AppendHiddenSkillsLiveWatchLog(string details)
    {
        var entry = $"{DateTimeOffset.Now:O} {details}";

        try
        {
            var logDirectory = Path.GetDirectoryName(HiddenSkillsLiveWatchLogPath);
            if (!string.IsNullOrWhiteSpace(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            File.AppendAllText(HiddenSkillsLiveWatchLogPath, entry + Environment.NewLine);
        }
        catch (Exception ex)
        {
            HiddenSkillsLiveWatchStatusText.Text = $"Hidden Skills live watch log write failed: {ex.Message}";
        }
    }

    private string GetHiddenSkillsCaptureLabel()
    {
        return HiddenSkillsCaptureLabelText.Text.Trim();
    }

    private string GetQuestItemsCaptureLabel()
    {
        return QuestItemsCaptureLabelText.Text.Trim();
    }

    private string GetQuestItemsCaptureNotes()
    {
        return QuestItemsCaptureNotesText.Text.Trim();
    }

    private string GetQuestItemsCaptureType()
    {
        var selectedType = QuestItemsCaptureTypeComboBox.SelectedItem is ComboBoxItem item
            ? item.Content?.ToString() ?? string.Empty
            : string.Empty;
        if (string.Equals(selectedType, "Custom", StringComparison.OrdinalIgnoreCase))
        {
            var customType = QuestItemsCustomCaptureTypeText.Text.Trim();
            return string.IsNullOrWhiteSpace(customType) ? "Custom" : customType;
        }

        return string.IsNullOrWhiteSpace(selectedType) ? "Custom" : selectedType;
    }

    private string BuildQuestItemsCaptureLabel(string fallback)
    {
        var label = GetQuestItemsCaptureLabel();
        return string.IsNullOrWhiteSpace(label) ? fallback : label;
    }

    private string GetQuestItemsCaptureALabel()
    {
        return string.IsNullOrWhiteSpace(_questItemsCaptureA?.Label)
            ? "Capture A"
            : _questItemsCaptureA.Label;
    }

    private string GetQuestItemsCaptureBLabel()
    {
        return string.IsNullOrWhiteSpace(_questItemsCaptureB?.Label)
            ? "Capture B"
            : _questItemsCaptureB.Label;
    }

    private void UpdateQuestItemsCaptureSummary()
    {
        QuestItemsCaptureAText.Text = FormatQuestItemsCaptureSummary(
            "A",
            _questItemsCaptureA,
            _questItemsBeforeSnapshotBytes,
            _questItemsBeforeSnapshotStart,
            _questItemsCaptureAPath);
        QuestItemsCaptureBText.Text = FormatQuestItemsCaptureSummary(
            "B",
            _questItemsCaptureB,
            _questItemsAfterSnapshotBytes,
            _questItemsAfterSnapshotStart,
            _questItemsCaptureBPath);
    }

    private static string FormatQuestItemsCaptureSummary(
        string slot,
        QuestItemsCaptureDocument? capture,
        byte[]? bytes,
        uint startOffset,
        string path)
    {
        if (bytes is null)
        {
            return $"Capture {slot}: not loaded";
        }

        var label = string.IsNullOrWhiteSpace(capture?.Label) ? $"Capture {slot}" : capture.Label;
        var timestamp = capture?.Timestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? "-";
        var source = string.IsNullOrWhiteSpace(path) ? "current session" : Path.GetFileName(path);
        return $"Capture {slot}: {label} | start 0x{startOffset:X} | length 0x{bytes.Length:X} | {timestamp} | {source}";
    }

    private QuestItemsCaptureDocument CreateQuestItemsCaptureDocument(
        DateTimeOffset timestamp,
        string label,
        uint startOffset,
        byte[] bytes,
        string notes)
    {
        return new QuestItemsCaptureDocument(
            timestamp,
            label,
            startOffset,
            bytes.Length,
            bytes.Select(value => $"0x{value:X2}").ToList(),
            notes,
            GetQuestItemsCaptureType(),
            Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown");
    }

    private string? TrySaveQuestItemsCapture(
        string label,
        uint startOffset,
        byte[] bytes,
        DateTimeOffset timestamp,
        string notes)
    {
        try
        {
            Directory.CreateDirectory(QuestSearchDirectory);
            var capture = CreateQuestItemsCaptureDocument(timestamp, label, startOffset, bytes, notes);
            var fileName = CreateQuestItemsCaptureFileName(timestamp, label);
            var path = Path.Combine(QuestSearchDirectory, fileName);
            File.WriteAllText(path, JsonSerializer.Serialize(capture, ExportJsonOptions));
            return path;
        }
        catch (Exception ex)
        {
            QuestItemsResearchStatusText.Text =
                $"{QuestItemsResearchStatusText.Text} Capture save failed: {ex.Message}";
            AppendQuestItemsResearchLog($"action=capture-save-failed label=\"{label}\" error=\"{ex.Message}\"");
            SetStatus("Quest Items capture save failed.", StatusKind.Warning);
            return null;
        }
    }

    private void SaveQuestItemsCapturedSlot(
        string slotName,
        uint startOffset,
        byte[]? bytes,
        DateTimeOffset? capturedAt,
        QuestItemsCaptureDocument? existingCapture)
    {
        if (bytes is null)
        {
            QuestItemsResearchStatusText.Text =
                $"Capture {slotName} is empty. Capture or load it before saving.";
            SetStatus("Quest Items capture is empty.", StatusKind.Neutral);
            return;
        }

        var label = BuildQuestItemsCaptureLabel(existingCapture?.Label ?? $"Capture {slotName}");
        var notes = GetQuestItemsCaptureNotes();
        if (string.IsNullOrWhiteSpace(notes) && existingCapture is not null)
        {
            notes = existingCapture.Notes;
        }

        var path = TrySaveQuestItemsCapture(label, startOffset, bytes, capturedAt ?? DateTimeOffset.Now, notes);
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        if (slotName == "A")
        {
            _questItemsCaptureA = CreateQuestItemsCaptureDocument(capturedAt ?? DateTimeOffset.Now, label, startOffset, bytes, notes);
            _questItemsCaptureAPath = path;
        }
        else
        {
            _questItemsCaptureB = CreateQuestItemsCaptureDocument(capturedAt ?? DateTimeOffset.Now, label, startOffset, bytes, notes);
            _questItemsCaptureBPath = path;
        }

        UpdateQuestItemsCaptureSummary();
        QuestItemsResearchStatusText.Text =
            $"Saved Capture {slotName} \"{label}\" to {Path.GetFileName(path)}.";
        AppendQuestItemsResearchLog(
            $"action=save-capture-{slotName.ToLowerInvariant()} label=\"{label}\" path=\"{path}\" start=0x{startOffset:X} length=0x{bytes.Length:X}");
        SetStatus("Quest Items capture saved.", StatusKind.Connected);
    }

    private bool TryLoadQuestItemsCapture(
        string slotName,
        out QuestItemsCaptureDocument capture,
        out byte[] bytes,
        out string path)
    {
        capture = new QuestItemsCaptureDocument(DateTimeOffset.MinValue, string.Empty, 0, 0, [], string.Empty, string.Empty, string.Empty);
        bytes = [];
        path = string.Empty;

        var dialog = new OpenFileDialog
        {
            Title = $"Load Quest Items Capture {slotName}",
            Filter = "Quest Items captures (*.json)|*.json|All files (*.*)|*.*",
            FileName = "quest-capture_*.json"
        };

        if (Directory.Exists(QuestSearchDirectory))
        {
            dialog.InitialDirectory = QuestSearchDirectory;
        }

        if (dialog.ShowDialog(this) != true)
        {
            return false;
        }

        return TryLoadQuestItemsCaptureFromPath(dialog.FileName, out capture, out bytes, out path);
    }

    private bool TryLoadQuestItemsCaptureFromPath(
        string filePath,
        out QuestItemsCaptureDocument capture,
        out byte[] bytes,
        out string path)
    {
        capture = new QuestItemsCaptureDocument(DateTimeOffset.MinValue, string.Empty, 0, 0, [], string.Empty, string.Empty, string.Empty);
        bytes = [];
        path = filePath;

        try
        {
            var loaded = JsonSerializer.Deserialize<QuestItemsCaptureDocument>(
                File.ReadAllText(filePath),
                ExportJsonOptions);
            if (loaded is null)
            {
                QuestItemsResearchStatusText.Text = "Selected Quest Items capture could not be read.";
                SetStatus("Quest Items capture load failed.", StatusKind.Warning);
                return false;
            }

            if (!TryParseQuestItemsCaptureBytes(loaded, out bytes, out var error))
            {
                QuestItemsResearchStatusText.Text = error;
                SetStatus("Quest Items capture load failed.", StatusKind.Warning);
                return false;
            }

            capture = loaded;
            return true;
        }
        catch (Exception ex)
        {
            QuestItemsResearchStatusText.Text = $"Quest Items capture load failed: {ex.Message}";
            SetStatus("Quest Items capture load failed.", StatusKind.Warning);
            return false;
        }
    }

    private static bool TryParseQuestItemsCaptureBytes(
        QuestItemsCaptureDocument capture,
        out byte[] bytes,
        out string error)
    {
        bytes = [];
        error = string.Empty;

        if (capture.RawBytes.Count != capture.Length)
        {
            error = $"Capture length mismatch: metadata says 0x{capture.Length:X}, file contains 0x{capture.RawBytes.Count:X} byte(s).";
            return false;
        }

        var parsedBytes = new byte[capture.RawBytes.Count];
        for (var index = 0; index < capture.RawBytes.Count; index++)
        {
            if (!TryParseCandidateValue(capture.RawBytes[index], out parsedBytes[index], out _))
            {
                error = $"Invalid byte value at capture index {index}: {capture.RawBytes[index]}";
                return false;
            }
        }

        bytes = parsedBytes;
        return true;
    }

    private static string CreateQuestItemsCaptureFileName(DateTimeOffset timestamp, string label)
    {
        return $"quest-capture_{timestamp:yyyyMMdd_HHmmss}_{SanitizeFileName(label)}.json";
    }

    private string CreateQuestItemsReportBaseName()
    {
        var timestamp = _lastQuestItemsResearchExport?.Timestamp ?? DateTimeOffset.Now;
        return $"quest-report_{timestamp:yyyyMMdd_HHmmss}_{SanitizeFileName(GetQuestItemsCaptureALabel())}_vs_{SanitizeFileName(GetQuestItemsCaptureBLabel())}";
    }

    private string WriteQuestItemsNamedJsonReport()
    {
        if (_lastQuestItemsResearchExport is null)
        {
            throw new InvalidOperationException("Compare Quest Items captures before exporting a report.");
        }

        Directory.CreateDirectory(QuestSearchDirectory);
        var path = Path.Combine(QuestSearchDirectory, CreateQuestItemsReportBaseName() + ".json");
        File.WriteAllText(path, JsonSerializer.Serialize(_lastQuestItemsResearchExport, ExportJsonOptions));
        return path;
    }

    private string WriteQuestItemsNamedCsvReport()
    {
        if (_lastQuestItemsResearchExport is null)
        {
            throw new InvalidOperationException("Compare Quest Items captures before exporting a report.");
        }

        Directory.CreateDirectory(QuestSearchDirectory);
        var path = Path.Combine(QuestSearchDirectory, CreateQuestItemsReportBaseName() + ".csv");
        File.WriteAllLines(path, CreateQuestItemsResearchCsvLines(_lastQuestItemsResearchExport));
        return path;
    }

    private void ExportQuestItemsNamedReport()
    {
        if (_lastQuestItemsResearchExport is null)
        {
            QuestItemsResearchStatusText.Text = "Compare Quest Items captures before exporting a report.";
            SetStatus("Compare Quest Items captures before exporting.", StatusKind.Neutral);
            return;
        }

        try
        {
            var jsonPath = WriteQuestItemsNamedJsonReport();
            var csvPath = WriteQuestItemsNamedCsvReport();
            QuestItemsResearchStatusText.Text =
                $"Exported Quest Items report: {Path.GetFileName(jsonPath)} and {Path.GetFileName(csvPath)}.";
            AppendQuestItemsResearchLog(
                $"action=export-report json=\"{jsonPath}\" csv=\"{csvPath}\" rows={_lastQuestItemsResearchExport.Rows.Count}");
            SetStatus("Quest Items report exported.", StatusKind.Connected);
        }
        catch (Exception ex)
        {
            QuestItemsResearchStatusText.Text = $"Quest Items report export failed: {ex.Message}";
            SetStatus("Quest Items report export failed.", StatusKind.Warning);
        }
    }

    private void TryWriteQuestItemsSnapshotExport(
        string fileName,
        string label,
        uint startOffset,
        byte[] bytes,
        DateTimeOffset? capturedAt)
    {
        try
        {
            Directory.CreateDirectory(ResearchSnapshotStore.ExportDirectory);
            var snapshot = CreateQuestItemsCaptureDocument(
                capturedAt ?? DateTimeOffset.Now,
                label,
                startOffset,
                bytes,
                GetQuestItemsCaptureNotes());
            var path = Path.Combine(ResearchSnapshotStore.ExportDirectory, fileName);
            File.WriteAllText(path, JsonSerializer.Serialize(snapshot, ExportJsonOptions));
        }
        catch (Exception ex)
        {
            QuestItemsResearchStatusText.Text =
                $"{QuestItemsResearchStatusText.Text} Snapshot export failed: {ex.Message}";
            AppendQuestItemsResearchLog($"action=snapshot-export-failed file=\"{fileName}\" error=\"{ex.Message}\"");
        }
    }

    private string? TrySaveHiddenSkillsCapture(
        string kind,
        uint startOffset,
        byte[] bytes,
        DateTimeOffset timestamp,
        string label)
    {
        try
        {
            Directory.CreateDirectory(HiddenSkillsCaptureDirectory);
            var fileName = CreateHiddenSkillsCaptureFileName(kind, timestamp, label);
            var path = Path.Combine(HiddenSkillsCaptureDirectory, fileName);
            var capture = new HiddenSkillsCaptureDocument(
                timestamp,
                startOffset,
                bytes.Length,
                bytes.Select(value => $"0x{value:X2}").ToList(),
                label);
            File.WriteAllText(path, JsonSerializer.Serialize(capture, ExportJsonOptions));
            return path;
        }
        catch (Exception ex)
        {
            HiddenSkillsResearchStatusText.Text =
                $"{HiddenSkillsResearchStatusText.Text} Capture save failed: {ex.Message}";
            AppendHiddenSkillsResearchLog($"action=capture-save-failed kind={kind} error=\"{ex.Message}\"");
            return null;
        }
    }

    private bool TryLoadHiddenSkillsCapture(string kind, out HiddenSkillsCaptureDocument capture)
    {
        capture = new HiddenSkillsCaptureDocument(DateTimeOffset.MinValue, 0, 0, [], string.Empty);

        var dialog = new OpenFileDialog
        {
            Title = $"Load Hidden Skills {kind} capture",
            Filter = "Hidden Skills captures (*.json)|*.json|All files (*.*)|*.*",
            FileName = $"hidden-skills-{kind}_*.json"
        };

        if (Directory.Exists(HiddenSkillsCaptureDirectory))
        {
            dialog.InitialDirectory = HiddenSkillsCaptureDirectory;
        }

        if (dialog.ShowDialog(this) != true)
        {
            return false;
        }

        try
        {
            var loaded = JsonSerializer.Deserialize<HiddenSkillsCaptureDocument>(
                File.ReadAllText(dialog.FileName),
                ExportJsonOptions);
            if (loaded is null)
            {
                HiddenSkillsResearchStatusText.Text = "Selected Hidden Skills capture could not be read.";
                SetStatus("Hidden Skills capture load failed.", StatusKind.Warning);
                return false;
            }

            if (!TryParseHiddenSkillsCaptureBytes(loaded, out _, out var parseError))
            {
                HiddenSkillsResearchStatusText.Text = parseError;
                SetStatus("Hidden Skills capture load failed.", StatusKind.Warning);
                return false;
            }

            capture = loaded;
            return true;
        }
        catch (Exception ex)
        {
            HiddenSkillsResearchStatusText.Text = $"Hidden Skills capture load failed: {ex.Message}";
            SetStatus("Hidden Skills capture load failed.", StatusKind.Warning);
            return false;
        }
    }

    private static byte[] ParseHiddenSkillsCaptureBytes(HiddenSkillsCaptureDocument capture)
    {
        return TryParseHiddenSkillsCaptureBytes(capture, out var bytes, out _)
            ? bytes
            : [];
    }

    private static bool TryParseHiddenSkillsCaptureBytes(
        HiddenSkillsCaptureDocument capture,
        out byte[] bytes,
        out string error)
    {
        bytes = [];
        error = string.Empty;

        if (capture.RawBytes.Count != capture.Length)
        {
            error = $"Capture length mismatch: metadata says 0x{capture.Length:X}, file contains 0x{capture.RawBytes.Count:X} byte(s).";
            return false;
        }

        var parsedBytes = new byte[capture.RawBytes.Count];
        for (var index = 0; index < capture.RawBytes.Count; index++)
        {
            if (!TryParseCandidateValue(capture.RawBytes[index], out parsedBytes[index], out _))
            {
                error = $"Invalid byte value at capture index {index}: {capture.RawBytes[index]}";
                return false;
            }
        }

        bytes = parsedBytes;
        return true;
    }

    private static string CreateHiddenSkillsCaptureFileName(
        string kind,
        DateTimeOffset timestamp,
        string label)
    {
        var suffix = string.IsNullOrWhiteSpace(label)
            ? string.Empty
            : "_" + SanitizeFileName(label);
        return $"hidden-skills-{kind}_{timestamp:yyyyMMdd_HHmmss}{suffix}.json";
    }

    private void TryWriteHiddenSkillsSnapshotExport(
        string fileName,
        string label,
        uint startOffset,
        byte[] bytes,
        DateTimeOffset? capturedAt)
    {
        try
        {
            Directory.CreateDirectory(ResearchSnapshotStore.ExportDirectory);
            var snapshot = new HiddenSkillsCaptureDocument(
                capturedAt ?? DateTimeOffset.Now,
                startOffset,
                bytes.Length,
                bytes.Select(value => $"0x{value:X2}").ToList(),
                label);
            var path = Path.Combine(ResearchSnapshotStore.ExportDirectory, fileName);
            File.WriteAllText(path, JsonSerializer.Serialize(snapshot, ExportJsonOptions));
        }
        catch (Exception ex)
        {
            HiddenSkillsResearchStatusText.Text =
                $"{HiddenSkillsResearchStatusText.Text} Snapshot export failed: {ex.Message}";
            AppendHiddenSkillsResearchLog($"action=snapshot-export-failed file=\"{fileName}\" error=\"{ex.Message}\"");
        }
    }

    private bool TryLoadHiddenSkillsCaptureForMultiAnalyzer(
        string slotName,
        out HiddenSkillsCaptureDocument capture,
        out byte[] bytes,
        out string filePath)
    {
        capture = new HiddenSkillsCaptureDocument(DateTimeOffset.MinValue, 0, 0, [], string.Empty);
        bytes = [];
        filePath = string.Empty;

        var dialog = new OpenFileDialog
        {
            Title = $"Load Hidden Skills Capture {slotName}",
            Filter = "Hidden Skills captures (*.json)|*.json|All files (*.*)|*.*",
            FileName = "hidden-skills-*.json"
        };

        if (Directory.Exists(HiddenSkillsCaptureDirectory))
        {
            dialog.InitialDirectory = HiddenSkillsCaptureDirectory;
        }

        if (dialog.ShowDialog(this) != true)
        {
            return false;
        }

        try
        {
            var loaded = JsonSerializer.Deserialize<HiddenSkillsCaptureDocument>(
                File.ReadAllText(dialog.FileName),
                ExportJsonOptions);
            if (loaded is null)
            {
                HiddenSkillsMultiCaptureStatusText.Text = "Selected Hidden Skills capture could not be read.";
                SetStatus("Hidden Skills multi-capture load failed.", StatusKind.Warning);
                return false;
            }

            if (!TryParseHiddenSkillsCaptureBytes(loaded, out var parsedBytes, out var parseError))
            {
                HiddenSkillsMultiCaptureStatusText.Text = parseError;
                SetStatus("Hidden Skills multi-capture load failed.", StatusKind.Warning);
                return false;
            }

            capture = loaded;
            bytes = parsedBytes;
            filePath = dialog.FileName;
            return true;
        }
        catch (Exception ex)
        {
            HiddenSkillsMultiCaptureStatusText.Text = $"Hidden Skills multi-capture load failed: {ex.Message}";
            SetStatus("Hidden Skills multi-capture load failed.", StatusKind.Warning);
            return false;
        }
    }

    private bool TryReadHiddenSkillsMultiSkillCounts(out int[] skillCounts)
    {
        skillCounts = new int[6];
        for (var index = 0; index < skillCounts.Length; index++)
        {
            var textBox = GetHiddenSkillsMultiCaptureCountTextBox(index);
            if (!int.TryParse(textBox.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) ||
                count < 0)
            {
                HiddenSkillsMultiCaptureStatusText.Text =
                    $"Capture {GetHiddenSkillsMultiCaptureSlotName(index)} skill count must be a non-negative whole number.";
                SetStatus("Invalid Hidden Skills multi-capture skill count.", StatusKind.Warning);
                return false;
            }

            skillCounts[index] = count;
        }

        return true;
    }

    private TextBox GetHiddenSkillsMultiCaptureCountTextBox(int slotIndex)
    {
        return slotIndex switch
        {
            0 => HiddenSkillsMultiCaptureACountText,
            1 => HiddenSkillsMultiCaptureBCountText,
            2 => HiddenSkillsMultiCaptureCCountText,
            3 => HiddenSkillsMultiCaptureDCountText,
            4 => HiddenSkillsMultiCaptureECountText,
            _ => HiddenSkillsMultiCaptureFCountText
        };
    }

    private TextBox GetHiddenSkillsMultiCaptureFileTextBox(int slotIndex)
    {
        return slotIndex switch
        {
            0 => HiddenSkillsMultiCaptureAFileText,
            1 => HiddenSkillsMultiCaptureBFileText,
            2 => HiddenSkillsMultiCaptureCFileText,
            3 => HiddenSkillsMultiCaptureDFileText,
            4 => HiddenSkillsMultiCaptureEFileText,
            _ => HiddenSkillsMultiCaptureFFileText
        };
    }

    private static string GetHiddenSkillsMultiCaptureSlotName(int slotIndex)
    {
        return ((char)('A' + slotIndex)).ToString();
    }

    private static IReadOnlyList<HiddenSkillsMultiCaptureCandidateViewModel> CreateHiddenSkillsMultiCaptureCandidates(
        IReadOnlyList<HiddenSkillsLoadedCapture> captures,
        IReadOnlyList<int> skillCounts)
    {
        var candidates = new List<HiddenSkillsMultiCaptureCandidateAnalysis>();
        var startOffset = captures[0].Document.StartOffset;

        for (var index = 0; index < captures[0].Bytes.Length; index++)
        {
            var offset = startOffset + (uint)index;
            var values = captures.Select(capture => capture.Bytes[index]).ToArray();
            if (!HasChanged(values.Select(value => (int)value)))
            {
                continue;
            }

            AddHiddenSkillsValueFieldCandidate(candidates, offset, values, skillCounts);
            AddHiddenSkillsBitfieldCandidate(candidates, offset, values, skillCounts);
            AddHiddenSkillsBitCandidates(candidates, offset, values, skillCounts);
        }

        return candidates
            .OrderByDescending(candidate => candidate.Score)
            .ThenByDescending(candidate => candidate.ProgressionMatch)
            .ThenByDescending(candidate => candidate.OnlyIncreases)
            .ThenBy(candidate => candidate.OffsetValue)
            .ThenBy(candidate => candidate.Kind, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.Bit)
            .Select((candidate, index) => new HiddenSkillsMultiCaptureCandidateViewModel(
                index + 1,
                candidate.OffsetValue,
                candidate.Kind,
                candidate.Bit,
                candidate.Values,
                FormatIntSequence(skillCounts),
                candidate.Score,
                candidate.IsMonotonic,
                candidate.OnlyIncreases,
                candidate.ProgressionMatch,
                candidate.Notes))
            .ToList();
    }

    private static void AddHiddenSkillsValueFieldCandidate(
        ICollection<HiddenSkillsMultiCaptureCandidateAnalysis> candidates,
        uint offset,
        byte[] values,
        IReadOnlyList<int> skillCounts)
    {
        var numericValues = values.Select(value => (int)value).ToArray();
        var isMonotonic = IsMonotonicNonDecreasing(numericValues);
        var progressionMatch = SequenceMatches(numericValues, skillCounts);
        if (!isMonotonic && !progressionMatch)
        {
            return;
        }

        var score =
            10 +
            (isMonotonic ? 12 : 0) +
            (progressionMatch ? 80 : 0) +
            CountIncreasingSteps(numericValues) * 3;
        var notes = progressionMatch
            ? "Raw byte value exactly follows the known learned-skill count progression."
            : "Raw byte value changes monotonically across loaded captures.";

        candidates.Add(new HiddenSkillsMultiCaptureCandidateAnalysis(
            offset,
            "Value field",
            "-",
            FormatByteSequence(values),
            score,
            isMonotonic,
            false,
            progressionMatch,
            notes));
    }

    private static void AddHiddenSkillsBitfieldCandidate(
        ICollection<HiddenSkillsMultiCaptureCandidateAnalysis> candidates,
        uint offset,
        byte[] values,
        IReadOnlyList<int> skillCounts)
    {
        var bitCounts = values.Select(value => CountSetBits(value)).ToArray();
        var onlyIncreases = BitsOnlyIncrease(values);
        var bitCountMonotonic = IsMonotonicNonDecreasing(bitCounts);
        var progressionMatch = SequenceMatches(bitCounts, skillCounts);
        if (!onlyIncreases && !progressionMatch)
        {
            return;
        }

        var score =
            16 +
            (onlyIncreases ? 28 : 0) +
            (bitCountMonotonic ? 10 : 0) +
            (progressionMatch ? 90 : 0) +
            CountIncreasingSteps(bitCounts) * 4;
        var notes = progressionMatch
            ? "Set-bit count exactly follows the known learned-skill count progression."
            : $"Bits only increase across captures. Set-bit counts: {FormatIntSequence(bitCounts)}.";

        candidates.Add(new HiddenSkillsMultiCaptureCandidateAnalysis(
            offset,
            "Bitfield",
            "byte",
            $"{FormatByteSequence(values)} | bits {FormatIntSequence(bitCounts)}",
            score,
            bitCountMonotonic,
            onlyIncreases,
            progressionMatch,
            notes));
    }

    private static void AddHiddenSkillsBitCandidates(
        ICollection<HiddenSkillsMultiCaptureCandidateAnalysis> candidates,
        uint offset,
        byte[] values,
        IReadOnlyList<int> skillCounts)
    {
        for (var bit = 0; bit < 8; bit++)
        {
            var states = values.Select(value => IsBitSet(value, bit) ? 1 : 0).ToArray();
            if (!HasChanged(states) || !IsMonotonicNonDecreasing(states))
            {
                continue;
            }

            var transitionCount = CountIncreasingSteps(states);
            var score =
                8 +
                transitionCount * 8 +
                (states[^1] == 1 ? 8 : 0) +
                (skillCounts.Zip(states, (count, state) => count > 0 && state == 1).Count(match => match) > 0 ? 2 : 0);
            candidates.Add(new HiddenSkillsMultiCaptureCandidateAnalysis(
                offset,
                "Bit",
                bit.ToString(CultureInfo.InvariantCulture),
                FormatIntSequence(states),
                score,
                true,
                true,
                false,
                "Individual bit turns on and never clears across loaded captures."));
        }
    }

    private HiddenSkillsMultiCaptureExport CreateHiddenSkillsMultiCaptureExport(
        IReadOnlyList<HiddenSkillsLoadedCapture> captures,
        IReadOnlyList<int> skillCounts)
    {
        return new HiddenSkillsMultiCaptureExport(
            DateTimeOffset.Now,
            captures[0].Document.StartOffset,
            captures[0].Bytes.Length,
            skillCounts.ToList(),
            captures.Select(capture => new HiddenSkillsMultiCaptureExportCapture(
                capture.SlotName,
                capture.Document.LabelOrDefault,
                capture.Document.Timestamp,
                capture.Document.StartOffset,
                capture.Document.Length,
                capture.FilePath)).ToList(),
            HiddenSkillsMultiCaptureCandidates.Select(candidate => new HiddenSkillsMultiCaptureExportRow(
                candidate.Rank,
                candidate.Offset,
                candidate.Kind,
                candidate.Bit,
                candidate.Values,
                candidate.SkillCounts,
                candidate.Score,
                candidate.IsMonotonic,
                candidate.OnlyIncreases,
                candidate.ProgressionMatch,
                candidate.Flags,
                candidate.Notes)).ToList());
    }

    private bool TryLoadHiddenSkillsCaptureForRegionAnalyzer(
        string slotName,
        out HiddenSkillsCaptureDocument capture,
        out byte[] bytes,
        out string filePath)
    {
        capture = new HiddenSkillsCaptureDocument(DateTimeOffset.MinValue, 0, 0, [], string.Empty);
        bytes = [];
        filePath = string.Empty;

        if (TryLoadHiddenSkillsCaptureFromDialog(
                $"Load Hidden Skills Region Capture {slotName}",
                out capture,
                out bytes,
                out filePath,
                out var error))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(error))
        {
            HiddenSkillsRegionStatusText.Text = error;
            SetStatus("Hidden Skills region capture load failed.", StatusKind.Warning);
        }

        return false;
    }

    private bool TryLoadHiddenSkillsCaptureFromDialog(
        string title,
        out HiddenSkillsCaptureDocument capture,
        out byte[] bytes,
        out string filePath,
        out string error)
    {
        capture = new HiddenSkillsCaptureDocument(DateTimeOffset.MinValue, 0, 0, [], string.Empty);
        bytes = [];
        filePath = string.Empty;
        error = string.Empty;

        var dialog = new OpenFileDialog
        {
            Title = title,
            Filter = "Hidden Skills captures (*.json)|*.json|All files (*.*)|*.*",
            FileName = "hidden-skills-*.json"
        };

        if (Directory.Exists(HiddenSkillsCaptureDirectory))
        {
            dialog.InitialDirectory = HiddenSkillsCaptureDirectory;
        }

        if (dialog.ShowDialog(this) != true)
        {
            return false;
        }

        try
        {
            var loaded = JsonSerializer.Deserialize<HiddenSkillsCaptureDocument>(
                File.ReadAllText(dialog.FileName),
                ExportJsonOptions);
            if (loaded is null)
            {
                error = "Selected Hidden Skills capture could not be read.";
                return false;
            }

            if (!TryParseHiddenSkillsCaptureBytes(loaded, out var parsedBytes, out var parseError))
            {
                error = parseError;
                return false;
            }

            capture = loaded;
            bytes = parsedBytes;
            filePath = dialog.FileName;
            return true;
        }
        catch (Exception ex)
        {
            error = $"Hidden Skills capture load failed: {ex.Message}";
            return false;
        }
    }

    private static bool TryExtractHiddenSkillsCaptureRange(
        HiddenSkillsCaptureDocument capture,
        byte[] captureBytes,
        uint startOffset,
        int length,
        out byte[] rangeBytes,
        out string error)
    {
        rangeBytes = [];
        error = string.Empty;

        var captureStart = capture.StartOffset;
        var captureEndExclusive = (ulong)capture.StartOffset + (ulong)captureBytes.Length;
        var requestedEndExclusive = (ulong)startOffset + (uint)length;
        if (startOffset < captureStart || requestedEndExclusive > captureEndExclusive)
        {
            error =
                $"Selected range _playerbase+0x{startOffset:X}/0x{length:X} is outside capture {capture.LabelOrDefault} (0x{capture.StartOffset:X}/0x{captureBytes.Length:X}).";
            return false;
        }

        rangeBytes = new byte[length];
        Array.Copy(captureBytes, startOffset - captureStart, rangeBytes, 0, length);
        return true;
    }

    private static IReadOnlyList<HiddenSkillsRegionAnalysisRowViewModel> CreateHiddenSkillsRegionAnalysisRows(
        uint startOffset,
        byte[] captureABytes,
        byte[] captureBBytes)
    {
        var changes = new List<HiddenSkillsRegionChangedByte>();
        for (var index = 0; index < captureABytes.Length; index++)
        {
            var beforeValue = captureABytes[index];
            var afterValue = captureBBytes[index];
            if (beforeValue == afterValue)
            {
                continue;
            }

            changes.Add(new HiddenSkillsRegionChangedByte(
                startOffset + (uint)index,
                beforeValue,
                afterValue,
                CountSetBits((uint)(beforeValue ^ afterValue)),
                Math.Abs(afterValue - beforeValue)));
        }

        if (changes.Count == 0)
        {
            return [];
        }

        var candidates = new List<HiddenSkillsRegionAnalysisCandidate>();
        var group = new List<HiddenSkillsRegionChangedByte>();
        foreach (var change in changes)
        {
            if (group.Count > 0 && change.OffsetValue - group[^1].OffsetValue > 16)
            {
                candidates.Add(CreateHiddenSkillsRegionAnalysisCandidate(group, startOffset, captureABytes.Length));
                group.Clear();
            }

            group.Add(change);
        }

        if (group.Count > 0)
        {
            candidates.Add(CreateHiddenSkillsRegionAnalysisCandidate(group, startOffset, captureABytes.Length));
        }

        return candidates
            .OrderBy(candidate => candidate.Density)
            .ThenByDescending(candidate => candidate.CandidateScore)
            .ThenByDescending(candidate => candidate.IsSingleBitRegion)
            .ThenBy(candidate => candidate.RegionStartValue)
            .Select((candidate, index) => new HiddenSkillsRegionAnalysisRowViewModel(
                index + 1,
                candidate.RegionStartValue,
                candidate.RegionEndValue,
                candidate.ChangedBytes,
                candidate.ChangedBits,
                candidate.Density,
                candidate.LargestChange,
                candidate.CandidateScore))
            .ToList();
    }

    private static HiddenSkillsRegionAnalysisCandidate CreateHiddenSkillsRegionAnalysisCandidate(
        IReadOnlyList<HiddenSkillsRegionChangedByte> changes,
        uint selectedStartOffset,
        int selectedLength)
    {
        var selectedEndOffset = selectedStartOffset + (uint)selectedLength - 1;
        var regionStart = changes[0].OffsetValue;
        var regionEnd = Math.Min(selectedEndOffset, Math.Max(changes[^1].OffsetValue, regionStart + 0xF));
        var regionLength = Math.Max(1, (int)(regionEnd - regionStart + 1));
        var changedBytes = changes.Count;
        var changedBits = changes.Sum(change => change.ChangedBitCount);
        var density = changedBytes * 100.0 / regionLength;
        var largestChange = changes.Max(change => change.AbsoluteDelta);
        var isSingleBit = changedBytes == 1 && changedBits == 1;
        var score =
            (int)Math.Round(Math.Max(0, 100 - density), MidpointRounding.AwayFromZero) +
            (isSingleBit ? 80 : 0) +
            (changedBytes <= 3 ? 35 : 0) +
            (changedBits <= 3 ? 25 : 0) +
            (largestChange == 1 ? 10 : 0);

        return new HiddenSkillsRegionAnalysisCandidate(
            regionStart,
            regionEnd,
            changedBytes,
            changedBits,
            density,
            largestChange,
            score,
            isSingleBit);
    }

    private HiddenSkillsRegionAnalysisExport CreateHiddenSkillsRegionAnalysisExport(uint startOffset, int length)
    {
        return new HiddenSkillsRegionAnalysisExport(
            DateTimeOffset.Now,
            startOffset,
            length,
            _hiddenSkillsRegionCaptureA is null
                ? null
                : new HiddenSkillsRegionAnalysisCaptureExport(
                    "A",
                    _hiddenSkillsRegionCaptureA.LabelOrDefault,
                    _hiddenSkillsRegionCaptureA.Timestamp,
                    _hiddenSkillsRegionCaptureA.StartOffset,
                    _hiddenSkillsRegionCaptureA.Length,
                    _hiddenSkillsRegionCaptureAPath),
            _hiddenSkillsRegionCaptureB is null
                ? null
                : new HiddenSkillsRegionAnalysisCaptureExport(
                    "B",
                    _hiddenSkillsRegionCaptureB.LabelOrDefault,
                    _hiddenSkillsRegionCaptureB.Timestamp,
                    _hiddenSkillsRegionCaptureB.StartOffset,
                    _hiddenSkillsRegionCaptureB.Length,
                    _hiddenSkillsRegionCaptureBPath),
            HiddenSkillsRegionAnalysisRows.Select(row => new HiddenSkillsRegionAnalysisExportRow(
                row.Rank,
                row.Region,
                row.ChangedBytes,
                row.ChangedBits,
                row.Density,
                row.LargestChange,
                row.CandidateScore,
                row.IsSingleBitRegion,
                row.HighlightLabel)).ToList());
    }

    private bool TryReadHiddenSkillsLiveWatchRange(out uint startOffset, out byte[] bytes)
    {
        bytes = [];
        if (!TryParseResearchOffset(HiddenSkillsLiveWatchStartOffsetText.Text, out startOffset, out var offsetError))
        {
            HiddenSkillsLiveWatchStatusText.Text = offsetError;
            SetStatus("Invalid Hidden Skills live watch start offset.", StatusKind.Warning);
            return false;
        }

        if (!TryParseResearchLength(HiddenSkillsLiveWatchLengthText.Text, out var length, out var lengthError))
        {
            HiddenSkillsLiveWatchStatusText.Text = lengthError;
            SetStatus("Invalid Hidden Skills live watch length.", StatusKind.Warning);
            return false;
        }

        return TryReadHiddenSkillsLiveWatchBytes(startOffset, length, out bytes);
    }

    private bool TryReadHiddenSkillsLiveWatchBytes(uint startOffset, int length, out byte[] bytes)
    {
        bytes = [];
        var memory = _memory;
        if (memory is null || !_playerBaseAddress.HasValue)
        {
            HiddenSkillsLiveWatchStatusText.Text = "Not attached. Attach to Cemu and rescan before starting live watch.";
            SetStatus("Attach to Cemu before starting Hidden Skills live watch.", StatusKind.Neutral);
            return false;
        }

        var absoluteAddress = _playerBaseAddress.Value + startOffset;
        if (!memory.TryReadBytes(absoluteAddress, length, out var readBytes, out var bytesRead) || bytesRead != length)
        {
            HiddenSkillsLiveWatchStatusText.Text =
                $"Could not read live watch range _playerbase+0x{startOffset:X}/0x{length:X}.";
            SetStatus("Hidden Skills live watch read failed.", StatusKind.Warning);
            return false;
        }

        bytes = readBytes;
        return true;
    }

    private void HiddenSkillsLiveWatchTimer_Tick(object? sender, EventArgs e)
    {
        if (_hiddenSkillsLiveWatchPreviousBytes is null)
        {
            StopHiddenSkillsLiveWatch("Live watch stopped because no baseline is available.");
            return;
        }

        if (!TryReadHiddenSkillsLiveWatchBytes(
                _hiddenSkillsLiveWatchStartOffset,
                _hiddenSkillsLiveWatchLength,
                out var currentBytes))
        {
            StopHiddenSkillsLiveWatch("Live watch stopped because the range could not be read.");
            return;
        }

        var timestamp = DateTimeOffset.Now;
        var changeCount = 0;
        for (var index = 0; index < currentBytes.Length; index++)
        {
            var beforeValue = _hiddenSkillsLiveWatchPreviousBytes[index];
            var afterValue = currentBytes[index];
            if (beforeValue == afterValue)
            {
                continue;
            }

            var offset = _hiddenSkillsLiveWatchStartOffset + (uint)index;
            _hiddenSkillsLiveWatchChangeCounts[offset] =
                _hiddenSkillsLiveWatchChangeCounts.TryGetValue(offset, out var previousCount)
                    ? previousCount + 1
                    : 1;
            var row = new HiddenSkillsLiveWatchRowViewModel(
                timestamp,
                offset,
                beforeValue,
                afterValue,
                _hiddenSkillsLiveWatchChangeCounts[offset]);
            HiddenSkillsLiveWatchRows.Insert(0, row);
            while (HiddenSkillsLiveWatchRows.Count > 500)
            {
                HiddenSkillsLiveWatchRows.RemoveAt(HiddenSkillsLiveWatchRows.Count - 1);
            }

            AppendHiddenSkillsLiveWatchLog(
                $"action=change offset=0x{offset:X} before={FormatCandidateLogByte(beforeValue)} after={FormatCandidateLogByte(afterValue)} " +
                $"before-binary={row.BeforeBinary} after-binary={row.AfterBinary} changed-bits=\"{row.ChangedBits}\" count={row.ChangeCount}");
            changeCount++;
        }

        _hiddenSkillsLiveWatchPreviousBytes = currentBytes;
        if (changeCount > 0)
        {
            HiddenSkillsLiveWatchStatusText.Text =
                $"Live watch running. Recorded {changeCount} change(s) at {timestamp.ToLocalTime():HH:mm:ss.fff}.";
        }
    }

    private void StopHiddenSkillsLiveWatch(string message)
    {
        if (_hiddenSkillsLiveWatchTimer.IsEnabled)
        {
            _hiddenSkillsLiveWatchTimer.Stop();
            AppendHiddenSkillsLiveWatchLog($"action=stop message=\"{message}\"");
        }

        HiddenSkillsLiveWatchStatusText.Text = message;
        if (_memory is not null && _playerBaseAddress.HasValue)
        {
            SetStatus(message, StatusKind.Neutral);
        }
    }

    private HiddenSkillsLiveWatchExport CreateHiddenSkillsLiveWatchExport()
    {
        return new HiddenSkillsLiveWatchExport(
            DateTimeOffset.Now,
            _hiddenSkillsLiveWatchStartedAt,
            _hiddenSkillsLiveWatchStartOffset,
            _hiddenSkillsLiveWatchLength,
            HiddenSkillsLiveWatchRows.Select(row => new HiddenSkillsLiveWatchExportRow(
                row.Timestamp,
                row.Offset,
                row.BeforeValue,
                row.AfterValue,
                row.BeforeBinary,
                row.AfterBinary,
                row.ChangedBits,
                row.ChangedBitCount,
                row.ChangeCount,
                row.IsSingleBitChange,
                row.IsRepeatedChange,
                row.IsMonotonicChange,
                row.HighlightLabel)).ToList(),
            HiddenSkillsEventMarkers.Select(marker => new HiddenSkillsEventMarkerExport(
                marker.Timestamp,
                marker.Label)).ToList());
    }

    private void AppendGoldenBugsBitfieldTestingDiagnostic(
        string action,
        GoldenBugBitViewModel bit,
        ulong absoluteAddress,
        byte? beforeByte,
        byte? afterByte,
        byte changedMask,
        byte? immediateReadback,
        byte? delayed250Readback,
        byte? delayed1000Readback,
        string status)
    {
        var entry =
            $"{DateTimeOffset.Now:O} action={action} offset={bit.Offset} bit={bit.Bit} " +
            $"address=0x{absoluteAddress:X} bug=\"{bit.ConfirmedBugName}\" mapping-status={bit.MappingStatus} " +
            $"before={FormatEquipmentByte(beforeByte)} after={FormatEquipmentByte(afterByte)} " +
            $"changed-mask={FormatEquipmentByte(changedMask)} immediate={FormatEquipmentByte(immediateReadback)} " +
            $"read250ms={FormatEquipmentByte(delayed250Readback)} read1000ms={FormatEquipmentByte(delayed1000Readback)} " +
            $"desired={bit.IsSetDesired} result=\"{status}\"";

        AppendGoldenBugsBitfieldTestingLog(entry);
    }

    private void AppendGoldenBugsBitfieldRestoreDiagnostic(
        ulong absoluteAddress,
        byte[]? beforeBytes,
        byte[] targetBytes,
        byte[]? immediateBytes,
        byte[]? delayed250Bytes,
        byte[]? delayed1000Bytes,
        string status)
    {
        var entry =
            $"{DateTimeOffset.Now:O} action=restore-bitfield offset=0x{GoldenBugsDefinitions.FirstOffset:X} " +
            $"address=0x{absoluteAddress:X} before=\"{FormatNullableByteArray(beforeBytes)}\" " +
            $"target=\"{FormatByteArray(targetBytes)}\" immediate=\"{FormatNullableByteArray(immediateBytes)}\" " +
            $"read250ms=\"{FormatNullableByteArray(delayed250Bytes)}\" read1000ms=\"{FormatNullableByteArray(delayed1000Bytes)}\" " +
            $"result=\"{status}\"";

        AppendGoldenBugsBitfieldTestingLog(entry);
    }

    private void AppendGoldenBugsEditorDiagnostic(
        string operation,
        byte[]? beforeBytes,
        byte[] desiredBytes,
        byte[]? immediateBytes,
        byte[]? delayed250Bytes,
        byte[]? delayed1000Bytes,
        string status)
    {
        var entry =
            $"{DateTimeOffset.Now:O} operation={operation} " +
            $"before=\"{FormatNullableByteArray(beforeBytes)}\" desired=\"{FormatByteArray(desiredBytes)}\" " +
            $"immediate=\"{FormatNullableByteArray(immediateBytes)}\" read250ms=\"{FormatNullableByteArray(delayed250Bytes)}\" " +
            $"read1000ms=\"{FormatNullableByteArray(delayed1000Bytes)}\" status=\"{status}\"";

        GoldenBugsEditorDiagnostics.Insert(0, entry);
        while (GoldenBugsEditorDiagnostics.Count > 100)
        {
            GoldenBugsEditorDiagnostics.RemoveAt(GoldenBugsEditorDiagnostics.Count - 1);
        }

        try
        {
            var logDirectory = Path.GetDirectoryName(GoldenBugsEditorLogPath);
            if (!string.IsNullOrWhiteSpace(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            File.AppendAllText(GoldenBugsEditorLogPath, entry + Environment.NewLine);
        }
        catch (Exception ex)
        {
            GoldenBugsEditorDiagnostics.Insert(0, $"{DateTimeOffset.Now:O} golden-bugs-editor-log-write-failed: {ex.Message}");
        }
    }

    private void AppendHiddenSkillsEditorDiagnostic(
        string operation,
        byte[]? beforeBytes,
        byte[] desiredBytes,
        byte[]? immediateBytes,
        byte[]? delayed250Bytes,
        byte[]? delayed1000Bytes,
        string status)
    {
        var entry =
            $"{DateTimeOffset.Now:O} operation={operation} " +
            $"before=\"{FormatNullableByteArray(beforeBytes)}\" desired=\"{FormatByteArray(desiredBytes)}\" " +
            $"immediate=\"{FormatNullableByteArray(immediateBytes)}\" read250ms=\"{FormatNullableByteArray(delayed250Bytes)}\" " +
            $"read1000ms=\"{FormatNullableByteArray(delayed1000Bytes)}\" status=\"{status}\"";

        HiddenSkillsEditorDiagnostics.Insert(0, entry);
        while (HiddenSkillsEditorDiagnostics.Count > 100)
        {
            HiddenSkillsEditorDiagnostics.RemoveAt(HiddenSkillsEditorDiagnostics.Count - 1);
        }

        try
        {
            var logDirectory = Path.GetDirectoryName(HiddenSkillsEditorLogPath);
            if (!string.IsNullOrWhiteSpace(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            File.AppendAllText(HiddenSkillsEditorLogPath, entry + Environment.NewLine);
        }
        catch (Exception ex)
        {
            HiddenSkillsEditorDiagnostics.Insert(0, $"{DateTimeOffset.Now:O} hidden-skills-editor-log-write-failed: {ex.Message}");
        }
    }

    private void AppendHiddenSkillsEditorStatusDiagnostic(string details)
    {
        var entry = $"{DateTimeOffset.Now:O} {details}";

        HiddenSkillsEditorDiagnostics.Insert(0, entry);
        while (HiddenSkillsEditorDiagnostics.Count > 100)
        {
            HiddenSkillsEditorDiagnostics.RemoveAt(HiddenSkillsEditorDiagnostics.Count - 1);
        }

        try
        {
            var logDirectory = Path.GetDirectoryName(HiddenSkillsEditorLogPath);
            if (!string.IsNullOrWhiteSpace(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            File.AppendAllText(HiddenSkillsEditorLogPath, entry + Environment.NewLine);
        }
        catch (Exception ex)
        {
            HiddenSkillsEditorDiagnostics.Insert(0, $"{DateTimeOffset.Now:O} hidden-skills-editor-log-write-failed: {ex.Message}");
        }
    }

    private void AppendGoldenBugsBitfieldTestingLog(string entry)
    {
        GoldenBugsBitfieldDiagnostics.Insert(0, entry);
        while (GoldenBugsBitfieldDiagnostics.Count > 100)
        {
            GoldenBugsBitfieldDiagnostics.RemoveAt(GoldenBugsBitfieldDiagnostics.Count - 1);
        }

        try
        {
            var logDirectory = Path.GetDirectoryName(GoldenBugsBitfieldTestingLogPath);
            if (!string.IsNullOrWhiteSpace(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            File.AppendAllText(GoldenBugsBitfieldTestingLogPath, entry + Environment.NewLine);
        }
        catch (Exception ex)
        {
            GoldenBugsBitfieldDiagnostics.Insert(0, $"{DateTimeOffset.Now:O} golden-bugs-bitfield-log-write-failed: {ex.Message}");
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

    private static bool IsBitSet(byte value, int bit)
    {
        return (value & (1 << bit)) != 0;
    }

    private static string FormatResearchByte(byte value)
    {
        return $"{value} / 0x{value:X2}";
    }

    private static string FormatHiddenSkillsByte(byte value)
    {
        return $"{value} / 0x{value:X2} / {Convert.ToString(value, 2).PadLeft(8, '0')}";
    }

    private static string FormatUInt32Binary(uint value)
    {
        var binary = Convert.ToString(value, 2).PadLeft(32, '0');
        return string.Join(" ", Enumerable.Range(0, 4).Select(index => binary.Substring(index * 8, 8)));
    }

    private static string FormatByteArray(byte[] bytes)
    {
        return string.Join(" ", bytes.Select(value => $"0x{value:X2}"));
    }

    private static string FormatByteSequence(IEnumerable<byte> values)
    {
        return string.Join(" -> ", values.Select(value => $"{value} / 0x{value:X2}"));
    }

    private static string FormatIntSequence(IEnumerable<int> values)
    {
        return string.Join(" -> ", values.Select(value => value.ToString(CultureInfo.InvariantCulture)));
    }

    private static string FormatNullableByteArray(byte[]? bytes)
    {
        return bytes is null ? "n/a" : FormatByteArray(bytes);
    }

    private static bool HasChanged(IEnumerable<int> values)
    {
        int? firstValue = null;
        foreach (var value in values)
        {
            firstValue ??= value;
            if (value != firstValue.Value)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsMonotonicNonDecreasing(IReadOnlyList<int> values)
    {
        for (var index = 1; index < values.Count; index++)
        {
            if (values[index] < values[index - 1])
            {
                return false;
            }
        }

        return true;
    }

    private static int CountIncreasingSteps(IReadOnlyList<int> values)
    {
        var count = 0;
        for (var index = 1; index < values.Count; index++)
        {
            if (values[index] > values[index - 1])
            {
                count++;
            }
        }

        return count;
    }

    private static bool SequenceMatches(IReadOnlyList<int> values, IReadOnlyList<int> expectedValues)
    {
        if (values.Count != expectedValues.Count)
        {
            return false;
        }

        for (var index = 0; index < values.Count; index++)
        {
            if (values[index] != expectedValues[index])
            {
                return false;
            }
        }

        return true;
    }

    private static bool BitsOnlyIncrease(IReadOnlyList<byte> values)
    {
        for (var index = 1; index < values.Count; index++)
        {
            if ((values[index - 1] & ~values[index]) != 0)
            {
                return false;
            }
        }

        return true;
    }

    private static byte[] GetBigEndianBytes(uint value)
    {
        return
        [
            (byte)(value >> 24),
            (byte)(value >> 16),
            (byte)(value >> 8),
            (byte)value
        ];
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
        UpdateBombSlotDiagnostics();
    }

    private void UpdateBombSlotDiagnostics()
    {
        UpdateBombSlotDiagnosticDisplay(
            BombSlot1.CurrentNumericValue,
            BombSlot1RawValueText,
            BombSlot1DetectedTypeText,
            BombSlot1DecimalText,
            BombSlot1HexText,
            BombSlot1BinaryText);
        UpdateBombSlotDiagnosticDisplay(
            BombSlot2.CurrentNumericValue,
            BombSlot2RawValueText,
            BombSlot2DetectedTypeText,
            BombSlot2DecimalText,
            BombSlot2HexText,
            BombSlot2BinaryText);
        UpdateBombSlotDiagnosticDisplay(
            BombSlot3.CurrentNumericValue,
            BombSlot3RawValueText,
            BombSlot3DetectedTypeText,
            BombSlot3DecimalText,
            BombSlot3HexText,
            BombSlot3BinaryText);
    }

    private static void UpdateBombSlotDiagnosticDisplay(
        int? value,
        TextBox rawText,
        TextBox detectedTypeText,
        TextBox decimalText,
        TextBox hexText,
        TextBox binaryText)
    {
        if (!value.HasValue || value.Value is < byte.MinValue or > byte.MaxValue)
        {
            rawText.Text = "Not read";
            detectedTypeText.Text = "Unknown";
            decimalText.Text = "-";
            hexText.Text = "-";
            binaryText.Text = "-";
            return;
        }

        var byteValue = (byte)value.Value;
        rawText.Text = FormatBombSlotByte(byteValue);
        detectedTypeText.Text = DecodeBombSlotType(byteValue);
        decimalText.Text = byteValue.ToString(CultureInfo.InvariantCulture);
        hexText.Text = $"0x{byteValue:X2}";
        binaryText.Text = Convert.ToString(byteValue, 2).PadLeft(8, '0');
    }

    private static string FormatBombSlotByte(byte value)
    {
        return $"{value} / 0x{value:X2}";
    }

    private static string DecodeBombSlotType(byte value)
    {
        return BombSlotDefinitions.GetContentName(value);
    }

    private void UpdateGoldenBugsResearchCurrentDisplay(uint rawValue, bool preserveDirty = true)
    {
        GoldenBugsResearchDecimalText.Text = rawValue.ToString(CultureInfo.InvariantCulture);
        GoldenBugsResearchHexText.Text = $"0x{rawValue:X8}";
        GoldenBugsResearchBinaryText.Text = FormatUInt32Binary(rawValue);
        GoldenBugsResearchBitCountText.Text = $"{CountSetBits(rawValue)} set";
        UpdateGoldenBugsBitRows(GetBigEndianBytes(rawValue), preserveDirty);
    }

    private void MarkGoldenBugsResearchNotRead()
    {
        GoldenBugsEditorOwnedCountText.Text = "Not read";
        GoldenBugsEditorRawBytesText.Text = "Not read";
        GoldenBugsEditorStatusText.Text = "Attach to Cemu and rescan before editing Golden Bugs.";
        GoldenBugsResearchDecimalText.Text = "Not read";
        GoldenBugsResearchHexText.Text = "Not read";
        GoldenBugsResearchBinaryText.Text = "Not read";
        GoldenBugsResearchBitCountText.Text = "Not read";
        foreach (var bit in GoldenBugsBitRows)
        {
            bit.MarkNotRead();
        }
    }

    private void UpdateGoldenBugsBitRows(IReadOnlyList<byte> bytes, bool preserveDirty)
    {
        foreach (var bit in GoldenBugsBitRows)
        {
            var byteIndex = (int)(bit.OffsetValue - GoldenBugsDefinitions.FirstOffset);
            if (byteIndex < 0 || byteIndex >= bytes.Count)
            {
                bit.MarkNotRead();
                continue;
            }

            var isSet = (bytes[byteIndex] & (1 << bit.Bit)) != 0;
            bit.SetDetected(isSet, preserveDirty);
            bit.CanEdit = HasPlayerData;
        }

        UpdateGoldenBugsEditorSummary(bytes);
    }

    private void UpdateGoldenBugsEditorSummary(IReadOnlyList<byte> bytes)
    {
        if (bytes.Count < GoldenBugsDefinitions.OwnershipByteCount)
        {
            GoldenBugsEditorOwnedCountText.Text = "Not read";
            GoldenBugsEditorRawBytesText.Text = "Not read";
            return;
        }

        var ownershipBytes = bytes.Take(GoldenBugsDefinitions.OwnershipByteCount).ToArray();
        GoldenBugsEditorOwnedCountText.Text =
            $"{GoldenBugsEditorRows.Count(bit => bit.IsSetDetected == true)} / 24";
        GoldenBugsEditorRawBytesText.Text = FormatByteArray(ownershipBytes);
        GoldenBugsCountText.Text = GoldenBugsEditorOwnedCountText.Text;
    }

    private void UpdateHiddenSkillsEditorRows(IReadOnlyList<byte> bytes, bool preserveDirty)
    {
        var previousSuppressHiddenSkillDependencyEnforcement = _suppressHiddenSkillDependencyEnforcement;
        _suppressHiddenSkillDependencyEnforcement = true;
        try
        {
            foreach (var skill in HiddenSkillsEditorRows)
            {
                var byteIndex = (int)(skill.OffsetValue - HiddenSkillsDefinitions.FirstOffset);
                if (byteIndex < 0 || byteIndex >= bytes.Count)
                {
                    skill.MarkNotRead();
                    continue;
                }

                var isOwned = (bytes[byteIndex] & (1 << skill.Bit)) != 0;
                skill.SetDetected(isOwned, preserveDirty);
                skill.CanEdit = HasPlayerData;
            }
        }
        finally
        {
            _suppressHiddenSkillDependencyEnforcement = previousSuppressHiddenSkillDependencyEnforcement;
        }

        UpdateHiddenSkillsByteDisplays(bytes);
    }

    private void UpdateHiddenSkillsByteDisplays(IReadOnlyList<byte> bytes)
    {
        var byte3D5 = bytes.Count > 0 ? FormatHiddenSkillsByte(bytes[0]) : "Not read";
        var byte3D6 = bytes.Count > 1 ? FormatHiddenSkillsByte(bytes[1]) : "Not read";
        HiddenSkillsByte3D5Text.Text = byte3D5;
        HiddenSkillsByte3D6Text.Text = byte3D6;
        HiddenSkillsDebugByte3D5Text.Text = byte3D5;
        HiddenSkillsDebugByte3D6Text.Text = byte3D6;
    }

    private void MarkHiddenSkillsNotRead()
    {
        var previousSuppressHiddenSkillDependencyEnforcement = _suppressHiddenSkillDependencyEnforcement;
        _suppressHiddenSkillDependencyEnforcement = true;
        try
        {
            foreach (var skill in HiddenSkillsEditorRows)
            {
                skill.MarkNotRead();
            }
        }
        finally
        {
            _suppressHiddenSkillDependencyEnforcement = previousSuppressHiddenSkillDependencyEnforcement;
        }

        HiddenSkillsByte3D5Text.Text = "Not read";
        HiddenSkillsByte3D6Text.Text = "Not read";
        HiddenSkillsDebugByte3D5Text.Text = "Not read";
        HiddenSkillsDebugByte3D6Text.Text = "Not read";
        HiddenSkillsEditorStatusText.Text = "Attach to Cemu and rescan before editing Hidden Skills.";
    }

    private void EnforceHiddenSkillProgressionFrom(HiddenSkillViewModel changedSkill)
    {
        var changedIndex = HiddenSkillsEditorRows.IndexOf(changedSkill);
        if (changedIndex < 0)
        {
            return;
        }

        var adjustedSkills = new List<string>();
        var previousSuppressHiddenSkillDependencyEnforcement = _suppressHiddenSkillDependencyEnforcement;
        _suppressHiddenSkillDependencyEnforcement = true;
        try
        {
            if (changedSkill.IsOwnedDesired)
            {
                for (var index = 0; index < changedIndex; index++)
                {
                    var prerequisite = HiddenSkillsEditorRows[index];
                    if (!prerequisite.IsOwnedDesired)
                    {
                        prerequisite.IsOwnedDesired = true;
                        adjustedSkills.Add(prerequisite.Name);
                    }
                }
            }
            else
            {
                for (var index = changedIndex + 1; index < HiddenSkillsEditorRows.Count; index++)
                {
                    var dependent = HiddenSkillsEditorRows[index];
                    if (dependent.IsOwnedDesired)
                    {
                        dependent.IsOwnedDesired = false;
                        adjustedSkills.Add(dependent.Name);
                    }
                }
            }
        }
        finally
        {
            _suppressHiddenSkillDependencyEnforcement = previousSuppressHiddenSkillDependencyEnforcement;
        }

        if (adjustedSkills.Count == 0)
        {
            return;
        }

        var message = changedSkill.IsOwnedDesired
            ? $"Auto-enabled prerequisites for {changedSkill.Name}: {string.Join(", ", adjustedSkills)}."
            : $"Auto-disabled dependent skills after {changedSkill.Name}: {string.Join(", ", adjustedSkills)}.";
        ReportHiddenSkillDependencyMessage(message, "desired-state-change");
    }

    private bool NormalizeHiddenSkillDesiredProgression(string reason, out string message)
    {
        message = string.Empty;
        var highestDesiredIndex = -1;
        for (var index = 0; index < HiddenSkillsEditorRows.Count; index++)
        {
            if (HiddenSkillsEditorRows[index].IsOwnedDesired)
            {
                highestDesiredIndex = index;
            }
        }

        var autoEnabled = new List<string>();
        var autoDisabled = new List<string>();
        var previousSuppressHiddenSkillDependencyEnforcement = _suppressHiddenSkillDependencyEnforcement;
        _suppressHiddenSkillDependencyEnforcement = true;
        try
        {
            for (var index = 0; index < HiddenSkillsEditorRows.Count; index++)
            {
                var skill = HiddenSkillsEditorRows[index];
                var desiredState = index <= highestDesiredIndex;
                if (skill.IsOwnedDesired == desiredState)
                {
                    continue;
                }

                skill.IsOwnedDesired = desiredState;
                if (desiredState)
                {
                    autoEnabled.Add(skill.Name);
                }
                else
                {
                    autoDisabled.Add(skill.Name);
                }
            }
        }
        finally
        {
            _suppressHiddenSkillDependencyEnforcement = previousSuppressHiddenSkillDependencyEnforcement;
        }

        if (autoEnabled.Count == 0 && autoDisabled.Count == 0)
        {
            return false;
        }

        var parts = new List<string>();
        if (autoEnabled.Count > 0)
        {
            parts.Add($"auto-enabled prerequisites: {string.Join(", ", autoEnabled)}");
        }

        if (autoDisabled.Count > 0)
        {
            parts.Add($"auto-disabled dependents: {string.Join(", ", autoDisabled)}");
        }

        message = "Hidden Skills progression normalized before apply: " + string.Join("; ", parts) + ".";
        ReportHiddenSkillDependencyMessage(message, reason);
        return true;
    }

    private void ReportHiddenSkillDependencyMessage(string message, string reason)
    {
        HiddenSkillsEditorStatusText.Text = message;
        SetStatus(message, StatusKind.Neutral);
        AppendHiddenSkillsEditorStatusDiagnostic($"dependency reason={reason} message=\"{message}\"");
    }

    private static void SetHiddenSkillBitInBytes(byte[] bytes, HiddenSkillViewModel skill, bool isSet)
    {
        var byteIndex = (int)(skill.OffsetValue - HiddenSkillsDefinitions.FirstOffset);
        if (byteIndex < 0 || byteIndex >= bytes.Length)
        {
            return;
        }

        var mask = (byte)(1 << skill.Bit);
        bytes[byteIndex] = isSet
            ? (byte)(bytes[byteIndex] | mask)
            : (byte)(bytes[byteIndex] & ~mask);
    }

    private void SetHiddenSkillsRowWriteStatuses(byte[] desiredBytes, string writeStatus, string verificationStatus)
    {
        foreach (var skill in HiddenSkillsEditorRows)
        {
            var byteIndex = (int)(skill.OffsetValue - HiddenSkillsDefinitions.FirstOffset);
            if (byteIndex < 0 || byteIndex >= desiredBytes.Length)
            {
                continue;
            }

            var desiredState = (desiredBytes[byteIndex] & (1 << skill.Bit)) != 0;
            skill.LastWriteStatus = $"{writeStatus}: desired {(desiredState ? "owned" : "not owned")}";
            skill.LastVerificationStatus = verificationStatus;
        }
    }

    private static int CountGoldenBugs(uint flags)
    {
        var usedBits = flags >> 8;
        var count = 0;

        while (usedBits != 0)
        {
            count += (int)(usedBits & 1);
            usedBits >>= 1;
        }

        return Math.Min(count, 24);
    }

    private static int GetGoldenBugReferenceIndex(string bugName)
    {
        for (var index = 0; index < GoldenBugCandidateNames.Count; index++)
        {
            if (string.Equals(GoldenBugCandidateNames[index], bugName, StringComparison.Ordinal))
            {
                return index + 1;
            }
        }

        return 0;
    }

    private static void SetGoldenBugBitInBytes(byte[] bytes, GoldenBugBitViewModel bit, bool isSet)
    {
        var byteIndex = (int)(bit.OffsetValue - GoldenBugsDefinitions.FirstOffset);
        if (byteIndex < 0 || byteIndex >= bytes.Length)
        {
            return;
        }

        var mask = (byte)(1 << bit.Bit);
        bytes[byteIndex] = isSet
            ? (byte)(bytes[byteIndex] | mask)
            : (byte)(bytes[byteIndex] & ~mask);
    }

    private static int CountSetBits(uint value)
    {
        var count = 0;
        while (value != 0)
        {
            count += (int)(value & 1);
            value >>= 1;
        }

        return count;
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

        foreach (var bombSlot in BombSlots)
        {
            bombSlot.MarkNotRead();
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

        MarkHiddenSkillsNotRead();
        ApplyProgressionState(ProgressionStateService.CreateUnavailable());

        UpdateDerivedDisplays();
        MarkGoldenBugsResearchNotRead();
        SetStatus("Player data could not be read. Load into gameplay and rescan.", StatusKind.Warning);
    }

    private void Detach(bool clearStatus)
    {
        _refreshTimer.Stop();
        StopHiddenSkillsLiveWatch("Stopped Hidden Skills live watch because the trainer detached.");
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

        foreach (var bombSlot in BombSlots)
        {
            bombSlot.MarkNotRead();
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

        MarkHiddenSkillsNotRead();
        ApplyProgressionState(ProgressionStateService.CreateUnavailable());

        GoldenBugsCountText.Text = "Not read";
        MarkGoldenBugsResearchNotRead();
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
            SetBrush("AccentBrush", 0x4C, 0xA3, 0xF5);
            SetBrush("ControlHoverBackgroundBrush", 0x3D, 0x42, 0x49);
            SetBrush("ControlFocusedBorderBrush", 0x63, 0xB3, 0xFF);
            SetBrush("DisabledControlBackgroundBrush", 0x27, 0x29, 0x2D);
            SetBrush("CheckBoxUncheckedBorderBrush", 0x68, 0x6D, 0x76);
            SetBrush("CheckBoxCheckedBackgroundBrush", 0x4C, 0xA3, 0xF5);
            SetBrush("CheckBoxCheckBrush", 0x10, 0x13, 0x18);
            SetBrush("ComboBoxItemHoverBackgroundBrush", 0x42, 0x4B, 0x57);
            SetBrush("ComboBoxItemSelectedBackgroundBrush", 0x3C, 0x62, 0x82);
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
            SetBrush("AccentBrush", 0x1F, 0x6F, 0xB2);
            SetBrush("ControlHoverBackgroundBrush", 0xF2, 0xF7, 0xFC);
            SetBrush("ControlFocusedBorderBrush", 0x1F, 0x6F, 0xB2);
            SetBrush("DisabledControlBackgroundBrush", 0xE8, 0xE8, 0xE8);
            SetBrush("CheckBoxUncheckedBorderBrush", 0x8A, 0x8A, 0x8A);
            SetBrush("CheckBoxCheckedBackgroundBrush", 0x1F, 0x6F, 0xB2);
            SetBrush("CheckBoxCheckBrush", 0xFF, 0xFF, 0xFF);
            SetBrush("ComboBoxItemHoverBackgroundBrush", 0xDD, 0xEE, 0xFF);
            SetBrush("ComboBoxItemSelectedBackgroundBrush", 0xC8, 0xE2, 0xFA);
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
        _hiddenSkillsLiveWatchTimer.Stop();
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

    private sealed record GoldenBugsResearchExport(
        DateTimeOffset Timestamp,
        uint StartOffset,
        int Length,
        DateTimeOffset? BeforeCapturedAt,
        DateTimeOffset? AfterCapturedAt,
        int ChangedByteCount,
        int ChangedBitCount,
        IReadOnlyList<string> CandidateBugReference,
        IReadOnlyList<GoldenBugsResearchExportRow> Rows);

    private sealed record GoldenBugsResearchExportRow(
        string Offset,
        byte BeforeValue,
        byte AfterValue,
        string BeforeBinary,
        string AfterBinary,
        string ChangedBits,
        int ByteIndex,
        int BitIndex,
        int CandidateBugIndex,
        string CandidateBugName);

    private sealed record QuestItemsCaptureDocument(
        DateTimeOffset Timestamp,
        string Label,
        uint StartOffset,
        int Length,
        IReadOnlyList<string> RawBytes,
        string Notes,
        string CaptureType,
        string AppVersion);

    private sealed record QuestItemsLoadedCapture(
        string Path,
        QuestItemsCaptureDocument Document,
        byte[] Bytes);

    private sealed record QuestItemsResearchExport(
        DateTimeOffset Timestamp,
        string CaptureALabel,
        string CaptureBLabel,
        uint CaptureAStartOffset,
        int CaptureALength,
        DateTimeOffset? CaptureACapturedAt,
        uint CaptureBStartOffset,
        int CaptureBLength,
        DateTimeOffset? CaptureBCapturedAt,
        string RangeWarning,
        int ChangedByteCount,
        int ChangedBitCount,
        IReadOnlyList<QuestItemsResearchExportRow> Rows);

    private sealed record QuestItemsResearchExportRow(
        string Offset,
        byte BeforeValue,
        byte AfterValue,
        string BeforeBinary,
        string AfterBinary,
        string ChangedBits,
        int ChangedBitCount,
        bool Changed,
        int CandidateScore,
        string CandidateGroup);

    private sealed record QuestItemsCandidateRankingExport(
        DateTimeOffset Timestamp,
        string CaptureALabel,
        string CaptureBLabel,
        IReadOnlyList<QuestItemsCandidateRankingExportRow> Rows);

    private sealed record QuestItemsCandidateRankingExportRow(
        string Offset,
        string CaptureAValue,
        string CaptureBValue,
        string ChangedBits,
        int ChangedBitCount,
        int CandidateScore,
        string Confidence,
        string GroupName,
        string Reasons);

    private sealed record QuestItemsCandidateGroupsExport(
        DateTimeOffset Timestamp,
        string CaptureALabel,
        string CaptureBLabel,
        IReadOnlyList<QuestItemsCandidateGroupsExportRow> Groups);

    private sealed record QuestItemsCandidateGroupsExportRow(
        string GroupName,
        string OffsetRange,
        int Count,
        int HighestCandidateScore,
        string Reasons);

    private sealed record QuestItemsMultiCaptureAnalysisExport(
        DateTimeOffset Timestamp,
        IReadOnlyList<QuestItemsMultiCaptureExportCapture> Captures,
        IReadOnlyList<QuestItemsMultiCaptureAnalysisExportRow> Rows);

    private sealed record QuestItemsMultiCaptureExportCapture(
        string Label,
        string CaptureType,
        DateTimeOffset Timestamp,
        uint StartOffset,
        int Length,
        string FileName);

    private sealed record QuestItemsMultiCaptureAnalysisExportRow(
        string Offset,
        string ValueProgression,
        int AppearanceCount,
        int CandidateScore,
        string Confidence,
        string Reasons);

    private sealed record SupportSnapshotDocument(
        string ApplicationVersion,
        DateTimeOffset Timestamp,
        string ProcessName,
        bool IsAttached,
        string Pid,
        string PlayerBaseAddress,
        string ConnectionStatus,
        bool HasPlayerData,
        bool InventoryInitialized,
        bool EquipmentInitialized,
        string OwnershipEditsState,
        bool DarkModeEnabled,
        string AobPattern,
        IReadOnlyList<SupportSnapshotCapacity> Capacities,
        IReadOnlyList<SupportSnapshotValue> Values,
        IReadOnlyList<string> IncludedLogFiles,
        IReadOnlyList<string> SkippedLogFiles);

    private sealed record SupportSnapshotCapacity(
        string Name,
        string CurrentStoredValue,
        string SelectedCapacity);

    private sealed record SupportSnapshotValue(
        string Name,
        string Offset,
        string CurrentValue,
        string TargetValue,
        bool Locked);

    private sealed record HiddenSkillsCaptureDocument(
        DateTimeOffset Timestamp,
        uint StartOffset,
        int Length,
        IReadOnlyList<string> RawBytes,
        string Label)
    {
        public string LabelOrDefault => string.IsNullOrWhiteSpace(Label) ? "Unlabeled capture" : Label;
    }

    private sealed record HiddenSkillsResearchExport(
        DateTimeOffset Timestamp,
        uint StartOffset,
        int Length,
        DateTimeOffset? BeforeCapturedAt,
        DateTimeOffset? AfterCapturedAt,
        bool TreatAfterAsPersisted,
        IReadOnlyList<string> KnownSkills,
        IReadOnlyList<HiddenSkillsResearchExportRow> Rows,
        IReadOnlyList<HiddenSkillsCandidateGroupExport> CandidateGroups);

    private sealed record HiddenSkillsResearchExportRow(
        string Offset,
        byte BeforeValue,
        byte AfterValue,
        string BeforeBinary,
        string AfterBinary,
        string ChangedBits,
        int ChangedBitCount,
        bool Changed,
        bool SingleBitChange,
        bool PersistedChange,
        bool ClusteredChange,
        int CandidateScore,
        string Highlights,
        bool Pinned,
        string GroupName);

    private sealed record HiddenSkillsCandidateGroupsExport(
        DateTimeOffset Timestamp,
        uint StartOffset,
        int Length,
        IReadOnlyList<HiddenSkillsCandidateGroupExport> Groups);

    private sealed record HiddenSkillsCandidateGroupExport(
        string Name,
        string Offsets,
        int Count,
        int HighestScore,
        string Reasons,
        IReadOnlyList<HiddenSkillsCandidateGroupRowExport> Rows);

    private sealed record HiddenSkillsCandidateGroupRowExport(
        string Offset,
        byte BeforeValue,
        byte AfterValue,
        string ChangedBits,
        int CandidateScore,
        string Highlights,
        bool Pinned);

    private sealed record HiddenSkillsLoadedCapture(
        string SlotName,
        HiddenSkillsCaptureDocument Document,
        byte[] Bytes,
        string FilePath);

    private sealed record HiddenSkillsMultiCaptureCandidateAnalysis(
        uint OffsetValue,
        string Kind,
        string Bit,
        string Values,
        int Score,
        bool IsMonotonic,
        bool OnlyIncreases,
        bool ProgressionMatch,
        string Notes);

    private sealed record HiddenSkillsMultiCaptureExport(
        DateTimeOffset Timestamp,
        uint StartOffset,
        int Length,
        IReadOnlyList<int> SkillCounts,
        IReadOnlyList<HiddenSkillsMultiCaptureExportCapture> Captures,
        IReadOnlyList<HiddenSkillsMultiCaptureExportRow> Candidates);

    private sealed record HiddenSkillsMultiCaptureExportCapture(
        string SlotName,
        string Label,
        DateTimeOffset Timestamp,
        uint StartOffset,
        int Length,
        string FilePath);

    private sealed record HiddenSkillsMultiCaptureExportRow(
        int Rank,
        string Offset,
        string Kind,
        string Bit,
        string Values,
        string SkillCounts,
        int Score,
        bool IsMonotonic,
        bool OnlyIncreases,
        bool ProgressionMatch,
        string Flags,
        string Notes);

    private sealed record HiddenSkillsRegionChangedByte(
        uint OffsetValue,
        byte BeforeValue,
        byte AfterValue,
        int ChangedBitCount,
        int AbsoluteDelta);

    private sealed record HiddenSkillsRegionAnalysisCandidate(
        uint RegionStartValue,
        uint RegionEndValue,
        int ChangedBytes,
        int ChangedBits,
        double Density,
        int LargestChange,
        int CandidateScore,
        bool IsSingleBitRegion);

    private sealed record HiddenSkillsRegionAnalysisExport(
        DateTimeOffset Timestamp,
        uint StartOffset,
        int Length,
        HiddenSkillsRegionAnalysisCaptureExport? CaptureA,
        HiddenSkillsRegionAnalysisCaptureExport? CaptureB,
        IReadOnlyList<HiddenSkillsRegionAnalysisExportRow> Rows);

    private sealed record HiddenSkillsRegionAnalysisCaptureExport(
        string SlotName,
        string Label,
        DateTimeOffset Timestamp,
        uint StartOffset,
        int Length,
        string FilePath);

    private sealed record HiddenSkillsRegionAnalysisExportRow(
        int Rank,
        string Region,
        int ChangedBytes,
        int ChangedBits,
        double Density,
        int LargestChange,
        int CandidateScore,
        bool SingleBitRegion,
        string Highlights);

    private sealed record HiddenSkillsLiveWatchExport(
        DateTimeOffset Timestamp,
        DateTimeOffset? StartedAt,
        uint StartOffset,
        int Length,
        IReadOnlyList<HiddenSkillsLiveWatchExportRow> Rows,
        IReadOnlyList<HiddenSkillsEventMarkerExport> Events);

    private sealed record HiddenSkillsLiveWatchExportRow(
        DateTimeOffset Timestamp,
        string Offset,
        byte BeforeValue,
        byte AfterValue,
        string BeforeBinary,
        string AfterBinary,
        string ChangedBits,
        int ChangedBitCount,
        int ChangeCount,
        bool SingleBitChange,
        bool RepeatedChange,
        bool MonotonicChange,
        string Highlights);

    private sealed record HiddenSkillsEventMarkerExport(
        DateTimeOffset Timestamp,
        string Label);

    private sealed record GoldenBugsBitfieldReport(
        DateTimeOffset Timestamp,
        DateTimeOffset? RestoreSnapshotCapturedAt,
        IReadOnlyList<string> RestoreSnapshotBytes,
        IReadOnlyList<GoldenBugsBitfieldReportRow> Rows);

    private sealed record GoldenBugsBitfieldReportRow(
        string Offset,
        int Bit,
        string CurrentState,
        bool Desired,
        string ConfirmedBugName,
        string MappingStatus,
        string AssignedBugName,
        string Notes,
        string LastWriteStatus);

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
