namespace TphdCemuTrainer.Cheats;

public static class HiddenSkillsDefinitions
{
    public const uint FirstOffset = 0x3D5;
    public const int OwnershipByteCount = 2;

    public static IReadOnlyList<HiddenSkillDefinition> Skills { get; } =
    [
        new(
            "back-slice",
            "Back Slice",
            0x3D5,
            0,
            "Live TPHD confirmed: _playerbase+0x3D5 bit 0"),
        new(
            "helm-splitter",
            "Helm Splitter",
            0x3D5,
            1,
            "Live TPHD confirmed: _playerbase+0x3D5 bit 1"),
        new(
            "ending-blow",
            "Ending Blow",
            0x3D5,
            2,
            "Live TPHD confirmed: enabling grants Ending Blow; disabling can re-enable wolf/Hero's Shade encounter after area reload"),
        new(
            "shield-attack",
            "Shield Attack",
            0x3D5,
            3,
            "Live TPHD confirmed: _playerbase+0x3D5 bit 3"),
        new(
            "mortal-draw",
            "Mortal Draw",
            0x3D6,
            5,
            "Live TPHD confirmed: _playerbase+0x3D6 bit 5"),
        new(
            "jump-strike",
            "Jump Strike",
            0x3D6,
            6,
            "Live TPHD confirmed: _playerbase+0x3D6 bit 6"),
        new(
            "great-spin",
            "Great Spin",
            0x3D6,
            7,
            "Live TPHD confirmed: _playerbase+0x3D6 bit 7")
    ];
}
