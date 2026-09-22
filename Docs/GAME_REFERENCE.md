# Alif — Referensi Game (Cerita, Aksi, UI)

Referensi tunggal untuk vibe coding: cerita lengkap, semua aksi pemain, sistem, gaya UI, dan
peta file. Sumber kebenaran tetap kode — kalau dokumen ini dan kode berbeda, kode yang benar,
lalu perbarui dokumen ini. Arsitektur teknis ada di [`ARCHITECTURE.md`](ARCHITECTURE.md).

---

## 1. Ringkasan

**Alif** adalah game petualangan edukatif 2D pixel-art (Unity 6000.6.0f1, URP) tentang
**literasi keuangan dan muamalah syariah**. Pemain menjadi **Alif**, perantau yang pulang ke kota
Cempaka/Solo, lalu membantu tetangga menghadapi masalah uang sehari-hari lewat dialog,
puzzle bukti, dan pertarungan "godaan" di dalam pikiran.

- **Nada:** hangat, tidak menghakimi, berbasis bukti. Konflik diselesaikan dengan fakta dan
  kesepakatan, bukan tuduhan. Tidak ada uang sungguhan, laporan, atau transaksi yang dikirim game.
- **Tema per bab:** kejelasan & pencatatan (enam kasus warga: harga, wakaf, kembalian, kas, upah,
  utang) → riba → maysir (judi/undian) → tadlis (penipuan label) → skema Ponzi.
- **Bahasa:** seluruh teks dalam game berbahasa Indonesia.

---

## 2. Alur Scene

```
MainMenu
 ├─ MAIN BARU ──► Chapter1Cutscene ──► AdventureChapter1 ──► Chapter1Ending ──► ChapterSelect
 └─ LANJUTKAN ──► ChapterSelect ──► AdventureChapterN (N = 1..5)
                               └─► Chapter2Cutscene ──► AdventureChapter2
Bab 2–4 selesai ──► langsung AdventureChapter(N+1);  Bab 5 selesai ──► MainMenu
```

| Scene | Isi |
|---|---|
| `MainMenu` | Logo, MAIN BARU / LANJUTKAN (nonaktif tanpa save) / KELUAR (+ popup konfirmasi). |
| `Chapter1Cutscene` | Prolog "Solo, sore itu…" lalu gambar layar penuh gaya visual novel. |
| `AdventureChapter1..5` | Gameplay utama (`AdventureGame`). |
| `Chapter1Ending`, `Chapter2Cutscene` | Cutscene penutup Bab 1 & pembuka Bab 2. |
| `ChapterSelect` | Kartu bab yang sudah terbuka. |
| `SampleScene`, `ChapterNGameplay` | Warisan/legacy; scene Adventure dibangun dari SampleScene. |

---

## 3. Karakter

| Karakter | Peran |
|---|---|
| **Alif** | Protagonis. Rapi, berkacamata, membawa buku catatan (Jurnal). Suara batin: "Alif (Batin)". |
| **Bu Siti** | Pemilik Warung Bu Siti. Jujur, menulis harga besar-besar. |
| **Raka** | Pencari kerja sambilan; malu soal uang terbatas; tergoda undian di Bab 3. |
| **Dimas** | Mahasiswa di Kos Cempaka lantai 2; terjerat tawaran pinjaman (Bab 2) dan undangan investasi (Bab 5). |
| **Naya** | Berhijab hijau; relawan bazar & pencatat fakta (Bab 4–5). Di stasiun Bab 1 berdiri antre di Loket Karcis. |
| **Ustadz Farid** | Tempat bertanya soal syarat akad (Bab 2). |
| **Petugas stasiun** | Berjaga di ruang Informasi stasiun (Bab 1). |
| **Penjual** | Penjual radio di bazar (Bab 4). |

---

## 4. Cerita per Bab

Setiap bab adalah rangkaian **tugas berurutan** (tugas berikutnya terbuka setelah yang
sebelumnya selesai). Data: `Assets/Scripts/Adventure/Model/AdventureContent.cs`; area,
papan puzzle, dan pemetaan area ditimpa oleh `OriginalCampaign.cs`. Bab 2–5 punya tugas
**"Hadapi godaan"** (encounter) yang disisipkan tepat sebelum tugas penyelesaian.

Area yang tampil di game:
- **Bab 1, 4, 5:** Dalam stasiun · Depan stasiun · Depan warung · Dalam Warung Bu Siti
- **Bab 2, 3:** Kamar Dimas · Lantai 1 kos · Halaman kos
- **Semua bab (Kota Cempaka, ditambahkan setelah area bab):** Jalan Pasar · Jalan Kafe ·
  Pusat Kota · Kampus Cempaka · Taman Cempaka

### Kota Cempaka (5 map)
Dirakit oleh `Tools/city-art/generate_city.py` (sumber tunggal tata letak: lantai, penghalang,
spawn, posisi NPC) → `Assets/Resources/Kota/*.png` + `city.json`, lalu dirakit saat runtime oleh
`Adventure/CityWorld.cs` (tanpa mengedit scene). Setiap map 64×24 petak = 32×12 unit, dijajarkan
di y≈−60 (di bawah plafon Y-sort prompt 8000). Ubah tata letak → jalankan ulang skrip. Nama area
harus sama dengan `AdventureContent.CityAreas`.

**Latar map ada dua jenis:**

| Jenis | Map | Gambar | PPU sprite | Geometri |
|---|---|---|---|---|
| Lukisan tangan | Jalan Pasar, Jalan Kafe, Pusat Kota, Kampus, Taman | `Tools/city-art/art/<file>.png` (rasio 8:3) disalin ke Resources pada 50 px/unit | 50 | `ART_LAYOUT` di skrip |
| Gambar kode | Interior (Lobi, Kelas, Ruang Dosen, Toilet, Minimarket 24, GG Learning Center, Gereja, Puskesmas, Masjid, Toko Glow, Bank Syariah, Idmaret, FFC) | digambar skrip | 32 | template `indoor` |

Lukisan tangan menang: kalau `art/<file>.png` ada **dan** `<file>` terdaftar di `ART_LAYOUT`,
gambar kodenya tetap dibuat tapi disimpan sebagai cadangan di `Tools/city-art/generated/`.
Koordinat lantai/penghalang/NPC ditulis dalam petak grid 64×24 lalu dikalikan `scale` map-nya,
jadi resolusi latar bebas selama rasionya 8:3.

**`scale` di `ART_LAYOUT`** mengecilkan jejak map di dunia (default 1 = 64×24 petak = 32×12
unit) tanpa memotong gambarnya: lukisan yang sama menutupi lebih sedikit unit, jadi pintu,
pohon, bangku, dan potnya ikut mengecil dan lebih banyak isi map yang muat di layar. Hasilnya
harus petak bulat dan tetap 8:3 → kelipatan ⅛ (`.5`→32×12, `.625`→40×15, `.75`→48×18,
`.875`→56×21).

**Tiap lukisan punya zoom sendiri**, jadi `scale` diukur per map: bandingkan properti yang sama
di lukisan dengan prop gambar-kode (pintu 1,5×1,75 petak, pot 1×1,2, bangku 2,1×1,06, pohon
2,06, tong sampah 0,75×0,94, lampu 3,06), lalu ambil 1/rasio. Sasaran: ~0,85–0,9× skala lama di
semua map, supaya ukurannya seragam di mata pemain.

| Map | Properti terukur (petak) | Rasio | `scale` | Petak | Unit | Lukisan |
|---|---|---|---|---|---|---|
| Jalan Pasar | pot 1,3, lampu 3,6, sepeda 2,0 | 1,31 | `.625` | 40×15 | 20×7,5 | 1000×375 |
| Jalan Kafe | pintu 1,9×2,4 | 1,32 | `.625` | 40×15 | 20×7,5 | 1000×375 |
| Pusat Kota | pintu 1,8×2,9 | 1,43 | `.625` | 40×15 | 20×7,5 | 1000×375 |
| Kampus | bangku 2,3, tong 1,15×1,75 | 1,45 | `.625` | 40×15 | 20×7,5 | 1000×375 |
| Taman | bangku 1,85, tong 0,8×1,05, lampu 3,45 | 0,98 | `.875` | 56×21 | 28×10,5 | 1400×525 |


### Pintu antar area (`doors`)

Satu data mengurus dua hal sekaligus: penanda di layar **dan** transisi areanya.

Tiap pintu di `generate_city.py` (`doors=` pada MAPS, atau pada `ART_LAYOUT` untuk map
berlukisan) berisi `id` unik, `(x, y)` titik lantai tepat di depan daun pintunya (posisi
trigger), `land` titik mendarat saat pemain masuk **dari sisi seberang**, `h` tinggi yang harus
dilewati penanda (lantai → puncak kusen/serambi), dan `label`. Satuannya petak grid map itu
sendiri, jadi semuanya ikut `scale`.

`LINKS` memasangkan dua pintu jadi satu jalur dua arah; skrip mengisi area tujuan + titik
mendarat tiap pintu dari pasangannya, dan menolak jalan kalau ada pintu yang belum dipasangkan.
Saat runtime `CityWorld.Build()` memasang, untuk tiap pintu: `BoxCollider2D` trigger +
`SceneDoor` (tujuannya sebuah transform kosong di map seberang) dan penanda label + panah bawah
melayang (`AdventureGame.BuildEntranceMarkers()`, `FloatingPrompt`, `PixelSkin.EntranceArrow()`).
Pintunya dipasang di lintasan kedua, setelah semua map berdiri, karena tujuannya ada di map lain.

Titik mendarat sengaja dijauhkan ≥ 0,7 unit dari trigger seberang (trigger setinggi 0,5 unit)
supaya pemain tidak langsung terpental balik. Transisinya lewat `SceneFadeController` (fade
hitam) dan **terkunci selama tutorial** — `Travel(SceneDoor)` menolak kalau `TutorialActive`.

### Pintu kota → interior di scene bab (`sceneDoors`) & area pensiunan (`retired`)

Interior yang digambar tangan di scene bab (Warung Bu Siti) tidak perlu dipindah jadi map kota:
`SCENE_DOORS` di `generate_city.py` memasang pintunya dari fasad toko di map kota, dan
`AdventureGame.BuildSceneDoors()` merakitnya runtime — trigger baru di sisi kota (mendarat di titik
masuk interior yang sudah digambar di scene) plus pintu keluar interior yang diarahkan ulang ke
trotoar depan fasad itu. Warung Bu Siti sekarang dimasuki lewat **etalase Toko Kelontong di Jalan
Pasar** (petak 9,5).

`RETIRED_AREAS` mendaftar area scene yang tidak dipakai lagi. `RetireUnusedAreas()` mematikan latar
& propnya, melucuti pintu yang masih menuju ke sana, memindahkan warga yang bisa diajak bicara ke
depan pintu penggantinya, dan memindahkan pemain yang checkpoint-nya tersimpan di area itu. Saat
ini: **Depan warung** (pin peta & jalur jalan kakinya sudah dicabut). Nama areanya masih ada di
`OriginalCampaign.Areas` karena `Centers`/`Spawns` bab 1 tersimpan di scene — menghapusnya betulan
perlu menjalankan ulang `AlifAdventureBuilder` di Unity.

### Jalur jalan kaki antar area (`roads`)

Arah kiri-kanan di dunia **sama dengan urutan pin di Peta HP**. `ROADS` di `generate_city.py`
menulis pasangan `(barat, timur)` ke `city.json`, dan `AdventureGame.BuildRoadDoors()` memasang
jalurnya saat runtime:

| Barat | Timur |
|---|---|
| Depan stasiun | Jalan Pasar |
| Jalan Pasar | Jalan Kafe |
| Jalan Kafe | Pusat Kota |
| Pusat Kota | Depan warung |
| Taman Cempaka | Kampus Cempaka |

Di sisi **map kota**, `CityWorld.Build()` menyiapkan `WestEdges`/`EastEdges`: strip pemicu
selebar 0,5 unit setinggi lantai utama map, 10 px dari tepi, plus titik mendarat 46 px dari tepi
sejajar `spawnY` map itu (selalu di trotoar). Titik mendarat digeser naik-turun oleh
`RoadLanding()` kalau kena properti — bangku depan masjid di Pusat Kota persis menempel tepi
timurnya. Jarak ke strip tepi map tujuan ±0,88 unit, di atas ambang terpental balik.

Di sisi **area scene bab** (Depan stasiun, Depan warung) geometrinya tidak ada di `city.json`,
jadi jalurnya memakai pintu yang sudah digambar di scene: `SideDoor()` mengambil pintu terluar
di sisi itu dan tujuannya diarahkan ulang. Dulu kedua pintu itu saling menyambung langsung
(Depan stasiun ⇄ Depan warung) sehingga berjalan ke kanan dari stasiun langsung tiba di warung
dan melompati seluruh kota. Area scene tanpa pintu di sisi yang diminta (mis. Halaman kos yang
hanya punya pintu balik) **dan tidak disebut di `ROADS`** dilewati. Kalau `ROADS` meminta sisi yang
tidak punya pintu — sisi barat Depan stasiun, menuju Pusat Kota — `TrySceneEdge()` membuat tepinya dari
lantai `WalkableArea` scene: pita pemicu .5 unit di ujung terluar lantai, mendarat 1,5 unit ke dalam
di tengah lantai terluas.

**Akibatnya untuk Bab 1:** dari Depan stasiun ke Depan warung tiga kali transisi jalan
kaki (Jalan Pasar → Jalan Kafe → Depan warung). `NextDoor()` mem-BFS jalur itu, jadi panah petunjuk
HUD otomatis mengarah ke pintu pertama yang benar; pemain yang ingin cepat tetap bisa memakai
Peta HP.

**Penanda sasaran.** Selama sasaran tugas (papan menu, tokoh, loket…) berada di area pemain,
panah oranye melayang tepat di atasnya (`AdventureGame.MarkObjective`) — termasuk Papan arah di
langkah 2 tutorial. Kalau sasarannya di area lain, klik banner tugas menandai pintu menuju ke sana
selama 6 detik. Posisi panah harus diberikan lewat `FloatingPrompt.SetBasePosition`, bukan
`transform.position` (FloatingPrompt menimpa posisi tiap frame). Panah HUD memakai
`InteractionArrow.png` yang aslinya menunjuk **atas**: Atas 0°, Kiri 90°, Bawah 180°, Kanan −90°.

**Kota = satu rantai barat–timur.** Map jalan tidak punya tepi atas/bawah yang bisa dilewati, jadi
Pusat Kota, Taman, dan Kampus ada di **barat stasiun** supaya bisa dicapai dengan berjalan:
`Kampus ⇄ Taman ⇄ Pusat Kota ⇄ Depan stasiun ⇄ Jalan Pasar ⇄ Jalan Kafe ⇄ Depan warung`.
Peta HP (`PINS` + `draw_phone_map()`) digambar mengikuti urutan yang sama, jadi kiri-kanan di
dunia selalu cocok dengan kiri-kanan di peta. Ujung terjauh (Kampus → Depan warung) enam transisi;
Peta HP tetap jadi jalan pintas.

### Interior gedung (15 ruangan)

Tata letaknya digambar kode (`template='indoor'`, `'cafe'`, `'mosque'`, `'church'`) pada
**skala 1**, jadi propnya sepadan dengan tokoh. Dinding belakang di 3 baris atas, dinding depan
berkaca di 2 baris bawah (tempat pintu keluar), lantai berubin di antaranya; lantai yang bisa
dilewati = baris 3 s.d. tinggi−3.

**Sepuluh ruangan sudah berlukisan tangan** (Kamar Alif, Kafe Senja, Puskesmas, Masjid, Toko Glow,
Minimarket 24, Idmaret, Bank Syariah, GG Learning Center, FFC). Berbeda dengan map jalan, lukisan
interior **hanya mengganti gambarnya** — lantai, penghalang, pintu, spawn, dan posisi NPC tetap
dari gambar kode, karena lukisannya memang dipesan mengikuti tata letak prop yang sama
(promptnya: [`Tools/city-art/PROMPT_INTERIOR.md`](../Tools/city-art/PROMPT_INTERIOR.md)). Jadi
mengganti latar tidak pernah membuat tabrakan meleset, dan gambar kodenya tetap jadi cadangan di
`Tools/city-art/generated/`.

Aturan file: taruh di `Tools/city-art/art/<file>.png` dengan rasio petak map-nya (`lebar_px =
petak × 25` untuk ukuran akhir; gambar boleh lebih besar, skrip yang mengecilkan dengan LANCZOS).
Meleset > 2 % dilaporkan saat generate ("diregangkan agar pas petak map"), > 6 % ditolak.
`art/<file>_Malam.png` opsional: latar malam yang ditukar `CityWorld.Built.SetNight()` pada pukul
19.00–06.00 (`DayLight.IsNight`), mengikuti jam langit berubah ungu — dipakai Kafe Senja
(sore hangat → malam berlampu).

| Area | Petak | Unit | Isi | Pintu |
|---|---|---|---|---|
| Lobi Kampus | 40×15 | 20×7,5 | meja resepsionis, sofa, papan, tanaman | `OUT`→Kampus, `IN`→Kelas/Dosen/Toilet |
| Kelas Ekonomi | 28×13 | 14×6,5 | papan tulis, meja dosen, 8 meja mahasiswa | `OUT`→Lobi |
| Ruang Dosen | 24×13 | 12×6,5 | 3 meja kerja, rak buku, papan | `OUT`→Lobi |
| Toilet Kampus | 20×12 | 10×6 | 2 bilik, 2 wastafel + cermin | `OUT`→Lobi |
| Gereja Kasih Sejati | 30×15 | 15×7,5 | altar + mimbar, 6 bangku jemaat, salib, kaca patri | `OUT`→Jalan Pasar |
| Puskesmas Cempaka | 34×15 | 17×7,5 | 2 bilik periksa bertirai + troli alat, wastafel, meja pendaftaran + meja kerja, lemari obat, 6 kursi tunggu, papan "KLINIK" | `OUT`→Jalan Kafe |
| Minimarket 24 | 30×15 | 15×7,5 | 3 lemari pendingin, 4 rak barang, meja kasir, keset masuk | `OUT`→Jalan Pasar |
| GG Learning Center | 34×16 | 17×8 | layar sambutan, papan rencana, poster mapel, rak buku, meja resepsionis, 4 meja belajar berlaptop | `OUT`→Jalan Pasar |
| FFC | 34×16 | 17×8 | jendela dapur, papan menu, plakat ember, meja pesan 2 kasir, dispenser minuman, stasiun nampan, 4 meja makan, 4 bilik | `OUT`→Pusat Kota |
| Masjid Al-Amanah | 36×15 | 18×7,5 | mihrab + mimbar berkanopi, 6 jendela lengkung, jam & 2 pengeras suara, 2 rak mushaf, 3 saf karpet sajadah, rak sandal + kotak amal | `OUT`→Pusat Kota |
| Toko Glow | 30×15 | 15×7,5 | papan "GLOW" + poster promo, 4 rak dinding, 5 rak pajang, meja rias bercermin lampu, meja kasir, standee, tumpukan keranjang | `OUT`→Jalan Kafe |
| Idmaret | 32×15 | 16×7,5 | 3 lemari pendingin, 4 rak barang, meja kasir, ATM, keset masuk | `OUT`→Pusat Kota |
| Bank Syariah | 34×15 | 17×7,5 | konter teller 3 loket bernomor, meja resepsionis BSI, 4 sofa tunggu + meja bundar, ATM, mesin antrean, layar nomor, kisi mashrabiya, medalion marmer | `OUT`→Pusat Kota |
| Kafe Senja | 34×16 | 17×8 | jendela senja, papan menu kapur, rak biji kopi, papan nama berbola lampu, meja barista (espresso + penggiling + kubah kue), 2 bangku empuk, 4 meja bundar, 3 meja tinggi | `OUT`→Jalan Kafe |
| Kamar Alif | 20×12 | 10×6 | ranjang (`npc:kasur` = titik tidur), lemari, meja, rak buku, karpet, jendela | `OUT`→Jalan Pasar (rumah paling kanan, `pasar.kos`) |

Gereja punya `template='church'` sendiri (`church_room()`): dinding belakang berkaca patri
dengan salib tepat di atas altar, panggung marmer di baris 3–6, lorong berkarpet merah selebar
4 petak di tengah (petak 13–17), dan tiga baris bangku di kiri-kanannya. Penghalang bangku
sengaja setinggi jarak antarbaris (2 petak) supaya baris-barisnya menyatu — tanpa itu ada celah
sempit tempat pemain tersangkut di antara dua bangku. Masuknya dari lukisan **Jalan Pasar**:
pintu `pasar.gereja` berdiri di tangga serambi, dan badan gereja dipecah jadi sayap kiri +
sayap kanan + badan atas supaya ada celah selebar pintu untuk naik ke serambinya.

Minimarket 24 (`template='mart'`, `mart_room()`) dan GG Learning Center (`template='gg'`,
`gg_room()`) juga masuk dari lukisan Jalan Pasar (`pasar.minimarket`, `pasar.gg`). Keduanya
memakai pola yang sama: lorong tengah selebar 4 petak segaris dengan pintu keluar, dua deret
perabot yang penghalangnya setinggi jarak antarbaris, dan koridor samping 3 petak supaya bagian
depan ruangan tetap bisa diputari. Bedanya:

- **Minimarket** — dinding berpita merah-biru, lantai ubin wajik biru, lemari pendingin menempel
  dinding belakang, rak barang di kiri-kanan lorong, kasir di pojok kiri.
- **Idmaret** — gerai kedua, memakai `template='mart'` yang sama. `mart_room()` menerima
  `sign`, `band` (tiga warna pita merek), dan `tile` (tiga warna ubin) dari MAPS, jadi gerai
  berikutnya cukup menambah satu entri MAPS: Idmaret memakai pita biru-merah beraksen kuning,
  ubin krem, pendingin di kiri, kasir + ATM di kanan — tanpa menyentuh kode gambarnya.
- **GG Learning Center** — dinding belakang **4 baris** (satu-satunya; `walk` mulai baris 4)
  supaya logo, layar sambutan, papan `TODAY'S PLAN`, poster mapel, banner SoCal, rak buku, dan
  mural maskot korgi muat berjajar; lantai papan kayu terang, meja resepsionis di depan kelas,
  dan dua kolom meja belajar berlaptop navy. Teks dindingnya pakai `FONT_SIGN` (11 px) — pada
  `FONT_SMALL` (9 px) huruf G/C-nya tidak terbaca.

Etalase GG dipecah jadi tiga penghalang (kiri, kanan, papan nama) supaya ada celah selebar
pintu; minimarket tidak perlu dipecah karena fasadnya berhenti di atas baris `walk`.

Bank Syariah (`template='bank'`, `bank_room()`) masuk dari lukisan **Pusat Kota**
(`pusat.bank`, trigger berdiri di tangga serambi berpilar). Lobinya bergaya BSI: plafon bilah
kayu bergaris cahaya tosca, dinding berkisi mashrabiya yang mengapit papan logo
`BSI BANK SYARIAH`, layar nomor antrean di pojok kanan, lantai marmer dengan medalion bunga di
tengah lorong (hiasan lantai — sengaja tanpa penghalang supaya pemain lewat di atasnya), konter
teller 3 loket di kanan, resepsionis di kiri, dua deret sofa tunggu mengapit lorong, serta ATM
dan mesin antrean di sudut. Penandanya `h=6,6` — berhenti di bawah papan nama, tepat di atas
ornamen emas pintunya.

FFC (`template='ffc'`, `ffc_room()`) adalah gerai ayam goreng di Pusat Kota: dinding belakang
bata merah berlis krem, lantai ubin krem berwajik merah, dan dinding depan merah berpita emas.
Propnya sendiri semua (`p_ffckitchen`, `p_ffcmenu`, `p_ffclogo`, `p_ffccounter`, `p_ffcdrink`,
`p_ffctray`, `p_ffctable`, `p_ffcbooth`, `p_ffcstandee`). Ruangannya dibagi tiga pita: baris 3–4
area kru di belakang meja pesan, baris 5–7 meja pesan (dua prop bersambung, petak 1,75–22,25)
plus ceruk swalayan di kanan, baris 8–13 area makan. **Lorong petak 15–19 sengaja dikosongkan**
supaya jalur pintu → kasir bebas penghalang. Masuknya dari lukisan **Pusat Kota**: pintu
`pusat.ffc` di pintu kaca ganda (petak 33,9); kotak tanaman di fasadnya berhenti di petak 32
sehingga tidak menutupi trigger pintu. Mbak Fira, kasirnya, berdiri di belakang meja pesan —
NPC tanpa quest seperti Bu Ningsih.

Masjid punya `template='mosque'` sendiri (`mosque_room()`): dinding kiblat berjendela lengkung
dengan relung mihrab di tengah (petak 16–20), panggung marmer imam di baris 3–4, tiga saf karpet
sajadah (`sajadah()`, tiap saf 2 petak dengan garis emas di depannya) di baris 5–10, lalu dua
baris serambi dalam berubin tempat rak sandal & kotak amal sebelum dinding depan berpintu ganda.
Mihrab, mimbar, rak mushaf, kotak amal, rak sandal, dan pengeras suara punya prop sendiri
(`p_mihrab`, `p_mimbar`, `p_quranrack`, `p_donation`, `p_shoerack`, `p_speaker`). Masuknya dari
lukisan **Pusat Kota**: pintu `pusat.masjid` di lengkung tengah fasad (petak 52,9). Karena papan
"MASJID AL-AMANAH" menempel tepat di atas lengkung itu, `h`-nya sengaja 3,4 petak — lebih pendek
dari puncak lengkung (8,55) — supaya panah + label berhenti di dalam lengkungnya dan tidak
menutupi papan nama.

Toko Glow memakai `template='indoor'` dengan dinding merah muda + ubin krem dan properti toko
kosmetik sendiri (`p_glowsign`, `p_promo`, `p_wallshelf`, `p_cosmeticshelf`, `p_vanity`,
`p_glowcashier`, `p_standee`, `p_basket` — meja kasirnya sengaja bernama `glowcashier` karena
`cashier` sudah dipakai kasir minimarket). Rak pajang ditata dua baris di kiri + satu di kanan
sehingga lorong dari pintu ke belakang toko (petak 14–18) selalu bebas. Masuknya dari lukisan
**Jalan Kafe**: pintu `kafe.glow` di pintu kaca merah muda (petak 19,8). Fasad Glow berdiri lebih
tinggi dari Puskesmas — ambangnya di petak 12,6 — jadi titik lantainya di 13,2 dan `h`-nya 2,9
petak supaya penandanya berhenti di bawah tenda bergaris.

Kafe Senja (`template='cafe'`, `cafe_room()`) memakai palet hangat fasadnya: dinding plester
krem berlambrisir kayu, lantai **ubin kunci** krem bermotif bunga (`cafe_floor()`) yang sengaja
terang supaya perabot kayu gelapnya terbaca, dan dinding depan kayu berkaca hangat. Cirinya
`Canvas.wash()` — tiga lapis warna jingga tembus pandang di empat baris lantai terdepan, jadi
cahaya senja dari jendela terlihat menumpah ke dalam. Propnya sendiri semua (`p_cafewindow`,
`p_cafeboard`, `p_cafeshelf`, `p_cafesign`, `p_cafebar`, `p_cafetable`, `p_cafesofa`,
`p_cafestool`, `p_cafeplant`). Meja barista di kiri, bangku empuk di kanan, dan **lorong petak
14–20 dikosongkan** supaya jalur pintu → barista bebas. Masuknya dari lukisan **Jalan Kafe**:
pintu `kafe.senja` di pintu kayu (petak 7,95); penghalang papan menunya dikoreksi ke kaki papan
yang sebenarnya (petak 5,55) karena yang lama menindih trigger pintu ini. Mas Bayu, pemiliknya,
berdiri di belakang meja barista — NPC tanpa quest.

Puskesmas memakai `template='indoor'` biasa dengan dinding krem + ubin putih dan properti klinik
sendiri (`p_curtain`, `p_clinicbed`, `p_trolley`, `p_medcabinet`, `p_chairrow`, `p_poster`,
`p_medsign`, `p_clock`). Masuknya dari lukisan **Jalan Kafe**: pintu `kafe.puskesmas` di pintu
kaca ganda (petak 33,3), dan deretan tanaman di depan fasadnya dipecah jadi dua penghalang
supaya ada celah selebar pintu (petak 31,9–34,9). Bu Ningsih, perawat jaga, berdiri di belakang
meja pendaftaran — NPC tanpa quest, hanya baris santai + adegan perkenalan.

**Jadwal harian warga (`Model/NpcSchedule.cs`).** Sehari dibagi empat waktu — **pagi 06–12, siang
12–18, malam 18–24, larut malam 00–06** (tampil di HUD di samping jam). Kehadiran tiap tokoh kota
diacak per *hari cerita × waktu* dengan peluang 75 / 80 / 55 / 20 %, tetapi **deterministik**
(hash FNV dari id tokoh + hari + waktu): hasilnya sama setelah save/reload dan tidak disimpan.
Jaminan: (1) tiap tokoh pasti hadir di pagi **atau** siang setiap hari; (2) **tidak ada map yang
kosong** — kalau undian mengosongkan map, satu tokoh dipaksa berjaga, bergilir (`PresentIn`);
(3) tokoh yang sedang punya langkah quest/kasus untuk pemain **tidak pernah dihilangkan**
(`AdventureGame.ScheduledNpcs`), jadi pemain tidak pernah dibuat menunggu dan panah penunjuk
selalu punya sasaran. Benda (papan, ranjang) tidak ikut jadwal. Pergantian waktu mengacak ulang
kehadiran (`ClockTicked`, ditunda selama dialog/papan terbuka) dan memunculkan toast. Akibat
jaminan (2): map yang penghuninya cuma satu praktis selalu berpenghuni orang yang sama — acaknya
baru terasa di map dengan ≥ 2 tokoh.

**Siang & malam (`Model/DayLight.cs` + `AdventureGame.Cases.cs › LateUpdate`).** Warna **Global Light
2D** tiap scene mengikuti jam: fajar hangat (06) → putih netral (08–16) → sore keemasan (17.30) →
senja ungu (19) → malam biru redup (21–04.30). Karena semua sprite memakai material Lit, seluruh
dunia (latar, Alif, warga) ikut menggelap; teks dunia dan UI tidak terpengaruh. Kecerahan malam
sengaja tidak pernah di bawah ±45 % supaya map tetap terbaca. **Interior** (`OutdoorAreas` =
daftar area luar; sisanya dianggap ruangan) diterangi lampu: makin gelap langit, makin dominan
warna lampu hangat (sampai 78 %), jadi malam di dalam ruangan hanya sedikit lebih redup (±80 %). Warna bergeser halus per frame, sehingga masuk gedung di malam hari
atau bangun tidur tidak berkedip. Mengubah suasana = ubah tabel `Keys` di `DayLight`.

**Kecepatan jam.** 1 detik nyata = **3 menit in-game** (`TimeSystem._gameMinutesPerRealSecond`,
nilainya tersimpan juga di tiap scene). Satu jam in-game = 20 detik nyata, dan satu hari main
(06:00 → tengah malam) = 6 menit nyata, jadi pagi → siang → malam sempat berganti dalam satu
sesi. Mengubah kecepatan = ubah default di `TimeSystem.cs` **dan** nilai di scene-nya.

**Jam & hari.** Jam tidak lagi berhenti di 23:59: lewat tengah malam masuk **larut malam** di hari
cerita yang sama. Hari berganti lewat tidur, atau — kalau pemain begadang — otomatis saat fajar
06:00 (`TimeSystem.OnStoryDawn` → `AdventureState.PassNight()`): kasus & jadwal tetap bergulir,
tetapi energi tidak pulih dan kabar pagi hanya muncul sebagai toast.

**Aturan: setiap map kota punya minimal satu tokoh sejak Hari 1** (dijaga tes
`EveryCityMapHasAtLeastOneResidentFromDayOne`). Interior yang tidak punya tokoh quest diisi
**penghuni tetap** tanpa quest di `SideQuestContent.Npcs` — satu baris santai bertema kebiasaan uang
+ adegan perkenalan berlatar interiornya (`kota:<file>`): Mbak Wulan (Lobi), Reza (Kelas), Bu Hana
(Ruang Dosen), Pak Karjo (Toilet), Pak Satrio (Bank Syariah), Mas Doni (Minimarket 24), Kak Vina
(GG), Pak Markus (Gereja), Kak Sasa (Glow), Bu Tini si ibu kos (Kamar Alif), selain Bu Ningsih,
Mbak Fira, Mas Bayu, Mbak Rini, dan Pak Mahfud yang sudah ada. Map baru = tokoh baru + posisinya
di `npcs=[…]` generator.

**Skala interior = skala jalan.** `INTERIOR_SCALE = .625` di `generate_city.py` mengecilkan jejak
dunia tiap interior, jadi 1 petak = 0,3125 unit di seluruh permainan. Tanpa itu interior 1,6x lebih
"gemuk" daripada jalan: pintu 1,5 unit di samping Alif yang tingginya 0,8 unit, dan perabot di
lukisan ikut terlihat raksasa. Gambarnya **tidak ikut mengecil** — `art_import_ppu()` menaikkan PPU
impornya sebesar 1/scale (lukisan 50 → 80, gambar kode 32 → 51,2), jadi resolusinya tetap penuh
sementara jejak dunianya menyusut. `MIN_LANDING` (0,8 unit) menjaga titik mendarat tiap pintu tetap
di luar trigger seberang walau ruangannya mengecil; `door_landing()` menggesernya otomatis.

**Jarak kamera sama di mana pun** (`orthographicSize` 3,2, +0,4 saat sprint), termasuk di dalam
ruangan. Ruangan yang lebih kecil dari layar dibiarkan **dibingkai gelap**, seperti interior stasiun
yang digambar tangan — `CameraBounds.ClampAxis` menaruh kamera di tengah areanya. Versi yang
merapatkan kamera ke ukuran ruangan sempat dicoba dan dibatalkan: lantai jadi terasa sempit dan
perpindahan zoom di pintu mengganggu. Ruangan yang lukisannya sedikit (Kamar Alif & Toilet Kampus,
6,25 × 3,75 unit) memang tampak kecil di layar; memperbesarnya = menaikkan `scale` map itu (dengan
konsekuensi perabotnya ikut membesar terhadap tokoh) atau menggambar ulang ruangannya lebih luas.

Interior **tidak dipasang pin di peta HP**: masuknya lewat pintu. `Travel(int area)` tetap bisa
menuju ke sana karena `NextDoor` mem-BFS jalur pintunya.

| Map | Isi |
|---|---|
| Jalan Pasar | Toko Kelontong, Minimarket 24 + Gereja Kasih Sejati + GG Learning Center (ketiganya bisa dimasuki), halte |
| Jalan Kafe | Kafe Senja + Toko Glow (kosmetik) + Puskesmas Cempaka (ketiganya bisa dimasuki), Gerai HP, Kedai Kopi, Toko Buku |
| Pusat Kota | Bank Syariah + FFC + Masjid Al-Amanah (ketiganya bisa dimasuki), Idmaret + ATM |
| Kampus Cempaka | Fakultas Ekonomi & Bisnis, Seni Rupa & Desain, Fotokopi, plaza berumput |
| Taman Cempaka | Gazebo, kolam, jalan setapak, bangku |

### Bab 1 — Cempaka, Minggu Pertama · *01 / Belajar bertanya*
**Intro:** Alif baru tiba di Cempaka. Hari pertama ia makan di Warung Bu Siti (tutorial + kasus
struk), mendapat kamar kos di Jalan Pasar, lalu selama seminggu membantu **satu persoalan warga
di tiap map kota**. Benang merahnya: sesuatu yang tidak pernah dibuat jelas dan dicatat.

**Tulang punggung (linear, `AdventureContent.One()`):**

| # | Id | Tugas | Target | Area | Jenis |
|---|---|---|---|---|---|
| 1 | `c1.arrival` | Baca petunjuk | Papan arah | Dalam stasiun | talk |
| 2 | `c1.menu` | Susun pesanan (≤ Rp20.000) | Papan menu | Dalam Warung | budget |
| 3 | `c1.receipt` | Cocokkan struk (teh dobel, kerupuk tak dipesan) | Meja jendela | Dalam Warung | match |
| 4 | `c1.resolve` | Selesaikan pembayaran Rp18.000 | Bu Siti | Dalam Warung | sort |
| 5 | `c1.room` | Tanyakan tempat menginap → dapat **Kamar Alif** | Bu Siti | Dalam Warung | talk |
| 6 | `c1.finale` | Kabari Bu Siti — **terkunci sampai 6 kasus selesai** (`NeedsCases`) | Bu Siti | Dalam Warung | talk |

**Hari cerita.** `AdventureState.Day` (mulai 1, tersimpan, terbawa antar-bab) maju **hanya lewat
tidur** di ranjang Kamar Alif (`npc:kasur` → `AdventureGame.Cases.cs`: konfirmasi → layar hitam
"Hari ke-N" → energi pulih → kabar pagi). Tidur baru bisa setelah `c1.room`
(`AdventureChapter.SleepAfter`). Tanpa tidur, hari tetap berganti saat fajar 06:00 (lihat *Jam & hari*). HUD menampilkan `BAB 1 • HARI N • area`.

**Kasus Warga (`CaseContent.cs`, wajib, satu per hari mulai Hari 2).** Kasus terbuka pada
harinya dan **tidak hangus** — bisa menumpuk. Tokoh pendatang baru muncul di harinya
(`QuestNpc.AppearsOnDay`); warga tetap sudah ada sejak Hari 1 dengan baris santai, dan sebagian
berganti baris setelah kasusnya selesai (`AfterQuest`/`IdleAfter`). Mesin langkahnya sama dengan
side quest (talk / inspect / sort / flow / match / give).

| Hari | Map | Kasus (`id`) | Tokoh | Isu → konsep syariah 📖 | Barang |
|---|---|---|---|---|---|
| 2 | Jalan Pasar | Harga yang Disembunyikan (`case.harga`) | Mbak Mira*, Pak Yanto, *Lapak* | harga disebut setelah ditimbang → **ghabn**, jual beli saling rela | Daftar Harga Pasaran |
| 3 | Taman (+ Masjid) | Tanah yang Diwakafkan (`case.wakaf`) | Pak Gunawan*, Bu Salamah, Pak Mahfud (nazhir), *Papan taman* | ahli waris mengklaim tanah wakaf → **wakaf tidak dijual/dihibahkan/diwariskan**, nazhir, Akta Ikrar Wakaf; akar masalah: sertifikat wakaf tak pernah diurus | Salinan Akta Ikrar Wakaf |
| 4 | Pusat Kota (+ Idmaret) | Kembalian dan Kotak Amal (`case.kembalian`) | Pak Joko*, Mbak Rini | kembalian diganti permen, donasi tercentang otomatis → harta halal hanya dengan **ridha** (QS An-Nisa: 29), sedekah sukarela | Struk Koreksi Idmaret |
| 5 | Kampus | Kas yang Tidak Dilaporkan (`case.kas`) | Ilham*, Salsa, *Mading* | kas himpunan tak dilaporkan + tuduhan tanpa nama → **amanah**, pencatatan, larangan menuduh tanpa bukti | Laporan Kas Himpunan |
| 6 | Jalan Kafe (+ Kafe Senja) | Upah yang Ditunda (`case.upah`) | Gilang*, Mas Bayu | upah 3 minggu tertunda karena kas kafe bercampur uang pribadi → **ijarah**: upah jelas di awal, dibayar tepat waktu | Surat Perjanjian Kerja |
| 7 | Jalan Pasar | Buku Bon Warung (`case.bon`) | Bu Wati*, Bima | bon tanpa tanggal/paraf → **qardh**, mencatat utang (QS Al-Baqarah: 282), memberi tangguh (280), denda telat = riba → umpan Bab 2 | Buku Bon Baru |

\* = hadir mulai hari kasusnya. *Miring* = benda (`QuestNpc.IsObject`): titik interaksi tanpa
sprite di atas benda yang sudah tergambar di lukisan; tanpa adegan perkenalan dan balon chat.
Semua tokoh memakai sprite placeholder yang dibedakan `Tint`. Gang Permukiman sudah tidak ada,
jadi kasus bon Bima pindah ke Jalan Pasar (Bima kini berdiri di sana). 📖 = klaim syariah yang
perlu ditinjau narasumber sebelum rilis (sama seperti `QUEST_DESIGN.md`).

**Selama `c1.finale` menunggu kasus:** HUD menampilkan `HARI N • KASUS WARGA — <langkah>` atau
"Belum ada kabar baru • pulang ke kamar kos dan tidur"; panah penunjuk mengarah ke tokoh langkah
kasus (atau pintu menuju map-nya, atau ranjang). Bicara ke Bu Siti sebelum waktunya hanya
memberi petunjuk. Jurnal menandai kasus sebagai `KASUS WARGA` dan tidak membocorkan kasus hari
mendatang.

**Pelajaran:** anggarkan sebelum memesan, cocokkan struk, koreksi tanpa menuduh niat — lalu hal
yang sama dalam enam bentuk lain. **Ending:** papan harga terpasang, taman wakaf terjaga,
kembalian utuh, kas terlapor, upah terbayar, buku bon berparaf; Bu Siti menitipkan kabar Dimas.

**Tutorial (hanya Bab 1, sebelum tugas):** 1/3 bergerak satu langkah → 2/3 baca Papan arah
(E) → 3/3 buka lalu tutup Jurnal. Bisa dilewati ("Lewati tutorial").

**Cutscene pembuka:** prolog teks → Alif tiba di peron Solo → di kereta → turun di peron →
"Akhirnya sampai juga…" → "Perut udah keroncongan… ayam geprek di warung Bu Siti".
**Cutscene penutup:** Bu Siti berjanji mencatat pemasukan & akad dengan jelas; Bab 2 terbuka.

### Bab 2 — Jebakan Riba · *02 / Ruang untuk berpikir*
**Intro:** Dimas menatap penawaran pinjaman di telepon; biaya kuliah datang bersamaan dengan
kebutuhan harian. Alif membantu membaca angka, bukan memutuskan hidup Dimas.

| Id | Tugas | Target | Jenis |
|---|---|---|---|
| `c2.arrival` | Cari kamar Dimas | Papan arah | talk |
| `c2.listen` | Dengarkan kebutuhan | Dimas | talk |
| `c2.expenses` | Kelompokkan kebutuhan (mendesak / tunda / belum boleh dihitung) | Buku anggaran | sort |
| `c2.ask` | Tanyakan syarat | Ustadz Farid | talk |
| `c2.terms` | Uraikan penawaran pinjaman | Telepon Dimas | match |
| `c2.support` | Periksa bantuan | Papan bantuan | talk |
| `c2.plan` | Susun rencana (Rp400.000, bantuan belum disetujui tak dihitung) | Buku anggaran | budget |
| `c2.encounter` | Hadapi godaan — boss **Dana Kilat** (HP 144) | — | encounter |
| `c2.send` | Sampaikan rencana | Dimas | talk |

**Ending:** Dimas menutup penawaran yang membebani, kebutuhan pokok terlindungi, pembicaraan
dengan keluarga dimulai.

### Bab 3 — Ilusi Maysir · *03 / Peluang bukan rencana*
**Intro:** Raka menemukan promosi tiket berhadiah; poster memperlihatkan satu pemenang.
Alif mengajak melihat yang tidak ditulis besar.

| Id | Tugas | Target | Jenis |
|---|---|---|---|
| `c3.arrival` | Temui Raka | Raka | talk |
| `c3.rules` | Buka syarat (tiket kalah hangus, hadiah dari setoran peserta) | Poster promosi | inspect |
| `c3.records` | Bandingkan hasil | Catatan peserta | match |
| `c3.listen` | Bicarakan alternatif | Raka | talk |
| `c3.plan` | Rancang sore warga (≤ Rp100.000, tanpa "hadiah promosi") | Meja kegiatan | budget |
| `c3.prepare` | Siapkan kegiatan | Rak buku | talk |
| `c3.encounter` | Hadapi godaan — boss **Sang Pengundi** (HP 180) | — | encounter |
| `c3.close` | Tutup kegiatan | Raka | talk |

**Ending:** Raka menyimpan uang kebutuhannya; warga menikmati sore tukar buku berbiaya jelas.

### Bab 4 — Sindikat Tadlis · *04 / Di balik label*
**Intro:** pembeli mengembalikan radio "baru" yang ternyata retak di bawah stiker. Naya dan
Alif mengumpulkan kondisi barang, label, dan catatan transaksi sebelum menyimpulkan.

| Id | Tugas | Target | Jenis |
|---|---|---|---|
| `c4.arrival` | Dengarkan keluhan | Naya | talk |
| `c4.inspect` | Periksa radio (stiker, kabel, label S-014) | Radio contoh | inspect |
| `c4.records` | Cocokkan dokumen | Dokumen barang | match |
| `c4.hear` | Minta klarifikasi | Penjual | talk |
| `c4.packet` | Susun map pengaduan (tanpa data pribadi, tanpa cerita palsu) | Map pengaduan | sort |
| `c4.encounter` | Hadapi godaan — boss **Saudagar Topeng** (HP 216) | — | encounter |
| `c4.label` | Perbaiki label | Dokumen barang | talk |
| `c4.close` | Catat tindak lanjut | Naya | talk |

**Ending:** penjual memperbaiki label, keluhan ditindaklanjuti, data pribadi dijaga.

### Bab 5 — The Grand Ponzi · *05 / Pertanyaan yang tepat*
**Intro:** Dimas membawa undangan investasi berhasil tinggi & bonus mengajak teman. Kali ini
semua memeriksa bukti bersama sebelum siapa pun mengirim uang.

| Id | Tugas | Target | Jenis |
|---|---|---|---|
| `c5.arrival` | Baca undangan | Dimas | talk |
| `c5.claims` | Bandingkan janji | Brosur penawaran | match |
| `c5.flow` | Hubungkan arus uang (setoran baru membayar peserta lama) | Papan arus dana | flow |
| `c5.support` | Dukung teman | Raka | talk |
| `c5.verify` | Susun berkas simulasi (cocokkan di kanal resmi) | Meja verifikasi | sort |
| `c5.encounter` | Hadapi godaan — boss **Menara Janji** (HP 252) | — | encounter |
| `c5.decline` | Nyatakan keputusan (tidak transfer) | Dimas | talk |
| `c5.epilogue` | Berkumpul kembali | Buku perjalanan | talk |

**Ending (epilog):** tidak ada uang dikirim; bukti disimpan; Alif menutup buku perjalanan.

**Tautan referensi di akhir bab** (`AdventureContent`): MUI (uang elektronik, kredit) dan OJK
(waspada investasi).

**Cerita warga opsional:** tiap bab punya 3 kutipan warga (`Optional`) yang terkumpul di Jurnal
sebagai "CERITA WARGA".

### Cerita Sampingan (side quest) — lintas bab, terbuka setelah tutorial
Konten: `Scripts/Adventure/Model/SideQuestContent.cs` · aturan: `SideQuest.cs` · runtime:
`Adventure/AdventureGame.SideQuests.cs` · desain & sumber interview: [`QUEST_DESIGN.md`](QUEST_DESIGN.md).

| Id | Judul | Tema | Tokoh | Langkah |
|---|---|---|---|---|
| `kpop.skincare` | Empat Set demi Photocard | Israf & tabdzir | Kirana, Tara | talk → inspect → give *Set Skincare Cadangan* → talk |
| `kpop.pockets` | Kantong-Kantong Tabungan | Tabungan syariah | Kirana, Petugas bank | talk → talk (dapat *Brosur*) → give → budget |
| `kpop.checkout` | Checkout Lightstick | Riba, paylater & calo | Tara, Kirana | talk → match (dapat *Struk*) → give |
| `kpop.fansign` | Tiket Undian Fansign | Maysir vs promosi | Tara, Kirana | talk → sort (dapat *Photocard Duplikat*) → give |
| `dosen.loan` | Pinjaman Sahabat | Qardh & mencatat utang | Pak Fadli, Bima | talk → inspect → talk → sort (dapat *Surat Kesepakatan*) → give |
| `dosen.phishing` | Tautan Undangan | Phishing & menjaga harta | Bima, Petugas bank | talk → inspect → flow SOP (dapat *Kronologi*) → give → talk |

| `invest.*` (10) | Investasi syariah satu per satu | lihat tabel di bawah | 10 NPC baru | talk (kenalan) → mini game → talk (buka **kartu investasi**) |

Rangkaian K-pop dan rangkaian dosen berjalan paralel; di dalam rangkaian, quest berikutnya
terbuka setelah yang sebelumnya selesai. Progres, isi Tas, dan hubungan terbawa antar-bab.

### Rangkaian Investasi Syariah (satu NPC, satu jenis, hadir satu per satu)
NPC investasi baru **muncul di dunia** setelah quest NPC sebelumnya selesai
(`QuestNpc.AppearsWithQuest`). Hadiahnya **kartu investasi** (`StoryContent.Cards`) di HP ›
**Investasi**: contoh, cara hasil, risiko, dan pelajaran. Kolom "cocok untuk game" dari tabel
rancangan hanya dipakai untuk urutan, tidak ditampilkan.

| # | NPC (placeholder) | Lokasi | Kartu | Mini game |
|---|---|---|---|---|
| 1 | Mas Arga, barista-investor (raka) | Jalan Kafe | Saham Syariah | sort: lolos seleksi saham syariah? |
| 2 | Bu Ratna, pensiunan guru (bu_siti) | Pusat Kota | Sukuk | match: SR vs ST |
| 3 | Dewi, klub investasi kampus (naya) | Kampus | Reksa Dana Syariah | match: tujuan ↔ jenis |
| 4 | Pak Harun, pedagang emas (pak_ustad) | Jalan Pasar | Emas Syariah | sort: sesuai syariah / waspada |
| 5 | Kak Nadia, kreator konten (naya) | Taman | ETF Syariah | match: ETF / reksa dana / saham |
| 6 | Bu Laras, UMKM keripik (bu_siti) | Jalan Pasar | P2P / Crowdfunding | flow: langkah pendanaan |
| 7 | Pak Darto, pemilik kontrakan (pak_ustad) | Pusat Kota | Properti Syariah | match: akad KPR |
| 8 | Mbak Sinta, teller (naya) | Pusat Kota | Deposito Syariah | match: mudharabah vs bunga |
| 9 | Om Hendra, pengelola properti (raka) | Jalan Pasar | DIRE Syariah | flow: alur DIRE |
| 10 | Pak Yusuf, dosen pasar modal (dimas) | Kampus | EBA Syariah | flow: alur EBA |

### Aplikasi Investasi (HP) — Alif bisa berinvestasi
Alur layar (`AdventureGame.Story.cs`): **daftar instrumen** → **Kenali dulu** (penjelasan kartu:
contoh, cara hasil, risiko, pelajaran) → **Penawaran berjalan** (mis. *Sukuk Ijarah Gedung Seni
Rupa & Desain* di Kampus Cempaka, dengan batang dana terkumpul) → **Detail** + pemilih unit →
**Konfirmasi** → **Portofolio**. Data & aturan ada di `Model/Investment.cs`:
`InvestmentContent.Listings` (harga unit, `RateBp` = ujrah per periode dalam basis poin,
`PeriodDays`, `Periods`) dan `InvestmentRules` (`Buy`, `Settle`, `Valid`).
- Dibayar dari **dompet dulu** (`State.Money`, uang yang terlihat di HUD), sisanya dari rekening
  (`State.Bank`); dana terkunci sampai jatuh tempo. Ujrah & pokok kembali ke dompet (kelebihan
  batas dompet masuk rekening). Pemain harus merasakan uangnya berkurang — kehabisan uang tunai
  adalah risiko nyatanya, dan di situlah Pinjol menggoda.
- `InvestmentRules.Settle` dipanggil tiap hari cerita berganti (tidur maupun begadang): ujrah periode
  yang lewat masuk dompet, pokok kembali saat jatuh tempo. Tersimpan di `AdventureState.Holdings`;
  tidak dibawa lintas bab karena uang pun disetel ulang tiap bab.
- Instrumen terbuka bila kartunya sudah didapat dari quest **atau** punya penawaran (sukuk terbuka
  dari awal). Menambah penawaran = menambah entri di `Listings` dengan `Card` = id kartunya.
- **Bahasa & angka syariah:** hasil sukuk ijarah adalah *ujrah* (sewa) tetap sesuai akad — jangan
  ditulis "bunga" atau "jaminan untung". `RateBp` dijaga ≤ 300 oleh tes: janji hasil tinggi adalah
  tanda bahaya yang diajarkan Bab 5.

**Dampak investasi.** Membeli menarik neraca skor ke jalan tengah (`ScoreSystem.MoveTowardsBalance`),
mencatat jurnal, dan layar konfirmasi memperingatkan bila sisa uang hanya cukup untuk sewa < 5 malam.
Tiap penawaran punya `Impact` (apa yang dibangun, tampil di detail) dan `MaturityNote` (catatan
jurnal saat jatuh tempo). Karena dana terkunci, berinvestasi berlebihan bisa membuat Alif kehabisan
uang tunai — di situlah aplikasi Pinjol menggoda.

### Skenario koin "Bang Jago" + Pasar Koin — pelajaran FOMO & pump-and-dump
`Model/CoinMarket.cs` + `AdventureGame.Coin.cs`. **Bang Jago** berdiri di *Depan stasiun* (dekat titik
spawn area; sprite placeholder Dimas berwarna emas) dan mengajak Alif membeli koin **MOON**
(MoonCempaka) — Rp1.000.000 **tunai**, jadi Alif harus menarik uang di ATM dalam stasiun dulu.
"Tidak, terima kasih" adalah tombol terfokus. Jalan ceritanya (harga deterministik per hari):
- **Ikut:** besoknya −80% (tinggal seperlima), lusa nyaris nol, Bang Jago lenyap. Koin bisa dijual
  kapan saja lewat HP selama harganya > 0. ("Turun 500%" tidak ada — harga paling jauh turun 100%.)
- **Menolak:** besoknya **+400%** → adegan pagi + **tawaran kedua** (dompet dulu, lalu rekening; hanya
  berlaku hari itu, juga bisa lewat Bang Jago). Lusa koin jadi **0** (pengembang kabur): yang tergoda
  kehilangan Rp1.000.000, yang tetap menolak selamat.
Adegan pagi: `StoryContent.CoinMorning` (`crash`, `dust`, `pump`, `rug.lost`, `rug.safe`), diputar
sekali setelah tidur. Jurnal mencatat tiap keputusan; ikut = neraca −8 dari Kepatuhan Syariah,
menolak = ke tengah; penutup demo menyebut nasib koinnya. Tersimpan di `AdventureState.Coin*`.
**Pasar Koin** (HP › Investasi › "Pasar Koin"): daftar 8 koin fiktif dengan harga & perubahan harian,
**kolom pencarian** (simbol/nama, Enter atau "Cari"), dan detail dengan grafik 5 hari + peringatan
gharar. Koin lain hanya bisa dipantau — tidak bisa dibeli.

### Aplikasi Pinjol "DanaKilat" (HP) — godaan, bukan jalan keluar
`Model/Pinjol.cs` (`PinjolRules`) + `AdventureGame.Pinjol.cs`. Nominal Rp100/300/500 ribu, **admin
10% dipotong di depan**, **bunga 5% per hari berbunga**, jatuh tempo 5 hari, lalu denda Rp10.000/hari.
Satu pinjaman aktif; dicicil/dilunasi dari dompet dulu lalu rekening. Layar "Hitung dulu"
memperlihatkan utang 3/7/14 hari ke depan + jalan lain yang lebih bijak sebelum tombol pinjam, dan
"Tidak jadi" adalah tombol terfokus. Meminjam = neraca −10 dari Kepatuhan Syariah, tiap bunga −2
(−4 bila lewat tempo), lunas = kembali ke tengah. Jurnal mencatat pinjaman (riba) dan, saat lunas,
ongkos sebenarnya. Bunga dihitung tiap hari berganti (`PinjolRules.Accrue`), tersimpan di
`AdventureState.Debt*`, tidak dibawa lintas bab, dan penutup demo menyebut utang yang masih berjalan.

### Adegan cerita (cutscene di dalam game)
`StoryContent` + `AdventureGame.Story.cs` + `StoryOverlay.cs`: tampilan **sama dengan
Chapter1Cutscene** — ilustrasi layar penuh, tokoh besar di tengah adegan, kotak dialog krem
selebar layar dengan tab nama, efek ketik, **LANJUT** di dalam kotak, **LEWATI >>** kanan atas
(klik/Space/Enter = lanjut, Esc = lewati). Tidak memakai kotak dialog gameplay. Diputar **sekali**
saat pertama bertemu NPC side quest dan tokoh main quest (Raka, Dimas, Ustadz Farid, Naya,
Penjual), dan **setiap** bab dimulai. Gambar dirujuk lewat `Assets/Resources/Story/StoryArt.asset`:
satu slot per area kota (`cerita_pasar`, `cerita_kafe`, `cerita_pusat`, `cerita_kampus`,
`cerita_taman`) yang sementara memakai latar painted terdekat, plus latar/panel
bab. Kunci berawalan `kota:` (mis. `kota:J3P_Puskesmas` untuk perkenalan Bu Ningsih) dibaca
langsung dari `Resources/Kota` tanpa entri di StoryArt. Ganti `Art` entri saat ilustrasi final siap. Adegan yang sudah
dilihat tersimpan di save (`SeenStories`).

**Adegan pembuka vs sesudah tugas.** `StoryContent.TaskScene(id)` diputar sebelum tugas,
`StoryContent.TaskOutro(id)` sesudah tugasnya selesai (sebelum kalimat penutup). `c1.meal`
memakai keduanya: Bu Siti mengantar pesanan → Alif makan → baru Raka menerobos masuk. Tugas yang
`Introduction`-nya kosong melewati dialog pembuka — adegan TaskScene-nya yang membawakan.

**Papan penilaian bebas & akhir bercabang.** `ActivityBoard.Free` (dipakai `c1.offer`, tawaran
pinjaman Raka): setiap kartu boleh dinilai apa saja dan bisa diubah; tidak ada jawaban yang
ditolak. UI-nya satu syarat per layar dengan pilihan besar, lalu ringkasan sebelum "Sampaikan
penilaian" (`AdventureGame.ShowJudgement`). Ketepatan penilaian (`ActivityBoard.Tier`: 2 tepat
semua, 1 ≥ separuh, 0 < separuh) memilih penutup tugas (`AdventureTask.Outcomes`), adegan pagi
berikutnya (`StoryContent.OfferAftermath`: Bu Siti menolak / menggantung / menandatangani), dan
satu baris penutup demo (`StoryContent.DemoOutroFor`). Tier dibaca dari `Placements` yang sudah
tersimpan (`AdventureState.BoardTier`) — tidak ada kolom save baru. Hanya pelajaran dari kartu
yang dinilai tepat yang masuk jurnal. Sisa Bab 1 (mis. `c1.finale`) belum ikut bercabang.

**Dokumen papan.** `ActivityBoard.Document` (`BoardDocument`) membuat papan dibuka lewat tampilan
dokumen dulu: kertas dokumen di kiri, pembandingnya di kanan, tombol "Periksa …" ke kartunya, dan
tombol "Lihat …" di papan untuk membukanya lagi. Dipakai `c1.receipt` (struk Rp28.000 vs pesanan
Rp18.000); datanya di `OriginalCampaign.Apply`.

Bu Siti sengaja **tidak** punya adegan "pertama bertemu": ia menyambut di pintu lewat AreaIntro
"Dalam Warung Bu Siti", jadi menyentuh papan menu langsung membuka tugas `c1.menu`.

**Tag panggung per baris** (`StoryLine.Parse`): sebuah baris boleh diawali
`[art=<kunci> tokoh=<nama>]` untuk mengganti gambar dan/atau tokoh mulai baris itu sampai diganti
lagi; `tokoh=-` menyembunyikan tokoh (dipakai untuk close-up makanan/suasana dan ilustrasi penuh
yang tokohnya sudah tergambar). Contoh: masuk Warung Bu Siti dibuka `[art=warung_dapur tokoh=-]`
(aroma dari dapur), lalu `[art=warung_dalam tokoh=bu_siti]` saat Bu Siti menyapa; `task:c1.meal`
memakai ilustrasi `warung_antar`. Gambar khusus cutscene disimpan di
`Assets/Sprites/Cutscenes/Warung/` — **jangan** menimpa `Backgrounds/WarungBuSiti_Interior.png`,
itu map gameplay. Tes `EverySceneArtKeyAndFigureTagResolves` menjaga kunci & nama tokoh tidak salah ketik.

---

## 5. Aksi Pemain & Kontrol

### Menjelajah (Activity `World`)
| Aksi | Keyboard / Mouse | Sentuh |
|---|---|---|
| Bergerak (8 arah) | WASD / panah | Joystick (tetap / mengambang / tersembunyi) |
| Lari | Shift + arah | — |
| Interaksi objek terdekat | **E** / klik objeknya | Tombol bulat **E** kanan-bawah |
| Buka Tas | **I** / ikon tas | Ikon tas |
| Buka Jurnal | **J** / ikon buku | Ikon buku |
| Jeda / tutup kartu | **Esc** / ikon menu | Ikon menu |
| Pindah fokus UI | Tab / Shift+Tab | — |

- **Prompt interaksi:** keycap "E" krem melayang di atas kepala Alif saat ada target dalam
  jangkauan (`InteractionPromptFX`); nama target tampil di teks bawah layar ("E • Papan arah").
- **Petunjuk arah tutorial/tugas:** panah + teks "Kiri • Papan arah" di bawah panel tugas.
- **Adegan masuk Main Baru (Bab 1):** Alif muncul di sisi kanan ruang tunggu lalu berjalan
  sendiri ke tengah (`PlayerController.AutoWalkTo`); input gerak pemain membatalkannya.

### Dialog
Kotak krem berbingkai oranye + tab nama pembicara, portrait di kanan, efek mesin ketik.
Lanjut: klik tombol **Lanjut**, klik di luar kotak, Space/Enter. Pilihan jawaban = tombol oranye.

### Puzzle / Aktivitas (Activity `Puzzle`)
| Jenis | Cara main |
|---|---|
| `talk` | Dialog dengan target; selesai setelah dialog berakhir. |
| `budget` | Klik item untuk masuk/keluar anggaran; lengkapi 3 kebutuhan (grup) tanpa melewati batas. |
| `match` / `sort` | Pilih kartu di kiri, lalu klik tujuan di kanan; kartu benar terkunci. |
| `flow` | Seperti sort: hubungkan sumber dana ke penerimanya. |
| `inspect` | Klik "Periksa" tiap bagian gambar/benda untuk mencatat temuan. |

Tombol: **Petunjuk** (setelah dua petunjuk berubah menjadi "Terapkan satu panduan"), **Kembali** (progres tersimpan),
**Selesaikan aktivitas** (aktif setelah papan benar).

### Encounter "Hadapi godaan" (Activity `Combat`, Bab 2–5)
Arena mental (bukan menyerang warga): gerak WASD/joystick, **Space / klik = serang**,
**Shift = menghindar** (ada imunitas & cooldown). Boss punya fase *Anticipation → Active →
Recovery* dengan area bahaya yang ditandai. Kalah = coba lagi tanpa kehilangan uang/bukti.
Data boss: `Assets/ScriptableObjects/Campaign/Encounter{2..5}.asset`.

### Cari → Berikan (side quest)
- NPC quest menampilkan **"!"** (ada langkah untukmu) atau **"?"** (menunggu barang yang belum kamu
  bawa) di atas kepala, plus nama di bawahnya.
- **E** ke NPC: jalankan langkah aktif (dialog / papan puzzle). Untuk langkah *give*, muncul kartu
  "Berikan … kepada …?" bila barangnya ada di Tas → barang keluar dari Tas.
- Hadiah: barang (popup "Aku mendapatkan …"), skor Logika/Syariah, **hubungan** (♥ di Jurnal).
- NPC tanpa langkah aktif mengucapkan baris santai.
- Lokasi NPC quest (dari `city.json`): Kirana & Tara di **Jalan Kafe**, Petugas bank di
  **Pusat Kota**, Pak Fadli di **Kampus Cempaka**, Bima & Bu Laras di **Jalan Pasar**,
  Pak Darto di **Pusat Kota**, Bu Ningsih di
  **Puskesmas Cempaka** (tanpa quest). Sprite masih
  placeholder dari tokoh lama; aset final: [`ASSET_BRIEF.md`](ASSET_BRIEF.md).

### HP & Peta (tombol ikon HP di HUD / **P**)
Layar utama berisi aplikasi: **Investasi** (kartu investasi yang terbuka), **Peta** (klik pin → teleport dengan fade ke titik spawn area;
terkunci selama tutorial — jalan kaki antar area yang bersebelahan lihat `roads` di atas), **Tas**, **Jurnal**, **Kontak** (hubungan ♥ NPC quest + tombol
"Temui" untuk teleport ke lokasinya), **Kalender**, **Pengaturan** (menu Jeda). Kode:
`Adventure/AdventureGame.Phone.cs`.

### Objek dunia Bab 1 (stasiun)
| Objek | Aksi |
|---|---|
| Layar info hijau (kanan Loket Karcis) | "Papan arah" — tugas/tutorial membaca petunjuk. |
| Mesin tiket bersegitiga (kiri meja loket) | Ambil **Voucher Promo Kereta** (sekali) → masuk Tas. |
| Mesin ATM biru (ruang kanan) | Buka UI ATM, tarik tunai 50k/100k/200k/500k/semua dari saldo bank. |
| Bangku | Monolog singkat. |
| Petugas stasiun (ruang Informasi), Naya (Loket Karcis) | NPC ambience / reaksi. |

---

## 6. Sistem

| Sistem | Ringkas | File |
|---|---|---|
| Orkestrasi bab | Tugas, HUD, modal (Jurnal/Tas/Jeda/Puzzle/Ending), tutorial, save | `Adventure/AdventureGame.cs` |
| State & save | `AdventureState` (tugas, bukti, posisi, uang), JSON berversi + backup | `Adventure/Model/*`, `Core/ChapterProgress.cs` |
| Inventory | 5 slot, stacking, pilih (pakai), tukar slot, event `OnItemAdded` | `Systems/InventorySystem.cs` |
| Katalog barang | Kegunaan + fun fact per nama barang | `Systems/ItemCatalog.cs` |
| Waktu | Jam/menit berjalan; **hari mengikuti hari cerita** (`SetStoryDay`); fajar tanpa tidur = `OnStoryDawn` | `Systems/TimeSystem.cs` |
| Jadwal warga | Hadir acak per hari × waktu (pagi/siang/malam/larut malam), deterministik, map tak pernah kosong | `Adventure/Model/NpcSchedule.cs`, `AdventureGame.Cases.cs` |
| Hari cerita & Kasus Warga | `AdventureState.Day`, tidur, gerbang `SideQuest.Day`/`QuestNpc.AppearsOnDay`, tugas `NeedsCases` | `Adventure/AdventureGame.Cases.cs`, `Model/CaseContent.cs`, `Model/SideQuest.cs` |
| Energi | 0–100% | `Systems/EnergySystem.cs` |
| Uang | Dompet + saldo bank, format Rupiah | `Systems/CurrencySystem.cs` |
| Skor | Logika Finansial & Kepatuhan Syariah (0–100%, ideal seimbang) | `Systems/ScoreSystem.cs` |
| Dialog | `DialogueManager` + `DialogueUI` | `Dialogue/*`, `UI/DialogueUI.cs` |
| Cutscene | Gambar layar penuh + kotak dialog ber-tab | `UI/CutscenePlayerController.cs` |
| Dev console | **F1** / **~** di PlayMode/dev build | `Dev/AlifDevConsole.cs` |

**Tas & barang**
- **Hotbar** 5 slot di bawah-tengah; turun saat Alif berjalan, naik lagi setelah berhenti ~0,3 dtk.
- **Klik barang** (hotbar/Tas) → kartu **DETAIL BARANG**: ikon, KEGUNAAN, FUN FACT, **Pakai/Lepas**.
- **Seret** barang ke slot lain (hotbar & Tas) untuk menata (`UI/InventorySlotDrag.cs`).
- **Dapat barang** → popup "Aku mendapatkan …" + ikon + OK (setelah dialog pengambilan selesai).
- Barang baru: tambahkan entri di `ItemCatalog` (nama harus sama dengan `ItemPickup._itemName`).

**Menu Jeda:** Lanjutkan · Simpan checkpoint · Ukuran teks (normal/besar) · Gerakan
(normal/dikurangi) · Joystick (tetap/mengambang/tersembunyi) · Simpan & menu utama.

**Save:** menyimpan juga isi Tas, pickup yang sudah diambil, side quest, dan hubungan NPC
(lintas bab). Otomatis saat tugas maju, saat pindah area, periodik selama tutorial, saat aplikasi
kehilangan fokus/keluar. MAIN BARU menghapus progres; LANJUTKAN memakai save terakhir.

---

## 7. HUD Adventure (tata letak)

```
┌ Tab hari ┐                ┌──────────── Panel tugas ────────────┐ [Tas][Jurnal][Jeda]
│ Papan status oranye:     │ TUGAS 2/8  Tanyakan menu — Depan…  │  [Lewati tutorial]
│  BAB 1 • Dalam stasiun   └─────────────────────────────────────┘
│  (jam) 07:14 Minggu ke-1        Kiri • Papan arah  (petunjuk)
│  (koin) Rp 100.000
└──────────────
 [⚡] ███ 100%
 [📈] Logika Finansial 50%
 [☾] Kepatuhan Syariah 50%
                                   E • Papan arah      (prompt)
 (joystick)            [ ][ ][ ][ ][ ]  (hotbar)                 (E)
```

---

## 8. Gaya UI — PixelSkin

Semua UI memakai `Assets/Scripts/Campaign/PixelSkin.cs`: sprite pixel-art digambar lewat kode
(tanpa aset), 9-slice, **1 piksel tekstur = 3 unit kanvas** (`PixelScale`), kanvas referensi
1280×720. Font: **Pixelify Sans SemiBold** (`Assets/Resources/Fonts/`, OFL).

| Token | Warna | Pakai untuk |
|---|---|---|
| `Outline` | #3E1F10 | Garis luar semua bingkai |
| `Orange` / `OrangeLight` / `OrangeShade` | #E4712C / #F6A66A / #B85420 | Badan tombol/papan, kilau, bayang |
| `Cream` / `CreamShade` | #F5F0E3 / #DDD4C0 | Isi panel, tab, slot |
| `TextDark` | #3A2519 | Teks di atas krem |
| `TextLight` | #FFF8EE | Teks di atas oranye / dunia |
| `Accent` | = OrangeShade | Label penekanan (TUGAS 1/8, KEGUNAAN) |
| `Gold*`, `SlotEdge`, `Warning` | — | Koin, garis slot, peringatan |

| Sprite | Bentuk |
|---|---|
| `Button()` | Tombol oranye (juga badan papan status) |
| `Panel()` | Bingkai oranye tebal + isi krem (kartu, kotak dialog) |
| `Tab()` | Tab krem berbingkai oranye (nama hari/pembicara, tombol sekunder) |
| `Slot()` | Kotak krem bergaris tipis (slot tas, baris status, bar) |
| `BarFill()` | Isi bar putih, diwarnai lewat `Image.color` |
| `Disc()`, `JoystickBase()`, `JoystickKnob()` | Tombol bulat & joystick |
| Ikon | `ClockIcon`, `CoinIcon`, `BoltIcon`, `MenuIcon`, `NotebookIcon`, `BagIcon`, `ChartIcon`, `CrescentIcon` |

**Aturan gaya**
- Tombol utama = oranye + teks terang; tombol sekunder (Lewati, Keluar) = tab krem + teks gelap.
- Kartu/modal = `Panel()` dengan judul kecil di `Tab()` yang menempel di tepi atas.
- Teks di dunia (petunjuk, prompt, toast) = terang bergaris `Outline`.
- Semua tombol flat di scene otomatis di-skin saat scene dimuat (`PixelSkin.SkinScene`);
  tombol buatan kode lewat `CampaignUI.Button` / `PixelSkin.StyleButton`.
- Hindari faux-bold (`FontStyles.Bold`) pada font pixel; tambah ikon baru sebagai pola ASCII
  di `PixelSkin.Pattern` atau bentuk bulat di `PixelSkin.Round`.

---

## 9. Peta File — "mau ubah X, buka Y"

| Mau mengubah | File |
|---|---|
| Teks cerita, tugas, intro/ending bab | `Scripts/Adventure/Model/AdventureContent.cs` |
| Area per bab, isi papan puzzle | `Scripts/Adventure/Model/OriginalCampaign.cs` |
| HUD, Tas, Jurnal, Jeda, puzzle UI, tutorial, popup barang | `Scripts/Adventure/AdventureGame.cs` |
| Warna, sprite, ikon, font UI | `Scripts/Campaign/PixelSkin.cs` |
| Tombol/teks/panel helper | `Scripts/Campaign/CampaignUI.cs` |
| Gerak, interaksi, jalan otomatis, prompt E | `Scripts/Player/PlayerController.cs`, `UI/InteractionPromptFX.cs` |
| Kotak dialog | `Scripts/UI/DialogueUI.cs` |
| Cutscene | `Scripts/UI/CutscenePlayerController.cs`, `Editor/AlifCutsceneBuilder.cs` |
| Kegunaan/fun fact/ikon barang | `Scripts/Systems/ItemCatalog.cs` |
| Side quest (teks, langkah, papan, hadiah) | `Scripts/Adventure/Model/SideQuestContent.cs` |
| Alur Berikan, penanda !/?, isi Tas di save | `Scripts/Adventure/AdventureGame.SideQuests.cs` |
| Map kota (latar, lantai, penghalang, spawn, posisi NPC, pin Peta) | `Tools/city-art/generate_city.py` (+ lukisan di `Tools/city-art/art/`) → `Assets/Resources/Kota/` |
| Perakitan kota runtime | `Scripts/Adventure/CityWorld.cs` |
| HP & aplikasi (Peta/Kontak/Kalender) | `Scripts/Adventure/AdventureGame.Phone.cs` |
| Kartu investasi, adegan cerita (teks) | `Scripts/Adventure/Model/StoryContent.cs` |
| Pemutar adegan cerita, aplikasi Investasi | `Scripts/Adventure/AdventureGame.Story.cs` |
| Gambar adegan cerita (placeholder → final) | `Assets/Resources/Story/StoryArt.asset` |
| Posisi prop/NPC/titik interaksi Bab 1 | `Editor/AlifAdventureBuilder.cs`, `Editor/AlifDemoSceneBuilder.cs` + scene |
| Boss encounter | `ScriptableObjects/Campaign/Encounter{2..5}.asset`, `Scripts/Campaign/ActionEncounterController.cs` |
| Bayangan kaki karakter | `Scripts/World/BlobShadow.cs` |

---

## 10. Aturan Kerja untuk Vibe Coding

1. **Gambar latar = dunia.** Stasiun, warung, dll. adalah gambar painted; banyak objek interaktif
   hanyalah collider/zona tak terlihat di atas gambar. Koordinat dunia dari gambar latar
   `Stasiun_Interior.png`: PPU 200, 2222×1888 px, pusat di (0,0) → lebar 11,11 × tinggi 9,44 unit.
2. **Jangan tumpuk prop di atas furnitur painted.** Pakai objek yang sudah ada di gambar sebagai
   titik interaksi (contoh: layar info = Papan arah, mesin tiket = voucher).
3. **Sprite karakter** pivot bawah-tengah, 256 PPU, ±18 px padding transparan di bawah kaki.
   Tinggi badan terlihat ±0,85 unit — samakan ukuran NPC/prop orang dengan ini.
4. **Builder adalah sumber kebenaran** (`AlifAdventureBuilder`, `AlifDemoSceneBuilder`,
   `AlifCutsceneBuilder`): setiap ubahan posisi/objek di scene juga harus masuk builder.
5. **Scene yang terbuka di Editor bisa basi.** Kalau `AdventureChapter1` terbuka dan belum
   disimpan, Play memakai versi memori, bukan file. Untuk mengubah scene yang sedang terbuka,
   pakai skrip Editor sekali-jalan (`[InitializeOnLoad]` + `playModeStateChanged`) yang
   idempotent, lalu hapus setelah scene disimpan.
6. **Skin UI dipasang runtime** — jangan mewarnai tombol/panel secara manual di scene; pakai
   `PixelSkin`.
7. **Perbarui test** saat mengubah geometri tutorial/stasiun: `Tests/PlayMode/AdventureChapterOneSmokeTests.cs`,
   `Tests/PlayMode/HudCaptureTests.cs`, `Tests/Adventure/ChapterOneWorldTests.cs`
   (tombol HUD dicari lewat nama GameObject, mis. `"Jurnal"`).
8. **Perintah:** `./Tools/alif validate`, `./Tools/alif test-edit`, `./Tools/alif test-play`,
   `./Tools/alif build-web`. Unity tidak recompile selama Play mode — hentikan Play dulu.
