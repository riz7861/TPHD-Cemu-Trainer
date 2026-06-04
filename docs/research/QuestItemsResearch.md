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

This feature is read-only. It does not grant quest items, change progression flags, write inventory slots, or modify story state.

Use copied saves or save states when researching progression changes so you can repeat the same before/after comparison safely.
