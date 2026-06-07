namespace TphdCemuTrainer.Cheats;

public static class StampDefinitions
{
    public const uint FirstOffset = 0xAF5;
    public const int ByteCount = 7;

    public static IReadOnlyList<StampBitDefinition> Stamps { get; } =
    [
        .. CreateLetterGroup(0xAFB, 'A'),
        .. CreateLetterGroup(0xAFA, 'I'),
        .. CreateLetterGroup(0xAF9, 'Q'),

        new(0xAF8, 0, "Y", "Confirmed Stamp bit"),
        new(0xAF8, 1, "Z", "Confirmed Stamp bit"),
        new(0xAF8, 2, "Rupee", "Confirmed Stamp bit"),
        new(0xAF8, 3, "Treasure Chest", "Confirmed Stamp bit"),
        new(0xAF8, 4, "Piece of Heart", "Confirmed Stamp bit"),
        new(0xAF8, 5, "Heart Container", "Confirmed Stamp bit"),
        new(0xAF8, 6, "Link Happy", "Confirmed Stamp bit"),
        new(0xAF8, 7, "Link Angry", "Confirmed Stamp bit"),

        new(0xAF7, 0, "Link Sad", "Confirmed Stamp bit"),
        new(0xAF7, 1, "Link Surprised", "Confirmed Stamp bit"),
        new(0xAF7, 2, "Wolf Link", "Confirmed Stamp bit"),
        new(0xAF7, 3, "Midna Happy", "Confirmed Stamp bit"),
        new(0xAF7, 4, "Midna Angry", "Confirmed Stamp bit"),
        new(0xAF7, 5, "Midna Sad", "Confirmed Stamp bit"),
        new(0xAF7, 6, "Midna Surprised", "Confirmed Stamp bit"),
        new(0xAF7, 7, "Ooccoo", "Confirmed Stamp bit"),

        new(0xAF6, 0, "Zelda Happy", "Confirmed Stamp bit"),
        new(0xAF6, 1, "Zelda Angry", "Confirmed Stamp bit"),
        new(0xAF6, 2, "Zelda Sad", "Confirmed Stamp bit"),
        new(0xAF6, 3, "Zelda Surprised", "Confirmed Stamp bit"),

        // Keep the endpoint bit positions used by the existing live-tested implementation.
        new(0xAF5, 0, "Fairy", "Confirmed active Stamp bit"),
        new(0xAF5, 7, "Twili Midna", "Confirmed active Stamp bit")
    ];

    public static int MappedStampCount => Stamps.Count;

    private static IReadOnlyList<StampBitDefinition> CreateLetterGroup(uint offset, char firstLetter)
    {
        return Enumerable.Range(0, 8)
            .Select(bit => new StampBitDefinition(
                offset,
                bit,
                ((char)(firstLetter + bit)).ToString(),
                "Confirmed Stamp bit"))
            .ToList();
    }
}
