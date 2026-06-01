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
- **Inventory**: ownership-first item detection, read-only current slot view, and unsafe raw CT writes for research
- **Equipment**: ownership flag editor with read-only current equipped armor/sword/shield diagnostics
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
- Read-only inventory slot detection at `_playerbase+0x258` through `_playerbase+0x26F`
- Unsafe raw inventory slot writes at `_playerbase+0x258` through `_playerbase+0x26F`
- Read-only equipped armor at `_playerbase+0x1D1`
- Read-only equipped sword at `_playerbase+0x1D2`
- Read-only equipped shield at `_playerbase+0x1D3`
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

Once a structure is initialized, the trainer removes the initialization restriction for supported writes. Equipment writes are ownership-flag writes only. Inventory ownership writes remain disabled until real ownership or progression flags are identified. The top bar shows **Player Data**, **Inventory**, and **Equipment** readiness after **Attach / Rescan**.

Progression diagnostics are shown on the Debug tab and written to `logs/progression.log` after each **Attach / Rescan**.

The Debug tab also includes research tools for finding real item ownership/progression flags. The single-byte watcher can read one `_playerbase`-relative offset while playing. The **Research Range Snapshot** tool captures a read-only byte range before an in-game event, then compares the current range afterward and highlights changed bytes.

Default range presets:

- Inventory Slots: start `0x258`, length `0x18`
- Equipment/Ownership: start `0x28D`, length `0x08`
- Equipped Gear: start `0x1D1`, length `0x03`

Range comparisons decode known item IDs with the inventory item catalog, so values such as `75` / Slingshot, `74` / Fishing Rod (Lure), and `255` / Nothing are easier to spot. Comparisons are written to `logs/research.log`.

The **Research Snapshot Library** saves named permanent snapshots as JSON under `logs/snapshots/`. Each snapshot stores a name, timestamp, notes, game-state description, and one or more captured ranges. The browser shows snapshot name, date, notes, and range count, and lets you load snapshots as A/B, delete them, or compare them offline without Cemu running.

Snapshot-to-snapshot comparisons display offset, Snapshot A value, Snapshot B value, difference, known item decode, and changed status. The discovery report calls out potential item discoveries and possible ownership/progression flags. Comparison exports are written to `logs/research/` as JSON and CSV.

## Inventory Editor

The Inventory tab reads the 24 CT-backed visible inventory bytes from `_playerbase+0x258` through `_playerbase+0x26F`. Research indicates these bytes are game-managed display/current-state fields, not arbitrary bag slots. TPHD may rebuild them from authoritative ownership or progression flags and may immediately revert direct writes.

The normal **Owned / Unlocked Inventory Items** section is ownership-first. It lists important items such as Fishing Rod, Slingshot, Lantern, Hero's Bow, Gale Boomerang, Clawshot, Double Clawshots, Spinner, Dominion Rod, Ball and Chain, Hawkeye, Horse Call, and Bottles. Until a real ownership flag offset/bit is identified, each item is marked **Not implemented**, editing is disabled, and the UI shows **Ownership flag unknown**. The detected state is read from visible CT slots only.

The **Current Inventory Slots (Read Only)** section shows all 24 CT-derived bytes: slot, offset, raw item ID, decoded item name, and notes. Slot 21 / `_playerbase+0x26C` is labeled as the Fishing Rod field with the note: **Game-managed. Direct writes revert. Real ownership/progression flag not identified yet.**

The trainer should eventually grant inventory through ownership/progression flags rather than raw slot forcing. Raw slots are still useful for detection and research, especially when comparing before/after saves or snapshots.

The **Advanced / Raw Inventory Writes (Unsafe)** section keeps the existing raw write diagnostics behind **Enable unsafe raw inventory writes**. Raw mode exposes the broad CT dropdown, **Apply**, and **Clear** for research only. Raw inventory writes are experimental; TPHD may immediately revert invalid writes. Use only on copied saves or save states.

Only a conservative set of CT dropdown items is exposed in unsafe raw mode by default: Nothing, usable items, bottle contents, and quest-related items listed in the CT table. Dangerous or unusable item IDs are intentionally omitted until they can be put behind a separate advanced/unsafe workflow.

Unsafe raw writes include diagnostics to help distinguish a failed external write from the game overwriting the slot afterward. After **Apply** or **Clear**, the trainer reads the exact byte immediately, then again after 250ms and 1000ms. Results are shown on the Debug tab and written to `logs/inventory.log`. The advanced **Hold inventory value for 2 seconds after Apply** checkbox is off by default and repeatedly writes the selected byte for two seconds to test whether gameplay code is restoring the old value.

Read-only inventory state refreshes are also logged to `logs/inventory.log`. If all 24 slots read as `255` / `Nothing`, the trainer treats inventory as not initialized rather than as an error. Unsafe Apply/Clear remain disabled unless **Enable unsafe raw inventory writes** is checked and the advanced **Allow editing uninitialized inventory** override is enabled.

After future ownership changes are implemented, TPHD may not refresh an already-open in-game inventory menu immediately. Close and reopen the in-game inventory menu to see newly granted items.

## Equipment Editor

The Equipment tab reads CT-derived equipment bytes from TPHD 2.2.CT:

- Equipped armor: `_playerbase+0x1D1`
- Equipped sword: `_playerbase+0x1D2`
- Equipped shield: `_playerbase+0x1D3`

It also reads ownership flags for Magic Armor, Zora Armor, Hero's Clothes, Ordon Sword, Master Sword, Ordon Shield, Wooden Shield, Hylian Shield, and Master Sword Infused. These flags are one-bit values inside the CT-backed bytes at `_playerbase+0x28D`, `_playerbase+0x28E`, and `_playerbase+0x292`.

Ownership flags appear to be the authoritative equipment data. The trainer grants or removes ownership only; current equipped armor, sword, and shield values are game-managed and displayed read-only for diagnostics.

Ownership checkboxes edit desired ownership separately from detected ownership. Background refreshes update the detected state without wiping pending checkbox changes. Click **Apply Ownership Changes** to write the desired ownership flags and verify readback.

After granting ownership, equip the item through Twilight Princess HD's own equipment screen. Direct equipped-value forcing is not supported because the game restores those values internally. Refreshing or attaching only reads equipment, and the trainer never writes equipment on attach, rescan, or refresh.

Ownership changes are logged to `logs/equipment.log`, along with current equipped values during refresh and after ownership applies. If the equipment screen is already open when ownership changes are applied, TPHD may not immediately refresh its displayed equipment. Close and reopen the in-game equipment screen to see newly granted equipment. This is expected game behavior, not a trainer bug.

Early-game uninitialized equipment structures may still require the **Allow editing uninitialized equipment** override for diagnostics. Test equipment ownership changes on copied saves or save states first.

## Attach / Rescan Performance

The first player-base scan may take longer because the trainer must enumerate Cemu memory regions and search for the CT AOB pattern. After a successful scan, the trainer caches the last PID, player base address, and matching region.

Later **Attach / Rescan** attempts validate the cached player base first, then scan the cached region before falling back to the filtered full scan. The scanner only considers committed readable memory, skips `PAGE_NOACCESS` and `PAGE_GUARD`, prefers private/mapped data regions, and avoids image/code regions where possible.

Scan diagnostics are shown in the Debug tab and written to `logs/scan.log`, including process discovery time, handle-open time, region enumeration time, scan time, regions scanned/skipped, bytes scanned, cache status, and match address.

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
- `TphdCemuTrainer/Memory/AobScanner.cs`: external AOB scanner with wildcard-byte support, region filtering, cache validation, and scan diagnostics
- `TphdCemuTrainer/Memory/BigEndianMemory.cs`: big-endian read/write helpers
- `TphdCemuTrainer/Memory/ProgressionStateService.cs`: inventory/equipment initialization detection and raw diagnostic reads
- `TphdCemuTrainer/Memory/InventoryMemoryService.cs`: CT-backed inventory slot reads plus unsafe raw research writes
- `TphdCemuTrainer/Memory/EquipmentMemoryService.cs`: CT-backed equipment byte and ownership flag reads/writes
- `TphdCemuTrainer/Cheats/CheatCatalog.cs`: CT-derived cheat and capacity definitions
- `TphdCemuTrainer/Cheats/InventoryDefinitions.cs`: safe CT inventory item dropdowns, ownership candidates, and managed slot metadata
- `TphdCemuTrainer/Cheats/InventoryOwnershipDefinition.cs`: inventory ownership/progression candidate metadata
- `TphdCemuTrainer/Cheats/InventoryFixedSlotDefinition.cs`: known fixed/game-managed inventory slot metadata
- `TphdCemuTrainer/Cheats/EquipmentDefinitions.cs`: TPHD 2.2.CT equipment dropdown and flag definitions
- `TphdCemuTrainer/Cheats/FutureFeatureCatalog.cs`: reserved trainer/save-editor feature groups
- `TphdCemuTrainer/ViewModels/`: UI-facing value and capacity models
- `TphdCemuTrainer/MainWindow.xaml`: tabbed WPF trainer UI

## Known Limitations

- The player base scan depends on the CT table AOB. It may fail on unsupported game revisions, different memory layouts, or if gameplay is not loaded.
- The first scan can still be slower than later rescans because no cache has been validated yet.
- Missing player data is handled as a rescan state, not an application failure.
- Story flags, hidden skills, and quest items are laid out for future expansion but not yet written.
- Normal inventory editing is read-only until real ownership/progression flags are identified. Unsafe raw inventory writes may be reverted by game-managed slots.
- Equipment editing grants ownership flags only. Current equipped armor, sword, and shield are game-managed and read-only in the trainer.
- Values are simple external memory edits. They do not patch game logic.
- If Cemu runs as administrator, the trainer may also need to run as administrator.

## Troubleshooting

**Cemu.exe is not running**

Start Cemu before clicking **Attach / Rescan**.

**Load into gameplay and rescan**

Cemu was found, but the player data AOB was not. Load Twilight Princess HD into active gameplay, then click **Attach / Rescan** again.

**Attach / Rescan is slow**

The first scan may take longer because the trainer has no validated cache yet. Later scans should be faster after a successful match. Check the Debug tab or `logs/scan.log` for scan timing, cache status, region counts, and bytes scanned.

**Values clamp lower than expected**

Check the selected capacity dropdown. Ammo, bombs, seeds, rupees, and health are clamped to the active capacity or maximum health before writing.

**Values do not change**

Confirm the trainer is connected and showing a player base address. Some values may update only after the game refreshes inventory or HUD state.

**Unsafe inventory writes revert**

This is expected for game-managed visible inventory slots. Raw inventory writes are for research on copied saves; normal inventory ownership writes are disabled until the real ownership/progression flags are identified.

**Inventory or Equipment says Not Initialized**

This is not a story lock. TPHD has not created or stabilized that memory structure yet. Progress until Link obtains a real inventory item or equipment, then click **Attach / Rescan**. Advanced overrides exist for diagnostics, but they are off by default.

**Newly granted equipment is not visible in the in-game equipment screen**

If the equipment screen was already open when ownership changed, TPHD may not refresh the displayed equipment immediately. Close and reopen the in-game equipment screen. The trainer grants ownership flags only; the player still equips owned items using the game's own menu.

**Access denied**

Run the trainer with the same privilege level as Cemu. If Cemu is elevated, the trainer likely needs to be elevated too.

## Safety / Legal Note

This project is intended for personal offline emulator use only. Do not use it with online services, shared competitive environments, or software you do not have permission to inspect or modify in memory.

## Credits

Cheat table offsets, AOB patterns, and behavior references are credited to **toto621**, author of `Zelda_TP_HD_Mega_Trainer (by toto621).ct`.
