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
- **Inventory**: ownership-first item detection, fixed-slot experimental grant/remove for mapped visible slots, Bottle Editor v1, read-only current slot view, mapping research, unsafe removal testing, and unsafe raw CT writes for research
- **Equipment**: ownership flag editor with read-only current equipped armor/sword/shield diagnostics
- **Ammo & Upgrades**: wallet, quiver, bomb bag, and seed capacity-aware edits
- **Collectibles**: verified Poe Souls editing, full Golden Bugs ownership editor, Golden Bugs research tools, and health/heart summaries
- **Story Flags**: reserved progression flags
- **Hidden Skills**: disabled research preview for the seven known skills
- **Quest Items**: reserved quest item/progression tracking
- **Debug**: player base, AOB pattern, progression diagnostics, research snapshots, Hidden Skills Research, Ownership Discovery Mode, raw CT-backed values, and future memory tools

The top bar includes a **Dark Mode** toggle. The app defaults to Light mode and stores the local preference under the user's AppData folder when possible.

## Implemented Memory Edits

- Current health
- Maximum health
- Rupees
- Lantern oil
- Arrows
- Bomb slots 1-3
- Seeds
- Poe souls at `_playerbase+0x2C8`, clamped to 0-60 with readback diagnostics
- Wallet capacity
- Quiver capacity
- Bomb bag capacity
- Golden Bugs ownership editor for all 24 bugs at `_playerbase+0x2A1` through `_playerbase+0x2A3`
- Read-only inventory slot detection at `_playerbase+0x258` through `_playerbase+0x26F`
- Experimental bottle slot editing at `_playerbase+0x263` through `_playerbase+0x266`
- Unsafe inventory removal testing at `_playerbase+0x258` through `_playerbase+0x26F`
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

The trainer also tracks a separate **Ownership Edits** state. This is intentionally separate from player data and memory initialization because early Ordon Village intro saves may expose readable player memory and may even accept writes briefly, while TPHD still ignores or rebuilds ownership state internally.

For now this is a conservative heuristic, not a mapped story flag:

- No player data: **Unknown**
- Player data with inventory/equipment not both initialized: **Likely No**
- Inventory and equipment both initialized: **Likely Yes**
- Manual override checked: **Likely Yes**

Starter saves should be treated as **Likely No**. Forest Temple and later saves are known to honor equipment ownership edits and should normally show **Likely Yes**. If automatic detection is too conservative, the UI has **I am past the Ordon Village intro arc** and **Allow ownership edits before intro completion** overrides.

Once a structure is initialized and ownership edits are likely accepted, the trainer removes the initialization restriction for supported writes. Equipment writes are ownership-flag writes only. Inventory ownership writes remain disabled until real ownership or progression flags are identified. The top bar shows **Player Data**, **Inventory**, **Equipment**, and **Ownership Edits** readiness after **Attach / Rescan**.

Progression diagnostics are shown on the Debug tab and written to `logs/progression.log` after each **Attach / Rescan**. The diagnostics include player data, memory initialization, ownership-edit acceptance, and the detection reason.

The Debug tab also includes research tools for finding real item ownership/progression flags. The single-byte watcher can read one `_playerbase`-relative offset while playing. The **Research Range Snapshot** tool captures a read-only byte range before an in-game event, then compares the current range afterward and highlights changed bytes.

Default range presets:

- Inventory Slots: start `0x258`, length `0x18`
- Equipment/Ownership: start `0x28D`, length `0x08`
- Equipped Gear: start `0x1D1`, length `0x03`

Range comparisons decode known item IDs with the inventory item catalog, so values such as `75` / Slingshot, `74` / Fishing Rod (Lure), and `255` / Nothing are easier to spot. Comparisons are written to `logs/research.log`.

The **Research Snapshot Library** saves named permanent snapshots as JSON under `logs/snapshots/`. Each snapshot stores a name, timestamp, notes, game-state description, and one or more captured ranges. The browser shows snapshot name, date, notes, and range count, and lets you load snapshots as A/B, delete them, or compare them offline without Cemu running.

Snapshot-to-snapshot comparisons display offset, Snapshot A value, Snapshot B value, difference, known item decode, and changed status. The discovery report calls out potential item discoveries and possible ownership/progression flags. Comparison exports are written to `logs/research/` as JSON and CSV.

The Debug tab also includes **Ownership Discovery Mode** for comparing temple/progression saves while hunting authoritative inventory ownership flags. It captures the same read-only ranges for Save A and Save B:

- Inventory Slots: `0x258`, length `0x18`
- Equipment Ownership: `0x28D`, length `0x08`
- Candidate Ownership Region: `0x240`, length `0xA0`
- Collectibles: `0x2A1`, length `0x30`

The Ownership Diff Report shows offset, before value, after value, changed status, score, and potential meaning. Bytes score higher when they changed, are outside the visible inventory slots, and Save B was captured after reload/persistence. This is intended to help identify candidates for Slingshot, Lantern, Bow, Bottle, and dungeon item ownership without manually searching offsets. It does not write memory. Reports export to `logs/research/` as JSON and CSV.

## Inventory Editor

The Inventory tab reads the 24 CT-backed visible inventory bytes from `_playerbase+0x258` through `_playerbase+0x26F`. Research indicates these bytes are game-managed display/current-state fields, not arbitrary bag slots. TPHD may rebuild them from authoritative ownership or progression flags and may immediately revert direct writes.

The normal **Owned / Unlocked Inventory Items** section is ownership-first, but the checked CT source still does not identify authoritative inventory ownership/progression flags:

- **Detected**: visible-slot detection for the listed item families.
- **Desired**: used by the fixed-slot experimental editor for mapped visible slots, and reserved for future real ownership flags.
- **Dirty**: indicates desired state differs from the current visible-slot detection or future mapped ownership state.

When mapped flags are added later, **Apply Inventory Ownership Changes** will write desired ownership flags in bulk. The trainer writes ownership/progression flags only; it does not fake ownership by writing raw visible inventory slots. Future mapped writes will be verified with immediate and delayed readback, then logged to `logs/inventory-ownership.log`.

The checked CT source currently does not expose real ownership flag offsets/bits for Fishing Rod, Slingshot, Lantern, Hero's Bow, Gale Boomerang, Clawshot, Double Clawshots, Spinner, Dominion Rod, Ball and Chain, Hawkeye, Horse Call, or Bottles. Normal ownership editing remains disabled for those rows until real flags are mapped.

The **Current Inventory Slots (Read Only)** section shows all 24 CT-derived bytes: slot, offset, raw item ID, decoded item name, and notes. Slot 21 / `_playerbase+0x26C` is labeled as the Fishing Rod field with the note: **Game-managed. Direct writes revert. Real ownership/progression flag not identified yet.**

The trainer should eventually grant inventory through ownership/progression flags rather than raw slot forcing. Raw slots are still useful for detection and research, especially when comparing before/after saves or snapshots.

The **Bottle Editor / Experimental** section edits only the four currently mapped bottle-content slots:

- Bottle Slot 1: `_playerbase+0x263`
- Bottle Slot 2: `_playerbase+0x264`
- Bottle Slot 3: `_playerbase+0x265`
- Bottle Slot 4: `_playerbase+0x266`

Bottle ownership is not fully understood yet. These bytes are currently believed to represent visible bottle contents, not necessarily authoritative bottle ownership. The editor does not automatically create bottle ownership, does not modify non-bottle inventory slots, and does not alter assigned button slots.

These values are confirmed from live TPHD memory testing and are enabled by default:

- `96` / `0x60`: Empty Bottle
- `97` / `0x61`: Milk
- `100` / `0x64`: Red Potion
- `101` / `0x65`: Milk (1/2)
- `102` / `0x66`: Lantern Oil
- `108` / `0x6C`: Fairy
- `115` / `0x73`: Great Fairy's Tears
- `116` / `0x74`: Worm
- `118` / `0x76`: Bee Larvae
- `119` / `0x77`: Rare Chu Jelly
- `120` / `0x78`: Red Chu Jelly
- `121` / `0x79`: Blue Chu Jelly
- `122` / `0x7A`: Green Chu Jelly
- `123` / `0x7B`: Yellow Chu Jelly
- `124` / `0x7C`: Purple Chu Jelly
- `255` / `0xFF`: Nothing / No Bottle

Bottle Editor writes happen only when **Apply**, **Set Nothing**, or **Restore** is clicked. Each write reads the previous value, writes one byte, verifies immediate readback, verifies again after 250ms and 1000ms, refreshes bottle state, and logs to `logs/bottle-editor.log`. Restore buffers are per bottle slot and session-only. This feature is experimental; use copied saves or save states.

No unconfirmed, internal, or research-only bottle values are exposed in Bottle Editor v1. The confirmed bottle values are also documented in `docs/research/BottleResearch.md`.

Current removal findings: removing visible inventory slot values can remove items from the in-game inventory, and some removals can persist after save/reload. Button assignments are separate from visible inventory slots, so a removed item assigned to Y/X/R may remain usable until manually unequipped or replaced. Removing all primary/progression items can also make bombs or bottles unreachable in the in-game inventory menu.

The **Inventory Mapping Mode (Research Only)** section documents the visible inventory layout without writing memory. It shows slot, offset, raw value, decoded item, inferred visual group, editable research group, row/column notes, and freeform notes. Exports are written to `logs/research/inventory-mapping.json` and `logs/research/inventory-mapping.csv`.

The **Advanced / Inventory Removal Testing (Unsafe)** section is research-only. It lists detected visible inventory items, captures each slot's previous byte, writes `0xFF` / `Nothing` to that visible CT slot, verifies immediate readback, refreshes inventory, and can restore the captured byte. Restore buffers are per-slot and session-only: they remain through inventory refreshes until that slot is restored, **Clear Restore Buffer** is clicked, or the app closes. **Restore All Removed Items** attempts to restore every buffered slot in the current trainer session. Results are shown in Debug under **Inventory Removal Diagnostics** and written to `logs/inventory-removal.log`.

Inventory removal writes directly to visible game-managed slots. TPHD may revert the value, rebuild it from authoritative state, or leave the save in an unexpected state. Use only on copied saves or save states. This tool is intended to help discover which visible values are authoritative and which are rebuilt by the game. It does not add items, does not write item IDs other than `0xFF` for removal, and does not write ownership/progression flags.

The **Fixed Slot Inventory Editor / Experimental** option is research-only. It edits fixed visible inventory slots, not story progression or authoritative ownership flags. When enabled, a checked inventory item row attempts to write that row's detected or static known item ID back to its fixed visible slot; an unchecked row writes `0xFF` / `Nothing`. If the item is currently detected, the trainer uses the detected slot/value. If it is absent but has a known fixed/special static mapping, the trainer still shows the static slot and can test writing that value after a restart.

Current confirmed fixed-slot inventory mappings:

- **Gale Boomerang**: `_playerbase+0x258` / `64`
- **Lantern**: `_playerbase+0x259` / `72`
- **Spinner**: `_playerbase+0x25A` / `65`
- **Iron Boots**: `_playerbase+0x25B` / `69`
- **Hero's Bow**: `_playerbase+0x25C` / `67`
- **Hawkeye**: `_playerbase+0x25D` / `62`
- **Ball and Chain**: `_playerbase+0x25E` / `66`
- **Ghost Lantern**: `_playerbase+0x25F` / `232`
- **Dominion Rod**: `_playerbase+0x260` / `70`
- **Clawshot**: `_playerbase+0x261` / `68`
- **Double Clawshots**: `_playerbase+0x262` / `71`
- **Fishing Rod + Earring**: `_playerbase+0x26C` / `92`
- **Horse Call**: `_playerbase+0x26D` / `132`
- **Slingshot**: `_playerbase+0x26F` / `75`

Forest Temple save testing has confirmed grant/remove for the early and midgame fixed-slot items above. Ghost Lantern is a late-game/story-specific item, so granting it may not fully replicate normal progression. Dominion Rod writes grant/remove the red unpowered variant; story flags or related state may still be needed before it behaves like the fully powered item.

Clawshot and Double Clawshots are separate visible inventory entries and can both be granted, assigned, and used, but enabling both creates an invalid inventory layout state. Live testing showed Dominion Rod can disappear from the visible inventory until the single Clawshot is removed. The trainer treats Clawshot and Double Clawshots as mutually exclusive in the fixed-slot editor: enabling one automatically disables the other, and Apply enforces the same rule as a backstop. Safeguard actions are logged to `logs/inventory-checkbox-testing.log`.

This fixed-slot mode is intentionally not arbitrary item replacement. Some items may appear but be unusable until story flags or related state are set, and button assignments are separate from inventory slots. The trainer verifies immediate readback plus 250ms and 1000ms delayed reads, labels values that revert as **Reverted by game**, logs whether the mapping source was **Detected** or **Static**, and writes diagnostics to `logs/inventory-checkbox-testing.log`. Bottles are not enabled for this mode.

The **Advanced / Raw Inventory Writes (Unsafe)** section keeps the existing raw write diagnostics behind **Enable unsafe raw inventory writes**. Raw mode exposes the broad CT dropdown, **Apply**, and **Clear** for research only. Raw inventory writes are experimental; TPHD may immediately revert invalid writes. Use only on copied saves or save states.

Only a conservative set of CT dropdown items is exposed in unsafe raw mode by default: Nothing, usable items, bottle contents, and quest-related items listed in the CT table. Dangerous or unusable item IDs are intentionally omitted until they can be put behind a separate advanced/unsafe workflow.

Unsafe raw writes include diagnostics to help distinguish a failed external write from the game overwriting the slot afterward. After **Apply** or **Clear**, the trainer reads the exact byte immediately, then again after 250ms and 1000ms. Results are shown on the Debug tab and written to `logs/inventory.log`. The advanced **Hold inventory value for 2 seconds after Apply** checkbox is off by default and repeatedly writes the selected byte for two seconds to test whether gameplay code is restoring the old value.

Read-only inventory state refreshes are also logged to `logs/inventory.log`. If all 24 slots read as `255` / `Nothing`, the trainer treats inventory as not initialized rather than as an error. Unsafe Apply/Clear remain disabled unless **Enable unsafe raw inventory writes** is checked and the advanced **Allow editing uninitialized inventory** override is enabled.

After inventory ownership changes are applied for any future mapped flags, TPHD may not refresh an already-open in-game inventory menu immediately. Close and reopen the in-game inventory menu to see newly granted items.

Inventory remains read-only in normal mode even when the Inventory state says **Ready**. That is expected: **Ready** means the visible slot bytes can be read, not that authoritative ownership flags have been mapped.

## Collectibles

The Collectibles tab currently supports stable edits only:

- **Poe Souls**: editable CT-backed byte at `_playerbase+0x2C8`, clamped to `0-60`, verified after write, refreshed after apply, and logged to `logs/collectibles.log`.
- **Golden Bugs**: editable ownership bits for all 24 bugs at `_playerbase+0x2A1` through `_playerbase+0x2A3`, with detected/desired state, Apply, Add All, Clear All, Restore Previous, readback verification, and logging to `logs/golden-bugs-editor.log`.
- **Health summary**: current health quarters and maximum health quarters are shown for reference. Health editing remains in the General tab.

TPHD has 24 Golden Bugs: 12 species with male and female variants. Live memory testing confirms that visible Golden Bug ownership uses the 24 bits across `_playerbase+0x2A1`, `_playerbase+0x2A2`, and `_playerbase+0x2A3`. The normal Golden Bugs editor does not write `_playerbase+0x2A4`.

The editor shows the current owned count out of 24, raw bytes `0x2A1-0x2A3`, and one row per bug with offset, bit, current state, desired checkbox, and dirty indicator. Refresh updates detected state and preserves unsaved desired edits. The trainer never writes Golden Bugs on attach, rescan, or refresh.

**Apply Golden Bug Changes** writes only changed desired bits and preserves the rest of each byte. **Add All Golden Bugs** sets all 24 confirmed ownership bits. **Clear All Golden Bugs** clears all 24 confirmed ownership bits. **Restore Previous Bug State** restores the previous 3-byte state captured before the last editor write in the current trainer session. All editor writes verify immediate, 250ms, and 1000ms readbacks.

Confirmed Golden Bugs mapping:

- `0x2A1` bit `0`: Male Snail
- `0x2A1` bit `1`: Female Snail
- `0x2A1` bit `2`: Male Dragonfly
- `0x2A1` bit `3`: Female Dragonfly
- `0x2A1` bit `4`: Male Ant
- `0x2A1` bit `5`: Female Ant
- `0x2A1` bit `6`: Male Dayfly
- `0x2A1` bit `7`: Female Dayfly
- `0x2A2` bit `0`: Male Phasmid
- `0x2A2` bit `1`: Female Phasmid
- `0x2A2` bit `2`: Male Pill Bug
- `0x2A2` bit `3`: Female Pill Bug
- `0x2A2` bit `4`: Male Mantis
- `0x2A2` bit `5`: Female Mantis
- `0x2A2` bit `6`: Male Ladybug
- `0x2A2` bit `7`: Female Ladybug
- `0x2A3` bit `0`: Male Beetle
- `0x2A3` bit `1`: Female Beetle
- `0x2A3` bit `2`: Male Butterfly
- `0x2A3` bit `3`: Female Butterfly
- `0x2A3` bit `4`: Male Stag Beetle
- `0x2A3` bit `5`: Female Stag Beetle
- `0x2A3` bit `6`: Male Grasshopper
- `0x2A3` bit `7`: Female Grasshopper

Golden Bugs editing controls collection-screen ownership only. It does not edit Agitha reward or turn-in flags, which may be separate game state. Use copied saves or save states.

The **Advanced / Golden Bugs Research** section keeps the raw research tools. The before/after compare defaults to `_playerbase+0x2A1` length `0x04` and logs to `logs/golden-bugs-research.log`. The **Golden Bugs Bitfield Tester / Experimental** still displays all 32 bits across `0x2A1-0x2A4` for controlled research, logs to `logs/golden-bugs-bitfield-testing.log`, and exports reports to `logs/research/`. Use that advanced area for `0x2A4` investigation and reward/turn-in research, not normal ownership editing. Details are tracked in `docs/research/GoldenBugsResearch.md`.

## Hidden Skills Research

Hidden Skills are currently under investigation. The Hidden Skills tab shows a disabled **Hidden Skills (Research)** preview for:

- Ending Blow
- Shield Attack
- Back Slice
- Helm Splitter
- Mortal Draw
- Jump Strike
- Great Spin

The Debug tab includes **Hidden Skills Research**, a read-only range comparison workflow. It defaults to `_playerbase+0x200` length `0x100`, but the start offset and length are user-configurable. Captures are saved immediately to `logs/research/hidden-skills-captures/` so you can close Cemu or the trainer between Save A and Save B.

Suggested workflow:

1. Enter a capture label, such as `Forest Temple Ending Blow`.
2. Save Before Capture.
3. Close/reopen Cemu or switch saves if needed.
4. Load Before Capture from disk.
5. Learn one Hidden Skill or load the after-save.
6. Save After Capture, or load a previous After capture.
7. Compare Loaded Captures.
8. Export results.

Persistent capture filenames use `hidden-skills-before_<timestamp>_<optional-label>.json` and `hidden-skills-after_<timestamp>_<optional-label>.json`. Each capture stores timestamp, playerbase-relative start offset, length, raw bytes, and optional label.

The comparison table shows offset, before byte, after byte, before binary, after binary, changed bits, candidate score, and highlight labels for single-bit, persisted, or clustered changes. Report exports are written to `logs/research/hidden-skills-before.json`, `logs/research/hidden-skills-after.json`, `logs/research/hidden-skills-report.json`, and `logs/research/hidden-skills-report.csv`. Activity is logged to `logs/hidden-skills-research.log`.

The Hidden Skills research table can filter to likely candidates: single-bit changes, persisted changes, and rows with candidate score `>= 6`. You can pin offsets as manual Hidden Skill candidates; pinned offsets are included in the filtered view and candidate grouping. Nearby candidate offsets are grouped automatically using a 4-byte proximity window, then exported with **Export Candidate Groups** to `logs/research/hidden-skills-candidate-groups.json` and `logs/research/hidden-skills-candidate-groups.csv`.

No Hidden Skills editing is implemented yet. Details are tracked in `docs/research/HiddenSkillsResearch.md`.

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

Early-game Ordon Village intro saves may not honor ownership edits even if a byte write appears to succeed. The trainer disables equipment Apply by default when ownership edits are **Likely No** and shows: **This save appears to be before TPHD begins honoring ownership edits. Progress past the Ordon Village intro arc and rescan.** Forest Temple and later saves are known to honor equipment ownership edits.

If you know the save is past the intro but the heuristic is too conservative, check **I am past the Ordon Village intro arc**. For diagnostics on copied saves, **Allow ownership edits before intro completion** bypasses that guard. Early-game uninitialized equipment structures may still require the separate **Allow editing uninitialized equipment** override. Test equipment ownership changes on copied saves or save states first.

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
- `TphdCemuTrainer/Memory/ProgressionStateService.cs`: inventory/equipment initialization detection, ownership-edit acceptance heuristic, and raw diagnostic reads
- `TphdCemuTrainer/Memory/InventoryMemoryService.cs`: CT-backed inventory slot reads, unsafe raw research writes, and mapped inventory ownership flag reads/writes
- `TphdCemuTrainer/Memory/EquipmentMemoryService.cs`: CT-backed equipment byte and ownership flag reads/writes
- `TphdCemuTrainer/Cheats/CheatCatalog.cs`: CT-derived cheat and capacity definitions
- `TphdCemuTrainer/Cheats/BottleDefinitions.cs`: experimental bottle slot and confirmed bottle content definitions
- `TphdCemuTrainer/Cheats/GoldenBugsDefinitions.cs`: confirmed Golden Bugs bit mappings and reference bug names
- `TphdCemuTrainer/Cheats/InventoryDefinitions.cs`: safe CT inventory item dropdowns, ownership candidates, and managed slot metadata
- `TphdCemuTrainer/Cheats/InventoryOwnershipDefinition.cs`: inventory ownership/progression candidate metadata
- `TphdCemuTrainer/Cheats/InventoryFixedSlotDefinition.cs`: known fixed/game-managed inventory slot metadata
- `TphdCemuTrainer/Cheats/EquipmentDefinitions.cs`: TPHD 2.2.CT equipment dropdown and flag definitions
- `TphdCemuTrainer/Cheats/FutureFeatureCatalog.cs`: reserved trainer/save-editor feature groups
- `TphdCemuTrainer/Research/`: named memory snapshots plus JSON/CSV comparison and ownership discovery exports
- `TphdCemuTrainer/ViewModels/`: UI-facing value and capacity models
- `docs/research/HiddenSkillsResearch.md`: current Hidden Skills mapping notes and workflow
- `TphdCemuTrainer/MainWindow.xaml`: tabbed WPF trainer UI

## Known Limitations

- The player base scan depends on the CT table AOB. It may fail on unsupported game revisions, different memory layouts, or if gameplay is not loaded.
- The first scan can still be slower than later rescans because no cache has been validated yet.
- Missing player data is handled as a rescan state, not an application failure.
- Story flags and quest items are laid out for future expansion but not yet written.
- Hidden Skills are research-only. The trainer can compare configurable before/after ranges, but no skill ownership mappings are confirmed and no editing is implemented.
- Inventory ownership editing uses detected/desired/apply where real flags are mapped. Current listed inventory ownership flags are not identified yet.
- Bottle Editor v1 edits visible bottle-content slots only; bottle ownership and unconfirmed bottled item raw values are still being researched.
- General inventory ownership/progression editing is not solved yet. Static fixed-slot grant/remove is confirmed only for specific visible-slot item/slot pairs.
- The fixed-slot experimental editor uses detected or static visible-slot mappings for known fixed/special items only. These are confirmed slot writes for tested items, not confirmed story progression or authoritative ownership flags.
- Clawshot and Double Clawshots are mutually exclusive in the fixed-slot editor because enabling both can hide or displace another progression item such as Dominion Rod.
- Arbitrary visible-slot replacement is not supported. Use the known static item/slot pairs only.
- Inventory removal testing is unsafe/research-only and may be reverted by game-managed visible slots.
- Inventory removal restore buffers are not saved to disk and are lost when the app closes.
- Button assignments are not cleared by inventory removal writes.
- Unsafe raw inventory writes may be reverted by game-managed visible slots.
- Equipment editing grants ownership flags only. Current equipped armor, sword, and shield are game-managed and read-only in the trainer.
- Early Ordon intro saves may accept byte writes in memory while TPHD ignores ownership edits. Progress past the intro arc and rescan, or use the manual overrides only for diagnostics on copied saves.
- Golden Bugs ownership editing is mapped for the visible collection-screen bugs, but Agitha reward and turn-in flags are not edited.
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

**Apply Inventory Ownership Changes says no mapped flags are available**

The checked CT source does not currently identify ownership/progression bits for the listed inventory items. Use the Debug research and snapshot tools to map real flags before enabling writes for those rows.

**Inventory or Equipment says Not Initialized**

This is not a story lock. TPHD has not created or stabilized that memory structure yet. Progress until Link obtains a real inventory item or equipment, then click **Attach / Rescan**. Advanced overrides exist for diagnostics, but they are off by default.

**Ownership Edits says Likely No**

The save appears to be before TPHD begins honoring ownership edits, usually during the Ordon Village intro arc. Progress farther, ideally past the intro arc, then click **Attach / Rescan**. If you are already past that point, check **I am past the Ordon Village intro arc**. Use **Allow ownership edits before intro completion** only for diagnostics on copied saves or save states.

**Newly granted equipment is not visible in the in-game equipment screen**

If the equipment screen was already open when ownership changed, TPHD may not refresh the displayed equipment immediately. Close and reopen the in-game equipment screen. The trainer grants ownership flags only; the player still equips owned items using the game's own menu.

**Access denied**

Run the trainer with the same privilege level as Cemu. If Cemu is elevated, the trainer likely needs to be elevated too.

## Safety / Legal Note

This project is intended for personal offline emulator use only. Do not use it with online services, shared competitive environments, or software you do not have permission to inspect or modify in memory.

## Credits

Cheat table offsets, AOB patterns, and behavior references are credited to **toto621**, author of `Zelda_TP_HD_Mega_Trainer (by toto621).ct`.
