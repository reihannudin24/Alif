# Prompt Gemini — Latar Kota Cempaka (6 map)

Tujuan: melukis ulang 6 map kota buatan kode (`Assets/Resources/Kota/*.png`) dengan gaya latar
stasiun (`Assets/Sprites/Backgrounds/Stasiun_Interior.png`). Usahakan tata letaknya sedekat
mungkin dengan map kodenya; kalau meleset, `ART_LAYOUT` di `generate_city.py` yang disesuaikan
(lihat bagian 4). Map buatan kode **tidak dihapus** — tersimpan di `Tools/city-art/generated/`.

---

## Cara pakai (per map)

1. Buka Gemini (mode pembuat/pengedit gambar).
2. Unggah **2 gambar**, urutannya:
   - **Gambar 1 — gaya & skala:** `Tools/city-art/art/J3_JalanKafe.png` — lukisan yang sudah
     terpasang di game. Pakai ini, bukan `Stasiun_Interior.png`: gayanya sudah pas **dan**
     zoom-nya jadi ikut, sehingga `scale`-nya bisa langsung `.625` tanpa diukur ulang.
   - **Gambar 2 — tata letak:** map kodenya, mis. `Assets/Resources/Kota/J2_JalanPasar.png`
3. Tempel **Blok Gaya** (bagian 2) lalu **prompt map** yang sesuai (bagian 3) dalam satu pesan.
4. Minta resolusi setinggi mungkin; rasio harus **sama dengan Gambar 2 (8:3, sangat lebar)**.
   Kalau Gemini hanya memberi 16:9/21:9, tidak apa — aku yang memotong & menyesuaikan.
5. Simpan hasil ke `Tools/city-art/art/<nama file sama>.png` (mis. `J3_JalanKafe.png`).
   Jangan menimpa `Assets/Resources/Kota/` — folder itu hasil skrip.
6. Kabari aku — aku potong ke 8:3, cek posisinya terhadap `city.json`, lalu memasangnya.

**Tips kalau hasil meleset:** kirim ulang dengan kalimat
*"Keep every building and object exactly where it is in Image 2; only change the art style."*
Kalau satu bagian jelek, pakai fitur edit Gemini untuk bagian itu saja.

---

## 2. Blok Gaya (tempel di setiap prompt)

```
Redraw Image 2 as a finished game background in the exact art style of Image 1.

SCALE (critical, match Image 1): draw every object at the SAME on-screen size as in Image 1 —
a shop door, a potted plant, a street lamp and a bench must each come out the same size as the
equivalent object in Image 1. Do not zoom in. The row of shopfronts must span the full width
with roughly the same number of buildings as Image 2.

STYLE (match Image 1): polished HD pixel art with visible square pixels, clean 1-pixel dark
brown outlines, soft cel shading with gentle gradients, warm afternoon light, cozy Indonesian
small-town mood (Solo, Central Java). Low top-down 3/4 orthographic view: front walls of
buildings visible, roofs seen slightly from above, flat walkable ground. Warm palette:
terracotta roof tiles, cream plaster walls, brown wood trim, sage and leaf greens, grey-purple
asphalt, beige checker paving. Same level of detail and texture as Image 1 (tiles, wood grain,
wall panels, window glass highlights).

LAYOUT (must follow Image 2 exactly): keep the same canvas proportions, the same horizon and
ground bands, and every building, tree, lamp, bench, table and prop at the same position and
roughly the same size. Do not add new buildings or furniture on the walkable ground. Keep the
walkable ground open and clear.

RULES: no people, no characters, no animals, no cars or motorbikes moving on the road, no UI,
no watermark, no border. Signboards contain only the short Indonesian text given below, spelled
exactly; if text cannot be rendered cleanly, leave the signboard blank.
```

---

## 3. Prompt per Map

Posisi dalam persen **lebar gambar dari kiri**. Pita tanah (dari atas): rumput/pepohonan di
belakang → **tepi bawah fasad bangunan di 46% tinggi gambar** → trotoar/lantai sampai bawah.

### J2 · Jalan Pasar — file `J2_JalanPasar.png`
```
SCENE: a traditional market street.
Top band (0–46% height): grass with a few round trees behind the buildings.
Buildings standing on the line at 46% height, left to right:
- 3–19%: grocery shop, sign "TOKO KELONTONG" (green), green-and-white striped awning, wooden
  shelves packed with colorful goods, small wooden counter in front.
- 22–36%: small convenience store, sign "MINIMARKET 24" (red), red stripe, big glass windows,
  glass double door.
- 39–50%: small house, cream wall, one window, wooden door.
- 72–86%: small food stall house, sign "WARUNG SEMBAKO" (brown), yellow-white striped awning,
  snacks on a wooden counter.
- 89–98%: small house, one window, wooden door.
Sidewalk (46–71% height), beige checker paving: potted plant at 19%, street lamp at 36%, a
teal bus stop shelter with sign "HALTE" at 53%, green trash bin at 69%, potted plant at 86%, a
parked motorbike at 88%, street lamp at 97%, a bicycle at 5% (lower sidewalk).
Road (71–92% height): two-lane asphalt with white dashed center line and a zebra crossing at
62%. Bottom strip (92–100%): sidewalk.
```

### J3 · Jalan Kafe — file `J3_JalanKafe.png`
```
SCENE: a trendy café street.
Top band: grass with trees behind the buildings.
Buildings on the 46% line, left to right:
- 3–19%: cozy wooden café, sign "KAFE SENJA" (dark brown), red-and-cream striped awning, warm
  glowing windows, small chalk menu board by the door.
- 22–36%: pastel pink cosmetics shop, sign "GLOW", pink-white awning, skincare boxes displayed
  in the window, pink glass door.
- 39–52%: phone store, sign "GERAI HP" (blue), blue stripe, glass windows with phones on
  display.
- 58–73%: coffee shop, sign "KEDAI KOPI" (brown), red-cream awning, lit windows.
- 78–97%: bookstore, sign "TOKO BUKU" (teal), large glass shop windows, slightly taller building.
Sidewalk (46–71%): two outdoor café tables with orange parasols at 5% and 12%, potted plants
at 19% and 52%, street lamps at 38% and 75%, trash bin at 53%, two small round tables with
chairs at 59% and 67%, wooden bench at 86%.
Road (71–92%): asphalt with dashed line, zebra crossing at 19%. Bottom strip: sidewalk.
```

### J4 · Pusat Kota — file `J4_PusatKota.png`
```
SCENE: a clean town center plaza.
Top band: grass with trees.
Buildings on the 46% line:
- 5–23%: Islamic bank, sign "BANK SYARIAH" (teal), white columns, green accents, a gold round
  emblem above a glass entrance, two windows.
- 25–30%: small ATM kiosk, sign "ATM" (blue), with a cash machine.
- 66–88%: mosque front, sign "MASJID AL-AMANAH" (green), white walls with green arched
  windows, a green dome with a gold finial rising above the roofline.
Plaza (46–71%): a round stone fountain with water in the center at about 41–47%, flower beds
at 33% and 53%, wooden benches at 34% and 56%, street lamps at 31% and 62%, a potted palm at
89%, a small notice board at 94%, potted plant at 3%.
Road (71–92%): asphalt with dashed line, zebra crossing at 47%. Bottom strip: sidewalk.
```

### K1 · Kampus Cempaka — file `K1_Kampus.png`
```
SCENE: a private university campus front.
Top band: grass with trees behind the buildings.
Buildings on the 46% line:
- 6–44%: two-story faculty building, sign "FAKULTAS EKONOMI & BISNIS" (teal), rows of windows
  on both floors, glass main entrance in the middle.
- 45–58%: brick campus gate with two pillars and an arch, sign "UNIV. CEMPAKA" (brick red).
- 59–69%: small photocopy kiosk, sign "FOTOKOPI" (blue), a copier visible in the window.
- 72–94%: two-story rectorate building, sign "GEDUNG REKTORAT" (brown), rows of windows,
  glass entrance.
Ground (46–100%): stone and beige paved plaza with four rectangular lawn patches (around
3–19%, 28–44%, 53–69%, 78–94% width, 58–83% height), round trees at 9%, 34%, 69%, 91% on the
lawns, wooden benches at 19% and 78%, a notice board at 56%, two parked bicycles at 59–62%,
lamp posts at 47% and 53% by the gate path, a flower bed at 41% near the bottom.
```

### K2 · Taman Cempaka — file `K2_Taman.png`
```
SCENE: a green city park, open and walkable.
Top edge (0–12% height): a continuous line of round trees.
Lawn everywhere below, with a light dirt/paved footpath crossing horizontally at 50–58% height
and vertically at 48–52% width.
Objects: a wooden gazebo with a terracotta roof at 16% width (upper-left area, 25% height), an
oval pond with a stone rim and a lily pad at 62% width (29% height), round trees at 6%, 41%,
47%, 88%, 94%, benches at 34% (by the path) and 56% (lower), flower beds at 69% and 25%, a
notice board at 47% near the top, lamp posts at 31% and 78%, a green trash bin at 38%.
```

---

## 4. Setelah gambar siap

Alurnya sekarang otomatis lewat `Tools/city-art/generate_city.py`:

1. Lukisan dipotong ke rasio **8:3** lalu disimpan di `Tools/city-art/art/<file>.png`.
2. `<file>` didaftarkan di `ART_LAYOUT` dengan lantai (`walk`) & penghalang (`blocks`) yang
   dibaca dari lukisannya — satuan petak (64 × 24), boleh pecahan, y dari atas.
3. Setel `scale`-nya. Tiap hasil Gemini punya zoom sendiri, jadi **ukur dulu**: ambil properti
   yang juga ada di gambar kode (pintu 1,5 × 1,75 petak, pot 1 × 1,2, bangku 2,1 × 1,06, pohon
   2,06, tong sampah 0,75 × 0,94, lampu 3,06), bandingkan ukurannya di lukisan, lalu
   `scale ≈ 1/rasio` dibulatkan ke kelipatan ⅛. Sasaran ~0,85–0,9× skala lama. Sekarang: Kafe,
   Pusat Kota & Kampus `.625`; Taman `.875` (lukisannya memang digambar lebih kecil).
   `.625` batas bawah — di `.5` kamera berhenti mengikuti pemain secara vertikal.
4. Jalankan `python3 Tools/city-art/generate_city.py`. Skrip menyalin lukisan ke
   `Assets/Resources/Kota/<file>.png` pada **50 px per unit dunia** (sprite PPU **50**, jadi
   kerapatan detail semua map sama) dan menulis geometri `ART_LAYOUT` ke `city.json`.
5. Map buatan kode tetap digambar dan disimpan sebagai cadangan di `Tools/city-art/generated/`.

Kalau properti di lukisan bergeser dari tata letak lama, `ART_LAYOUT` (dan bila perlu `spawn`
/`npcs` di `MAPS`) yang disesuaikan — bukan lukisannya.

**Status:** sudah terpasang → J2 Jalan Pasar (`.625`), J3 Jalan Kafe (`.625`), J4 Pusat Kota
(`.625`), K1 Kampus (`.625`), K2 Taman (`.875`) — seluruh map luar Kota Cempaka. Belum →
**4 ruangan interior kampus** (Lobi, Kelas, Ruang Dosen, Toilet — gambar kode
`template='indoor'` skala 1). Kalau Gambar 1-nya `J3_JalanKafe.png` seperti langkah 2,
`scale`-nya tinggal disetel `.625`.

Untuk interior, Gambar 2-nya pakai map kodenya (mis. `Assets/Resources/Kota/K1L_Lobi.png`) dan
minta gaya interior stasiun: dinding belakang, lantai berubin, dinding depan berkaca. Rasionya
ikut map itu (bukan 8:3) — beri tahu aku, nanti `art_grid` yang aku sesuaikan.

---

## 5. Prompt interior — format grid petak

Interior tidak memakai persen seperti map jalan, tapi **koordinat petak** map itu sendiri, jadi
perabot di lukisan jatuh tepat di atas penghalang yang sudah ada di `city.json`. Aturannya:

- Kanvas = `lebar petak × 50` dikali `tinggi petak × 50` px (50 px per petak).
- Zona ikut template ruangannya: 3 baris atas dinding belakang, baris 3 s.d. tinggi−3 lantai,
  dua baris terbawah sisi dalam dinding depan, pintu keluar di petak `front`.
- Koordinat tiap benda = **sudut kiri-atas** kotaknya, diambil dari `props` di MAPS.
- Tempat berdiri NPC dan lorong pintu harus dibiarkan kosong.

Blok Gaya (bagian 2) tetap dipasang lebih dulu; bagian itu yang melarang manusia, UI, dan bingkai.

### J3P · Puskesmas Cempaka — file `J3P_Puskesmas.png`
```
CANVAS: 1700×750 px, a grid of 34×15 tiles (50 px per tile).
ZONES: rows 0-2 = back wall (face-on). rows 3-12 = floor, all furniture stands here.
rows 13-14 = the inside face of the front wall, with the exit door centred on tile 17.

ROOM: "Puskesmas Cempaka", a small neighbourhood community health clinic in the late morning.
Cool daylight from the front windows, cream walls, pale beige-and-white checkered floor tiles,
mint-green cubicle curtains. Clean, calm, orderly, a little worn from daily use.

BACK WALL (rows 0-2), left to right:
- tiles 2-6: two framed health posters (immunisation, hand washing) in dark wooden frames.
- tiles 12-19: a cream signboard with a red cross badge on its left and the word "KLINIK".
- tiles 23-24: a round wall clock in a dark wooden case.
- tiles 27-31: two more framed health posters, same frames as the left pair.
- a dark wooden dado rail runs the full width along the bottom of the wall.

FLOOR (rows 3-12), tile coordinates are the top-left corner of each item:
- tiles 1-7, row 3: a metal curtain rail with a folded mint-green privacy curtain hanging from
  it, closing examination cubicle 1; the same rail and curtain again at tiles 7-13, row 3.
- tiles 1-5, row 5: an examination bed - metal frame, white mattress, blue pillow, paper sheet;
  an identical bed again at tiles 7-11, row 5.
- tiles 12-14, row 6: a metal instrument trolley on castors, an antiseptic bottle and a gauze
  box on its top shelf.
- tiles 15-16, row 4: a small white wash basin with a chrome tap and a mirror above it.
- tile 18, row 4: a potted plant in a terracotta pot.
- tiles 20-26, row 6: the registration counter - a long wooden desk with a pale top, a short
  stack of forms and a queue-number box on it.
- tiles 27-30, row 5: a staff desk with a monitor and a paper tray.
- tiles 31-33, row 4: a tall medicine cabinet with glass doors and three shelves of small
  coloured bottles.
- rows 10-11: two rows of three linked waiting chairs, red seats on a metal frame, at tiles 3
  and 10, both facing the front door.
- tile 28, row 9: a small green pedal bin. tile 32, row 9: a second potted plant.
- Keep the vertical strip from tile 15 to tile 19 free of furniture - that is the walkway from
  the door to the registration counter - and leave tile 23, row 4 empty.

FRONT WALL (bottom two rows):
- Pale plaster with a dark skirting board and a frosted glass window every three tiles.
- A glass door with an aluminium frame and a push bar centred on tile 17, three tiles wide,
  with a dark grey doormat in front of it.
```

### J3G · Toko Glow — file `J3G_Glow.png`
```
CANVAS: 1500×750 px, a grid of 30×15 tiles (50 px per tile).
ZONES: rows 0-2 = back wall (face-on). rows 3-12 = floor, all furniture stands here.
rows 13-14 = the inside face of the front wall, with the exit door centred on tile 15.

ROOM: "Toko Glow", a small skincare and cosmetics boutique in the afternoon. Pale pink walls
above a dark wooden dado rail, cream-and-greige checkered floor tiles, white display units with
pink shelf strips and rows of pastel bottles and jars. Bright, airy, soft and girly, spotless.

BACK WALL (rows 0-2), left to right:
- tiles 1-5: a white wall shelf, three levels of small pastel skincare bottles.
- tiles 6-9: a framed promo poster - a pink serum bottle on the left, two lines of text and a
  gold discount tag on the right.
- tiles 10-16: a pink signboard with the word "GLOW" in brown letters.
- tiles 21-25 and tiles 26-30: two more white wall shelves, same as the first.

FLOOR (rows 3-12), tile coordinates are the top-left corner of each item:
- tiles 1-7 and tiles 8-14, row 5: two white display gondolas, three tiers of pastel skincare
  products each, seen from the front.
- tiles 1-7, tiles 8-14 and tiles 17-23, row 9: three more identical display gondolas.
- tiles 18-21, row 4: a wooden vanity table with a pink-framed oval mirror above it, a row of
  round bulb lights across the top of the mirror, and three tester pots on the table.
- tiles 22-23, row 4: a standing pink promo sign on a small metal foot.
- tiles 23-29, row 7: the checkout counter - a pink counter with a white top, a white cash
  register and two pastel paper shopping bags standing on it.
- tiles 25-27, row 11: a stack of three pink and lilac shopping baskets.
- tile 0 and tile 28, row 11: potted plants in terracotta pots.
- Keep the vertical strip from tile 14 to tile 17 free of furniture - that is the walkway from
  the door up to the GLOW sign - and leave tile 26, row 5 empty.

FRONT WALL (bottom two rows):
- Pale pink panelling with a dark skirting board and a glass window every three tiles.
- A glass door with a pale frame and two handles centred on tile 15, three tiles wide, with a
  doormat in front of it.
```

### J4M · Masjid Al-Amanah — file `J4M_Masjid.png`
```
CANVAS: 1800×750 px, a grid of 36×15 tiles (50 px per tile).
ZONES: rows 0-2 = qibla wall (face-on). rows 3-4 = a raised marble prayer platform.
rows 5-10 = the green prayer carpet. rows 11-12 = the tiled inner porch.
rows 13-14 = the inside face of the front wall, with the exit door centred on tile 18.

ROOM: "Masjid Al-Amanah", the prayer hall of a small neighbourhood mosque in the afternoon.
Cream plaster walls with a teal and gold border, pale marble at the front, a deep green carpet
with gold lines, dark wooden furniture. Calm, spacious, spotless, nobody inside.

QIBLA WALL (rows 0-2), left to right:
- a teal band with small gold dashes runs the full width along the very top of the wall.
- eight tall arched windows with green and pale blue glass and gold frames, four on each side
  of the mihrab, starting at tiles 2, 5, 8, 11 and 23, 26, 29, 32, each about 1.5 tiles wide.
- tiles 10 and 25, row 1: a small grey wall loudspeaker.
- tiles 13-14, row 1: a square wall clock in a pale frame.
- tiles 16-20: the mihrab - a tall arched niche with a gold outer frame, a green inner arch and
  a small gold calligraphy panel inside; its foot reaches down onto the marble platform.
- a dark wooden rail runs the full width along the bottom of the wall.

FLOOR, tile coordinates are the top-left corner of each item:
- rows 3-4: a pale marble platform across the full width, with a shallow step edge along its
  bottom where it meets the carpet.
- tiles 20-23, row 3: the mimbar - a wooden pulpit with three steps climbing from the left, a
  domed canopy with a small gold finial reaching up over the wall.
- tiles 1-3 and tiles 31-33, row 3: a low two-shelf wooden rack of Qur'an copies.
- rows 5-10: a deep green prayer carpet filling the full width, divided into three prayer rows
  by thin gold lines at the top of rows 5, 7 and 9; each row is a repeating line of individual
  prayer mats, about 2.5 tiles wide with a small arch woven into each mat.
- rows 11-12: the inner porch, beige and grey stone tiles.
- tiles 14-15, row 11: a wooden donation box on short legs with a coin slot on top.
- tiles 21-24, row 11: a two-tier wooden shoe rack with a few sandals on it.
- tile 1 and tile 34, row 11: potted plants in terracotta pots.
- Keep tiles 16-20 of the porch free of furniture - that is the walkway from the door to the
  carpet - and leave tile 8, row 5 empty.

FRONT WALL (bottom two rows):
- Cream plaster with a dark wooden rail along the top.
- A wooden double door with brass handles centred on tile 18, three tiles wide.
```

### J2L · GG Learning Center — file `J2L_GGLearning.png`
```
CANVAS: 1700×800 px, a grid of 34×16 tiles (50 px per tile).
ZONES: rows 0-3 = back wall (face-on, four rows tall - taller than the other rooms).
rows 4-13 = floor, all furniture stands here. rows 14-15 = the inside face of the front wall,
with the exit door centred on tile 17.

ROOM: "GG Learning Center", a small modern tutoring classroom in the afternoon. White walls,
a light wood plank floor, navy blue and gold branding everywhere, warm downlights. Clean,
bright, corporate but friendly, nobody inside.

BACK WALL (rows 0-3), left to right:
- tiles 0-6: four framed subject posters in a 2x2 grid, thin wooden frames, each with a small
  coloured icon square on the left and two lines of text on the right.
- tiles 6.5-9: a tall navy blue banner with gold palm tree silhouettes over a gold hill and the
  word "SOCAL" in gold at the top.
- tiles 12-22, row 0: the words "GG GENIUSGROWTH.AI" in gold across the wall.
- tiles 12-22, rows 1-3: a large wall-mounted screen in a grey frame, navy background, the
  words "WELCOME TO" in white and "GG LEARNING CENTER" in gold on the left, and a friendly
  cartoon corgi mascot in gold and cream holding a book on the right.
- tiles 22.5-28.5: a white whiteboard with "TODAY'S PLAN" underlined at the top and four ticked
  checkbox lines under it, with a marker rail along the bottom.
- tiles 28.5-31: a tall wooden bookshelf, four shelves of colourful books.
- tiles 31.5-33: the same corgi mascot again, painted on the wall as a decal.
- a navy skirting band runs the full width along the bottom of the wall.

FLOOR (rows 4-13), tile coordinates are the top-left corner of each item:
- tiles 13-21, row 5: the reception desk - a cream counter with a light wooden worktop, a big
  gold "GG" on its front panel and an open laptop standing on top.
- tile 0 and tile 33, row 5: potted plants in terracotta pots.
- tiles 3-13, row 8 and tiles 3-13, row 11: two long light wooden study tables, each with two
  open navy laptops with a gold "GG" on the lid, a white mouse beside each laptop, and two navy
  chairs with metal legs tucked in front of the table.
- tiles 21-31, row 8 and tiles 21-31, row 11: the same two tables again on the right.
- Keep the vertical strip from tile 13 to tile 21 free of furniture below the reception desk -
  that is the walkway from the door to the desk.

FRONT WALL (bottom two rows):
- Cream panelling with a navy band along the top and a glass window every three tiles.
- A glass door in a grey frame with a small gold "GG" centred on tile 17, three tiles wide.
```

### J2M · Minimarket 24 — file `J2M_Minimarket.png`
```
CANVAS: 1500×750 px, a grid of 30×15 tiles (50 px per tile).
ZONES: rows 0-2 = back wall (face-on). rows 3-12 = floor, all furniture stands here.
rows 13-14 = the inside face of the front wall, with the exit door centred on tile 15.

ROOM: "Minimarket 24", a small 24-hour convenience store at night, brightly lit inside. White
walls under a red and blue brand stripe, pale blue floor tiles with a white diamond in each
tile, white shelving with blue bases. Clean, bright, busy with products, nobody inside.

BACK WALL (rows 0-2), left to right:
- a cream ceiling strip along the very top, then a red brand stripe and a thinner blue stripe
  under it, both running the full width.
- long cream fluorescent light strips spaced evenly across the wall below the stripes.
- tiles 10-20: the words "MINIMARKET 24" in blue on the cream wall.
- a blue skirting band runs the full width along the bottom of the wall.

FLOOR (rows 3-12), tile coordinates are the top-left corner of each item:
- tiles 16-20, tiles 20-24 and tiles 24-28, row 3: three upright drink coolers standing against
  the back wall, glass double doors with metal handles, three shelves of colourful bottles and
  cans inside, blue kick plate at the bottom.
- tiles 2-8, row 5: the checkout counter - a blue counter with a white worktop, a grey cash
  register and a small red candy rack standing on it.
- tile 28, row 4: a potted plant in a terracotta pot.
- tiles 3-13, row 7 and tiles 3-13, row 10: two long shelf gondolas, three tiers of small
  colourful packets and boxes, white frame with a blue base.
- tiles 17-27, row 7 and tiles 17-27, row 10: the same two gondolas again on the right.
- tiles 13-17, row 12: a dark red entrance mat on the floor in front of the door.
- Keep the vertical strip from tile 13 to tile 17 free of furniture - that is the aisle from the
  door to the back wall.

FRONT WALL (bottom two rows):
- Cream panelling with a blue band along the top and a glass window every three tiles.
- A sliding glass door in a grey frame with two small handles, centred on tile 15, three tiles
  wide.
```

### J4B · Bank Syariah — file `J4B_BankSyariah.png`
```
CANVAS: 1700×750 px, a grid of 34×15 tiles (50 px per tile).
ZONES: rows 0-2 = back wall (face-on). rows 3-12 = floor, all furniture stands here.
rows 13-14 = the inside face of the front wall, with the exit door centred on tile 17.

ROOM: the banking hall of "Bank Syariah", a modern Indonesian islamic bank branch, mid-morning.
Warm wood slat ceiling, a turquoise light cove, taupe and light wood wall panels, gold
perforated geometric screens, cream and turquoise furniture, pale marble floor. Bright, calm,
corporate and welcoming, nobody inside.

BACK WALL (rows 0-2), left to right:
- a warm wood slat ceiling band runs the full width along the very top, with a thin turquoise
  light line glowing under it.
- tiles 0.5-8.5: four tall gold perforated screens with a repeating islamic geometric lattice
  pattern in dark wooden frames.
- tiles 10.5-23.5: a wide taupe panel carrying the backlit brand sign "BSI BANK SYARIAH" -
  "BSI" in turquoise, the rest in turquoise as well, with a gold line and a thinner turquoise
  line underlined beneath it.
- tiles 23-27.5: two more gold perforated screens, same as the left ones.
- tiles 28-33: a dark queue display screen in a grey frame showing "A-07" in turquoise.
- a wooden rail runs the full width along the bottom of the wall.

FLOOR (rows 3-12), tile coordinates are the top-left corner of each item:
- tiles 0-2, row 3: a free-standing ATM, dark grey body, turquoise screen, gold card slot.
- tiles 20-32, row 3: the teller counter - three numbered service windows in a row, each a
  turquoise pod with a white worktop, a glass screen above it and a document slot, numbered
  "1", "2" and "3" from the left, with a long cream counter and a turquoise ribbon band running
  along its front.
- tile 32, row 4: a potted plant in a terracotta pot.
- tiles 6-14, row 5: the reception desk - a cream curved counter with a warm glowing strip
  along its base, a small taupe sign board standing behind it with "BSI" in turquoise.
- tiles 4-12, row 8 and tiles 4-12, row 11: a long waiting sofa, cream frame with three
  turquoise seat cushions and small wooden feet, and a small round dark wooden side table at
  its right end.
- tiles 21-29, row 8 and tiles 21-29, row 11: the same two sofas again on the right.
- tiles 30-31, row 10: a queue ticket machine, grey body with a turquoise screen and a ticket
  slot.
- tile 0, row 11: a potted plant in a terracotta pot.
- tiles 13-21, rows 8-12: a large round marble medallion inlaid into the floor - a dark brown
  marble disc with eight cream petals and a gold centre. It is floor decoration, flat, nothing
  stands on it.
- Keep the vertical strip from tile 12 to tile 21 free of furniture - that is the walkway from
  the door to the reception desk.

FRONT WALL (bottom two rows):
- Taupe panelling with a wooden rail along the top and a glass window every three tiles.
- A wooden framed glass double door with gold handles centred on tile 17, three tiles wide.
```

### J4F · FFC — file `J4F_FFC.png`
```
CANVAS: 1700×800 px, a grid of 34×16 tiles (50 px per tile).
ZONES: rows 0-2 = back wall (face-on). rows 3-13 = floor, all furniture stands here.
rows 14-15 = the inside face of the front wall, with the exit door centred on tile 17.

ROOM: "FFC", a fried chicken fast food restaurant at lunchtime. Dark red brick back wall with a
yellow band along the top, cream floor tiles with a small red diamond at every tile corner,
deep red furniture with gold trim and cream table tops. Bright, warm, clean, nobody inside.

BACK WALL (rows 0-2), left to right:
- a yellow band runs the full width along the very top of the brick wall.
- tiles 1-10: the kitchen service window - a long stainless steel hatch with a row of orange
  warming lamps above a shelf of golden fried chicken trays.
- tiles 11-22: a lit menu board in a dark red frame, three panels with a food photo on the left
  of each and two lines of prices on the right.
- tiles 24-31: a cream plaque with a red-and-white striped chicken bucket on the left and the
  letters "FFC" in red with a gold underline on the right.
- tiles 32-33: a square wall clock in a pale frame.

FLOOR (rows 3-13), tile coordinates are the top-left corner of each item:
- tiles 2-12 and tiles 12-22, row 5: the order counter, drawn as two identical halves that meet
  into one long counter - deep red body with a gold trim line, a cream worktop, two cash
  registers and a serving tray of food standing on top.
- tiles 23-26, row 3: a standing promo sign, cream board with a red chicken bucket and the word
  "PAKET" under it.
- tiles 24-27, row 6: the tray return station - a grey bin cabinet with a red "TRAY" sign and a
  stack of trays on top.
- tiles 28-33, row 4: a self service drink dispenser - a grey machine with four taps and paper
  cups, standing on a red counter.
- tile 0, row 4: a potted plant in a terracotta pot.
- tiles 2-7, tiles 9-14, tiles 20-25 and tiles 27-32, row 8: four small dining tables, cream
  round top on a wooden pedestal, a red chair on each side and a food tray on the table.
- tiles 1-7, tiles 8-14, tiles 21-27 and tiles 28-34, row 11: four booths, a long dark wooden
  table between two tall red bench backs.
- tiles 15-19, row 13: a dark red entrance mat on the floor in front of the door.
- Keep the vertical strip from tile 14 to tile 20 free of furniture - that is the walkway from
  the door to the order counter - and leave tile 11, row 3 empty.

FRONT WALL (bottom two rows):
- Dark wooden panelling with a yellow band along the top and a glass window every three tiles.
- A sliding glass door in a pale frame centred on tile 17, three tiles wide.
```

### J4I · Idmaret — file `J4I_Idmaret.png`
```
CANVAS: 1600×750 px, a grid of 32×15 tiles (50 px per tile).
ZONES: rows 0-2 = back wall (face-on). rows 3-12 = floor, all furniture stands here.
rows 13-14 = the inside face of the front wall, with the exit door centred on tile 16.

ROOM: "Idmaret", a small neighbourhood convenience store in the afternoon. White walls under a
blue and red brand stripe, warm cream floor tiles with a faint white diamond in each tile,
white shelving with blue bases, red trim. Bright, tidy, fully stocked, nobody inside.

BACK WALL (rows 0-2), left to right:
- a blue brand stripe runs the full width along the very top, with a thinner red stripe under it.
- five long yellow fluorescent light strips spaced evenly across the cream wall below the stripes.
- tiles 11-21: the word "IDMARET" in red on the cream wall.
- a red skirting band runs the full width along the bottom of the wall.

FLOOR (rows 3-12), tile coordinates are the top-left corner of each item:
- tiles 2-6, tiles 6-10 and tiles 10-14, row 3: three upright drink coolers standing side by
  side against the back wall on the left, glass double doors with metal handles, three shelves
  of colourful bottles and cans inside, blue kick plate at the bottom.
- tiles 22-28, row 5: the checkout counter - a blue counter with a white worktop, a grey cash
  register and a small red candy rack standing on it.
- tiles 29-31, row 3: a free-standing ATM in the right corner, dark grey body, teal screen,
  gold card slot.
- tile 0, row 11: a potted plant in a terracotta pot.
- tiles 3-13, row 7 and tiles 3-13, row 10: two long shelf gondolas, three tiers of small
  colourful packets and boxes, white frame with a blue base.
- tiles 18-28, row 7 and tiles 18-28, row 10: the same two gondolas again on the right.
- tiles 14-18, row 12: a dark red entrance mat on the floor in front of the door.
- Keep the vertical strip from tile 13 to tile 18 free of furniture - that is the aisle from the
  door to the back wall.

FRONT WALL (bottom two rows):
- Cream panelling with a red band along the top and a glass window every three tiles.
- A sliding glass door in a grey frame with two small handles, centred on tile 16, three tiles
  wide.
```

### J2K · Kamar Alif — file `J2K_KamarAlif.png`
```
CANVAS: 1000×600 px, a grid of 20×12 tiles (50 px per tile).
ZONES: rows 0-2 = back wall (face-on). rows 3-9 = floor, all furniture stands here.
rows 10-11 = the inside face of the front wall, with the exit door centred on tile 10.

ROOM: "Kamar Alif", a single rented room in a small Indonesian boarding house, early morning.
Cream panelled walls above a dark wooden rail, warm beige floor tiles, plain wooden furniture,
a few personal things. Small, tidy, quiet, lived-in, nobody inside.

BACK WALL (rows 0-2), left to right:
- tiles 1-4: a two-door wooden wardrobe standing against the wall, gold handles.
- tiles 8-12: a window with a dark wooden frame, pale morning sky and a green field outside,
  warm yellow curtains tied at both sides.
- tiles 15-17: a small wooden bookshelf, four shelves of colourful books.
- a dark wooden rail runs the full width along the bottom of the wall.

FLOOR (rows 3-9), tile coordinates are the top-left corner of each item:
- tiles 1-4, row 5: a wooden study desk with a small monitor and a stack of paper on it.
- tiles 7-12, row 6: a woven floor rug, orange border around a warm yellow centre. It is floor
  decoration, flat, nothing stands on it.
- tiles 14-19, row 4: a single bed seen from above - wooden headboard on the left, white sheet,
  a cream pillow at the head and a folded teal blanket over the lower half.
- tile 18, row 8: a potted plant in a terracotta pot.
- Keep the strip from tile 5 to tile 14 free of furniture - that is the walkway from the door
  into the room - and leave tiles 12-13, row 6 empty (the floor beside the bed) and tile 4,
  row 8 empty.

FRONT WALL (bottom two rows):
- Cream panelling with a dark wooden rail along the top and a window every three tiles.
- A wooden door with a large glass pane centred on tile 10, three tiles wide.
```

### K1L · Lobi Kampus — file `K1L_Lobi.png`
```
CANVAS: 2000×750 px, a grid of 40×15 tiles (50 px per tile).
ZONES: rows 0-2 = back wall (face-on). rows 3-12 = floor, all furniture stands here.
rows 13-14 = the inside face of the front wall, with the main entrance centred on tile 20.

ROOM: "Lobi Kampus", the ground floor lobby of a small university faculty building, daytime.
Cream panelled walls above a dark wooden rail, warm beige floor tiles, plain wooden doors, teal
seating. Wide, open, quiet, institutional but friendly, nobody inside.

BACK WALL (rows 0-2), left to right:
- tiles 6-8, tiles 18-20 and tiles 30-32: three identical wooden doors with brass handles, each
  set into the wall - these are real doorways the player walks through, so draw them fully and
  leave the floor in front of each one clear.
- tiles 24-26: a small notice board on two wooden legs, a white sheet with faint lines on it.
- a dark wooden rail runs the full width along the bottom of the wall.

FLOOR (rows 3-12), tile coordinates are the top-left corner of each item:
- tile 2 and tile 37, row 4: potted plants in terracotta pots.
- tile 12, row 4: a small green bin.
- tiles 15-21, row 7: the reception counter - a long dark wooden desk with a pale worktop.
- tiles 3-6, row 9 and tiles 34-37, row 9: a teal two-seat sofa with white cushions, one on
  each side of the lobby.
- Keep the strip from tile 18 to tile 22 free of furniture below the counter - that is the
  walkway from the entrance to the reception - and leave tile 18, row 6 empty.

FRONT WALL (bottom two rows):
- Cream panelling with a dark wooden rail along the top and a glass window every three tiles.
- A wide glass entrance door in a pale frame centred on tile 20, three tiles wide.
```

### K1K · Kelas Ekonomi — file `K1K_Kelas.png`
```
CANVAS: 1400×650 px, a grid of 28×13 tiles (50 px per tile).
ZONES: rows 0-2 = back wall (face-on). rows 3-10 = floor, all furniture stands here.
rows 11-12 = the inside face of the front wall, with the exit door centred on tile 14.

ROOM: "Kelas Ekonomi", a small university classroom, morning light. Cream panelled walls above
a dark wooden rail, warm beige floor tiles, plain wooden desks with blue chairs. Tidy, empty,
ready for class, nobody inside.

BACK WALL (rows 0-2), left to right:
- tiles 2-4: a wooden bookshelf, four shelves of colourful books.
- tiles 8-15: a large whiteboard in a grey frame, a blue underline and two faint grey lines of
  writing on it.
- a dark wooden rail runs the full width along the bottom of the wall.

FLOOR (rows 3-10), tile coordinates are the top-left corner of each item:
- tiles 12-15, row 4: the lecturer's desk - a dark wooden desk with a small stack of paper on it.
- tile 24, row 4: a potted plant in a terracotta pot.
- tiles 4, 9, 14 and 19, row 6: four student desks in a row - a wooden desk about 2 tiles wide
  with a blue chair back showing behind it.
- tiles 4, 9, 14 and 19, row 8: the same four student desks again, a second row behind the first.
- Keep the strip from tile 13 to tile 16 free of furniture below the lecturer's desk - that is
  the walkway from the door - and leave tile 20, row 4 empty.

FRONT WALL (bottom two rows):
- Cream panelling with a dark wooden rail along the top and a glass window every three tiles.
- A glass door in a pale wooden frame centred on tile 14, three tiles wide.
```

### K1D · Ruang Dosen — file `K1D_RuangDosen.png`
```
CANVAS: 1200×650 px, a grid of 24×13 tiles (50 px per tile).
ZONES: rows 0-2 = back wall (face-on). rows 3-10 = floor, all furniture stands here.
rows 11-12 = the inside face of the front wall, with the exit door centred on tile 12.

ROOM: "Ruang Dosen", the lecturers' office of a small university faculty, daytime. Cream
panelled walls above a dark wooden rail, warm beige floor tiles, dark wooden office furniture.
Quiet, orderly, a working room, nobody inside.

BACK WALL (rows 0-2), left to right:
- tiles 2-4 and tiles 19-21: two wooden bookshelves, four shelves of colourful books each.
- tiles 9-11: a notice board on two wooden legs, a white sheet with faint lines on it.
- a dark wooden rail runs the full width along the bottom of the wall.

FLOOR (rows 3-10), tile coordinates are the top-left corner of each item:
- tiles 2-5, tiles 9-12 and tiles 16-19, row 6: three identical office desks in a row - a dark
  wooden desk with a small grey monitor standing on the right half and a stack of white paper
  on the left half.
- tile 21, row 4: a potted plant in a terracotta pot.
- Keep the strip from tile 11 to tile 14 free of furniture below the middle desk - that is the
  walkway from the door - and leave tile 12, row 4 empty.

FRONT WALL (bottom two rows):
- Cream panelling with a dark wooden rail along the top and a glass window every three tiles.
- A glass door in a pale wooden frame centred on tile 12, three tiles wide.
```

### K1T · Toilet Kampus — file `K1T_Toilet.png`
```
CANVAS: 1000×600 px, a grid of 20×12 tiles (50 px per tile).
ZONES: rows 0-2 = back wall (face-on). rows 3-9 = floor, all furniture stands here.
rows 10-11 = the inside face of the front wall, with the exit door centred on tile 10.

ROOM: "Toilet Kampus", a small university washroom, daytime. Pale grey-beige tiled walls and a
grey and beige checkered tile floor, white fittings, pale blue glass. Cool, clean, plain,
nobody inside.

BACK WALL (rows 0-2), left to right:
- tiles 2-4 and tiles 6-8: two wall mirrors in pale frames, a small chrome tap under each one.
- tiles 10-12 and tiles 15-17: two toilet cubicles - pale frames with a white closed door and a
  small grey lock plate on each.
- a dark rail runs the full width along the bottom of the wall.

FLOOR (rows 3-9), tile coordinates are the top-left corner of each item:
- tiles 2-3 and tiles 6-7, row 3: two white wash basins mounted on the wall under the mirrors.
- tile 17, row 5: a small green bin.
- Leave the rest of the floor empty and open, and keep tile 14, row 7 free.

FRONT WALL (bottom two rows):
- Pale panelling with a dark rail along the top and a glass window every three tiles.
- A glass door in a pale frame centred on tile 10, three tiles wide.
```

Sesudah gambarnya jadi: simpan ke `Tools/city-art/art/J3P_Puskesmas.png`. Pemasangannya belum
otomatis — `install_art()` masih menolak rasio selain 8:3 (map jalan), jadi `art_grid`/
`install_art` perlu diberi jalur khusus interior (rasio = rasio petak map, `scale` tetap 1)
sebelum lukisan interior bisa dipasang.
