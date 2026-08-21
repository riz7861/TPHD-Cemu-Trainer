namespace TphdCemuTrainer.Credits;

public sealed record CreditEntry(string Name, string Contribution);

public static class CreditsCatalog
{
    public static IReadOnlyList<CreditEntry> Entries { get; } =
    [
        new CreditEntry(
            "SomeRandomGuy",
            "TPHD Cheat Engine table research and game memory research."),
        new CreditEntry(
            "TPHD / GBAtemp Community",
            "Previous research into Twilight Princess HD memory, save data and Cheat Engine functionality."),
        new CreditEntry(
            "Riz7861",
            "Application development, additional game and memory research, testing, verification and UI development.")
    ];

    public const string Acknowledgement =
        "This project builds on community research into Twilight Princess HD memory and save data. Existing research used by the project has been tested and verified during development.";

    public const string MissingContributorNotice =
        "If you contributed research used by this project and are missing from the credits, please get in touch so you can be added.";
}
