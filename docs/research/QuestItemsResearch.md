# Quest Items Research

## Status

Research only. No Quest Items editor has been implemented.

The goal is to identify authoritative quest-item and progression fields before any write controls are added.

## UI Location

The Quest Items tab includes **Quest Items Research / Experimental**.

## Default Range

The research tool captures a configurable `_playerbase`-relative byte range.

Default:

- Start: `_playerbase+0x200`
- Length: `0x200`

The range can be changed in the UI when comparing different candidate areas.

## Workflow

1. Load a save before receiving or advancing a quest item.
2. Click **Capture Before**.
3. Progress naturally in-game.
4. Click **Capture After**.
5. Click **Compare**.
6. Export JSON or CSV for notes and offline comparison.

The tool does not write memory.

## Compare Output

Each compared byte shows:

- Offset
- Before byte
- After byte
- Before binary
- After binary
- Changed bit list
- Candidate score
- Nearby candidate group

Candidate scoring favors changed bytes, single-bit changes, small bit-count changes, and nearby grouped changes.

## Logs And Exports

Activity is logged to:

- `logs/quest-items-research.log`

Exports are written to:

- `logs/research/quest-items-before.json`
- `logs/research/quest-items-after.json`
- `logs/research/quest-items-report.json`
- `logs/research/quest-items-report.csv`

## Safety

This feature is read-only. It does not grant quest items, change progression flags, write inventory slots, or modify story state.

Use copied saves or save states when researching progression changes so you can repeat the same before/after comparison safely.
