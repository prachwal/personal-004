# Test tapes

Real, protocol-valid Commodore Datasette `.tap` fixtures for `PetTapFile`/`PetTapePulseDecoder`
tests - not synthesized in code, actual captured/authored tape images.

## tower-and-dragon-town.tap

Extracted from `TowerAndDragonCassette30.zip`'s `TowerAndDragonTown30.tap`, downloaded from
[zimmers.net's Commodore archive](https://www.zimmers.net/anonftp/pub/cbm/pet/games/english/tower_and_dragon_d64.zip)
(a long-standing hobbyist preservation mirror). *Tower and Dragon* is a freeware PET game (2024
release, still under the author's copyright - not public domain), used here only as a real
pulse-encoded sample to validate the parser/decoder against actual Commodore tape data; not
redistributed as a playable game asset. The header's platform byte reads `0` (C64) rather than
`3` (PET) - almost certainly a mislabel by the authoring tool rather than evidence this isn't a
real PET tape (it was published in zimmers' `pet/games/` tree, and the pulse-cycle stream matches
the PET/Commodore shared cassette protocol - see `PetTapFileRealFixtureTests` for the actual
proof: a real `Long,Medium` start marker plus Short/Medium bit pairs, decoded through
`PetTapePulseDecoder` exactly as documented in `PetTapeCassetteFormat`).
