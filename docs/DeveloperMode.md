# TPHD Cemu Trainer Developer Mode

## Overview

Developer Mode exposes advanced tools used during reverse engineering, save research, memory analysis, and feature development.

Developer Mode is disabled by default.

Most users should leave Developer Mode turned off.

The normal trainer interface contains all supported public editing features.

---

## Enabling Developer Mode

Open:

```text
Options → Developer Mode
```

A warning dialog will appear.

Accepting the warning enables Developer Mode for the current session.

A visible indicator appears in the trainer while Developer Mode is active.

---

## Purpose

Developer Mode exists to:

* Research new save mappings
* Validate memory offsets
* Compare before/after save states
* Investigate progression flags
* Test candidate memory locations
* Develop future trainer features

Many Developer Mode features are experimental.

---

## Research Tab

The Research tab contains tools used during reverse engineering.

### Live Capture

Monitors a selected memory range while gameplay is active.

Useful for:

* Tracking changing values
* Finding progression flags
* Detecting ownership changes

Live Capture is read-only.

It does not write memory.

---

### Snapshot Diff

Captures memory state before and after an event.

Examples:

* Obtaining an item
* Learning a Hidden Skill
* Completing a dungeon
* Collecting a Heart Piece

Differences can then be reviewed and exported.

---

### Candidate Tester

Allows direct testing of candidate offsets.

Used during:

* Ownership flag research
* Quest item research
* Progression analysis

Only advanced users should use this tool.

---

### Analysis Reports

Displays exported research reports.

Reports may include:

* Candidate rankings
* Snapshot comparisons
* Progression analysis
* Exported research data

---

## Debug Tab

The Debug tab provides detailed internal diagnostics.

Examples include:

* Attach status
* Memory scan results
* Player base information
* Scan timing
* Verification results

Useful when troubleshooting issues.

---

## Advanced Inventory Tools

Developer Mode exposes additional inventory research tools.

Examples:

* Raw inventory values
* Fixed-slot diagnostics
* Experimental inventory testing
* Research-only inventory controls

These tools are not intended for normal gameplay editing.

---

## Advanced Quest Item Tools

Developer Mode provides access to:

* Raw quest item values
* Quest research utilities
* Progression investigations
* Candidate discovery workflows

Not all discovered values are fully understood.

---

## Advanced Golden Bug Tools

Developer Mode exposes:

* Raw ownership bytes
* Bit-level diagnostics
* Collection testing
* Experimental research controls

The standard Collectibles tab should be used for normal editing.

## Stamp Diagnostics

Developer Mode exposes raw Stamp offsets, bit numbers, byte values, pending changes, verification status, and session restore controls. Normal Mode shows only the 46 confirmed mapped Stamp names and collection status.

---

## Advanced Bottle Tools

Developer Mode may expose:

* Raw bottle values
* Content diagnostics
* Verification information
* Experimental testing tools

Normal users should use the standard Bottle Editor.

---

## Logging

Developer Mode generates additional diagnostic logs.

These may include:

* Scan information
* Verification reports
* Research exports
* Candidate rankings
* Snapshot data

Log files are stored within the trainer's logs folder.

---

## Safety

Developer Mode may expose tools that:

* Write raw memory
* Test candidate values
* Operate on partially researched offsets

Always use copied saves when performing research.

Back up saves before testing experimental features.

---

## Intended Audience

Developer Mode is intended for:

* Reverse engineers
* Save researchers
* Trainer developers
* Advanced users

Most players should leave Developer Mode disabled.

---

## Future Research

Developer Mode continues to be used for:

* Quest progression mapping
* Inventory ownership research
* Additional dungeon item mapping
* Story flag discovery
* Collectible research
* Future trainer features

Features may change between releases as new discoveries are made.
