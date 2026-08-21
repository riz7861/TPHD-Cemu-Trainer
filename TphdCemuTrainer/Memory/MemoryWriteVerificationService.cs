namespace TphdCemuTrainer.Memory;

public sealed record MemoryByteDelayedReadbacks(byte? Read250Ms, byte? Read1000Ms)
{
    public bool HasMismatch(byte expectedValue) =>
        MemoryWriteVerificationService.HasByteDelayedMismatch(expectedValue, Read250Ms, Read1000Ms);
}

public sealed record MemoryBytesDelayedReadbacks(byte[]? Read250Ms, byte[]? Read1000Ms)
{
    public bool HasMismatch(IReadOnlyList<byte> expectedBytes) =>
        MemoryWriteVerificationService.HasBytesDelayedMismatch(expectedBytes, Read250Ms, Read1000Ms);
}

public static class MemoryWriteVerificationService
{
    public const int FirstDelayedReadbackMilliseconds = 250;
    public const int FinalDelayedReadbackMilliseconds = 1000;

    private const int SecondDelayMilliseconds = FinalDelayedReadbackMilliseconds - FirstDelayedReadbackMilliseconds;

    public delegate bool TryReadByte(out byte value);

    public delegate bool TryReadBytes(out byte[] bytes);

    public static async Task<MemoryByteDelayedReadbacks> ReadDelayedByteReadbacksAsync(TryReadByte readByte)
    {
        byte? read250Ms = null;
        byte? read1000Ms = null;

        await Task.Delay(FirstDelayedReadbackMilliseconds);
        if (readByte(out var delayed250Value))
        {
            read250Ms = delayed250Value;
        }

        await Task.Delay(SecondDelayMilliseconds);
        if (readByte(out var delayed1000Value))
        {
            read1000Ms = delayed1000Value;
        }

        return new MemoryByteDelayedReadbacks(read250Ms, read1000Ms);
    }

    public static async Task<MemoryBytesDelayedReadbacks> ReadDelayedBytesReadbacksAsync(TryReadBytes readBytes)
    {
        byte[]? read250Ms = null;
        byte[]? read1000Ms = null;

        await Task.Delay(FirstDelayedReadbackMilliseconds);
        if (readBytes(out var delayed250Bytes))
        {
            read250Ms = delayed250Bytes;
        }

        await Task.Delay(SecondDelayMilliseconds);
        if (readBytes(out var delayed1000Bytes))
        {
            read1000Ms = delayed1000Bytes;
        }

        return new MemoryBytesDelayedReadbacks(read250Ms, read1000Ms);
    }

    public static bool HasByteDelayedMismatch(byte expectedValue, byte? read250Ms, byte? read1000Ms) =>
        read250Ms.HasValue && read250Ms.Value != expectedValue ||
        read1000Ms.HasValue && read1000Ms.Value != expectedValue;

    public static bool HasBytesDelayedMismatch(
        IReadOnlyList<byte> expectedBytes,
        byte[]? read250Ms,
        byte[]? read1000Ms) =>
        read250Ms is not null && !expectedBytes.SequenceEqual(read250Ms) ||
        read1000Ms is not null && !expectedBytes.SequenceEqual(read1000Ms);

    public static bool TryReadExactBytes(
        ProcessMemory memory,
        ulong address,
        int length,
        out byte[] bytes)
    {
        if (memory.TryReadBytes(address, length, out bytes, out var bytesRead) && bytesRead == length)
        {
            return true;
        }

        return false;
    }
}
