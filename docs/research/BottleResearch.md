# Bottle Research

## Scope

Bottle Editor v1 is a research/experimental feature for the CT-derived visible bottle slots:

- Bottle Slot 1: `_playerbase+0x263`
- Bottle Slot 2: `_playerbase+0x264`
- Bottle Slot 3: `_playerbase+0x265`
- Bottle Slot 4: `_playerbase+0x266`

These bytes may represent bottle contents rather than authoritative bottle ownership. The editor must not infer ownership, create bottles automatically, alter assigned button slots, or write non-bottle inventory slots.

## Confirmed Live Values

These values are confirmed from live TPHD memory testing and are enabled in Bottle Editor v1.

| Name | Raw Value | Status |
| --- | ---: | --- |
| Empty Bottle | 96 / 0x60 | Confirmed |
| Milk | 97 / 0x61 | Confirmed |
| Red Potion | 100 / 0x64 | Confirmed |
| Milk (1/2) | 101 / 0x65 | Confirmed |
| Lantern Oil | 102 / 0x66 | Confirmed |
| Fairy | 108 / 0x6C | Confirmed |
| Great Fairy's Tears | 115 / 0x73 | Confirmed |
| Worm | 116 / 0x74 | Confirmed |
| Bee Larvae | 118 / 0x76 | Confirmed |
| Rare Chu Jelly | 119 / 0x77 | Confirmed |
| Red Chu Jelly | 120 / 0x78 | Confirmed |
| Blue Chu Jelly | 121 / 0x79 | Confirmed |
| Green Chu Jelly | 122 / 0x7A | Confirmed |
| Yellow Chu Jelly | 123 / 0x7B | Confirmed |
| Purple Chu Jelly | 124 / 0x7C | Confirmed |
| Nothing / No Bottle | 255 / 0xFF | Confirmed clear value |

No other bottle values are exposed in Bottle Editor v1.

## External Bottle Item Reference

Zelda Dungeon's Twilight Princess Bottles page lists bottle locations and bottled item names for Twilight Princess:

https://www.zeldadungeon.net/wiki/Twilight_Princess_Bottles

The page is useful for identifying expected bottle content names, but it does not provide TPHD HD raw memory values.

## Open Questions

- Whether bottle ownership is stored separately from the visible bottle content slots.
- Whether each slot requires an ownership/progression flag before content writes are fully safe.
- Whether CT item IDs for bottle contents always match the values TPHD expects in the four visible bottle slots.
- Whether button assignments keep stale bottle content values after a bottle slot is changed.
