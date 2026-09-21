using Alif.Campaign;
using Alif.Systems;
using TMPro;
using UnityEngine;

namespace Alif.Adventure
{
    /// <summary>
    /// Aplikasi pinjol "DanaKilat" di HP Alif (aturan & angkanya di PinjolRules). Layarnya sengaja
    /// memperlihatkan harga sebenarnya — uang yang diterima, utang beberapa hari ke depan, dan jalan
    /// lain yang lebih bijak — SEBELUM tombol pinjam, dan "Tidak jadi" selalu jadi pilihan pertama.
    /// Meminjam menggeser neraca menjauh dari Kepatuhan Syariah; melunasi menariknya kembali.
    /// </summary>
    public sealed partial class AdventureGame
    {
        static readonly Color PinjolColor = new Color(.52f, .3f, .68f);
        const int InstallmentStep = 50000;

        /// <summary>Tiap bunga pinjol bertambah, neraca ikut tertekan — lebih keras setelah jatuh tempo.</summary>
        void DebtPressure() => ScoreSystem.Instance?.AdjustShariaCompliance(PinjolRules.Overdue(State) ? -4f : -2f);

        void ShowPinjolApp(string feedback = null)
        {
            PullBalances();
            var screen = PhoneScreen($"{PinjolRules.AppName.ToUpperInvariant()} • Dompet Rp{State.Money:N0}");
            if (State.Debt > 0) { ShowDebt(screen, feedback); return; }

            Text(screen, "Cair 1 menit!  Tanpa jaminan!  Tanpa ribet!", new Vector2(.02f, .78f), new Vector2(.98f, .88f), 19, PixelSkin.OrangeLight).alignment = TextAlignmentOptions.Center;
            Text(screen, "Alif (Batin): Kalau semudah ini, siapa yang menanggung harganya? Lihat dulu angkanya.", new Vector2(.04f, .68f), new Vector2(.96f, .78f), 14, PixelSkin.Cream).alignment = TextAlignmentOptions.Center;
            for (int i = 0; i < PinjolRules.Amounts.Length; i++)
            {
                int amount = PinjolRules.Amounts[i];
                float top = .66f - i * .17f;
                var row = Panel(screen, new Vector2(.02f, top - .155f), new Vector2(.98f, top), Color.white);
                ApplySprite(row, PixelSkin.Slot()); row.raycastTarget = false;
                Text(row.transform, $"Pinjam Rp{amount:N0}", new Vector2(.03f, .5f), new Vector2(.72f, 1f), 17, PixelSkin.TextDark);
                Text(row.transform, $"Diterima Rp{PinjolRules.Received(amount):N0}  •  bunga {PinjolRules.DailyBp / 100}% per HARI  •  tempo {PinjolRules.DueDays} hari",
                    new Vector2(.03f, 0f), new Vector2(.72f, .5f), 13, PixelSkin.Accent);
                Button(row.transform, "Lihat", new Vector2(.74f, .15f), new Vector2(.98f, .85f), () => ShowPinjolOffer(amount));
            }
            if (feedback != null) Text(screen, feedback, new Vector2(.04f, .09f), new Vector2(.96f, .16f), 13, PixelSkin.OrangeLight).alignment = TextAlignmentOptions.Center;
            PhoneNav(screen, OpenPhone);
            FocusFirst();
        }

        void ShowDebt(RectTransform screen, string feedback)
        {
            bool overdue = PinjolRules.Overdue(State);
            var panel = Panel(screen, new Vector2(.02f, .1f), new Vector2(.98f, .88f), Color.white);
            ApplySprite(panel, PixelSkin.Panel()); panel.raycastTarget = false;
            var p = panel.transform;
            Text(p, overdue ? "LEWAT JATUH TEMPO" : "PINJAMAN BERJALAN", new Vector2(.04f, .86f), new Vector2(.96f, .97f), 16, PixelSkin.Accent).alignment = TextAlignmentOptions.Center;
            Text(p, $"Utang Rp{State.Debt:N0}", new Vector2(.04f, .7f), new Vector2(.96f, .86f), 30, PixelSkin.TextDark).alignment = TextAlignmentOptions.Center;
            Text(p, $"Dipinjam Rp{State.DebtPrincipal:N0}, diterima hanya Rp{PinjolRules.Received(State.DebtPrincipal):N0}  •  sudah dicicil Rp{State.DebtPaid:N0}\n" +
                    $"Jatuh tempo Hari ke-{PinjolRules.DueDay(State)} (sekarang Hari ke-{State.Day})  •  besok jadi Rp{PinjolRules.DebtAfter(State.Debt, 1) + (State.Day + 1 > PinjolRules.DueDay(State) ? PinjolRules.LateFee : 0):N0}",
                new Vector2(.04f, .5f), new Vector2(.96f, .7f), 14, PixelSkin.TextDark).alignment = TextAlignmentOptions.Center;
            Text(p, feedback ?? (overdue ? "Tiap hari terlambat: bunga 5% ditambah denda. Lunasi secepat mungkin." : "Setiap malam utang ini bertambah 5% — dari jumlah yang sudah membengkak."),
                new Vector2(.06f, .36f), new Vector2(.94f, .5f), 14, PixelSkin.Accent).alignment = TextAlignmentOptions.Center;
            int funds = State.Money + State.Bank;
            Button(p, $"Lunasi Rp{State.Debt:N0}", new Vector2(.06f, .19f), new Vector2(.94f, .34f), () => RepayPinjol(State.Debt)).interactable = funds >= State.Debt;
            Button(p, $"Cicil Rp{InstallmentStep:N0}", new Vector2(.06f, .04f), new Vector2(.94f, .17f), () => RepayPinjol(InstallmentStep)).interactable = funds >= Mathf.Min(InstallmentStep, State.Debt) && State.Debt > InstallmentStep;
            PhoneNav(screen, OpenPhone);
            FocusFirst();
        }

        /// <summary>Simulasi sebelum meminjam: harga sebenarnya, lalu jalan lain yang lebih bijak.</summary>
        void ShowPinjolOffer(int amount)
        {
            PullBalances();
            var screen = PhoneScreen("HITUNG DULU");
            var panel = Panel(screen, new Vector2(.02f, .1f), new Vector2(.98f, .88f), Color.white);
            ApplySprite(panel, PixelSkin.Panel()); panel.raycastTarget = false;
            var p = panel.transform;
            Text(p, $"Pinjam Rp{amount:N0}", new Vector2(.04f, .87f), new Vector2(.96f, .98f), 21, PixelSkin.TextDark);
            Text(p, $"Masuk dompet hanya Rp{PinjolRules.Received(amount):N0} — biaya admin {PinjolRules.AdminBp / 100}% dipotong di depan.", new Vector2(.04f, .78f), new Vector2(.96f, .87f), 14, PixelSkin.Accent);
            int[] days = { 3, 7, 14 };
            for (int i = 0; i < days.Length; i++)
            {
                float top = .76f - i * .085f;
                var left = Text(p, $"Kalau baru dibayar {days[i]} hari lagi", new Vector2(.06f, top - .08f), new Vector2(.62f, top), 15, PixelSkin.TextDark);
                var right = Text(p, $"Rp{PinjolRules.DebtAfter(amount, days[i]):N0}", new Vector2(.62f, top - .08f), new Vector2(.94f, top), 15, PixelSkin.TextDark);
                left.alignment = TextAlignmentOptions.MidlineLeft; right.alignment = TextAlignmentOptions.MidlineRight; right.fontStyle = FontStyles.Bold;
            }
            int locked = InvestmentRules.Invested(State);
            string wiser = "Jalan lain: tarik tabungan lewat ATM  •  tunda belanja yang bukan kebutuhan  •  bicarakan sewa dengan Bu Tini (tunggakan dicatat, tanpa bunga)." +
                           (locked > 0 ? $"  Rp{locked:N0}-mu sedang terkunci di investasi — lain kali sisakan dana darurat." : "");
            Text(p, wiser, new Vector2(.05f, .2f), new Vector2(.95f, .49f), 13, PixelSkin.TextDark).alignment = TextAlignmentOptions.TopLeft;
            Text(p, "Tambahan atas utang adalah riba — dan ini berbunga tiap hari.", new Vector2(.05f, .155f), new Vector2(.95f, .215f), 13, PixelSkin.Accent);
            // "Tidak jadi" dibuat lebih dulu supaya jadi tombol yang terfokus.
            Button(p, "Tidak jadi", new Vector2(.05f, .03f), new Vector2(.55f, .15f), () => { ScoreSystem.Instance?.MoveTowardsBalance(2f); ShowPinjolApp("Keputusan bijak. Cari jalan yang tidak menjerat."); });
            var borrow = Button(p, "Tetap pinjam", new Vector2(.6f, .03f), new Vector2(.95f, .15f), () => BorrowPinjol(amount));
            PixelSkin.StyleButton(borrow, secondary: true);
            borrow.interactable = !TutorialActive;
            PhoneNav(screen, () => ShowPinjolApp());
            FocusFirst();
        }

        void BorrowPinjol(int amount)
        {
            PullBalances();
            if (!PinjolRules.Borrow(State, amount, out string feedback)) { ShowPinjolApp(feedback); return; }
            CurrencySystem.Instance?.RestoreBalances(State.Money, State.Bank);
            ScoreSystem.Instance?.AdjustShariaCompliance(-10f);     // riba: neraca menjauh dari Kepatuhan Syariah
            Save(false);
            ShowPinjolApp();
            Toast(feedback, 8);
        }

        void RepayPinjol(int amount)
        {
            PullBalances();
            bool ok = PinjolRules.Repay(State, amount, out string feedback);
            if (ok)
            {
                CurrencySystem.Instance?.RestoreBalances(State.Money, State.Bank);
                if (State.Debt == 0) ScoreSystem.Instance?.MoveTowardsBalance(8f);
                Save(false);
            }
            ShowPinjolApp(feedback);
        }
    }
}
