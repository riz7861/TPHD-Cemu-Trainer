namespace TphdCemuTrainer.Cheats;

public static class CheatCatalog
{
    public const int MaxSupportedHealthCapacity = 1000;
    public const string WalletCapacityId = "wallet";
    public const string QuiverCapacityId = "quiver";
    public const string BombBagCapacityId = "bomb-bag";
    public const string SeedBagCapacityId = "seed-bag";

    // CT source: Zelda_TP_HD_Mega_Trainer (by toto621).ct, "Activate" Auto Assembler script.
    public const string PlayerBaseAob = "10 08 9B CC 00 00 00 01 18 3A ?? F0 10 08 9B C4";

    // CT source: direct _playerbase entries. Value kinds match the Cheat Engine custom types.
    public static IReadOnlyList<CheatDefinition> Values { get; } =
    [
        new(
            CheatId.MaximumHealth,
            "Maximum health (quarters)",
            0x1BE,
            CheatValueKind.UInt16BigEndian,
            MaxSupportedHealthCapacity,
            true,
            "CT ID 134/216: _playerbase+1BE, 2 Byte Big Endian; +1BF stores the all-hearts byte"),

        new(
            CheatId.CurrentHealth,
            "Current health (quarters)",
            0x1C0,
            CheatValueKind.UInt16BigEndian,
            MaxSupportedHealthCapacity,
            true,
            "CT ID 71: _playerbase+1C0, 2 Byte Big Endian"),

        new(
            CheatId.HeartProgress,
            "Heart piece progress",
            0x1BF,
            CheatValueKind.Byte,
            byte.MaxValue,
            false,
            "Live-confirmed total heart-piece / heart-container progress counter"),

        new(
            CheatId.LanternOil,
            "Lantern oil",
            0x1C6,
            CheatValueKind.Byte,
            byte.MaxValue,
            true,
            "CT ID 117: _playerbase+1C6, Byte"),

        new(
            CheatId.Rupees,
            "Current rupees",
            0x1C2,
            CheatValueKind.UInt16BigEndian,
            9999,
            true,
            "CT ID 5: _playerbase+1C2, 2 Byte Big Endian",
            WalletCapacityId),

        new(
            CheatId.Arrows,
            "Current arrows",
            0x2A8,
            CheatValueKind.Byte,
            100,
            true,
            "CT ID 164: _playerbase+2A8, Byte",
            QuiverCapacityId),

        new(
            CheatId.BombSlot1,
            "Current bombs",
            0x2A9,
            CheatValueKind.Byte,
            60,
            true,
            "CT ID 165: _playerbase+2A9, Byte",
            BombBagCapacityId),

        new(
            CheatId.BombSlot2,
            "Current bombs",
            0x2AA,
            CheatValueKind.Byte,
            60,
            true,
            "CT ID 169: _playerbase+2AA, Byte",
            BombBagCapacityId),

        new(
            CheatId.BombSlot3,
            "Current bombs",
            0x2AB,
            CheatValueKind.Byte,
            60,
            true,
            "CT ID 170: _playerbase+2AB, Byte",
            BombBagCapacityId),

        new(
            CheatId.Seeds,
            "Current seeds",
            0x2B0,
            CheatValueKind.Byte,
            50,
            true,
            "CT ID 128: _playerbase+2B0, Byte",
            SeedBagCapacityId),

        new(
            CheatId.PoeSouls,
            "Poe Souls Collected",
            0x2C8,
            CheatValueKind.Byte,
            60,
            false,
            "CT ID 166: _playerbase+2C8, Byte"),

        new(
            CheatId.GoldenBugsFlags,
            "Golden bugs flags",
            0x2A1,
            CheatValueKind.UInt32BigEndian,
            int.MaxValue,
            false,
            "CT ID 208: _playerbase+2A1, 4 Byte Big Endian bitfield"),

        new(
            CheatId.CurrentDungeonSmallKeys,
            "Small Keys (current dungeon)",
            0xFD0,
            CheatValueKind.Byte,
            9,
            false,
            "Live-confirmed current/active dungeon small key count"),

        new(
            CheatId.GoronMinesKeyShardState,
            "Goron Mines key-shard state",
            0x2A4,
            CheatValueKind.Byte,
            byte.MaxValue,
            false,
            "Live-confirmed Goron Mines key-shard completion state")
    ];

    // Capacity options use real TPHD capacities. Stored values match the CT table where an offset exists.
    public static IReadOnlyList<CapacityDefinition> Capacities { get; } =
    [
        new(
            WalletCapacityId,
            "Wallet capacity",
            0x1D7,
            CheatValueKind.Byte,
            [
                new CapacityOption("500 Rupees", 500, 0),
                new CapacityOption("1,000 Rupees", 1000, 1),
                new CapacityOption("2,000 Rupees", 2000, 2),
                new CapacityOption("9,999 Rupees", 9999, 3)
            ],
            "CT ID 111: _playerbase+1D7, Byte wallet tier",
            500),

        new(
            QuiverCapacityId,
            "Quiver capacity",
            0x2B4,
            CheatValueKind.Byte,
            [
                new CapacityOption("30 Arrows", 30, 30),
                new CapacityOption("60 Arrows", 60, 60),
                new CapacityOption("100 Arrows", 100, 100)
            ],
            "CT ID 204: _playerbase+2B4, Byte capacity",
            30),

        new(
            BombBagCapacityId,
            "Bomb bag capacity",
            0x2B5,
            CheatValueKind.Byte,
            [
                new CapacityOption("30 Bombs", 30, 30),
                new CapacityOption("60 Bombs", 60, 60)
            ],
            "CT ID 206: _playerbase+2B5, Byte capacity shared by bomb slots in the CT",
            30),

        new(
            SeedBagCapacityId,
            "Seed bag capacity",
            null,
            null,
            [
                new CapacityOption("50 Seeds", 50, 50)
            ],
            "Fixed TPHD seed bag capacity; no separate CT capacity offset is exposed",
            50)
    ];

    public static CheatDefinition GetValue(CheatId id) => Values.First(value => value.Id == id);

    public static CapacityDefinition GetCapacity(string id) => Capacities.First(capacity => capacity.Id == id);
}
