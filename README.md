# TPHD Cemu Trainer v1.0

A Windows WPF trainer for **The Legend of Zelda: Twilight Princess HD** running in **Cemu**. It attaches to `Cemu.exe`, scans for the player data block using the Cheat Engine table AOB, and edits selected values through external process memory reads and writes.

This trainer is external only. It does not inject DLLs, install drivers, hook emulator code, bypass anti-cheat systems, or modify Cemu or game files.

> **Back up your save before using this trainer.** Confirmed editors preserve unrelated bits and verify writes, but game progression can still react differently on edited or imported saves.

## Supported Game / Emulator

- Game: The Legend of Zelda: Twilight Princess HD for Wii U
- Emulator: Cemu on Windows
- Process name: `Cemu.exe`
- Source table: `Zelda_TP_HD_Mega_Trainer (by toto621).ct`

## Current Features

The UI is organized as a tabbed trainer/save-editor hybrid so new systems can be added without crowding one large grid. Release 1.0 uses game-facing names and compact editors by default; raw offsets, diagnostic columns, and research terminology appear only in Developer Mode.

- **General**: heart-based health editing, max health, safe Heart Piece progress, lantern oil, wallet, rupees, Poe Souls, Golden Bugs count
- **Inventory**: item detection and confirmed fixed-slot grant/remove for mapped visible slots
- **Equipment**: ownership flag editor with read-only current equipped armor/sword/shield diagnostics
- **Ammo & Upgrades**: wallet, quiver, friendly Bomb Bag type/count/capacity controls, and seed capacity-aware edits
- **Collectibles**: friendly Poe Souls editing, full Golden Bugs editor, and health/heart summaries
- **Story Flags**: reserved progression flags
- **Hidden Skills**: confirmed dependency-safe progression editor
- **Quest Items**: confirmed Quest / Special item controls, Dominion Rod restoration, and current dungeon item bits
- **Developer Mode**: optional Research and Debug workspaces, experimental tools, raw values, diagnostics, and unsafe research editors

The traditional WPF menu provides File actions for attach/detach/exit, Options for Dark Mode and Developer Mode, Tools for logs and Developer Mode workspaces, and Help links for documentation, About information, and the backup warning. The existing top bar remains available for quick attach/detach and status checks.

The app defaults to Light mode with Developer Mode off and stores both preferences locally under the user's AppData folder when possible.

## Developer Mode

Developer Mode is off by default so the public v1.0 interface stays focused on confirmed editors. Enabling it requires accepting a warning and reveals the Research tab, Debug tab, raw addresses, detailed diagnostics, experimental candidate tools, advanced bottle details, inventory removal/raw writes, bomb/Golden Bug diagnostics, and embedded research panels.

Developer Mode never writes its preference to TPHD memory or save data. Disabling it hides the research tools again and stops active live-memory watches. A visible **Developer Mode Enabled** indicator remains in the top bar while it is active.

## Implemented Memory Edits

- Current health
- Maximum health
- Safe partial Heart Piece progress within the current collection cycle
- Rupees
- Lantern oil
- Arrows
- Bomb slot count bytes 1-3
- Bomb slot contents at `_playerbase+0x267`, `_playerbase+0x268`, and `_playerbase+0x269`
- Seeds
- Poe souls at `_playerbase+0x2C8`, clamped to 0-60 with readback diagnostics
- Wallet capacity
- Quiver capacity
- Shared bomb slot capacity
- Golden Bugs ownership editor for all 24 bugs at `_playerbase+0x2A1` through `_playerbase+0x2A3`
- Read-only inventory slot detection at `_playerbase+0x258` through `_playerbase+0x26F`
- Experimental bottle slot editing at `_playerbase+0x263` through `_playerbase+0x266`
- Unsafe inventory removal testing at `_playerbase+0x258` through `_playerbase+0x26F`
- Unsafe raw inventory slot writes at `_playerbase+0x258` through `_playerbase+0x26F`
- Read-only equipped armor at `_playerbase+0x1D1`
- Read-only equipped sword at `_playerbase+0x1D2`
- Read-only equipped shield at `_playerbase+0x1D3`
- Equipment ownership flags at `_playerbase+0x28D`, `_playerbase+0x28E`, and `_playerbase+0x292`
- Confirmed Quest / Special item slots at `_playerbase+0x26A` through `_playerbase+0x26E`
- Dominion Rod restoration flag at `_playerbase+0x3D1` bit 7
- Current dungeon Map, Compass, and Boss Key / Large Key bits at `_playerbase+0xFD1` bits 0-2
- Current dungeon Small Keys at `_playerbase+0xFD0`, limited to 0-9
- Goron Mines key-shard completion at `_playerbase+0x2A4`

## Capacity System

Capacity-limited values are clamped before every write. The **Max** button means the currently selected capacity, not a global maximum.

Capacity dropdowns use friendly labels such as **500 Rupees**, **30 Arrows**, and **30 Bombs**. Reading player data selects the matching current capacity automatically. Normal-mode health values and targets are displayed in hearts, while the underlying quarter-heart writes remain unchanged. Health editing supports values through 1000 quarter-hearts and no longer clamps valid values above 80. Maximum-health targets initialize from the current read value after attach/rescan.

- Wallet: 500, 1000, 2000, 9999
- Quiver: 30, 60, 100
- Bomb Slots: 30, 60
- Seed Bag: 50

The CT exposes one shared bomb capacity byte, so all three CT-backed bomb count fields currently share that capacity selector. Normal Mode presents them as **Bomb Bag 1-3** for a familiar game-facing workflow. Developer Mode retains the underlying slot terminology and diagnostics. The seed bag capacity is fixed because the CT table does not expose a separate seed capacity offset.

Targets initialize from the current in-memory value after a successful scan. Lock mode writes only clamped values. Current health is also clamped to maximum health.

## Bomb Bags And Developer Diagnostics

The normal Ammo & Upgrades tab presents three compact **Bomb Bag** sections. Each shows Bomb Type, shared Capacity, Current Bombs, and Lock Bombs. The type dropdown contains only:

- Normal Bombs
- Water Bombs
- Bomblings

Select **Apply Bomb Type Changes** to write pending type changes. Raw slot values, delayed verification details, previous-value restore, and research wording are visible only in Developer Mode.

Internally, the confirmed visible bomb content bytes are:

- Bomb Slot 1 content: `_playerbase+0x267`
- Bomb Slot 2 content: `_playerbase+0x268`
- Bomb Slot 3 content: `_playerbase+0x269`

Confirmed live-tested content values:

- `0x70`: Normal Bombs
- `0x71`: Water Bombs
- `0x72`: Bomblings

The invalid `0x50` value produced an empty/glitched bomb icon during testing and is intentionally not exposed in the editor.

Bomb type writes happen only when **Apply Bomb Type Changes** is clicked. The trainer reads the previous slot bytes, writes only the selected confirmed slot values, verifies immediate readback, verifies again after 250ms and 1000ms, refreshes the slot state, and logs to `logs/bomb-slot-editor.log`. Developer Mode provides a session-only **Restore Previous Bomb Slots** action.

The existing CT-backed bomb count/capacity behavior remains unchanged:

- Bomb Slot 1 count: `_playerbase+0x2A9`
- Bomb Slot 2 count: `_playerbase+0x2AA`
- Bomb Slot 3 count: `_playerbase+0x2AB`
- Shared bomb capacity: `_playerbase+0x2B5`

The count/capacity controls remain CT-backed diagnostics and existing count/capacity editing is preserved. Slot contents, counts, capacity, and button assignments are separate pieces of game state.

See `docs/research/BombSlotsResearch.md` for current notes.

## Quest / Special Items And Research

The normal Quest Items tab uses a two-column layout so important actions remain reachable at common window sizes. Confirmed Quest / Special slots use friendly current-value and **Set To** dropdowns with per-slot **Apply** buttons. Items sharing one game slot, such as Ooccoo/Ooccoo Jr. and Ilia's Charm/Horse Call, remain mutually exclusive. The verified Ancient Sky Book/Ooccoo safety rule still applies.

With Developer Mode enabled, the **Advanced Slot Editor** exposes the live-confirmed fixed slots:

- Ooccoo slot: `_playerbase+0x26A`, supporting Empty, Ooccoo, and Ooccoo Jr.
- Generic Quest Slot / Bottom-Right Quest Slot: `_playerbase+0x26B`, supporting Empty, Ooccoo, Ooccoo Jr., Fishing Rod, Ilia's Charm, Horse Call, and Ancient Sky Book. This is marked advanced / experimental-safe because the intended story item is still unknown.
- Fishing Rod slot: `_playerbase+0x26C`, supporting Empty, Fishing Rod, and Fishing Rod + Coral Earring
- Horse Call / Ilia item slot: `_playerbase+0x26D`, supporting Empty, Ilia's Charm, and Horse Call. Ilia's Charm and Horse Call were verified working in-game.
- Ancient Sky Book slot: `_playerbase+0x26E`, supporting Empty and Ancient Sky Book

Ancient Sky Book at `_playerbase+0x26E` can only be written when the Ooccoo slot at `_playerbase+0x26A` is empty (`0xFF`). The trainer blocks the write and logs a warning rather than silently replacing Ooccoo or Ooccoo Jr.

The tab also includes **Restore Dominion Rod / Complete Restoration** at `_playerbase+0x3D1` bit 7. This was verified working in-game. It does not grant the Dominion Rod item; it only restores/reactivates the rod if the player already owns it. The write preserves every unrelated bit in `0x3D1`.

The **Current Dungeon Items** editor uses `_playerbase+0xFD1` as the active/current dungeon item ownership byte. It is not Forest Temple-specific and is confirmed working in Forest Temple, Goron Mines, and Lakebed Temple:

- bit 0: Map
- bit 1: Compass
- bit 2: Boss Key / Large Key

Useful low-bit values are `0x00` None, `0x01` Map, `0x02` Compass, `0x04` Boss Key / Large Key, and `0x07` all three. Writes only modify bits 0-2 and preserve bits 3-7. The dungeon editor does not expose `0xC7`, edit key shards, touch `_playerbase+0x28F`, or claim to control dungeon-specific quest items. Goron Mines key shard progression appears to be separate from these bits.

The same section exposes **Small Keys for the current dungeon** at `_playerbase+0xFD0`. The normal editor limits this byte to `0-9` and verifies the written value. It is separate from the Map, Compass, and Boss Key / Large Key bits at `0xFD1`.

The separate **Goron Mines Key Shards** control writes the live-confirmed completed state `0x6E` to `_playerbase+0x2A4`. It completes the Goron Mines key-shard sequence and forms the Big Key without touching `0xFD1`. This is a completion control, not a shard-count editor.

With Developer Mode enabled, the Quest Items tab includes **Quest Items Research / Experimental**. It is read-only and is intended to help identify quest-item and progression bytes without promoting unconfirmed offsets into an editor.

The tool captures Before and After snapshots of a configurable `_playerbase`-relative range. The recommended broad scan defaults to start `0x200` and length `0x400`; quick buttons also cover `0x200-0x2FF`, `0x200-0x3FF`, `0x000-0x3FF`, and `0x000-0x7FF`. Compare shows offset, before/after byte, before/after binary, changed bits, candidate score, and nearby candidate group.

Persistent Quest Items captures are saved under `logs/research/quest-search/` with labels, capture type, notes, timestamps, app version, start offset, length, and raw bytes:

- `quest-capture_<timestamp>_<safe-label>.json`

You can load saved captures as Capture A and Capture B later, compare them without Cemu running, and export named comparison reports:

- `logs/research/quest-search/quest-report_<timestamp>_<captureA>_vs_<captureB>.json`
- `logs/research/quest-search/quest-report_<timestamp>_<captureA>_vs_<captureB>.csv`

The **Candidate Ranking** section scores changed bytes so low-value noise is easier to filter out. Scores increase for single-bit changes, small value transitions, nearby grouped changes, monotonic increases, persisted saved-capture changes, flag-like transitions, and offsets that appear across progression comparisons. Scores are reduced for large noisy changes and timer-like/counter-like values. The default filter shows changed rows only.

The **Candidate Groups** section groups nearby changed offsets such as `0x2B1-0x2B3` into named groups with count, range, highest score, and reasons. The **Multi-Capture Analysis** section loads multiple saved captures, or all `quest-capture_*.json` files in `quest-search`, sorts known temple/progression types, and ranks offsets whose values only increase, step upward, or appear once and remain set across a progression chain.

Analyzer exports are written to:

- `logs/research/quest-search/quest-candidate-ranking.json`
- `logs/research/quest-search/quest-candidate-ranking.csv`
- `logs/research/quest-search/quest-candidate-groups.json`
- `logs/research/quest-search/quest-candidate-groups.csv`
- `logs/research/quest-search/quest-multi-capture-analysis.json`
- `logs/research/quest-search/quest-multi-capture-analysis.csv`

The legacy quick exports are still written for convenience:

- `logs/research/quest-items-before.json`
- `logs/research/quest-items-after.json`
- `logs/research/quest-items-report.json`
- `logs/research/quest-items-report.csv`

Research activity is logged to `logs/quest-items-research.log`. Quest / Special editor writes are logged to `logs/quest-special-editor.log`. See `docs/research/QuestItemsResearch.md` and `docs/research/DungeonItemsResearch.md`.

## Heart Piece Progress

The General tab displays Current Health and Maximum Health in hearts. Targets accept quarter-heart increments, while Developer Mode and raw-memory tools retain the underlying quarter-heart values. The guarded Heart Piece progress editor is backed by `_playerbase+0x1BF`; it treats the byte as a running progress counter, shows the current partial progress using `value % 5`, and allows only `1/5` through `4/5` within the current cycle. It does not expose unsafe raw values or destructive low resets in normal mode.

The editor changes only partial progress within the current cycle; it does not grant a specific overworld Heart Piece or alter chest/history flags. Raw value details are visible only in Developer Mode. See `docs/research/HeartProgressResearch.md`.

## Research Workspace

With Developer Mode enabled, the **Research** tab is a compact WPF workspace for continuous memory analysis and report review. It is designed to reduce long Debug-tab scrolling while preserving the existing Quest Items, Hidden Skills, Ownership Discovery, and support snapshot tools.

Sub-tabs:

- **Live Capture**: read-only continuous monitoring of a selected `_playerbase` range, defaulting to start `0x0000`, length `0x400`, and 250ms sampling.
- **Snapshot Diff**: quick in-memory A/B range capture, changed-byte display, candidate ranking, and JSON/CSV export.
- **Candidate Tester**: compact raw byte read/write/restore tester for copied-save research.
- **Analysis Reports**: searchable report browser for JSON/CSV exports under `logs/research/`.
- **Logs**: filtered scrolling log viewer with category selection and display-only clearing.

Live Capture takes a baseline when **Start Capture** is clicked, then tracks only offsets whose byte values change. It records initial, previous, and current values, change counts, first/last seen timestamps, changed bits, single-bit transitions, candidate score, confidence, persisted/reverted status, and a bounded timeline. It does not write memory.

Use **Check Persistence** after saving/reloading or continuing a session. A byte marked **Persisted** remains changed from the baseline; **Reverted** returned to the baseline. This is a research hint only, not proof of an ownership flag.

Known-region hints are used only for scoring and filtering:

- `0x26A-0x26E`: confirmed/researched inventory special item slots, including Ooccoo, Fishing Rod, Horse Call/Ilia item, and Sky Book-style fields
- `0x298-0x29B`: candidate ownership/progression area
- `0x3D1`: confirmed Dominion Rod restoration bit
- `0x3D5-0x3D6`: confirmed Hidden Skills ownership/progression bytes
- `0xFD1`: confirmed current dungeon Map/Boss Key/Compass bits
- `0x221-0x22D`: known scene/location/runtime noise, filterable with **Hide Known Scene Noise**

Live Capture exports are written to:

- `logs/research/live-capture-session_<timestamp>_<label>.json`

Runtime Live Capture activity is logged to:

- `logs/live-capture.log`

See `docs/research/LiveCaptureResearch.md`.

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

In the default v1.0 interface, Inventory uses a compact editor with quick actions, item name, friendly current state, change checkbox, and a short status. Raw offsets, slot IDs, ownership/progression research notes, write diagnostics, unsafe controls, and the full technical table are hidden by default. Enable Developer Mode and expand **Developer / Research Details** to inspect those fields without changing the normal editor workflow.

The technical inventory ownership and fixed-slot details below are visible only in Developer Mode. The checked CT source still does not identify authoritative inventory ownership/progression flags:

- **Detected**: visible-slot detection for the listed item families.
- **Desired**: used by the fixed-slot experimental editor for mapped visible slots, and reserved for future real ownership flags.
- **Dirty**: indicates desired state differs from the current visible-slot detection or future mapped ownership state.

When mapped flags are added later, **Apply Inventory Ownership Changes** will write desired ownership flags in bulk. The trainer writes ownership/progression flags only; it does not fake ownership by writing raw visible inventory slots. Future mapped writes will be verified with immediate and delayed readback, then logged to `logs/inventory-ownership.log`.

The checked CT source currently does not expose real ownership flag offsets/bits for Fishing Rod, Slingshot, Lantern, Hero's Bow, Gale Boomerang, Clawshot, Double Clawshots, Spinner, Dominion Rod, Ball and Chain, Hawkeye, Horse Call, or Bottles. Normal ownership editing remains disabled for those rows until real flags are mapped.

The **Current Inventory Slots (Read Only)** section shows all 24 CT-derived bytes: slot, offset, raw item ID, decoded item name, and notes. Slot 21 / `_playerbase+0x26C` is labeled as the Fishing Rod field with the note: **Game-managed. Direct writes revert. Real ownership/progression flag not identified yet.**

The trainer should eventually grant inventory through ownership/progression flags rather than raw slot forcing. Raw slots are still useful for detection and research, especially when comparing before/after saves or snapshots.

The normal Inventory tab includes a compact four-row **Bottles** editor showing each bottle's current content and a friendly content dropdown. Developer Mode reveals raw values and detailed bottle diagnostics.

The bottle editor changes only the four currently mapped bottle-content slots:

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

- **Poe Souls**: friendly current/set/max controls for values `0-60`; technical verification details remain in Developer Mode and `logs/collectibles.log`.
- **Golden Bugs**: simple **Owned** checkboxes for all 24 bugs, plus Apply, Add All, and Clear All. Raw ownership details and session restore remain in Developer Mode.
- **Health summary**: current and maximum health are shown in hearts. Health editing remains in the General tab.

TPHD has 24 Golden Bugs: 12 species with male and female variants. Live memory testing confirms that visible Golden Bug ownership uses the 24 bits across `_playerbase+0x2A1`, `_playerbase+0x2A2`, and `_playerbase+0x2A3`. The normal Golden Bugs editor does not write `_playerbase+0x2A4`.

Normal Mode shows the current owned count out of 24 and one friendly **Owned** checkbox per bug. Developer Mode adds raw bytes `0x2A1-0x2A3`, offset/bit/current state, pending indicators, diagnostics, and session restore. Refresh updates detected state and preserves unsaved desired edits. The trainer never writes Golden Bugs on attach, rescan, or refresh.

**Apply Changes** writes only changed desired bits and preserves the rest of each byte. **Collect All** sets all 24 confirmed collection bits. **Clear All** clears all 24 confirmed collection bits. Developer Mode's **Restore Previous Bug State** restores the previous 3-byte state captured before the last editor write in the current trainer session. All editor writes verify immediate, 250ms, and 1000ms readbacks.

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

## Hidden Skills Progression Editor And Research

The Hidden Skills tab includes a confirmed progression editor for all seven Hidden Skills. The editor treats the skills as an ordered chain:

1. Ending Blow: `_playerbase+0x3D5` bit `2`
2. Shield Attack: `_playerbase+0x3D5` bit `3`
3. Back Slice: `_playerbase+0x3D5` bit `0`
4. Helm Splitter: `_playerbase+0x3D5` bit `1`
5. Mortal Draw: `_playerbase+0x3D6` bit `5`
6. Jump Strike: `_playerbase+0x3D6` bit `6`
7. Great Spin: `_playerbase+0x3D6` bit `7`

Enabling a skill automatically enables all earlier prerequisites. Disabling a skill automatically disables all later dependent skills. The in-game Skills menu may show progression slots rather than exact isolated bit state, and some moves require prerequisite flags for combat usability. Treat combat usability as the real validation signal when testing copied saves.

The editor shows detected state, desired state, dirty state, and last write/verification status. It interprets only confirmed ownership bits in `0x3D5` bits `0-3` and `0x3D6` bits `5-7`; unrelated bits remain visible in Developer Mode diagnostics only. If detected ownership has a later skill without prerequisites, the editor shows **Hidden Skill progression appears non-standard. This can happen on edited or imported saves.** and does not write fixes until Apply is clicked. It supports refresh, apply changed skills, add all, clear all, and session-only restore of the previous two-byte state. Writes preserve unrelated bits in `0x3D5` and `0x3D6`, verify immediate/250ms/1000ms readbacks, and log to `logs/hidden-skills-editor.log`.

Hidden Skill bits affect both menu ownership and Hero's Shade/wolf progression. Removing a learned skill may cause the wolf/Hero's Shade encounter to become available again after area reload. Use copied saves first.

Important: `_playerbase+0x238` bit `0` is not Hidden Skill ownership. It appears to be Hero's Shade / lesson active state, and the editor does not write it.

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

The **Hidden Skills Multi-Capture Analyzer** can load six saved Hidden Skills captures offline. It defaults the known learned-skill counts to `1 -> 2 -> 3 -> 5 -> 6 -> 7`, then ranks offsets that change monotonically, bits that only ever increase, byte value fields, and byte-level bitfields. Rows where the raw value or set-bit count follows `1 -> 2 -> 3 -> 5 -> 6 -> 7` are highlighted as strong research candidates. Ranking exports are written to `logs/research/hidden-skills-multi-capture-ranking.json` and `logs/research/hidden-skills-multi-capture-ranking.csv`.

The **Hidden Skills Bit Tester / Experimental** section can test one candidate bit at a time. It supports offset/bit presets such as `0x218 bit0`, `0x219 bit0`, `0x214 bit1`, and `0x238 bit0`, preserves all other bits in the byte, and verifies immediate, 250ms, and 1000ms readbacks. Bit-test activity is logged to `logs/hidden-skills-bit-testing.log`.

Details are tracked in `docs/research/HiddenSkillsResearch.md`.

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

Scan diagnostics are shown in the Developer Mode Debug tab and written to `logs/scan.log`, including process discovery time, handle-open time, region enumeration time, scan time, regions scanned/skipped, bytes scanned, cache status, and match address.

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

In the app, click **Attach / Rescan**. If the AOB scan succeeds, the trainer reports that player data is ready. Developer Mode also shows the Cemu process ID, resolved player base address, and detailed scan diagnostics.

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
- `TphdCemuTrainer/Cheats/BombSlotDefinitions.cs`: confirmed visible bomb slot offsets and content values
- `TphdCemuTrainer/Cheats/BottleDefinitions.cs`: experimental bottle slot and confirmed bottle content definitions
- `TphdCemuTrainer/Cheats/GoldenBugsDefinitions.cs`: confirmed Golden Bugs bit mappings and reference bug names
- `TphdCemuTrainer/Cheats/HiddenSkillsDefinitions.cs`: confirmed Hidden Skills ownership bit mappings
- `TphdCemuTrainer/Cheats/InventoryDefinitions.cs`: safe CT inventory item dropdowns, ownership candidates, and managed slot metadata
- `TphdCemuTrainer/Cheats/QuestSpecialDefinitions.cs`: confirmed Quest / Special item slots, Dominion Rod restoration bit, and current dungeon item bits
- `TphdCemuTrainer/Cheats/InventoryOwnershipDefinition.cs`: inventory ownership/progression candidate metadata
- `TphdCemuTrainer/Cheats/InventoryFixedSlotDefinition.cs`: known fixed/game-managed inventory slot metadata
- `TphdCemuTrainer/Cheats/EquipmentDefinitions.cs`: TPHD 2.2.CT equipment dropdown and flag definitions
- `TphdCemuTrainer/Cheats/FutureFeatureCatalog.cs`: reserved trainer/save-editor feature groups
- `TphdCemuTrainer/Research/`: named memory snapshots plus JSON/CSV comparison and ownership discovery exports
- `TphdCemuTrainer/ViewModels/`: UI-facing value and capacity models
- `docs/research/HiddenSkillsResearch.md`: current Hidden Skills mapping notes and workflow
- `docs/research/QuestItemsResearch.md`: Quest Items research workflow and export notes
- `docs/research/DungeonItemsResearch.md`: current dungeon item bits and dungeon event research notes
- `docs/research/HeartProgressResearch.md`: confirmed guarded Heart Piece progress behavior
- `docs/research/LiveCaptureResearch.md`: generic live capture workflow and known-region hints
- `TphdCemuTrainer/MainWindow.xaml`: tabbed WPF trainer UI

## Known Limitations

- The player base scan depends on the CT table AOB. It may fail on unsupported game revisions, different memory layouts, or if gameplay is not loaded.
- The first scan can still be slower than later rescans because no cache has been validated yet.
- Missing player data is handled as a rescan state, not an application failure.
- Broader story flags and many quest progression fields are still research-only. Only the confirmed Quest / Special item slots, Dominion Rod restoration bit, current dungeon items/Small Keys, and Goron Mines key-shard completion state are exposed as editors.
- Heart Piece partial progress is editable within the current collection cycle, but individual Heart Piece ownership/history and stamp ownership/history are not mapped.
- Goron Mines key-shard completion is confirmed, but individual shard count/state editing is not exposed.
- Some quest items remain unknown until natural playthrough captures identify their authoritative state.
- Developer Mode tools are experimental and hidden by default.
- Hidden Skills ownership is mapped and editable, but related Hero's Shade/wolf lesson progression state is not fully mapped. Clearing learned skills may affect encounter availability after area reload.
- Inventory ownership editing uses detected/desired/apply where real flags are mapped. Current listed inventory ownership flags are not identified yet.
- Bottle Editor v1 edits visible bottle-content slots only; bottle ownership and unconfirmed bottled item raw values are still being researched.
- General inventory ownership/progression editing is not solved yet. Static fixed-slot grant/remove is confirmed only for specific visible-slot item/slot pairs.
- Bomb Slot Editor changes visible slot content bytes only. Bomb counts, shared capacity, button assignments, and any related story state remain separate.
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
- Quest Items Research remains read-only. Unconfirmed quest/event candidates are not exposed as editors until mappings are confirmed.
- Support snapshots include trainer logs and state summaries only. They intentionally do not include Cemu saves, game files, memory dumps, or personal account data.
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

**Need to share diagnostics**

Use **Debug -> Create Support Snapshot**. The trainer writes a zip to `support-snapshots/` with app version, attach status, player base, trainer state summary, config values, and a snapshot of the `logs/` directory. It does not include save files or game files.

**Access denied**

Run the trainer with the same privilege level as Cemu. If Cemu is elevated, the trainer likely needs to be elevated too.

## Safety / Legal Note

Back up your save before editing. This project is intended for personal offline emulator use only. Do not use it with online services, shared competitive environments, or software you do not have permission to inspect or modify in memory.

## Credits

Cheat table offsets, AOB patterns, and behavior references are credited to **toto621**, author of `Zelda_TP_HD_Mega_Trainer (by toto621).ct`.
