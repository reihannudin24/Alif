using System;
using System.Linq;

namespace Alif.Adventure
{
    /// <summary>
    /// Kasus Warga Bab 1 — "Cempaka, Minggu Pertama". Satu map kota, satu persoalan; tiap kasus
    /// terbuka pada hari cerita tertentu (<see cref="SideQuest.Day"/>) dan tetap terbuka sampai
    /// selesai. Semuanya wajib (<see cref="SideQuest.Mandatory"/>): tugas penutup bab baru bisa
    /// dikerjakan setelah keenamnya beres. Benang merahnya kejelasan dan pencatatan — harga,
    /// wakaf, kembalian, kas, upah, utang. Mesin langkahnya sama dengan side quest
    /// (<see cref="SideQuestRules"/>); tokohnya memakai sprite placeholder yang dibedakan lewat
    /// <see cref="QuestNpc.Tint"/>. Klaim syariah perlu ditinjau narasumber sebelum rilis
    /// (lihat Docs/GAME_REFERENCE.md).
    /// </summary>
    public static class CaseContent
    {
        public const int Chapter = 1;

        // Tokoh kasus
        public const string Yanto = "npc:yanto", Mira = "npc:mira", Gunawan = "npc:gunawan", Salamah = "npc:salamah",
            Mahfud = "npc:mahfud", Joko = "npc:joko", Rini = "npc:rini", Ilham = "npc:ilham", Salsa = "npc:salsa",
            Gilang = "npc:gilang", Wati = "npc:wati";
        // Benda yang bisa diperiksa (titik interaksi tanpa sprite) + ranjang kamar kos
        public const string Stall = "npc:lapak", ParkBoard = "npc:papantaman", NoticeBoard = "npc:mading", Bed = "npc:kasur";

        public const string PriceList = "Daftar Harga Pasaran", WaqfDeed = "Salinan Akta Ikrar Wakaf",
            FixedReceipt = "Struk Koreksi Idmaret", CashReport = "Laporan Kas Himpunan",
            WorkAgreement = "Surat Perjanjian Kerja", DebtBook = "Buku Bon Baru";

        public static readonly QuestNpc[] Npcs =
        {
            // Jalan Pasar
            new QuestNpc { Id = Yanto, Name = "Pak Yanto", Placeholder = "pak_ustad", Tint = "ffe2b0", Idle = "Pak Yanto|Jeruknya manis, Mas. Salak juga baru datang dari lereng.",
                AfterQuest = "case.harga", IdleAfter = "Pak Yanto|Sejak harga saya pajang, pembeli malah tambah ramai. Tidak ada lagi yang pergi sambil menggerutu." },
            new QuestNpc { Id = Mira, Name = "Mbak Mira", Placeholder = "naya", Tint = "ffc9c9", AppearsOnDay = 2, Idle = "Mbak Mira|Sekarang aku tinggal baca papan harganya. Lebih enak begini, nggak perlu tebak-tebakan." },
            new QuestNpc { Id = Wati, Name = "Bu Wati", Placeholder = "bu_siti", Tint = "e0d0ff", AppearsOnDay = 7, Idle = "Bu Wati|Bon saya sudah lunas dan ada parafnya. Tidur jadi lebih nyenyak." },
            new QuestNpc { Id = Stall, Name = "Lapak Pak Yanto", IsObject = true, Idle = "Alif (Batin)|Buah-buahan segar tersusun rapi di meja dagangan." },
            // Taman Cempaka + Masjid Al-Amanah
            new QuestNpc { Id = Salamah, Name = "Bu Salamah", Placeholder = "bu_siti", Tint = "d8f0d0", Idle = "Bu Salamah|Dua puluh tahun saya menyapu daun di taman ini. Tamannya milik semua orang, jadi semua orang ikut menjaga." },
            new QuestNpc { Id = Gunawan, Name = "Pak Gunawan", Placeholder = "raka", Tint = "c8c8d8", AppearsOnDay = 3, Idle = "Pak Gunawan|Saya jadi sering ke sini. Ternyata begini rasanya melihat amal Bapak masih dipakai orang." },
            new QuestNpc { Id = Mahfud, Name = "Pak Mahfud", Placeholder = "pak_ustad", Tint = "d0f0e8", Idle = "Pak Mahfud|Saya nazhir wakaf di lingkungan ini. Tugas nazhir itu menjaga amanah orang yang sudah tiada.",
                AfterQuest = "case.wakaf", IdleAfter = "Pak Mahfud|Berkas sertifikat wakaf Taman Cempaka sudah masuk ke KUA. Dua puluh tahun terlambat, tapi lebih baik daripada tidak sama sekali." },
            new QuestNpc { Id = ParkBoard, Name = "Papan taman", IsObject = true, Idle = "Alif (Batin)|Papan informasi Taman Cempaka. Ada jadwal kerja bakti dan peta taman." },
            // Pusat Kota + Idmaret
            new QuestNpc { Id = Joko, Name = "Pak Joko", Placeholder = "dimas", Tint = "ffe0c0", AppearsOnDay = 4, Idle = "Pak Joko|Sekarang kasirnya bertanya dulu sebelum menambah donasi. Saya malah jadi sering ikut menyumbang." },
            new QuestNpc { Id = Rini, Name = "Mbak Rini", Placeholder = "naya", Tint = "cfe0ff", Idle = "Mbak Rini|Selamat datang di Idmaret. Ada yang bisa dibantu?",
                AfterQuest = "case.kembalian", IdleAfter = "Mbak Rini|Stok koin sudah datang, dan sekarang saya selalu bertanya dulu: \"Mau ikut donasi, Kak?\"" },
            // Kampus Cempaka
            new QuestNpc { Id = Salsa, Name = "Salsa", Placeholder = "naya", Tint = "f0d8ff", Idle = "Salsa|Aku bendahara himpunan. Nota-nota ini… nanti deh kurapikan habis ujian.",
                AfterQuest = "case.kas", IdleAfter = "Salsa|Sekarang tiap ada nota langsung kucatat hari itu juga. Ternyata lima menit sehari jauh lebih ringan daripada sekardus sebulan." },
            new QuestNpc { Id = Ilham, Name = "Ilham", Placeholder = "raka", Tint = "d0e8ff", AppearsOnDay = 5, Idle = "Ilham|Laporan kas sekarang ditempel tiap bulan. Aku yang paling rajin membacanya." },
            new QuestNpc { Id = NoticeBoard, Name = "Mading kampus", IsObject = true, Idle = "Alif (Batin)|Papan pengumuman kampus: lowongan asisten, jadwal seminar, dan poster kuis." },
            // Jalan Kafe (Mas Bayu didaftarkan di SideQuestContent)
            new QuestNpc { Id = Gilang, Name = "Gilang", Placeholder = "dimas", Tint = "e8ffd8", AppearsOnDay = 6, Idle = "Gilang|Upahku sekarang turun tiap tanggal satu, sesuai surat. Kerja jadi lebih tenang." },
            // Kamar Alif
            new QuestNpc { Id = Bed, Name = "Ranjang", IsObject = true, Idle = "Alif (Batin)|Ranjang kamar kos. Sederhana, tapi cukup untuk melepas lelah." },
        };

        /// <summary>Kabar yang Alif dengar saat bangun — petunjuk map mana yang punya kasus baru.</summary>
        public static string MorningNews(int day) => All.FirstOrDefault(q => q.Day == day)?.News;

        static ActivityBoard Inspect(string[] cards, string[] notes) => new ActivityBoard
        {
            Kind = "inspect", Instruction = "Klik setiap bagian untuk mencatat temuan.", Cards = cards,
            Slots = new[] { "Catat temuan" }, Answers = new int[cards.Length], Notes = notes,
        };

        static ActivityBoard Sort(string kind, string instruction, string[] cards, string[] slots, int[] answers, string[] notes) => new ActivityBoard
        {
            Kind = kind, Instruction = instruction, Cards = cards, Slots = slots, Answers = answers, Notes = notes,
        };

        static QuestStep Talk(string target, string objective, string[] lines, string[] outcome = null) =>
            new QuestStep { Kind = "talk", Target = target, Objective = objective, Lines = lines, Outcome = outcome ?? Array.Empty<string>() };

        static QuestStep Give(string target, string objective, string item, string[] lines, string[] outcome) =>
            new QuestStep { Kind = "give", Target = target, Objective = objective, RequiredItem = item, Lines = lines, Outcome = outcome };

        static QuestStep Board(string target, string objective, string[] lines, ActivityBoard board, string[] outcome) =>
            new QuestStep { Kind = board.Kind, Target = target, Objective = objective, Lines = lines, Board = board, Outcome = outcome };

        static QuestStep With(this QuestStep step, string reward = null, float logic = 0, float sharia = 0, int bond = 0)
        {
            step.RewardItem = reward; step.Logic = logic; step.Sharia = sharia; step.Bond = bond;
            return step;
        }

        static SideQuest Case(int day, string id, string title, string theme, string summary, string giver, string news, params QuestStep[] steps) =>
            new SideQuest { Id = id, Title = title, Theme = theme, Summary = summary, Giver = giver, News = news, Steps = steps, Day = day, Chapter = Chapter, Mandatory = true };

        public static readonly SideQuest[] All =
        {
            // ─────────────── HARI 2 · Jalan Pasar ───────────────
            Case(2, "case.harga", "Harga yang Disembunyikan", "Ghabn & kejelasan harga",
                "Dua pembeli membayar harga berbeda untuk jeruk yang sama di lapak tanpa papan harga.", Mira,
                "Alif (Batin)|Dari jendela kamar terdengar suara orang beradu mulut di Jalan Pasar, dekat Toko Kelontong.",
                Talk(Mira, "Dengarkan keluhan Mbak Mira", new[]
                {
                    "Mbak Mira|Mas, coba lihat! Saya beli jeruk sekilo dua puluh lima ribu. Ibu sebelum saya cuma lima belas ribu. Jeruknya sama persis!",
                    "Alif|Sebelum membeli, harganya sempat disebut, Mbak?",
                    "Mbak Mira|Nggak. Ditimbang dulu, baru disebut. Saya sungkan mau batal.",
                    "Alif|Saya coba tanyakan baik-baik ke Pak Yanto, ya. Mungkin ada penjelasannya.",
                }).With(bond: 1),
                Talk(Yanto, "Tanyakan harga ke Pak Yanto", new[]
                {
                    "Pak Yanto|Harga ya lihat-lihat orangnya, Mas. Langganan saya kasih murah. Yang kelihatan baru… ya beda sedikit.",
                    "Alif|Pembelinya tahu nggak, Pak, kalau harganya beda?",
                    "Pak Yanto|Ya… nggak saya kasih tahu. Kalau tahu nanti ribut, kan?",
                    "Alif (Batin)|Beliau tidak merasa menipu. Tapi pembelinya tidak pernah diberi kesempatan tahu harga pasaran.",
                }),
                Board(Stall, "Periksa lapak Pak Yanto", new[] { "Alif (Batin)|Aku lihat dulu lapaknya. Yang dicatat yang terlihat saja, bukan dugaan." },
                    Inspect(new[] { "Papan harga", "Timbangan", "Nota tulis tangan", "Harga di lapak sebelah" },
                        new[]
                        {
                            "Tidak ada papan harga. Harga baru disebut setelah buah ditimbang.",
                            "Timbangan menunjuk nol saat kosong dan bersegel tera. Takarannya jujur.",
                            "Nota hanya memuat total: tanpa harga per kilo, tanpa berat.",
                            "Tiga lapak lain memajang jeruk Rp15.000–16.000 per kilo.",
                        }),
                    new[] { "Alif (Batin)|Timbangannya jujur. Masalahnya bukan takaran, tapi harga yang disembunyikan sampai pembeli sungkan menolak." }).With(logic: 3),
                Board(Mira, "Pilah cara menetapkan harga", new[] { "Mbak Mira|Tapi pedagang kan boleh cari untung, Mas? Saya jadi bingung salahnya di mana." },
                    Sort("sort", "Pilih cara di kiri, lalu kelompokkan di kanan.",
                        new[] { "Harga disebut sebelum buah ditimbang", "Tawar-menawar sampai sama-sama rela", "Diskon langganan yang diumumkan terbuka", "Menaikkan harga karena pembeli tampak tidak tahu pasaran", "Menyebut harga setelah barang dibungkus", "Nota tanpa harga per kilo dan berat" },
                        new[] { "Jelas & saling rela", "Merugikan pembeli yang tidak tahu" },
                        new[] { 0, 0, 0, 1, 1, 1 },
                        new[]
                        {
                            "Harga yang diketahui sebelum akad memberi pembeli pilihan untuk setuju atau batal.",
                            "Tawar-menawar sah selama kedua pihak tahu apa yang disepakati.",
                            "Harga langganan boleh berbeda selama aturannya terbuka, bukan disembunyikan.",
                            "Memanfaatkan ketidaktahuan pembeli akan harga pasaran disebut ghabn: selisih mencolok yang tidak disadari.",
                            "Harga yang baru disebut belakangan membuat pembeli sungkan menolak — relanya jadi terpaksa.",
                            "Nota tanpa rincian tidak bisa diperiksa ulang oleh siapa pun.",
                        }),
                    new[]
                    {
                        "Alif|Untung itu boleh, Mbak. Yang jadi soal kalau pembeli tidak tahu harga pasaran lalu dimanfaatkan. Jual beli itu harus sama-sama rela, dan rela butuh tahu.",
                        "Mbak Mira|Tadi saya sempat catat harga di tiga lapak lain. Ini, bawa saja ke Pak Yanto.",
                    }).With(reward: PriceList, logic: 3, sharia: 4, bond: 1),
                Give(Yanto, "Berikan daftar harga ke Pak Yanto", PriceList,
                    new[] { "Pak Yanto|Ini… harga lapak lain? Wah, selisih saya ke Mbak tadi sepuluh ribu, ya." },
                    new[]
                    {
                        "Alif|Bapak tidak curang di timbangan. Tinggal satu langkah lagi: harga dipajang, jadi semua pembeli tahu sebelum setuju.",
                        "Pak Yanto|Saya kira begitu itu biasa saja. Ternyata yang bikin orang kapok bukan mahalnya, tapi merasa dibohongi.",
                        "Pak Yanto|Mbak Mira, ini selisihnya saya kembalikan. Besok papan harga saya pasang besar-besar.",
                    }).With(sharia: 3, bond: 2)),

            // ─────────────── HARI 3 · Taman Cempaka ───────────────
            Case(3, "case.wakaf", "Tanah yang Diwakafkan", "Wakaf, nazhir & akta ikrar",
                "Seorang ahli waris memasang papan sengketa di taman yang ternyata tanah wakaf.", Gunawan,
                "Alif (Batin)|Pagi ini warga ramai membicarakan Taman Cempaka. Katanya ada papan larangan yang baru dipasang.",
                Talk(Gunawan, "Dengarkan Pak Gunawan", new[]
                {
                    "Pak Gunawan|Tolong jangan ikut campur, Mas. Tanah taman ini atas nama almarhum Bapak saya, Haji Dahlan.",
                    "Pak Gunawan|Sertifikatnya belum pernah dibalik nama. Artinya masih milik keluarga. Ada yang mau beli, dan saya butuh biaya.",
                    "Alif|Boleh saya tahu, Pak, siapa yang bilang tanah ini bisa dijual?",
                    "Pak Gunawan|Seorang makelar. Katanya tinggal pasang papan sengketa, nanti warga mundur sendiri.",
                }),
                Talk(Salamah, "Dengarkan Bu Salamah", new[]
                {
                    "Bu Salamah|Haji Dahlan mewakafkan tanah ini tahun 1998, Nak. Saya ada di sana waktu beliau mengucapkan ikrarnya.",
                    "Bu Salamah|Dulu ada plakatnya di papan taman. Sekarang tertutup perdu, mungkin orang sudah lupa.",
                    "Alif|Berarti yang kita perlukan bukan adu suara, tapi buktinya.",
                }).With(bond: 1),
                Board(ParkBoard, "Periksa papan taman", new[] { "Alif (Batin)|Aku singkap perdunya pelan-pelan." },
                    Inspect(new[] { "Plakat di balik perdu", "Tahun pada plakat", "Peruntukan", "Papan larangan baru" },
                        new[]
                        {
                            "Plakat kuningan berkarat: \"Tanah wakaf H. Dahlan\".",
                            "Tertera tahun 1998 — cocok dengan cerita Bu Salamah.",
                            "\"Untuk taman dan musala warga Cempaka.\" Peruntukannya jelas.",
                            "Papan \"TANAH SENGKETA\" baru dipasang kemarin, tanpa nomor perkara apa pun.",
                        }),
                    new[] { "Alif (Batin)|Plakat ini petunjuk, belum bukti. Wakaf yang sah punya akta. Pengurus wakafnya pasti tahu — Masjid Al-Amanah di Pusat Kota." }).With(logic: 3),
                Talk(Mahfud, "Temui nazhir di Masjid Al-Amanah", new[]
                {
                    "Pak Mahfud|Tanah Taman Cempaka? Betul, itu wakaf Haji Dahlan. Saya nazhirnya — pengelola yang ditunjuk menjaga harta wakaf.",
                    "Pak Mahfud|Akta Ikrar Wakafnya ada di KUA, salinannya saya simpan. Tapi jujur saja… sertifikat tanah wakafnya belum pernah saya urus. Itu kelalaian saya.",
                    "Alif|Jadi wajar kalau Pak Gunawan mengira tanah itu masih atas nama ayahnya.",
                }).With(bond: 1),
                Board(Mahfud, "Susun alur wakaf", new[] { "Pak Mahfud|Supaya bisa menjelaskan ke Pak Gunawan, mari kita urutkan dulu jalannya wakaf." },
                    Sort("flow", "Pilih peristiwa di kiri, lalu letakkan pada urutan langkahnya.",
                        new[] { "Nazhir mengelola; manfaatnya dinikmati warga", "Wakif mengucapkan ikrar di hadapan pejabat KUA & saksi", "Tanah didaftarkan menjadi sertifikat wakaf", "Pemilik berniat melepas hartanya untuk kepentingan umum" },
                        new[] { "Langkah 1", "Langkah 2", "Langkah 3", "Langkah 4" },
                        new[] { 3, 1, 2, 0 },
                        new[]
                        {
                            "Sejak diwakafkan, pokok hartanya ditahan dan manfaatnya terus mengalir.",
                            "Ikrar di depan pejabat dan saksi dituangkan menjadi Akta Ikrar Wakaf.",
                            "Langkah inilah yang terlewat: tanpa sertifikat wakaf, status tanah mudah dipersoalkan.",
                            "Wakaf berawal dari niat pemilik yang sah atas hartanya sendiri.",
                        }),
                    new[]
                    {
                        "Pak Mahfud|Lihat, alurnya tidak punya jalan balik ke ahli waris. Harta wakaf tidak dijual, tidak dihibahkan, tidak diwariskan.",
                        "Pak Mahfud|\"Tahanlah pokoknya, sedekahkan hasilnya\" — begitu pesan Nabi kepada Umar tentang tanahnya di Khaibar. Bawalah salinan akta ini.",
                    }).With(reward: WaqfDeed, logic: 3, sharia: 5),
                Board(Salamah, "Pilah bukti status tanah", new[] { "Bu Salamah|Sebelum menemui Pak Gunawan, kita pilah dulu mana yang bukti, mana yang cuma kata orang." },
                    Sort("sort", "Pilih keterangan di kiri, lalu kelompokkan di kanan.",
                        new[] { "Akta Ikrar Wakaf tahun 1998", "Nazhir terdaftar: Pak Mahfud", "Kesaksian Bu Salamah atas ikrar", "Sertifikat tanah wakaf belum terbit", "\"Sertifikat lama belum dibalik nama\"", "\"Sudah lama dipakai warga\"" },
                        new[] { "Bukti status wakaf", "Perlu diurus ke KUA/BPN", "Bukan bukti kepemilikan" },
                        new[] { 0, 0, 0, 1, 2, 2 },
                        new[]
                        {
                            "Akta ikrar adalah bukti utama bahwa tanah sudah diwakafkan.",
                            "Nazhir yang terdaftar menunjukkan wakafnya dikelola secara resmi.",
                            "Saksi ikrar menguatkan akta.",
                            "Sertifikat wakaf belum ada: kelalaian administrasi yang harus segera dibereskan.",
                            "Nama di sertifikat lama tidak membatalkan ikrar wakaf yang sah.",
                            "Lama dipakai bukan dasar kepemilikan — tapi juga bukan alasan menuduh siapa pun.",
                        }),
                    new[] { "Bu Salamah|Jadi persoalannya bukan Pak Gunawan yang jahat. Administrasinya yang dibiarkan menggantung dua puluh tahun lebih." }).With(logic: 3, sharia: 3, bond: 1),
                Give(Gunawan, "Tunjukkan akta ke Pak Gunawan", WaqfDeed,
                    new[] { "Pak Gunawan|Akta Ikrar Wakaf… tanda tangan Bapak. Tahun 1998. Saya waktu itu masih merantau, tidak pernah diberi tahu." },
                    new[]
                    {
                        "Alif|Bapak tidak salah karena tidak tahu. Tapi tanah ini sudah bukan milik keluarga sejak ikrar itu — manfaatnya milik warga, pahalanya terus mengalir untuk almarhum.",
                        "Pak Gunawan|Makelar itu hampir membuat saya menjual amal Bapak saya sendiri. Papannya saya cabut sekarang.",
                        "Pak Mahfud|Dan saya yang akan mengurus sertifikat wakafnya ke KUA minggu ini. Niat baik saja tidak cukup; harus dicatat.",
                        "Bu Salamah|Plakatnya kita pasang lagi di tempat yang terlihat, ya.",
                    }).With(sharia: 4, bond: 2)),

            // ─────────────── HARI 4 · Pusat Kota ───────────────
            Case(4, "case.kembalian", "Kembalian dan Kotak Amal", "Ridha dalam muamalah & sedekah sukarela",
                "Kembalian diganti permen dan donasi tercentang otomatis di kasir Idmaret.", Joko,
                "Alif (Batin)|Ibu kos bercerita: kemarin sore ada pembeli yang menolak pergi dari depan Idmaret di Pusat Kota.",
                Talk(Joko, "Dengarkan Pak Joko", new[]
                {
                    "Pak Joko|Lihat struk saya, Mas. Kembalian lima ratus diganti permen. Lalu ada \"donasi Rp1.000\" yang tidak pernah saya setujui.",
                    "Pak Joko|Bukan soal seribu rupiahnya. Saya cuma tidak suka uang saya diputuskan orang lain.",
                    "Alif|Itu keberatan yang wajar, Pak. Saya tanyakan ke kasirnya, ya.",
                }).With(bond: 1),
                Talk(Rini, "Tanyakan ke Mbak Rini", new[]
                {
                    "Mbak Rini|Koin lima ratusan sering kosong, Mas. Dari dulu gantinya permen. Donasi itu… di layar memang sudah tercentang dari sananya.",
                    "Mbak Rini|Saya juga tidak enak sebenarnya. Tapi antrean panjang, jadi saya tekan lanjut saja.",
                    "Alif|Boleh saya lihat layar kasir dan aturan tertulisnya?",
                }),
                Board(Rini, "Periksa meja kasir", new[] { "Mbak Rini|Silakan, Mas. Saya juga ingin tahu yang benar bagaimana." },
                    Inspect(new[] { "Layar kasir", "Laci koin", "Buku pedoman kasir", "Kotak donasi" },
                        new[]
                        {
                            "Kolom \"Donasi Rp1.000\" sudah tercentang sebelum pembeli ditanya.",
                            "Laci koin lima ratusan kosong; stok belum diminta ke kantor sejak pekan lalu.",
                            "Pedoman tertulis: \"TAWARKAN donasi kepada pelanggan.\" Bukan mencentang duluan.",
                            "Kotak donasi tidak mencantumkan penyalur maupun peruntukannya.",
                        }),
                    new[] { "Alif (Batin)|Pedomannya sudah benar: ditawarkan. Yang melenceng kebiasaannya — dan koin yang tidak pernah diisi ulang." }).With(logic: 3),
                Board(Rini, "Pilah kebiasaan di kasir", new[] { "Mbak Rini|Jadi mana yang boleh, mana yang harus saya hentikan?" },
                    Sort("sort", "Pilih kebiasaan di kiri, lalu kelompokkan di kanan.",
                        new[] { "Kembalian utuh dalam rupiah", "Menawarkan donasi, pembeli boleh menolak", "Mengganti kembalian dengan permen tanpa bertanya", "Donasi tercentang sebelum pembeli ditanya", "Pembulatan ke atas tanpa pemberitahuan", "Mencantumkan penyalur dan peruntukan donasi" },
                        new[] { "Hak pembeli & sukarela", "Mengambil tanpa kerelaan" },
                        new[] { 0, 0, 1, 1, 1, 0 },
                        new[]
                        {
                            "Kembalian adalah hak pembeli, sekecil apa pun.",
                            "Sedekah bernilai karena sukarela; menawarkan itu baik.",
                            "Permen bukan alat bayar. Tanpa persetujuan, pembeli dipaksa membeli barang yang tidak dimintanya.",
                            "Donasi yang diputuskan sepihak bukan sedekah — itu mengambil harta tanpa kerelaan pemiliknya.",
                            "Pembulatan harus diketahui dan disetujui pembeli.",
                            "Donasi yang jelas penyalurnya membuat orang tenang menyumbang.",
                        }),
                    new[]
                    {
                        "Alif|Harta orang lain hanya halal diambil dengan kerelaan pemiliknya — termasuk lima ratus rupiah dan seribu rupiah.",
                        "Mbak Rini|Saya cetak ulang struk Pak Joko tanpa donasi, dan kembaliannya saya ambilkan dari kas. Tolong sampaikan, ya. Hari ini juga saya minta stok koin.",
                    }).With(reward: FixedReceipt, logic: 3, sharia: 4, bond: 1),
                Give(Joko, "Serahkan struk koreksi ke Pak Joko", FixedReceipt,
                    new[] { "Pak Joko|Struk baru… kembalian lima ratus rupiah, tanpa donasi. Wah, benar-benar diurus." },
                    new[]
                    {
                        "Alif|Mbak Rini juga akan mulai bertanya dulu sebelum menambahkan donasi.",
                        "Pak Joko|Kalau ditanya baik-baik, saya malah senang ikut menyumbang. Nah, ini seribunya — sekarang saya yang mau.",
                    }).With(sharia: 3, bond: 2)),

            // ─────────────── HARI 5 · Kampus Cempaka ───────────────
            Case(5, "case.kas", "Kas yang Tidak Dilaporkan", "Amanah, pencatatan & larangan menuduh tanpa bukti",
                "Tuduhan tanpa nama ditempel di mading karena kas himpunan tidak pernah dilaporkan.", Ilham,
                "Alif (Batin)|Ada mahasiswa lewat depan kos sambil ribut soal tulisan di mading Kampus Cempaka.",
                Talk(Ilham, "Dengarkan Ilham", new[]
                {
                    "Ilham|Satu juta rupiah iuran seminar, dan sampai sekarang tidak ada laporannya! Bendaharanya Salsa. Pasti uangnya dipakai.",
                    "Alif|\"Pasti\" itu kata yang berat. Kamu sudah lihat catatannya?",
                    "Ilham|…belum. Tapi semua orang juga bilang begitu di mading.",
                }),
                Board(NoticeBoard, "Periksa mading kampus", new[] { "Alif (Batin)|Aku baca dulu apa yang sebenarnya tertulis." },
                    Inspect(new[] { "Kertas tuduhan", "Pengumuman iuran", "Poster seminar", "Tanggal kegiatan" },
                        new[]
                        {
                            "\"BENDAHARA MAKAN UANG KAS\" — tulisan tangan, tanpa nama penulis, tanpa satu angka pun.",
                            "Iuran seminar: 40 anggota × Rp25.000 = Rp1.000.000.",
                            "Seminar terlaksana: ada konsumsi, sewa ruang, dan cetak materi.",
                            "Seminar selesai tiga minggu lalu. Laporan memang belum pernah ditempel.",
                        }),
                    new[] { "Alif (Batin)|Dua hal sama-sama benar: laporannya memang terlambat, dan tuduhannya memang tanpa bukti." }).With(logic: 3),
                Talk(Salsa, "Dengarkan Salsa", new[]
                {
                    "Salsa|Aku lihat tulisan itu tadi pagi. Rasanya mau berhenti kuliah saja.",
                    "Salsa|Semua notanya ada di kardus ini. Aku belum sempat merekap karena ujian… dan aku malah nombok tiga puluh ribu dari uangku sendiri.",
                    "Alif|Kalau begitu kita rekap sekarang. Catatan yang rapi adalah pembelaan terbaikmu.",
                }).With(bond: 1),
                Board(Salsa, "Pilah nota di kardus", new[] { "Salsa|Ini campur aduk, maaf ya. Ada nota pribadiku juga yang keselip." },
                    Sort("sort", "Pilih nota di kiri, lalu kelompokkan di kanan.",
                        new[] { "Konsumsi seminar • 450.000", "Sewa ruang • 250.000", "Cetak materi • 120.000", "Pulsa pribadi Salsa • 50.000", "Makan siang pribadi • 25.000", "Talangan Salsa untuk spanduk • 30.000" },
                        new[] { "Pengeluaran kegiatan", "Bukan beban kas", "Utang kas kepada Salsa" },
                        new[] { 0, 0, 0, 1, 1, 2 },
                        new[]
                        {
                            "Konsumsi adalah pengeluaran kegiatan yang sah dan ada notanya.",
                            "Sewa ruang tercatat atas nama himpunan.",
                            "Cetak materi sesuai jumlah peserta.",
                            "Pulsa pribadi dikeluarkan dari rekap — dan memang tidak dibayar dari kas.",
                            "Makan pribadi bukan beban kas.",
                            "Salsa menalangi spanduk dari uangnya sendiri: kas justru berutang kepadanya.",
                        }),
                    new[] { "Salsa|Jadi… aku tidak memakai uang kas sepeser pun. Malah kas yang berutang padaku." }).With(logic: 4, sharia: 2),
                Board(Salsa, "Susun laporan kas", new[] { "Alif|Sekarang angkanya kita pasangkan, supaya siapa pun bisa memeriksa." },
                    Sort("match", "Pilih rincian di kiri, lalu pasangkan dengan baris laporannya.",
                        new[] { "40 anggota × 25.000", "450.000 + 250.000 + 120.000 + spanduk 30.000", "Pemasukan dikurangi pengeluaran", "Talangan spanduk dari uang Salsa" },
                        new[] { "Pemasukan Rp1.000.000", "Pengeluaran Rp850.000", "Sisa kas Rp150.000", "Dikembalikan ke Salsa Rp30.000" },
                        new[] { 0, 1, 2, 3 },
                        new[]
                        {
                            "Pemasukan tercatat lengkap dengan jumlah anggota.",
                            "Seluruh pengeluaran kegiatan, termasuk spanduk yang ditalangi, berjumlah Rp850.000.",
                            "Sisa kas Rp150.000 masih utuh di amplop kas.",
                            "Talangan dicatat dan dikembalikan: uang pribadi dan uang amanah tidak boleh bercampur.",
                        }),
                    new[]
                    {
                        "Alif|Memegang uang orang banyak itu amanah, dan amanah dijaga dengan catatan yang bisa diperiksa. Terlambat melapor itu kelalaian — tapi bukan pencurian.",
                        "Salsa|Tolong berikan laporan ini ke Ilham. Aku belum sanggup menatap mukanya.",
                    }).With(reward: CashReport, logic: 4, sharia: 3, bond: 1),
                Give(Ilham, "Serahkan laporan kas ke Ilham", CashReport,
                    new[] { "Ilham|Pemasukan sejuta, keluar delapan ratus lima puluh, sisa seratus lima puluh… dan Salsa malah nombok?" },
                    new[]
                    {
                        "Alif|Menagih laporan itu hakmu. Menuduh tanpa bukti di depan umum itu lain soal — nama baik orang juga harta yang harus dijaga.",
                        "Ilham|Kertas itu… aku yang tempel. Aku cabut sekarang, dan aku minta maaf langsung ke Salsa.",
                        "Ilham|Mulai bulan ini laporan kas kita tempel di mading. Biar tidak ada lagi yang menebak-nebak.",
                    }).With(sharia: 4, bond: 2)),

            // ─────────────── HARI 6 · Jalan Kafe ───────────────
            Case(6, "case.upah", "Upah yang Ditunda", "Ijarah: upah jelas & tepat waktu",
                "Barista paruh waktu Kafe Senja belum dibayar tiga minggu karena kas kafe bercampur uang pribadi.", Gilang,
                "Alif (Batin)|Kafe Senja di Jalan Kafe kabarnya tutup sejak siang kemarin. Ada pemuda yang menunggu di depannya.",
                Talk(Gilang, "Dengarkan Gilang", new[]
                {
                    "Gilang|Aku barista paruh waktu di Kafe Senja. Sudah tiga minggu upahku belum turun. Uang kosku jatuh tempo lusa.",
                    "Gilang|Mas Bayu orangnya baik, makanya aku sungkan menagih. Dulu cuma bilang \"nanti gampang diatur\".",
                    "Alif|Jadi besaran dan tanggal upahnya tidak pernah disepakati?",
                    "Gilang|Nggak pernah. Salahku juga sih, nggak tanya.",
                }).With(bond: 1),
                Talk(SideQuestContent.Bayu, "Bicara dengan Mas Bayu", new[]
                {
                    "Mas Bayu|Gilang belum dibayar? Astagfirullah… aku bukan mau menahan, Mas. Aku sendiri bingung uang kafe ke mana.",
                    "Mas Bayu|Pemasukan ada tiap hari. Tapi tiap mau bayar upah, lacinya kosong.",
                    "Alif|Boleh kita buka laci kasnya bersama-sama?",
                }),
                Board(SideQuestContent.Bayu, "Periksa laci kas kafe", new[] { "Mas Bayu|Silakan. Malu sebenarnya, isinya berantakan." },
                    Inspect(new[] { "Isi laci kas", "Nota belanja", "Buku catatan", "Kesepakatan kerja Gilang" },
                        new[]
                        {
                            "Uang penjualan, uang belanja rumah, dan uang jajan anak ada di laci yang sama.",
                            "Nota biji kopi bercampur nota listrik rumah dan cicilan motor.",
                            "Buku catatan berhenti di bulan lalu. Tidak ada catatan upah sama sekali.",
                            "Tidak ada kesepakatan tertulis: besaran upah dan tanggal bayar hanya \"nanti diatur\".",
                        }),
                    new[] { "Alif (Batin)|Kafenya tidak rugi. Uangnya hanya tidak pernah dipisahkan — dan upah tidak pernah dijanjikan dengan jelas." }).With(logic: 3),
                Board(SideQuestContent.Bayu, "Pisahkan isi laci", new[] { "Mas Bayu|Ajari aku memilahnya, Mas. Dari mana mulainya?" },
                    Sort("sort", "Pilih pos di kiri, lalu kelompokkan di kanan.",
                        new[] { "Upah Gilang tiga minggu", "Belanja biji kopi & susu", "Listrik kafe", "Listrik rumah", "Cicilan motor pribadi", "Gaji Mas Bayu sebagai pengelola" },
                        new[] { "Kewajiban kafe — dahulukan", "Biaya usaha", "Uang pribadi" },
                        new[] { 0, 1, 1, 2, 2, 2 },
                        new[]
                        {
                            "Upah pekerja adalah utang yang paling didahulukan: tenaganya sudah dipakai.",
                            "Bahan baku dibayar dari kas kafe.",
                            "Listrik kafe adalah biaya usaha.",
                            "Listrik rumah dibayar dari uang pribadi, bukan laci kafe.",
                            "Cicilan pribadi tidak boleh mengambil kas usaha.",
                            "Pemilik pun sebaiknya menggaji dirinya dengan angka tetap, lalu berhenti mengambil dari laci.",
                        }),
                    new[]
                    {
                        "Alif|Upah itu harus jelas besarnya sejak awal akad, dan dibayar begitu pekerjaan selesai — \"sebelum kering keringatnya\", begitu pesan Nabi.",
                        "Mas Bayu|Ternyata uang upahnya ada, cuma terpakai cicilan motor. Ini kutulis surat perjanjian kerja: upah per jam, dibayar tiap tanggal satu. Tolong berikan ke Gilang bersama upahnya.",
                    }).With(reward: WorkAgreement, logic: 4, sharia: 4, bond: 1),
                Give(Gilang, "Serahkan surat perjanjian ke Gilang", WorkAgreement,
                    new[] { "Gilang|Surat perjanjian… upah per jam, tanggal bayar, tanda tangan Mas Bayu. Dan ini upah tiga mingguku, utuh!" },
                    new[]
                    {
                        "Alif|Mas Bayu juga sudah memisahkan kas kafe dari uang rumahnya. Bukan niatnya yang salah, tapi caranya mencatat.",
                        "Gilang|Aku juga belajar: bertanya soal upah di awal itu bukan tidak sopan. Justru supaya sama-sama enak.",
                    }).With(sharia: 3, bond: 2)),

            // ─────────────── HARI 7 · Jalan Pasar ───────────────
            Case(7, "case.bon", "Buku Bon Warung", "Qardh: mencatat utang & memberi tangguh",
                "Bon pelanggan Bima tidak tercatat rapi; ada yang merasa sudah membayar.", Wati,
                "Alif (Batin)|Hari ketujuh di Cempaka. Dari arah lapak Bima di Jalan Pasar terdengar dua orang saling membantah soal bon.",
                Talk(Wati, "Dengarkan Bu Wati", new[]
                {
                    "Bu Wati|Saya sudah bayar bon keripik bulan lalu, Nak. Lima puluh ribu, saya serahkan sendiri ke Bima!",
                    "Bu Wati|Sekarang ditagih lagi. Saya tidak punya bukti apa-apa — dia juga tidak.",
                    "Alif|Kalau dua-duanya tidak punya catatan, dua-duanya bisa sama-sama merasa benar. Saya lihat bukunya dulu, Bu.",
                }).With(bond: 1),
                Talk(SideQuestContent.Bima, "Tanyakan ke Bima", new[]
                {
                    "Bima|Aku nggak bermaksud menagih dua kali. Di bukuku nama Bu Wati masih ada… atau itu Bu Wati yang lain, ya?",
                    "Bima|Aku sendiri lagi belajar soal utang, Mas. Rasanya nggak enak di dua sisi: ditagih nggak enak, menagih juga nggak enak.",
                }),
                Board(SideQuestContent.Bima, "Periksa buku bon Bima", new[] { "Bima|Ini bukunya. Jangan ketawa, ya." },
                    Inspect(new[] { "Nama pengutang", "Tanggal", "Jumlah", "Tanda lunas" },
                        new[]
                        {
                            "Hanya nama panggilan: \"Bu Wati\" muncul dua kali, tanpa keterangan yang mana.",
                            "Tidak ada tanggal utang maupun tenggat bayar.",
                            "Sebagian jumlah ditulis, sebagian hanya \"keripik 3\". Tidak ada total berjalan.",
                            "Tanda lunas berupa coretan. Tidak ada paraf pembeli maupun penjual.",
                        }),
                    new[] { "Alif (Batin)|Tidak ada yang berbohong di sini. Bukunya saja yang tidak bisa menjawab siapa pun." }).With(logic: 3),
                Board(SideQuestContent.Bima, "Susun aturan buku bon", new[] { "Bima|Kalau bikin buku baru, apa saja yang harus ada? Dan kalau ada yang telat bayar, boleh nggak kutambahi sedikit?" },
                    Sort("sort", "Pilih aturan di kiri, lalu kelompokkan di kanan.",
                        new[] { "Nama lengkap, tanggal, jumlah, dan tenggat", "Paraf kedua pihak saat berutang dan saat lunas", "Memberi kelonggaran bagi yang sedang kesulitan", "Merelakan sebagian sebagai sedekah", "Menambah denda karena telat bayar", "Menaikkan harga khusus bagi yang pernah menunggak" },
                        new[] { "Wajib dicatat", "Dianjurkan", "Tidak boleh" },
                        new[] { 0, 0, 1, 1, 2, 2 },
                        new[]
                        {
                            "Utang dianjurkan ditulis lengkap: siapa, kapan, berapa, sampai kapan (QS Al-Baqarah: 282).",
                            "Paraf menjadi saksi sederhana yang melindungi dua pihak.",
                            "Memberi tangguh kepada yang kesulitan adalah kebaikan (QS Al-Baqarah: 280).",
                            "Merelakan utang adalah pilihan mulia pemberi utang — bukan kewajiban.",
                            "Tambahan atas utang karena terlambat adalah riba, sekecil apa pun.",
                            "Harga hukuman bagi penunggak adalah tambahan terselubung atas utangnya.",
                        }),
                    new[]
                    {
                        "Alif|Mencatat utang itu bukan tanda tidak percaya. Justru supaya persaudaraan tidak rusak gara-gara lupa.",
                        "Bima|Untuk bon Bu Wati yang tidak jelas itu, kuanggap lunas saja. Ini buku bon baruku — tolong minta Bu Wati memaraf halaman pertamanya.",
                    }).With(reward: DebtBook, logic: 3, sharia: 5, bond: 1),
                Give(Wati, "Minta Bu Wati memaraf buku bon baru", DebtBook,
                    new[] { "Bu Wati|Buku baru? Ada tanggal, jumlah, tenggat, kolom paraf… nah, begini baru enak." },
                    new[]
                    {
                        "Bu Wati|Bon lama dianggap lunas? Alhamdulillah. Mulai sekarang saya paraf tiap belanja, biar Bima juga tenang.",
                        "Alif (Batin)|Harga, wakaf, kembalian, kas, upah, utang. Enam persoalan, satu akarnya: sesuatu yang tidak pernah dibuat jelas dan dicatat.",
                        "Alif (Batin)|Sebaiknya aku kabari Bu Siti. Beliau yang pertama mengajariku soal ini.",
                    }).With(sharia: 3, bond: 2)),
        };
    }
}
