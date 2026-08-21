using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TphdCemuTrainer.Updates;

public sealed class GitHubUpdateService
{
    public const string Owner = "Riz7861";
    public const string Repository = "TPHD-Cemu-Trainer";

    private static readonly Uri ReleasesApiUri = new(
        $"https://api.github.com/repos/{Owner}/{Repository}/releases");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    public GitHubUpdateService()
        : this(CreateDefaultHttpClient())
    {
    }

    public GitHubUpdateService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<UpdateCheckResult> CheckForUpdatesAsync(
        string installedVersionText,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseVersion(installedVersionText, out var installedVersion))
        {
            return UpdateCheckResult.Unavailable(installedVersionText);
        }

        try
        {
            var installedVersionDisplay = ToDisplayVersion(installedVersion);
            using var request = new HttpRequestMessage(HttpMethod.Get, ReleasesApiUri);
            request.Headers.UserAgent.ParseAdd($"TPHD-Cemu-Trainer-UpdateChecker/{installedVersionDisplay}");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

            using var response = await _httpClient
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return UpdateCheckResult.Unavailable(installedVersionText);
            }

            await using var responseStream = await response.Content
                .ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false);

            var releases = await JsonSerializer
                .DeserializeAsync<List<GitHubRelease>>(responseStream, JsonOptions, cancellationToken)
                .ConfigureAwait(false);

            var latestRelease = FindLatestStableRelease(releases);
            if (latestRelease is null)
            {
                return UpdateCheckResult.Unavailable(installedVersionText);
            }

            var comparison = NormalizeVersion(latestRelease.Version)
                .CompareTo(NormalizeVersion(installedVersion));

            return comparison > 0
                ? UpdateCheckResult.UpdateAvailable(
                    ToDisplayVersion(installedVersion),
                    ToDisplayVersion(latestRelease.Version),
                    latestRelease.ReleaseUri)
                : UpdateCheckResult.Current(
                    ToDisplayVersion(installedVersion),
                    ToDisplayVersion(latestRelease.Version));
        }
        catch (JsonException)
        {
            return UpdateCheckResult.Unavailable(installedVersionText);
        }
        catch (HttpRequestException)
        {
            return UpdateCheckResult.Unavailable(installedVersionText);
        }
        catch (TaskCanceledException)
        {
            return UpdateCheckResult.Unavailable(installedVersionText);
        }
        catch (OperationCanceledException)
        {
            return UpdateCheckResult.Unavailable(installedVersionText);
        }
    }

    public static bool TryParseVersion(string versionText, out Version version)
    {
        version = new Version(0, 0, 0);

        if (string.IsNullOrWhiteSpace(versionText))
        {
            return false;
        }

        var candidate = versionText.Trim();
        if (candidate.StartsWith('v') || candidate.StartsWith('V'))
        {
            candidate = candidate[1..];
        }

        var metadataIndex = candidate.IndexOf('+', StringComparison.Ordinal);
        if (metadataIndex >= 0)
        {
            candidate = candidate[..metadataIndex];
        }

        if (candidate.Contains('-', StringComparison.Ordinal))
        {
            return false;
        }

        return Version.TryParse(candidate, out version!) &&
               version.Major >= 0 &&
               version.Minor >= 0;
    }

    public static bool IsSafeGitHubReleaseUrl(Uri? uri)
    {
        return uri is not null &&
               uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) &&
               uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) &&
               uri.AbsolutePath.StartsWith(
                   $"/{Owner}/{Repository}/releases/",
                   StringComparison.OrdinalIgnoreCase);
    }

    private static HttpClient CreateDefaultHttpClient()
    {
        return new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(6)
        };
    }

    private static StableRelease? FindLatestStableRelease(IEnumerable<GitHubRelease>? releases)
    {
        StableRelease? latestRelease = null;

        if (releases is null)
        {
            return null;
        }

        foreach (var release in releases)
        {
            if (release.Draft ||
                release.Prerelease ||
                string.IsNullOrWhiteSpace(release.TagName) ||
                !TryParseVersion(release.TagName, out var releaseVersion) ||
                !Uri.TryCreate(release.HtmlUrl, UriKind.Absolute, out var releaseUri) ||
                !IsSafeGitHubReleaseUrl(releaseUri))
            {
                continue;
            }

            var stableRelease = new StableRelease(releaseVersion, releaseUri);
            if (latestRelease is null ||
                NormalizeVersion(stableRelease.Version).CompareTo(NormalizeVersion(latestRelease.Version)) > 0)
            {
                latestRelease = stableRelease;
            }
        }

        return latestRelease;
    }

    private static Version NormalizeVersion(Version version)
    {
        return new Version(
            version.Major,
            Math.Max(0, version.Minor),
            Math.Max(0, version.Build),
            Math.Max(0, version.Revision));
    }

    private static string ToDisplayVersion(Version version)
    {
        return $"{version.Major}.{Math.Max(0, version.Minor)}.{Math.Max(0, version.Build)}";
    }

    private sealed record StableRelease(Version Version, Uri ReleaseUri);

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; set; }

        [JsonPropertyName("html_url")]
        public string? HtmlUrl { get; set; }

        [JsonPropertyName("draft")]
        public bool Draft { get; set; }

        [JsonPropertyName("prerelease")]
        public bool Prerelease { get; set; }
    }
}
