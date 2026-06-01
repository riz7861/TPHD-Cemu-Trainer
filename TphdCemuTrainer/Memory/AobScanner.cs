using System.Diagnostics;

namespace TphdCemuTrainer.Memory;

public sealed record AobScanCache(int ProcessId, ulong PlayerBaseAddress, ulong RegionBaseAddress, ulong RegionSize);

public sealed record AobScanProgress(int CurrentRegion, int TotalRegions, MemoryRegion Region);

public sealed record AobScanResult(
    ulong? MatchAddress,
    MemoryRegion? MatchRegion,
    TimeSpan RegionEnumerationTime,
    TimeSpan ScanTime,
    int RegionsScanned,
    int RegionsSkipped,
    ulong TotalBytesScanned,
    bool CachedBaseValidated,
    bool CachedRegionValidated,
    string CacheStatus,
    string MatchSource);

public sealed class AobScanner
{
    private const int ChunkSize = 4 * 1024 * 1024;
    private const ulong PreferredMinimumRegionSize = 64 * 1024;

    public ulong? FindFirst(ProcessMemory memory, AobPattern pattern, CancellationToken cancellationToken)
    {
        return FindFirstWithDiagnostics(memory, pattern, null, null, cancellationToken).MatchAddress;
    }

    public AobScanResult FindFirstWithDiagnostics(
        ProcessMemory memory,
        AobPattern pattern,
        AobScanCache? cache,
        IProgress<AobScanProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(memory);
        ArgumentNullException.ThrowIfNull(pattern);

        var scanWatch = Stopwatch.StartNew();
        var bytesScanned = 0UL;
        var cacheStatus = "no-cache";
        var cachedBaseValidated = false;
        var cachedRegionValidated = false;
        MemoryRegion? cachedRegion = null;

        if (cache is not null && cache.ProcessId == memory.ProcessId)
        {
            cacheStatus = "cache-present";
            if (TryMatchAt(memory, cache.PlayerBaseAddress, pattern, out var validationBytes))
            {
                bytesScanned += validationBytes;
                cachedBaseValidated = true;
                cacheStatus = "cached-base-valid";

                if (memory.TryQueryRegion(cache.PlayerBaseAddress, out var baseRegion) && baseRegion.IsReadable)
                {
                    cachedRegion = baseRegion;
                    cachedRegionValidated = true;
                }

                scanWatch.Stop();
                return new AobScanResult(
                    cache.PlayerBaseAddress,
                    cachedRegion,
                    TimeSpan.Zero,
                    scanWatch.Elapsed,
                    0,
                    0,
                    bytesScanned,
                    cachedBaseValidated,
                    cachedRegionValidated,
                    cacheStatus,
                    "cached-base");
            }

            bytesScanned += validationBytes;
            cacheStatus = "cached-base-invalid";

            if (memory.TryQueryRegion(cache.RegionBaseAddress, out var matchingRegion) &&
                matchingRegion.IsReadable &&
                matchingRegion.Size >= (ulong)pattern.Length)
            {
                cachedRegion = matchingRegion;
                cachedRegionValidated = true;
                cacheStatus = "cached-region-valid";
            }
        }

        var enumerationWatch = Stopwatch.StartNew();
        var regions = EnumerateRegions(memory, cancellationToken).ToList();
        enumerationWatch.Stop();

        var candidates = BuildScanPlan(regions, cachedRegion, pattern.Length);
        var skippedRegions = regions.Count - candidates.Count;
        var regionsScanned = 0;
        var totalRegions = candidates.Count;

        for (var index = 0; index < candidates.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var region = candidates[index];
            regionsScanned++;
            progress?.Report(new AobScanProgress(regionsScanned, totalRegions, region));

            var regionResult = ScanRegion(memory, region, pattern, cancellationToken);
            bytesScanned += regionResult.BytesScanned;

            if (regionResult.MatchAddress.HasValue)
            {
                scanWatch.Stop();
                return new AobScanResult(
                    regionResult.MatchAddress.Value,
                    region,
                    enumerationWatch.Elapsed,
                    scanWatch.Elapsed,
                    regionsScanned,
                    skippedRegions,
                    bytesScanned,
                    cachedBaseValidated,
                    cachedRegionValidated,
                    cacheStatus,
                    cachedRegion.HasValue && region.BaseAddress == cachedRegion.Value.BaseAddress
                        ? "cached-region"
                        : "full-scan");
            }
        }

        scanWatch.Stop();
        return new AobScanResult(
            null,
            null,
            enumerationWatch.Elapsed,
            scanWatch.Elapsed,
            regionsScanned,
            skippedRegions,
            bytesScanned,
            cachedBaseValidated,
            cachedRegionValidated,
            cacheStatus,
            "not-found");
    }

    private static IEnumerable<MemoryRegion> EnumerateRegions(ProcessMemory memory, CancellationToken cancellationToken)
    {
        var range = ProcessMemory.GetSystemAddressRange();
        var address = range.Minimum;

        while (address < range.Maximum)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!memory.TryQueryRegion(address, out var region))
            {
                address += 0x1000;
                continue;
            }

            var nextAddress = region.BaseAddress + Math.Max(region.Size, 0x1000UL);
            yield return region;

            address = nextAddress > address ? nextAddress : address + 0x1000;
        }
    }

    private static List<MemoryRegion> BuildScanPlan(
        IReadOnlyCollection<MemoryRegion> regions,
        MemoryRegion? cachedRegion,
        int patternLength)
    {
        var readableDataRegions = regions
            .Where(region => IsCandidateRegion(region, patternLength))
            .ToList();

        var plan = new List<MemoryRegion>();
        if (cachedRegion.HasValue && IsCandidateRegion(cachedRegion.Value, patternLength))
        {
            plan.Add(cachedRegion.Value);
        }

        plan.AddRange(readableDataRegions
            .Where(region => !cachedRegion.HasValue || region.BaseAddress != cachedRegion.Value.BaseAddress)
            .OrderBy(region => GetRegionPriority(region))
            .ThenByDescending(region => region.Size));

        return plan;
    }

    private static bool IsCandidateRegion(MemoryRegion region, int patternLength)
    {
        if (!region.IsReadable ||
            region.IsImage ||
            region.Size < (ulong)Math.Max(patternLength, 0x1000))
        {
            return false;
        }

        return region.IsPrivate || region.IsMapped;
    }

    private static int GetRegionPriority(MemoryRegion region)
    {
        if (!region.IsExecutable && region.Size >= PreferredMinimumRegionSize && region.IsPrivate)
        {
            return 0;
        }

        if (!region.IsExecutable && region.Size >= PreferredMinimumRegionSize && region.IsMapped)
        {
            return 1;
        }

        if (!region.IsExecutable)
        {
            return 2;
        }

        return 3;
    }

    private static AobRegionScanResult ScanRegion(
        ProcessMemory memory,
        MemoryRegion region,
        AobPattern pattern,
        CancellationToken cancellationToken)
    {
        var overlapLength = Math.Max(0, pattern.Length - 1);
        var carry = Array.Empty<byte>();
        var regionOffset = 0UL;
        var bytesScanned = 0UL;

        while (regionOffset < region.Size)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var readLength = (int)Math.Min(ChunkSize, region.Size - regionOffset);
            var readAddress = region.BaseAddress + regionOffset;

            if (memory.TryReadBytes(readAddress, readLength, out var chunk, out var bytesRead) && bytesRead > 0)
            {
                bytesScanned += (ulong)bytesRead;
                var scanBytes = Combine(carry, chunk);
                var scanBase = readAddress - (ulong)carry.Length;
                var index = IndexOf(scanBytes, pattern);

                if (index >= 0)
                {
                    return new AobRegionScanResult(scanBase + (ulong)index, bytesScanned);
                }

                carry = Tail(scanBytes, overlapLength);
            }
            else
            {
                carry = Array.Empty<byte>();
            }

            regionOffset += (ulong)Math.Max(readLength, 1);
        }

        return new AobRegionScanResult(null, bytesScanned);
    }

    private static bool TryMatchAt(ProcessMemory memory, ulong address, AobPattern pattern, out ulong bytesRead)
    {
        bytesRead = 0;
        if (!memory.TryReadBytes(address, pattern.Length, out var bytes, out var actualBytesRead) ||
            actualBytesRead != pattern.Length)
        {
            bytesRead = (ulong)Math.Max(actualBytesRead, 0);
            return false;
        }

        bytesRead = (ulong)actualBytesRead;
        return IndexOf(bytes, pattern) == 0;
    }

    private static int IndexOf(byte[] bytes, AobPattern pattern)
    {
        if (bytes.Length < pattern.Length)
        {
            return -1;
        }

        for (var start = 0; start <= bytes.Length - pattern.Length; start++)
        {
            var matched = true;
            for (var offset = 0; offset < pattern.Length; offset++)
            {
                var expected = pattern.Bytes[offset];
                if (expected.HasValue && bytes[start + offset] != expected.Value)
                {
                    matched = false;
                    break;
                }
            }

            if (matched)
            {
                return start;
            }
        }

        return -1;
    }

    private static byte[] Combine(byte[] left, byte[] right)
    {
        if (left.Length == 0)
        {
            return right;
        }

        var combined = new byte[left.Length + right.Length];
        Buffer.BlockCopy(left, 0, combined, 0, left.Length);
        Buffer.BlockCopy(right, 0, combined, left.Length, right.Length);
        return combined;
    }

    private static byte[] Tail(byte[] bytes, int count)
    {
        if (count <= 0 || bytes.Length == 0)
        {
            return Array.Empty<byte>();
        }

        var actual = Math.Min(count, bytes.Length);
        var tail = new byte[actual];
        Buffer.BlockCopy(bytes, bytes.Length - actual, tail, 0, actual);
        return tail;
    }

    private readonly record struct AobRegionScanResult(ulong? MatchAddress, ulong BytesScanned);
}
