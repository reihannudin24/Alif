using System;

namespace Alif.Adventure
{
    /// <summary>
    /// Aplikasi pinjaman online "DanaKilat" di HP Alif — godaan saat uang menipis, bukan jalan
    /// keluar. Dibuat sejujur praktik pinjol yang menjerat: uang cair seketika tetapi dipotong biaya
    /// admin di depan, utang berbunga harian (riba) dan berbunga atas bunganya, lalu denda setelah
    /// jatuh tempo. Pelajarannya datang dari angkanya sendiri: layar simulasi memperlihatkan utang
    /// itu beberapa hari ke depan SEBELUM Alif menekan "pinjam". Satu pinjaman aktif dalam satu
    /// waktu; utang (seperti uang) tidak dibawa ke bab berikutnya.
    /// </summary>
    public static class PinjolRules
    {
        public const string AppName = "DanaKilat";
        public static readonly int[] Amounts = { 100000, 300000, 500000 };
        /// <summary>Biaya admin dipotong di depan dan bunga harian, dalam basis poin (500 = 5%).</summary>
        public const int AdminBp = 1000, DailyBp = 500;
        public const int DueDays = 5, LateFee = 10000, MaxDebt = 3000000, MaxBalance = 2000000;

        /// <summary>Uang yang benar-benar diterima setelah biaya admin dipotong.</summary>
        public static int Received(int amount) => amount - (int)((long)amount * AdminBp / 10000);

        static int Grow(int debt, bool late) =>
            (int)Math.Min(MaxDebt, debt + (long)debt * DailyBp / 10000 + (late ? LateFee : 0));

        /// <summary>Utang setelah sekian hari bila tidak dicicil sama sekali — dipakai layar simulasi.</summary>
        public static int DebtAfter(int amount, int days)
        {
            int debt = amount;
            for (int day = 1; day <= days; day++) debt = Grow(debt, day > DueDays);
            return debt;
        }

        public static bool Overdue(AdventureState state) => state.Debt > 0 && state.Day > state.DebtDay + DueDays;
        public static int DueDay(AdventureState state) => state.DebtDay + DueDays;

        public static bool Borrow(AdventureState state, int amount, out string feedback)
        {
            feedback = "Nominal tidak tersedia.";
            if (Array.IndexOf(Amounts, amount) < 0) return false;
            if (state.TutorialStep != 0) { feedback = "Selesaikan tutorial dulu."; return false; }
            if (state.Debt > 0) { feedback = "Masih ada pinjaman berjalan. Lunasi dulu sebelum meminjam lagi."; return false; }
            int received = Received(amount);
            if (state.Money + received > MaxBalance) { feedback = "Dompetmu masih penuh — kamu tidak butuh pinjaman."; return false; }
            state.Money += received;
            state.Debt = state.DebtPrincipal = amount; state.DebtDay = state.DebtAccruedDay = state.Day;
            string note = $"Pinjol {AppName}: pinjam Rp{amount:N0}, diterima Rp{received:N0}; utang tumbuh {DailyBp / 100}% per hari. Tambahan atas utang seperti ini adalah riba.";
            if (!state.Evidence.Contains(note)) state.Evidence.Add(note);
            feedback = $"Rp{received:N0} masuk dompet • utangmu Rp{amount:N0}, jatuh tempo Hari ke-{DueDay(state)}.";
            return true;
        }

        /// <summary>Cicil atau lunasi: dompet dipakai dulu, sisanya dari rekening.</summary>
        public static bool Repay(AdventureState state, int amount, out string feedback)
        {
            feedback = "Tidak ada pinjaman berjalan.";
            if (state.Debt <= 0) return false;
            amount = Math.Min(amount, state.Debt);
            if (amount <= 0) { feedback = "Nominal tidak sah."; return false; }
            if (amount > state.Money + state.Bank) { feedback = $"Uangmu kurang Rp{amount - state.Money - state.Bank:N0}."; return false; }
            int fromWallet = Math.Min(amount, state.Money);
            state.Money -= fromWallet; state.Bank -= amount - fromWallet;
            state.Debt -= amount;
            if (state.Debt > 0) { state.DebtPaid += amount; feedback = $"Rp{amount:N0} dibayar • sisa utang Rp{state.Debt:N0}."; return true; }
            int cost = Math.Max(0, state.DebtPaid + amount - Received(state.DebtPrincipal));
            string note = $"Pinjol {AppName} lunas: menerima Rp{Received(state.DebtPrincipal):N0}, membayar total Rp{state.DebtPaid + amount:N0} — ongkos utangnya Rp{cost:N0}.";
            if (!state.Evidence.Contains(note)) state.Evidence.Add(note);
            state.DebtPrincipal = state.DebtDay = state.DebtAccruedDay = state.DebtPaid = 0;
            feedback = $"Lunas. Ongkos pinjaman ini Rp{cost:N0} — catat baik-baik.";
            return true;
        }

        /// <summary>Dipanggil tiap hari cerita berganti: bunga harian (dan denda bila lewat jatuh
        /// tempo) ditambahkan untuk tiap hari yang berlalu. Null bila tidak ada utang.</summary>
        public static string Accrue(AdventureState state)
        {
            if (state.Debt <= 0 || state.DebtAccruedDay >= state.Day) return null;
            int before = state.Debt;
            for (int day = state.DebtAccruedDay + 1; day <= state.Day; day++) state.Debt = Grow(state.Debt, day > DueDay(state));
            state.DebtAccruedDay = state.Day;
            return Overdue(state)
                ? $"{AppName} MENAGIH: lewat jatuh tempo • utang naik Rp{state.Debt - before:N0} jadi Rp{state.Debt:N0} (bunga + denda)."
                : $"{AppName}: bunga harian Rp{state.Debt - before:N0} • utang kini Rp{state.Debt:N0}.";
        }

        public static bool Valid(AdventureState state)
        {
            if (state.Debt < 0 || state.Debt > MaxDebt || state.DebtPaid < 0) return false;
            if (state.Debt == 0) return state.DebtPrincipal == 0 && state.DebtDay == 0 && state.DebtAccruedDay == 0 && state.DebtPaid == 0;
            return Array.IndexOf(Amounts, state.DebtPrincipal) >= 0 && state.DebtDay >= 1 && state.DebtDay <= state.Day &&
                   state.DebtAccruedDay >= state.DebtDay && state.DebtAccruedDay <= state.Day;
        }
    }
}
