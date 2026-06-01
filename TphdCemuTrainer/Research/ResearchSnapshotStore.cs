using System.IO;
using System.Text.Json;

namespace TphdCemuTrainer.Research;

public static class ResearchSnapshotStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static string SnapshotDirectory =>
        Path.Combine(AppContext.BaseDirectory, "logs", "snapshots");

    public static string ExportDirectory =>
        Path.Combine(AppContext.BaseDirectory, "logs", "research");

    public static string SaveSnapshot(ResearchSnapshotDocument snapshot)
    {
        Directory.CreateDirectory(SnapshotDirectory);

        var fileName = $"{snapshot.Timestamp:yyyyMMdd_HHmmss}_{SanitizeFileName(snapshot.Name)}.json";
        var path = Path.Combine(SnapshotDirectory, fileName);
        File.WriteAllText(path, JsonSerializer.Serialize(snapshot, JsonOptions));
        return path;
    }

    public static IReadOnlyList<(string Path, ResearchSnapshotDocument Snapshot)> LoadSnapshots()
    {
        if (!Directory.Exists(SnapshotDirectory))
        {
            return [];
        }

        var snapshots = new List<(string Path, ResearchSnapshotDocument Snapshot)>();
        foreach (var path in Directory.EnumerateFiles(SnapshotDirectory, "*.json"))
        {
            try
            {
                var snapshot = JsonSerializer.Deserialize<ResearchSnapshotDocument>(File.ReadAllText(path), JsonOptions);
                if (snapshot is not null)
                {
                    snapshots.Add((path, snapshot));
                }
            }
            catch
            {
                // Ignore malformed research files so the browser remains usable.
            }
        }

        return snapshots
            .OrderByDescending(item => item.Snapshot.Timestamp)
            .ToList();
    }

    public static (string JsonPath, string CsvPath) ExportComparison(ResearchComparisonExport comparison)
    {
        Directory.CreateDirectory(ExportDirectory);

        var baseName =
            $"{comparison.Timestamp:yyyyMMdd_HHmmss}_{SanitizeFileName(comparison.SnapshotAName)}_vs_{SanitizeFileName(comparison.SnapshotBName)}";
        var jsonPath = Path.Combine(ExportDirectory, baseName + ".json");
        var csvPath = Path.Combine(ExportDirectory, baseName + ".csv");

        File.WriteAllText(jsonPath, JsonSerializer.Serialize(comparison, JsonOptions));
        File.WriteAllLines(csvPath, CreateCsvLines(comparison));

        return (jsonPath, csvPath);
    }

    public static (string JsonPath, string CsvPath) ExportOwnershipDiscovery(OwnershipDiscoveryExport export)
    {
        Directory.CreateDirectory(ExportDirectory);

        var baseName =
            $"{export.Timestamp:yyyyMMdd_HHmmss}_ownership-discovery_{SanitizeFileName(export.SnapshotAName)}_vs_{SanitizeFileName(export.SnapshotBName)}";
        var jsonPath = Path.Combine(ExportDirectory, baseName + ".json");
        var csvPath = Path.Combine(ExportDirectory, baseName + ".csv");

        File.WriteAllText(jsonPath, JsonSerializer.Serialize(export, JsonOptions));
        File.WriteAllLines(csvPath, CreateOwnershipDiscoveryCsvLines(export));

        return (jsonPath, csvPath);
    }

    private static IEnumerable<string> CreateCsvLines(ResearchComparisonExport comparison)
    {
        yield return "Offset,Snapshot A,Snapshot B,Snapshot A Decode,Snapshot B Decode,Difference,Changed";

        foreach (var row in comparison.Rows)
        {
            yield return string.Join(
                ",",
                Csv(row.Offset),
                Csv(row.SnapshotAValue?.ToString() ?? string.Empty),
                Csv(row.SnapshotBValue?.ToString() ?? string.Empty),
                Csv(row.SnapshotADecode),
                Csv(row.SnapshotBDecode),
                Csv(row.Difference),
                Csv(row.Changed.ToString()));
        }
    }

    private static IEnumerable<string> CreateOwnershipDiscoveryCsvLines(OwnershipDiscoveryExport export)
    {
        yield return "Offset,Before Value,After Value,Changed,Persisted After Reload,Outside Visible Inventory Slots,Score,Strong Candidate,Potential Meaning";

        foreach (var row in export.Rows)
        {
            yield return string.Join(
                ",",
                Csv(row.Offset),
                Csv(row.BeforeValue?.ToString() ?? string.Empty),
                Csv(row.AfterValue?.ToString() ?? string.Empty),
                Csv(row.Changed.ToString()),
                Csv(row.PersistedAfterReload.ToString()),
                Csv(row.OutsideVisibleInventorySlots.ToString()),
                Csv(row.Score.ToString()),
                Csv(row.StrongCandidate.ToString()),
                Csv(row.PotentialMeaning));
        }
    }

    private static string Csv(string value)
    {
        return "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }

    private static string SanitizeFileName(string value)
    {
        var fileName = string.IsNullOrWhiteSpace(value) ? "snapshot" : value.Trim();
        foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(invalidCharacter, '-');
        }

        return fileName.Replace(' ', '-');
    }
}
