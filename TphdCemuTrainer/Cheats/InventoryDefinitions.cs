namespace TphdCemuTrainer.Cheats;

public static class InventoryDefinitions
{
    // CT source: "Link's Inventory" item slots, _playerbase+258 through _playerbase+26F.
    public const uint FirstSlotOffset = 0x258;
    public const int SlotCount = 24;
    public const byte EmptyItemId = 255;
    public const int FishingRodSlotIndex = 20;

    public static IReadOnlyList<InventoryItemDefinition> SafeItems { get; } =
    [
        new(255, "Nothing", "Nothing"),

        new(62, "Hawkeye", "Usable items"),
        new(64, "Gale Boomerang", "Usable items"),
        new(65, "Spinner", "Usable items"),
        new(66, "Ball and Chain", "Usable items"),
        new(67, "Hero's Bow", "Usable items"),
        new(89, "Hero's Bow With Bomb Arrows", "Usable items"),
        new(90, "Hero's Bow With Hawkeye", "Usable items"),
        new(68, "Clawshot", "Usable items"),
        new(71, "Double Clawshots", "Usable items"),
        new(69, "Iron Boots", "Usable items"),
        new(114, "Bomblings", "Usable items"),
        new(70, "Dominion Rod (cannot be used)", "Usable items"),
        new(76, "Dominion Rod", "Usable items"),
        new(75, "Slingshot", "Usable items"),
        new(72, "Lantern", "Usable items"),
        new(112, "Bombs", "Usable items"),
        new(113, "Water Bombs", "Usable items"),
        new(132, "Horse Call", "Usable items"),
        new(232, "Ghost Lantern", "Usable items"),
        new(248, "Lantern", "Usable items"),
        new(74, "Fishing Rod (Lure)", "Usable items"),
        new(91, "Fishing Rod (Bobber)", "Usable items"),
        new(92, "Fishing Rod + Earring", "Usable items"),
        new(93, "Fishing Rod With Worm", "Usable items"),
        new(94, "Fishing Rod (Bobber) + Earring", "Usable items"),
        new(95, "Fishing Rod (Lure) + Worm + Earring", "Usable items"),

        new(96, "Empty Bottle", "Bottle contents"),
        new(97, "Red Potion", "Bottle contents"),
        new(98, "Magic Potion", "Bottle contents"),
        new(99, "Blue Potion", "Bottle contents"),
        new(100, "Milk", "Bottle contents"),
        new(101, "Milk (1/2)", "Bottle contents"),
        new(102, "Lantern Oil", "Bottle contents"),
        new(103, "Water", "Bottle contents"),
        new(106, "Nasty Soup", "Bottle contents"),
        new(107, "Hot Springwater", "Bottle contents"),
        new(108, "Fairy", "Bottle contents"),
        new(115, "Great Fairy's Tears", "Bottle contents"),
        new(116, "Worm", "Bottle contents"),
        new(118, "Bee Larvae", "Bottle contents"),
        new(119, "Rare Chu Jelly", "Bottle contents"),
        new(120, "Red Chu Jelly", "Bottle contents"),
        new(121, "Blue Chu Jelly", "Bottle contents"),
        new(122, "Green Chu Jelly", "Bottle contents"),
        new(123, "Yellow Chu Jelly", "Bottle contents"),
        new(124, "Purple Chu Jelly", "Bottle contents"),
        new(125, "Simple Soup", "Bottle contents"),
        new(126, "Good Soup", "Bottle contents"),
        new(127, "Superb Soup", "Bottle contents"),
        new(158, "Bee Larvae", "Bottle contents"),
        new(159, "Black Chu Jelly", "Bottle contents"),

        new(45, "Ooccoo's Note", "Quest-related items"),
        new(128, "Renado's Letter", "Quest-related items"),
        new(129, "Invoice", "Quest-related items"),
        new(145, "Ashei's Sketch", "Quest-related items"),
        new(61, "Coral Earring", "Quest-related items"),
        new(130, "Wooden Statue", "Quest-related items"),
        new(131, "Ilia's Charm", "Quest-related items"),
        new(233, "Ancient Sky Book", "Quest-related items"),
        new(234, "Ancient Sky Book", "Quest-related items"),
        new(235, "Filled Sky Book", "Quest-related items")
    ];

    public static InventoryItemDefinition Nothing => SafeItems[0];

    public static IReadOnlyList<InventoryItemDefinition> FishingRodVariants { get; } =
    [
        GetItemDefinition(74),
        GetItemDefinition(91),
        GetItemDefinition(92),
        GetItemDefinition(93),
        GetItemDefinition(94),
        GetItemDefinition(95)
    ];

    public static IReadOnlyList<InventoryFixedSlotDefinition> FixedSlots { get; } =
    [
        new(
            "fishing-rod",
            "Fishing Rod",
            FishingRodSlotIndex,
            FishingRodVariants,
            IsGameManaged: true,
            "Observed CT-backed inventory byte: slot 21, _playerbase+0x26C",
            "TPHD appears to manage this fixed slot and may restore it when unrelated item IDs are written.")
    ];

    public static string GetItemName(byte itemId)
    {
        return SafeItems.FirstOrDefault(item => item.ItemId == itemId)?.Name
            ?? $"Unknown / unsafe item ({itemId})";
    }

    public static InventoryItemDefinition GetDefaultSelection(byte itemId)
    {
        return SafeItems.FirstOrDefault(item => item.ItemId == itemId)
            ?? Nothing;
    }

    public static InventoryItemDefinition GetFixedSlotDefaultSelection(
        InventoryFixedSlotDefinition slot,
        byte itemId)
    {
        return slot.AllowedItems.FirstOrDefault(item => item.ItemId == itemId)
            ?? slot.AllowedItems[0];
    }

    public static bool IsGameManagedSlot(int slotIndex)
    {
        return FixedSlots.Any(slot => slot.SlotIndex == slotIndex && slot.IsGameManaged);
    }

    public static string GetManagementStatus(int slotIndex)
    {
        return IsGameManagedSlot(slotIndex) ? "Game-managed" : "Raw / unknown";
    }

    private static InventoryItemDefinition GetItemDefinition(byte itemId)
    {
        return SafeItems.First(item => item.ItemId == itemId);
    }
}
