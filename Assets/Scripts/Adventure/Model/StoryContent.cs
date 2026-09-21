using System;
using System.Collections.Generic;
using System.Linq;

namespace Alif.Adventure
{
    /// <summary>Adegan cerita layar penuh (gambar + dialog), diputar sekali: saat pertama
    /// bertemu NPC, saat bab dimulai, atau saat pertama bertemu tokoh main quest.</summary>
    public sealed class StoryScene
    {
        public string Id;
        /// <summary>Kunci gambar di StoryArtLibrary, mis. "cerita_kafe"; "kota:&lt;file&gt;" masih didukung untuk map kota.</summary>
        public string Art;
        /// <summary>Tokoh yang digambar besar di adegan (nama CharacterData placeholder), boleh null.</summary>
        public string Character;
        /// <summary>Format "Pembicara|teks". Boleh diawali tag panggung, mis.
        /// "[art=warung_dapur tokoh=-] Narator|…" — lihat <see cref="StoryLine"/>.</summary>
        public string[] Lines = Array.Empty<string>();
    }

    /// <summary>
    /// Satu baris adegan yang sudah diurai. Tag panggung di awal baris mengganti gambar dan/atau
    /// tokoh mulai baris itu sampai diganti lagi: "[art=kunci]" memakai kunci StoryArtLibrary,
    /// "[tokoh=nama]" memakai nama CharacterData, "[tokoh=-]" menyembunyikan tokoh. Tanpa tag,
    /// panggung baris sebelumnya dipertahankan (baris pertama memakai Art/Character adegannya).
    /// </summary>
    public readonly struct StoryLine
    {
        public readonly string Speaker, Text;
        /// <summary>Kunci gambar baru, atau null bila tidak berubah.</summary>
        public readonly string Art;
        /// <summary>Tokoh baru; "" = tanpa tokoh, null = tidak berubah.</summary>
        public readonly string Character;

        StoryLine(string speaker, string text, string art, string character)
        {
            Speaker = speaker; Text = text; Art = art; Character = character;
        }

        public static StoryLine Parse(string raw)
        {
            raw ??= "";
            string art = null, character = null;
            int close = raw.StartsWith("[") ? raw.IndexOf(']') : -1;
            if (close > 0)
            {
                foreach (var pair in raw.Substring(1, close - 1).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    int eq = pair.IndexOf('=');
                    if (eq <= 0) continue;
                    string key = pair.Substring(0, eq), value = pair.Substring(eq + 1);
                    if (key == "art") art = value;
                    else if (key == "tokoh") character = value == "-" ? "" : value;
                }
                raw = raw.Substring(close + 1).TrimStart();
            }
            var parts = raw.Split(new[] { '|' }, 2);
            return new StoryLine(parts[0], parts.Length > 1 ? parts[1] : "", art, character);
        }
    }

    /// <summary>Kartu investasi syariah yang terbuka setelah quest NPC-nya selesai (aplikasi
    /// Investasi di HP). Kolom mengikuti tabel rancangan: contoh, cara hasil, risiko. Stars =
    /// penilaian "cocok untuk game" dari rancangan — hanya untuk urutan, tidak ditampilkan.</summary>
    public sealed class InvestmentCard
    {
        public string Id, Name, Examples, Returns, Risk, Lesson;
        public int Stars;
    }

    /// <summary>
    /// Konten cerita & investasi. Tokoh investasi memakai sprite tokoh lama sebagai placeholder
    /// (QuestNpc.Placeholder) sampai gambar finalnya dibuat. Klaim syariah perlu ditinjau
    /// narasumber sebelum rilis (lihat Docs/QUEST_DESIGN.md).
    /// </summary>
    /// <summary>Batas versi demo. Ubah di sini kalau demonya mau dipanjangkan: matikan dengan
    /// <see cref="Enabled"/> = false untuk rilis penuh, atau geser <see cref="EndDay"/> /
    /// <see cref="EndArea"/> ke titik lain (mis. Hari 3 saat keluar dari Warung Bu Siti).</summary>
    public static class DemoStage
    {
        public const bool Enabled = true;
        /// <summary>Demo berakhir saat pemain keluar dari EndArea pada hari ini atau sesudahnya.</summary>
        public const int EndDay = 2;
        public const string EndArea = "Kamar Alif";
        public const string SceneId = "demo:outro";

        public static readonly string[] Credits =
        {
            "ALIF — DEMO",
            "Bab 1: Cempaka, Minggu Pertama",
            "",
            "Cerita & desain permainan  ·  Tim Alif",
            "Pixel art kota & interior  ·  Tim Alif",
            "Materi literasi keuangan syariah  ·  ditinjau narasumber",
            "",
            "Terima kasih sudah memainkan demo ini.",
            "Bab 2 — Jebakan Riba sedang disiapkan.",
        };
    }

    public static class StoryContent
    {
        public static readonly InvestmentCard[] Cards =
        {
            new InvestmentCard { Id = "saham", Name = "Saham Syariah", Stars = 5, Risk = "Tinggi",
                Examples = "Saham yang masuk Daftar Efek Syariah (DES), ISSI, atau JII.",
                Returns = "Capital gain (kenaikan harga) + dividen.",
                Lesson = "Saham lolos seleksi syariah bila usahanya halal, utang berbasis bunga ≤45% total aset, dan pendapatan non-halal ≤10%. Harganya naik-turun: pakai uang dingin, untuk jangka panjang." },
            new InvestmentCard { Id = "sukuk", Name = "Sukuk", Stars = 5, Risk = "Rendah–menengah",
                Examples = "Sukuk Ritel (SR), Sukuk Tabungan (ST), sukuk korporasi.",
                Returns = "Imbal hasil / bagi hasil / sewa (ijarah) sesuai akad.",
                Lesson = "Sukuk berbasis aset dasar (underlying asset), bukan utang berbunga. SR bisa diperdagangkan sebelum jatuh tempo; ST tidak, tetapi bisa dicairkan sebagian lebih awal." },
            new InvestmentCard { Id = "reksadana", Name = "Reksa Dana Syariah", Stars = 5, Risk = "Rendah–tinggi tergantung jenis",
                Examples = "Pasar uang, pendapatan tetap, saham, campuran.",
                Returns = "Kenaikan NAB (Nilai Aktiva Bersih) per unit.",
                Lesson = "Dikelola manajer investasi dan diawasi Dewan Pengawas Syariah. Pilih jenis sesuai jangka waktu: pasar uang untuk jangka pendek, saham untuk jangka panjang." },
            new InvestmentCard { Id = "emas", Name = "Emas Syariah", Stars = 5, Risk = "Menengah",
                Examples = "Emas fisik / layanan emas syariah.",
                Returns = "Kenaikan harga emas.",
                Lesson = "Fatwa DSN-MUI 77/2010: jual beli emas tidak tunai boleh selama emas bukan alat tukar resmi. Beli di lembaga resmi dengan bukti kepemilikan; waspadai selisih harga jual-beli." },
            new InvestmentCard { Id = "etf", Name = "ETF Syariah", Stars = 4, Risk = "Menengah–tinggi",
                Examples = "ETF berbasis indeks syariah.",
                Returns = "Kenaikan harga + kinerja portofolio acuannya.",
                Lesson = "Reksa dana yang unitnya diperdagangkan di bursa seperti saham, berisi sekeranjang saham syariah mengikuti indeks." },
            new InvestmentCard { Id = "p2p", Name = "P2P / Crowdfunding Syariah", Stars = 4, Risk = "Menengah–tinggi",
                Examples = "Pendanaan UMKM berbasis syariah.",
                Returns = "Bagi hasil / margin sesuai akad.",
                Lesson = "Pastikan platform berizin OJK, baca akad dan profil usaha, sebar dana ke beberapa UMKM. Risikonya gagal bayar dan dana belum bisa ditarik sebelum tenor selesai." },
            new InvestmentCard { Id = "properti", Name = "Properti Syariah", Stars = 4, Risk = "Menengah",
                Examples = "Kepemilikan properti atau investasi real estate syariah.",
                Returns = "Sewa + apresiasi (kenaikan nilai) aset.",
                Lesson = "Pembiayaan rumah syariah memakai akad seperti murabahah (harga disepakati di awal) atau musyarakah mutanaqisah. Properti sulit dijual cepat dan butuh biaya perawatan." },
            new InvestmentCard { Id = "deposito", Name = "Deposito Syariah", Stars = 3, Risk = "Rendah",
                Examples = "Deposito mudharabah bank syariah.",
                Returns = "Bagi hasil sesuai nisbah.",
                Lesson = "Hasilnya dari nisbah bagi hasil atas pendapatan bank, bukan bunga tetap, sehingga bisa naik-turun. Simpanan di bank syariah juga dijamin LPS sesuai ketentuan." },
            new InvestmentCard { Id = "dire", Name = "DIRE Syariah", Stars = 3, Risk = "Menengah",
                Examples = "Dana Investasi Real Estat syariah.",
                Returns = "Pendapatan properti (sewa) yang dibagikan.",
                Lesson = "Investor membeli unit; dananya dipakai memiliki properti produktif seperti gedung perkantoran atau pusat belanja. Risikonya bila tingkat hunian atau sewa menurun." },
            new InvestmentCard { Id = "eba", Name = "EBA Syariah", Stars = 2, Risk = "Menengah",
                Examples = "Efek Beragun Aset syariah.",
                Returns = "Arus kas dari aset dasar (underlying asset).",
                Lesson = "Kumpulan aset keuangan, misalnya piutang pembiayaan, dikemas menjadi efek. Strukturnya lebih rumit — pelajari setelah memahami investasi dasar." },
        };

        public static InvestmentCard Card(string id) => Cards.FirstOrDefault(c => c.Id == id);

        static readonly Dictionary<string, StoryScene> NpcIntros = new[]
        {
            // Side quest yang sudah ada
            Scene("intro:kirana", "cerita_kafe", "naya", "Narator|Di depan toko kosmetik Glow, seorang gadis berkaus lilac melompat-lompat kecil sambil memeluk kotak-kotak skincare.", "Kirana|Kakak! Kakak suka K-pop nggak? Aku barusan dapet photocard NOVA, lihat deh!", "Alif (Batin)|Semangatnya menular… tapi kotak di tangannya banyak sekali."),
            Scene("intro:tara", "cerita_kafe", "bu_siti", "Narator|Seorang mahasiswi berhijab merah muda duduk tenang di meja kafe, lightstick putih bersandar di cangkirnya.", "Tara|Halo. Kamu temannya Kirana, ya? Aku Tara—bendahara grup fans kami.", "Tara|Tugas bendahara itu berat, lho. Harus bikin teman-teman senang tanpa bikin dompet mereka nangis."),
            Scene("intro:bank", "cerita_pusat", "naya", "Narator|Pintu kaca Bank Syariah terbuka. Seorang petugas berhijab hijau menyambut dengan papan klip di tangan.", "Petugas Bank Syariah|Selamat datang di Bank Syariah Cempaka. Ada yang bisa kami bantu hari ini?", "Alif (Batin)|Tempat yang pas untuk bertanya soal uang tanpa rasa sungkan."),
            Scene("intro:fadli", "cerita_kampus", "raka", "Narator|Di depan Fakultas Ekonomi & Bisnis, seorang pria berkemeja batik biru memandangi ponselnya dengan wajah gelisah.", "Pak Fadli|Oh, maaf—saya Fadli, dosen di sini. Kelihatan sekali ya saya sedang banyak pikiran?", "Pak Fadli|Mengajar keuangan itu mudah. Menagih uang ke sahabat sendiri… itu yang sulit."),
            Scene("intro:bima", "cerita_pasar", "dimas", "Narator|Di Jalan Pasar, seorang pria menata keripik di etalase warung kecilnya. Ponselnya yang retak terus berbunyi.", "Bima|Eh, mau beli keripik? Silakan… maaf, HP-ku ramai notifikasi terus.", "Alif (Batin)|Wajahnya lelah, seperti sedang menanggung sesuatu sendirian."),
            // Investasi — satu NPC untuk satu jenis
            Scene("intro:arga", "cerita_kafe", "raka", "Narator|Di Kedai Kopi, seorang barista meracik kopi sambil melirik grafik hijau-merah di laptopnya.", "Mas Arga|Kopi sama saham itu mirip: dua-duanya pahit kalau diminum sambil panik.", "Mas Arga|Aku Arga. Pagi jadi barista, malam belajar jadi investor. Duduk dulu, Mas."),
            Scene("intro:ratna", "cerita_pusat", "bu_siti", "Narator|Di bangku dekat air mancur, seorang ibu pensiunan guru merajut syal sambil mengawasi burung merpati.", "Bu Ratna|Anak muda sekarang sukanya yang cepat-cepat. Ibu dulu sukanya yang pasti-pasti saja.", "Bu Ratna|Panggil saja Bu Ratna. Tiga puluh tahun mengajar, gajinya Ibu tanam pelan-pelan."),
            Scene("intro:dewi", "cerita_kampus", "naya", "Narator|Seorang mahasiswi menempelkan poster kuis di papan pengumuman kampus: \"MENANG KUIS, DAPAT KOPI GRATIS!\"", "Dewi|Kakak mau ikut kuis? Tenang, ini kuis beneran, bukan undian!", "Dewi|Aku Dewi, anak klub investasi kampus. Misi kami: bikin teman-teman berani mulai investasi dari kecil."),
            Scene("intro:harun", "cerita_pasar", "pak_ustad", "Narator|Di Jalan Pasar, seorang bapak menimbang cincin di timbangan kecil sambil bersenandung.", "Pak Harun|Emas itu nggak pernah bohong, Nak. Tapi harganya bisa bikin jantung naik-turun.", "Pak Harun|Harun, pedagang emas tiga generasi. Mau lihat cara Bapak memeriksa keaslian emas?"),
            Scene("intro:nadia", "cerita_taman", "naya", "Narator|Di Taman Cempaka, seorang kreator konten merekam video sambil berlari kecil mengitari kolam.", "Kak Nadia|Halo, teman-teman! Hari ini kita bahas… eh, maaf, aku kira kamu penontonku!", "Kak Nadia|Aku Nadia. Kontenku soal keuangan anak muda—biar nggak takut sama istilah ribet."),
            Scene("intro:laras", "cerita_pasar", "bu_siti", "Narator|Aroma keripik pisang tercium dari lapak kecil di Jalan Pasar. Seorang ibu mengemas pesanan dengan cekatan.", "Bu Laras|Mampir, Nak! Keripik ini hasil urunan warga lewat pendanaan syariah, lho.", "Bu Laras|Saya Laras. Dulu modal cuma wajan satu, sekarang sudah kirim ke tiga kota."),
            Scene("intro:darto", "cerita_pusat", "pak_ustad", "Narator|Di trotoar Pusat Kota, seorang bapak menghitung tagihan listrik kontrakannya sambil mencatat di buku kecil.", "Pak Darto|Punya rumah itu enak, Nak. Merawatnya yang bikin pusing.", "Pak Darto|Saya Darto, pemilik kontrakan ini. Dulu belinya lewat pembiayaan syariah."),
            Scene("intro:sinta", "cerita_pusat", "naya", "Narator|Seorang teller muda keluar dari Bank Syariah untuk istirahat siang, membawa kotak bekal.", "Mbak Sinta|Istirahat dulu, hitung-hitungan uang orang lain terus bikin lapar.", "Mbak Sinta|Aku Sinta, teller di sini. Pertanyaan favoritku: \"Bunganya berapa, Mbak?\" Hehe."),
            Scene("intro:hendra", "cerita_pasar", "raka", "Narator|Seorang pria berkemeja rapi mengamati ruko-ruko di Jalan Pasar sambil menghitung jumlah penyewa.", "Om Hendra|Tahu nggak? Gedung besar itu kadang dimiliki ribuan orang sekaligus.", "Om Hendra|Hendra. Pekerjaanku mengelola properti untuk para pemilik yang patungan."),
            Scene("intro:yusuf", "cerita_kampus", "dimas", "Narator|Di depan Gedung Rektorat, seorang dosen berkacamata tebal membawa setumpuk buku keuangan.", "Pak Yusuf|Materi saya ini biasanya untuk semester akhir. Tapi kalau kamu sudah sampai sini, berarti kamu siap.", "Pak Yusuf|Yusuf. Saya mengajar pasar modal syariah."),
            // Warga Puskesmas Cempaka
            Scene("intro:ningsih", "kota:J3P_Puskesmas", "bu_siti", "Narator|Pintu kaca Puskesmas menutup pelan. Aroma antiseptik, deret kursi tunggu, dan seorang perawat berhijab biru muda menata berkas di meja pendaftaran.", "Bu Ningsih|Selamat datang di Puskesmas Cempaka. Mau berobat, atau cuma numpang teduh?", "Bu Ningsih|Saya Ningsih. Di sini saya sering lihat orang sehat jadi sakit karena memikirkan biaya—padahal bisa disiapkan dari jauh hari."),
            // Kasir FFC Pusat Kota
            Scene("intro:fira", "kota:J4F_FFC", "naya", "Narator|Pintu kaca FFC mendesis menutup. Aroma ayam goreng, papan menu bercahaya, dan seorang kasir berseragam merah menyapa dari balik meja pesan.", "Mbak Fira|Selamat datang di FFC! Mau paket hemat atau cuma numpang adem, Kak?", "Mbak Fira|Aku Fira. Tiap hari aku lihat orang bayar pakai \"nanti dipikir\"—padahal struknya tetap datang di akhir bulan."),
            // Pemilik Kafe Senja
            Scene("intro:bayu", "kota:J3S_KafeSenja", "raka", "Narator|Lonceng pintu Kafe Senja berdenting. Cahaya jingga dari jendela jatuh di lantai ubin, dan seorang pria bercelemek mengelap mesin espresso.", "Mas Bayu|Mari, Kak. Mau seduh manual atau yang cepat saja?", "Mas Bayu|Aku Bayu, yang punya tempat ini. Dulu kafenya ramai tapi kasnya kosong—ternyata uang warung kucampur uang rumah."),
            // Penghuni tetap interior (tanpa quest) — setiap map punya minimal satu tokoh
            Scene("intro:wulan", "kota:K1L_Lobi", "naya", "Narator|Lobi Fakultas Ekonomi lengang di sela jam kuliah. Di balik meja resepsionis, seorang staf tata usaha merapikan map berwarna-warni.", "Mbak Wulan|Selamat siang. Tamu fakultas, ya? Saya Wulan dari tata usaha. Kelas di kiri, ruang dosen di tengah, toilet di kanan."),
            Scene("intro:reza", "kota:K1K_Kelas", "dimas", "Narator|Kelas Ekonomi kosong selepas kuliah pagi. Seorang mahasiswa masih duduk menyalin catatan dari papan tulis.", "Reza|Eh, bukan dosen pengganti, kan? Syukurlah. Aku Reza. Lagi nyalin rumus sebelum dihapus petugas."),
            Scene("intro:hana", "kota:K1D_RuangDosen", "bu_siti", "Narator|Ruang dosen beraroma kertas dan teh hangat. Seorang ibu dosen menandai setumpuk lembar jawaban dengan pena merah.", "Bu Hana|Silakan masuk. Saya Hana. Kalau mau bertanya soal muamalah, tanya saja—yang penting jangan tanya bocoran ujian."),
            Scene("intro:karjo", "kota:K1T_Toilet", "pak_ustad", "Narator|Lantai toilet kampus mengilap, bau karbol masih segar. Seorang bapak bertopi menyandarkan alat pelnya ke dinding.", "Pak Karjo|Hati-hati licin, Mas. Saya Karjo, yang bersih-bersih di sini dari subuh."),
            Scene("intro:satrio", "kota:J4B_BankSyariah", "raka", "Narator|Pintu kaca Bank Syariah terbuka pelan. Seorang satpam berseragam biru tua menyambut sambil menunjuk mesin antrean.", "Pak Satrio|Selamat datang di Bank Syariah Cempaka. Saya Satrio. Keperluannya teller atau layanan nasabah?"),
            Scene("intro:doni", "kota:J2M_Minimarket", "raka", "Narator|Bel pintu Minimarket 24 berdenting. Dari balik meja kasir, seorang pemuda berseragam merah mengangkat tangan menyapa.", "Mas Doni|Selamat datang di Minimarket 24! Saya Doni. Keranjang di sebelah kiri, ya, Kak."),
            Scene("intro:vina", "kota:J2L_GGLearning", "naya", "Narator|GG Learning Center terang dan rapi; layar sambutan menyala di dinding. Seorang tutor muda menata modul di meja resepsionis.", "Kak Vina|Halo! Mau daftar kelas, atau lihat-lihat dulu? Aku Vina, tutor matematika di sini."),
            Scene("intro:markus", "kota:J2G_Gereja", "dimas", "Narator|Cahaya kaca patri jatuh berwarna-warni di lorong Gereja Kasih Sejati. Seorang bapak merapikan buku nyanyian di bangku jemaat.", "Pak Markus|Selamat datang, Nak. Saya Markus, koster gereja ini. Silakan kalau mau duduk sebentar—pintu kami terbuka untuk tetangga."),
            Scene("intro:sasa", "kota:J3G_Glow", "naya", "Narator|Toko Glow wangi bunga dan serba merah muda. Seorang pramuniaga menata botol serum di rak pajang dengan teliti.", "Kak Sasa|Selamat datang di Glow! Aku Sasa. Cari sesuatu untuk kulitmu, atau titipan seseorang?"),
            Scene("intro:tini", "kota:J2K_KamarAlif", "bu_siti", "Narator|Kamar depan rumah kos itu sederhana: ranjang, lemari, meja tulis, dan jendela menghadap Jalan Pasar. Seorang ibu meletakkan seprai bersih di ujung ranjang.", "Bu Tini|Kamu Alif yang dibilang Siti, kan? Saya Tini, sepupunya. Ini kamarmu. Anggap rumah sendiri, tapi jangan lupa kunci pintu."),
            // Tokoh Kasus Warga Bab 1 (CaseContent) — satu map, satu persoalan
            Scene("intro:yanto", "cerita_pasar", "pak_ustad", "Narator|Di depan Toko Kelontong, seorang bapak menata jeruk dan salak di meja dagangan. Tidak ada satu pun papan harga di lapaknya.", "Pak Yanto|Monggo, Mas, dilihat-lihat dulu. Buahnya segar semua, baru turun dari lereng."),
            Scene("intro:mira", "cerita_pasar", "naya", "Narator|Seorang perempuan berdiri di dekat lapak buah sambil menggenggam kantong jeruk dan selembar nota. Wajahnya merah menahan kesal.", "Mbak Mira|Mas, maaf, boleh saya minta tolong jadi saksi? Saya merasa dibohongi, tapi bingung membuktikannya."),
            Scene("intro:wati", "cerita_pasar", "bu_siti", "Narator|Seorang ibu berjalan mondar-mandir di trotoar Jalan Pasar, sesekali menoleh ke arah lapak keripik Bima.", "Bu Wati|Nak, kamu yang sering bantu-bantu warga itu, kan? Tolong Ibu. Ibu ditagih utang yang sudah Ibu bayar."),
            Scene("intro:salamah", "cerita_taman", "bu_siti", "Narator|Di Taman Cempaka, seorang ibu sepuh menyapu daun kering di jalan setapak dengan sapu lidi, pelan tapi telaten.", "Bu Salamah|Pendatang baru, ya? Duduklah di bangku mana saja. Taman ini memang dibuat supaya siapa pun boleh singgah."),
            Scene("intro:gunawan", "cerita_taman", "raka", "Narator|Kerumunan kecil terbentuk di dekat gazebo. Seorang pria bermap cokelat menancapkan papan: \"TANAH SENGKETA — MILIK AHLI WARIS\".", "Pak Gunawan|Mulai hari ini taman ini ditutup. Saya ahli warisnya, dan saya punya hak atas tanah ini!", "Alif (Batin)|Warga saling pandang. Tidak ada yang berani membantah—tapi tidak ada juga yang percaya begitu saja."),
            Scene("intro:mahfud", "kota:J4M_Masjid", "pak_ustad", "Narator|Masjid Al-Amanah lengang selepas zuhur. Seorang bapak berpeci merapikan mushaf di rak, lalu menoleh sambil tersenyum.", "Pak Mahfud|Assalamu'alaikum. Saya Mahfud, takmir sekaligus nazhir wakaf di lingkungan sini. Ada yang bisa saya bantu?"),
            Scene("intro:joko", "cerita_pusat", "dimas", "Narator|Di depan Idmaret, seorang bapak berdiri sambil menatap struk belanja dan sebutir permen di telapak tangannya.", "Pak Joko|Saya belanja dua puluh sembilan ribu lima ratus, bayar tiga puluh ribu. Kembaliannya… permen. Permen!"),
            Scene("intro:rini", "kota:J4I_Idmaret", "naya", "Narator|Pintu kaca Idmaret bergeser. Di balik meja kasir, seorang pegawai berseragam biru menyapa dengan senyum yang sudah terlatih.", "Mbak Rini|Selamat datang di Idmaret, selamat berbelanja!"),
            Scene("intro:salsa", "cerita_kampus", "naya", "Narator|Di plaza kampus, seorang mahasiswi duduk memeluk kardus berisi nota-nota yang menyembul tidak beraturan.", "Salsa|Eh—maaf, aku nggak lihat ada orang. Aku Salsa, bendahara himpunan. Kardus ini? Ini… PR-ku yang paling menakutkan."),
            Scene("intro:ilham", "cerita_kampus", "raka", "Narator|Beberapa mahasiswa berkerumun di depan mading. Seorang di antaranya menunjuk-nunjuk selembar kertas bertulisan tangan.", "Ilham|Baca sendiri! Sudah tiga minggu, dan uang iuran kita tidak jelas ke mana!"),
            Scene("intro:gilang", "cerita_kafe", "dimas", "Narator|Kafe Senja tutup di jam yang biasanya ramai. Seorang pemuda bercelemek duduk di trotoar, menghitung uang receh di telapak tangannya.", "Gilang|Oh, maaf, kafenya lagi tutup, Mas. Aku? Aku kerja di sini. Harusnya sih… masih."),
        }.ToDictionary(s => s.Id.Substring("intro:".Length));

        /// <summary>Adegan saat pertama bertemu NPC side quest (kunci = id NPC tanpa "npc:").</summary>
        public static StoryScene NpcIntro(string npcId) =>
            npcId != null && NpcIntros.TryGetValue(npcId.Replace("npc:", ""), out var scene) ? scene : null;

        static readonly Dictionary<string, StoryScene> MainIntros = new[]
        {
            // Bu Siti tidak punya adegan "pertama bertemu": ia sudah menyambut di pintu lewat
            // AreaIntro "Dalam Warung Bu Siti", jadi papan menu langsung membuka tugasnya.
            Scene("main:Raka", "warung_depan", "raka", "Narator|Seorang pemuda berbatik merah bata berdiri di pinggir gang sambil memegang struk dengan wajah kesal.", "Raka|Kamu lihat sendiri kan? Struknya beda sama yang aku pesan!"),
            Scene("main:Dimas", "kos_kamar", "dimas", "Narator|Kamar kos lantai dua. Dimas duduk memeluk lutut, layar ponselnya penuh iklan pinjaman cepat cair.", "Dimas|Alif? Maaf kamarnya berantakan… pikiranku juga."),
            Scene("main:Ustadz Farid", "kos_halaman", "pak_ustad", "Narator|Seorang ustadz berpeci hitam duduk di teras sambil memutar tasbih, menyambut siapa saja yang ingin bertanya.", "Ustadz Farid|Duduklah. Pertanyaan yang baik adalah awal dari keputusan yang baik."),
            Scene("main:Naya", "warung_depan", "naya", "Narator|Di tengah keramaian bazar, Naya mencatat keluhan pembeli di tabletnya dengan cepat dan teliti.", "Naya|Alif! Pas sekali. Aku butuh orang yang bisa melihat fakta tanpa buru-buru menyalahkan."),
            Scene("main:Penjual", "warung_depan", "raka", "Narator|Penjual radio berdiri kaku di lapaknya, tangannya menutupi stiker di casing radio.", "Penjual|Kalian dari mana? Barang saya semua bagus, kok."),
        }.ToDictionary(s => s.Id.Substring("main:".Length));

        static readonly Dictionary<string, StoryScene> AreaIntros = new[]
        {
            Scene("area:Dalam Warung Bu Siti", "warung_dalam", "bu_siti",
                "[art=warung_dapur tokoh=-] Narator|Aroma nasi hangat dan sambal goreng menyambut dari balik pintu. Kipas angin berputar pelan di atas meja-meja kayu yang sudah terisi separuh.",
                "[art=warung_sambut] Bu Siti|Mari, Nak, duduk saja di mana suka. Papan menunya di sebelah sana — harganya sudah ditulis semua, jadi tidak ada kejutan.",
                "Alif (Batin)|Tempat pertama di kota ini yang terasa seperti rumah. Lihat papan menunya dulu, baru pesan."),
        }.ToDictionary(s => s.Id.Substring("area:".Length));

        /// <summary>Adegan penutup demo, diputar sebelum layar kredit.</summary>
        public static readonly StoryScene DemoOutro = Scene(DemoStage.SceneId, "kota:J2K_KamarAlif", "dimas",
            "Narator|Pagi kedua. Kamar kos yang semalam masih asing kini sudah punya bau kopi dan suara pasar di luar jendela.",
            "Alif (Batin)|Satu hari, dan aku sudah belajar tiga hal: baca dulu sebelum tanda tangan, catat setiap kesepakatan, dan tanya sebelum membayar.",
            "Narator|Di luar, Cempaka baru mulai bergerak — Dimas dengan tawaran pinjamannya, Kirana dengan kantong konsernya, warga lain dengan persoalannya masing-masing.",
            "Narator|Tetapi perjalanan Alif berhenti di sini dulu. Versi demo berakhir; ceritanya berlanjut di Bab 2 — Jebakan Riba.");

        static readonly Dictionary<string, StoryScene> TaskScenes = new[]
        {
            // Makanan datang dulu; Raka baru masuk setelah Alif selesai makan (lihat TaskOutros).
            Scene("task:c1.meal", "warung_dalam", "bu_siti",
                "[art=warung_antar tokoh=-] Narator|Tidak sampai sepuluh menit, Bu Siti datang membawa nampan: nasi telur yang masih mengepul dan segelas es teh berembun.",
                "Bu Siti|Ini pesanannya, Nak. Nasi telur satu, es teh satu. Dimakan pelan-pelan saja, warung belum ramai."),

            Scene("task:c1.offer", "warung_dalam", "bu_siti",
                "Narator|Raka pergi setelah meninggalkan map plastiknya di meja kasir. Bu Siti menatap map itu lama sekali.",
                "Bu Siti|Dapur saya perlu diperbaiki, Nak. Tawarannya cepat sekali cair… tapi dadanya kok tidak enak, ya.",
                "Alif|Boleh saya bantu baca, Bu? Kita pisahkan mana yang wajar dan mana yang jadi tanda bahaya."),
        }.ToDictionary(s => s.Id.Substring("task:".Length));

        // Akibat penilaian Alif atas tawaran Raka (papan bebas "c1.offer"), diputar pagi berikutnya.
        // Indeks = ActivityBoard.Tier: 0 Bu Siti menandatangani, 1 menggantung, 2 menolak.
        static readonly StoryScene[] OfferAftermaths =
        {
            Scene("aftermath:c1.offer.signed", "warung_raka", null,
                "Narator|Pagi berikutnya, pesan dari Bu Siti masuk ke HP Alif. Raka sudah berdiri di depan kasir sejak warung dibuka.",
                "Raka|Bunga hari pertama, Bu. Sepuluh persen dari dua juta — dua ratus ribu. Besok segitu lagi, ya.",
                "Bu Siti|Baru semalam uangnya cair, Nak, hari ini sudah ditagih. KTP saya juga masih dia pegang…",
                "Alif (Batin)|Aku yang bilang syaratnya wajar. Bunga harian, KTP ditahan, surat yang tak boleh dibaca — semuanya tanda bahaya, dan aku melewatkannya. Ini harus kubantu bereskan."),
            Scene("aftermath:c1.offer.pending", "warung_raka", null,
                "Narator|Pagi berikutnya, pesan dari Bu Siti masuk ke HP Alif. Raka sudah kembali ke warung sejak subuh.",
                "Raka|Kemarin katanya mau dipikir dulu, Bu. Sudah semalam, kan? Promo cair cepatnya cuma sampai hari ini.",
                "Bu Siti|Saya masih bingung, Nak. Kemarin sebagian syaratnya kamu bilang wajar… yang mana yang sebenarnya berbahaya?",
                "Alif (Batin)|Aku menilai setengah-setengah, dan sekarang Bu Siti yang menanggung ragunya. Bunga harian, KTP ditahan, surat yang tak boleh dibaca — tiga-tiganya tanda bahaya. Hanya waktu untuk membaca yang wajar."),
            Scene("aftermath:c1.offer.refused", "warung_sambut", null,
                "Narator|Pagi berikutnya, pesan dari Bu Siti masuk ke HP Alif lebih dulu daripada sarapan.",
                "Bu Siti|Raka datang lagi subuh tadi, Nak. Saya bilang: tidak ada tanda tangan tanpa surat yang boleh dibaca. Dia pergi sambil menggerutu.",
                "Bu Siti|Dapurnya tetap perlu diperbaiki. Tapi saya mau cari jalan yang akadnya jelas — pelan-pelan tidak apa-apa.",
                "Alif (Batin)|Satu tanda tangan yang ditahan, satu jerat yang batal. Bunga sepuluh persen sehari bukan pertolongan."),
        };

        /// <summary>Adegan pagi setelah tawaran Raka dinilai (tier dari AdventureState.BoardTier); null bila belum dinilai.</summary>
        public static StoryScene OfferAftermath(int tier) => tier >= 0 && tier < OfferAftermaths.Length ? OfferAftermaths[tier] : null;

        static readonly string[] DemoOutroByOffer =
        {
            "Narator|Di warung, Bu Siti menghitung ulang uang kasnya: dua ratus ribu untuk bunga hari ini, dan besok segitu lagi.",
            "Narator|Di laci kasir warung, map plastik biru itu masih menunggu jawaban — dan Raka berjanji kembali sore ini.",
            "Narator|Di warung, Bu Siti menempel kertas kecil di dekat kasir: \"Tidak ada tanda tangan tanpa dibaca.\"",
        };

        /// <summary>Penutup demo, dengan satu baris tambahan sesuai nasib tawaran Raka (tier &lt; 0 = tanpa tambahan).</summary>
        public static StoryScene DemoOutroFor(int offerTier, int debt = 0)
        {
            bool offer = offerTier >= 0 && offerTier < DemoOutroByOffer.Length;
            if (!offer && debt <= 0) return DemoOutro;
            var lines = DemoOutro.Lines.ToList();
            if (offer) lines.Insert(lines.Count - 1, DemoOutroByOffer[offerTier]);
            if (debt > 0) lines.Insert(lines.Count - 1, $"Alif (Batin)|Dan di HP-ku, notifikasi {PinjolRules.AppName} berkedip lagi: utangku sudah Rp{debt:N0}. Meminjam itu cepat — melunasinya yang tidak.");
            return new StoryScene { Id = DemoOutro.Id, Art = DemoOutro.Art, Character = DemoOutro.Character, Lines = lines.ToArray() };
        }

        static readonly Dictionary<string, StoryScene> TaskOutros = new[]
        {
            Scene("after:c1.meal", "warung_dalam", null,
                "Narator|— sepuluh menit kemudian —",
                "[art=warung_raka] Narator|Piring Alif tinggal sisa kerak nasi ketika pintu warung terbuka keras. Seorang pemuda berbatik merah bata masuk, menaruh map plastik di meja kasir tanpa dipersilakan.",
                "Raka|Bu, saya bawa yang kemarin. Modal cair hari ini juga, tidak pakai ribet. Bunganya kecil kok, sepuluh persen — harian.",
                "Bu Siti|Nanti dulu, Mas. Saya belum baca apa-apa…",
                "Raka|Nggak usah dibaca, Bu. Orang lain juga langsung tanda tangan. KTP-nya saya pegang dulu buat jaminan, ya.",
                "Alif (Batin)|Sepuluh persen sehari. KTP ditahan. Tanpa surat yang boleh dibaca. Ini bukan bantuan — ini jerat.",
                "Alif (Batin)|Aku bayar dulu makananku, baru urus ini. Pesananku dua item, Rp18.000. Angka itu kuingat baik-baik."),
        }.ToDictionary(s => s.Id.Substring("after:".Length));

        /// <summary>Adegan sesudah satu tugas selesai (kunci = id tugasnya), diputar sekali sebelum
        /// kalimat penutup tugas — mis. Raka menerobos masuk setelah Alif selesai makan.</summary>
        public static StoryScene TaskOutro(string taskId) =>
            taskId != null && TaskOutros.TryGetValue(taskId, out var scene) ? scene : null;

        /// <summary>Adegan pembuka satu tugas (kunci = id tugasnya), diputar sekali sebelum
        /// dialog tugas itu dimulai — mis. Bu Siti mengantar makanan sebelum Alif makan.</summary>
        public static StoryScene TaskScene(string taskId) =>
            taskId != null && TaskScenes.TryGetValue(taskId, out var scene) ? scene : null;

        /// <summary>Adegan saat pertama kali memasuki sebuah area (kunci = nama areanya).</summary>
        public static StoryScene AreaIntro(string area) =>
            area != null && AreaIntros.TryGetValue(area, out var scene) ? scene : null;

        /// <summary>Adegan saat pertama bertemu tokoh main quest (kunci = nama pembicara tugas).</summary>
        public static StoryScene MainIntro(string speaker) =>
            speaker != null && MainIntros.TryGetValue(speaker, out var scene) ? scene : null;

        static readonly string[] ChapterArt = { "panel_tiba", "kos_kamar", "kos_halaman", "warung_depan", "stasiun_depan" };

        /// <summary>Adegan pembuka bab: judul lalu pengantar cerita.</summary>
        public static StoryScene ChapterIntro(AdventureChapter chapter) => new StoryScene
        {
            Id = "chapter:" + chapter.Number,
            Art = ChapterArt[Math.Max(0, Math.Min(ChapterArt.Length - 1, chapter.Number - 1))],
            Lines = new[] { $"Narator|Bab {chapter.Number} — {chapter.Title}. {chapter.Subtitle}.", "Narator|" + chapter.Introduction },
        };

        static StoryScene Scene(string id, string art, string character, params string[] lines) =>
            new StoryScene { Id = id, Art = art, Character = character, Lines = lines };

        public static IEnumerable<StoryScene> AllScenes =>
            NpcIntros.Values.Concat(MainIntros.Values).Concat(AreaIntros.Values).Concat(TaskScenes.Values).Concat(TaskOutros.Values).Concat(OfferAftermaths);
    }
}
