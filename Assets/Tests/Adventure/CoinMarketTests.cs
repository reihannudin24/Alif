using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Alif.Adventure.Tests
{
    public class CoinMarketTests
    {
        static readonly Coin Moon = CoinMarket.Find(CoinMarket.HypeSymbol);

        [Test]
        public void SearchMatchesSymbolOrNameAndPricesAreStablePerDay()
        {
            Assert.That(CoinMarket.Coins.Select(c => c.Symbol).Distinct().Count(), Is.EqualTo(CoinMarket.Coins.Length));
            Assert.That(CoinMarket.Search("").Count(), Is.EqualTo(CoinMarket.Coins.Length));
            Assert.That(CoinMarket.Search("  moon ").Single().Symbol, Is.EqualTo("MOON"));
            Assert.That(CoinMarket.Search("chain").Single().Symbol, Is.EqualTo("SOLO"));
            Assert.That(CoinMarket.Search("zzz"), Is.Empty);
            var state = new AdventureState();
            foreach (var coin in CoinMarket.Coins)
                for (int day = 1; day <= 10; day++)
                {
                    int price = CoinMarket.Price(coin, day, state);
                    Assert.That(price, Is.EqualTo(CoinMarket.Price(coin, day, state)), "Harga hari yang sama harus sama.");
                    if (coin != Moon) Assert.That(price, Is.InRange(coin.BasePrice * (100 - coin.Swing) / 100 - 1, coin.BasePrice * (100 + coin.Swing) / 100 + 1));
                }
            Assert.That(CoinMarket.Price(Moon, 9, state), Is.EqualTo(CoinMarket.HypePrice), "Sebelum dijawab, MOON diam di harga awal.");
        }

        [Test]
        public void JoiningNeedsCashFromTheAtmThenCrashesToAFifthAndThenToDust()
        {
            var state = new AdventureState { TutorialStep = 1 };
            Assert.That(CoinMarket.NpcPresent(state), Is.False, "Bang Jago tidak mengganggu tutorial.");
            state.TutorialStep = 0;
            Assert.That(CoinMarket.NpcPresent(state), Is.True);
            Assert.That(CoinMarket.AnswerFirst(state, true, out string feedback), Is.False, "Dompet Rp100.000: harus ke ATM dulu.");
            Assert.That(feedback, Does.Contain("ATM")); Assert.That(state.CoinFirst, Is.Zero, "Tawaran tetap terbuka.");

            state.Bank -= 900000; state.Money += 900000;                       // tarik tunai di ATM
            Assert.That(CoinMarket.AnswerFirst(state, true, out _), Is.True);
            Assert.That(state.Money, Is.Zero); Assert.That(state.CoinUnits, Is.EqualTo(1000));
            Assert.That(CoinMarket.AnswerFirst(state, false, out _), Is.False, "Tidak bisa dijawab dua kali.");
            Assert.That(CoinMarket.MorningKey(state), Is.Null);

            state.Day = 2; CoinMarket.NewDay(state); state = SideQuestTests.Reload(state);
            Assert.That(CoinMarket.ChangePercent(Moon, 2, state), Is.EqualTo(-80));
            Assert.That(CoinMarket.HoldingValue(state), Is.EqualTo(200000));
            Assert.That(CoinMarket.MorningKey(state), Is.EqualTo("crash")); Assert.That(CoinMarket.NpcPresent(state), Is.True);
            Assert.That(CoinMarket.SecondOfferOpen(state), Is.False);

            state.Day = 3; CoinMarket.NewDay(state); state = SideQuestTests.Reload(state);
            Assert.That(CoinMarket.HoldingValue(state), Is.EqualTo(10000));
            Assert.That(CoinMarket.MorningKey(state), Is.EqualTo("dust")); Assert.That(CoinMarket.NpcPresent(state), Is.False);
            Assert.That(CoinMarket.Sell(state, out _), Is.True);
            Assert.That(state.Money, Is.EqualTo(10000)); Assert.That(state.CoinUnits, Is.Zero);
            Assert.That(SideQuestTests.Reload(state).CoinSpent, Is.EqualTo(CoinMarket.Stake));
        }

        [Test]
        public void RefusingPumpsFourHundredPercentThenFomoBuyersLoseEverything()
        {
            foreach (bool tempted in new[] { true, false })
            {
                var state = new AdventureState();
                Assert.That(CoinMarket.AnswerFirst(state, false, out _), Is.True);
                Assert.That(CoinMarket.SecondOfferOpen(state), Is.False, "Tawaran kedua baru ada besok.");
                state.Day = 2; CoinMarket.NewDay(state);
                Assert.That(CoinMarket.ChangePercent(Moon, 2, state), Is.EqualTo(400));
                Assert.That(CoinMarket.MorningKey(state), Is.EqualTo("pump")); Assert.That(CoinMarket.SecondOfferOpen(state), Is.True);

                Assert.That(CoinMarket.AnswerSecond(state, tempted, out _), Is.True);
                Assert.That(CoinMarket.AnswerSecond(state, tempted, out _), Is.False);
                if (tempted)
                {
                    Assert.That(state.Money, Is.Zero, "Dompet dulu…"); Assert.That(state.Bank, Is.EqualTo(100000), "…lalu rekening.");
                    Assert.That(state.CoinUnits, Is.EqualTo(200), "Rp1.000.000 di harga puncak Rp5.000.");
                }
                state = SideQuestTests.Reload(state);

                state.Day = 3; CoinMarket.NewDay(state); state = SideQuestTests.Reload(state);
                Assert.That(CoinMarket.Price(Moon, 3, state), Is.Zero);
                Assert.That(CoinMarket.MorningKey(state), Is.EqualTo(tempted ? "rug.lost" : "rug.safe"));
                Assert.That(state.CoinUnits, Is.Zero, "Koin yang jadi nol dihapus.");
                Assert.That(state.Money + state.Bank, Is.EqualTo(tempted ? 100000 : 1100000));
                Assert.That(CoinMarket.NpcPresent(state), Is.False);
                Assert.That(StoryContent.CoinMorning(CoinMarket.MorningKey(state)), Is.Not.Null);
            }
        }

        [Test]
        public void UnansweredSecondOfferExpiresAndTamperedStateIsRejected()
        {
            var state = new AdventureState();
            CoinMarket.AnswerFirst(state, false, out _);
            state.Day = 3; CoinMarket.NewDay(state);
            Assert.That(state.CoinSecond, Is.EqualTo(CoinMarket.Refused)); Assert.That(CoinMarket.AnswerSecond(state, true, out _), Is.False);
            Assert.That(state.Valid(), Is.True);

            Assert.That(new AdventureState { CoinUnits = 5 }.Valid(), Is.False, "Koin tanpa pernah membeli.");
            Assert.That(new AdventureState { CoinFirst = CoinMarket.Joined, CoinDay = 1, CoinSecond = CoinMarket.Joined, CoinSpent = CoinMarket.Stake }.Valid(), Is.False);
            var poor = new AdventureState { Money = 0, Bank = 0 };
            CoinMarket.AnswerFirst(poor, false, out _); poor.Day = 2;
            Assert.That(CoinMarket.AnswerSecond(poor, true, out _), Is.False, "Tidak punya uang: tidak bisa ikut.");
            Assert.That(CoinMarket.SecondOfferOpen(poor), Is.True);

            // Setiap kunci pagi punya adegannya, dan penutup demo menyebut koin bila Alif bersinggungan dengannya.
            foreach (string key in new[] { "crash", "dust", "pump", "rug.lost", "rug.safe" }) Assert.That(StoryContent.AllScenes, Does.Contain(StoryContent.CoinMorning(key)));
            Assert.That(CoinMarket.OutroLine(new AdventureState()), Is.Null);
            var outro = StoryContent.DemoOutroFor(-1, 0, CoinMarket.OutroLine(state));
            Assert.That(outro.Lines.Any(l => l.Contains(CoinMarket.HypeSymbol)), Is.True);
            Assert.That(outro.Lines.Last(), Is.EqualTo(StoryContent.DemoOutro.Lines.Last()));
        }
    }
}
