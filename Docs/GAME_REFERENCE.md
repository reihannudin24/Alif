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
- **Tema per bab:** kejujuran transaksi → riba → maysir (judi/undian) → tadlis (penipuan label)
  → skema Ponzi.
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
  Pusat Kota · Kampus Cempaka · Taman Cempaka · Gang Permukiman

### Kota Cempaka (6 map)
Digambar lewat kode oleh `Tools/city-art/generate_city.py` (sumber tunggal tata letak: bangunan,
properti, lantai, penghalang, spawn, posisi NPC) → `Assets/Resources/Kota/*.png` + `city.json`,
lalu dirakit saat runtime oleh `Adventure/CityWorld.cs` (tanpa mengedit scene). Setiap map
64×24 petak (32×12 unit, PPU 32), dijajarkan di y≈−60 (di bawah plafon Y-sort prompt 8000).
Ubah tata letak → jalankan ulang skrip. Nama area harus sama dengan
`AdventureContent.CityAreas`.

| Map | Isi |
|---|---|
| Jalan Pasar | Toko Kelontong, Minimarket 24, Warung Sembako, halte |
| Jalan Kafe | Kafe Senja, Glow (kosmetik), Gerai HP, Kedai Kopi, Toko Buku |
| Pusat Kota | Bank Syariah + ATM, Masjid Al-Amanah, air mancur |
| Kampus Cempaka | Fakultas Ekonomi & Bisnis, gerbang Univ. Cempaka, Fotokopi, Rektorat |
| Taman Cempaka | Gazebo, kolam, jalan setapak, bangku |
| Gang Permukiman | Kos Cempaka, Warung Bima, rumah warga, kontrakan |

### Bab 1 — Warung Bu Siti · *01 / Awal yang jujur*
**Intro:** Alif baru tiba di Cempaka membawa buku catatan, alamat kos, dan uang untuk memulai
hari. Sebelum mencari kamar ia perlu makan; keputusan kecil mempertemukannya dengan teman baru.

| # | Id | Tugas | Target | Area | Jenis |
|---|---|---|---|---|---|
| 1 | `c1.arrival` | Baca petunjuk | Papan arah (layar info) | Dalam stasiun | talk |
| 2 | `c1.greeting` | Tanyakan menu | Bu Siti | Depan warung | talk |
| 3 | `c1.menu` | Susun pesanan (≤ Rp20.000) | Papan menu | Dalam Warung | budget |
| 4 | `c1.order` | Sampaikan pesanan | Bu Siti | Dalam Warung | talk |
| 5 | `c1.receipt` | Cocokkan struk (teh dobel, kerupuk tak dipesan) | Meja jendela | Dalam Warung | match |
| 6 | `c1.listen` | Dengarkan Raka | Raka | Depan warung | talk |
| 7 | `c1.resolve` | Selesaikan pembayaran Rp18.000 | Bu Siti | Dalam Warung | sort |
| 8 | `c1.next` | Ambil alamat kos Dimas | Papan arah | Depan warung | talk |

**Pelajaran:** anggarkan sebelum memesan, cocokkan struk dengan pesanan, koreksi kesalahan
tanpa menuduh niat. **Ending:** pesanan cocok, Raka merasa didengar, Bu Siti memberi alamat
kos Dimas; Alif menyimpan struk pertamanya.

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

Rangkaian K-pop dan rangkaian dosen berjalan paralel; di dalam rangkaian, quest berikutnya
terbuka setelah yang sebelumnya selesai. Progres, isi Tas, dan hubungan terbawa antar-bab.

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
  **Pusat Kota**, Pak Fadli di **Kampus Cempaka**, Bima di **Gang Permukiman**. Sprite masih
  placeholder dari tokoh lama; aset final: [`ASSET_BRIEF.md`](ASSET_BRIEF.md).

### HP & Peta (tombol ikon HP di HUD / **P**)
Layar utama berisi aplikasi: **Peta** (klik pin → teleport dengan fade ke titik spawn area;
terkunci selama tutorial), **Tas**, **Jurnal**, **Kontak** (hubungan ♥ NPC quest + tombol
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
| Waktu | Jam/menit, hari (Inggris → ditampilkan Indonesia), minggu | `Systems/TimeSystem.cs` |
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
| Map kota (gambar, lantai, penghalang, spawn, posisi NPC, pin Peta) | `Tools/city-art/generate_city.py` → `Assets/Resources/Kota/` |
| Perakitan kota runtime | `Scripts/Adventure/CityWorld.cs` |
| HP & aplikasi (Peta/Kontak/Kalender) | `Scripts/Adventure/AdventureGame.Phone.cs` |
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
