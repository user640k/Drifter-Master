# Texture sources

`tools/make_textures.py <download dir>` builds the photo textures and embeds them in `index.html` as `IMG`, at small early-2000s sizes (128-256 px, slightly posterised and sharpened, like add-on scenery of that era). Glow and shadow gradients, the LCD, the chain-link and signs are drawn in the game code.

Most arena textures are sampled from the project owner's reference photo of the real track (`ref_arena.jpg`, used with permission): each crop has the photo's lighting divided out, is blended into a seamless tile and shrunk.

| File | Source | Used for |
|---|---|---|
| phTrack.jpg | reference photo, foreground track surface | epoxy track deck |
| phPad.jpg | reference photo, infield pad | the pad inside the island |
| phWall.jpg | reference photo, grey painted wall | lower walls |
| phConcrete.jpg | reference photo, board-formed concrete | upper wall on the stand side |
| phRed.jpg | reference photo, red band | red band on the other walls |
| phCurtain.jpg | reference photo, stand curtains | curtains under the driver's stand |
| phDoors.jpg | reference photo, steel double doors | the doors |
| arFloor.jpg | Poly Haven `hangar_concrete_floor` (CC0, lightened) | floor outside the track |
| fl_*.jpg | Wikimedia Commons national flags (Australia, Brazil, Canada, France, Germany, Ireland, Italy, Japan, Mexico, South Korea, Spain, Sweden, Netherlands, USA) (public domain) | flags round the garage walls |
