# Stamp Research

## Confirmed Ownership Range

TPHD Stamp collection data is stored across `_playerbase+0xAF5` through `_playerbase+0xAFB`.

Each mapped Stamp uses one bit. The editor exposes only confirmed mapped entries, writes only selected bits, preserves all other bits, and verifies readback.

## Confirmed Mapping

| Offset | Bit | Stamp |
| --- | ---: | --- |
| `0xAFB` | 0-7 | A, B, C, D, E, F, G, H |
| `0xAFA` | 0-7 | I, J, K, L, M, N, O, P |
| `0xAF9` | 0-7 | Q, R, S, T, U, V, W, X |
| `0xAF8` | 0 | Y |
| `0xAF8` | 1 | Z |
| `0xAF8` | 2 | Rupee |
| `0xAF8` | 3 | Treasure Chest |
| `0xAF8` | 4 | Piece of Heart |
| `0xAF8` | 5 | Heart Container |
| `0xAF8` | 6 | Link Happy |
| `0xAF8` | 7 | Link Angry |
| `0xAF7` | 0 | Link Sad |
| `0xAF7` | 1 | Link Surprised |
| `0xAF7` | 2 | Wolf Link |
| `0xAF7` | 3 | Midna Happy |
| `0xAF7` | 4 | Midna Angry |
| `0xAF7` | 5 | Midna Sad |
| `0xAF7` | 6 | Midna Surprised |
| `0xAF7` | 7 | Ooccoo |
| `0xAF6` | 0 | Zelda Happy |
| `0xAF6` | 1 | Zelda Angry |
| `0xAF6` | 2 | Zelda Sad |
| `0xAF6` | 3 | Zelda Surprised |
| `0xAF5` | 0 | Fairy |
| `0xAF5` | 7 | Twili Midna |

The Fairy and Twili Midna entries retain the active endpoint bit positions used by the existing live-tested implementation. Unused bits at `0xAF5` and `0xAF6` are not exposed or modified.

## Confirmed Tests

- `0xAFB = 0x02`: B only
- `0xAFB = 0x04`: C only
- `0xAFB = 0x80`: H only
- `0xAFA = 0x20`: N
- `0xAFA = 0x40`: O
- `0xAFA = 0x60`: N and O
- `0xAFA = 0xFF`: I through P

## Mapped Count TODO

Public Stamp lists describe 50 total Stamps. The current confirmed memory mapping covers 46 entries.

- The trainer displays **46 mapped Stamps**.
- The remaining four entries require further verification.
- Unmapped entries are not exposed in Normal Mode and are not modified by Collect All or Clear All.

## False Leads And Related State

### `0x289`

- Observed `0x04 -> 0x0C` during Forest Temple Stamp acquisition.
- Manually reverting to `0x04` did not remove the obtained Stamp.
- The change disappeared when filtering for persisted changes.
- Conclusion: temporary event, UI, or session state; not Stamp ownership.

### `0x29A`

- Observed `0x00 -> 0x02` during another Stamp acquisition.
- Persisted alongside the real `0xAFA` ownership change.
- Likely a chest or event flag, not confirmed Stamp ownership.

Neither `0x289` nor `0x29A` is exposed by the Stamp editor.
