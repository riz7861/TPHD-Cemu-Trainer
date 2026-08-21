namespace TphdCemuTrainer.Research;

public sealed record InventoryMappingExport(
    DateTimeOffset Timestamp,
    IReadOnlyList<InventoryMappingExportRow> Rows);

public sealed record InventoryMappingExportRow(
    int SlotNumber,
    string Offset,
    byte RawValue,
    string DecodedItem,
    string DetectedVisualGroup,
    string ResearchGroup,
    string RowNote,
    string ColumnNote,
    string Notes);

public sealed record GoldenBugsResearchExport(
    DateTimeOffset Timestamp,
    uint StartOffset,
    int Length,
    DateTimeOffset? BeforeCapturedAt,
    DateTimeOffset? AfterCapturedAt,
    int ChangedByteCount,
    int ChangedBitCount,
    IReadOnlyList<string> CandidateBugReference,
    IReadOnlyList<GoldenBugsResearchExportRow> Rows);

public sealed record GoldenBugsResearchExportRow(
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

public sealed record QuestItemsCaptureDocument(
    DateTimeOffset Timestamp,
    string Label,
    uint StartOffset,
    int Length,
    IReadOnlyList<string> RawBytes,
    string Notes,
    string CaptureType,
    string AppVersion);

public sealed record QuestItemsLoadedCapture(
    string Path,
    QuestItemsCaptureDocument Document,
    byte[] Bytes);

public sealed record QuestItemsResearchExport(
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

public sealed record QuestItemsResearchExportRow(
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

public sealed record QuestItemsCandidateRankingExport(
    DateTimeOffset Timestamp,
    string CaptureALabel,
    string CaptureBLabel,
    IReadOnlyList<QuestItemsCandidateRankingExportRow> Rows);

public sealed record QuestItemsCandidateRankingExportRow(
    string Offset,
    string CaptureAValue,
    string CaptureBValue,
    string ChangedBits,
    int ChangedBitCount,
    int CandidateScore,
    string Confidence,
    string GroupName,
    string Reasons);

public sealed record QuestItemsCandidateGroupsExport(
    DateTimeOffset Timestamp,
    string CaptureALabel,
    string CaptureBLabel,
    IReadOnlyList<QuestItemsCandidateGroupsExportRow> Groups);

public sealed record QuestItemsCandidateGroupsExportRow(
    string GroupName,
    string OffsetRange,
    int Count,
    int HighestCandidateScore,
    string Reasons);

public sealed record QuestItemsMultiCaptureAnalysisExport(
    DateTimeOffset Timestamp,
    IReadOnlyList<QuestItemsMultiCaptureExportCapture> Captures,
    IReadOnlyList<QuestItemsMultiCaptureAnalysisExportRow> Rows);

public sealed record QuestItemsMultiCaptureExportCapture(
    string Label,
    string CaptureType,
    DateTimeOffset Timestamp,
    uint StartOffset,
    int Length,
    string FileName);

public sealed record QuestItemsMultiCaptureAnalysisExportRow(
    string Offset,
    string ValueProgression,
    int AppearanceCount,
    int CandidateScore,
    string Confidence,
    string Reasons);

public sealed record SupportSnapshotDocument(
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

public sealed record SupportSnapshotCapacity(
    string Name,
    string CurrentStoredValue,
    string SelectedCapacity);

public sealed record SupportSnapshotValue(
    string Name,
    string Offset,
    string CurrentValue,
    string TargetValue,
    bool Locked);

public sealed record HiddenSkillsCaptureDocument(
    DateTimeOffset Timestamp,
    uint StartOffset,
    int Length,
    IReadOnlyList<string> RawBytes,
    string Label)
{
    public string LabelOrDefault => string.IsNullOrWhiteSpace(Label) ? "Unlabeled capture" : Label;
}

public sealed record HiddenSkillsResearchExport(
    DateTimeOffset Timestamp,
    uint StartOffset,
    int Length,
    DateTimeOffset? BeforeCapturedAt,
    DateTimeOffset? AfterCapturedAt,
    bool TreatAfterAsPersisted,
    IReadOnlyList<string> KnownSkills,
    IReadOnlyList<HiddenSkillsResearchExportRow> Rows,
    IReadOnlyList<HiddenSkillsCandidateGroupExport> CandidateGroups);

public sealed record HiddenSkillsResearchExportRow(
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

public sealed record HiddenSkillsCandidateGroupsExport(
    DateTimeOffset Timestamp,
    uint StartOffset,
    int Length,
    IReadOnlyList<HiddenSkillsCandidateGroupExport> Groups);

public sealed record HiddenSkillsCandidateGroupExport(
    string Name,
    string Offsets,
    int Count,
    int HighestScore,
    string Reasons,
    IReadOnlyList<HiddenSkillsCandidateGroupRowExport> Rows);

public sealed record HiddenSkillsCandidateGroupRowExport(
    string Offset,
    byte BeforeValue,
    byte AfterValue,
    string ChangedBits,
    int CandidateScore,
    string Highlights,
    bool Pinned);

public sealed record HiddenSkillsLoadedCapture(
    string SlotName,
    HiddenSkillsCaptureDocument Document,
    byte[] Bytes,
    string FilePath);

public sealed record HiddenSkillsMultiCaptureCandidateAnalysis(
    uint OffsetValue,
    string Kind,
    string Bit,
    string Values,
    int Score,
    bool IsMonotonic,
    bool OnlyIncreases,
    bool ProgressionMatch,
    string Notes);

public sealed record HiddenSkillsMultiCaptureExport(
    DateTimeOffset Timestamp,
    uint StartOffset,
    int Length,
    IReadOnlyList<int> SkillCounts,
    IReadOnlyList<HiddenSkillsMultiCaptureExportCapture> Captures,
    IReadOnlyList<HiddenSkillsMultiCaptureExportRow> Candidates);

public sealed record HiddenSkillsMultiCaptureExportCapture(
    string SlotName,
    string Label,
    DateTimeOffset Timestamp,
    uint StartOffset,
    int Length,
    string FilePath);

public sealed record HiddenSkillsMultiCaptureExportRow(
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

public sealed record HiddenSkillsRegionChangedByte(
    uint OffsetValue,
    byte BeforeValue,
    byte AfterValue,
    int ChangedBitCount,
    int AbsoluteDelta);

public sealed record HiddenSkillsRegionAnalysisCandidate(
    uint RegionStartValue,
    uint RegionEndValue,
    int ChangedBytes,
    int ChangedBits,
    double Density,
    int LargestChange,
    int CandidateScore,
    bool IsSingleBitRegion);

public sealed record HiddenSkillsRegionAnalysisExport(
    DateTimeOffset Timestamp,
    uint StartOffset,
    int Length,
    HiddenSkillsRegionAnalysisCaptureExport? CaptureA,
    HiddenSkillsRegionAnalysisCaptureExport? CaptureB,
    IReadOnlyList<HiddenSkillsRegionAnalysisExportRow> Rows);

public sealed record HiddenSkillsRegionAnalysisCaptureExport(
    string SlotName,
    string Label,
    DateTimeOffset Timestamp,
    uint StartOffset,
    int Length,
    string FilePath);

public sealed record HiddenSkillsRegionAnalysisExportRow(
    int Rank,
    string Region,
    int ChangedBytes,
    int ChangedBits,
    double Density,
    int LargestChange,
    int CandidateScore,
    bool SingleBitRegion,
    string Highlights);

public sealed record MemoryRangeReadResult(
    bool Success,
    byte[] Bytes,
    string Error);

public sealed record KnownResearchRegionHint(
    uint StartOffset,
    uint EndOffset,
    string Category,
    string Description);

public sealed class LiveCaptureTrackedAddress
{
    public LiveCaptureTrackedAddress(
        uint offsetValue,
        byte initialValue,
        byte previousValue,
        byte currentValue,
        DateTimeOffset timestamp)
    {
        OffsetValue = offsetValue;
        InitialValue = initialValue;
        PreviousValue = previousValue;
        CurrentValue = currentValue;
        FirstSeen = timestamp;
        LastSeen = timestamp;
        ChangeCount = 1;
        Timeline.Add(new LiveCaptureTimelinePoint(timestamp, previousValue, currentValue));
    }

    public uint OffsetValue { get; }

    public byte InitialValue { get; }

    public byte PreviousValue { get; set; }

    public byte CurrentValue { get; set; }

    public int ChangeCount { get; private set; }

    public DateTimeOffset FirstSeen { get; }

    public DateTimeOffset LastSeen { get; set; }

    public string PersistedStatus { get; set; } = "Unknown";

    public List<LiveCaptureTimelinePoint> Timeline { get; } = [];

    public void RecordChange(byte previousValue, byte currentValue, DateTimeOffset timestamp)
    {
        PreviousValue = previousValue;
        CurrentValue = currentValue;
        LastSeen = timestamp;
        ChangeCount++;
        PersistedStatus = "Unknown";
        Timeline.Add(new LiveCaptureTimelinePoint(timestamp, previousValue, currentValue));
        while (Timeline.Count > 24)
        {
            Timeline.RemoveAt(0);
        }
    }
}

public sealed record LiveCaptureTimelinePoint(
    DateTimeOffset Timestamp,
    byte PreviousValue,
    byte CurrentValue);

public sealed record LiveCaptureSessionExport(
    DateTimeOffset Timestamp,
    string Label,
    DateTimeOffset? StartedAt,
    uint StartOffset,
    int Length,
    int SamplingRateMs,
    IReadOnlyList<LiveCaptureTrackedAddressExport> TrackedAddresses,
    IReadOnlyList<string> AppliedFilters,
    IReadOnlyList<KnownResearchRegionHint> KnownRegionHints);

public sealed record LiveCaptureTrackedAddressExport(
    string Offset,
    byte InitialValue,
    byte PreviousValue,
    byte CurrentValue,
    int ChangeCount,
    DateTimeOffset FirstSeen,
    DateTimeOffset LastSeen,
    string PersistedStatus,
    int CandidateScore,
    string Confidence,
    IReadOnlyList<string> Reasons,
    IReadOnlyList<LiveCaptureTimelinePointExport> Timeline);

public sealed record LiveCaptureTimelinePointExport(
    DateTimeOffset Timestamp,
    byte PreviousValue,
    byte CurrentValue);

public sealed record ResearchWorkspaceSnapshotExport(
    DateTimeOffset Timestamp,
    string Label,
    uint StartOffset,
    int Length,
    DateTimeOffset? CaptureACapturedAt,
    DateTimeOffset? CaptureBCapturedAt,
    int ChangedByteCount,
    int ChangedBitCount,
    IReadOnlyList<QuestItemsResearchExportRow> Rows);

public sealed record HiddenSkillsLiveWatchExport(
    DateTimeOffset Timestamp,
    DateTimeOffset? StartedAt,
    uint StartOffset,
    int Length,
    IReadOnlyList<HiddenSkillsLiveWatchExportRow> Rows,
    IReadOnlyList<HiddenSkillsEventMarkerExport> Events);

public sealed record HiddenSkillsLiveWatchExportRow(
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

public sealed record HiddenSkillsEventMarkerExport(
    DateTimeOffset Timestamp,
    string Label);

public sealed record GoldenBugsBitfieldReport(
    DateTimeOffset Timestamp,
    DateTimeOffset? RestoreSnapshotCapturedAt,
    IReadOnlyList<string> RestoreSnapshotBytes,
    IReadOnlyList<GoldenBugsBitfieldReportRow> Rows);

public sealed record GoldenBugsBitfieldReportRow(
    string Offset,
    int Bit,
    string CurrentState,
    bool Desired,
    string ConfirmedBugName,
    string MappingStatus,
    string AssignedBugName,
    string Notes,
    string LastWriteStatus);

public sealed class OwnershipCorrelationAccumulator
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
