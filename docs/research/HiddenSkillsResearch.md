# Hidden Skills Research

## Status

Hidden Skill ownership/unlock flags are not confirmed yet.

The trainer includes a read-only **Hidden Skills Research** workflow in the Debug tab. It captures and compares a configurable playerbase-relative memory range, defaulting to:

- Start: `_playerbase+0x200`
- Length: `0x100`

Before and After captures are persisted immediately so the trainer and Cemu can be closed between save states.

No Hidden Skills editing is implemented.

Current findings:

- Candidate bit testing has been performed, but no Hidden Skill ownership bit is confirmed.
- Hidden Skills likely involve multiple progression states, such as wolf/howling interactions, Hero's Shade lesson state, tutorial completion, and final menu visibility.
- The default `0x200-0x300` research area remains useful, but the authoritative state may be elsewhere in player data.
- Live Memory Watch was added for direct observation while triggering Howling Stones, Golden Wolves, Hero's Shade lessons, and menu updates.

## Known Skills

- Ending Blow
- Shield Attack
- Back Slice
- Helm Splitter
- Mortal Draw
- Jump Strike
- Great Spin

## Expected Storage

Current expectation is that Hidden Skills may be stored as either:

- A small bitfield, where each learned skill toggles one bit.
- A nearby group of progression/event flags.

No offset, bit, or byte mapping is confirmed yet.

## Suggested Workflow

1. Enter a capture label, such as `Forest Temple Ending Blow`.
2. Save Before Capture.
3. Close/reopen the trainer or Cemu if needed.
4. Load Before Capture from disk.
5. Learn one Hidden Skill naturally or load the after-save.
6. Save After Capture, or load a previous After capture.
7. Compare Loaded Captures.
8. Export JSON and CSV reports.

If possible, capture After from a reloaded save so the tool can mark changes as persisted candidates.

## Outputs

The research tool writes:

- `logs/hidden-skills-research.log`
- `logs/research/hidden-skills-captures/hidden-skills-before_<timestamp>_<optional-label>.json`
- `logs/research/hidden-skills-captures/hidden-skills-after_<timestamp>_<optional-label>.json`
- `logs/research/hidden-skills-before.json`
- `logs/research/hidden-skills-after.json`
- `logs/research/hidden-skills-report.json`
- `logs/research/hidden-skills-report.csv`
- `logs/research/hidden-skills-candidate-groups.json`
- `logs/research/hidden-skills-candidate-groups.csv`
- `logs/research/hidden-skills-multi-capture-ranking.json`
- `logs/research/hidden-skills-multi-capture-ranking.csv`
- `logs/research/hidden-skills-region-analysis.json`
- `logs/research/hidden-skills-region-analysis.csv`
- `logs/research/hidden-skills-live-watch.json`
- `logs/research/hidden-skills-live-watch.csv`
- `logs/hidden-skills-bit-testing.log`
- `logs/hidden-skills-live-watch.log`

Capture files include:

- Timestamp
- Playerbase-relative start offset
- Length
- Raw bytes
- Optional label

## Candidate Scoring

Rows score higher when they:

- Changed between snapshots.
- Changed exactly one bit.
- Were captured after reload/persistence.
- Are clustered near other changed bytes.
- Were pinned manually as suspected Hidden Skill candidates.

This score is a research aid only. It is not confirmation.

## Filtering

The Debug tab filter can show only rows matching likely candidate criteria:

- Single-bit changes
- Persisted changes
- Candidate score `>= 6`

Pinned offsets are always shown while filtering is enabled.

## Pinning

Use the Pin checkbox beside a row to mark that offset as a Hidden Skill candidate. Pins are session-only research annotations, but they are included in reports and candidate-group exports.

## Candidate Groups

Candidate rows are grouped automatically when nearby offsets are within 4 bytes of each other.

Example group shapes:

- Group A: `0x20F`, `0x210`, `0x214`, `0x218`
- Group B: `0x238`, `0x239`
- Group C: `0x272`

Candidate groups can be exported to:

- `logs/research/hidden-skills-candidate-groups.json`
- `logs/research/hidden-skills-candidate-groups.csv`

## Multi-Capture Analyzer

The **Hidden Skills Multi-Capture Analyzer** is an offline workflow for comparing multiple saved captures from different progression saves.

It supports six loaded capture slots:

- Capture A: known skill count `1`
- Capture B: known skill count `2`
- Capture C: known skill count `3`
- Capture D: known skill count `5`
- Capture E: known skill count `6`
- Capture F: known skill count `7`

The skill-count fields are editable, but the default sequence is intended for the current temple/progression save set where a four-skill capture is unavailable.

The analyzer ranks:

- Offsets whose raw byte values change monotonically.
- Individual bits that only ever increase from clear to set.
- Candidate byte value fields where the raw byte follows the known skill-count sequence.
- Candidate byte-level bitfields where the set-bit count follows the known skill-count sequence.

Rows where the raw byte value or set-bit count progresses `1 -> 2 -> 3 -> 5 -> 6 -> 7` are highlighted as strong candidates. Ranking exports are written to:

- `logs/research/hidden-skills-multi-capture-ranking.json`
- `logs/research/hidden-skills-multi-capture-ranking.csv`

## Region Comparison Analyzer

The **Hidden Skills Region Comparison** section compares two persisted captures over a larger playerbase-relative range and ranks sparse changed regions.

Inputs:

- Capture A
- Capture B
- Start offset
- Length

Presets:

- `0x000` length `0x400`
- `0x000` length `0x800`
- `0x000` length `0x1000`

The analyzer groups nearby changed bytes into candidate regions, then reports:

- Region
- Changed bytes
- Changed bits
- Density percentage
- Largest byte delta
- Candidate score

Sparse regions and single-bit regions are highlighted. Exports are written to:

- `logs/research/hidden-skills-region-analysis.json`
- `logs/research/hidden-skills-region-analysis.csv`

## Live Memory Watch

The **Hidden Skills Live Memory Watch** section reads a live playerbase-relative range every `250ms`.

Defaults:

- Start: `_playerbase+0x000`
- Length: `0x1000`

Use it while interacting with:

- Howling Stones
- Golden Wolves
- Hero's Shade
- Hidden Skill tutorials
- Skill completion and menu appearance

The watch displays:

- Timestamp
- Offset
- Before byte
- After byte
- Binary before/after
- Changed bits

Rows highlight single-bit changes, repeated changes at the same offset, and monotonic byte changes. Activity is logged to `logs/hidden-skills-live-watch.log`.

## Event Tracking

The live watch includes event markers. Use **Mark Event** to timestamp moments such as:

- Howling Stone Activated
- Wolf Appeared
- Lesson Started
- Lesson Completed
- Skill Appeared In Menu

Event markers are included in live watch JSON and CSV exports:

- `logs/research/hidden-skills-live-watch.json`
- `logs/research/hidden-skills-live-watch.csv`

## Bit Tester

The **Hidden Skills Bit Tester / Experimental** section can test one candidate bit without promoting it to a real editor.

Controls:

- Offset
- Bit
- Read Current
- Toggle State
- Apply

Suggested presets:

- `0x218` bit `0`
- `0x219` bit `0`
- `0x214` bit `1`
- `0x238` bit `0`

Apply writes only the selected bit and preserves all other bits in the same byte. The tester verifies immediate, 250ms, and 1000ms readbacks and logs to `logs/hidden-skills-bit-testing.log`.

## Safety

Hidden Skills snapshot capture and comparison do not write memory. The experimental bit tester writes one selected bit only when Apply is clicked. Use copied saves or save states when testing candidate bits.
