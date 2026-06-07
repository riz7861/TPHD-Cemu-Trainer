# TPHD Cemu Trainer

A trainer and save editor for **The Legend of Zelda: Twilight Princess HD** running in **Cemu**.

TPHD Cemu Trainer allows you to edit inventory items, equipment, upgrades, collectibles, quest items, dungeon items, and other save-related data while the game is running.

---

# Features

## General

* Edit current health
* Edit maximum health
* Edit rupees
* Edit lantern oil

## Inventory

* Fixed-slot inventory editor
* Bottle editor
* Quest item editing
* Equipment ownership editing

## Ammo & Upgrades

* Wallet upgrades
* Quiver upgrades
* Bomb bag upgrades
* Seed bag upgrades
* Ammo editing

## Collectibles

* Poe Souls editor
* Golden Bugs editor
* Mapped Stamps editor
* Heart Piece progress tracking

## Hidden Skills

* Learn or remove Hidden Skills
* Restore skill progression

## Quest Items

* Ooccoo
* Fishing Rod
* Horse Call
* Ancient Sky Book
* Dominion Rod restoration

## Dungeon Items

* Current dungeon Map
* Current dungeon Compass
* Current dungeon Boss Key / Large Key
* Current dungeon Small Keys
* Goron Mines Key Shard completion

## Research Tools

Developer Mode includes advanced research utilities used to discover and validate save data mappings.

These tools are intended for advanced users and researchers.

---

# Requirements

Before running TPHD Cemu Trainer, install:

## .NET 9 Desktop Runtime

Download:

https://dotnet.microsoft.com/download/dotnet/9.0

## Microsoft Visual C++ Redistributable

Download:

https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist

---

# Installation

1. Download the latest release from the Releases page.
2. Extract the ZIP archive.
3. Install the required runtimes listed above.
4. Launch Cemu and load Twilight Princess HD.
5. Start TPHD Cemu Trainer.
6. Click **Attach / Rescan**.

---

# Windows SmartScreen Warning

TPHD Cemu Trainer is currently not code-signed.

Windows may display a SmartScreen warning when launching the trainer.

If downloaded from the official GitHub Releases page:

1. Click **More info**
2. Click **Run anyway**

This is normal for unsigned hobby projects.

---

# Usage

1. Start Cemu.
2. Load your save.
3. Open TPHD Cemu Trainer.
4. Click **Attach / Rescan**.
5. Make your desired changes.
6. Apply changes using the appropriate controls.

Changes are written directly to game memory.

---

# Important

Always back up your save before editing.

While most editors have been tested extensively, modifying save data always carries some risk.

Use experimental and research features carefully.

---

# Known Limitations

* Some ownership flags remain under research.
* Some quest progression data remains under investigation.
* Developer Mode contains experimental tools.
* Future releases may improve or replace current research implementations.

---

# Developer Mode

Developer Mode exposes advanced tools used during reverse engineering and save mapping.

Features may include:

* Live memory capture
* Snapshot comparison
* Candidate testing
* Research reporting
* Experimental editors

These tools are intended primarily for research purposes.

---

# Reporting Issues

If you encounter a bug:

1. Include the game version.
2. Include the trainer version.
3. Describe exactly what was edited.
4. Include screenshots if possible.
5. Include any error messages displayed by the trainer.

---

# Credits

TPHD Cemu Trainer was developed through extensive reverse engineering and live save research of Twilight Princess HD running in Cemu.

Special thanks to everyone who contributed save files, testing, and research results.

---

# Disclaimer

TPHD Cemu Trainer is provided as-is without warranty.

The authors are not responsible for save corruption, game instability, or other issues resulting from use of this software.

Always keep backup saves.

---

# Version

Current Release: **v1.0.0**
