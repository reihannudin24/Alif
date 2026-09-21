using System;
using System.Collections.Generic;
using System.Linq;
namespace Alif.Adventure
{
    /// <summary>Dokumen yang diperlihatkan sebelum papannya dibuka (struk, tagihan), berdampingan
    /// dengan pembandingnya — pemain melihat sendiri selisihnya sebelum diminta mengoreksi.
    /// Baris berformat "Keterangan|Jumlah".</summary>
    [Serializable]
    public sealed class BoardDocument
    {
        /// <summary>Nama pendek dokumennya, dipakai di tab dan tombol ("Lihat struk").</summary>
        public string Label = "dokumen";
        public string Prompt, Header, Total, CompareHeader, CompareTotal;
        public string[] Rows = Array.Empty<string>(), CompareRows = Array.Empty<string>();
    }

    [Serializable]
    public sealed class ActivityBoard
    {
        public string Kind, Instruction;
        /// <summary>Boleh null. Kalau ada, papan dibuka lewat tampilan dokumen ini dulu.</summary>
        public BoardDocument Document;
        /// <summary>Papan penilaian bebas: tiap kartu boleh ditaruh di tujuan mana pun dan bisa diubah;
        /// papan selesai begitu semua kartu dinilai. Ketepatannya (<see cref="Tier"/>) tidak
        /// menghalangi pemain — ia menentukan akibat ceritanya (AdventureTask.Outcomes).</summary>
        public bool Free;
        public string[] Cards, Slots, Notes;
        /// <summary>Nama tiap kebutuhan di papan anggaran (indeksnya = nilai di Groups), dipakai
        /// sebagai judul baris kartu — mis. "Makanan" dan "Minuman".</summary>
        public string[] GroupNames;
        public int[] Answers, Costs, Groups;
        public int Limit;
        public bool Solved(IReadOnlyList<int> values)
        {
            if(values==null||values.Count!=Cards.Length)return false;
            if(Free)return values.All(v=>v>=0&&v<Slots.Length);
            if(Kind!="budget")return Enumerable.Range(0,Cards.Length).All(i=>values[i]==Answers[i]);
            int total=0;var groups=new HashSet<int>();
            for(int i=0;i<values.Count;i++)if(values[i]==1){total+=Costs[i];if(Groups[i]>=0&&!groups.Add(Groups[i]))return false;}
            // Jumlah kebutuhan mengikuti data (Groups), bukan dipatok tiga: papan menu warung
            // hanya punya dua kebutuhan (makanan & minuman).
            return total<=Limit && groups.Count==Groups.Where(g=>g>=0).Distinct().Count();
        }
        /// <summary>Jumlah kartu yang dinilai tepat.</summary>
        public int Score(IReadOnlyList<int> values) => values==null?0:Enumerable.Range(0,Math.Min(Cards.Length,values.Count)).Count(i=>values[i]==Answers[i]);
        /// <summary>Tingkat hasil papan bebas: 2 = tepat semua, 1 = setidaknya separuh, 0 = kurang dari separuh.</summary>
        public int Tier(IReadOnlyList<int> values){int score=Score(values);return score==Cards.Length?2:score*2>=Cards.Length?1:0;}
        public int Total(IReadOnlyList<int> values) => Enumerable.Range(0,Cards.Length).Where(i=>values[i]==1).Sum(i=>Costs[i]);
    }
    public static class OriginalCampaign
    {
        static ActivityBoard Sort(string[] cards,string[] slots,int[] answers,string[] notes,string kind="sort") => new ActivityBoard {Kind=kind,Cards=cards,Slots=slots,Answers=answers,Notes=notes,Instruction=kind=="flow"?"Pilih sumber dana, lalu hubungkan ke penerimanya. Hubungan yang benar tetap tersimpan.":"Pilih kartu di kiri, lalu letakkan pada tujuan di kanan. Kartu yang benar tetap tersimpan."};
        static ActivityBoard Budget(string[] cards,int[] costs,int[] groups,int limit,string[] notes,string[] groupNames=null) => new ActivityBoard {Kind="budget",Cards=cards,Costs=costs,Groups=groups,Limit=limit,Notes=notes,GroupNames=groupNames,Slots=new[]{"Tidak dipilih","Dipilih"},Instruction="Pilih satu untuk tiap kebutuhan, jangan sampai melewati dana yang tersedia. Klik lagi untuk membatalkan."};
        public static void Apply(AdventureChapter c)
        {
            bool kos=c.Number==2||c.Number==3;
            c.Areas=kos?new[]{"Kamar Dimas", "Lantai 1 kos", "Halaman kos"}:new[]{"Dalam stasiun", "Depan stasiun", "Depan warung", "Dalam Warung Bu Siti"};
            c.StartArea=c.Number==3||c.Number==4?2:c.Number==5?1:0;
            c.Areas=c.Areas.Concat(AdventureContent.CityAreas).ToArray();
            foreach(var t in c.Tasks)
            {
                t.Area=c.Number==1?(t.Area==0?0:t.Area==1?2:3):c.Number==2?(t.Area==1?1:0):c.Number==3?(t.Area==2?1:2):c.Number==4?(t.Area==2?3:2):(t.Area==0?1:0);
                t.Title=t.Title.Split('—')[0].Trim()+" — "+c.Areas[t.Area];
                if(t.Steps.Length==0)continue;
                // Documents are matched simultaneously, rather than one multiple-choice question at a time.
                var slots=t.Steps.Select(s=>s.Options[s.Answer]).Distinct().ToArray();
                t.Board=Sort(t.Steps.Select(s=>s.Evidence).ToArray(),slots,t.Steps.Select(s=>Array.IndexOf(slots,s.Options[s.Answer])).ToArray(),t.Steps.Select(s=>s.Explanation).ToArray(),"match");
            }
            foreach(var t in c.Tasks)
            {
                switch(t.Id)
                {
                    case "c1.menu": t.Board=Budget(
                        new[]{"Nasi telur", "Bakso", "Soto ayam", "Ayam geprek",
                              "Es teh", "Air kemasan", "Jus jeruk", "Es campur"},
                        new[]{12000,15000,16000,18000, 6000,9000,10000,12000},
                        new[]{0,0,0,0, 1,1,1,1},20000,
                        new[]{"Pilih satu makanan dan satu minuman; totalnya tidak boleh lebih dari Rp20.000.",
                              "Nasi telur Rp12.000 + es teh Rp6.000 = Rp18.000 — satu-satunya pasangan yang muat, masih sisa Rp2.000."},
                        new[]{"Makanan","Minuman"});break;
                    // Struk keliru ditunjukkan dulu di samping pesanan Alif; dua baris salahnya (es teh
                    // 2x, kerupuk) adalah kartu papan ini. 12.000 + 12.000 + 4.000 = 28.000 vs 18.000.
                    case "c1.receipt": t.Board.Document=new BoardDocument{
                        Label="struk",Prompt="Bandingkan struk dari kasir dengan pesananmu. Baris mana yang tidak cocok?",
                        Header="WARUNG BU SITI",Rows=new[]{"Nasi telur   1 x 12.000|12.000","Es teh   2 x 6.000|12.000","Kerupuk   1 x 4.000|4.000"},Total="TOTAL|28.000",
                        CompareHeader="PESANANMU",CompareRows=new[]{"Nasi telur   1 x 12.000|12.000","Es teh   1 x 6.000|6.000"},CompareTotal="TOTAL|18.000"};break;
                    // Tawaran Raka dinilai bebas: Bu Siti mengikuti penilaian Alif, benar atau keliru,
                    // dan akibatnya baru terasa keesokan paginya (StoryContent.OfferAftermath).
                    case "c1.offer": t.Board.Free=true;
                        t.Board.Instruction="Menurutmu, syarat ini melindungi Bu Siti atau menjeratnya? Tidak ada jawaban yang dikunci — Bu Siti akan mengikuti penilaianmu.";break;
                    case "c2.plan": t.Board=Budget(new[]{"Makan minggu ini", "Transport kuliah", "Kebutuhan kuliah", "Dekorasi kamar", "Langganan hiburan"},new[]{140000,60000,200000,80000,90000},new[]{0,1,2,-1,-1},400000,new[]{"Dana Rp300.000 + bantuan keluarga terkonfirmasi Rp100.000 menutup kebutuhan Rp400.000.","Bantuan kampus yang belum disetujui tidak dihitung sebagai dana tersedia."});break;
                    case "c3.plan": t.Board=Budget(new[]{"Tikar pinjaman + izin", "Kursi sewa", "Minuman semua peserta", "Poster dan kebersihan", "Hadiah promosi"},new[]{0,80000,40000,30000,70000},new[]{0,0,1,2,-1},100000,new[]{"Tikar dipinjam dengan izin; minuman Rp40.000, poster dan kebersihan Rp30.000.","Total Rp70.000, cadangan Rp30.000. Tidak ada pembayaran untuk peluang hadiah."});break;
                    case "c2.expenses": t.Board=Sort(new[]{"Makan minggu ini • 140.000", "Transport menuju kelas • 60.000", "Dekorasi kamar • 80.000", "Tagihan kuliah • 200.000", "Ganti casing telepon • 50.000", "Bantuan kampus belum disetujui"},new[]{"Kebutuhan mendesak", "Dapat ditunda", "Belum boleh dihitung sebagai dana"},new[]{0,0,1,0,1,2},new[]{"Makan dilindungi.","Transport diperlukan untuk kelas.","Dekorasi tidak mendesak.","Tagihan kuliah masuk kebutuhan yang perlu direncanakan.","Casing masih berfungsi; penggantian dapat ditunda.","Permohonan bukan dana yang sudah diterima."});break;
                    // Dua baris keliru sudah dipecahkan di papan struk — di sini yang dilatih hanya
                    // memisahkan fakta, kesepakatan, dan tuduhan.
                    case "c1.resolve": t.Board=Sort(new[]{"Struk baru: dua item, total 18.000", "Bayar 18.000 setelah struk dikoreksi", "Kembalian 2.000 dari uang 20.000", "Bu Siti pasti sengaja menipu"},new[]{"Fakta yang diperiksa", "Kesepakatan penyelesaian", "Tuduhan tanpa bukti"},new[]{0,1,1,2},new[]{"Struk yang sudah dikoreksi memuat dua item senilai Rp18.000.","Pembayaran mengikuti struk yang telah disepakati.","20.000 dikurangi 18.000 adalah 2.000.","Kesalahan pada struk tidak membuktikan niat menipu."});break;
                    case "c4.packet": t.Board=Sort(new[]{"Foto retak radio", "Struk dengan janji tanpa cacat", "Catatan servis S-014", "Alamat rumah pembeli di poster publik", "Permintaan penyelesaian tertulis", "Cerita kerusakan yang tidak pernah terjadi"},new[]{"Masukkan berkas terbatas", "Jangan sebarkan", "Bukan bukti yang jujur"},new[]{0,0,0,1,0,2},new[]{"Foto mencatat kondisi yang terlihat.","Struk adalah bukti kesepakatan.","Nomor servis harus sesuai dengan barang.","Data pribadi bukan materi papan publik.","Permintaan tertulis membuat tindak lanjut jelas.","Jangan menambah cerita untuk memperkuat keluhan."});break;
                    case "c5.verify": t.Board=Sort(new[]{"Brosur dan pesan asli", "Kronologi tanggal penawaran", "Identitas dan izin kegiatan", "Logo pada tangkapan layar", "Daftar nomor pribadi semua teman", "Pernyataan: laporan ini baru simulasi"},new[]{"Simpan dalam berkas", "Cocokkan di kanal resmi", "Jangan sebarkan"},new[]{0,0,1,1,2,0},new[]{"Simpan bukti asli yang relevan.","Tulis urutan kejadian tanpa dugaan sebagai fakta.","Identitas, izin, dan kegiatan harus dicocokkan.","Logo bukan bukti verifikasi.","Lindungi data pribadi teman.","Game tidak mengirim laporan atau menjamin pemulihan uang."});break;
                    case "c5.flow": t.Board=Sort(new[]{"Setoran baru A • 1.000.000", "Pengelola mengirim 200.000", "Bonus mengajak anggota • 50.000", "Berkas tanpa transaksi penjualan"},new[]{"Rekening pengelola", "Peserta lama B", "Perekrut C", "Belum ada bukti pendapatan usaha"},new[]{0,1,2,3},new[]{"Setoran berasal dari peserta baru, bukan penjualan.","Peserta lama dibayar setelah setoran baru masuk.","Bonus perekrutan juga berasal dari dana peserta.","Pembayaran awal tidak membuktikan usaha yang sehat."},"flow");break;
                    case "c3.rules": t.Board=Sort(new[]{"Harga tiket", "Syarat tiket kalah", "Sumber hadiah", "Pembanding hadiah gratis"},new[]{"Catat temuan"},new[]{0,0,0,0},new[]{"Tiket berbayar Rp20.000 hanya membeli peluang hadiah.","Tiket kalah hangus dan tidak ditukar barang.","Hadiah berasal dari kumpulan setoran peserta.","Hadiah gratis tanpa setoran berbeda dari taruhan ini."},"inspect");break;
                    case "c4.inspect": t.Board=Sort(new[]{"Stiker sudut casing", "Sambungan kabel", "Label belakang S-014", "Catatan kondisi"},new[]{"Catat temuan"},new[]{0,0,0,0},new[]{"Setelah stiker diangkat dengan izin, terlihat retakan di casing.","Kabel longgar dicatat untuk petugas; jangan membuka rangkaian listrik sendiri.","Nomor S-014 akan dicocokkan dengan riwayat servis.","Catat kondisi yang terlihat tanpa menebak siapa yang merusaknya."},"inspect");break;
                }
                if(t.Board!=null)t.Kind=t.Board.Kind;
            }
        }
    }
}
