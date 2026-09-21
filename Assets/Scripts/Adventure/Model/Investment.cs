using System;
using System.Collections.Generic;
using System.Linq;

namespace Alif.Adventure
{
    /// <summary>
    /// Satu penawaran yang sedang berjalan di aplikasi Investasi (HP), mis. sukuk ijarah untuk
    /// pembangunan gedung kampus. Imbal hasilnya <b>ujrah</b> (sewa) tetap per periode sesuai akad —
    /// bukan bunga dan bukan "jaminan untung": pokok kembali saat jatuh tempo, dan risikonya tetap
    /// ada. Waktu permainan dipadatkan: satu periode dihitung dalam hari cerita (AdventureState.Day).
    /// </summary>
    public sealed class InvestmentListing
    {
        public string Id, Card, Name, Issuer, Place, Akad, Description, Risk;
        /// <summary>Apa yang dibangun uang Alif (ditampilkan di detail), dan catatan jurnal saat
        /// penawarannya jatuh tempo — dampak nyata investasi, di luar angka imbal hasilnya.</summary>
        public string Impact, MaturityNote;
        /// <summary>Harga satu unit (Rp) dan ujrah per periode dalam basis poin (200 = 2%).</summary>
        public int UnitPrice, RateBp;
        /// <summary>Panjang satu periode (hari cerita) dan jumlah periode sampai jatuh tempo.</summary>
        public int PeriodDays, Periods;
        /// <summary>Persentase dana yang sudah terkumpul dari warga lain — hanya untuk tampilan.</summary>
        public int FundedPercent;

        public int TenorDays => PeriodDays * Periods;
        public string RateLabel => $"{RateBp / 100f:0.##}% / {(PeriodDays == 1 ? "hari" : PeriodDays + " hari")}";
    }

    /// <summary>Kepemilikan Alif atas satu penawaran (satu baris per pembelian).</summary>
    [Serializable]
    public sealed class Holding
    {
        public string Listing;
        public int Units, StartDay, PaidPeriods;
    }

    public static class InvestmentContent
    {
        // Angka dipilih supaya terasa di permainan tanpa mengajarkan hal yang keliru: di dunia nyata
        // sukuk ritel berkisar ±6% per TAHUN. Janji hasil tinggi "dijamin" justru tanda bahaya
        // (materi Bab 5), jadi jangan menaikkan RateBp tanpa menimbang pesan itu.
        public static readonly InvestmentListing[] Listings =
        {
            new InvestmentListing { Id = "sukuk.senirupa", Card = "sukuk", Name = "Sukuk Ijarah Gedung Seni Rupa & Desain",
                Issuer = "Yayasan Kampus Cempaka", Place = "Kampus Cempaka", Akad = "Ijarah (sewa)",
                Description = "Dana membiayai pembangunan gedung studio Seni Rupa & Desain. Pemegang sukuk ikut memiliki manfaat gedung itu; kampus membayar sewa (ujrah) tetap tiap periode dan mengembalikan pokok saat jatuh tempo.",
                Risk = "Rendah–menengah: pembangunan bisa molor, dan dana tidak bisa ditarik sebelum jatuh tempo.",
                Impact = "Tiap unit ikut membiayai satu sudut studio gambar untuk mahasiswa Seni Rupa.",
                MaturityNote = "Sukuk Gedung Seni Rupa jatuh tempo: studio barunya mulai dipakai mahasiswa, pokokmu kembali utuh, dan ujrahnya berasal dari sewa gedung — bukan dari bunga.",
                UnitPrice = 100000, RateBp = 200, PeriodDays = 2, Periods = 5, FundedPercent = 62 },
            new InvestmentListing { Id = "sukuk.pasar", Card = "sukuk", Name = "Sukuk Ijarah Renovasi Los Pasar",
                Issuer = "Koperasi Pedagang Jalan Pasar", Place = "Jalan Pasar", Akad = "Ijarah (sewa)",
                Description = "Dana merenovasi atap dan lantai los pasar. Pedagang menyewa los yang sudah diperbaiki; sewa hariannya dibagikan ke pemegang sukuk sebagai ujrah, pokok kembali saat jatuh tempo.",
                Risk = "Menengah: pendapatan sewa bergantung pada ramainya pasar.",
                Impact = "Tiap unit ikut mengganti atap bocor di atas lapak pedagang Jalan Pasar.",
                MaturityNote = "Sukuk Los Pasar jatuh tempo: atap los sudah tidak bocor, pedagang membayar sewanya, dan pokokmu kembali utuh.",
                UnitPrice = 100000, RateBp = 100, PeriodDays = 1, Periods = 7, FundedPercent = 35 },
        };

        public static InvestmentListing Find(string id) => Listings.FirstOrDefault(l => l.Id == id);
        public static IEnumerable<InvestmentListing> ListingsFor(string cardId) => Listings.Where(l => l.Card == cardId);
    }

    public static class InvestmentRules
    {
        public const int MaxHoldings = 12, MaxUnits = 20, MaxBalance = 2000000;

        /// <summary>Instrumen bisa dibuka bila kartunya sudah didapat dari quest, atau bila ada
        /// penawaran berjalan untuknya (halaman penjelasannya sendiri yang mengajarkan dasarnya).</summary>
        public static bool Open(AdventureState state, InvestmentCard card) =>
            state.Cards.Contains(card.Id) || InvestmentContent.ListingsFor(card.Id).Any();

        /// <summary>Ujrah yang dibayarkan tiap periode untuk sejumlah unit.</summary>
        public static int Payout(InvestmentListing listing, int units) => (int)((long)units * listing.UnitPrice * listing.RateBp / 10000);

        public static int Invested(AdventureState state) =>
            state.Holdings.Sum(h => h.Units * (InvestmentContent.Find(h.Listing)?.UnitPrice ?? 0));

        /// <summary>Hari cerita pembayaran ujrah berikutnya untuk sebuah kepemilikan.</summary>
        public static int NextPayoutDay(Holding holding)
        {
            var listing = InvestmentContent.Find(holding.Listing);
            return holding.StartDay + (holding.PaidPeriods + 1) * listing.PeriodDays;
        }

        /// <summary>Uang yang bisa dipakai berinvestasi: dompet + rekening.</summary>
        public static int Funds(AdventureState state) => state.Money + state.Bank;

        /// <summary>Beli dari dompet dulu (uang yang terlihat di HUD), sisanya dari rekening — pemain
        /// harus merasakan uangnya berkurang, dan kehabisan uang tunai adalah risiko nyatanya.</summary>
        public static bool Buy(AdventureState state, InvestmentListing listing, int units, out string feedback)
        {
            feedback = "Penawaran tidak ditemukan.";
            if (listing == null) return false;
            if (state.TutorialStep != 0) { feedback = "Selesaikan tutorial dulu."; return false; }
            if (units < 1 || units > MaxUnits) { feedback = $"Jumlah unit 1–{MaxUnits}."; return false; }
            if (state.Holdings.Count >= MaxHoldings) { feedback = "Portofoliomu sudah penuh. Tunggu ada yang jatuh tempo."; return false; }
            int cost = units * listing.UnitPrice;
            if (cost > Funds(state)) { feedback = $"Uangmu kurang Rp{cost - Funds(state):N0}."; return false; }
            int fromWallet = Math.Min(cost, state.Money);
            state.Money -= fromWallet; state.Bank -= cost - fromWallet;
            state.Holdings.Add(new Holding { Listing = listing.Id, Units = units, StartDay = state.Day });
            string note = $"Investasi: {listing.Name} ({listing.Akad}) — dana terkunci sampai jatuh tempo; hasilnya ujrah, bukan bunga.";
            if (!state.Evidence.Contains(note)) state.Evidence.Add(note);
            feedback = $"{units} unit {listing.Name} dibeli • ujrah Rp{Payout(listing, units):N0} tiap {(listing.PeriodDays == 1 ? "hari" : listing.PeriodDays + " hari")}.";
            return true;
        }

        /// <summary>Dipanggil tiap kali hari cerita berganti: bayarkan ujrah periode yang sudah lewat
        /// ke dompet, dan kembalikan pokok yang jatuh tempo (kelebihan batas dompet masuk rekening).
        /// Null bila tidak ada pembayaran.</summary>
        public static string Settle(AdventureState state)
        {
            long ujrah = 0, principal = 0;
            foreach (var holding in state.Holdings.ToArray())
            {
                var listing = InvestmentContent.Find(holding.Listing);
                if (listing == null) continue;
                int due = Math.Min(listing.Periods, (state.Day - holding.StartDay) / listing.PeriodDays);
                if (due <= holding.PaidPeriods) continue;
                ujrah += (long)(due - holding.PaidPeriods) * Payout(listing, holding.Units);
                holding.PaidPeriods = due;
                if (due < listing.Periods) continue;
                principal += (long)holding.Units * listing.UnitPrice;
                state.Holdings.Remove(holding);
                if (!string.IsNullOrEmpty(listing.MaturityNote) && !state.Evidence.Contains(listing.MaturityNote)) state.Evidence.Add(listing.MaturityNote);
            }
            if (ujrah == 0 && principal == 0) return null;
            long wallet = state.Money + ujrah + principal, overflow = Math.Max(0, wallet - MaxBalance);
            state.Money = (int)(wallet - overflow);
            state.Bank = (int)Math.Min(MaxBalance, state.Bank + overflow);
            return principal > 0
                ? $"Ujrah sukuk Rp{ujrah:N0} + pokok jatuh tempo Rp{principal:N0} masuk dompet."
                : $"Ujrah sukuk Rp{ujrah:N0} masuk dompet.";
        }

        public static bool Valid(AdventureState state)
        {
            if (state.Holdings == null || state.Holdings.Count > MaxHoldings) return false;
            foreach (var holding in state.Holdings)
            {
                var listing = holding == null ? null : InvestmentContent.Find(holding.Listing);
                if (listing == null || holding.Units < 1 || holding.Units > MaxUnits || holding.StartDay < 1 || holding.StartDay > state.Day ||
                    holding.PaidPeriods < 0 || holding.PaidPeriods >= listing.Periods ||
                    holding.PaidPeriods > (state.Day - holding.StartDay) / listing.PeriodDays) return false;
            }
            return true;
        }
    }
}
