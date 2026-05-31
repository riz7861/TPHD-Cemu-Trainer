using System.Globalization;

namespace TphdCemuTrainer.Memory;

public sealed class AobPattern
{
    private AobPattern(IReadOnlyList<byte?> bytes)
    {
        Bytes = bytes;
    }

    public IReadOnlyList<byte?> Bytes { get; }

    public int Length => Bytes.Count;

    public static AobPattern Parse(string pattern)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        var bytes = new List<byte?>();
        foreach (var rawToken in pattern.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (rawToken is "?" or "??")
            {
                bytes.Add(null);
                continue;
            }

            if (rawToken.Length != 2 ||
                !byte.TryParse(rawToken, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
            {
                throw new FormatException($"Invalid AOB token '{rawToken}'. Use hex bytes or ?? wildcards.");
            }

            bytes.Add(value);
        }

        if (bytes.Count == 0)
        {
            throw new FormatException("The AOB pattern did not contain any bytes.");
        }

        return new AobPattern(bytes);
    }
}
