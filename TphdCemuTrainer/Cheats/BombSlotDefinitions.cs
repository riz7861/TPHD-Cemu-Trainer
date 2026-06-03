namespace TphdCemuTrainer.Cheats;

public static class BombSlotDefinitions
{
    public static IReadOnlyList<BombSlotDefinition> Slots { get; } =
    [
        new(1, 0x267),
        new(2, 0x268),
        new(3, 0x269)
    ];

    // Confirmed by live TPHD memory testing. 0x50 is intentionally not exposed:
    // it creates an empty/glitched bomb bag icon.
    public static IReadOnlyList<BombContentDefinition> ConfirmedContents { get; } =
    [
        new(0x70, "Normal Bombs"),
        new(0x71, "Water Bombs"),
        new(0x72, "Bomblings")
    ];

    public static BombContentDefinition DefaultContent => ConfirmedContents[0];

    public static BombContentDefinition GetDefaultSelection(byte value)
    {
        return ConfirmedContents.FirstOrDefault(content => content.Value == value)
            ?? DefaultContent;
    }

    public static bool IsConfirmedContent(byte value)
    {
        return ConfirmedContents.Any(content => content.Value == value);
    }

    public static string GetContentName(byte value)
    {
        return ConfirmedContents.FirstOrDefault(content => content.Value == value)?.Name
            ?? "Unknown / unsupported";
    }
}
