using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Alif.Adventure.Tests
{
    public class InvestmentTests
    {
        static readonly InvestmentListing Campus = InvestmentContent.Find("sukuk.senirupa");

        [Test]
        public void EveryListingBelongsToARealCardAndKeepsReturnsModest()
        {
            Assert.That(InvestmentContent.Listings.Select(l => l.Id).Distinct().Count(), Is.EqualTo(InvestmentContent.Listings.Length));
            foreach (var listing in InvestmentContent.Listings)
            {
                Assert.That(StoryContent.Card(listing.Card), Is.Not.Null, listing.Id);
                Assert.That(listing.UnitPrice, Is.GreaterThan(0)); Assert.That(listing.PeriodDays, Is.GreaterThan(0)); Assert.That(listing.Periods, Is.GreaterThan(0));
                Assert.That(listing.Description, Is.Not.Empty); Assert.That(listing.Risk, Is.Not.Empty); Assert.That(listing.Akad, Is.Not.Empty);
                // Janji hasil tinggi adalah tanda bahaya (materi Bab 5) — penawaran resmi tidak boleh mencontohkannya.
                Assert.That(listing.RateBp, Is.InRange(1, 300), listing.Id);
            }
            // Sukuk bisa dibuka dari awal karena punya penawaran; instrumen lain menunggu kartunya.
            var fresh = new AdventureState();
            Assert.That(InvestmentRules.Open(fresh, StoryContent.Card("sukuk")), Is.True);
            Assert.That(InvestmentRules.Open(fresh, StoryContent.Card("saham")), Is.False);
        }

        [Test]
        public void BuyingEmptiesTheWalletFirstThenTheBankAndInvalidPurchasesChangeNothing()
        {
            var state = new AdventureState { TutorialStep = 1 };
            Assert.That(InvestmentRules.Buy(state, Campus, 1, out _), Is.False, "Tutorial masih aktif.");
            state.TutorialStep = 0;
            Assert.That(InvestmentRules.Buy(state, Campus, 0, out _), Is.False);
            Assert.That(InvestmentRules.Buy(state, Campus, InvestmentRules.MaxUnits + 1, out _), Is.False);
            Assert.That(InvestmentRules.Buy(state, null, 1, out _), Is.False);
            state.Money = 50000; state.Bank = 200000;
            Assert.That(InvestmentRules.Buy(state, Campus, 3, out _), Is.False, "Dompet + rekening kurang.");
            Assert.That(state.Money, Is.EqualTo(50000)); Assert.That(state.Bank, Is.EqualTo(200000)); Assert.That(state.Holdings, Is.Empty);

            Assert.That(InvestmentRules.Buy(state, Campus, 2, out _), Is.True);
            Assert.That(state.Money, Is.Zero, "Dompet (uang yang terlihat di HUD) dipakai lebih dulu.");
            Assert.That(state.Bank, Is.EqualTo(50000), "Sisanya baru dari rekening.");
            Assert.That(InvestmentRules.Invested(state), Is.EqualTo(200000));
            Assert.That(SideQuestTests.Reload(state).Holdings.Single().Units, Is.EqualTo(2));
        }

        [Test]
        public void UjrahIsPaidEachPeriodAndPrincipalReturnsAtMaturityWithReloadEveryDay()
        {
            var state = new AdventureState { Money = 100000, Bank = 500000 };
            Assert.That(InvestmentRules.Buy(state, Campus, 3, out _), Is.True);   // 300.000, ujrah 2% = 6.000 per 2 hari, 5 periode
            Assert.That(state.Money, Is.Zero); Assert.That(state.Bank, Is.EqualTo(300000));
            int paid = 0;
            for (int day = 2; day <= 1 + Campus.TenorDays; day++)
            {
                state.Day = day;
                string note = InvestmentRules.Settle(state);
                bool payday = (day - 1) % Campus.PeriodDays == 0;
                Assert.That(note != null, Is.EqualTo(payday), "Hari ke-" + day);
                if (payday) paid++;
                bool matured = paid == Campus.Periods;
                Assert.That(state.Money, Is.EqualTo(paid * 6000 + (matured ? 300000 : 0)), "Ujrah & pokok masuk dompet • Hari ke-" + day);
                Assert.That(state.Bank, Is.EqualTo(300000));
                Assert.That(InvestmentRules.Settle(state), Is.Null, "Hari yang sama tidak dibayar dua kali.");
                state = SideQuestTests.Reload(state);
            }
            Assert.That(state.Holdings, Is.Empty, "Jatuh tempo: pokok kembali dan kepemilikan ditutup.");
            Assert.That(state.Money + state.Bank, Is.EqualTo(600000 + 5 * 6000));
        }

        [Test]
        public void SkippedDaysAreSettledTogetherAndTamperedHoldingsAreRejected()
        {
            var state = new AdventureState { Money = 100000, Bank = 0 };
            InvestmentRules.Buy(state, Campus, 1, out _);
            state.Day = 1 + 3 * Campus.PeriodDays;              // tiga periode lewat sekaligus
            Assert.That(InvestmentRules.Settle(state), Is.Not.Null);
            Assert.That(state.Money, Is.EqualTo(3 * 2000)); Assert.That(state.Holdings.Single().PaidPeriods, Is.EqualTo(3));

            state.Holdings[0].PaidPeriods = 4;                   // dibayar lebih dari hari yang berlalu
            Assert.That(state.Valid(), Is.False);
            state.Holdings[0].PaidPeriods = 3; state.Holdings[0].Listing = "tidak.ada";
            Assert.That(AdventureSave.Decode(JsonUtility.ToJson(state)), Is.Null);

            // Uang disetel ulang tiap bab, jadi investasi pun tidak dibawa (tidak ada uang ganda).
            var next = new AdventureState { HighestUnlocked = 2, Bank = 100000 };
            InvestmentRules.Buy(next, Campus, 1, out _);
            Assert.That(next.StartChapter(2).Holdings, Is.Empty);
        }
    }
}
