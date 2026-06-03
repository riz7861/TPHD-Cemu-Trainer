# Bomb Slots Research

## Status

Confirmed visible bomb slot content editor implemented.

The trainer still preserves the CT-backed bomb count and shared bomb capacity controls, but the UI labels them as **Bomb Slot 1**, **Bomb Slot 2**, and **Bomb Slot 3** instead of assuming they are independent bomb bags.

## Confirmed Visible Slot Content Offsets

Live TPHD memory testing confirms these visible bomb slot content bytes:

| Slot | Offset |
| --- | --- |
| Bomb Slot 1 | `_playerbase+0x267` |
| Bomb Slot 2 | `_playerbase+0x268` |
| Bomb Slot 3 | `_playerbase+0x269` |

## Confirmed Slot Content Values

| Value | Hex | Meaning |
| --- | --- | --- |
| 112 | `0x70` | Normal Bombs |
| 113 | `0x71` | Water Bombs |
| 114 | `0x72` | Bomblings |

The value `0x50` was observed to create an empty or glitched bomb slot icon and is intentionally not exposed in the trainer UI.

## Editor Behavior

The Ammo & Upgrades tab includes **Bomb Slot Editor**.

Writes occur only when the user clicks **Apply Bomb Slot Changes** or **Restore Previous Bomb Slots**. The editor:

- Reads the current 3-byte slot state.
- Captures a session-only previous snapshot before writes.
- Writes only confirmed slot content values.
- Verifies immediate readback.
- Verifies delayed readback after 250ms and 1000ms.
- Refreshes bomb slot state after writing.
- Logs to `logs/bomb-slot-editor.log`.

Restore writes the last captured 3-byte snapshot for the current trainer session. The restore buffer is not saved to disk.

## CT-Backed Count / Capacity Values

The Cheat Engine table exposes these existing count/capacity fields:

- Bomb Slot 1 count: `_playerbase+0x2A9`
- Bomb Slot 2 count: `_playerbase+0x2AA`
- Bomb Slot 3 count: `_playerbase+0x2AB`
- Shared bomb capacity byte: `_playerbase+0x2B5`

These remain in the trainer as diagnostics and preserve existing behavior.

## Current Understanding

TPHD appears to separate visible bomb slot contents from bomb counts, shared capacity, button assignments, and progression state.

The confirmed editor changes the visible slot content bytes only. It does not grant story progression, modify button assignments, or edit bomb count/capacity values.

## Safety

Use copied saves or save states when testing. The confirmed values are live-tested, but bomb slot contents are still one part of a larger inventory system.
