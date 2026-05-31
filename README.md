# TPHD Cemu Trainer

A Windows WPF trainer for **The Legend of Zelda: Twilight Princess HD** running in **Cemu**. It attaches to `Cemu.exe`, scans for the player data block using the Cheat Engine table AOB, and edits selected values through external process memory reads and writes.

This trainer is external only. It does not inject DLLs, install drivers, hook emulator code, bypass anti-cheat systems, or modify Cemu or game files.

## Supported Game / Emulator

- Game: The Legend of Zelda: Twilight Princess HD for Wii U
- Emulator: Cemu on Windows
- Process name: `Cemu.exe`
- Source table: `Zelda_TP_HD_Mega_Trainer (by toto621).ct`

## Current Features

The UI is organized as a tabbed trainer/save-editor hybrid so new systems can be added without crowding one large grid.

- **General**: health, max health, heart container summary, lantern oil, wallet, rupees, Poe Souls, Golden Bugs count
- **Inventory**: reserved item ownership checklist for inventory-screen items
- **Equipment**: reserved weapons, shields, armor, and equipment groups
- **Ammo & Upgrades**: wallet, quiver, bomb bag, and seed capacity-aware edits
- **Collectibles**: Poe Souls, Golden Bugs summary, heart containers, future heart-piece tracking
- **Story Flags**: reserved progression flags
- **Hidden Skills**: reserved hidden-skill tracking
- **Quest Items**: reserved quest item/progression tracking
- **Debug**: player base, AOB pattern, raw CT-backed values, and future memory tools

## Implemented Memory Edits

- Current health
- Maximum health
- Rupees
- Lantern oil
- Arrows
- Bomb slots 1-3
- Seeds
- Poe souls
- Wallet capacity
- Quiver capacity
- Bomb bag capacity
- Golden Bugs count display from the CT bitfield

## Capacity System

Capacity-limited values are clamped before every write. The **Max** button means the currently selected capacity, not a global maximum.

- Wallet: 500, 1000, 2000, 9999
- Quiver: 30, 60, 100
- Bomb Bags: 30, 60
- Seed Bag: 50

The CT exposes one bomb bag capacity byte, so all three bomb slots currently share that CT-backed capacity selector. The seed bag capacity is fixed because the CT table does not expose a separate seed capacity offset.

Targets initialize from the current in-memory value after a successful scan. Lock mode writes only clamped values. Current health is also clamped to maximum health.

## Requirements

- Windows 10 or newer
- .NET 9 SDK or Visual Studio with the .NET desktop workload
- Cemu with Twilight Princess HD loaded

## How to Build

From the repository root:

```powershell
dotnet build TPHD-Cemu-Trainer.sln
```

The WPF app builds to:

```text
TphdCemuTrainer\bin\Debug\net9.0-windows\
```

## How to Run

Start Cemu, load Twilight Princess HD, and load into gameplay before attaching. Then run:

```powershell
dotnet run --project TphdCemuTrainer\TphdCemuTrainer.csproj
```

In the app, click **Attach / Rescan**. If the AOB scan succeeds, the trainer shows the Cemu process ID and the resolved player base address.

## How the CT File Is Used

The Cheat Engine table is treated as the source of truth:

- Player base AOB: `10 08 9B CC 00 00 00 01 18 3A ?? F0 10 08 9B C4`
- Cheat offsets are copied from `_playerbase+...` entries in the CT file.
- CT custom types marked `2 Byte Big Endian` and `4 Byte Big Endian` are read as big-endian values.
- Byte entries are read and written as single-byte values.
- Capacity dropdowns use the CT-backed capacity offsets where they exist.

The app does not parse or execute Cheat Engine scripts at runtime. The relevant AOB, offsets, capacities, and value formats are documented in `TphdCemuTrainer/Cheats/CheatCatalog.cs`.

## Project Structure

- `TphdCemuTrainer/Memory/ProcessMemory.cs`: process attach plus `ReadProcessMemory` / `WriteProcessMemory` wrappers
- `TphdCemuTrainer/Memory/AobScanner.cs`: external AOB scanner with wildcard-byte support
- `TphdCemuTrainer/Memory/BigEndianMemory.cs`: big-endian read/write helpers
- `TphdCemuTrainer/Cheats/CheatCatalog.cs`: CT-derived cheat and capacity definitions
- `TphdCemuTrainer/Cheats/FutureFeatureCatalog.cs`: reserved trainer/save-editor feature groups
- `TphdCemuTrainer/ViewModels/`: UI-facing value and capacity models
- `TphdCemuTrainer/MainWindow.xaml`: tabbed WPF trainer UI

## Known Limitations

- The player base scan depends on the CT table AOB. It may fail on unsupported game revisions, different memory layouts, or if gameplay is not loaded.
- Missing player data is handled as a rescan state, not an application failure.
- Inventory, equipment, story flags, hidden skills, and quest items are laid out for future expansion but not yet written.
- Values are simple external memory edits. They do not patch game logic.
- If Cemu runs as administrator, the trainer may also need to run as administrator.

## Troubleshooting

**Cemu.exe is not running**

Start Cemu before clicking **Attach / Rescan**.

**Load into gameplay and rescan**

Cemu was found, but the player data AOB was not. Load Twilight Princess HD into active gameplay, then click **Attach / Rescan** again.

**Values clamp lower than expected**

Check the selected capacity dropdown. Ammo, bombs, seeds, rupees, and health are clamped to the active capacity or maximum health before writing.

**Values do not change**

Confirm the trainer is connected and showing a player base address. Some values may update only after the game refreshes inventory or HUD state.

**Access denied**

Run the trainer with the same privilege level as Cemu. If Cemu is elevated, the trainer likely needs to be elevated too.

## Safety / Legal Note

This project is intended for personal offline emulator use only. Do not use it with online services, shared competitive environments, or software you do not have permission to inspect or modify in memory.

## Credits

Cheat table offsets, AOB patterns, and behavior references are credited to **toto621**, author of `Zelda_TP_HD_Mega_Trainer (by toto621).ct`.
