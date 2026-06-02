namespace TphdCemuTrainer.Cheats;

public static class BottleDefinitions
{
    public static IReadOnlyList<BottleSlotDefinition> Slots { get; } =
    [
        new(1, 0x263),
        new(2, 0x264),
        new(3, 0x265),
        new(4, 0x266)
    ];

    // Bottle Editor v1 exposes only live-confirmed bottle-slot values.
    public static IReadOnlyList<BottleContentDefinition> ConfirmedContents { get; } =
    [
        new(InventoryDefinitions.EmptyItemId, "Nothing / No Bottle", "Live-confirmed clear value"),
        new(96, "Empty Bottle", "Live-confirmed bottle value"),
        new(108, "Fairy", "Live-confirmed bottle value"),
        new(115, "Great Fairy's Tears", "Live-confirmed bottle value"),
        new(119, "Rare Chu Jelly", "Live-confirmed bottle value"),
        new(121, "Blue Chu Jelly", "Live-confirmed bottle value")
    ];

    public static BottleContentDefinition Nothing => ConfirmedContents[0];

    public static BottleContentDefinition GetDefaultSelection(byte itemId)
    {
        return ConfirmedContents.FirstOrDefault(content => content.ItemId == itemId)
            ?? Nothing;
    }

    public static string GetBottleContentName(byte itemId)
    {
        return ConfirmedContents.FirstOrDefault(content => content.ItemId == itemId)?.Name
            ?? InventoryDefinitions.GetItemName(itemId);
    }
}
