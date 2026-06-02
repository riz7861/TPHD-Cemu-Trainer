# Hidden Skills Research

## Status

Hidden Skill ownership/unlock flags are not confirmed yet.

The trainer includes a read-only **Hidden Skills Research** workflow in the Debug tab. It captures and compares a configurable playerbase-relative memory range, defaulting to:

- Start: `_playerbase+0x200`
- Length: `0x100`

No Hidden Skills editing is implemented.

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

1. Capture Before.
2. Learn one Hidden Skill naturally.
3. Capture After.
4. Compare.
5. Export JSON and CSV reports.

If possible, capture After from a reloaded save so the tool can mark changes as persisted candidates.

## Outputs

The research tool writes:

- `logs/hidden-skills-research.log`
- `logs/research/hidden-skills-before.json`
- `logs/research/hidden-skills-after.json`
- `logs/research/hidden-skills-report.json`
- `logs/research/hidden-skills-report.csv`

## Candidate Scoring

Rows score higher when they:

- Changed between snapshots.
- Changed exactly one bit.
- Were captured after reload/persistence.
- Are clustered near other changed bytes.

This score is a research aid only. It is not confirmation.

## Safety

Hidden Skills Research does not write memory. It is intended only to discover candidate ownership, unlock, or progression bytes for later live testing.
