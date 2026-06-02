namespace TphdCemuTrainer.Cheats;

public static class InventoryDefinitions
{
    // CT source: "Link's Inventory" item slots, _playerbase+258 through _playerbase+26F.
    public const uint FirstSlotOffset = 0x258;
    public const int SlotCount = 24;
    public const byte EmptyItemId = 255;
    public const int FishingRodSlotIndex = 20;
    private const string FixedSlotConfirmedStatus = "Confirmed grant/remove";

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

    public static IReadOnlyList<InventoryOwnershipDefinition> OwnershipItems { get; } =
    [
        CreateUnknownOwnership(
            "fishing-rod",
            "Fishing Rod",
            [74, 91, 92, 93, 94, 95],
            "Detected from visible inventory. Ownership/progression flag not mapped yet. Visible CT slot 21 / _playerbase+0x26C reflects fishing rod state, but direct writes revert.",
            staticSlotOffset: 0x26C,
            staticItemId: 92,
            fixedSlotStatus: FixedSlotConfirmedStatus),
        CreateUnknownOwnership(
            "slingshot",
            "Slingshot",
            [75],
            "Detected from visible inventory. Ownership/progression flag not mapped yet. Raw CT item ID 75 indicates Slingshot when present.",
            staticSlotOffset: 0x26F,
            staticItemId: 75,
            fixedSlotStatus: FixedSlotConfirmedStatus),
        CreateUnknownOwnership(
            "lantern",
            "Lantern",
            [72, 248],
            "Detected from visible inventory. Ownership/progression flag not mapped yet. Raw CT lantern item IDs indicate Lantern when present.",
            staticSlotOffset: 0x259,
            staticItemId: 72,
            fixedSlotStatus: FixedSlotConfirmedStatus),
        CreateUnknownOwnership(
            "iron-boots",
            "Iron Boots",
            [69],
            "Detected from visible inventory. Ownership/progression flag not mapped yet. Raw CT item ID 69 indicates Iron Boots when present.",
            staticSlotOffset: 0x25B,
            staticItemId: 69,
            fixedSlotStatus: FixedSlotConfirmedStatus),
        CreateUnknownOwnership(
            "heros-bow",
            "Hero's Bow",
            [67, 89, 90],
            "Detected from visible inventory. Ownership/progression flag not mapped yet. Raw CT bow item IDs indicate Hero's Bow when present.",
            staticSlotOffset: 0x25C,
            staticItemId: 67,
            fixedSlotStatus: FixedSlotConfirmedStatus),
        CreateUnknownOwnership(
            "gale-boomerang",
            "Gale Boomerang",
            [64],
            "Detected from visible inventory. Ownership/progression flag not mapped yet. Raw CT item ID 64 indicates Gale Boomerang when present.",
            staticSlotOffset: 0x258,
            staticItemId: 64,
            fixedSlotStatus: FixedSlotConfirmedStatus),
        CreateUnknownOwnership(
            "clawshot",
            "Clawshot",
            [68],
            "Detected from visible inventory. Ownership/progression flag not mapped yet. Raw CT item ID 68 indicates Clawshot when present.",
            staticSlotOffset: 0x261,
            staticItemId: 68),
        CreateUnknownOwnership(
            "double-clawshots",
            "Double Clawshots",
            [71],
            "Detected from visible inventory. Ownership/progression flag not mapped yet. Raw CT item ID 71 indicates Double Clawshots when present.",
            staticSlotOffset: 0x262,
            staticItemId: 71,
            fixedSlotStatus: FixedSlotConfirmedStatus),
        CreateUnknownOwnership(
            "spinner",
            "Spinner",
            [65],
            "Detected from visible inventory. Ownership/progression flag not mapped yet. Raw CT item ID 65 indicates Spinner when present.",
            staticSlotOffset: 0x25A,
            staticItemId: 65,
            fixedSlotStatus: FixedSlotConfirmedStatus),
        CreateUnknownOwnership(
            "dominion-rod",
            "Dominion Rod",
            [70, 76],
            "Detected from visible inventory. Ownership/progression flag not mapped yet. Raw CT dominion rod item IDs indicate Dominion Rod when present.",
            staticSlotOffset: 0x260,
            staticItemId: 70,
            fixedSlotStatus: "Confirmed grant/remove; unpowered/red variant"),
        CreateUnknownOwnership(
            "ball-and-chain",
            "Ball and Chain",
            [66],
            "Detected from visible inventory. Ownership/progression flag not mapped yet. Raw CT item ID 66 indicates Ball and Chain when present.",
            staticSlotOffset: 0x25E,
            staticItemId: 66,
            fixedSlotStatus: FixedSlotConfirmedStatus),
        CreateUnknownOwnership(
            "hawkeye",
            "Hawkeye",
            [62, 90],
            "Detected from visible inventory. Ownership/progression flag not mapped yet. Hawkeye-related CT item IDs indicate Hawkeye when present.",
            staticSlotOffset: 0x25D,
            staticItemId: 62,
            fixedSlotStatus: FixedSlotConfirmedStatus),
        CreateUnknownOwnership(
            "horse-call",
            "Horse Call",
            [132],
            "Detected from visible inventory. Ownership/progression flag not mapped yet. Raw CT item ID 132 indicates Horse Call when present.",
            staticSlotOffset: 0x26D,
            staticItemId: 132,
            fixedSlotStatus: FixedSlotConfirmedStatus),
        CreateUnknownOwnership(
            "bottles",
            "Bottles",
            [96, 97, 98, 99, 100, 101, 102, 103, 106, 107, 108, 115, 116, 118, 119, 120, 121, 122, 123, 124, 125, 126, 127, 158, 159],
            "Detected from visible inventory. Ownership/progression flag not mapped yet. Bottle content item IDs indicate bottles when present.")
    ];

    public static string GetItemName(byte itemId)
    {
        return SafeItems.FirstOrDefault(item => item.ItemId == itemId)?.Name
            ?? $"Unknown / unsafe item ({itemId})";
    }

    public static string GetKnownItemName(byte itemId)
    {
        return SafeItems.FirstOrDefault(item => item.ItemId == itemId)?.Name
            ?? "-";
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
        return slotIndex >= 0 && slotIndex < SlotCount;
    }

    public static string GetManagementStatus(int slotIndex)
    {
        var fixedSlot = FixedSlots.FirstOrDefault(slot => slot.SlotIndex == slotIndex);
        return fixedSlot is not null ? $"{fixedSlot.Name} field" : "Game-managed raw slot";
    }

    public static string GetSlotLabel(int slotIndex)
    {
        var fixedSlot = FixedSlots.FirstOrDefault(slot => slot.SlotIndex == slotIndex);
        return fixedSlot is not null ? $"Slot {slotIndex + 1} / {fixedSlot.Name} field" : $"Slot {slotIndex + 1}";
    }

    public static string GetSlotNotes(int slotIndex)
    {
        var fixedSlot = FixedSlots.FirstOrDefault(slot => slot.SlotIndex == slotIndex);
        return fixedSlot is not null
            ? "Game-managed. Direct writes revert. Real ownership/progression flag not identified yet."
            : "Game-managed CT-derived visible inventory slot. Useful for detection/research; not an ownership flag.";
    }

    public static string GetSlotOffset(int slotIndex)
    {
        return $"0x{FirstSlotOffset + (uint)slotIndex:X}";
    }

    private static InventoryItemDefinition GetItemDefinition(byte itemId)
    {
        return SafeItems.First(item => item.ItemId == itemId);
    }

    private static InventoryOwnershipDefinition CreateUnknownOwnership(
        string id,
        string name,
        IReadOnlyList<byte> detectedItemIds,
        string notes,
        uint? staticSlotOffset = null,
        byte? staticItemId = null,
        string fixedSlotStatus = "")
    {
        var source = staticSlotOffset.HasValue
            ? "Static fixed visible slot mapping from CT-derived slots and live-save research. Ownership/progression flag not mapped."
            : "Ownership/progression flag not mapped in the checked CT source. Visible slots are detection-only.";

        return new InventoryOwnershipDefinition(
            id,
            name,
            detectedItemIds,
            null,
            null,
            source,
            notes,
            staticSlotOffset,
            staticItemId,
            fixedSlotStatus);
    }
}
