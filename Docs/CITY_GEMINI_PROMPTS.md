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
