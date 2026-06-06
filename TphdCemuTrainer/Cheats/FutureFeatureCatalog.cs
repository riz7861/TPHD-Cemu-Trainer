namespace TphdCemuTrainer.Cheats;

public sealed record FutureFeatureDefinition(string Name, string Notes = "Reserved for future CT/save mapping.");

public static class FutureFeatureCatalog
{
    public static IReadOnlyList<FutureFeatureDefinition> InventoryItems { get; } =
    [
        new("Lantern"),
        new("Fishing Rod"),
        new("Hero's Bow"),
        new("Slingshot"),
        new("Gale Boomerang"),
        new("Clawshot"),
        new("Double Clawshots"),
        new("Spinner"),
        new("Dominion Rod"),
        new("Ball and Chain"),
        new("Horse Call"),
        new("Hawkeye"),
        new("Bottles"),
        new("Ooccoo"),
        new("Ooccoo Jr.")
    ];

    public static IReadOnlyList<FutureFeatureDefinition> Weapons { get; } =
    [
        new("Wooden Sword"),
        new("Ordon Sword"),
        new("Master Sword")
    ];

    public static IReadOnlyList<FutureFeatureDefinition> Shields { get; } =
    [
        new("Wooden Shield"),
        new("Ordon Shield"),
        new("Hylian Shield")
    ];

    public static IReadOnlyList<FutureFeatureDefinition> Armor { get; } =
    [
        new("Hero's Clothes"),
        new("Zora Armor"),
        new("Magic Armor")
    ];

    public static IReadOnlyList<FutureFeatureDefinition> Equipment { get; } =
    [
        new("Iron Boots")
    ];

    public static IReadOnlyList<FutureFeatureDefinition> StoryFlags { get; } =
    [
        new("Fused Shadows"),
        new("Mirror Shards"),
        new("Master Sword obtained"),
        new("Wolf form progression"),
        new("Midna progression"),
        new("Dungeon completion flags")
    ];

    public static IReadOnlyList<FutureFeatureDefinition> HiddenSkills { get; } =
    [
        new("Ending Blow"),
        new("Shield Attack"),
        new("Back Slice"),
        new("Helm Splitter"),
        new("Mortal Draw"),
        new("Jump Strike"),
        new("Great Spin")
    ];

    public static IReadOnlyList<FutureFeatureDefinition> QuestItems { get; } =
    [
        new("Auru's Memo"),
        new("Ashei's Sketch"),
        new("Invoice"),
        new("Wooden Statue")
    ];

    public static IReadOnlyList<FutureFeatureDefinition> DebugTools { get; } =
    [
        new("AOB scan diagnostics", "Shows the current player base AOB and scan result."),
        new("Raw memory values", "Lists CT-backed values as they are read from Cemu."),
        new("Coordinate display", "Reserved for the CT coordinate base scan."),
        new("Future teleport functionality"),
        new("Memory viewer")
    ];
}
