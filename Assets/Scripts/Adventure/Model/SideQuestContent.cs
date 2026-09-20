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
        public const string Arga = "npc:arga", Ratna = "npc:ratna", Dewi = "npc:dewi", Harun = "npc:harun", Nadia = "npc:nadia",
            Laras = "npc:laras", Darto = "npc:darto", Sinta = "npc:sinta", Hendra = "npc:hendra", Yusuf = "npc:yusuf";
        public const string Ningsih = "npc:ningsih", Fira = "npc:fira", Bayu = "npc:bayu";

        public const string SkincareSet = "Set Skincare Cadangan", SavingsBrochure = "Brosur Tabungan Syariah",
            PriceReceipt = "Struk Perbandingan Harga", SparePhotocard = "Photocard Duplikat",
            InstallmentLetter = "Surat Kesepakatan Cicilan", Chronology = "Kronologi Tertulis";

        public static readonly QuestNpc[] Npcs = new[]
        {
            new QuestNpc { Id = Kirana, Name = "Kirana", Placeholder = "naya", Idle = "Kirana|NOVA comeback bulan depan! Aku lagi semangat nabung nih." },
            new QuestNpc { Id = Tara, Name = "Tara", Placeholder = "bu_siti", Idle = "Tara|Lightstick sudah di tangan, tinggal jaga kantong tabungan." },
            new QuestNpc { Id = Fadli, Name = "Pak Fadli", Placeholder = "raka", Idle = "Pak Fadli|Mengajar itu amanah. Meminjamkan uang juga amanah, ternyata." },
            new QuestNpc { Id = Bima, Name = "Bima", Placeholder = "dimas", Idle = "Bima|Sedikit demi sedikit, cicilanku jalan terus." },
            new QuestNpc { Id = Bank, Name = "Petugas Bank Syariah", Placeholder = "naya", Tint = "d6ffe0", Idle = "Petugas Bank Syariah|Ada yang bisa kami bantu? Jangan pernah bagikan kode OTP, ya." },
            // Rangkaian investasi: satu tokoh satu jenis investasi, hadir satu per satu.
            new QuestNpc { Id = Arga, Name = "Mas Arga", Placeholder = "raka", Tint = "dde6ff", AppearsWithQuest = true, Idle = "Mas Arga|Pasar lagi merah? Tarik napas, seduh kopi, cek lagi alasanmu berinvestasi." },
            new QuestNpc { Id = Ratna, Name = "Bu Ratna", Placeholder = "bu_siti", Tint = "eadcff", AppearsWithQuest = true, Idle = "Bu Ratna|Pelan-pelan asal pasti, Nak. Syal ini juga jadinya dari satu rajutan ke rajutan." },
            new QuestNpc { Id = Dewi, Name = "Dewi", Placeholder = "naya", Tint = "fff0b8", AppearsWithQuest = true, Idle = "Dewi|Klub investasi kumpul tiap Jumat. Kakak boleh ikut, gratis kok!" },
            new QuestNpc { Id = Harun, Name = "Pak Harun", Placeholder = "pak_ustad", Tint = "fff2d6", AppearsWithQuest = true, Idle = "Pak Harun|Simpan surat emasnya baik-baik. Bukti kepemilikan itu sama berharganya." },
            new QuestNpc { Id = Nadia, Name = "Kak Nadia", Placeholder = "naya", Tint = "cfe6ff", AppearsWithQuest = true, Idle = "Kak Nadia|Jangan lupa follow… eh, jangan lupa diversifikasi maksudku!" },
            new QuestNpc { Id = Laras, Name = "Bu Laras", Placeholder = "bu_siti", Tint = "d9f5d0", AppearsWithQuest = true, Idle = "Bu Laras|Bagi hasil bulan ini sudah saya kirim ke para pemodal. Alhamdulillah." },
            new QuestNpc { Id = Darto, Name = "Pak Darto", Placeholder = "pak_ustad", Tint = "e8d8c4", AppearsWithQuest = true, Idle = "Pak Darto|Genteng bocor lagi. Begitulah punya properti—ada sewa, ada perawatan." },
            new QuestNpc { Id = Sinta, Name = "Mbak Sinta", Placeholder = "naya", Tint = "ffd9e6", AppearsWithQuest = true, Idle = "Mbak Sinta|Nisbah bulan ini sudah diumumkan di papan cabang, ya." },
            new QuestNpc { Id = Hendra, Name = "Om Hendra", Placeholder = "raka", Tint = "d8d8d8", AppearsWithQuest = true, Idle = "Om Hendra|Tingkat hunian gedung naik. Kabar baik untuk para pemilik unit." },
            new QuestNpc { Id = Yusuf, Name = "Pak Yusuf", Placeholder = "dimas", Tint = "d6eef0", AppearsWithQuest = true, Idle = "Pak Yusuf|Ilmu itu investasi yang imbal hasilnya tidak pernah habis." },
            // Penjaga Puskesmas Cempaka — tanpa quest, mengingatkan soal dana darurat kesehatan.
            new QuestNpc { Id = Ningsih, Name = "Bu Ningsih", Placeholder = "bu_siti", Tint = "d9f0ff", Idle = "Bu Ningsih|Sehat itu modal juga, lho. Sisihkan dana darurat sebelum badan yang menagih." },
            // Kasir FFC — tanpa quest, menyentil pengeluaran gaya hidup yang gampang bocor.
            new QuestNpc { Id = Fira, Name = "Mbak Fira", Placeholder = "naya", Tint = "ffd6cf", Idle = "Mbak Fira|Paket hemat memang murah satuan. Yang mahal itu kalau tiap hari, hehe." },
            // Pemilik Kafe Senja — tanpa quest, soal memisahkan uang usaha dari uang pribadi.
            new QuestNpc { Id = Bayu, Name = "Mas Bayu", Placeholder = "raka", Tint = "f6d9b8", Idle = "Mas Bayu|Kas kafe dan dompet pribadi itu dua dompet berbeda. Baru kutahu setelah setahun rugi." },
        }.Concat(CaseContent.Npcs).ToArray();   // + tokoh & benda Kasus Warga

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

        static readonly SideQuest[] Community =
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

        static SideQuest Invest(string id, string giver, string card, string requires, string title, string theme, string[] meet, ActivityBoard board, string[] boardOutcome, string[] close) => new SideQuest
        {
            Id = id, Giver = giver, Title = title, Theme = theme, Summary = "Kenalan dengan " + Npc(giver).Name + " dan belajar " + StoryContent.Card(card).Name + ".",
            Requires = requires == null ? Array.Empty<string>() : new[] { requires },
            Steps = new[]
            {
                Talk(giver, "Kenalan dengan " + Npc(giver).Name, meet).With(bond: 1),
                Board(giver, "Mini game: " + StoryContent.Card(card).Name, board, boardOutcome).With(logic: 3, sharia: 2),
                Talk(giver, "Dengarkan pesan " + Npc(giver).Name, close).With(bond: 1, card: card),
            },
        };

        // Rangkaian investasi (urut sesuai tabel rancangan): quest berikutnya terbuka setelah
        // quest sebelumnya selesai, dan NPC-nya baru hadir saat itu.
        static readonly SideQuest[] Investments =
        {
            Invest("invest.saham", Arga, "saham", null, "Kopi dan Saham", "Saham Syariah",
                new[] { "Mas Arga|Kamu tahu nggak, pemilik kedai kopi ini punya ratusan \"bos\"? Mereka pemegang sahamnya.", "Alif|Jadi beli saham itu artinya ikut memiliki perusahaan?", "Mas Arga|Tepat! Tapi nggak semua saham syariah. Yuk, main tebak-tebakan seleksi saham." },
                Sort("sort", "Pilih perusahaan di kiri, lalu tentukan apakah lolos seleksi saham syariah.",
                    new[] { "Pabrik makanan halal, utang berbunga 20% dari aset", "Toko ritel, pendapatan non-halal 3%", "Produsen minuman keras", "Situs judi daring", "Rumah sakit, utang berbunga 60% dari aset", "Operator jalan tol, pendapatan bunga 15%" },
                    new[] { "Lolos seleksi saham syariah", "Tidak lolos" },
                    new[] { 0, 0, 1, 1, 1, 1 },
                    new[] { "Usahanya halal dan utang berbasis bunganya di bawah 45% aset.", "Pendapatan non-halal masih di bawah batas 10%.", "Barang yang diharamkan membuat usaha tidak lolos.", "Perjudian (maysir) tidak lolos seleksi.", "Utang berbasis bunga melebihi 45% total aset.", "Pendapatan non-halal (bunga) melebihi 10% pendapatan." }),
                new[] { "Mas Arga|Mantap! Daftar saham yang lolos itu namanya Daftar Efek Syariah. Kumpulannya ada di indeks ISSI, dan 30 yang paling likuid di JII." },
                new[] { "Mas Arga|Satu lagi: harga saham bisa turun dalam semalam. Pakai uang yang tidak dipakai untuk kebutuhan, dan pikirkan jangka panjang.", "Mas Arga|Nih, catatan kecil dariku. Kalau mau belajar yang lebih tenang, temui Bu Ratna di Pusat Kota." }),
            Invest("invest.sukuk", Ratna, "sukuk", "invest.saham", "Rajutan Sukuk", "Sukuk",
                new[] { "Bu Ratna|Arga yang menyuruhmu ke sini? Anak itu suka yang naik-turun. Ibu sukanya sukuk.", "Alif|Sukuk itu seperti obligasi, Bu?", "Bu Ratna|Mirip, tapi ada aset dasarnya dan akadnya syariah—bukan utang berbunga. Coba bedakan Sukuk Ritel dan Sukuk Tabungan." },
                Sort("match", "Cocokkan ciri di kiri dengan jenis sukuknya.",
                    new[] { "Bisa dijual ke investor lain sebelum jatuh tempo", "Tidak bisa diperdagangkan, tapi bisa dicairkan sebagian lebih awal", "Imbalan dibayar setiap bulan", "Diterbitkan negara dengan aset dasar (underlying asset)", "Harganya bisa naik-turun di pasar sekunder" },
                    new[] { "Sukuk Ritel (SR)", "Sukuk Tabungan (ST)", "Keduanya" },
                    new[] { 0, 1, 2, 2, 0 },
                    new[] { "SR dapat diperdagangkan di pasar sekunder.", "ST tidak dapat diperdagangkan, tetapi punya fasilitas pencairan sebagian sebelum jatuh tempo.", "SR dan ST sama-sama membayar imbalan bulanan.", "Sukuk negara diterbitkan dengan aset dasar sebagai landasan akad.", "Karena bisa diperjualbelikan, harga SR bisa bergerak." }),
                new[] { "Bu Ratna|Pintar. Dulu gaji guru Ibu sebagian ditanam di sukuk negara—pelan, tapi hasilnya datang tiap bulan." },
                new[] { "Bu Ratna|Risikonya rendah-menengah, cocok untuk yang ingin tenang. Tapi tetap baca syarat dan jangka waktunya.", "Bu Ratna|Kalau mau mulai dari kecil dan dikelola ahlinya, cari Dewi di kampus. Anak itu pintar menjelaskan." }),
            Invest("invest.reksadana", Dewi, "reksadana", "invest.sukuk", "Kuis Reksa Dana", "Reksa Dana Syariah",
                new[] { "Dewi|Kuis dimulai! Pertanyaan pertama: kalau belum paham saham, bisa investasi nggak?", "Alif|Hmm… bisa, lewat reksa dana?", "Dewi|Betul! Uang kita dikumpulkan dan dikelola manajer investasi. Sekarang pasangkan tujuan dengan jenis reksa dananya!" },
                Sort("match", "Cocokkan tujuan keuangan di kiri dengan jenis reksa dana syariah yang pas.",
                    new[] { "Dana darurat, dipakai dalam 6 bulan", "DP motor 2 tahun lagi", "Tujuan 3–5 tahun, ingin seimbang", "Dana pensiun 20 tahun lagi" },
                    new[] { "Pasar uang", "Pendapatan tetap", "Campuran", "Saham" },
                    new[] { 0, 1, 2, 3 },
                    new[] { "Pasar uang: risiko rendah, cocok untuk jangka pendek.", "Pendapatan tetap (mis. sukuk): cocok untuk 1–3 tahun.", "Campuran: menyeimbangkan saham dan sukuk.", "Saham: naik-turun, cocok untuk jangka panjang." }),
                new[] { "Dewi|Skor sempurna! Kopi gratis untuk Kakak. Hasil reksa dana dilihat dari kenaikan NAB per unit." },
                new[] { "Dewi|Pastikan beli di agen penjual berizin OJK, dan reksa dananya punya Dewan Pengawas Syariah.", "Dewi|Oh iya, Pak Harun di Jalan Pasar lagi cari anak muda yang mau belajar soal emas." }),
            Invest("invest.emas", Harun, "emas", "invest.reksadana", "Timbangan Pak Harun", "Emas Syariah",
                new[] { "Pak Harun|Emas itu investasi paling tua, Nak. Tapi caranya beli juga harus benar.", "Alif|Memangnya ada cara beli emas yang salah, Pak?", "Pak Harun|Ada! Coba kamu pilah transaksi-transaksi ini." },
                Sort("sort", "Pilih transaksi di kiri, lalu kelompokkan.",
                    new[] { "Beli emas tunai di toko resmi, barang langsung diterima", "Cicil emas dengan akad murabahah, harga disepakati di awal", "Tabungan emas di lembaga resmi dengan bukti kepemilikan", "\"Emas digital\" di akun tak berizin, dijanjikan untung 5% per bulan", "Beli emas pakai uang SPP karena takut ketinggalan" },
                    new[] { "Sesuai syariah", "Waspada" },
                    new[] { 0, 0, 0, 1, 1 },
                    new[] { "Jual beli emas tunai jelas dan barangnya diterima.", "Fatwa DSN-MUI 77/2010 membolehkan jual beli emas tidak tunai selama emas bukan alat tukar resmi.", "Lembaga resmi memberi bukti kepemilikan atas emasnya.", "Janji untung tetap dari pihak tak berizin: tanda penipuan dan gharar.", "Kebutuhan pokok didahulukan; jangan membeli karena takut ketinggalan." }),
                new[] { "Pak Harun|Nah, begitu. Emas yang dibeli dengan cara benar bikin tidur nyenyak." },
                new[] { "Pak Harun|Ingat, harga emas juga bisa turun sementara, dan ada selisih harga jual-beli. Simpan untuk jangka menengah-panjang.", "Pak Harun|Kak Nadia sering bikin video di Taman Cempaka. Dia paham investasi yang dijual di bursa." }),
            Invest("invest.etf", Nadia, "etf", "invest.emas", "Playlist Investasi", "ETF Syariah",
                new[] { "Kak Nadia|ETF itu kayak playlist. Kamu nggak perlu pilih lagu satu-satu—satu playlist sudah berisi banyak lagu bagus.", "Alif|Jadi satu ETF isinya banyak saham?", "Kak Nadia|Yes! Tapi bedanya sama reksa dana dan saham apa? Ayo kita bikin konten tebak-tebakan!" },
                Sort("match", "Cocokkan ciri di kiri dengan jenis investasinya.",
                    new[] { "Dibeli di bursa lewat aplikasi sekuritas, harga berubah sepanjang hari", "Sekeranjang saham yang mengikuti indeks syariah", "Dibeli lewat agen penjual, harganya NAB sekali sehari", "Kepemilikan pada satu perusahaan saja" },
                    new[] { "ETF Syariah", "Reksa Dana Syariah", "Saham Syariah" },
                    new[] { 0, 0, 1, 2 },
                    new[] { "ETF diperdagangkan di bursa seperti saham.", "ETF syariah berisi sekeranjang saham mengikuti indeks syariah.", "Reksa dana biasa dihargai dengan NAB harian.", "Satu saham berarti memiliki satu perusahaan." }),
                new[] { "Kak Nadia|Kontennya jadi! Makasih, kamu jadi bintang tamu dadakan." },
                new[] { "Kak Nadia|Risikonya menengah-tinggi karena isinya saham. Tetap cek indeks acuannya dan biayanya.", "Kak Nadia|Kalau mau lihat investasi yang langsung menyentuh usaha kecil, mampir ke Bu Laras di Jalan Pasar." }),
            Invest("invest.p2p", Laras, "p2p", "invest.etf", "Keripik Urunan Warga", "P2P / Crowdfunding Syariah",
                new[] { "Bu Laras|Wajan-wajan baru ini hasil pendanaan syariah dari banyak orang. Mereka dapat bagi hasil dari penjualan keripik.", "Alif|Tapi bagaimana pemodal tahu usahanya aman, Bu?", "Bu Laras|Nah, ada langkah-langkahnya. Coba susun urutannya." },
                Sort("flow", "Pilih langkah di kiri, lalu letakkan pada urutannya.",
                    new[] { "Danai sebagian, sebar ke beberapa UMKM", "Cek izin platform di OJK", "Terima bagi hasil sesuai akad", "Baca akad & profil usaha", "UMKM menjalankan usaha dan melapor" },
                    new[] { "Langkah 1", "Langkah 2", "Langkah 3", "Langkah 4", "Langkah 5" },
                    new[] { 2, 0, 4, 1, 3 },
                    new[] { "Menyebar dana mengurangi risiko satu usaha gagal.", "Platform wajib berizin dan diawasi OJK.", "Bagi hasil/margin diterima sesuai akad yang disepakati.", "Pahami akad (musyarakah, mudharabah, murabahah) dan usaha yang didanai.", "Usaha berjalan dan pemodal menerima laporan." }),
                new[] { "Bu Laras|Persis begitu. Dengan urutan itu, pemodal dan saya sama-sama tenang." },
                new[] { "Bu Laras|Risikonya ada: usaha bisa merugi atau telat bayar, dan dana tidak bisa ditarik sebelum waktunya. Jangan taruh semua di satu tempat.", "Bu Laras|Pak Darto di Pusat Kota punya cerita soal investasi yang bisa ditinggali. Tanyakan saja." }),
            Invest("invest.properti", Darto, "properti", "invest.p2p", "Kontrakan Pak Darto", "Properti Syariah",
                new[] { "Pak Darto|Kontrakan ini dulu saya beli lewat pembiayaan syariah. Cicilannya tetap, tidak ada bunga berlipat.", "Alif|Akadnya macam-macam ya, Pak?", "Pak Darto|Betul. Coba cocokkan ciri-cirinya." },
                Sort("match", "Cocokkan ciri di kiri dengan akad atau penilaiannya.",
                    new[] { "Harga rumah disepakati di awal, cicilan tetap sampai lunas", "Bank dan pembeli patungan, porsi bank dibeli bertahap", "Rumah disewakan, pemilik menerima uang sewa", "Denda bunga berlipat saat telat membayar" },
                    new[] { "Murabahah", "Musyarakah mutanaqisah", "Ijarah (sewa)", "Tidak sesuai syariah" },
                    new[] { 0, 1, 2, 3 },
                    new[] { "Murabahah: jual beli dengan margin yang disepakati di awal.", "Musyarakah mutanaqisah: kepemilikan bersama yang porsinya berpindah bertahap.", "Ijarah: pendapatan dari sewa manfaat aset.", "Tambahan berlipat atas keterlambatan termasuk riba." }),
                new[] { "Pak Darto|Nah. Dari kontrakan ini saya dapat sewa, dan nilai tanahnya naik pelan-pelan." },
                new[] { "Pak Darto|Tapi properti sulit dijual cepat dan butuh biaya perawatan. Hitung dulu sebelum membeli.", "Pak Darto|Kalau mau yang risikonya rendah, Mbak Sinta di Bank Syariah bisa jelaskan deposito." }),
            Invest("invest.deposito", Sinta, "deposito", "invest.properti", "Istirahat Siang Teller", "Deposito Syariah",
                new[] { "Mbak Sinta|Nasabah sering tanya, \"Bunga depositonya berapa, Mbak?\" Aku jawab: di sini bukan bunga, tapi nisbah.", "Alif|Bedanya apa, Mbak?", "Mbak Sinta|Coba tebak mana yang deposito syariah, mana yang konvensional." },
                Sort("match", "Cocokkan ciri di kiri dengan jenis depositonya.",
                    new[] { "Nisbah bagi hasil 60:40 dari pendapatan bank", "Bunga tetap 5% per tahun", "Hasilnya bisa naik-turun mengikuti kinerja bank", "Simpanan dijamin LPS sesuai ketentuan" },
                    new[] { "Deposito mudharabah (syariah)", "Deposito konvensional", "Keduanya" },
                    new[] { 0, 1, 0, 2 },
                    new[] { "Mudharabah membagi hasil sesuai nisbah yang disepakati.", "Bunga tetap adalah ciri deposito konvensional.", "Karena bagi hasil, imbal hasilnya mengikuti pendapatan bank.", "LPS menjamin simpanan di bank umum maupun bank syariah sesuai ketentuan." }),
                new[] { "Mbak Sinta|Pintar! Risikonya rendah, tapi uangnya terkunci sampai jatuh tempo." },
                new[] { "Mbak Sinta|Cocok untuk dana yang belum dipakai dalam waktu dekat.", "Mbak Sinta|Om Hendra di Jalan Pasar punya cerita soal gedung yang dimiliki ribuan orang. Seru, lho." }),
            Invest("invest.dire", Hendra, "dire", "invest.deposito", "Gedung Milik Bersama", "DIRE Syariah",
                new[] { "Om Hendra|Gedung perkantoran di ujung jalan itu, pemiliknya ribuan orang lewat DIRE syariah.", "Alif|Kok bisa, Om?", "Om Hendra|Susun alurnya, nanti kamu paham." },
                Sort("flow", "Pilih kejadian di kiri, lalu letakkan pada urutannya.",
                    new[] { "Penyewa membayar sewa", "Investor membeli unit DIRE syariah", "Pendapatan sewa dibagikan ke pemegang unit", "Dana dipakai memiliki properti produktif" },
                    new[] { "Langkah 1", "Langkah 2", "Langkah 3", "Langkah 4" },
                    new[] { 2, 0, 3, 1 },
                    new[] { "Penyewa (kantor, toko) membayar sewa.", "Investor membeli unit untuk ikut memiliki.", "Pendapatan properti dibagikan kepada pemegang unit.", "Dana dipakai memiliki gedung yang menghasilkan pendapatan halal." }),
                new[] { "Om Hendra|Itu dia. Kalau gedungnya ramai penyewa, pemilik unit ikut senang." },
                new[] { "Om Hendra|Risikonya: kalau hunian turun, pendapatan juga turun. Pahami propertinya dulu.", "Om Hendra|Yang terakhir paling rumit—EBA syariah. Pak Yusuf di kampus jagonya." }),
            Invest("invest.eba", Yusuf, "eba", "invest.dire", "Kuliah Semester Akhir", "EBA Syariah",
                new[] { "Pak Yusuf|EBA itu singkatan Efek Beragun Aset. Kedengarannya rumit, tapi intinya soal arus kas.", "Alif|Arus kas dari mana, Pak?", "Pak Yusuf|Dari aset dasar. Coba urutkan alurnya." },
                Sort("flow", "Pilih kejadian di kiri, lalu letakkan pada urutannya.",
                    new[] { "Investor membeli EBA syariah", "Bank memiliki kumpulan piutang pembiayaan rumah", "Cicilan nasabah mengalir ke investor", "Piutang dikemas menjadi efek (EBA syariah)" },
                    new[] { "Langkah 1", "Langkah 2", "Langkah 3", "Langkah 4" },
                    new[] { 2, 0, 3, 1 },
                    new[] { "Investor membeli efeknya.", "Awalnya ada kumpulan aset keuangan, misalnya piutang pembiayaan.", "Arus kas dari aset dasar dibayarkan kepada investor.", "Aset dikemas menjadi efek yang bisa dimiliki investor." }),
                new[] { "Pak Yusuf|Luar biasa. Kamu sudah menamatkan sepuluh jenis investasi syariah." },
                new[] { "Pak Yusuf|Strukturnya rumit, jadi pelajari setelah paham investasi dasar. Dan ingat: investasi terbaik tetap yang kamu pahami.", "Pak Yusuf|Buka aplikasi Investasi di HP-mu. Semua catatanmu tersimpan di sana." }),
        };

        /// <summary>Kasus Warga didahulukan: bila satu tokoh punya langkah kasus dan langkah side
        /// quest sekaligus, kasus bab yang sedang berjalan menang (lihat SideQuestRules.StepAt).</summary>
        public static readonly SideQuest[] All = CaseContent.All.Concat(Community).Concat(Investments).ToArray();

        public static SideQuest Find(string id) => All.FirstOrDefault(q => q.Id == id);

        public static QuestNpc Npc(string id) => Npcs.FirstOrDefault(n => n.Id == id);

        static QuestStep With(this QuestStep step, string reward = null, float logic = 0, float sharia = 0, int bond = 0, string card = null)
        {
            step.RewardItem = reward ?? step.RewardItem;
            step.Card = card ?? step.Card;
            step.Logic = logic;
            step.Sharia = sharia;
            step.Bond = bond;
            return step;
        }
    }
}
