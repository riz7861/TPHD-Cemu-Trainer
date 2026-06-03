# Bomb Slots Research

## Status

Research only. No bomb slot content write editor has been implemented.

The current trainer still supports the CT-backed bomb count and shared bomb capacity values, but the UI now labels them as **Bomb Slot 1**, **Bomb Slot 2**, and **Bomb Slot 3** instead of assuming they are independent bomb bags.

## Current Understanding

Live research suggests Twilight Princess HD may represent the visible bomb inventory as slot contents rather than three fully independent bomb bags.

The player-facing slots can contain:

- Bombs
- Water Bombs
- Bomblings

The count/capacity bytes may be stored separately from the visible slot-content bytes.

## CT-Backed Count / Capacity Values

The Cheat Engine table exposes these existing count/capacity fields:

- Bomb Slot 1 count: `_playerbase+0x2A9`
- Bomb Slot 2 count: `_playerbase+0x2AA`
- Bomb Slot 3 count: `_playerbase+0x2AB`
- Shared bomb capacity byte: `_playerbase+0x2B5`

These remain in the trainer as diagnostics and preserve existing behavior.

## Research Tool

The Ammo & Upgrades tab includes **Bomb Slot Research / Experimental**.

Default range:

- Start: `_playerbase+0x250`
- Length: `0x40`

The tool captures Before and After snapshots, compares bytes and changed bits, and exports:

- `logs/research/bomb-slots-before.json`
- `logs/research/bomb-slots-after.json`
- `logs/research/bomb-slots-report.json`
- `logs/research/bomb-slots-report.csv`

Activity is logged to:

- `logs/bomb-slot-research.log`

## Goals

- Identify which bytes represent visible bomb slot contents.
- Confirm whether Bombs, Water Bombs, and Bomblings use item IDs or a separate slot-content encoding.
- Distinguish slot contents from bomb counts and capacity.
- Avoid assuming the three slots are independent bomb bags until live memory evidence confirms that model.

## Safety

The Bomb Slot Research tool is read-only. It does not write memory, modify bomb counts, grant items, remove items, or alter button assignments.
