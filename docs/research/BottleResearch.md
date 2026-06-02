# Bottle Research

## Scope

Bottle Editor v1 is a research/experimental feature for the CT-derived visible bottle slots:

- Bottle Slot 1: `_playerbase+0x263`
- Bottle Slot 2: `_playerbase+0x264`
- Bottle Slot 3: `_playerbase+0x265`
- Bottle Slot 4: `_playerbase+0x266`

These bytes may represent bottle contents rather than authoritative bottle ownership. The editor must not infer ownership, create bottles automatically, alter assigned button slots, or write non-bottle inventory slots.

## Confirmed Live Values

These values were confirmed from live TPHD testing/mapping and are enabled in Bottle Editor v1.

| Name | Raw Value | Status |
| --- | ---: | --- |
| Empty Bottle | 96 / 0x60 | Confirmed |
| Fairy | 108 / 0x6C | Confirmed |
| Great Fairy's Tears | 115 / 0x73 | Confirmed |
| Rare Chu Jelly | 119 / 0x77 | Confirmed |
| Blue Chu Jelly | 121 / 0x79 | Confirmed |

The editor also exposes `255 / 0xFF` as **Nothing / No Bottle** for clearing a bottle slot.

## External Bottle Item Reference

Zelda Dungeon's Twilight Princess Bottles page lists bottle locations and bottled item names for Twilight Princess:

https://www.zeldadungeon.net/wiki/Twilight_Princess_Bottles

The page is useful for identifying expected bottle content names, but it does not provide TPHD HD raw memory values.

## Needs Raw-Value Confirmation

These names are known from Zelda Dungeon, but their TPHD bottle-slot raw values are not confirmed for Bottle Editor v1. They are intentionally not exposed in the UI yet.

| Name | Source | Raw Value | Status |
| --- | --- | --- | --- |
| Milk | Known from Zelda Dungeon | Unknown | Needs confirmation |
| Red Potion | Known from Zelda Dungeon | Unknown | Needs confirmation |
| Blue Potion | Known from Zelda Dungeon | Unknown | Needs confirmation |
| Lantern Oil | Known from Zelda Dungeon | Unknown | Needs confirmation |
| Bee Larva | Known from Zelda Dungeon | Unknown | Needs confirmation |
| Worm | Known from Zelda Dungeon | Unknown | Needs confirmation |
| Red Chu Jelly | Known from Zelda Dungeon | Unknown | Needs confirmation |
| Yellow Chu Jelly | Known from Zelda Dungeon | Unknown | Needs confirmation |
| Purple Chu Jelly | Known from Zelda Dungeon | Unknown | Needs confirmation |

## Open Questions

- Whether bottle ownership is stored separately from the visible bottle content slots.
- Whether each slot requires an ownership/progression flag before content writes are fully safe.
- Whether CT item IDs for bottle contents always match the values TPHD expects in the four visible bottle slots.
- Whether button assignments keep stale bottle content values after a bottle slot is changed.
