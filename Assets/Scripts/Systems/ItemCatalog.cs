using System.Collections.Generic;
using Alif.Campaign;
using UnityEngine;

namespace Alif.Systems
{
    /// <summary>
    /// Keterangan barang untuk kartu detail di Tas (kegunaan di dalam cerita dan fun fact
    /// literasi keuangan) serta ikonnya. Dicari dari nama barang (sama dengan
    /// ItemPickup._itemName / hadiah side quest), jadi isi tas bisa dipulihkan dari save hanya
    /// dengan nama. Ikon berupa pola piksel PixelSkin: o garis, c krem, S oranye tua, l garis
    /// halaman, Y kuning, g hijau, p merah muda, b biru.
    /// </summary>
    public static class ItemCatalog
    {
        public readonly struct ItemInfo
        {
            public readonly string Usage;
            public readonly string FunFact;
            public readonly string[] Icon;

            public ItemInfo(string usage, string funFact, string[] icon = null)
            {
                Usage = usage;
                FunFact = funFact;
                Icon = icon;
            }
        }

        static readonly string[] PaperIcon =
        {
            "ooooooooo.",
            "occcccccoo",
            "oclllllcco",
            "occcccccco",
            "oclllllcco",
            "occcccccco",
            "oclllllcco",
            "occcccccco",
            "ocllllccco",
            "occcccccco",
            "oooooooooo",
        };

        private static readonly Dictionary<string, ItemInfo> Items = new Dictionary<string, ItemInfo>
        {
            ["Voucher Promo Kereta"] = new ItemInfo(
                "Memberi potongan harga tiket kereta untuk perjalanan berikutnya. Tunjukkan ke petugas loket sebelum membayar.",
                "Voucher adalah diskon, bukan uang tunai: nilainya hanya berlaku untuk produk, waktu, dan syarat tertentu. Cek masa berlaku dan ketentuannya dulu — membeli hanya karena ada promo justru bisa menambah pengeluaran.",
                new[]
                {
                    ".oooooooooooo.",
                    "oYYYYcYYYYYYYo",
                    "oYYYYYYYYYYYYo",
                    "oYYYYcYYYYYYYo",
                    "oYYYYYYYYYYYYo",
                    "oYYYYcYYYYYYYo",
                    ".oooooooooooo.",
                }),
            ["Set Skincare Cadangan"] = new ItemInfo(
                "Set skincare kedua dari Kirana, untuk diberikan kepada Tara agar terpakai sebelum kedaluwarsa.",
                "Membeli berlebihan hingga barang terbuang termasuk israf dan tabdzir. Sebelum membeli bundel demi bonus, hitung dulu berapa lama barangnya akan terpakai.",
                new[]
                {
                    "..oooooooo..",
                    "..oppppppo..",
                    "oooooooooooo",
                    "occcccccccco",
                    "occppppppcco",
                    "occpccccpcco",
                    "occppppppcco",
                    "occcccccccco",
                    "occcccccccco",
                    "oooooooooooo",
                }),
            ["Brosur Tabungan Syariah"] = new ItemInfo(
                "Penjelasan tabungan syariah dari petugas bank, untuk Kirana yang menabung konser.",
                "Tabungan syariah memakai akad wadiah (titipan tanpa bunga) atau mudharabah (bagi hasil sesuai nisbah), bukan bunga tetap yang dijanjikan.",
                new[]
                {
                    "oooooooooo",
                    "oggggggggo",
                    "ogcccccggo",
                    "oggggggggo",
                    "occcccccco",
                    "oclllllcco",
                    "occcccccco",
                    "oclllllcco",
                    "occcccccco",
                    "oooooooooo",
                }),
            ["Struk Perbandingan Harga"] = new ItemInfo(
                "Catatan harga lightstick dari dua aplikasi, dibawa untuk meyakinkan Kirana agar tidak membeli dari calo.",
                "Membandingkan harga sebelum membeli membantu memisahkan kebutuhan dari keinginan sesaat. Bayar dari tabungan, bukan paylater berbunga.",
                PaperIcon),
            ["Photocard Duplikat"] = new ItemInfo(
                "Photocard dobel milik Tara untuk Kirana — hasil tukar-menukar sesama penggemar.",
                "Barter barang sesama teman yang jelas dan sukarela diperbolehkan, dan sering lebih hemat daripada membeli berulang demi bonus acak.",
                new[]
                {
                    "oooooooo",
                    "obbbbbbo",
                    "obpbbpbo",
                    "obppppbo",
                    "obbppbbo",
                    "obbbbbbo",
                    "occcccco",
                    "occcccco",
                    "oooooooo",
                }),
            ["Surat Kesepakatan Cicilan"] = new ItemInfo(
                "Kesepakatan tertulis antara Pak Fadli dan Bima: jumlah pinjaman, cicilan per bulan, dan tanggalnya.",
                "Utang dianjurkan ditulis — jumlah dan tenggatnya — serta disaksikan (QS Al-Baqarah: 282). Catatan yang jelas menjaga persahabatan dari salah paham.",
                new[]
                {
                    "ooooooooo.",
                    "occcccccoo",
                    "oclllllcco",
                    "occcccccco",
                    "oclllllcco",
                    "occcccccco",
                    "occcccSSco",
                    "occccSSSSo",
                    "occcccSSco",
                    "oooooooooo",
                }),
            ["Kronologi Tertulis"] = new ItemInfo(
                "Catatan urutan kejadian phishing yang dialami Bima, untuk diserahkan ke bank.",
                "Korban penipuan perlu segera memblokir rekening lewat call center resmi, mencatat kronologi, menyimpan bukti, dan melapor ke bank serta kanal resmi seperti Kontak OJK 157.",
                PaperIcon),
            // Barang Kasus Warga Bab 1 (CaseContent)
            ["Daftar Harga Pasaran"] = new ItemInfo(
                "Catatan Mbak Mira: harga jeruk per kilo di tiga lapak Jalan Pasar, untuk ditunjukkan ke Pak Yanto.",
                "Jual beli sah bila kedua pihak sama-sama rela—dan rela butuh tahu. Memanfaatkan ketidaktahuan pembeli akan harga pasaran disebut ghabn.",
                PaperIcon),
            ["Salinan Akta Ikrar Wakaf"] = new ItemInfo(
                "Salinan akta ikrar wakaf H. Dahlan tahun 1998 atas tanah Taman Cempaka, dari nazhir Pak Mahfud.",
                "Harta wakaf ditahan pokoknya dan dialirkan manfaatnya: tidak dijual, tidak dihibahkan, tidak diwariskan. Ikrar dicatat dalam Akta Ikrar Wakaf di hadapan pejabat KUA.",
                PaperIcon),
            ["Struk Koreksi Idmaret"] = new ItemInfo(
                "Struk cetak ulang untuk Pak Joko: tanpa donasi otomatis, dengan kembalian Rp500 utuh.",
                "Kembalian adalah hak pembeli, dan sedekah hanya bernilai bila sukarela. Harta orang lain halal diambil hanya dengan kerelaan pemiliknya (QS An-Nisa: 29).",
                PaperIcon),
            ["Laporan Kas Himpunan"] = new ItemInfo(
                "Rekap kas seminar: pemasukan Rp1.000.000, pengeluaran Rp850.000, sisa Rp150.000, talangan Salsa dikembalikan.",
                "Memegang uang orang banyak adalah amanah; catatan yang bisa diperiksa melindungi pemegangnya dari prasangka.",
                PaperIcon),
            ["Surat Perjanjian Kerja"] = new ItemInfo(
                "Kesepakatan tertulis Mas Bayu dan Gilang: upah per jam dan tanggal pembayaran tiap bulan.",
                "Dalam akad ijarah, besaran upah harus jelas sejak awal dan dibayar tepat waktu—\"sebelum kering keringatnya\".",
                PaperIcon),
            ["Buku Bon Baru"] = new ItemInfo(
                "Buku bon Bima yang baru: nama lengkap, tanggal, jumlah, tenggat, dan kolom paraf kedua pihak.",
                "Ayat terpanjang dalam Al-Qur'an (QS Al-Baqarah: 282) justru berisi anjuran mencatat utang. Tambahan karena telat bayar adalah riba.",
                PaperIcon),
        };

        private static readonly ItemInfo Unknown = new ItemInfo(
            "Barang bawaan Alif.",
            "Belum ada catatan tentang barang ini.");

        private static readonly Dictionary<string, Sprite> Icons = new Dictionary<string, Sprite>();

        public static ItemInfo Get(string itemName) =>
            itemName != null && Items.TryGetValue(itemName, out ItemInfo info) ? info : Unknown;

        /// <summary>Ikon katalog barang, atau null bila barang tidak punya pola ikon.</summary>
        public static Sprite Icon(string itemName)
        {
            if (itemName == null) return null;
            if (Icons.TryGetValue(itemName, out Sprite cached)) return cached;
            var rows = Get(itemName).Icon;
            Sprite sprite = rows == null ? null : PixelSkin.PatternSprite("Item " + itemName, rows);
            Icons[itemName] = sprite;
            return sprite;
        }
    }
}
