namespace TphdCemuTrainer.Cheats;

public static class GoldenBugsDefinitions
{
    public const uint FirstOffset = 0x2A1;
    public const int OwnershipByteCount = 3;
    public const int ResearchByteCount = 4;
    public const int ByteCount = ResearchByteCount;

    public static IReadOnlyList<string> BugNames { get; } =
    [
        "Male Ant",
        "Female Ant",
        "Male Mantis",
        "Female Mantis",
        "Male Butterfly",
        "Female Butterfly",
        "Male Phasmid",
        "Female Phasmid",
        "Male Dayfly",
        "Female Dayfly",
        "Male Stag Beetle",
        "Female Stag Beetle",
        "Male Ladybug",
        "Female Ladybug",
        "Male Grasshopper",
        "Female Grasshopper",
        "Male Beetle",
        "Female Beetle",
        "Male Pill Bug",
        "Female Pill Bug",
        "Male Snail",
        "Female Snail",
        "Male Dragonfly",
        "Female Dragonfly"
    ];

    public static IReadOnlyList<GoldenBugBitDefinition> Bits { get; } =
        CreateBitDefinitions();

    public static IReadOnlyList<GoldenBugBitDefinition> OwnershipBits { get; } =
        Bits.Where(bit => bit.IsConfirmed && bit.Offset < FirstOffset + OwnershipByteCount).ToList();

    private static IReadOnlyList<GoldenBugBitDefinition> CreateBitDefinitions()
    {
        var confirmed = new Dictionary<(uint Offset, int Bit), (string Name, string Evidence)>
        {
            [(0x2A1, 0)] = ("Male Snail", "Live TPHD confirmed ownership bit"),
            [(0x2A1, 1)] = ("Female Snail", "Live TPHD confirmed ownership bit"),
            [(0x2A1, 2)] = ("Male Dragonfly", "Live TPHD confirmed ownership bit"),
            [(0x2A1, 3)] = ("Female Dragonfly", "Live TPHD confirmed ownership bit"),
            [(0x2A1, 4)] = ("Male Ant", "Live TPHD confirmed ownership bit"),
            [(0x2A1, 5)] = ("Female Ant", "Live TPHD: 0x2A1 changed 0xD8 -> 0xF8"),
            [(0x2A1, 6)] = ("Male Dayfly", "Live TPHD confirmed ownership bit"),
            [(0x2A1, 7)] = ("Female Dayfly", "Live TPHD confirmed ownership bit"),
            [(0x2A2, 0)] = ("Male Phasmid", "Live TPHD confirmed ownership bit"),
            [(0x2A2, 1)] = ("Female Phasmid", "Live TPHD confirmed ownership bit"),
            [(0x2A2, 2)] = ("Male Pill Bug", "Live TPHD: 0x2A2 changed 0xC0 -> 0xC4"),
            [(0x2A2, 3)] = ("Female Pill Bug", "Live TPHD: 0x2A2 changed 0xC4 -> 0xCC"),
            [(0x2A2, 4)] = ("Male Mantis", "Live TPHD confirmed ownership bit"),
            [(0x2A2, 5)] = ("Female Mantis", "Live TPHD confirmed ownership bit"),
            [(0x2A2, 6)] = ("Male Ladybug", "Live TPHD confirmed ownership bit"),
            [(0x2A2, 7)] = ("Female Ladybug", "Live TPHD confirmed ownership bit"),
            [(0x2A3, 0)] = ("Male Beetle", "Live TPHD confirmed ownership bit"),
            [(0x2A3, 1)] = ("Female Beetle", "Live TPHD confirmed ownership bit"),
            [(0x2A3, 2)] = ("Male Butterfly", "Live TPHD confirmed ownership bit"),
            [(0x2A3, 3)] = ("Female Butterfly", "Live TPHD confirmed ownership bit"),
            [(0x2A3, 4)] = ("Male Stag Beetle", "Live TPHD confirmed ownership bit"),
            [(0x2A3, 5)] = ("Female Stag Beetle", "Live TPHD confirmed ownership bit"),
            [(0x2A3, 6)] = ("Male Grasshopper", "Live TPHD: 0x2A3 changed 0x14 -> 0x54"),
            [(0x2A3, 7)] = ("Female Grasshopper", "Live TPHD: 0x2A3 changed 0x54 -> 0xD4")
        };

        var bits = new List<GoldenBugBitDefinition>();
        for (var byteIndex = 0; byteIndex < ByteCount; byteIndex++)
        {
            var offset = FirstOffset + (uint)byteIndex;
            for (var bit = 0; bit < 8; bit++)
            {
                if (confirmed.TryGetValue((offset, bit), out var mapping))
                {
                    bits.Add(new GoldenBugBitDefinition(
                        offset,
                        bit,
                        mapping.Name,
                        "Confirmed",
                        mapping.Evidence));
                }
                else
                {
                    bits.Add(new GoldenBugBitDefinition(
                        offset,
                        bit,
                        null,
                        "Unconfirmed",
                        "Unconfirmed Golden Bug bit"));
                }
            }
        }

        return bits;
    }
}
