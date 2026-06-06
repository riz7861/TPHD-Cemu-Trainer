# TPHD Cemu Trainer User Guide

## Getting Started

### Requirements

Before running the trainer, install:

* .NET 9 Desktop Runtime
* Cemu
* The Legend of Zelda: Twilight Princess HD

### Launching

1. Start Cemu.
2. Launch Twilight Princess HD.
3. Load into gameplay.
4. Start TPHD Cemu Trainer.
5. Click **Attach / Rescan**.

If the trainer successfully finds player data, the status bar will indicate that the game is attached.

---

## General Tab

The General tab contains common player values.

Available edits include:

* Current Health
* Maximum Health
* Rupees
* Lantern Oil

Changes can be applied individually or through lock controls where available.

### Heart Piece Progress

Heart Piece progress is displayed as:

```text
1/5
2/5
3/5
4/5
```

The trainer intentionally prevents unsafe values that could damage heart progression.

---

## Inventory Tab

The Inventory tab provides access to supported inventory items and bottles.

### Bottles

Bottle slots can be edited directly.

Supported contents include:

* Empty Bottle
* Milk
* Red Potion
* Lantern Oil
* Fairy
* Great Fairy's Tears
* Worm
* Bee Larvae
* Chu Jellies

Bottle ownership itself is still under research.

---

## Equipment Tab

The Equipment tab manages ownership of:

### Armor

* Hero's Clothes
* Zora Armor
* Magic Armor

### Swords

* Ordon Sword
* Master Sword
* Master Sword (Infused)

### Shields

* Wooden Shield
* Ordon Shield
* Hylian Shield

Equipment is granted through ownership flags.

Use the game's equipment menu to equip newly granted items.

---

## Ammo & Upgrades Tab

Manage:

* Wallet capacity
* Rupees
* Quiver capacity
* Arrows
* Bomb Bags
* Bomb counts
* Seed capacity
* Seed count

### Bomb Bags

Supported bomb types:

* Normal Bombs
* Water Bombs
* Bomblings

---

## Collectibles Tab

### Poe Souls

Edit collected Poe Souls.

Range:

```text
0 - 60
```

### Golden Bugs

Edit collected Golden Bugs.

The trainer supports all 24 bugs.

Note:

Agitha reward progression may still require speaking to Agitha in-game.

---

## Hidden Skills Tab

Manage all seven Hidden Skills:

1. Ending Blow
2. Shield Attack
3. Back Slice
4. Helm Splitter
5. Mortal Draw
6. Jump Strike
7. Great Spin

The trainer automatically handles prerequisite relationships between skills.

---

## Quest Items Tab

Supported items include:

* Ooccoo
* Ooccoo Jr.
* Fishing Rod
* Fishing Rod + Coral Earring
* Ilia's Charm
* Horse Call
* Ancient Sky Book

Additional tools:

* Dominion Rod Restoration
* Current Dungeon Items
* Current Dungeon Small Keys
* Goron Mines Key Completion

---

## Current Dungeon Items

Supported current-dungeon edits:

* Map
* Compass
* Boss Key / Large Key
* Small Keys

These apply to the currently active dungeon.

---

## Developer Mode

Developer Mode is disabled by default.

When enabled it provides:

* Research tools
* Live memory capture
* Snapshot comparison
* Candidate testing
* Debug information
* Experimental editors

These features are intended for advanced users and researchers.

---

## Save Safety

Always back up your save before editing.

Most features have been tested extensively, but save editing always carries risk.

Keeping multiple save backups is strongly recommended.

---

## Troubleshooting

### Trainer cannot find Cemu

Ensure:

* Cemu is running
* Twilight Princess HD is loaded
* Gameplay is active

Then click:

```text
Attach / Rescan
```

### Changes do not appear immediately

Some game menus cache values.

Close and reopen the relevant in-game menu.

### SmartScreen warning appears

Because the trainer is currently unsigned:

1. Click More Info
2. Click Run Anyway

This is expected.

---

## Support

When reporting issues include:

* Trainer version
* Game version
* Description of the problem
* Screenshots if available
* Steps to reproduce
