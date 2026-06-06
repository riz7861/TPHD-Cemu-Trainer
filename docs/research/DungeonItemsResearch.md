# Dungeon Items Research

## Status

Confirmed current-dungeon editor plus research notes.

The trainer exposes the confirmed active/current dungeon item ownership byte, the current dungeon Small Key count, and a separate confirmed Goron Mines key-shard completion action. Other dungeon-specific events, individual key shards, chest history, and progression state remain research-only.

The confirmed Current Dungeon Items editor remains available in the normal v1.0 interface. Raw diagnostics and research-only dungeon candidates require Developer Mode.

## Confirmed Current Dungeon Items

Current dungeon item ownership is stored at:

- `_playerbase+0xFD1`

Confirmed in live testing:

- Forest Temple
- Goron Mines
- Lakebed Temple

Confirmed bits:

| Bit | Meaning |
| --- | --- |
| 0 | Map |
| 1 | Compass |
| 2 | Boss Key / Large Key |

Useful values:

| Value | Meaning |
| --- | --- |
| `0x00` | None |
| `0x01` | Map |
| `0x02` | Compass |
| `0x04` | Boss Key / Large Key |
| `0x07` | All three |

Editor rules:

- Modify only bits 0, 1, and 2.
- Preserve bits 3-7.
- Do not touch key shards.
- Do not touch `_playerbase+0x28F`.
- Do not present this as dungeon-specific quest item editing.
- Do not expose `0xC7` as an editor value because it includes extra progression bits.

Goron Mines has unique key shard progression. Boss Key / Large Key behavior may depend on dungeon and story context.

Bits 6 and 7 at `0xFD1` are not key shard count.

## Confirmed Current Dungeon Small Keys

The current dungeon Small Key count is stored at:

- `_playerbase+0xFD0`

The normal editor limits values to `0-9`, writes only this byte, and verifies readback. This field is separate from the Map, Compass, and Boss Key / Large Key ownership bits at `0xFD1`.

## Confirmed Goron Mines Key-Shard Completion

Live testing confirmed:

- `_playerbase+0x2A4 = 0x6E`: completes the Goron Mines key-shard sequence and forms the Big Key.

The trainer exposes this as a separate **Complete Key Shards** action. It does not write `0xFD1`, does not claim to edit individual shard count, and is not part of Golden Bug ownership editing.

## Dungeon Event / Progression Byte

`_playerbase+0x28F` appears to be dungeon event/progression history, not ownership.

Observed bits:

- bit 3: map chest event
- bit 0: small key event
- bit 5: Ooccoo event

These bits should stay research-only until their behavior is better understood.

## Goron Mines First-Shard Candidate

`_playerbase+0xFC3` bit 7 is a strong candidate for the Goron Mines first key shard collected flag.

Current findings:

- Observed `0x00 -> 0x80` on the first Goron Mines key shard pickup.
- It is not a shard count.
- Manual editing produced no visible UI change.
- It is not exposed as an editor.
- It remains distinct from the confirmed completed-state value at `_playerbase+0x2A4`.

## Chest History Candidates

Observed history flags:

- `_playerbase+0x289` bit 3: stamp chest collected/history flag. Manual editing did not grant the stamp.
- `_playerbase+0x49B` bit 7: heart piece collected/history flag. Manual editing did not grant a heart piece, health, or container progress.

These are documented as history/event candidates only.

## Rejected / Failed Visible-Effect Candidates

Manual testing did not produce the expected visible dungeon item or quest effect for:

- `0xFCA`
- `0xFC2`
- `0x1030`
- `0x1010`
- `0xFBE`
- `0xFBD`
- `0xFC1`
- `0xFC5`

Keep these out of editor controls until new evidence changes their status.

## Safety

All confirmed editor writes must preserve unrelated bits. Research-only candidates should be tested only with copied saves or save states.
