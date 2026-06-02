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
        new(96, "Empty Bottle", "Live-confirmed bottle value"),
        new(97, "Milk", "Live-confirmed bottle value"),
        new(100, "Red Potion", "Live-confirmed bottle value"),
        new(101, "Milk (1/2)", "Live-confirmed bottle value"),
        new(102, "Lantern Oil", "Live-confirmed bottle value"),
        new(108, "Fairy", "Live-confirmed bottle value"),
        new(115, "Great Fairy's Tears", "Live-confirmed bottle value"),
        new(116, "Worm", "Live-confirmed bottle value"),
        new(118, "Bee Larvae", "Live-confirmed bottle value"),
        new(119, "Rare Chu Jelly", "Live-confirmed bottle value"),
        new(120, "Red Chu Jelly", "Live-confirmed bottle value"),
        new(121, "Blue Chu Jelly", "Live-confirmed bottle value"),
        new(122, "Green Chu Jelly", "Live-confirmed bottle value"),
        new(123, "Yellow Chu Jelly", "Live-confirmed bottle value"),
        new(124, "Purple Chu Jelly", "Live-confirmed bottle value"),
        new(InventoryDefinitions.EmptyItemId, "Nothing / No Bottle", "Live-confirmed clear value")
    ];

    public static BottleContentDefinition Nothing =>
        ConfirmedContents.First(content => content.ItemId == InventoryDefinitions.EmptyItemId);

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
