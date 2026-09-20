using System;
namespace Alif.Adventure
{
    public static class AdventureContent
    {
        public const string MuiUrl = "https://mui.or.id/public/baca/berita/hindari-7-hal-dalam-transaksi-dengan-uang-elektronik";
        public const string CreditUrl = "https://mui.or.id/public/baca/bimbingan/praktik-kredit-dalam-sudut-pandang-islam";
        public const string OjkUrl = "https://www.ojk.go.id/waspada-investasi/id/FAQ.aspx";
        /// <summary>Map Kota Cempaka yang ada di setiap bab, ditambahkan setelah area bab
        /// (dirakit runtime oleh CityWorld dari Resources/Kota/city.json; urutan harus sama).</summary>
        public static readonly string[] CityAreas = { "Jalan Pasar", "Jalan Kafe", "Pusat Kota", "Kampus Cempaka", "Taman Cempaka",
            // Interior — dimasuki lewat pintu (doors di city.json), bukan dari peta HP.
            "Lobi Kampus", "Kelas Ekonomi", "Ruang Dosen", "Toilet Kampus", "Gereja Kasih Sejati", "Puskesmas Cempaka",
            "Minimarket 24", "GG Learning Center", "FFC", "Masjid Al-Amanah", "Toko Glow", "Bank Syariah", "Idmaret", "Kafe Senja", "Kamar Alif" };
        public static int SceneAreaCount(AdventureChapter chapter) => chapter.Areas.Length - CityAreas.Length;
        static PuzzleStep S(string p, string e, string why, int answer, params string[] options) => new PuzzleStep(p,e,why,answer,options);
        static AdventureTask T(string id, int area, string target, string speaker, string title, string intro, string outcome, string kind = "talk", params PuzzleStep[] steps)
            => new AdventureTask { Id=id, Area=area, Target=target, Speaker=speaker, Title=title, Introduction=intro, Outcome=outcome, Kind=kind, Steps=steps };
        public static AdventureChapter Get(int number)
        {
            var c = number == 1 ? One() : number == 2 ? Two() : number == 3 ? Three() : number == 4 ? Four() : Five();
            c.Number=number;
            if (number == 1)
            {
                Array.Find(c.Tasks, t => t.Id == "c1.resolve").Cost = 18000;
                Array.Find(c.Tasks, t => t.Id == "c1.meal").SkipMinutes = 10;   // waktu makan
                Array.Find(c.Tasks, t => t.Id == "c1.rent").Cost = 40000;       // sewa malam pertama
                c.StartHour = 13;                                               // turun dari kereta siang hari
            }
            if (number == 1) Array.Find(c.Tasks, t => t.Id == "c1.finale").NeedsCases = true;
            for(int i=1;i<c.Tasks.Length;i++) c.Tasks[i].Prerequisites=new[]{c.Tasks[i-1].Id};
            OriginalCampaign.Apply(c);
            if (number >= 2)
            {
                string before = number == 2 ? "c2.send" : number == 3 ? "c3.close" : number == 4 ? "c4.label" : "c5.decline";
                var tasks = new System.Collections.Generic.List<AdventureTask>(c.Tasks);
                int index = tasks.FindIndex(t => t.Id == before);
                var resolution = tasks[index];
                tasks.Insert(index, T("c" + number + ".encounter", resolution.Area, resolution.Target, resolution.Speaker,
                    "Hadapi godaan — " + c.Areas[resolution.Area],
                    "Bukti sudah terkumpul. Kini Alif menghadapi gambaran godaan dalam pikirannya, bukan menyerang warga. Kamu dapat mencoba lagi tanpa kehilangan uang atau bukti.",
                    "Godaan teratasi. Gunakan bukti dan lanjutkan penyelesaian bersama.", "encounter"));
                c.Tasks = tasks.ToArray();
                for (int i = 1; i < c.Tasks.Length; i++) c.Tasks[i].Prerequisites = new[] { c.Tasks[i - 1].Id };
            }
            if (number == 1)
            {
                // Apply() memetakan semua tugas bab 1 ke area scene; tugas sewa terjadi di kota,
                // di trotoar depan kos, jadi areanya diperbaiki setelah pemetaan itu.
                var rent = Array.Find(c.Tasks, t => t.Id == "c1.rent");
                int street = Array.IndexOf(c.Areas, "Jalan Pasar");
                if (rent != null && street >= 0)
                {
                    rent.Area = street;
                    rent.Title = rent.Title.Split('—')[0].Trim() + " — " + c.Areas[street];
                }
            }
            return c;
        }
        // Bab 1 = Hari 1 (tutorial + struk Bu Siti) lalu enam Kasus Warga di CaseContent yang terbuka
        // per hari cerita. "c1.room" memberi Alif kamar kos (tempat tidur = ganti hari); "c1.finale"
        // baru bisa dikerjakan setelah semua kasus wajib selesai (AdventureTask.NeedsCases).
        static AdventureChapter One() => new AdventureChapter {
            Title="Cempaka, Minggu Pertama", Subtitle="01 / Belajar bertanya", SleepAfter="c1.room",
            Areas=new[]{"Stasiun Cempaka", "Gang warung", "Warung Bu Siti"},
            Introduction="Alif baru tiba di Cempaka. Di dalam tasnya ada buku catatan dan uang untuk memulai hari. Sebelum mencari tempat menginap, ia perlu makan. Di kota ini setiap sudut punya ceritanya sendiri—dan hampir semuanya bermula dari sesuatu yang tidak pernah dibuat jelas.",
            Ending="Tujuh hari di Cempaka: papan harga terpasang, taman wakaf terjaga, kembalian utuh, kas himpunan terlapor, upah terbayar, dan buku bon berparaf. Alif menutup halaman pertama buku catatannya: kejujuran sering dimulai dari hal kecil—lalu dicatat.",
            Optional=new[]{"Petugas stasiun|Warung Bu Siti ada di sebelah timur. Di kampung ini, orang masih saling menitipkan kabar lewat warung. Papan arah akan membantumu pulang juga.","Raka|Aku sedang mencari kerja sambilan. Kadang malu mengaku uangku terbatas. Terima kasih sudah bertanya sebelum menghakimi.","Bu Siti|Aku menulis harga besar-besar supaya setiap orang bisa memilih dengan tenang. Kalau ada yang tidak jelas, tanyakan sebelum memesan."},
            Tasks=new[]{
                T("c1.arrival",0,"Papan arah","Alif","Baca petunjuk — papan stasiun","Papan menunjukkan jalan keluar stasiun dan arah Warung Bu Siti di sebelah timur. Periksa arah sebelum melanjutkan perjalanan.","Warung berada di timur. Ikuti penanda KELUAR menuju gang, lalu MASUK Warung Bu Siti."),
                T("c1.menu",2,"Papan menu","Bu Siti","Susun pesanan — papan menu","Selamat datang, Nak! Harga di papan berlaku untuk semua. Sisihkan dulu uang perjalananmu, lalu pilih makanan dan minuman tanpa melewati Rp20.000. Jangan sungkan bertanya sebelum memesan.","Pesanan dicatat: nasi telur dan es teh, total Rp18.000. Belum ada uang yang dipotong; struknya menunggu di meja dekat jendela.","budget",
                    S("Pilih lauk agar es teh Rp6.000 tetap masuk anggaran.","ANGGARAN 20.000  |  Minuman 6.000  |  Batas lauk 14.000","Nasi telur Rp12.000 menyisakan Rp8.000, cukup untuk minuman Rp6.000.",1,"Ayam geprek • 18.000","Nasi telur • 12.000","Paket spesial • 22.000"),
                    S("Masukkan minuman sesuai pesanan.","Nasi telur 12.000  +  ?  =  total maksimal 20.000","Es teh Rp6.000 membuat total Rp18.000; masih ada Rp2.000 dari anggaran makan.",2,"Jus • 10.000","Kopi susu • 12.000","Es teh • 6.000"),
                    S("Pisahkan sisa anggaran makan.","20.000 − 12.000 − 6.000 = ?","Sisa Rp2.000 tetap milik Alif. Tidak perlu dihabiskan hanya karena sudah dianggarkan.",0,"Simpan 2.000","Tambah kerupuk 4.000","Anggap tidak ada sisa"),
                    S("Konfirmasi harga sebelum pesanan dibuat.","Nasi telur 12.000 / Es teh 6.000 / Tidak ada biaya lain","Kesepakatan pesanan memuat barang, jumlah, dan total yang jelas.",1,"Bayar berapa saja nanti","Pesan dua item • total 18.000","Minta paket tanpa harga")),
                // Urutan warung mengikuti alur nyata: lihat papan menu → duduk & makan (adegan
                // Bu Siti mengantar pesanan) → baru struknya ketahuan keliru saat membayar.
                T("c1.meal",2,"Meja jendela","Bu Siti","Nikmati pesananmu — meja jendela","Pesanan datang ke meja: nasi telur hangat dan es teh. Makan dulu, tidak perlu buru-buru.","Makan selesai. Di kasir ada tamu yang bicara keras soal pinjaman — bayar dulu, lalu urus itu."),
                // Dua baris keliru saja — baris yang sudah benar dan baris total dulu ikut dikartukan,
                // dan itu membuat papannya terasa berat tanpa menambah pelajaran apa pun.
                T("c1.receipt",2,"Bu Siti","Alif","Periksa struk — kasir","Di kasir, struknya tertulis Rp28.000 padahal pesananmu Rp18.000. Ada dua baris yang tidak cocok — pasangkan tiap baris itu dengan tindakan yang benar sebelum membayar.","Struk diperbaiki: nasi telur Rp12.000 + es teh Rp6.000 = Rp18.000.","match",
                    S("Periksa jumlah minuman.","Es teh ditulis 2 x 6.000, padahal kamu pesan 1 gelas","Jumlahnya dikoreksi jadi satu; selisihnya Rp6.000.",0,"Ubah jumlahnya jadi satu","Biarkan saja"),
                    S("Periksa baris tambahan.","Kerupuk 4.000 tertulis, padahal tidak dipesan","Barang yang tidak dipesan diminta dihapus dari tagihan, bukan dibayar supaya cepat.",0,"Minta hapus dari struk","Bayar saja supaya cepat")),
                T("c1.resolve",2,"Bu Siti","Alif","Selesaikan pembayaran — kasir","Bawa struk ke kasir dan pakai catatan yang sama dengan Bu Siti. Sebutkan item yang berbeda, dengarkan penjelasannya, lalu sepakati koreksi. Bayar hanya setelah rinciannya cocok.","Alif membayar Rp18.000 sekali. Bu Siti meminta maaf atas struk yang tertukar dan berjanji membacakan ulang setiap pesanan sebelum menagih.","sort",
                    S("Mulai percakapan dari fakta.","BUKTI: struk awal mencantumkan item yang tidak dipesan","Membandingkan pesanan dengan struk memberi ruang untuk memperbaiki kesalahan tanpa menuduh niat seseorang.",1,"Bu Siti pasti sengaja menipu","Mari cocokkan pesanan dan struk","Jangan bayar apa pun"),
                    S("Pilih koreksi yang dapat diperiksa.","Pesanan: 18.000 / Tagihan awal: 28.000 / Selisih: 10.000","Struk baru harus memuat dua item dan total Rp18.000.",0,"Tulis ulang dua item • 18.000","Coret total tanpa rincian","Minta semuanya gratis"),
                    S("Tentukan kembalian dari pembayaran Rp20.000.","Dibayar 20.000 − tagihan 18.000","Kembalian Rp2.000 sesuai kesepakatan. Dalam saldo permainan, hanya biaya bersih Rp18.000 yang dicatat.",2,"Tidak perlu kembalian","Kembalian 10.000","Kembalian 2.000"),
                    S("Tutup percakapan dengan kesepakatan.","Struk sudah benar; Bu Siti berterima kasih sudah diingatkan","Simpan struk dan akui koreksi. Masalah selesai tanpa mempermalukan salah satu pihak.",0,"Terima koreksi dan simpan struk","Sebarkan tuduhan lama","Minta pembeli lain ikut membayar")),
                // Kejadian utama Hari 1: tawaran pinjaman Raka ke Bu Siti — pembuka Bab 2 (Jebakan Riba).
                T("c1.offer",2,"Bu Siti","Alif","Bongkar tawaran pinjaman — kasir","Map yang ditinggalkan Raka masih di meja kasir. Pisahkan mana syarat yang wajar dan mana tanda bahaya, supaya Bu Siti memutuskan dengan mata terbuka, bukan karena terdesak.","Bu Siti menahan tanda tangannya. Tawaran itu ditolak untuk sekarang; Alif mencatat cirinya di buku perjalanan sebagai bekal menghadapi kasus serupa.","sort",
                    S("Periksa bunganya.","Bunga 10% per hari, dihitung harian","Bunga harian menggandakan utang dalam hitungan minggu — ini tanda pinjaman menjerat.",0,"Tanda bahaya","Syarat yang wajar"),
                    S("Periksa jaminannya.","KTP ditahan sebagai jaminan","Menahan identitas bukan jaminan yang sah; itu alat menekan, bukan pengaman pinjaman.",0,"Tanda bahaya","Syarat yang wajar"),
                    S("Periksa perjanjiannya.","Tidak ada surat yang boleh dibaca sebelum tanda tangan","Akad yang tidak boleh dibaca menutup hak orang untuk tahu apa yang ia sepakati.",0,"Tanda bahaya","Syarat yang wajar"),
                    S("Periksa hak meninjau.","Boleh dibawa pulang dan dibaca dulu sebelum menyetujui","Waktu untuk membaca dan bertanya adalah syarat paling dasar dari kesepakatan yang adil.",1,"Tanda bahaya","Syarat yang wajar")),
                T("c1.room",2,"Bu Siti","Bu Siti","Tanyakan tempat menginap — kasir","Kamu belum punya tempat menginap, kan? Sepupu saya punya rumah kos di ujung timur Jalan Pasar—rumah bergenteng paling kanan. Kamar depannya kosong. Bilang saja dari Bu Siti.","Alamat kamar kos dicatat: rumah paling kanan di Jalan Pasar (lihat peta di HP). Tidur di ranjang untuk mengakhiri hari—tiap pagi ada kabar warga yang baru."),
                // Sewa kamar: Bu Tini membuka Rp50.000 semalam. Yang dilatih bukan menawar sekeras
                // mungkin, tapi menawar dengan cara yang sehat — bertanya, membandingkan, minta kuitansi.
                // Biayanya dipotong saat tugas selesai, jadi kamar baru terbuka setelah benar-benar dibayar.
                T("c1.rent",2,"Bu Tini","Bu Tini","Sewa kamar — depan kos","Kamar depan masih kosong, Nak. Lima puluh ribu semalam, dibayar tiap kali menginap. Kalau mau menawar, silakan—asal caranya baik.","Bu Tini setuju Rp40.000 per malam dengan kuitansi tertulis tiap bayar. Sewa malam ini lunas; berikutnya ditagih setiap Alif tidur.","sort",
                    S("Tanyakan dulu isinya.","Tanya apa saja yang sudah termasuk: listrik, air, kamar mandi","Tahu dulu apa yang dibayar, baru bicara harga.",0,"Cara menawar yang sehat","Sebaiknya dihindari"),
                    S("Pakai pembanding yang nyata.","Sebut harga kamar sebelah yang Rp40.000 sebagai pembanding","Menawar dengan pembanding yang bisa dicek itu wajar, bukan menekan.",0,"Cara menawar yang sehat","Sebaiknya dihindari"),
                    S("Kunci kesepakatannya.","Minta kuitansi tertulis tiap kali membayar","Kesepakatan yang dicatat melindungi dua pihak, persis seperti struk di warung tadi.",0,"Cara menawar yang sehat","Sebaiknya dihindari"),
                    S("Periksa janji yang belum tentu sanggup.","Janji bayar sebulan di muka padahal uangnya belum ada","Janji yang tidak sanggup ditepati adalah utang baru, bukan cara menawar.",1,"Cara menawar yang sehat","Sebaiknya dihindari")),
                T("c1.finale",2,"Bu Siti","Bu Siti","Kabari Bu Siti — kasir","Seminggu ini namamu sering disebut orang, Nak. Pak Yanto memasang papan harga, taman wakaf aman, Bima punya buku bon baru. Semuanya berawal dari hal yang sama dengan struk kita dulu: dibuat jelas, lalu dicatat.","Bu Siti menitipkan kabar: Dimas, mahasiswa di Kos Cempaka lantai dua, sedang bingung menghadapi tawaran pinjaman. Bawa catatanmu. Bab berikutnya terbuka: Jebakan Riba.")
            }
        };
        static AdventureChapter Two() => new AdventureChapter {
            Title="Jebakan Riba", Subtitle="02 / Ruang untuk berpikir",
            Areas=new[]{"Halaman kos", "Ruang bersama", "Kamar Dimas • lantai 2"},
            Introduction="Di Kos Cempaka, Dimas menatap penawaran pinjaman di telepon. Biaya kuliah datang bersamaan dengan kebutuhan sehari-hari. Alif datang untuk mendengarkan dan membantu membaca angka, bukan memutuskan hidup Dimas untuknya.",
            Ending="Dimas menyimpan rencana tertulis dan menutup penawaran yang membebaninya. Bantuan kampus belum pasti, tetapi kebutuhan pokok sudah dilindungi dan pembicaraan dengan keluarga sudah dimulai. Mereka sepakat mengecek kembali kabarnya bersama.",
            Optional=new[]{"Ibu kos|Papan di ruang bersama memuat jadwal dan kontak bantuan. Informasi yang jelas sering lebih berguna daripada janji cepat.","Ustadz Farid|Jangan menyamakan semua jual beli bertempo dengan pinjaman berbunga. Jenis akad dan syarat perlu dibaca. Jika belum paham, tanyakan kepada pihak yang kompeten.","Dimas|Aku takut dianggap tidak mampu mengurus diri. Saat kamu mendengarkan dulu, aku lebih berani membuka catatan yang selama ini kusembunyikan."},
            Tasks=new[]{
                T("c2.arrival",0,"Papan arah","Alif","Cari kamar Dimas — papan kos","Papan menunjukkan ruang bersama di sebelah kanan, lalu tangga ke kamar Dimas. Setiap pintu mencantumkan tempat tujuan dan jalan kembali.","Ikuti pintu menuju ruang bersama, kemudian naik ke kamar Dimas."),
                T("c2.listen",2,"Dimas","Dimas","Dengarkan kebutuhan — kamar Dimas","Aku punya Rp300.000. Biaya makan Rp140.000, transport Rp60.000, dan kebutuhan kuliah Rp200.000. Iklan ini menjanjikan uang cair hari ini. Aku belum membaca semuanya.","Kekurangan dihitung dari kebutuhan nyata; angka dalam aktivitas adalah simulasi, terpisah dari dompet Alif."),
                T("c2.expenses",2,"Buku anggaran","Alif","Kelompokkan kebutuhan — buku Dimas","Letakkan setiap kartu ke kategori yang tepat. Contoh: ongkos menuju kelas termasuk kebutuhan pokok; hiasan kamar dapat ditunda. Melindungi kebutuhan bukan berarti mengabaikan kesenangan selamanya.","Kebutuhan mendesak Rp400.000. Dana tersedia Rp300.000, sehingga selisihnya Rp100.000.","sort",
                    S("Tempatkan biaya makan minggu ini.","MAKAN 140.000 / Tanpa makanan, aktivitas harian terganggu","Makan masuk kebutuhan pokok yang dilindungi.",0,"Kebutuhan pokok","Bisa ditunda","Pendapatan"),
                    S("Tempatkan biaya transport kuliah.","TRANSPORT 60.000 / Dipakai menuju kelas minggu ini","Transport untuk mengikuti kelas termasuk kebutuhan pokok dalam situasi Dimas.",0,"Kebutuhan pokok","Hiburan","Pendapatan"),
                    S("Tempatkan dekorasi kamar.","DEKORASI 80.000 / Kamar tetap dapat digunakan tanpa pembelian ini","Dekorasi dapat ditunda agar uang tidak mengambil porsi kebutuhan mendesak.",1,"Wajib hari ini","Bisa ditunda","Bantuan tersedia"),
                    S("Hitung kekurangan yang perlu dibicarakan.","140.000 + 60.000 + 200.000 − 300.000","Kekurangan Rp100.000, bukan seluruh Rp400.000. Mengetahui selisih menghindari pinjaman berlebihan.",2,"400.000","200.000","100.000")),
                T("c2.ask",1,"Ustadz Farid","Ustadz Farid","Tanyakan syarat — ruang bersama","Baca uang yang diterima, total yang wajib dikembalikan, dan tambahan karena waktu. Dalam contoh utang ini, tambahan atas pokok disyaratkan untuk pemberi pinjaman. Mari periksa angkanya, bukan hanya nama produknya.","Catat pokok, potongan, jadwal, dan total pembayaran sebelum menyetujui apa pun."),
                T("c2.terms",2,"Telepon Dimas","Alif","Uraikan penawaran — telepon Dimas","Contoh perhitungan: dua cicilan Rp60.000 berarti total Rp120.000, bukan Rp60.000. Susun rincian penawaran Dimas dari dokumen yang terlihat.","Penawaran tidak diterima. Dimas memahami perbedaan uang cair, pokok tercatat, dan total pembayaran.","match",
                    S("Cocokkan uang cair dengan rincian.","Pokok tercatat 200.000 / Potongan di muka 20.000","Uang yang benar-benar diterima hanya Rp180.000. Potongan perlu dibaca terpisah dari pokok.",1,"Cair 220.000","Cair 180.000","Cair 200.000"),
                    S("Hubungkan cicilan dengan total pembayaran.","Tiga pembayaran, masing-masing 80.000","3 × Rp80.000 menghasilkan total Rp240.000.",2,"Total 80.000","Total 160.000","Total 240.000"),
                    S("Temukan tambahan yang disyaratkan atas pokok utang.","Pokok 200.000 / Wajib kembali 240.000 / Tambahan untuk pemberi pinjaman karena tempo","Dalam contoh akad utang ini, Rp40.000 tambahan disyaratkan atas pokok karena waktu; itulah persoalan riba yang diperiksa.",0,"Tambahan 40.000","Tambahan 0","Pokok 240.000"),
                    S("Bandingkan kewajiban dengan uang yang diterima.","Terima 180.000 / Bayar 240.000","Selisih arus kas Rp60.000 memperlihatkan seluruh beban contoh ini, termasuk potongan di muka.",1,"Beban hanya cicilan pertama","Selisih 60.000; minta seluruh syarat","Tidak perlu membaca potongan")),
                T("c2.support",1,"Papan bantuan","Alif","Periksa bantuan — papan ruang bersama","Dalam simulasi ini, keluarga bersedia membantu Rp100.000 tanpa pengembalian. Kampus menerima permohonan penjadwalan, tetapi belum menyetujui. Kerja sambilan baru membayar bulan depan.","Pisahkan dukungan yang telah dikonfirmasi dari harapan yang belum pasti."),
                T("c2.plan",2,"Buku anggaran","Dimas","Susun rencana — meja Dimas","Susun dana yang tersedia dan bantuan terkonfirmasi. Contoh: Rp50.000 yang baru dijanjikan tanpa kepastian tidak boleh dianggap sudah masuk rekening.","Rencana siap: dana Rp300.000 + bantuan keluarga Rp100.000 menutup kebutuhan Rp400.000 tanpa penawaran tadi.","budget",
                    S("Pilih sumber untuk menutup kekurangan minggu ini.","Kurang 100.000 / Keluarga: 100.000 terkonfirmasi / Kerja: bulan depan","Bantuan keluarga Rp100.000 yang terkonfirmasi menutup selisih dalam simulasi ini.",2,"Hadiah undian","Upah yang belum diterima","Bantuan keluarga 100.000"),
                    S("Lindungi makan dan transport.","Dana 400.000 / Makan 140.000 / Transport 60.000","Sisihkan Rp200.000 untuk makan dan transport sebelum mengalokasikan sisanya.",0,"Sisihkan 200.000","Sisihkan 20.000","Belanjakan semua untuk dekorasi"),
                    S("Alokasikan sisa untuk kebutuhan kuliah.","Dana 400.000 − makan dan transport 200.000","Sisa Rp200.000 menutup kebutuhan kuliah dalam skenario ini.",1,"Kuliah 300.000","Kuliah 200.000","Kuliah 0"),
                    S("Catat status permohonan kampus dengan jujur.","Permohonan diterima petugas; persetujuan belum keluar","Belum disetujui berarti belum pasti. Rencana utama tidak bergantung pada dana yang belum ada.",0,"Menunggu; belum dihitung sebagai dana","Pasti cair hari ini","Abaikan kabar berikutnya")),
                T("c2.send",2,"Dimas","Dimas","Sampaikan rencana — kamar Dimas","Aku akan menghubungi keluarga untuk mengonfirmasi waktu bantuan dan menjelaskan rinciannya. Permohonan kampus tetap kutindaklanjuti. Jika keadaan berubah, aku akan meminta bantuan lagi, bukan menyembunyikannya.","Dimas menyimpan rencana dan menolak penawaran. Percakapan ini simulasi; tidak ada pesan yang dikirim ke luar game.")
            }
        };
        static AdventureChapter Three() => new AdventureChapter {
            Title="Ilusi Maysir", Subtitle="03 / Peluang bukan rencana",
            Areas=new[]{"Halaman kos", "Lapak promosi", "Balai kegiatan"},
            Introduction="Raka menemukan promosi tiket berhadiah di halaman kos. Poster besar memperlihatkan satu pemenang yang tersenyum. Ia berharap hadiah itu bisa menyelesaikan semuanya. Alif mengajaknya melihat apa yang tidak tertulis besar di poster.",
            Ending="Raka menyimpan uang kebutuhannya. Di balai, warga menikmati sore bertukar buku dengan biaya yang jelas. Ia menemukan sesuatu yang tidak dijanjikan poster: kesempatan berbuat bersama tanpa mempertaruhkan uang makan.",
            Optional=new[]{"Dimas|Aku dulu juga tertarik pada janji serba cepat. Bertanya bukan berarti meremehkan harapan teman.","Penjaga lapak|Poster hanya memuat pemenang. Catatan peserta ada di meja. Informasi kecil bisa mengubah cara kita melihat iklan besar.","Raka|Aku suka merancang poster. Mungkin kemampuan itu bisa kupakai untuk kegiatan yang manfaatnya benar-benar bisa dinikmati semua orang."},
            Tasks=new[]{
                T("c3.arrival",0,"Raka","Raka","Temui Raka — halaman kos","Satu tiket dua puluh ribu, hadiahnya ratusan ribu. Aku tahu belum tentu menang, tetapi rasanya sayang kalau kesempatan ini lewat.","Periksa syarat di lapak sebelum membayar."),
                T("c3.rules",1,"Poster promosi","Alif","Buka syarat — poster promosi","Pisahkan hadiah gratis dari tiket berbayar yang hangus jika kalah. Contoh: hadiah buku gratis tanpa setoran berbeda dari membeli peluang uang dengan risiko kehilangan setoran.","Pada penawaran ini, peserta membayar hanya untuk peluang hadiah; tiket kalah hangus.","inspect",
                    S("Pilih bagian yang menjelaskan biaya masuk.","POSTER: HADIAH 500.000! / Tiket 20.000 / Syarat berlaku","Tiket Rp20.000 berarti ada uang peserta yang dipertaruhkan dalam penawaran ini.",1,"Judul hadiah","Harga tiket","Warna poster"),
                    S("Temukan apa yang diterima peserta yang kalah.","SYARAT: tiket tidak dapat ditukar barang; tiket kalah hangus","Tiket kalah tidak memberi barang atau manfaat yang dijanjikan; uangnya hilang.",2,"Pasti mendapat barang","Uang kembali","Tiket hangus"),
                    S("Cocokkan sumber hadiah.","CATATAN: hadiah dibayar dari kumpulan setoran tiket peserta","Hadiah berasal dari setoran peserta. Ini memperjelas bentuk pertaruhan dalam kasus ini.",0,"Setoran peserta","Hadiah gratis sponsor","Hasil penjualan buku"),
                    S("Pilih pembanding yang berbeda dari taruhan ini.","A: tiket uang berbayar / B: pembagian buku gratis tanpa pembayaran atau taruhan","Hadiah gratis tanpa taruhan tidak sama dengan kasus tiket berbayar yang sedang diperiksa.",1,"Semua hadiah sama saja","Buku gratis tanpa taruhan","Tiket berbayar lebih aman")),
                T("c3.records",1,"Catatan peserta","Alif","Bandingkan hasil — catatan peserta","Baca seluruh catatan, bukan hanya satu cerita sukses. Contoh: satu orang untung tidak berarti semua peserta untung. Hubungkan kartu angka dengan maknanya.","Poster menyorot satu pemenang; catatan memperlihatkan sembilan peserta kehilangan setoran.","match",
                    S("Temukan jumlah peserta dalam catatan contoh.","Daftar A–J: sepuluh peserta / satu tiket per peserta","Ada sepuluh peserta pada contoh ini, bukan satu orang yang terlihat di poster.",0,"10 peserta","1 peserta","100 peserta"),
                    S("Hitung setoran semua peserta.","10 peserta × tiket 20.000","Setoran bersama Rp200.000. Angka ini membantu membaca arus uang contoh, bukan memperkirakan peluang undian lain.",2,"20.000","100.000","200.000"),
                    S("Pasangkan peserta tanpa hadiah.","A menerima hadiah; B, C, D, E, F, G, H, I, J tidak menerima apa pun","Sembilan orang kehilangan setoran. Foto pemenang tidak menceritakan hasil mereka.",1,"Semua untung","9 kehilangan setoran","Tidak ada yang rugi"),
                    S("Baca ajakan setelah kalah.","PESAN PROMOSI: beli lagi agar kerugian kemarin kembali","Pengeluaran sebelumnya tidak menjamin hasil pembelian berikutnya. Berhenti melindungi kebutuhan yang masih tersisa.",0,"Berhenti; hasil berikutnya tidak dijamin","Pasti menang setelah kalah","Tambah tiket sampai habis")),
                T("c3.listen",0,"Raka","Raka","Bicarakan alternatif — halaman kos","Aku ingin tetap punya kegiatan yang seru. Bagaimana kalau kita membuat sore tukar buku? Orang datang membawa buku, dan biaya minum diumumkan jelas.","Bawa rencana ke balai kegiatan. Tidak perlu tiket taruhan untuk berkumpul."),
                T("c3.plan",2,"Meja kegiatan","Alif","Rancang sore warga — meja kegiatan","Anggaran kegiatan Rp100.000, terpisah dari dompet Alif. Pilih kebutuhan yang manfaatnya diterima peserta. Contoh: sewa tikar punya biaya dan penggunaan yang jelas.","Rencana: tikar pinjaman, minum Rp40.000, bahan poster Rp20.000, kebersihan Rp10.000; cadangan Rp30.000.","budget",
                    S("Pilih tempat duduk.","Tikar warga boleh dipinjam gratis / Kursi sewa 80.000","Meminjam tikar dengan izin menyisakan anggaran untuk kebutuhan bersama.",2,"Kursi 80.000","Dekorasi 100.000","Tikar pinjaman 0"),
                    S("Masukkan minuman untuk semua.","Dana 100.000 / Paket air dan teh 40.000","Minuman Rp40.000 punya jumlah dan manfaat yang jelas untuk peserta.",0,"Minuman 40.000","Tiket hadiah 40.000","Minuman tanpa harga"),
                    S("Tambahkan poster dan kebersihan.","Minum 40.000 + poster 20.000 + kebersihan 10.000","Total biaya Rp70.000 menyisakan Rp30.000 cadangan.",1,"Total 100.000","Total 70.000","Total 40.000"),
                    S("Tulis pengumuman biaya dengan jelas.","Kegiatan tukar buku / Biaya yang disepakati dan manfaat diketahui / Tidak ada taruhan","Pengumuman harus menyebut biaya, penggunaan, dan tidak menjanjikan hadiah uang acak.",0,"Rincian biaya dan manfaat terbuka","Bayar untuk peluang kaya","Sembunyikan biaya hingga tiba")),
                T("c3.prepare",2,"Rak buku","Raka","Siapkan kegiatan — rak balai","Buku dipisahkan menurut tema, daftar pinjaman ditulis, dan minuman diletakkan di meja. Aku akan menempel rincian biaya di pintu supaya tidak ada yang merasa terjebak.","Warga mulai datang. Raka tidak membeli tiket; uang kebutuhannya tetap aman."),
                T("c3.close",2,"Raka","Alif","Tutup kegiatan — balai","Kita tidak bisa menjanjikan keberuntungan. Tetapi kita bisa membuat kesepakatan yang jelas dan kegiatan yang menyenangkan. Terima kasih sudah mengubah rencana bersama.","Sore tukar buku selesai. Naya menitipkan kabar tentang barang di bazar warga.")
            }
        };
        static AdventureChapter Four() => new AdventureChapter {
            Title="Sindikat Tadlis", Subtitle="04 / Di balik label",
            Areas=new[]{"Gerbang bazar", "Lapak elektronik", "Meja layanan warga"},
            Introduction="Naya membantu bazar ketika seorang pembeli mengembalikan radio yang disebut baru. Ada retakan di bawah stiker. Sebelum menyimpulkan siapa yang salah, Naya dan Alif mengumpulkan kondisi barang, label, dan catatan transaksi.",
            Ending="Penjual menerima bukti, memperbaiki label, dan menyepakati tindak lanjut keluhan pembeli. Naya mencatat kesepakatan tanpa menyebarkan data pribadi. Kepercayaan diperbaiki melalui tindakan yang bisa diperiksa.",
            Optional=new[]{"Naya|Aku ingin pembeli merasa aman, tetapi aku juga tidak mau membuat tuduhan yang tidak bisa dibuktikan.","Penjual|Catatan servis tersimpan di bawah kotak. Seharusnya kondisi itu dijelaskan sejak awal. Aku bersedia memeriksanya bersama.","Bu Siti|Kesalahan perlu diperbaiki. Menutupinya agar cepat selesai justru membuat masalah berikutnya lebih besar."},
            Tasks=new[]{
                T("c4.arrival",0,"Naya","Naya","Dengarkan keluhan — gerbang bazar","Pembeli mengatakan radio sering mati. Label menyebut baru tanpa cacat. Kita boleh memeriksa dengan izin, menyimpan bukti, lalu meminta penjelasan.","Bawa tiga pertanyaan: bagaimana kondisi barang, apa yang dijanjikan, dan apa bukti transaksinya?"),
                T("c4.inspect",1,"Radio contoh","Alif","Periksa radio — meja lapak","Pilih bagian pada gambar radio untuk membaca temuan. Contoh: foto retakan membuktikan kondisi yang terlihat, tetapi tidak otomatis membuktikan kapan atau siapa yang membuatnya.","Temuan disimpan: retak di bawah stiker, sambungan longgar, dan nomor servis pada casing.","inspect",
                    S("Pilih bagian yang perlu diperiksa setelah stiker diangkat dengan izin.","RADIO / Stiker besar menutupi sudut kanan casing","Retakan di bawah stiker adalah kondisi yang perlu dijelaskan kepada pembeli.",2,"Antena utuh","Tombol volume","Sudut di bawah stiker"),
                    S("Hubungkan gejala mati dengan pemeriksaan aman.","Radio mati saat kabel bergerak / Pemeriksaan dilakukan tanpa membuka bagian listrik","Catat sambungan longgar dan minta pemeriksaan petugas; jangan membongkar bagian listrik sendiri.",0,"Catat kabel longgar","Buka rangkaian listrik","Simpulkan dari warna"),
                    S("Temukan informasi riwayat barang.","Casing belakang memiliki label servis S-014","Nomor servis membantu mencocokkan riwayat perbaikan dengan catatan penjual.",1,"Ukuran kotak","Label servis S-014","Logo merek"),
                    S("Pilih catatan temuan yang jujur.","Foto menunjukkan retak; catatan servis belum dicocokkan","Catat apa yang terlihat dan pisahkan pertanyaan yang belum terjawab dari fakta.",2,"Semua barang bazar palsu","Penjual pasti merusaknya","Retak terlihat; riwayat perlu dicek")),
                T("c4.records",1,"Dokumen barang","Naya","Cocokkan dokumen — meja lapak","Pasangkan klaim dengan bukti yang sesuai. Contoh: nomor pada barang harus sama dengan nomor pada catatan servis sebelum catatan itu dipakai sebagai bukti.","Nomor cocok. Label baru tanpa cacat tidak sesuai dengan riwayat servis dan kondisi radio ini.","match",
                    S("Hubungkan radio dengan catatan servis.","Radio S-014 / Catatan S-014: konektor diganti bulan lalu / S-041: televisi","S-014 adalah catatan radio yang sama; S-041 milik barang lain.",0,"Catatan S-014","Catatan S-041","Poster bazar"),
                    S("Bandingkan label dengan riwayat.","LABEL: baru, belum pernah diperbaiki / SERVIS: konektor diganti","Riwayat perbaikan bertentangan dengan klaim belum pernah diperbaiki.",2,"Keduanya cocok","Servis tidak relevan","Label perlu dikoreksi"),
                    S("Hubungkan janji penjualan dengan struk.","STRUK: radio tanpa cacat, Rp150.000 / KONDISI: retakan tersembunyi","Janji tanpa cacat berbeda dengan kondisi yang ditemukan. Simpan struk sebagai bukti kesepakatan.",1,"Buang struk","Simpan struk dan foto","Ubah isi struk"),
                    S("Pilih penjelasan istilah tadlis untuk kasus ini.","Cacat ditutup sehingga informasi penting tidak diketahui pembeli","Menyembunyikan kondisi yang penting agar pembeli terkecoh merupakan tadlis; harga murah tidak membenarkannya.",0,"Menyembunyikan cacat penting","Semua barang bekas dilarang","Semua diskon menipu")),
                T("c4.hear",1,"Penjual","Penjual","Minta klarifikasi — lapak","Radio ini memang pernah diservis. Label lama belum kuganti. Retakan juga perlu dijelaskan. Mari tuliskan koreksi dan pilihan penyelesaian untuk pembeli.","Penjual mengakui perbedaan informasi. Susun permintaan berdasarkan kesepakatan, bukan ancaman."),
                T("c4.packet",2,"Map pengaduan","Naya","Susun permintaan — meja layanan","Masukkan bukti yang relevan ke map. Contoh: foto kondisi dan struk membantu memeriksa keluhan; nomor pribadi orang lain tidak perlu dipasang di ruang publik.","Map berisi foto kondisi, struk, catatan servis yang cocok, dan permintaan tertulis untuk penyelesaian.","sort",
                    S("Pilih bukti kondisi barang.","FOLDER: foto retak / foto kerumunan / komentar anonim","Foto retak pada radio yang sama menjelaskan kondisi yang dikeluhkan.",1,"Foto kerumunan","Foto retak radio","Komentar anonim"),
                    S("Pilih bukti kesepakatan.","FOLDER: struk transaksi / poster makanan / kabar dari teman","Struk mencatat barang, harga, dan janji kondisi yang perlu dibandingkan.",2,"Kabar dari teman","Poster makanan","Struk transaksi"),
                    S("Tulis permintaan yang proporsional.","Penjual bersedia berdiskusi / Pembeli ingin barang sesuai janji","Meminta perbaikan, penggantian, atau pengembalian sesuai kesepakatan membuka penyelesaian yang dapat ditindaklanjuti.",0,"Minta opsi penyelesaian tertulis","Ancaman sebar alamat rumah","Tambahkan kerusakan fiktif"),
                    S("Tentukan apa yang boleh ditampilkan pada papan bazar.","Ada data pembeli di struk / Papan bazar dapat dilihat semua orang","Papan cukup memuat label produk yang diperbaiki. Data pribadi dan berkas pengaduan disimpan terbatas.",1,"Semua data pribadi","Label kondisi yang benar","Foto identitas pembeli")),
                T("c4.label",1,"Dokumen barang","Penjual","Perbaiki label — meja lapak","Label baru: radio bekas, pernah diperbaiki, retakan pada casing, sambungan perlu ditangani. Pembeli dapat menilai kembali setelah mendapat informasi yang lengkap.","Label sudah dikoreksi; radio tidak ditawarkan sebagai baru tanpa cacat."),
                T("c4.close",2,"Naya","Naya","Catat tindak lanjut — layanan warga","Penjual dan pembeli sepakat menindaklanjuti keluhan besok melalui meja layanan. Aku mencatat tanggal dan pilihan yang disepakati. Keluhan belum dianggap selesai hanya karena formulir sudah dibuat.","Kesepakatan dan jadwal tercatat. Kelompok menerima undangan investasi yang perlu diperiksa bersama.")
            }
        };
        static AdventureChapter Five() => new AdventureChapter {
            Title="The Grand Ponzi", Subtitle="05 / Pertanyaan yang tepat",
            Areas=new[]{"Taman Cempaka", "Ruang pertemuan", "Pusat informasi warga"},
            Introduction="Dimas membawa undangan investasi dengan janji hasil tinggi dan bonus mengajak teman. Beberapa peserta memang sudah dibayar. Kali ini Alif, Naya, Raka, dan Dimas memilih memeriksa bukti bersama sebelum siapa pun mengirim uang.",
            Ending="Tidak ada uang yang dikirim. Bukti disimpan dan langkah verifikasi dicatat. Raka memilih bertanya, Dimas berani meminta bantuan, dan Naya menjaga fakta. Di taman Cempaka, Alif menutup buku: perjalanan berakhir, kebiasaan jujur tetap dibawa pulang.",
            Optional=new[]{"Raka|Aku sudah tahu bagaimana satu kisah kemenangan bisa menutupi cerita orang lain. Kali ini aku ingin melihat catatannya.","Dimas|Bukti pembayaran awal terasa meyakinkan. Tetapi sekarang aku bertanya: uang pembayarannya datang dari mana?","Naya|Kalau ada teman yang sudah terlanjur ikut, kita bisa mendukungnya tanpa mempermalukannya. Simpan bukti dan cari saluran resmi."},
            Tasks=new[]{
                T("c5.arrival",0,"Dimas","Dimas","Baca undangan — taman","Ada janji hasil dua puluh persen tiap bulan, disebut tanpa risiko. Kita diberi waktu sampai sore dan bonus jika membawa teman. Aku belum mengirim uang.","Waktu yang mendesak bukan alasan melewati pemeriksaan. Temui kelompok di ruang pertemuan."),
                T("c5.claims",1,"Brosur penawaran","Alif","Bandingkan janji — brosur","Hubungkan setiap janji dengan bukti yang seharusnya mendukungnya. Contoh: tangkapan layar saldo bukan laporan kegiatan usaha atau penjelasan risiko.","Klaim hasil tinggi, tanpa risiko, dan bonus perekrutan tidak didukung bukti usaha yang dapat diperiksa.","match",
                    S("Pilih dokumen yang menjelaskan sumber keuntungan.","JANJI: hasil 20% per bulan / Dokumen usaha belum diberikan","Laporan kegiatan dan sumber pendapatan perlu diperiksa. Janji saja tidak menjelaskan dari mana keuntungan datang.",1,"Foto mobil promotor","Laporan usaha dan pendapatan","Jumlah pengikut"),
                    S("Periksa klaim tanpa risiko.","BROSUR: hasil tinggi dijamin / Tidak ada penjelasan risiko","Janji hasil tinggi tanpa penjelasan risiko adalah tanda untuk berhenti dan memeriksa, bukan alasan terburu-buru.",0,"Minta penjelasan risiko dan bukti","Anggap risiko pasti nol","Kirim uang agar mendapat tempat"),
                    S("Pisahkan pembayaran awal dari bukti usaha sehat.","Tiga peserta menerima uang / Sumber uang belum dijelaskan","Pembayaran awal membuktikan adanya pembayaran, bukan keamanan atau keberlanjutan usaha.",2,"Semua pasti aman","Tidak perlu bukti lain","Telusuri sumber pembayaran"),
                    S("Tanggapi tekanan waktu.","PESAN: transfer sebelum sore atau kehilangan kesempatan","Menunda transfer memberi waktu untuk memeriksa. Kesempatan yang aman tidak dibuktikan oleh tekanan waktu.",0,"Tunda sampai pemeriksaan selesai","Transfer sedikit dulu","Ajak teman menanggung risiko")),
                T("c5.flow",1,"Papan arus dana","Naya","Hubungkan arus uang — papan","Susun hubungan sumber dan penerima dari catatan contoh. Contoh: dana pelanggan untuk membeli barang berbeda dari setoran anggota baru untuk membayar janji anggota lama.","Catatan menunjukkan pembayaran lama berasal dari setoran baru, bukan pendapatan usaha yang tercatat.","flow",
                    S("Hubungkan sumber transfer pertama.","CATATAN 01: peserta baru A menyetor 1.000.000 ke rekening pengelola","Sumber dana pada catatan pertama adalah setoran peserta baru A.",1,"Penjualan barang","Setoran peserta baru A","Pendapatan sewa"),
                    S("Hubungkan rekening pengelola dengan penerima.","CATATAN 02: setelah setoran A, 200.000 dibayar ke peserta lama B","Peserta lama B menerima pembayaran yang mengikuti masuknya setoran baru.",2,"Pembelian stok usaha","Pajak kegiatan","Pembayaran peserta lama B"),
                    S("Temukan bukti usaha pada berkas contoh.","BERKAS: daftar anggota, bonus ajakan, transfer antaranggota / Tidak ada penjualan tercatat","Berkas ini tidak menyediakan bukti pendapatan usaha. Ketiadaan bukti dicatat tanpa mengarang transaksi.",0,"Belum ada bukti pendapatan usaha","Pasti ada keuntungan tersembunyi","Semua setoran adalah laba"),
                    S("Baca akibat ketika peserta baru berhenti menyetor.","Pembayaran lama bergantung pada setoran baru","Tanpa sumber usaha yang menopang pembayaran, berhentinya setoran baru merusak kemampuan memenuhi janji. Pola ini merupakan ciri Ponzi.",1,"Keuntungan otomatis naik","Pembayaran tidak punya penopang","Peserta lama pasti tetap untung")),
                T("c5.support",0,"Raka","Raka","Dukung teman — taman","Seorang temanku sudah ikut. Aku tidak ingin menyebutnya bodoh. Kita bisa mendengarkan, menyarankan ia menyimpan bukti, dan tidak mengajak orang lain untuk menutup kerugian.","Kelompok sepakat membantu tanpa menyebarkan data pribadi atau menjanjikan uang pasti kembali."),
                T("c5.verify",2,"Meja verifikasi","Alif","Susun berkas simulasi — pusat informasi","Ini simulasi, bukan pengiriman laporan. Pilih informasi minimum yang relevan. Contoh: izin perusahaan harus dicocokkan dengan identitas dan kegiatan yang ditawarkan; logo saja tidak cukup.","Berkas simulasi lengkap: identitas penawaran, kronologi, bukti, status verifikasi, dan kanal resmi untuk diperiksa sendiri.","sort",
                    S("Pilih cara memeriksa legalitas penawaran.","PROMOTOR: tangkapan layar logo / Tidak ada hasil pencocokan resmi","Cocokkan identitas, izin, dan kegiatan penawaran melalui kanal lembaga berwenang. Logo bukan verifikasi.",2,"Percaya logo","Tanya grup promotor saja","Cocokkan melalui kanal resmi"),
                    S("Pilih kronologi yang dapat diperiksa.","Undangan diterima Senin / Janji hasil dan batas waktu ada di pesan","Tanggal, urutan kejadian, dan kutipan penawaran membantu pemeriksaan; hindari dugaan sebagai fakta.",0,"Tanggal, pesan, dan urutan kejadian","Cerita tambahan agar dramatis","Nama seluruh teman di publik"),
                    S("Pilih bukti yang perlu disimpan.","Brosur, pesan penawaran, dan catatan pembayaran contoh tersedia","Simpan dokumen asli yang relevan. Jangan mengubah bukti atau mengirim data melalui tautan dari promotor.",1,"Edit angka agar lebih jelas","Simpan bukti asli yang relevan","Hapus semua pesan"),
                    S("Tandai status dan tujuan berkas.","Game tidak mengirim data / Verifikasi dan pengaduan nyata dilakukan di kanal resmi","Berkas ini hanya simulasi. Kanal resmi OJK dapat dibuka secara terpisah; permainan tidak mengirim laporan atau menjamin pemulihan dana.",0,"Simulasi siap; belum dikirim","Laporan resmi sudah terkirim","Uang pasti kembali")),
                T("c5.decline",1,"Dimas","Dimas","Nyatakan keputusan — ruang pertemuan","Kami tidak akan mentransfer atau mengajak orang lain. Kami akan menyimpan bukti dan memeriksa kanal resmi. Keputusan ini tidak membutuhkan perdebatan panjang dengan promotor.","Penawaran ditolak dalam cerita. Tidak ada transfer, pesan, atau laporan yang dikirim oleh game."),
                T("c5.epilogue",0,"Buku perjalanan","Alif","Berkumpul kembali — taman","Raka membawa poster kegiatan, Naya membawa label yang sudah benar, Dimas membawa rencana pengeluaran. Kita tidak selalu memiliki jawaban seketika. Tetapi sekarang kita tahu cara bertanya, memeriksa, dan saling membantu.","Kelima bab selesai. Buka jurnal untuk membaca jejak keputusan, atau mainkan ulang bab tanpa menghapus bab yang sudah terbuka.")
            }
        };
    }
}
