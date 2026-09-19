using System;
using System.Linq;

namespace Alif.Adventure
{
    /// <summary>
    /// Konten side quest dari interview narasumber (lihat Docs/QUEST_DESIGN.md):
    /// rangkaian K-pop "Kantong Konser" (Kirana & Tara) dan rangkaian dosen "Amanah yang
    /// Tertunda" (Pak Fadli & Bima). Klaim syariah perlu ditinjau narasumber sebelum rilis.
    /// </summary>
    public static class SideQuestContent
    {
        public const string Kirana = "npc:kirana", Tara = "npc:tara", Fadli = "npc:fadli", Bima = "npc:bima", Bank = "npc:bank";

        public const string SkincareSet = "Set Skincare Cadangan", SavingsBrochure = "Brosur Tabungan Syariah",
            PriceReceipt = "Struk Perbandingan Harga", SparePhotocard = "Photocard Duplikat",
            InstallmentLetter = "Surat Kesepakatan Cicilan", Chronology = "Kronologi Tertulis";

        public static readonly QuestNpc[] Npcs =
        {
            new QuestNpc { Id = Kirana, Name = "Kirana", Placeholder = "naya", Idle = "Kirana|NOVA comeback bulan depan! Aku lagi semangat nabung nih." },
            new QuestNpc { Id = Tara, Name = "Tara", Placeholder = "bu_siti", Idle = "Tara|Lightstick sudah di tangan, tinggal jaga kantong tabungan." },
            new QuestNpc { Id = Fadli, Name = "Pak Fadli", Placeholder = "raka", Idle = "Pak Fadli|Mengajar itu amanah. Meminjamkan uang juga amanah, ternyata." },
            new QuestNpc { Id = Bima, Name = "Bima", Placeholder = "dimas", Idle = "Bima|Sedikit demi sedikit, cicilanku jalan terus." },
            new QuestNpc { Id = Bank, Name = "Petugas Bank Syariah", Placeholder = "naya", Idle = "Petugas Bank Syariah|Ada yang bisa kami bantu? Jangan pernah bagikan kode OTP, ya." },
        };

        static ActivityBoard Inspect(string instruction, string[] cards, string[] notes) => new ActivityBoard
        {
            Kind = "inspect", Instruction = instruction, Cards = cards, Slots = new[] { "Catat temuan" },
            Answers = new int[cards.Length], Notes = notes,
        };

        static ActivityBoard Sort(string kind, string instruction, string[] cards, string[] slots, int[] answers, string[] notes) => new ActivityBoard
        {
            Kind = kind, Instruction = instruction, Cards = cards, Slots = slots, Answers = answers, Notes = notes,
        };

        static QuestStep Talk(string target, string objective, string[] lines, string[] outcome = null) =>
            new QuestStep { Kind = "talk", Target = target, Objective = objective, Lines = lines, Outcome = outcome ?? Array.Empty<string>() };

        static QuestStep Give(string target, string objective, string item, string[] lines, string[] outcome = null) =>
            new QuestStep { Kind = "give", Target = target, Objective = objective, RequiredItem = item, Lines = lines, Outcome = outcome ?? Array.Empty<string>() };

        static QuestStep Board(string target, string objective, ActivityBoard board, string[] outcome) =>
            new QuestStep { Kind = board.Kind, Target = target, Objective = objective, Board = board, Outcome = outcome };

        public static readonly SideQuest[] All =
        {
            // ─────────────── K-POP · Kantong Konser ───────────────
            new SideQuest
            {
                Id = "kpop.skincare", Title = "Empat Set demi Photocard", Theme = "Israf & tabdzir",
                Summary = "Kirana membeli empat set skincare demi mengoleksi semua photocard.",
                Steps = new[]
                {
                    Talk(Kirana, "Sapa Kirana", new[]
                    {
                        "Kirana|Aku barusan beli 4 set skincare ini biar dapet semua photocard NOVA! Jujur ga bisa milih, empat member cantik semua jadi aku beliii dehh.",
                        "Alif|Tapi produknya bakal kepakai semua nggak? Satu set aja bisa 6–7 bulan baru habis loh… dan kamu beli empat.",
                        "Kirana|…nggak juga sih. Duh, takut kedaluwarsa nanti. Tapi sayang, photocard-nya eksklusif.",
                    }),
                    Board(Kirana, "Periksa kotak skincare Kirana", Inspect("Klik setiap bagian kotak untuk mencatat temuan.",
                        new[] { "Tanggal kedaluwarsa", "Lama pemakaian satu set", "Jumlah set yang dibeli", "Hitung pemakaian setahun" },
                        new[]
                        {
                            "Tertulis: baik digunakan 12 bulan sejak diproduksi.",
                            "Satu set habis dalam 6–7 bulan pemakaian rutin.",
                            "Empat set = 24–28 bulan pemakaian.",
                            "Dalam 12 bulan hanya ±2 set terpakai; 2 set berisiko kedaluwarsa dan terbuang.",
                        }),
                        new[]
                        {
                            "Alif|Membeli sampai barangnya terbuang itu contoh israf—berlebihan—dan tabdzir, menyia-nyiakan. \"Makan dan minumlah, tapi jangan berlebihan\" (QS Al-A'raf: 31).",
                            "Kirana|Iya juga ya… terus dua set lebihnya gimana?",
                            "Alif|Berikan ke yang benar-benar memakainya. Temanmu Tara lagi cari skincare, kan?",
                            "Kirana|Ide bagus! Tolong antarkan satu ke Tara, ya. Satu lagi buat Mama.",
                        }).With(reward: SkincareSet, sharia: 4, bond: 1),
                    Give(Tara, "Berikan set skincare ke Tara", SkincareSet,
                        new[] { "Tara|Eh, ini dari Kirana? Pas banget, punyaku habis minggu lalu!" },
                        new[] { "Tara|Bilang makasih ke Kirana ya. Daripada kedaluwarsa, mending dipakai." }).With(bond: 1),
                    Talk(Kirana, "Kabari Kirana", new[]
                    {
                        "Alif|Set skincare-nya sudah sampai ke Tara.",
                        "Kirana|Makasih, Alif! Lain kali aku pilih satu set dulu—photocard lain bisa tukeran sama teman.",
                    }).With(bond: 1),
                },
            },
            new SideQuest
            {
                Id = "kpop.pockets", Title = "Kantong-Kantong Tabungan", Theme = "Tabungan syariah",
                Summary = "Kirana menabung untuk konser dan ingin tahu tabungan tanpa bunga.",
                Requires = new[] { "kpop.skincare" },
                Steps = new[]
                {
                    Talk(Kirana, "Dengarkan rencana konser Kirana", new[]
                    {
                        "Kirana|Konser NOVA tiga bulan lagi! Aku nabung di aplikasi bank yang bisa dipecah jadi kantong-kantong. Tapi… bunganya halal nggak ya?",
                        "Alif|Coba kita tanya petugas bank syariah dulu.",
                    }),
                    Talk(Bank, "Tanya petugas bank syariah", new[]
                    {
                        "Petugas Bank Syariah|Tabungan syariah tidak memakai bunga. Ada akad wadiah—titipan tanpa bunga—dan mudharabah, bagi hasil sesuai nisbah, bukan angka tetap yang dijanjikan.",
                        "Petugas Bank Syariah|Fiturnya tetap bisa dipecah menjadi beberapa kantong tujuan. Ini brosurnya.",
                    }).With(reward: SavingsBrochure, logic: 3),
                    Give(Kirana, "Berikan brosur ke Kirana", SavingsBrochure,
                        new[] { "Kirana|Wah, ternyata ada kantong tanpa bunga! Bantu aku bagi tabungannya, ya." }),
                    Board(Kirana, "Bagi tabungan konser ke kantong", new ActivityBoard
                    {
                        Kind = "budget",
                        Instruction = "Tabungan 3 bulan: Rp2.100.000. Isi kantong tiket, transport, dan lightstick tanpa menyentuh uang sekolah.",
                        Cards = new[] { "Kantong tiket konser", "Kantong transport", "Kantong lightstick", "Ambil dari uang SPP", "Photocard tambahan" },
                        Costs = new[] { 1200000, 150000, 750000, 500000, 300000 },
                        Groups = new[] { 0, 1, 2, -1, -1 },
                        Limit = 2100000,
                        Slots = new[] { "Tidak dipilih", "Dipilih" },
                        Notes = new[]
                        {
                            "Tiga kantong pas Rp2.100.000: konser terbiayai tanpa utang.",
                            "Uang SPP adalah kebutuhan sekolah dan tidak dipakai untuk hiburan.",
                        },
                    }, new[]
                    {
                        "Alif|Semua kantong terisi dari tabungan sendiri—tanpa pinjaman, tanpa bunga.",
                        "Kirana|Kantong-kantongnya lucu banget. Aku jadi semangat nabung!",
                    }).With(logic: 5, bond: 1),
                },
            },
            new SideQuest
            {
                Id = "kpop.checkout", Title = "Checkout Lightstick", Theme = "Riba, paylater & calo",
                Summary = "Tara membandingkan cara membeli lightstick dan tiket konser.",
                Requires = new[] { "kpop.pockets" },
                Steps = new[]
                {
                    Talk(Tara, "Temui Tara", new[]
                    {
                        "Tara|Alif, lightstick NOVA ada di dua aplikasi. Yang satu bisa paylater, yang satu terima koin belanja. Terus ada calo tiket, harganya dua kali lipat…",
                        "Alif|Kita pilah dulu satu per satu.",
                    }),
                    Board(Tara, "Nilai cara bayar & beli", Sort("match", "Pilih cara di kiri, lalu letakkan pada penilaiannya di kanan.",
                        new[] { "Debit dari kantong lightstick", "Koin hasil belanja sebelumnya", "Kartu kredit dicicil berbunga", "Paylater berbunga & denda telat", "Tiket calo 2× harga hasil memborong", "Bandingkan harga dua aplikasi dulu" },
                        new[] { "Aman", "Mengandung riba", "Hati-hati: ihtikar & gharar", "Kebiasaan bijak" },
                        new[] { 0, 0, 1, 1, 2, 3 },
                        new[]
                        {
                            "Bayar dari tabungan sendiri: jelas dan tanpa utang.",
                            "Koin adalah diskon/hadiah dari penjual, bukan pinjaman.",
                            "Tambahan bunga atas utang termasuk riba.",
                            "Bunga dan denda keterlambatan atas pinjaman termasuk riba.",
                            "Memborong lalu menjual jauh lebih mahal mirip ihtikar (menimbun); tiket palsu juga mengandung gharar.",
                            "Membandingkan harga membantu memilih kebutuhan, bukan keinginan sesaat.",
                        }),
                        new[]
                        {
                            "Tara|Oke, aku batal paylater. Beli dari kantong tabungan aja, di aplikasi yang lebih murah.",
                            "Tara|Ini struk perbandingannya. Tolong tunjukkan ke Kirana, biar dia nggak beli ke calo.",
                        }).With(reward: PriceReceipt, logic: 3, sharia: 3),
                    Give(Kirana, "Tunjukkan struk ke Kirana", PriceReceipt,
                        new[] { "Kirana|Selisihnya lumayan! Sisanya bisa buat ongkos pulang." },
                        new[] { "Kirana|Tiketnya aku beli di penjual resmi aja deh, nggak jadi ke calo." }).With(bond: 1),
                },
            },
            new SideQuest
            {
                Id = "kpop.fansign", Title = "Tiket Undian Fansign", Theme = "Maysir vs promosi",
                Summary = "Tara tergoda membeli banyak album demi undian fansign.",
                Requires = new[] { "kpop.checkout" },
                Steps = new[]
                {
                    Talk(Tara, "Dengarkan rencana Tara", new[]
                    {
                        "Tara|Ada undian fansign NOVA. Makin banyak album yang dibeli, makin besar peluangnya. Aku kepikiran beli sepuluh…",
                        "Tara|Terus ada HP baru yang hadiahnya undian ketemu idol.",
                        "Alif|Yuk, kita bedakan mana promosi, mana yang mirip taruhan.",
                    }),
                    Board(Tara, "Pilah promosi dan undian", Sort("sort", "Pilih situasi di kiri, lalu letakkan pada kelompoknya di kanan.",
                        new[] { "Beli satu album yang memang dikoleksi, dapat kupon undian gratis", "Beli sepuluh album hanya demi memperbesar peluang", "Beli HP karena HP lama rusak, kebetulan ada undian", "Ganti HP yang masih bagus hanya demi undian" },
                        new[] { "Umumnya boleh (hadiah promosi)", "Mendekati maysir & israf", "Keputusan kebutuhan" },
                        new[] { 0, 1, 2, 1 },
                        new[]
                        {
                            "Hadiah promosi tanpa biaya tambahan umumnya dibolehkan, selama barangnya memang dibutuhkan.",
                            "Membeli barang yang tak dipakai demi peluang menang mendekati maysir dan israf.",
                            "Membeli karena butuh; undian hanyalah bonus.",
                            "Mengganti barang yang masih baik demi peluang hadiah termasuk berlebihan.",
                        }),
                        new[]
                        {
                            "Tara|Satu album cukup deh. Kalau mau photocard lain, aku tukeran sama teman aja.",
                            "Tara|Nih, photocard dobelku. Kasih ke Kirana, dia belum punya yang ini.",
                        }).With(reward: SparePhotocard, sharia: 4),
                    Give(Kirana, "Berikan photocard ke Kirana", SparePhotocard,
                        new[] { "Kirana|HAH! Ini photocard yang aku cari! Dari Tara?" },
                        new[]
                        {
                            "Kirana|Tukeran begini lebih seru daripada beli sampai numpuk.",
                            "Kirana|Makasih, Alif. Sampai ketemu di konser—bawa lightstick, tanpa utang!",
                        }).With(bond: 1),
                },
            },

            // ─────────────── DOSEN · Amanah yang Tertunda ───────────────
            new SideQuest
            {
                Id = "dosen.loan", Title = "Pinjaman Sahabat", Theme = "Qardh & mencatat utang",
                Summary = "Pinjaman Pak Fadli kepada sahabat SMA-nya, Bima, tak kunjung lunas.",
                Steps = new[]
                {
                    Talk(Fadli, "Dengarkan Pak Fadli", new[]
                    {
                        "Pak Fadli|Orang kira gaji dosen kampus swasta itu fantastis. Makanya teman SMA saya, Bima, beberapa kali pinjam uang.",
                        "Pak Fadli|Dulu dia selalu bayar tepat waktu. Tapi pinjaman terakhir sudah berbulan-bulan belum lunas… dan dia mulai sulit dihubungi.",
                        "Alif|Boleh saya lihat catatan pinjamannya, Pak?",
                    }),
                    Board(Fadli, "Periksa catatan pinjaman", Inspect("Klik setiap bagian catatan untuk melihat apa yang tertulis.",
                        new[] { "Chat \"Pinjam dulu ya, Fad\"", "Jumlah pinjaman", "Tanggal jatuh tempo", "Saksi / tanda terima" },
                        new[]
                        {
                            "Hanya ada chat singkat tanpa rincian.",
                            "Jumlah tidak tertulis; Pak Fadli mengingat Rp5.000.000 dari mutasi rekening.",
                            "Tidak pernah disepakati kapan harus lunas.",
                            "Tidak ada saksi atau tanda terima.",
                        }),
                        new[]
                        {
                            "Alif|Pinjaman yang tidak dicatat mudah jadi salah paham. Al-Qur'an menganjurkan utang ditulis—jumlah dan tenggatnya—dan disaksikan (QS Al-Baqarah: 282).",
                            "Pak Fadli|Benar juga. Tolong temui Bima dulu, ya. Saya tidak mau persahabatan kami rusak.",
                        }).With(logic: 3),
                    Talk(Bima, "Temui Bima", new[]
                    {
                        "Bima|Alif… kamu disuruh Fadli, ya?",
                        "Bima|Aku bukan mau kabur. Aku malu. Uangku habis karena… nanti aku cerita. Sekarang aku cuma bisa bayar sedikit-sedikit.",
                        "Alif|Kalau begitu, kita cari jalan yang adil buat kalian berdua.",
                    }),
                    Board(Bima, "Susun penyelesaian yang adil", Sort("sort", "Pilih usulan di kiri, lalu kelompokkan di kanan.",
                        new[] { "Jadwal cicilan tertulis sesuai kemampuan Bima", "Memberi tangguh karena Bima benar-benar kesulitan", "Merelakan sebagian sebagai sedekah (pilihan Pak Fadli)", "Menambah denda bunga karena telat", "Mengumumkan utang Bima di grup alumni", "Pura-pura tidak mampu padahal mampu" },
                        new[] { "Penyelesaian yang adil", "Tidak adil / dilarang" },
                        new[] { 0, 0, 0, 1, 1, 1 },
                        new[]
                        {
                            "Cicilan tertulis membuat kewajiban jelas dan bisa dipenuhi.",
                            "Pemberi pinjaman dianjurkan memberi tangguh kepada yang kesulitan (QS Al-Baqarah: 280).",
                            "Merelakan sebagian utang orang yang kesulitan bernilai sedekah, tetapi tidak wajib.",
                            "Tambahan atas pinjaman yang disyaratkan termasuk riba.",
                            "Membuka aib orang lain merusak kehormatan dan persahabatan.",
                            "Menunda pembayaran padahal mampu adalah kezaliman.",
                        }),
                        new[] { "Bima|Cicilan lima ratus ribu per bulan, aku sanggup. Tolong tulis jadi surat kesepakatan." }).With(reward: InstallmentLetter, sharia: 4),
                    Give(Fadli, "Serahkan surat ke Pak Fadli", InstallmentLetter,
                        new[] { "Pak Fadli|Jumlah, cicilan, dan tanggalnya jelas. Saya setuju—tanpa tambahan apa pun." },
                        new[] { "Pak Fadli|Terima kasih, Alif. Yang saya takutkan bukan uangnya, tapi kehilangan sahabat." }).With(bond: 1),
                },
            },
            new SideQuest
            {
                Id = "dosen.phishing", Title = "Tautan Undangan", Theme = "Phishing & menjaga harta",
                Summary = "Bima ternyata korban phishing lewat file undangan palsu.",
                Requires = new[] { "dosen.loan" },
                Steps = new[]
                {
                    Talk(Bima, "Dengarkan cerita Bima", new[]
                    {
                        "Bima|Aku janji cerita, kan. Bulan lalu ada file \"undangan pernikahan\" di WhatsApp. Aku buka, aku pasang…",
                        "Bima|Besoknya saldo tabunganku habis. Karena malu, aku pinjam sana-sini buat menutupnya—termasuk ke Fadli.",
                        "Alif|Boleh aku lihat HP-mu? Kita cek apa yang terjadi.",
                    }),
                    Board(Bima, "Periksa HP Bima", Inspect("Klik setiap bagian HP untuk mencatat temuan.",
                        new[] { "File \"Undangan Pernikahan.apk\"", "SMS berisi kode OTP", "Notifikasi transfer yang tidak dilakukan", "Kontak \"CS Bank\" lewat chat" },
                        new[]
                        {
                            "Undangan asli tidak berbentuk aplikasi .apk; file ini membuka akses ke HP.",
                            "OTP adalah kunci transaksi dan tidak boleh dibagikan kepada siapa pun.",
                            "Ada transfer yang tidak dilakukan Bima: tanda rekening diambil alih.",
                            "Bank tidak meminta data lewat chat pribadi; nomor ini palsu.",
                        }),
                        new[] { "Alif|Ini phishing: penipu memancing lewat file dan tautan palsu untuk mengambil data dan uang. Kita harus bergerak cepat." }).With(logic: 3),
                    Board(Bima, "Susun langkah darurat", Sort("flow", "Pilih tindakan di kiri, lalu letakkan pada urutan langkahnya.",
                        new[] { "Hubungi call center resmi & blokir rekening", "Putuskan internet, hapus aplikasi mencurigakan", "Tulis kronologi & simpan bukti", "Ganti PIN & kata sandi, jangan bagikan OTP", "Lapor ke bank & kanal resmi (OJK 157)", "Kabari kontak bahwa nomornya disalahgunakan" },
                        new[] { "Langkah 1", "Langkah 2", "Langkah 3", "Langkah 4", "Langkah 5", "Langkah 6" },
                        new[] { 1, 0, 3, 2, 4, 5 },
                        new[]
                        {
                            "Blokir lewat nomor di kartu atau aplikasi resmi, bukan nomor dari chat.",
                            "Memutus internet menghentikan akses aplikasi jahat.",
                            "Kronologi dan bukti (tangkapan layar, mutasi) diperlukan bank.",
                            "PIN dan kata sandi baru menutup akses penipu.",
                            "Laporkan ke bank, lalu ke kanal resmi seperti Kontak OJK 157.",
                            "Kontak perlu tahu agar tidak ikut tertipu.",
                        }),
                        new[] { "Bima|Rekening sudah diblokir, semua kata sandi sudah diganti. Ini kronologinya." }).With(reward: Chronology, logic: 4),
                    Give(Bank, "Serahkan kronologi ke petugas bank", Chronology,
                        new[] { "Petugas Bank Syariah|Terima kasih. Kami proses laporannya dan periksa transaksi yang tidak sah. Hasilnya tidak selalu bisa dijamin, tetapi laporan yang cepat sangat membantu." },
                        new[] { "Petugas Bank Syariah|Menjaga harta termasuk tujuan syariat. Ikhtiar sudah dilakukan—sisanya bersabar, dan jangan menutup kerugian dengan utang berbunga." }).With(sharia: 3),
                    Talk(Bima, "Kabari Bima", new[]
                    {
                        "Alif|Laporanmu sudah diterima bank.",
                        "Bima|Aku lega. Cicilan pertama ke Fadli sudah aku transfer, sesuai surat.",
                        "Alif|Mengambil harta orang dengan cara batil itu dilarang (QS An-Nisa': 29). Kamu korban—yang penting sekarang hartamu dijaga dan utangmu diselesaikan dengan jujur.",
                    }).With(bond: 1),
                },
            },
        };

        public static SideQuest Find(string id) => All.FirstOrDefault(q => q.Id == id);

        public static QuestNpc Npc(string id) => Npcs.FirstOrDefault(n => n.Id == id);

        static QuestStep With(this QuestStep step, string reward = null, float logic = 0, float sharia = 0, int bond = 0)
        {
            step.RewardItem = reward ?? step.RewardItem;
            step.Logic = logic;
            step.Sharia = sharia;
            step.Bond = bond;
            return step;
        }
    }
}
