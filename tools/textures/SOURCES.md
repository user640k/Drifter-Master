# Texture sources

`tools/make_textures.py <download dir>` shrinks photo textures (CC0 or public domain) to small early-2000s sizes (128-512 px, slightly posterised) and embeds them in `index.html` as `IMG`. Glow and shadow gradients, rails, signs and the chain-link are drawn procedurally in the game code.

| File | Source | Licence | Used for |
|---|---|---|---|
| curtain.jpg | Poly Haven `velour_velvet` (tinted deep red) | CC0 | curtains under the driver's stand |
| arTurf.jpg | ambientCG `Grass001` | CC0 | arena turf island and strip |
| arEpoxy.jpg | Poly Haven `concrete_floor_01` (grey, darkened) | CC0 | arena epoxy track deck |
| arWall.jpg | Poly Haven `painted_plaster_wall` (tinted) | CC0 | grey painted walls (both maps) |
| arConcrete.jpg | Poly Haven `concrete_wall_004` (grey) | CC0 | arena board-formed concrete wall |
| arFloor.jpg | Poly Haven `hangar_concrete_floor` (lightened) | CC0 | arena floor outside the track |

The warehouse and living-room maps (removed for now) used more photo textures; see git history before the RC arena commit.
