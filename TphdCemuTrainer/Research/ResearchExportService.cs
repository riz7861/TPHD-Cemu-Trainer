using System.Globalization;
using System.IO;
using System.Text.Json;

namespace TphdCemuTrainer.Research;

public static class ResearchExportService
{
    public static JsonSerializerOptions JsonOptions { get; } = new()
    {
        WriteIndented = true
    };

    public static string SerializeJson<T>(T value) =>
        JsonSerializer.Serialize(value, JsonOptions);

    public static T? DeserializeJson<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, JsonOptions);

    public static void WriteJson<T>(string path, T value)
    {
        File.WriteAllText(path, SerializeJson(value));
    }

    public static void WriteCsvLines(string path, IEnumerable<string> lines)
    {
        File.WriteAllLines(path, lines);
    }

    public static IEnumerable<string> CreateInventoryMappingCsvLines(IEnumerable<InventoryMappingExportRow> rows)
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

    public static IEnumerable<string> CreateGoldenBugsResearchCsvLines(IEnumerable<GoldenBugsResearchExportRow> rows)
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

    public static IEnumerable<string> CreateQuestItemsResearchCsvLines(QuestItemsResearchExport export)
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

    public static IEnumerable<string> CreateQuestItemsCandidateRankingCsvLines(QuestItemsCandidateRankingExport export)
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

    public static IEnumerable<string> CreateQuestItemsCandidateGroupsCsvLines(QuestItemsCandidateGroupsExport export)
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

    public static IEnumerable<string> CreateQuestItemsMultiCaptureAnalysisCsvLines(
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

    public static IEnumerable<string> CreateHiddenSkillsResearchCsvLines(IEnumerable<HiddenSkillsResearchExportRow> rows)
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

    public static IEnumerable<string> CreateHiddenSkillsCandidateGroupsCsvLines(IEnumerable<HiddenSkillsCandidateGroupExport> groups)
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

    public static IEnumerable<string> CreateHiddenSkillsMultiCaptureCsvLines(IEnumerable<HiddenSkillsMultiCaptureExportRow> rows)
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

    public static IEnumerable<string> CreateHiddenSkillsRegionAnalysisCsvLines(IEnumerable<HiddenSkillsRegionAnalysisExportRow> rows)
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

    public static IEnumerable<string> CreateHiddenSkillsLiveWatchCsvLines(HiddenSkillsLiveWatchExport export)
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

    public static IEnumerable<string> CreateGoldenBugsBitfieldReportCsvLines(IEnumerable<GoldenBugsBitfieldReportRow> rows)
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

    public static IEnumerable<string> CreateResearchWorkspaceSnapshotCsvLines(ResearchWorkspaceSnapshotExport export)
    {
        yield return "Field,Value";
        yield return string.Join(",", Csv("Label"), Csv(export.Label));
        yield return string.Join(",", Csv("Start Offset"), Csv($"0x{export.StartOffset:X}"));
        yield return string.Join(",", Csv("Length"), Csv($"0x{export.Length:X}"));
        yield return string.Join(",", Csv("Changed Bytes"), Csv(export.ChangedByteCount.ToString(CultureInfo.InvariantCulture)));
        yield return string.Join(",", Csv("Changed Bits"), Csv(export.ChangedBitCount.ToString(CultureInfo.InvariantCulture)));
        yield return string.Empty;
        yield return "Offset,Before Value,After Value,Before Binary,After Binary,Changed Bits,Changed Bit Count,Candidate Score,Candidate Group";
        foreach (var row in export.Rows)
        {
            yield return string.Join(
                ",",
                Csv(row.Offset),
                Csv($"0x{row.BeforeValue:X2}"),
                Csv($"0x{row.AfterValue:X2}"),
                Csv(row.BeforeBinary),
                Csv(row.AfterBinary),
                Csv(row.ChangedBits),
                Csv(row.ChangedBitCount.ToString(CultureInfo.InvariantCulture)),
                Csv(row.CandidateScore.ToString(CultureInfo.InvariantCulture)),
                Csv(row.CandidateGroup));
        }
    }

    public static string Csv(string value)
    {
        return "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }

    public static string SanitizeFileName(string value)
    {
        var fileName = string.IsNullOrWhiteSpace(value) ? "capture" : value.Trim();
        foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(invalidCharacter, '-');
        }

        return fileName.Replace(' ', '-');
    }
}
