# Quest Items Research

## Status

Mixed confirmed editor plus research tooling.

The trainer now exposes a compact editor only for live-confirmed Quest / Special fields. Broader quest-item, event, and progression candidates remain research-only until confirmed.

The confirmed editor remains visible in the public v1.0 interface. **Quest Items Research / Experimental** is preserved behind Developer Mode so unconfirmed candidates are not presented as normal editor options.

## Confirmed Quest / Special Editor

The Quest Items tab includes **Quest / Special Item Editor**.

Confirmed fixed slots:

| Field | Offset | Values | Notes |
| --- | --- | --- | --- |
| Ooccoo | `0x26A` | `0xFF` Empty, `0x25` Ooccoo, `0x27` Ooccoo Jr. | Confirmed special item slot. |
| Generic Quest Slot / Bottom-Right Quest Slot | `0x26B` | `0xFF` Empty, `0x25` Ooccoo, `0x27` Ooccoo Jr., `0x4A` Fishing Rod, `0x83` Ilia's Charm, `0x84` Horse Call, `0xE9` Ancient Sky Book | Confirmed generic quest item renderer. Advanced / experimental-safe because its intended story item is unknown. |
| Fishing Rod | `0x26C` | `0xFF` Empty, `0x4A` Fishing Rod, `0x5C` Fishing Rod + Coral Earring | Confirmed fixed rod-state slot. |
| Horse Call / Ilia Item | `0x26D` | `0xFF` Empty, `0x83` Ilia's Charm, `0x84` Horse Call | Confirmed companion item slot; Ilia's Charm and Horse Call verified working in-game. |
| Ancient Sky Book | `0x26E` | `0xFF` Empty, `0xE9` Ancient Sky Book | Ancient Sky Book requires the Ooccoo slot at `0x26A` to be empty. |

Writes happen only when the user clicks Apply. These controls write the confirmed slot byte only.

Ancient Sky Book validation:

- Before writing `0xE9` to `0x26E`, the trainer reads `0x26A`.
- If `0x26A` is not `0xFF`, the write is blocked.
- The trainer shows and logs: `Ancient Sky Book requires the Ooccoo slot to be empty.`
- The trainer never silently overwrites Ooccoo or Ooccoo Jr.

## Confirmed Dominion Rod Restoration

`0x3D1` bit 7 controls Dominion Rod restoration/reactivation state.

This control was verified working in-game.

Observed values:

- `0x32`: inactive/red state
- `0xB2`: restored/blue state

The trainer toggles only bit 7 and preserves every other bit in `0x3D1`.

Important:

- This does not grant the Dominion Rod item.
- The player must already own the Dominion Rod item.
- The Dominion Rod inventory slot is not touched by this control.

## Confirmed Current Dungeon Items

`0xFD1` is the current dungeon item ownership byte in at least Forest Temple, Goron Mines, and Lakebed Temple.

Confirmed bits:

- bit 0: Map
- bit 1: Compass
- bit 2: Boss Key / Large Key

Useful values:

- `0x00`: None
- `0x01`: Map
- `0x02`: Compass
- `0x04`: Boss Key / Large Key
- `0x07`: All three

The trainer modifies only bits 0-2 and preserves bits 3-7. It does not touch key shards, `_playerbase+0x28F`, or dungeon-specific quest item state.

`0xC7` is not exposed as an editor value because it includes extra progression bits outside the confirmed ownership bits.

Notes:

- Goron Mines has unique key shard progression, so Boss Key behavior may depend on dungeon/story context.
- Bits 6 and 7 are not key shard count.

## UI Location

The Quest Items tab includes **Quest Items Research / Experimental**.

The top-level **Research** tab also includes a compact **Snapshot Diff** workflow for quick in-memory A/B range comparison and a **Live Capture** workflow for continuous read-only monitoring. Use the Quest Items tab when you need persistent labeled quest captures and multi-capture progression analysis.

## Default Range

The research tool captures a configurable `_playerbase`-relative byte range.

Recommended broad scan:

- Start: `_playerbase+0x200`
- Length: `0x400`

The range can be changed in the UI when comparing different candidate areas.

Quick range buttons:

- `0x200-0x2FF`: start `0x200`, length `0x100`
- `0x200-0x3FF`: start `0x200`, length `0x200`
- `0x000-0x3FF`: start `0x000`, length `0x400`
- `0x000-0x7FF`: start `0x000`, length `0x800`

Use the broader `0x200` / `0x400` range when looking for quest flags that may live past `0x2FF`.

## Persistent Capture Library

Quest Items captures can be saved permanently under:

- `logs/research/quest-search/`

Capture filenames use:

- `quest-capture_<timestamp>_<safe-label>.json`

Examples:

- `quest-capture_20260603_223000_forest-temple.json`
- `quest-capture_20260603_224500_goron-mines.json`

Each capture stores:

- Timestamp
- Label
- Capture type
- Start offset
- Length
- Raw bytes
- Optional notes
- App version

Capture type options:

- Forest Temple
- Goron Mines
- Lakebed Temple
- Arbiter's Grounds
- Snowpeak Ruins
- Temple of Time
- City in the Sky
- Palace of Twilight
- Hyrule Castle
- Custom

## Save-Swap Workflow

1. Load save A.
2. Enter a label such as `Forest Temple Before`.
3. Capture and save with **Capture Before** then **Save Before Capture**, or use **Save Current Capture**.
4. Swap to another save or close/reopen Cemu and the trainer.
5. Enter a label such as `Goron Mines After`.
6. Capture and save the second state.
7. Load both captures later with **Load Capture A** and **Load Capture B**.
8. Click **Compare Loaded Captures**.
9. Export with **Export Report**, **Export JSON**, or **Export CSV**.

Loaded captures can be compared and exported without Cemu running.

## Recommended Progression Save Workflow

For broad quest progression discovery, keep a chain of copied saves:

1. Forest Temple
2. Goron Mines
3. Lakebed Temple
4. Arbiter's Grounds
5. Snowpeak Ruins
6. Temple of Time
7. City in the Sky

Capture the same range for each save. Use the capture type dropdown so the trainer can sort known progression captures in the expected order during multi-capture analysis.

## Compare Output

The comparison summary shows:

- Capture A label
- Capture B label
- Start offset and length for each capture
- Range mismatch warning if the captures do not cover the same range
- Changed byte count
- Changed bit count

Pairwise comparisons and multi-capture analysis require matching start offsets and lengths. If ranges differ, the tool warns instead of silently comparing unrelated bytes.

Each compared byte row shows:

- Offset
- Capture A byte
- Capture B byte
- Capture A binary
- Capture B binary
- Changed bit list
- Candidate score
- Nearby candidate group

Candidate scoring favors changed bytes, single-bit changes, small bit-count changes, and nearby grouped changes.

## Candidate Ranking

The **Candidate Ranking** section scores comparison rows so likely quest flags rise above low-value noise.

Score increases for:

- Single-bit changes
- Small value transitions
- Nearby grouped changes
- Monotonic increases
- Persisted saved-capture changes
- Flag-like transitions
- Offsets that appear in repeated progression comparisons

Score decreases for:

- Large noisy value changes
- Frequently changing counter-like values
- Timer-like values

Confidence is derived from score:

- High: score `10+`
- Medium: score `6-9`
- Low: score below `6`

Default filter:

- Changed rows only

Additional filters:

- Single-bit changes only
- Candidate score minimum
- Nearby grouped candidates only
- Persisted changes only

## Candidate Groups

Nearby changed offsets are grouped automatically using a small proximity window.

Example:

- `0x2B1`
- `0x2B2`
- `0x2B3`

becomes:

- Group A
- Offset range `0x2B1-0x2B3`

Groups show:

- Group name
- Offset range
- Count
- Highest candidate score
- Reasons

Use groups to spot compact quest/progression structures instead of isolated noisy bytes.

## Multi-Capture Analysis

The **Multi-Capture Analysis** section loads multiple saved Quest Items captures and compares them as a progression chain.

Use **Load Multi-Captures** to pick specific files, or **Load All Quest Captures** to load every `quest-capture_*.json` file in `logs/research/quest-search/`.

It ranks offsets that look like:

- Bits that only ever increase
- Values that increase in steps
- Flags that appear once and remain set
- Candidate quest progression structures

Output rows show:

- Offset
- Value progression
- Number of appearances
- Score
- Confidence
- Reasons

Multi-capture appearances are also fed back into pairwise candidate ranking. If an offset appears across multiple progression comparisons, its pairwise candidate score increases.

## Logs And Exports

Activity is logged to:

- `logs/quest-items-research.log`
- `logs/quest-special-editor.log`

Persistent reports are written to:

- `logs/research/quest-search/quest-report_<timestamp>_<captureA>_vs_<captureB>.json`
- `logs/research/quest-search/quest-report_<timestamp>_<captureA>_vs_<captureB>.csv`

Analyzer exports are written to:

- `logs/research/quest-search/quest-candidate-ranking.json`
- `logs/research/quest-search/quest-candidate-ranking.csv`
- `logs/research/quest-search/quest-candidate-groups.json`
- `logs/research/quest-search/quest-candidate-groups.csv`
- `logs/research/quest-search/quest-multi-capture-analysis.json`
- `logs/research/quest-search/quest-multi-capture-analysis.csv`

Legacy quick exports are still written to:

- `logs/research/quest-items-before.json`
- `logs/research/quest-items-after.json`
- `logs/research/quest-items-report.json`
- `logs/research/quest-items-report.csv`

## Safety

Quest Items Research is read-only. It does not grant quest items, change progression flags, write inventory slots, or modify story state.

The confirmed Quest / Special editor writes only the confirmed slot bytes or confirmed bitfields listed above. All bitfield writes preserve unrelated bits.

Use copied saves or save states when researching progression changes so you can repeat the same before/after comparison safely.

## Research-Only Notes

These findings are documented for future investigation and are not exposed as editor controls:

- `0x28F` appears to be a dungeon event/progression byte, not ownership.
- `0x28F` bit 3: observed map chest event.
- `0x28F` bit 0: observed small key event.
- `0x28F` bit 5: observed Ooccoo event.
- `0xFC3` bit 7: strong candidate for Goron Mines first key shard collected flag. Observed `0x00 -> 0x80` on the first shard pickup. It is not a shard count, and manual editing produced no visible UI change.
- `0x289` bit 3: stamp chest collected/history flag. Manual editing did not grant a stamp.
- `0x49B` bit 7: heart piece collected/history flag. Manual editing did not grant a heart piece, health, or container progress.

Rejected or failed visible-effect candidates:

- `0xFCA`
- `0xFC2`
- `0x1030`
- `0x1010`
- `0xFBE`
- `0xFBD`
- `0xFC1`
- `0xFC5`
