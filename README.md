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
- **Inventory**: fixed-slot item editors plus an advanced/raw CT byte view
- **Equipment**: CT-backed equipped armor/sword/shield editor plus guarded ownership flag toggles
- **Ammo & Upgrades**: wallet, quiver, bomb bag, and seed capacity-aware edits
- **Collectibles**: Poe Souls, Golden Bugs summary, heart containers, future heart-piece tracking
- **Story Flags**: reserved progression flags
- **Hidden Skills**: reserved hidden-skill tracking
- **Quest Items**: reserved quest item/progression tracking
- **Debug**: player base, AOB pattern, progression diagnostics, raw CT-backed values, and future memory tools

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
- Inventory fixed-slot item IDs at `_playerbase+0x258` through `_playerbase+0x26F`
- Equipped armor at `_playerbase+0x1D1`
- Equipped sword at `_playerbase+0x1D2`
- Equipped shield at `_playerbase+0x1D3`
- Equipment ownership flags at `_playerbase+0x28D`, `_playerbase+0x28E`, and `_playerbase+0x292`

## Capacity System

Capacity-limited values are clamped before every write. The **Max** button means the currently selected capacity, not a global maximum.

- Wallet: 500, 1000, 2000, 9999
- Quiver: 30, 60, 100
- Bomb Bags: 30, 60
- Seed Bag: 50

The CT exposes one bomb bag capacity byte, so all three bomb slots currently share that CT-backed capacity selector. The seed bag capacity is fixed because the CT table does not expose a separate seed capacity offset.

Targets initialize from the current in-memory value after a successful scan. Lock mode writes only clamped values. Current health is also clamped to maximum health.

## Progression Initialization

The trainer does not enforce story progression. It does not care whether Link has legitimately unlocked an item, sword, shield, armor, or upgrade.

Inventory and Equipment editing are blocked only when Twilight Princess HD has not initialized the underlying memory structures yet. This can happen at the very beginning of the game: player stats such as health and rupees are readable, but inventory slots and equipment ownership bytes may still be empty or immediately restored by game code.

Inventory is considered initialized once any of the 24 CT-backed inventory slots at `_playerbase+0x258` through `_playerbase+0x26F` contains a real item instead of `255` / `Nothing`.

Equipment is considered initialized once the CT-backed ownership bytes or equipped gear bytes are no longer all empty/default:

- Armor ownership byte: `_playerbase+0x28D`
- Equipment ownership byte: `_playerbase+0x28E`
- Master Sword Infused bit: `_playerbase+0x292` bit 1
- Equipped armor/sword/shield: `_playerbase+0x1D1`, `_playerbase+0x1D2`, `_playerbase+0x1D3`

Once a structure is initialized, the trainer removes the initialization restriction. Users may edit supported equipment values regardless of story progress. Some inventory display/fixed slots remain read-only until the real ownership or progression flags are identified. The top bar shows **Player Data**, **Inventory**, and **Equipment** readiness after **Attach / Rescan**.

Progression diagnostics are shown on the Debug tab and written to `logs/progression.log` after each **Attach / Rescan**.

The Debug tab also includes a **Research** panel for finding real item ownership/progression flags. Enter a `_playerbase`-relative offset, read the current byte, optionally auto-refresh while playing, capture a before snapshot, and compare after an in-game event or upgrade.

## Inventory Editor

The Inventory tab reads the 24 CT-backed inventory bytes from `_playerbase+0x258` through `_playerbase+0x26F`. These bytes are not treated as arbitrary free bag slots. Observed behavior shows at least some of them are fixed item/UI slots that TPHD manages directly and may restore after writes.

The normal **Inventory Items** view now treats known fixed slots as read-only detected state. For now, the first detected field is:

- **Fishing Rod**: slot 21, `_playerbase+0x26C`
- Known same-family values: Fishing Rod (Lure), Fishing Rod (Bobber), Fishing Rod + Earring, Fishing Rod With Worm, Fishing Rod (Bobber) + Earring, Fishing Rod (Lure) + Worm + Earring

Observed Fishing Rod writes revert even when using same-family values, so the normal editor disables writes and shows: **This field appears game-managed. Real ownership/progression flags are not identified yet.**

The original 24-byte view is still available under **Advanced / Raw Inventory (Unsafe)**. Raw mode shows every slot number, CT offset, current item ID, current item name, management status, and the broad CT dropdown. **Clear** writes `255` (`Nothing`). **Apply** writes only the selected item ID. Raw writes are disabled until **Enable unsafe raw inventory writes** is checked. Raw mode is for diagnostics and should not be used as the primary way to replace fixed/game-managed slots.

Only a conservative set of CT dropdown items is exposed in raw mode by default: Nothing, usable items, bottle contents, and quest-related items listed in the CT table. Dangerous or unusable item IDs are intentionally omitted until they can be put behind a separate advanced/unsafe workflow.

Progression-sensitive warning: the trainer does not enforce story legitimacy once inventory is initialized, but some items may still be ignored by the game or affect story state if added before Twilight Princess HD expects them.

Inventory writes include diagnostics to help distinguish a failed external write from the game overwriting the slot afterward. After **Apply** or **Clear**, the trainer reads the exact byte immediately, then again after 250ms and 1000ms. Results are shown on the Debug tab and written to `logs/inventory.log`. The advanced **Hold inventory value for 2 seconds after Apply** checkbox is off by default and repeatedly writes the selected byte for two seconds to test whether gameplay code is restoring the old value.

If all 24 slots read as `255` / `Nothing`, the trainer treats inventory as not initialized rather than as an error. Apply/Clear are disabled and the Inventory tab asks you to progress until Link has a real item, such as the Fishing Rod, Slingshot, or Lantern. The advanced **Allow editing uninitialized inventory** override is off by default.

## Equipment Editor

The Equipment tab reads CT-derived equipment bytes from TPHD 2.2.CT:

- Equipped armor: `_playerbase+0x1D1`
- Equipped sword: `_playerbase+0x1D2`
- Equipped shield: `_playerbase+0x1D3`

It also reads ownership flags for Magic Armor, Zora Armor, Hero's Clothes, Ordon Sword, Master Sword, Ordon Shield, Wooden Shield, Hylian Shield, and Master Sword Infused. These flags are one-bit values inside the CT-backed bytes at `_playerbase+0x28D`, `_playerbase+0x28E`, and `_playerbase+0x292`.

Equipment writes are guarded by the **Advanced equipment editing** checkbox, which is off by default. If the equipment structure is not initialized, writes also require the **Allow editing uninitialized equipment** override. Refreshing or attaching only reads equipment. The trainer never writes equipment on attach, rescan, or refresh.

After an equipment write, the trainer immediately reads back the exact byte, then reads again after 250ms and 1000ms. Diagnostics are shown on the Debug tab and written to `logs/equipment.log`. If the immediate readback does not match, the UI reports **Write failed or wrong address.** If the game later restores another value, it reports **Write succeeded, but game reverted it.**

Early-game uninitialized equipment structures and some active game states can reject or overwrite equipment changes. This is reported as a write/revert diagnostic, not as a story progression rule. Test equipment editing on copied saves or save states first.

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
- `TphdCemuTrainer/Memory/ProgressionStateService.cs`: inventory/equipment initialization detection and raw diagnostic reads
- `TphdCemuTrainer/Memory/InventoryMemoryService.cs`: CT-backed inventory slot reads/writes
- `TphdCemuTrainer/Memory/EquipmentMemoryService.cs`: CT-backed equipment byte and ownership flag reads/writes
- `TphdCemuTrainer/Cheats/CheatCatalog.cs`: CT-derived cheat and capacity definitions
- `TphdCemuTrainer/Cheats/InventoryDefinitions.cs`: safe CT inventory item dropdown definitions
- `TphdCemuTrainer/Cheats/InventoryFixedSlotDefinition.cs`: fixed/game-managed inventory slot metadata
- `TphdCemuTrainer/Cheats/EquipmentDefinitions.cs`: TPHD 2.2.CT equipment dropdown and flag definitions
- `TphdCemuTrainer/Cheats/FutureFeatureCatalog.cs`: reserved trainer/save-editor feature groups
- `TphdCemuTrainer/ViewModels/`: UI-facing value and capacity models
- `TphdCemuTrainer/MainWindow.xaml`: tabbed WPF trainer UI

## Known Limitations

- The player base scan depends on the CT table AOB. It may fail on unsupported game revisions, different memory layouts, or if gameplay is not loaded.
- Missing player data is handled as a rescan state, not an application failure.
- Story flags, hidden skills, and quest items are laid out for future expansion but not yet written.
- Inventory-specific editors write constrained variants for known fixed slots. Advanced/raw inventory writes raw item IDs and may be reverted by game-managed slots.
- Equipment editing writes raw CT-derived bytes and bits. The trainer blocks only uninitialized equipment structures by default, not story-illegitimate equipment choices.
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

**Inventory or Equipment says Not Initialized**

This is not a story lock. TPHD has not created or stabilized that memory structure yet. Progress until Link obtains a real inventory item or equipment, then click **Attach / Rescan**. Advanced overrides exist for diagnostics, but they are off by default.

**Equipment write succeeded, but game reverted it**

The external write worked, but Twilight Princess HD restored the value afterward. This commonly means the current story state, equipped item state, or active scene is overwriting the equipment byte or ownership flag. Try after progressing further, changing areas, or testing on a copied save/save state.

**Access denied**

Run the trainer with the same privilege level as Cemu. If Cemu is elevated, the trainer likely needs to be elevated too.

## Safety / Legal Note

This project is intended for personal offline emulator use only. Do not use it with online services, shared competitive environments, or software you do not have permission to inspect or modify in memory.

## Credits

Cheat table offsets, AOB patterns, and behavior references are credited to **toto621**, author of `Zelda_TP_HD_Mega_Trainer (by toto621).ct`.
