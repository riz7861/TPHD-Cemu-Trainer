# Heart Piece Progress Research

## Confirmed Field

Live testing confirmed Heart Piece collection progress at:

- `_playerbase+0x1BF`

The byte is a running total progress counter, and low values on early saves are valid. The trainer derives progress toward the next Heart Container using `value % 5`.

## Safe Editor Behavior

The normal editor allows only:

- `1 / 5`
- `2 / 5`
- `3 / 5`
- `4 / 5`

The selected partial progress is applied within the current cycle using:

`cycleBase = current - (current % 5)`

`newValue = cycleBase + desiredPartial`

The editor does not expose `0 / 5`, `5 / 5`, arbitrary raw values, or destructive low resets.

## Scope And Safety

This field controls progress toward the next Heart Container. It does not identify or grant a specific overworld Heart Piece and does not edit chest/history flags.

Raw value details are visible only in Developer Mode. Back up the save before editing.
