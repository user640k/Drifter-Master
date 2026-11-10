# Texture sources

`tools/make_textures.py <download dir>` builds the photo textures and embeds them in `index.html` as `IMG`, at small early-2000s sizes (128-256 px, slightly posterised and sharpened, like add-on scenery of that era). Glow and shadow gradients and signs are drawn in the game code.

The garage wall texture is sampled from the project owner's reference photo of the real track (`ref_arena.jpg`, used with permission): the crop has the photo's lighting divided out, is blended into a seamless tile and shrunk.

| File | Source | Used for |
|---|---|---|
| phWall.jpg | reference photo, grey painted wall | lower walls |
| fl_*.jpg | Wikimedia Commons national flags (Australia, Brazil, Canada, France, Germany, Ireland, Italy, Japan, Mexico, South Korea, Spain, Sweden, Netherlands, USA) (public domain) | flags round the garage walls |
| hmWallpaper.jpg | William Morris, *Strawberry Thief* (1883), via Wikimedia Commons (public domain), made seamless | home track wallpaper |
| hmFloor.jpg | Poly Haven `herringbone_parquet` (CC0) | home floor |
| hmBoard.jpg | Poly Haven `plywood` (CC0), greyed and darkened like painted board | home track board |
| hmCeiling.jpg | Poly Haven `white_plaster_02` (CC0) | home ceiling |
| hmFoil.jpg | ambientCG `Foil001` (CC0) | foil sheet on the home wall |
| hmRug.jpg | Wikimedia Commons, *Ardabil Carpet* (public domain) | home rug |
| hmTurf.jpg | ambientCG `Grass001` (CC0) | home islands |
