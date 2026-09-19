# Desain Quest — Kasus K-Popers & Dosen

Status: **diimplementasikan** (konten di `SideQuestContent.cs`, dimainkan dengan NPC
placeholder sampai aset di [`ASSET_BRIEF.md`](ASSET_BRIEF.md) tersedia). Encounter boss
"Si Penjerat Tautan" & hubungan yang membuka dialog baru belum dibuat. Diturunkan dari dua interview: seorang
K-poper (side quest) dan seorang dosen (quest line utama). Setelah disetujui, bagian yang final
dipindahkan ke [`GAME_REFERENCE.md`](GAME_REFERENCE.md).

> ⚠️ **Perlu dicek narasumber/dosen:** setiap klaim hukum syariah di dokumen ini ditandai 📖.
> Game ini edukatif — pastikan rumusan akhirnya disetujui orang yang kompeten sebelum rilis.

---

## 1. Mekanik Baru: Cari → Berikan (gaya Citampi)

Loop inti yang ditambahkan ke sistem yang sudah ada (Tas, hotbar, tombol Pakai, popup barang):

```
Bicara ke NPC ──► NPC butuh sesuatu ──► cari barang di dunia / hasil puzzle
      ▲                                              │
      └── cerita maju, hadiah, hubungan naik ◄── "Berikan" barang ke NPC
```

| Fitur | Cara kerja | Sudah ada? |
|---|---|---|
| Ambil barang | `ItemPickup` → Tas → popup "Aku mendapatkan …" | ✅ |
| Pilih barang | Kartu detail → **Pakai** (barang aktif disorot di hotbar) | ✅ |
| **Berikan barang** | Interaksi (E) ke NPC saat barang yang diminta sedang dipakai → NPC menerima, barang keluar dari Tas | 🆕 |
| **Barang hasil puzzle** | Menyelesaikan puzzle memberi barang (mis. "Surat kesepakatan cicilan") | 🆕 |
| **Hubungan NPC** | Ikon hati per NPC di Jurnal; naik saat dibantu, membuka dialog baru | 🆕 |
| **Side quest di Jurnal** | Halaman terpisah "CERITA SAMPINGAN" dengan status tiap quest | 🆕 |
| Hadiah | Uang, barang, skor Logika Finansial / Kepatuhan Syariah | ✅ (skor & uang) |

Aturan desain: edukasi **tipis-tipis** — maksimal 1–2 kalimat penjelasan per momen, selalu
lewat dialog tokoh, tidak menggurui, dan NPC tidak dipermalukan.

---

## 2. Tokoh Baru

| Nama (usulan) | Siapa | Dipakai di |
|---|---|---|
| **Kirana** | Siswi SMA 17 tahun, K-poper fans girl group, ceria, impulsif soal photocard | Side quest K-pop |
| **Tara** | Teman se-fandom Kirana, 20 tahun, mahasiswa, lebih hemat | Side quest K-pop |
| **Pak Fadli** | Dosen kampus swasta; dikira bergaji "fantastis" | Quest line Amanah |
| **Bima** | Teman SMA Pak Fadli, pedagang/karyawan, dulu selalu tepat bayar | Quest line Amanah |
| **"Om Rudi" / nomor tak dikenal** | Oknum penipu (villain) — tidak pernah muncul langsung, hanya lewat pesan | Quest line Amanah (bag. B) |
| Petugas bank syariah | NPC penjelas tabungan & SOP bank | Keduanya |

Nama sengaja fiktif (bukan nama narasumber). Merek asli (Weverse, Ktown4u, Oppo, Lemonilo,
Mediheal, aespa, BABYMONSTER, Bank Jago) **diganti nama fiktif** di game, mis. "Verse+",
"K-Mart Merch", "girl group NOVA", supaya aman dari masalah merek & tidak mengiklankan.

---

## 3. Side Quest K-Pop — "Kantong Konser" (4 quest, bisa dimainkan terpisah)

Lokasi usulan: **Minimarket / toko kosmetik** dekat stasiun, **kafe** tempat fans kumpul,
**bank syariah** (bisa memakai ATM/area yang ada).

### SQ-1 · "Empat Set demi Photocard" — *israf & tabdzir*
- **Pemicu:** Kirana di depan minimarket memeluk 4 kotak skincare.
- **Dialog (dari narasumber):**
  - Kirana: "Aku barusan beli 4 set skincare ini biar dapet semua photocard NOVA! Jujur ga bisa
    milih, empat member cantik semua jadi aku beliii dehh."
  - Alif: "Tapi produknya bakal kepakai semua nggak? Satu set aja bisa 6–7 bulan baru habis
    loh… dan kamu beli empat."
  - Kirana: "…nggak juga sih. Duh, takut kedaluwarsa. Tapi sayang, photocard-nya eksklusif."
- **Aktivitas (inspect):** periksa kotak → temukan *tanggal kedaluwarsa 12 bulan*.
  Hitung: 4 set × 6–7 bulan ≈ 24–28 bulan > 12 bulan → 2 set berisiko terbuang.
- **Cari → Berikan:** 2 set cadangan → berikan ke **ibu Kirana** dan **Tara** (dipakai,
  bukan dibuang) *atau* dijual lagi ke teman dengan harga wajar.
- **Edukasi 📖:** membeli berlebihan hingga barang terbuang termasuk *israf* (berlebihan) /
  *tabdzir* (menyia-nyiakan) — QS Al-A'raf: 31, QS Al-Isra': 26–27.
- **Hadiah:** Kepatuhan Syariah ↑, hubungan Kirana ↑, barang *Photocard duplikat* (untuk SQ-4).

### SQ-2 · "Kantong-Kantong Tabungan" — *tabungan syariah, tanpa bunga*
- **Pemicu:** Kirana ingin nonton konser 3 bulan lagi: tiket + transport + lightstick + merch.
- **Aktivitas (budget):** bagi uang saku ke "kantong" tabungan: Tiket · Transport · Lightstick ·
  Merch · *Kebutuhan sekolah (wajib dilindungi)*. Tidak boleh mengambil kantong kebutuhan.
- **Cari → Berikan:** ambil *Brosur Tabungan Syariah* dari petugas bank → berikan ke Kirana.
- **Edukasi 📖:** tabungan syariah memakai akad *wadiah* (titipan, tanpa bunga) atau
  *mudharabah* (bagi hasil, bukan bunga tetap). Bunga tabungan konvensional = persoalan riba
  menurut fatwa DSN-MUI.
- **Hadiah:** Logika Finansial ↑, barang *Buku kantong tabungan*.

### SQ-3 · "Checkout Lightstick" — *riba, paylater, koin, calo*
- **Pemicu:** Kirana & Tara membandingkan harga lightstick di dua aplikasi merch dan tiket calo.
- **Aktivitas (match):** cocokkan tiap cara bayar/beli dengan penilaiannya:

  | Cara | Penilaian (📖 cek ulang) |
  |---|---|
  | Bayar tunai/debit dari kantong tabungan | Aman |
  | Pakai koin/poin hasil belanja sebelumnya | Aman — diskon/hadiah penjual |
  | Kartu kredit dicicil dengan bunga / denda telat | Mengandung riba |
  | Paylater berbunga / denda keterlambatan | Mengandung riba |
  | Tiket dari calo 2× harga, hasil memborong tiket | Hati-hati: *ihtikar* (menimbun) & risiko tiket palsu (*gharar*) |
  | Bandingkan harga di dua aplikasi dulu | Bijak — kebutuhan vs keinginan |
- **Cari → Berikan:** *Struk perbandingan harga* → berikan ke Tara (yang ternyata juga
  hampir pakai paylater).
- **Hadiah:** Logika Finansial & Kepatuhan Syariah ↑, uang kembali (hemat).

### SQ-4 · "Tiket Undian Fansign" — *maysir vs promosi*
- **Pemicu:** Tara ingin membeli 10 album agar peluang menang undian fansign besar; teman lain
  membeli HP karena ada undian bertemu idol.
- **Aktivitas (sort):**
  - *Hadiah promosi tanpa biaya tambahan, barangnya memang dibutuhkan* → umumnya boleh 📖
  - *Membeli banyak barang yang tak dipakai demi memperbesar peluang* → mendekati *maysir* &
    *israf* 📖
  - *Membeli karena benar-benar butuh HP baru* → keputusan kebutuhan
- **Cari → Berikan:** *Photocard duplikat* (dari SQ-1) → tukar dengan Tara (barter sesama fans,
  jelas & sukarela) — Tara batal beli 10 album.
- **Penutup rangkaian:** Kirana & Tara menonton konser dengan kantong tabungan yang cukup,
  membawa lightstick, tanpa utang.

---

## 4. Quest Line Dosen — "Amanah yang Tertunda" (2 bagian)

Lokasi usulan: **Kampus (ruang dosen)**, **rumah/usaha Bima**, **kantor bank syariah**.
Cocok sebagai quest utama baru atau bab tambahan.

### Bagian A · "Pinjaman Sahabat" — *qardh, mencatat utang, memberi tangguh*
1. **Curhat Pak Fadli (talk):** orang mengira gajinya fantastis; sahabat SMA-nya, Bima, sudah
   beberapa kali meminjam dan selalu tepat bayar — kali ini belum lunas berbulan-bulan.
2. **Cari bukti (inspect):** Alif meminta catatan pinjaman → yang ada hanya chat singkat,
   tanpa jumlah & tanggal jelas.
   - **Edukasi 📖:** utang dianjurkan **ditulis** (jumlah, tenggat) dan disaksikan — QS
     Al-Baqarah: 282.
3. **Temui Bima (talk):** Bima menghindar, lalu mengaku sedang kesulitan besar (disimpan
   untuk Bagian B).
4. **Mediasi (sort):**

   | Pilihan | Kelompok |
   |---|---|
   | Susun ulang jadwal cicilan tertulis sesuai kemampuan | ✅ Penyelesaian |
   | Beri tangguh karena Bima benar-benar kesulitan (📖 QS Al-Baqarah: 280) | ✅ Penyelesaian |
   | Merelakan sebagian sebagai sedekah (pilihan, bukan kewajiban) | ✅ Penyelesaian |
   | Tambah "denda bunga" karena telat | ❌ Riba 📖 |
   | Umumkan utang Bima di grup alumni | ❌ Membuka aib |
   | Bima pura-pura tak mampu padahal mampu | ❌ Zalim — menunda utang padahal mampu 📖 |
5. **Cari → Berikan:** hasil mediasi = *Surat Kesepakatan Cicilan* → berikan ke Pak Fadli,
   lalu ke Bima untuk ditandatangani.
- **Hadiah:** hubungan keduanya pulih; Kepatuhan Syariah ↑.

### Bagian B · "Tautan Undangan" — *phishing, SOP bank, menjaga harta*
Pilihan kasus dari dosen: **phishing** (dipilih — Ponzi sudah dipakai di Bab 5). Alternatif
cadangan: judol, pinjol, MLM-Ponzi.

1. **Curhat Bima (talk):** ia menerima file "undangan pernikahan" di WhatsApp, memasangnya,
   lalu saldo rekening terkuras. Karena malu, ia menutup kerugian dengan pinjaman — sebab ia
   telat membayar Pak Fadli.
2. **Periksa HP Bima (inspect):** pesan berisi file *.apk*, SMS kode OTP, notifikasi transaksi
   yang tidak ia lakukan, nomor "CS bank" palsu.
3. **SOP darurat (flow — susun urutan):**
   1. Putuskan internet & hapus aplikasi mencurigakan
   2. Hubungi **call center resmi** bank (nomor di kartu/aplikasi resmi, bukan dari chat)
      untuk **blokir** rekening/kartu
   3. Ganti PIN & kata sandi; **jangan pernah bagikan OTP**
   4. Tulis **kronologi** & simpan bukti (tangkapan layar, mutasi)
   5. Lapor ke bank & kanal resmi (OJK — Kontak 157) 📖 cek kanal terbaru
   6. Beri tahu kontak bahwa nomornya disalahgunakan
- **Cari → Berikan:** *Kronologi tertulis* + *Mutasi rekening* → berikan ke petugas bank
  syariah.
- **Edukasi 📖:** menjaga harta (*hifzh al-mal*) termasuk tujuan syariat; mengambil harta
  orang dengan cara batil dilarang (QS An-Nisa': 29); setelah ikhtiar, bersabar — dan jangan
  menutup kerugian dengan utang berbunga.
4. **Encounter "Hadapi godaan":** boss mental **"Si Penjerat Tautan"** (pesan palsu yang
   beterbangan) memakai sistem encounter yang sudah ada.
- **Penutup:** Bima mengembalikan cicilan pertama sesuai surat; Pak Fadli & Bima kembali
  akrab; Pak Fadli mengajak Alif mengisi kelas literasi keuangan.

---

## 5. Dampak ke Game (untuk implementasi setelah disetujui)

| Kebutuhan | Keterangan |
|---|---|
| **Latar baru** | Minimarket, kafe, kampus/ruang dosen, rumah/usaha Bima, kantor bank syariah. Butuh gambar latar pixel-art bergaya sama dengan stasiun — **sumbernya perlu diputuskan** (AI generate / artis / aset pack). |
| **Sprite NPC** | Kirana, Tara, Pak Fadli, Bima, petugas bank (idle + jalan 4–8 arah, pivot bawah, 256 PPU). |
| **Kode** | Aksi "Berikan", barang hasil puzzle, side quest di Jurnal, hubungan NPC, `ItemCatalog` untuk barang baru. |
| **Konten** | Dialog, papan puzzle, dan barang ditulis di model konten (seperti `AdventureContent`). |
| **Tes** | Test model untuk urutan quest & "Berikan"; smoke test PlayMode per quest. |

---

## 6. Keputusan yang Dibutuhkan

1. **Nama tokoh** — setuju Kirana, Tara, Pak Fadli, Bima? Atau ada usulan lain?
2. **Posisi di cerita** — K-pop sebagai side quest bebas (kapan saja setelah Bab 1)?
   Dosen sebagai **bab baru (Bab 6)** atau disisipkan ke bab yang ada?
3. **Bagian B** — phishing (usulan) atau pinjol / judol / MLM-Ponzi?
4. **Gambar latar & sprite NPC** — dari mana asetnya?
5. **Review syariah** — siapa yang memeriksa klaim bertanda 📖 (dosen narasumber?).
