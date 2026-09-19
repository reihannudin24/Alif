# Brief Aset — Kota Cempaka & NPC Baru

Untuk digenerate di tool AI (latar) dan **PixelLab** (karakter), lalu dirakit ke game. Desain
quest yang memakai aset ini: [`QUEST_DESIGN.md`](QUEST_DESIGN.md).

---

## 1. Denah Kota (target ±10× peta sekarang)

Kota dibangun sebagai **deretan area** yang tersambung lewat tepi kiri/kanan dan pintu (seperti
Depan stasiun ↔ Depan warung sekarang), bukan satu gambar raksasa — lebih ringan untuk WebGL dan
lebih mudah digenerate konsisten.

```
                         ┌──────────── JALAN KAMPUS ────────────┐
                         │ [K1 Gerbang Kampus] [K2 Taman Cempaka]│
                         └───────────────┬───────────────────────┘
┌──────────────────────────────── JALAN CEMPAKA (jalan utama) ────────────────────────────────┐
│ [J1 Depan Stasiun]*  [J2 Jalan Pasar]  [J3 Jalan Kafe]  [J4 Pusat Kota]  [J5 Depan Warung]* │
└───────────────────────────────────────────────────────────────────┬─────────────────────────┘
                                                            [P1 Gang Permukiman] (Kos*, rumah Bima)
* = sudah ada
```

| Kode | Area luar (latar baru) | Bangunan yang bisa dimasuki | Dipakai quest |
|---|---|---|---|
| J2 | **Jalan Pasar** | Toko Kelontong, Minimarket 24 | K-pop SQ-1 |
| J3 | **Jalan Kafe** | Kafe Senja, Toko Kosmetik "Glow", Gerai HP | K-pop SQ-3, SQ-4 |
| J4 | **Pusat Kota** | Bank Syariah (+ATM), Masjid (luar saja) | K-pop SQ-2, Dosen B |
| K1 | **Gerbang Kampus** | Gedung Fakultas → Ruang Dosen | Dosen A |
| K2 | **Taman Cempaka** | — (taman terbuka, disebut di Bab 5) | Epilog / ambience |
| P1 | **Gang Permukiman** | Rumah & lapak Bima | Dosen A, B |

| Kode | Interior (latar baru) |
|---|---|
| I1 | Dalam Toko Kelontong |
| I2 | Dalam Kafe Senja |
| I3 | Dalam Bank Syariah (teller, meja CS, antrean) |
| I4 | Ruang Dosen (meja kerja, rak buku, papan tulis) |
| I5 | Rumah Bima (ruang tamu sederhana + etalase dagangan) |
| I6 | Dalam Toko Kosmetik "Glow" |

---

## 2. Spesifikasi Teknis Latar (wajib, supaya serasi dengan stasiun)

| Hal | Nilai |
|---|---|
| Ukuran | **2752 × 1536 px** (16:9, sama dengan `KosKosan_Halaman.png`) — interior boleh 2400 × 1600 |
| Skala di game | PPU 200 → ±13,8 × 7,7 unit per area |
| Sudut pandang | **Low top-down / 3/4 ortografis**, sama dengan Stasiun & Warung (dinding belakang terlihat, lantai luas) |
| Gaya | Pixel art bersih, piksel terlihat, shading lembut, palet hangat |
| Palet | Atap terakota, dinding krem, kayu coklat, hijau sage, aspal abu keunguan, ubin krem |
| Isi | **Tanpa orang, tanpa kendaraan di jalur jalan**, lantai/trotoar luas untuk berjalan |
| Tepi sambung | Area jalan: **jalan raya di sepertiga bawah, trotoar di tengah, bangunan di atas** — posisi sama di setiap area supaya sambungan kiri-kanan terasa menyatu |
| Teks | Hanya papan nama pendek berbahasa Indonesia (tulis persis di prompt). Kalau teks AI rusak, biarkan papan kosong — aku tambahkan teks di game |
| Format | PNG, tanpa watermark, tanpa UI |

**Kerangka prompt (salin, lalu isi [ADEGAN]):**

> Top-down 3/4 view pixel art game background, orthographic low top-down angle, clean pixel
> art with visible pixels and soft shading, warm late-afternoon light, cozy Indonesian small
> town (Central Java vibe). [ADEGAN]. Cohesive warm palette: terracotta roof tiles, cream
> plaster walls, brown wood, sage green plants, grey-purple asphalt, cream checker paving.
> Wide open walkable floor area, no people, no characters, no cars on walkways, no UI, no
> watermark. 16:9 horizontal composition, 2752x1536.

---

## 3. Prompt [ADEGAN] per Area

**J2 · Jalan Pasar** — *A street segment: a traditional Indonesian grocery shop "TOKO KELONTONG"
with green-and-white striped awning and shelves full of colorful goods visible at the front, next
to a small modern convenience store "MINIMARKET 24" with glass doors and a red sign. Sidewalk in
the middle with potted plants and a bus stop bench, two-lane asphalt road across the bottom third.*

**J3 · Jalan Kafe** — *A street segment: a cozy wooden café "KAFE SENJA" with warm lit windows and
two small outdoor tables with chairs, a bright cosmetics shop "GLOW" with pastel pink storefront
and product posters, and a small phone store "GERAI HP" with a blue sign. Sidewalk with a lamp
post and planters, road across the bottom third.*

**J4 · Pusat Kota** — *A clean town-center segment: an Islamic bank building "BANK SYARIAH" with
white columns, green accents and a small ATM booth beside it, and the front of a mosque with a
green dome and arched gate on the other side. Small plaza with a fountain and benches, road across
the bottom third.*

**K1 · Gerbang Kampus** — *A private university gate "UNIVERSITAS CEMPAKA" with a brick arch and
security post, a tree-lined path leading to a two-story faculty building, a photocopy kiosk
"FOTOKOPI" beside the gate, bicycles parked neatly.*

**K2 · Taman Cempaka** — *A green city park with paved walking paths, frangipani trees, a small
gazebo, benches, and a notice board, flower beds; open grass areas to walk.*

**P1 · Gang Permukiman** — *A narrow residential alley with small row houses, potted plants, a
clothesline, one house with a tiny front stall selling snacks (Bima's house), a boarding house
gate "KOS CEMPAKA", motorbikes parked at the edges only.*

**I1 · Dalam Toko Kelontong** — *Interior of a small grocery shop: wooden shelves of rice sacks,
snacks, soap and drinks, a cashier counter with an old cash register, a fridge with drinks.*

**I2 · Dalam Kafe Senja** — *Interior of a cozy café: wooden tables and chairs, a coffee bar with
espresso machine and menu board, plants, warm pendant lights, a corner with a bookshelf.*

**I3 · Dalam Bank Syariah** — *Interior of an Islamic bank branch: teller counters with glass
dividers, a customer service desk, a queue number machine, waiting chairs, green-white decor,
information posters (blank).*

**I4 · Ruang Dosen** — *A university lecturer office: a wooden desk with laptop and stacked
papers, bookshelves, a whiteboard with charts, a small sofa for guests, a window.*

**I5 · Rumah Bima** — *A modest living room of a small house: a sofa, a low table, a small glass
display case of packaged snacks for sale, a calendar on the wall, a fan.*

**I6 · Dalam Toko Kosmetik "Glow"** — *A bright cosmetics store interior: pastel shelves of
skincare sets, a promotion stand with idol-style posters (faces blank), a cashier counter.*

Simpan ke: `Assets/Sprites/Backgrounds/Kota/<Kode>_<Nama>.png` (mis. `J3_JalanKafe.png`).

---

## 4. Karakter NPC (PixelLab)

Samakan dengan karakter yang ada: **template `mannequin`, 256 × 256, 8 arah, view "low
top-down"**, animasi **Walking** (untuk NPC yang berjalan) atau Idle saja (NPC diam). Akhiri setiap
prompt dengan: *"Clean pixel art style, chibi proportions, idle standing pose facing forward."*

| Id folder | Prompt |
|---|---|
| `kirana` | Cheerful Indonesian teenage girl (17), long black hair in a half-up ponytail with a small star hair clip, excited sparkling eyes. Oversized pastel lilac hoodie with a tiny idol-group logo, pleated navy skirt, white sneakers, tote bag with a photocard holder keychain. Hugging a stack of four small skincare boxes. |
| `tara` | Calm Indonesian young Muslim woman (20), soft pink hijab, round glasses, gentle smile. Light denim jacket over a white tee, long beige skirt, canvas sneakers. Holding a smartphone in one hand and a white concert lightstick in the other. |
| `pak_fadli` | Polite Indonesian man in his mid-30s, neat short black hair, thin mustache, rectangular glasses, slightly troubled expression. Light blue batik long-sleeve shirt, khaki trousers, brown leather shoes. Holding a leather folder under his arm. |
| `bima` | Indonesian man in his mid-30s, short wavy hair, light stubble, anxious guilty expression with a sweat drop. Faded navy polo shirt, jeans, rubber sandals. Holding an old cracked smartphone. |
| `petugas_bank` | Friendly Indonesian young woman bank officer, dark green hijab, green-and-white bank uniform blazer with a small gold name badge, black trousers. Holding a clipboard, professional warm smile. |

Ekspor ZIP PixelLab → ekstrak ke `Assets/Sprites/Characters/<id>/` (pertahankan
`metadata.json`, `Idle/rotations/`, `Idle/animations/Walking/`).

---

## 5. Setelah Aset Masuk

Aku akan: potong/cek ukuran, set import (PPU, Point filter), susun area & pintu antar-area,
buat collider lantai (WalkableArea) dan penghalang furnitur, pasang NPC & titik interaksi,
lalu memindahkan quest dari lokasi placeholder ke lokasi finalnya.
