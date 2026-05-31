namespace TphdCemuTrainer.Memory;

public sealed class AobScanner
{
    private const int ChunkSize = 1024 * 1024;

    public ulong? FindFirst(ProcessMemory memory, AobPattern pattern, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(memory);
        ArgumentNullException.ThrowIfNull(pattern);

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
            if (region.IsReadable)
            {
                var match = ScanRegion(memory, region, pattern, cancellationToken);
                if (match.HasValue)
                {
                    return match.Value;
                }
            }

            address = nextAddress > address ? nextAddress : address + 0x1000;
        }

        return null;
    }

    private static ulong? ScanRegion(
        ProcessMemory memory,
        MemoryRegion region,
        AobPattern pattern,
        CancellationToken cancellationToken)
    {
        var overlapLength = Math.Max(0, pattern.Length - 1);
        var carry = Array.Empty<byte>();
        var regionOffset = 0UL;

        while (regionOffset < region.Size)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var readLength = (int)Math.Min(ChunkSize, region.Size - regionOffset);
            var readAddress = region.BaseAddress + regionOffset;

            if (memory.TryReadBytes(readAddress, readLength, out var chunk, out var bytesRead) && bytesRead > 0)
            {
                var scanBytes = Combine(carry, chunk);
                var scanBase = readAddress - (ulong)carry.Length;
                var index = IndexOf(scanBytes, pattern);

                if (index >= 0)
                {
                    return scanBase + (ulong)index;
                }

                carry = Tail(scanBytes, overlapLength);
            }
            else
            {
                carry = Array.Empty<byte>();
            }

            regionOffset += (ulong)Math.Max(readLength, 1);
        }

        return null;
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
}
