# Live Capture Research

## Status

Research only. Live Capture is read-only and does not write memory.

The goal is to observe TPHD memory continuously while the player performs an event, then rank offsets that look like persistent progression, collectible, quest, inventory, or event flags.

## UI Location

The top-level **Research** tab includes:

- Live Capture
- Snapshot Diff
- Candidate Tester
- Analysis Reports
- Logs

The older Debug tab research tools remain available for specialized workflows and support diagnostics.

## Live Capture Workflow

Default inputs:

- Start offset: `_playerbase+0x0000`
- Length: `0x400`
- Sampling rate: `250ms`

Suggested workflow:

1. Attach to Cemu and load into gameplay.
2. Enter a label such as `before-sky-book` or `hidden-village-cats`.
3. Choose a start offset and length.
4. Click **Start Capture**.
5. Perform one in-game event.
6. Save and reload if persistence matters.
7. Click **Check Persistence**.
8. Export the session.

Live Capture stores only changed addresses. Unchanged bytes are not tracked.

## Tracked Data

For each changed offset the session records:

- Offset
- Initial value
- Previous value
- Current value
- Change count
- First seen timestamp
- Last seen timestamp
- Changed bits
- Single-bit transition flag
- Persisted status
- Candidate score
- Confidence
- Reasons
- Bounded timeline entries

## Persistence Meaning

Persistence is a research hint:

- **Persisted**: current byte is still different from the baseline when checked.
- **Reverted**: current byte returned to the baseline.
- **Unknown**: persistence has not been checked or the byte changed again.

Persisted does not automatically mean ownership. Some runtime state can persist until area reload, scene transition, or save reload.

## Filters

Live Capture supports:

- Hide Frequently Changing
- Show Single-Bit Only
- Show Persisted Only
- Show Changed Once
- Hide Known Scene Noise
- Candidate Score minimum

Use filters to keep noisy gameplay state from burying likely flags.

## Candidate Scoring

Scores increase for:

- Changed once
- Stayed changed from baseline
- Single-bit transition
- Persisted after a check
- Located near useful research regions

Scores decrease for:

- Repeated changes
- Reverted state
- Large/noisy bit transitions
- Known scene/location/runtime noise

Known-region hints:

- `0x26A-0x26E`: confirmed/researched inventory special item slots.
- `0x298-0x29B`: ownership/progression candidates.
- `0x3D1`: confirmed Dominion Rod restoration bit.
- `0x3D5-0x3D6`: confirmed Hidden Skills ownership/progression bytes.
- `0xFD1`: confirmed current dungeon Map/Boss Key/Compass bits.
- `0x221-0x22D`: known scene/location/runtime noise.

Hints are not editor mappings. They only affect research ranking.

## Exports

Live Capture exports JSON to:

- `logs/research/live-capture-session_<timestamp>_<label>.json`

The JSON includes:

- Session metadata
- Selected range
- Sampling rate
- Tracked addresses
- Timeline data
- Candidate scores
- Persistence results
- Applied filters
- Known-region hints

Runtime activity is logged to:

- `logs/live-capture.log`

## Safety

Live Capture never writes memory.

The adjacent **Candidate Tester** is experimental and does write one raw byte only when the user clicks **Write**. Use copied saves or save states for candidate writes.
