# Prompt lukisan interior (kota Cempaka)

Untuk mengganti interior gambar-kode (`template='indoor'`, `'cafe'`, `'mosque'`, …) dengan lukisan
tangan bergaya sama seperti `Assets/Sprites/Backgrounds/Stasiun_Interior.png` dan lukisan jalan di
`Tools/city-art/art/`.

Lampirkan **dua gambar referensi** setiap kali menagih ke model gambar:

1. `Assets/Sprites/Backgrounds/Stasiun_Interior.png` — patokan gaya (garis tepi, dinding, ubin, prop).
2. PNG interior yang sekarang, mis. `Assets/Resources/Kota/J3S_KafeSenja.png` — patokan **tata letak**:
   posisi meja, bar, pintu, dan papan nama harus sama supaya data tabrakan di `city.json` tetap cocok.

## Ukuran kanvas

Satu petak = 25 px di file final (`ART_PPU` 50, `tile` 16, `ppu` 32 → 25 px/petak). Minta gambar pada
**2× ukuran final** lalu skrip yang mengecilkan:

| Interior | Petak | File final | Minta ke model (2×) | Rasio |
|---|---|---|---|---|
| Kafe Senja | 34×16 | 850×400 | **1700×800** | 17:8 |
| Puskesmas Cempaka | 34×15 | 850×375 | 1700×750 | 34:15 |
| Masjid Al-Amanah | 36×15 | 900×375 | 1800×750 | 12:5 |
| Toko Glow | 30×15 | 750×375 | 1500×750 | 2:1 |
| Lobi Kampus | 40×15 | 1000×375 | 2000×750 | 8:3 |

Rumusnya: `lebar_px = petak × 25`. Rasionya harus persis, bukan dibulatkan — kalau modelnya hanya bisa
rasio tertentu, minta yang terdekat lalu potong/regangkan sendiri, **jangan** biarkan ada bilah kosong.

## Blok gaya (salin apa adanya)

```
Top-down 2D pixel-art interior background for a cozy Indonesian town RPG, in the exact art style of
the attached reference image (a painted train-station interior).

PROJECTION — this is the most important rule:
- The back wall is drawn face-on (seen from the front) across the top three tile rows.
- Everything below it is seen straight from above, flat top-down, like Stardew Valley or RPG Maker
  interiors: tabletops, counters and floor read as seen from the ceiling.
- No vanishing point, no isometric or 3/4 rotation, no camera tilt, no depth perspective on the floor.

STYLE:
- Hand-painted pixel art on a strict square grid, crisp hard pixel edges, no anti-aliasing haze,
  no blur, no bloom, no photographic lighting, no gradients that band.
- Every wall, prop and piece of furniture has a dark warm-brown outline (near-black brown), like the
  reference. Props sit on a soft dark contact shadow.
- Warm, muted, earthy palette matching the town: cream plaster #efe0c2, wood #8a5a3a and #6e4529,
  terracotta #b5562e, teal #2f6f6a, gold #e4b33c, off-white #f5f0e3, outline #3e1f10.
- Walls: cream plaster with a dark wood chair rail and wainscot panel along the bottom of the wall,
  plus a thin ceiling trim at the very top.
- Floor: square tiles in three close beige/taupe tones mixed irregularly, visible grout lines,
  a little honest wear — never a single flat colour, never a repeating obvious checkerboard.
- Signage: dark plate with bold white pixel letters and a thick outline, exactly like the reference.
- Lighting: soft warm ambient occlusion where walls meet the floor and under furniture.

SCALE: one grid tile = 50 px in this image. A standing human character is about 1.5 tiles tall
(75 px), so a dining table is ~2 tiles wide, a chair ~1 tile, a door opening 3 tiles.

DO NOT: no people, animals, UI, watermark, logo or signature; no text other than the signs named
below; no isometric view; no transparent background; no border, frame or empty margin — the room
fills the whole canvas; do not move, add or remove furniture beyond the list below.
```

## Blok isi (isi per ruangan)

Tulis tata letaknya dalam **koordinat petak** (x dari kiri, y dari atas), disalin dari `props=` map itu
di `generate_city.py`, supaya penghalang dan pintu di `city.json` tidak perlu digambar ulang.

```
CANVAS: {LEBAR_PX}×{TINGGI_PX} px, a grid of {W}×{H} tiles ({PX} px per tile).
ZONES: rows 0–2 = back wall (face-on). rows 3–{H-3} = floor, all furniture stands here.
rows {H-2}–{H-1} = the inside face of the front wall, with the exit door centred on tile {FRONT}.

ROOM: {nama & suasana ruangan}

BACK WALL (rows 0–2), left to right:
{daftar prop dinding + petak}

FLOOR (rows 3–{H-3}), tile coordinates are the top-left corner of each item:
{daftar perabot + petak}

FRONT WALL (bottom two rows):
{jendela / pintu / keset}
```

## Contoh siap pakai — Kafe Senja (`J3S_KafeSenja`)

```
CANVAS: 1700×800 px, a grid of 34×16 tiles (50 px per tile).
ZONES: rows 0–2 = back wall (face-on). rows 3–13 = floor, all furniture stands here.
rows 14–15 = the inside face of the front wall, with the exit door centred on tile 17.

ROOM: "Kafe Senja", a small neighbourhood coffee shop at dusk. Warm golden light spills from the
front windows across the floor, strongest near the bottom of the image. Cosy, lived-in, quiet.

BACK WALL (rows 0–2), left to right:
- tiles 1–7: a tall window with a dusk view outside — orange sky over rooftops — wooden frame.
- tiles 8–15: a dark chalkboard menu hanging above the bar, white chalk lettering "KAFE SENJA"
  and a short price list underneath.
- tiles 18–25: open wooden shelves with coffee bean sacks, glass jars, stacked mugs and a grinder.
- tiles 26–33: a carved wooden sign reading "SEDUH PELAN" with three hanging bulb lamps below it.

FLOOR (rows 3–13), tile coordinates are the top-left corner of each item:
- tiles 5–13, row 5: the barista bar — long wooden counter with a stone top seen from above,
  espresso machine, cash register, a tray of cups and a small pastry case.
- tiles 21–26, row 4: a teal upholstered booth sofa against a low wooden partition.
- tiles 0 and 31, row 4: potted plants in terracotta pots.
- rows 8–9: four round wooden tables, each with two chairs, at tiles 1, 8, 21 and 28.
- rows 11–12: three small stool tables at tiles 2, 9 and 21, and a second teal booth sofa at tile 26.
- tile 33, row 12: a small wooden bin.
- Keep the vertical strip from tile 15 to tile 19 free of furniture — that is the walkway from the
  door to the bar.

FRONT WALL (bottom two rows):
- Dark wooden panelling with warm glowing windows every three tiles.
- A wooden door with a glass pane and two brass handles centred on tile 17, three tiles wide,
  with a doormat in front of it.
```

## Setelah gambarnya jadi

1. Simpan ke `Tools/city-art/art/<file>.png` (mis. `J3S_KafeSenja.png`) — sumbernya di situ, bukan di
   `Assets/`, supaya `generate_city.py` yang menyalin & mengecilkan.
2. Cek dengan mata: garis lantai tetap di baris 3, pintunya tetap di petak tengah, dan tidak ada
   perabot yang pindah dari koordinat di atas.
3. Pipeline-nya sudah menerima lukisan interior: `install_art()` memakai rasio petak map-nya
   sendiri (toleransi 6 %, meleset > 2 % dilaporkan), dan geometrinya **tetap dari gambar kode** —
   tidak perlu entri `ART_LAYOUT`. Latar malam opsional: `art/<file>_Malam.png`.
4. Jalankan `python3 Tools/city-art/generate_city.py`, lalu periksa hasilnya di Unity.
