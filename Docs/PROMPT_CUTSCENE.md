# Prompt cutscene (adegan cerita layar penuh)

Untuk membuat panel cutscene baru bergaya sama dengan `Assets/Sprites/Cutscenes/Chapter1/*.png`
(mis. `Chapter1_04_Peron.png` — peron Stasiun Solo yang dipakai sebagai adegan pembuka Bab 1).

**Selalu lampirkan `Chapter1_04_Peron.png` sebagai gambar acuan gaya.** Gayanya *bukan* pixel art —
ilustrasi digital bergaya anime dengan garis tegas dan pewarnaan cel, berbeda dari map kota.

## Spesifikasi kanvas

| Hal | Nilai |
|---|---|
| Ukuran | **2200 × 1174 px** (rasio ≈ 1,87 : 1), sama dengan panel yang sudah ada |
| Pita hitam | Tepi atas & bawah dimiringkan (letterbox diagonal), lihat acuan |
| Area aman | **28% bawah tertutup kotak dialog** dan pojok kanan-atas tertutup tombol "LEWATI" |
| Teks di gambar | Hanya papan nama tempat (mis. "STASIUN CEMPAKA"); tidak ada teks lain |

Karena kotak dialog menutupi bawah, taruh subjek utama di **sepertiga atas–tengah**; bagian bawah
diisi lantai/jalan yang boleh tertutup.

## Blok gaya (salin apa adanya)

```
Wide cinematic key-art illustration for an Indonesian slice-of-life RPG, in the exact art style of
the attached reference image (a painted anime-style Indonesian train station).

STYLE:
- Clean anime / modern Indonesian webtoon illustration: crisp confident line art, cel shading with
  soft gradient light, no texture noise, no painterly brush strokes, no pixel art.
- Warm daylight palette: terracotta roof tiles, cream plaster walls, teak-brown timber, dusty
  sunlight, soft blue sky with thin clouds.
- Real Indonesian details: Javanese joglo/limasan roofs, trotoar berpaving, kabel listrik, angkot,
  becak, warung tenda, plastic stools, hanging plants, papan nama bercat.
- Architecture and props drawn with accurate perspective and vanishing points (this is illustration,
  not the game's flat top-down map).

FRAMING:
- 2200 x 1174 px, wide cinematic panel with slanted black letterbox bars at the very top and bottom,
  exactly like the reference.
- Keep the main subject in the upper two-thirds: the bottom 28% will be covered by a dialogue box,
  and the top-right corner by a skip button.

DO NOT: no on-screen text except the signboard named below, no UI, no watermark, no logo, no
speech bubbles, no frame borders other than the letterbox bars, no modern western city look.
```

## Adegan sesudah tutorial (Bab 1)

Tutorial berakhir di dalam stasiun setelah Alif membaca papan arah; adegan berikutnya adalah
Alif keluar stasiun dan pertama kali melihat Cempaka. Isi blok adegannya:

```
SCENE: Morning, just outside the main entrance of a small Indonesian town train station. A young
man in his early twenties — black hair, thin glasses, navy blazer over a white shirt, dark
trousers, a canvas messenger bag on his shoulder and a small notebook in one hand — has just
stepped through the station doorway and pauses at the top of the entrance steps, looking out at
the street ahead. He is seen from behind and slightly to the side, small against the scene, so the
town is the subject rather than his face.

BACKGROUND: The station facade behind him with a Javanese tiled roof and a painted signboard
reading "STASIUN CEMPAKA". Ahead: a paved street with a row of small shops — a warung with an
orange awning, a minimarket, a market stall with fruit crates — becak and a parked motorbike at
the curb, potted plants, power lines crossing the sky, a few townspeople going about their
morning. Distant hills and a soft blue morning sky.

MOOD: The quiet, slightly nervous excitement of arriving somewhere new. Long morning shadows,
warm low sun from the left.
```

## Setelah gambarnya jadi

1. Simpan ke `Assets/Sprites/Cutscenes/Chapter1/Chapter1_06_KeluarStasiun.png` (2200 × 1174).
2. Daftarkan di `Assets/Resources/Story/StoryArt.asset`: tambah entri `Key: panel_keluar` yang
   menunjuk sprite itu.
3. Pakai di adegan: `StoryContent.ChapterArt[0]` (`"panel_tiba"`) diganti `"panel_keluar"` kalau
   ingin adegan pembuka bab memakai gambar baru, atau panggil lewat `Scene(...)` untuk adegan
   tersendiri.
4. Importer sprite-nya ikut aturan cutscene yang sudah ada (lihat `.meta` panel lain): `Sprite
   (2D and UI)`, filter `Bilinear`, PPU bebas karena ditampilkan memenuhi layar.
