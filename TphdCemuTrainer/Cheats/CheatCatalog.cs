namespace TphdCemuTrainer.Cheats;

public static class CheatCatalog
{
    // CT source: Zelda_TP_HD_Mega_Trainer (by toto621).ct, "Activate" Auto Assembler script.
    public const string PlayerBaseAob = "10 08 9B CC 00 00 00 01 18 3A ?? F0 10 08 9B C4";

    // CT source: child entries under _playerbase. Offsets and value types are copied from the table.
    public static IReadOnlyList<CheatDefinition> All { get; } =
    [
        new(
            "Rupees",
            0x1C2,
            CheatValueKind.UInt16BigEndian,
            9999,
            9999,
            true,
            "CT ID 5: _playerbase+1C2, 2 Byte Big Endian"),

        new(
            "Max hearts (quarters)",
            0x1BE,
            CheatValueKind.UInt16BigEndian,
            80,
            80,
            true,
            "CT ID 134/216: _playerbase+1BE, 2 Byte Big Endian; all hearts byte is +1BF = 80"),

        new(
            "Hearts (quarters)",
            0x1C0,
            CheatValueKind.UInt16BigEndian,
            80,
            80,
            true,
            "CT ID 71: _playerbase+1C0, 2 Byte Big Endian"),

        new(
            "Lantern oil",
            0x1C6,
            CheatValueKind.Byte,
            255,
            255,
            true,
            "CT ID 117: _playerbase+1C6, Byte"),

        new(
            "Arrows",
            0x2A8,
            CheatValueKind.Byte,
            100,
            100,
            true,
            "CT ID 164: _playerbase+2A8, Byte"),

        new(
            "Bomb slot 1",
            0x2A9,
            CheatValueKind.Byte,
            60,
            60,
            true,
            "CT ID 165: _playerbase+2A9, Byte"),

        new(
            "Bomb slot 2",
            0x2AA,
            CheatValueKind.Byte,
            60,
            60,
            true,
            "CT ID 169: _playerbase+2AA, Byte"),

        new(
            "Bomb slot 3",
            0x2AB,
            CheatValueKind.Byte,
            60,
            60,
            true,
            "CT ID 170: _playerbase+2AB, Byte"),

        new(
            "Seeds",
            0x2B0,
            CheatValueKind.Byte,
            50,
            50,
            true,
            "CT ID 128: _playerbase+2B0, Byte"),

        new(
            "Quiver size",
            0x2B4,
            CheatValueKind.Byte,
            100,
            100,
            false,
            "CT ID 204: _playerbase+2B4, Byte",
            [
                new CheatOption(0, "None"),
                new CheatOption(30, "Normal"),
                new CheatOption(60, "Big"),
                new CheatOption(100, "Giant")
            ]),

        new(
            "Bomb bag size",
            0x2B5,
            CheatValueKind.Byte,
            60,
            60,
            false,
            "CT ID 206: _playerbase+2B5, Byte",
            [
                new CheatOption(0, "None"),
                new CheatOption(30, "Normal"),
                new CheatOption(60, "Big")
            ]),

        new(
            "Poe souls",
            0x2C8,
            CheatValueKind.Byte,
            60,
            60,
            false,
            "CT ID 166: _playerbase+2C8, Byte")
    ];
}
