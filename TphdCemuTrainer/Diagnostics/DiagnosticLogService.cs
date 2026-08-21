using System.Collections.Generic;
using System.IO;

namespace TphdCemuTrainer.Diagnostics;

internal static class DiagnosticLogService
{
    public const int DefaultMaxEntries = 100;

    public static void AppendEntry(
        IList<string> diagnostics,
        string logPath,
        string entry,
        string logWriteFailurePrefix,
        int maxEntries = DefaultMaxEntries)
    {
        InsertAndTrim(diagnostics, entry, maxEntries);

        if (!TryAppendFileLine(logPath, entry, out var errorMessage))
        {
            diagnostics.Insert(0, $"{DateTimeOffset.Now:O} {logWriteFailurePrefix}: {errorMessage}");
        }
    }

    public static bool TryAppendFileLine(
        string logPath,
        string entry,
        out string errorMessage)
    {
        errorMessage = string.Empty;

        try
        {
            var logDirectory = Path.GetDirectoryName(logPath);
            if (!string.IsNullOrWhiteSpace(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            File.AppendAllText(logPath, entry + Environment.NewLine);
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    private static void InsertAndTrim(IList<string> diagnostics, string entry, int maxEntries)
    {
        diagnostics.Insert(0, entry);
        while (diagnostics.Count > maxEntries)
        {
            diagnostics.RemoveAt(diagnostics.Count - 1);
        }
    }
}
