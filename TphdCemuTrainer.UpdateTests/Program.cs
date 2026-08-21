using System.Net;
using TphdCemuTrainer.Memory;
using TphdCemuTrainer.Updates;

var updateTests = new (string Name, Func<Task> Run)[]
{
    ("installed 1.0.0, latest 1.0.0 is current", InstalledCurrent),
    ("installed 1.0.0, latest 1.0.1 has update", PatchUpdateAvailable),
    ("installed 1.9.0, latest 1.10.0 has update", NumericVersionComparison),
    ("installed 2.0.0, latest 1.9.0 is current", InstalledNewerThanLatest),
    ("v-prefixed tags parse", VPrefixedTagParsing),
    ("malformed version tag is unavailable", MalformedVersionTag),
    ("malformed GitHub response is unavailable", MalformedGitHubResponse),
    ("network failure is unavailable", NetworkFailure),
    ("timeout is unavailable", TimeoutFailure),
    ("drafts and prereleases are ignored", DraftsAndPrereleasesIgnored)
};

foreach (var test in updateTests)
{
    await test.Run();
    Console.WriteLine($"PASS {test.Name}");
}

Console.WriteLine($"All {updateTests.Length} update tests passed.");

var memoryVerificationTests = new (string Name, Action Run)[]
{
    ("byte delayed readback ignores missing delayed reads", ByteDelayedReadbackIgnoresMissingReads),
    ("byte delayed readback detects 250ms mismatch", ByteDelayedReadbackDetects250MsMismatch),
    ("byte delayed readback detects 1000ms mismatch", ByteDelayedReadbackDetects1000MsMismatch),
    ("byte-array delayed readback ignores missing delayed reads", ByteArrayDelayedReadbackIgnoresMissingReads),
    ("byte-array delayed readback detects mismatch", ByteArrayDelayedReadbackDetectsMismatch),
    ("verification timings remain 250ms and 1000ms", VerificationTimingsRemainExpected)
};

foreach (var test in memoryVerificationTests)
{
    test.Run();
    Console.WriteLine($"PASS {test.Name}");
}

Console.WriteLine($"All {memoryVerificationTests.Length} memory verification tests passed.");

static async Task InstalledCurrent()
{
    var result = await CheckAsync("1.0.0", ReleasesJson(Release("1.0.0")));
    AssertState(result, UpdateCheckState.Current);
    AssertEqual("1.0.0", result.LatestVersionText);
}

static async Task PatchUpdateAvailable()
{
    var result = await CheckAsync("1.0.0", ReleasesJson(Release("1.0.1")));
    AssertState(result, UpdateCheckState.UpdateAvailable);
    AssertEqual("1.0.1", result.LatestVersionText);
    AssertTrue(GitHubUpdateService.IsSafeGitHubReleaseUrl(result.ReleaseUri), "Release URL should be safe.");
}

static async Task NumericVersionComparison()
{
    var result = await CheckAsync("1.9.0", ReleasesJson(Release("1.10.0")));
    AssertState(result, UpdateCheckState.UpdateAvailable);
    AssertEqual("1.10.0", result.LatestVersionText);
}

static async Task InstalledNewerThanLatest()
{
    var result = await CheckAsync("2.0.0", ReleasesJson(Release("1.9.0")));
    AssertState(result, UpdateCheckState.Current);
}

static async Task VPrefixedTagParsing()
{
    var result = await CheckAsync("1.0.0", ReleasesJson(Release("v1.1.0")));
    AssertState(result, UpdateCheckState.UpdateAvailable);
    AssertEqual("1.1.0", result.LatestVersionText);
}

static async Task MalformedVersionTag()
{
    var result = await CheckAsync("1.0.0", ReleasesJson(Release("release-one")));
    AssertState(result, UpdateCheckState.Unavailable);
}

static async Task MalformedGitHubResponse()
{
    var result = await CheckAsync("1.0.0", "{ not-json");
    AssertState(result, UpdateCheckState.Unavailable);
}

static async Task NetworkFailure()
{
    var handler = new FakeHttpMessageHandler((_, _) =>
        throw new HttpRequestException("network unavailable"));
    var result = await CreateService(handler).CheckForUpdatesAsync("1.0.0");
    AssertState(result, UpdateCheckState.Unavailable);
}

static async Task TimeoutFailure()
{
    var handler = new FakeHttpMessageHandler((_, _) =>
        throw new TaskCanceledException("timed out"));
    var result = await CreateService(handler).CheckForUpdatesAsync("1.0.0");
    AssertState(result, UpdateCheckState.Unavailable);
}

static async Task DraftsAndPrereleasesIgnored()
{
    var result = await CheckAsync(
        "1.0.0",
        ReleasesJson(
            Release("2.0.0", draft: true),
            Release("1.5.0", prerelease: true),
            Release("1.0.0")));

    AssertState(result, UpdateCheckState.Current);
    AssertEqual("1.0.0", result.LatestVersionText);
}

static void ByteDelayedReadbackIgnoresMissingReads()
{
    AssertFalse(
        MemoryWriteVerificationService.HasByteDelayedMismatch(0x42, null, null),
        "Missing delayed byte readbacks should not count as mismatches.");
}

static void ByteDelayedReadbackDetects250MsMismatch()
{
    AssertTrue(
        MemoryWriteVerificationService.HasByteDelayedMismatch(0x42, 0x41, 0x42),
        "A 250ms delayed byte mismatch should be detected.");
}

static void ByteDelayedReadbackDetects1000MsMismatch()
{
    AssertTrue(
        MemoryWriteVerificationService.HasByteDelayedMismatch(0x42, 0x42, 0x41),
        "A 1000ms delayed byte mismatch should be detected.");
}

static void ByteArrayDelayedReadbackIgnoresMissingReads()
{
    AssertFalse(
        MemoryWriteVerificationService.HasBytesDelayedMismatch([0x01, 0x02], null, null),
        "Missing delayed byte-array readbacks should not count as mismatches.");
}

static void ByteArrayDelayedReadbackDetectsMismatch()
{
    AssertTrue(
        MemoryWriteVerificationService.HasBytesDelayedMismatch([0x01, 0x02], [0x01, 0x03], [0x01, 0x02]),
        "A delayed byte-array mismatch should be detected.");
}

static void VerificationTimingsRemainExpected()
{
    AssertEqual(250, MemoryWriteVerificationService.FirstDelayedReadbackMilliseconds);
    AssertEqual(1000, MemoryWriteVerificationService.FinalDelayedReadbackMilliseconds);
}

static async Task<UpdateCheckResult> CheckAsync(string installedVersion, string responseJson)
{
    var handler = new FakeHttpMessageHandler((_, _) =>
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson)
        };
        return Task.FromResult(response);
    });

    return await CreateService(handler).CheckForUpdatesAsync(installedVersion);
}

static GitHubUpdateService CreateService(HttpMessageHandler handler)
{
    return new GitHubUpdateService(new HttpClient(handler)
    {
        Timeout = TimeSpan.FromMilliseconds(250)
    });
}

static string ReleasesJson(params string[] releases) =>
    $"[{string.Join(",", releases)}]";

static string Release(string tag, bool draft = false, bool prerelease = false)
{
    return $$"""
        {
            "tag_name": "{{tag}}",
            "html_url": "https://github.com/Riz7861/TPHD-Cemu-Trainer/releases/tag/{{tag}}",
            "draft": {{draft.ToString().ToLowerInvariant()}},
            "prerelease": {{prerelease.ToString().ToLowerInvariant()}}
        }
        """;
}

static void AssertState(UpdateCheckResult result, UpdateCheckState expected)
{
    AssertEqual(expected, result.State);
}

static void AssertEqual<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"Expected {expected}, got {actual}.");
    }
}

static void AssertTrue(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static void AssertFalse(bool condition, string message)
{
    if (condition)
    {
        throw new InvalidOperationException(message);
    }
}

sealed class FakeHttpMessageHandler(
    Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send)
    : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return send(request, cancellationToken);
    }
}
