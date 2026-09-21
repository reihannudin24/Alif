using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Alif.Adventure.Tests
{
    public class PinjolTests
    {
        [Test]
        public void BorrowingPaysOutLessThanTheDebtAndOnlyOneLoanRunsAtATime()
        {
            var state = new AdventureState { TutorialStep = 1, Money = 0 };
            Assert.That(PinjolRules.Borrow(state, 300000, out _), Is.False, "Tutorial masih aktif.");
            state.TutorialStep = 0;
            Assert.That(PinjolRules.Borrow(state, 123456, out _), Is.False, "Nominal di luar daftar.");
            Assert.That(PinjolRules.Borrow(state, 300000, out _), Is.True);
            Assert.That(state.Money, Is.EqualTo(270000), "Admin 10% dipotong di depan.");
            Assert.That(state.Debt, Is.EqualTo(300000));
            Assert.That(state.Evidence.Any(e => e.Contains("riba")), Is.True);
            Assert.That(PinjolRules.Borrow(state, 100000, out _), Is.False, "Tidak boleh gali lubang tutup lubang.");
            Assert.That(SideQuestTests.Reload(state).Debt, Is.EqualTo(300000));
        }

        [Test]
        public void DebtCompoundsDailyThenAddsLateFeesAndNeverAccruesTwiceForOneDay()
        {
            var state = new AdventureState { Money = 0 };
            PinjolRules.Borrow(state, 100000, out _);
            Assert.That(PinjolRules.Accrue(state), Is.Null, "Hari yang sama belum berbunga.");
            int expected = 100000;
            for (int day = 2; day <= 9; day++)
            {
                state.Day = day;
                expected += expected * 5 / 100 + (day > 1 + PinjolRules.DueDays ? PinjolRules.LateFee : 0);
                Assert.That(PinjolRules.Accrue(state), Is.Not.Null);
                Assert.That(state.Debt, Is.EqualTo(expected), "Hari ke-" + day);
                Assert.That(PinjolRules.Accrue(state), Is.Null);
                Assert.That(PinjolRules.Overdue(state), Is.EqualTo(day > 1 + PinjolRules.DueDays));
                state = SideQuestTests.Reload(state);
            }
            Assert.That(PinjolRules.DebtAfter(100000, 8), Is.EqualTo(expected), "Layar simulasi harus sama dengan kenyataan.");
            Assert.That(PinjolRules.DebtAfter(100000, 14), Is.GreaterThan(200000), "Dua minggu: utang lebih dari dua kali lipat.");

            // Beberapa hari terlewat sekaligus tetap dihitung per hari.
            var skipped = new AdventureState { Money = 0 };
            PinjolRules.Borrow(skipped, 100000, out _);
            skipped.Day = 9; PinjolRules.Accrue(skipped);
            Assert.That(skipped.Debt, Is.EqualTo(expected));
        }

        [Test]
        public void RepayingUsesTheWalletFirstAndRecordsWhatTheLoanReallyCost()
        {
            var state = new AdventureState { Money = 0, Bank = 400000 };
            PinjolRules.Borrow(state, 300000, out _);          // dompet 270.000
            state.Day = 3; PinjolRules.Accrue(state);           // 300.000 → 315.000 → 330.750
            Assert.That(state.Debt, Is.EqualTo(330750));
            Assert.That(PinjolRules.Repay(state, 50000, out _), Is.True);
            Assert.That(state.Money, Is.EqualTo(220000)); Assert.That(state.Bank, Is.EqualTo(400000)); Assert.That(state.DebtPaid, Is.EqualTo(50000));
            Assert.That(PinjolRules.Repay(state, 9999999, out _), Is.True, "Lebih dari utang = lunasi sisanya saja.");
            Assert.That(state.Debt, Is.Zero); Assert.That(state.Money, Is.Zero); Assert.That(state.Bank, Is.EqualTo(400000 - (280750 - 220000)));
            Assert.That(state.Evidence.Any(e => e.Contains("ongkos utangnya Rp60") ), Is.True, "330.750 dibayar − 270.000 diterima = 60.750.");
            Assert.That(state.Valid(), Is.True); Assert.That(SideQuestTests.Reload(state).DebtPrincipal, Is.Zero);
            Assert.That(PinjolRules.Repay(state, 1000, out _), Is.False);

            var broke = new AdventureState { Money = 0, Bank = 0 };
            PinjolRules.Borrow(broke, 100000, out _); broke.Money = 10000;
            Assert.That(PinjolRules.Repay(broke, 100000, out _), Is.False, "Tidak bisa membayar lebih dari yang dimiliki.");
            Assert.That(broke.Debt, Is.EqualTo(100000)); Assert.That(broke.Money, Is.EqualTo(10000));
        }

        [Test]
        public void TamperedDebtIsRejectedAndDebtDoesNotFollowIntoTheNextChapter()
        {
            var state = new AdventureState { Money = 0, HighestUnlocked = 2 };
            PinjolRules.Borrow(state, 100000, out _);
            Assert.That(state.StartChapter(2).Debt, Is.Zero);
            state.DebtPrincipal = 0;
            Assert.That(AdventureSave.Decode(JsonUtility.ToJson(state)), Is.Null);
            Assert.That(new AdventureState { DebtPaid = 5 }.Valid(), Is.False, "Tanpa utang, semua kolom pinjol harus nol.");
            // Penutup demo menyebut utang yang masih berjalan, dan tetap diakhiri baris penutupnya.
            var outro = StoryContent.DemoOutroFor(-1, 150000);
            Assert.That(outro.Lines.Any(l => l.Contains(PinjolRules.AppName)), Is.True);
            Assert.That(outro.Lines.Last(), Is.EqualTo(StoryContent.DemoOutro.Lines.Last()));
        }

        [Test]
        public void InvestingLeavesAJournalTrailAndMaturityRecordsItsImpact()
        {
            var state = new AdventureState { Bank = 100000 };
            var market = InvestmentContent.Find("sukuk.pasar");
            Assert.That(InvestmentContent.Listings.All(l => !string.IsNullOrEmpty(l.Impact) && !string.IsNullOrEmpty(l.MaturityNote)), Is.True);
            InvestmentRules.Buy(state, market, 1, out _);
            Assert.That(state.Evidence.Any(e => e.Contains(market.Name)), Is.True);
            state.Day = 1 + market.TenorDays; InvestmentRules.Settle(state);
            Assert.That(state.Evidence, Does.Contain(market.MaturityNote));
        }
    }
}
