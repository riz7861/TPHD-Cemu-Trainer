# Inventory Ownership Research

Date: 2026-06-01

## Scope

This note compares the current TPHD Cemu trainer findings, CT-derived mappings, TwilightEditor save structure behavior, and local research notes to classify candidate inventory ownership/progression fields.

This is research only. Do not add Inventory ownership editing or hardcode any of these candidates until live testing confirms that a field is authoritative, persistent after reload, and not just a game-managed display value.

## Sources Reviewed

- `TphdCemuTrainer/bin/Debug/net9.0-windows/logs/research/20260601_210641_ownership-discovery_Before-item_vs_After-item---reloaded.json`
- `TphdCemuTrainer/bin/Debug/net9.0-windows/logs/research/20260601_210641_ownership-discovery_Before-item_vs_After-item---reloaded.csv`
- `Zelda_TP_HD_Mega_Trainer (by toto621).ct`
- `TPHD 2.2.CT`
- [TwilightEditor](https://github.com/zsrtp/TwilightEditor)
- [TwilightEditor source](https://raw.githubusercontent.com/zsrtp/TwilightEditor/master/source/Twilight%20Editor.cpp)
- Pasted local research briefs from this thread. These describe observed behavior, but they do not provide additional offset maps beyond the CT and discovery logs.

## Known Anchors

The toto621 CT maps visible/current inventory bytes at `_playerbase+0x258` through `_playerbase+0x26F`. There are 24 one-byte slots. Testing showed direct writes to these bytes can succeed briefly, then TPHD rebuilds or restores them, so they are treated as game-managed visible state rather than authoritative ownership.

`TPHD 2.2.CT` maps an inventory slot at `_baseaddr+101453E4`. Since that corresponds to toto621's `_playerbase+0x258`, the live player base used by this trainer aligns with approximately:

```text
_playerbase = _baseaddr + 0x1014518C
```

That same CT-to-playerbase relationship is consistent with the Golden Bugs mapping: `TPHD 2.2.CT` uses `_baseaddr+1014542D`, which maps to `_playerbase+0x2A1`, matching the toto621 CT Golden Bugs byte/bitfield area.

TwilightEditor confirms a raw quest-log/save editing model for Wii U saves and handles Wii U quest-log data as a 0xE00 region. It is useful structural context, but the reviewed source does not name these inventory ownership candidates.

## Discovery Report Context

The current Ownership Discovery report compares:

- Snapshot A: `Before item`
- Snapshot B: `After item / reloaded`

The visible inventory byte at `_playerbase+0x258` changed from `255` to `64`, decoded by the trainer catalog as Gale Boomerang. Because Snapshot B was captured after reload, changed values that persist outside the visible inventory slots are good candidates. They are not confirmed ownership fields yet.

## Candidate Tester Update

Follow-up Candidate Flag Tester work showed that:

```text
0x288 = 0x40
0x293 = 0x01
0x29A = 0x02
```

can be written and can persist, but they do not grant Gale Boomerang. Therefore these offsets are not sufficient ownership flags. They may still be related progression, UI, item-state, or secondary bookkeeping fields, but they should not be promoted into Inventory ownership editing.

## Candidate Classification

| Offset | Before | After | Observed Pattern | Current Classification | Confidence |
| --- | ---: | ---: | --- | --- | --- |
| `0x253` | `0x04` | `0x0C` | Bit `0x08` appears to become set during the Gale Boomerang acquisition/reload comparison. | Candidate progression or ownership bitfield. Not identified by the CT as an inventory ownership field. | Medium candidate, unconfirmed |
| `0x288` | `0x00` | `0x40` | Changed to `64`, which is the Gale Boomerang item ID; later write/persist test did not grant the item. | Item-ID-like state or game-managed inventory/UI mirror. Not sufficient ownership. | High as state signal, low as ownership |
| `0x293` | `0x00` | `0x01` | Clean one-bit-style transition; later write/persist test did not grant the item. | Candidate secondary bitfield or progression flag. Not sufficient ownership by itself. | Medium as related flag, low as standalone ownership |
| `0x29A` | `0x00` | `0x02` | Clean one-bit-style transition; later write/persist test did not grant the item. | Candidate secondary bitfield or progression flag. Not sufficient ownership by itself. | Medium as related flag, low as standalone ownership |

## Offset Notes

### `0x253`

`0x253` is immediately before the visible inventory-slot block and changed from `0x04` to `0x0C`. The delta is `0x08`, which makes it look like a bitfield. It may be an item progression flag, a story/event flag, or a nearby inventory-state flag.

Important distinction: the CT dropdown includes item ID `253:Key Shards All Assembled` inside inventory slot dropdown lists, but that is an item ID value. It is not evidence that `_playerbase+0x253` is itself an item ID or Key Shards field.

### `0x288`

`0x288` changed from `0x00` to `0x40`. Decimal `64` is the Gale Boomerang item ID in the CT item dropdown. Because this byte took the exact item ID value, it is more suspicious as an item ID mirror, UI slot helper, active item state, or game-managed materialized field than as an ownership flag.

Do not use `0x288` as an ownership write target. It can persist when written, but the current test result says it does not grant Gale Boomerang.

### `0x293`

`0x293` changed from `0x00` to `0x01`. It sits outside the visible slot range and has a clean bit-like change. No CT-backed label was found for this byte. It can persist when written, but the current test result says it does not grant Gale Boomerang by itself.

### `0x29A`

`0x29A` changed from `0x00` to `0x02`. Like `0x293`, it sits outside the visible slot range and changed in a bit-like way. No CT-backed label was found. It can persist when written, but the current test result says it does not grant Gale Boomerang by itself.

## Determination Matrix

| Candidate | Ownership Flag | Progression Flag | Item ID | Bitfield | Story/Event Data |
| --- | --- | --- | --- | --- | --- |
| `0x253` | Possible | Possible | Unlikely | Likely | Possible |
| `0x288` | Not sufficient | Possible | Likely | Unclear | Possible |
| `0x293` | Not sufficient alone | Possible | Unlikely | Likely | Possible |
| `0x29A` | Not sufficient alone | Possible | Unlikely | Likely | Possible |

No candidate is confirmed as the authoritative inventory ownership field yet.

## Candidate Flag Tester Use

The Debug tab Candidate Flag Tester now includes presets for:

- `0x253`
- `0x288`
- `0x293`
- `0x29A`

It supports:

- Read current byte.
- Write a test byte.
- Restore the previous byte captured by Read or Write.
- Immediate readback verification.
- Logging to `logs/candidate-testing.log`.

Use this only under Advanced / Research on copied saves or save states.

## Confirmation Criteria

A candidate should only be promoted into the normal Inventory ownership editor after all of the following are true:

- The value changes naturally when the item is obtained.
- The value persists after saving/reloading.
- Writing the candidate on a copied save causes TPHD to recognize the item through the in-game menu or gameplay.
- The visible inventory slots update naturally from the candidate rather than being manually forced.
- Restoring the previous value reverses the detected ownership/progression state, or at least clearly changes the same game-managed state.
- The result reproduces across at least two save states or progression points.

## Recommended Next Tests

1. Build more Ownership Discovery exports for Gale Boomerang, Hero's Bow, Clawshot, Spinner, and Dominion Rod.
2. Use the Ownership Correlation Report to find offsets that repeatedly change across item gains, especially outside visible inventory slots.
3. Review bitfield analysis for repeated bit positions rather than only repeated bytes.
4. Test candidate combinations only on copied saves, because `0x288`, `0x293`, and `0x29A` are proven insufficient individually.
5. Test `0x253` by OR-ing bit `0x08` into the existing byte, for example `0x04 -> 0x0C`, rather than replacing the whole byte blindly.
6. After each write, close and reopen the in-game inventory menu, save/reload if needed, then check whether visible slots and item usability change without raw slot writes.
7. Restore the original byte after each isolated test and confirm the restore.

## Current Conclusion

No tested field is sufficient yet. `0x288`, `0x293`, and `0x29A` remain useful analysis signals, but current testing rules them out as standalone Gale Boomerang ownership flags. The next research step is correlation across multiple item gains and bit positions.

No inventory ownership editor feature should be implemented from these candidates yet.
