# Hidden Skills Research

Date: 2026-06-01

## Scope

This note records the current CT-backed evidence for Twilight Princess HD Hidden Skills state and proposes offsets/masks for future read-only research. It does not enable Hidden Skills editing.

## Sources Reviewed

- `TPHD 2.2.CT`
- `Zelda_TP_HD_Mega_Trainer (by toto621).ct`
- Existing trainer research tools and CT-to-playerbase alignment
- [TwilightEditor](https://github.com/zsrtp/TwilightEditor)

No standalone save-structure note with a Hidden Skills map was found in the repo. The strongest evidence currently comes from `TPHD 2.2.CT`.

## CT Evidence

`TPHD 2.2.CT` defines a `Hidden Moves` entry:

```text
Address: _baseaddr+10145561
Type: 2 Byte Big Endian
```

The CT dropdown lists cumulative values:

| Decimal | Hex | CT Description |
| ---: | ---: | --- |
| `0` | `0x0000` | No Hidden Skills |
| `256` | `0x0100` | Ending Blow |
| `768` | `0x0300` | All Before + Shield Attack |
| `1792` | `0x0700` | All Before + Back Slice |
| `3840` | `0x0F00` | All Before + Helm Splitter |
| `3902` | `0x0F3E` | All Before + Mortal Draw |
| `3967` | `0x0F7F` | All Before + Jump Strike |
| `4095` | `0x0FFF` | All Before + Great Spin-Attack |

## Possible Offsets

`TPHD 2.2.CT` maps an inventory slot at `_baseaddr+101453E4`, while toto621's CT maps the same visible inventory slot block at `_playerbase+0x258`. This implies:

```text
_playerbase = _baseaddr + 0x1014518C
```

Using that inferred anchor:

```text
Hidden Moves = _baseaddr+10145561
Hidden Moves = _playerbase+0x3D5
```

Confidence:

- `_baseaddr+10145561`: High, directly from `TPHD 2.2.CT`.
- `_playerbase+0x3D5`: Medium, inferred from CT address alignment and not yet verified live.

## Possible Bit Masks

The first four skills line up cleanly as cumulative high-byte bits:

| Skill | Cumulative Value | Incremental Mask | Confidence |
| --- | ---: | ---: | --- |
| Ending Blow | `0x0100` | `0x0100` | High |
| Shield Attack | `0x0300` | `0x0200` | High |
| Back Slice | `0x0700` | `0x0400` | High |
| Helm Splitter | `0x0F00` | `0x0800` | High |

The later CT dropdown values are still cumulative, but they do not advance as one simple next high-byte bit:

| Skill | Cumulative Value | Delta From Previous | Confidence |
| --- | ---: | ---: | --- |
| Mortal Draw | `0x0F3E` | `0x003E` | Medium |
| Jump Strike | `0x0F7F` | `0x0041` | Medium-low |
| Great Spin-Attack | `0x0FFF` | `0x0F80` from Jump Strike, `0x0FFF` as all-skills value | Medium for individual mask, high for all-skills value |

This suggests the Hidden Skills field may include multiple internal state bits, display bits, or learned/training flags rather than exactly one bit per skill for the later skills.

## Validation Plan

Use read-only discovery before implementing any editor:

1. Capture a Research Range Snapshot around `_playerbase+0x3D0` with length `0x10`.
2. Compare saves before and after learning a Hidden Skill.
3. Confirm whether `_playerbase+0x3D5` and `_playerbase+0x3D6` hold the CT's two-byte big-endian value.
4. Repeat for at least one early skill and one late skill.
5. Avoid byte-only writes with the current Candidate Flag Tester for this field because the CT defines it as a two-byte big-endian value.

## Current Conclusion

Hidden Skills have a strong CT-backed save/base address at `_baseaddr+10145561` and a plausible inferred trainer offset at `_playerbase+0x3D5`.

Future implementation should start with a read-only Hidden Skills display. Editing should wait until live snapshots confirm the inferred playerbase-relative offset and the meaning of later composite values.
