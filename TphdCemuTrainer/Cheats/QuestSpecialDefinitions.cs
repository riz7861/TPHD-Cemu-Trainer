namespace TphdCemuTrainer.Cheats;

public static class QuestSpecialDefinitions
{
    public const string OoccooSlotId = "ooccoo";
    public const string GenericQuestSlotId = "generic-quest-slot";
    public const string SkyBookSlotId = "sky-book";

    public const uint OoccooSlotOffset = 0x26A;
    public const uint GenericQuestSlotOffset = 0x26B;
    public const uint SkyBookSlotOffset = 0x26E;
    public const uint DominionRodRestorationOffset = 0x3D1;
    public const int DominionRodRestorationBit = 7;
    public const uint CurrentDungeonItemsOffset = 0xFD1;

    public const byte AncientSkyBookValue = 0xE9;

    private static QuestSpecialOptionDefinition EmptyOption { get; } =
        new(InventoryDefinitions.EmptyItemId, "Empty", "Confirmed empty value");

    public static IReadOnlyList<QuestSpecialSlotDefinition> Slots { get; } =
    [
        new(
            OoccooSlotId,
            "Ooccoo",
            OoccooSlotOffset,
            "Confirmed Ooccoo slot. This edits the visible/special item byte only.",
            [
                EmptyOption,
                new(0x25, "Ooccoo", "Confirmed Ooccoo value"),
                new(0x27, "Ooccoo Jr.", "Confirmed Ooccoo Jr. value")
            ]),
        new(
            GenericQuestSlotId,
            "Generic Quest Slot / Bottom-Right Quest Slot",
            GenericQuestSlotOffset,
            "Advanced / experimental-safe. Confirmed generic quest item renderer, but the intended story item is still unknown.",
            [
                EmptyOption,
                new(0x25, "Ooccoo", "Confirmed rendered value"),
                new(0x27, "Ooccoo Jr.", "Confirmed rendered value"),
                new(0x4A, "Fishing Rod", "Confirmed rendered value"),
                new(0x83, "Ilia's Charm", "Confirmed rendered value"),
                new(0x84, "Horse Call", "Confirmed rendered value"),
                new(AncientSkyBookValue, "Ancient Sky Book", "Confirmed rendered value")
            ]),
        new(
            "fishing-rod",
            "Fishing Rod",
            0x26C,
            "Confirmed Fishing Rod slot. This edits the fixed rod-state slot.",
            [
                EmptyOption,
                new(0x4A, "Fishing Rod", "Confirmed Fishing Rod value"),
                new(0x5C, "Fishing Rod + Coral Earring", "Confirmed upgraded rod value")
            ]),
        new(
            "companion-item",
            "Horse Call / Ilia Item",
            0x26D,
            "Confirmed companion item slot for Ilia's Charm and Horse Call.",
            [
                EmptyOption,
                new(0x83, "Ilia's Charm", "Confirmed Ilia item value"),
                new(0x84, "Horse Call", "Confirmed Horse Call value")
            ]),
        new(
            SkyBookSlotId,
            "Ancient Sky Book",
            SkyBookSlotOffset,
            "Confirmed Sky Book slot. Ancient Sky Book requires the Ooccoo slot to be empty.",
            [
                EmptyOption,
                new(AncientSkyBookValue, "Ancient Sky Book", "Confirmed Ancient Sky Book value")
            ])
    ];

    public static QuestBitFlagDefinition DominionRodRestoration { get; } =
        new(
            "dominion-rod-restoration",
            "Restore Dominion Rod / Complete Restoration",
            DominionRodRestorationOffset,
            DominionRodRestorationBit,
            "Requires the Dominion Rod item. Sets restoration/reactivation state only and does not grant the item.");

    public static IReadOnlyList<QuestBitFlagDefinition> CurrentDungeonItems { get; } =
    [
        new("current-dungeon-map", "Map", CurrentDungeonItemsOffset, 0, "Current dungeon item ownership bit; mask 0x01"),
        new("current-dungeon-compass", "Compass", CurrentDungeonItemsOffset, 1, "Current dungeon item ownership bit; mask 0x02"),
        new("current-dungeon-boss-key", "Boss Key / Large Key", CurrentDungeonItemsOffset, 2, "Current dungeon item ownership bit; mask 0x04; behavior may depend on dungeon/story context")
    ];

    public static QuestSpecialOptionDefinition GetDefaultSelection(QuestSpecialSlotDefinition slot, byte value)
    {
        return slot.Options.FirstOrDefault(option => option.Value == value)
            ?? slot.Options[0];
    }

    public static string GetSlotValueName(QuestSpecialSlotDefinition slot, byte value)
    {
        return slot.Options.FirstOrDefault(option => option.Value == value)?.Name
            ?? InventoryDefinitions.GetItemName(value);
    }
}
