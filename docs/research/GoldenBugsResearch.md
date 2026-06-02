# Golden Bugs Research

## Status

Golden Bug collection-screen ownership is confirmed in live TPHD memory testing.

Visible ownership uses the 24 bits across:

- `_playerbase+0x2A1`
- `_playerbase+0x2A2`
- `_playerbase+0x2A3`

The fourth byte at `_playerbase+0x2A4` was included in earlier 4-byte research snapshots, but it is not used for visible Golden Bug ownership editing. It may still be useful for unrelated collectible, reward, or turn-in research.

## Confirmed Ownership Mapping

| Bug | Offset | Bit | Evidence |
| --- | --- | ---: | --- |
| Male Snail | `0x2A1` | 0 | Live TPHD confirmed ownership bit |
| Female Snail | `0x2A1` | 1 | Live TPHD confirmed ownership bit |
| Male Dragonfly | `0x2A1` | 2 | Live TPHD confirmed ownership bit |
| Female Dragonfly | `0x2A1` | 3 | Live TPHD confirmed ownership bit |
| Male Ant | `0x2A1` | 4 | Live TPHD confirmed ownership bit |
| Female Ant | `0x2A1` | 5 | Live TPHD: `0xD8 -> 0xF8` |
| Male Dayfly | `0x2A1` | 6 | Live TPHD confirmed ownership bit |
| Female Dayfly | `0x2A1` | 7 | Live TPHD confirmed ownership bit |
| Male Phasmid | `0x2A2` | 0 | Live TPHD confirmed ownership bit |
| Female Phasmid | `0x2A2` | 1 | Live TPHD confirmed ownership bit |
| Male Pill Bug | `0x2A2` | 2 | Live TPHD: `0xC0 -> 0xC4` |
| Female Pill Bug | `0x2A2` | 3 | Live TPHD: `0xC4 -> 0xCC` |
| Male Mantis | `0x2A2` | 4 | Live TPHD confirmed ownership bit |
| Female Mantis | `0x2A2` | 5 | Live TPHD confirmed ownership bit |
| Male Ladybug | `0x2A2` | 6 | Live TPHD confirmed ownership bit |
| Female Ladybug | `0x2A2` | 7 | Live TPHD confirmed ownership bit |
| Male Beetle | `0x2A3` | 0 | Live TPHD confirmed ownership bit |
| Female Beetle | `0x2A3` | 1 | Live TPHD confirmed ownership bit |
| Male Butterfly | `0x2A3` | 2 | Live TPHD confirmed ownership bit |
| Female Butterfly | `0x2A3` | 3 | Live TPHD confirmed ownership bit |
| Male Stag Beetle | `0x2A3` | 4 | Live TPHD confirmed ownership bit |
| Female Stag Beetle | `0x2A3` | 5 | Live TPHD confirmed ownership bit |
| Male Grasshopper | `0x2A3` | 6 | Live TPHD: `0x14 -> 0x54` |
| Female Grasshopper | `0x2A3` | 7 | Live TPHD: `0x54 -> 0xD4` |

## Editor Behavior

The normal Collectibles Golden Bugs editor writes only `_playerbase+0x2A1` through `_playerbase+0x2A3`.

- Apply writes changed desired bits only.
- Add All sets the 24 confirmed ownership bits.
- Clear All clears the 24 confirmed ownership bits.
- Restore Previous restores the previous 3-byte state captured before the last editor write in the current trainer session.
- All writes preserve bits outside the confirmed ownership bytes and verify immediate, 250ms, and 1000ms readbacks.

## Research Tools

The advanced research tools remain available for controlled investigation:

- Golden Bugs Research compares before/after byte ranges.
- Golden Bugs Bitfield Tester can still inspect and test all 32 bits across `0x2A1-0x2A4`.
- Research exports are written to `logs/research/`.

Use the advanced tools for `0x2A4`, Agitha reward state, and turn-in flag research. Do not treat those fields as normal Golden Bug ownership.

## Safety

Golden Bugs editing uses live-tested ownership bits, but it is still a live memory editor. Use copied saves or save states.

Agitha reward and turn-in flags are not edited by the Golden Bugs editor and may be separate game state.
