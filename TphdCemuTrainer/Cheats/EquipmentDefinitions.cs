namespace TphdCemuTrainer.Cheats;

public static class EquipmentDefinitions
{
    // CT source: TPHD 2.2.CT Equipment entries. Offsets are expressed relative to _playerbase.
    public static EquipmentSlotDefinition ArmorSlot { get; } = new(
        "armor",
        "Equipped armor",
        0x1D1,
        [
            new(46, "None"),
            new(47, "Hero's Tunic"),
            new(48, "Magic Armor"),
            new(49, "Zora Armor")
        ],
        "TPHD 2.2.CT: Equiped Armor, _playerbase+0x1D1, Byte");

    public static EquipmentSlotDefinition SwordSlot { get; } = new(
        "sword",
        "Equipped sword",
        0x1D2,
        [
            new(255, "None"),
            new(63, "Wooden Sword"),
            new(40, "Ordon Sword"),
            new(41, "Master Sword"),
            new(73, "Master Sword Infused")
        ],
        "TPHD 2.2.CT: Equiped Sword, _playerbase+0x1D2, Byte");

    public static EquipmentSlotDefinition ShieldSlot { get; } = new(
        "shield",
        "Equipped shield",
        0x1D3,
        [
            new(255, "None"),
            new(42, "Ordon Shield"),
            new(43, "Wooden Shield"),
            new(44, "Hylian Shield")
        ],
        "TPHD 2.2.CT: Equiped Shield, _playerbase+0x1D3, Byte");

    public static IReadOnlyList<EquipmentSlotDefinition> Slots { get; } =
    [
        ArmorSlot,
        SwordSlot,
        ShieldSlot
    ];

    public static IReadOnlyList<EquipmentFlagDefinition> OwnershipFlags { get; } =
    [
        new(
            "magic-armor",
            "Magic Armor",
            0x28D,
            0,
            "TPHD 2.2.CT: Have Magic Armor, _playerbase+0x28D bit 0"),
        new(
            "zora-armor",
            "Zora Armor",
            0x28D,
            1,
            "TPHD 2.2.CT: Have Zora Armor, _playerbase+0x28D bit 1"),
        new(
            "heros-clothes",
            "Hero's Clothes",
            0x28E,
            7,
            "TPHD 2.2.CT: Have Hero's Clothes, _playerbase+0x28E bit 7"),
        new(
            "ordon-sword",
            "Ordon Sword",
            0x28E,
            0,
            "TPHD 2.2.CT: Have Ordon Sword, _playerbase+0x28E bit 0"),
        new(
            "master-sword",
            "Master Sword",
            0x28E,
            1,
            "TPHD 2.2.CT: Have Master Sword, _playerbase+0x28E bit 1"),
        new(
            "ordon-shield",
            "Ordon Shield",
            0x28E,
            2,
            "TPHD 2.2.CT: Have Ordon Shield, _playerbase+0x28E bit 2"),
        new(
            "wooden-shield",
            "Wooden Shield",
            0x28E,
            3,
            "TPHD 2.2.CT: Have Wooden Shield, _playerbase+0x28E bit 3"),
        new(
            "hylian-shield",
            "Hylian Shield",
            0x28E,
            4,
            "TPHD 2.2.CT: Have Hylian Shield, _playerbase+0x28E bit 4"),
        new(
            "master-sword-infused",
            "Master Sword Infused",
            0x292,
            1,
            "TPHD 2.2.CT: Have Master Sword Imbued With Light, _playerbase+0x292 bit 1")
    ];

    public static string GetSlotValueName(EquipmentSlotDefinition slot, byte value)
    {
        return slot.Options.FirstOrDefault(option => option.Value == value)?.Name
            ?? $"Unknown equipment value ({value})";
    }

    public static EquipmentOptionDefinition GetDefaultSelection(EquipmentSlotDefinition slot, byte value)
    {
        return slot.Options.FirstOrDefault(option => option.Value == value)
            ?? slot.Options[0];
    }
}
