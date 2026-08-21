namespace TphdCemuTrainer.Updates;

public sealed record UpdateCheckResult(
    UpdateCheckState State,
    string InstalledVersionText,
    string? LatestVersionText,
    Uri? ReleaseUri)
{
    public static UpdateCheckResult Current(string installedVersionText, string latestVersionText) =>
        new(UpdateCheckState.Current, installedVersionText, latestVersionText, null);

    public static UpdateCheckResult UpdateAvailable(
        string installedVersionText,
        string latestVersionText,
        Uri releaseUri) =>
        new(UpdateCheckState.UpdateAvailable, installedVersionText, latestVersionText, releaseUri);

    public static UpdateCheckResult Unavailable(string installedVersionText) =>
        new(UpdateCheckState.Unavailable, installedVersionText, null, null);
}
