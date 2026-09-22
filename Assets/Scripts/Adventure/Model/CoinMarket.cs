using System;
using System.Collections.Generic;
using System.Linq;

namespace Alif.Adventure
{
    /// <summary>Satu koin di "Pasar Koin" (aplikasi Investasi). Harganya fiktif dan deterministik per
    /// hari cerita, jadi bisa dicek kapan saja tanpa disimpan.</summary>
    public sealed class Coin
    {
        public string Symbol, Name, Note;
        public int BasePrice;
        /// <summary>Ayunan harian maksimum (%) untuk koin hiasan; koin skenario mengabaikannya.</summary>
        public int Swing;
    }

    /// <summary>
    /// Pasar koin spekulatif + skenario "Bang Jago" di depan stasiun: pelajaran FOMO dan
    /// pump-and-dump. Koin tidak punya aset atau usaha nyata di baliknya — naik-turunnya murni
    /// tebakan (gharar) dan untung-untungan (maysir) — dan jalan ceritanya memperlihatkan itu:
    /// <list type="bullet">
    /// <item>Alif IKUT (Rp1.000.000 tunai, harus tarik di ATM dulu): besoknya harga tinggal seperlima
    /// (−80%), lusa nyaris nol. "Turun 500%" tidak ada — harga paling jauh turun 100%.</item>
    /// <item>Alif MENOLAK: besoknya harga naik 400% dan ia ditawari lagi (godaan FOMO). Ikut atau
    /// tidak, lusa koinnya jadi 0 (pengembangnya kabur); yang ikut kehilangan seluruh uangnya.</item>
    /// </list>
    /// </summary>
    public static class CoinMarket
    {
        public const string NpcId = "npc:koin", NpcName = "Bang Jago", NpcArea = "Depan stasiun", HypeSymbol = "MOON";
        public const int Stake = 1000000, HypePrice = 1000, MaxUnits = 2000;
        public const int Joined = 1, Refused = 2;

        public static readonly Coin[] Coins =
        {
            new Coin { Symbol = HypeSymbol, Name = "MoonCempaka", BasePrice = HypePrice, Note = "Koin baru yang ramai dibicarakan di depan stasiun. Tidak ada laporan usaha, tidak ada aset — hanya janji \"pasti naik\"." },
            new Coin { Symbol = "BTRA", Name = "Batara", BasePrice = 925000, Swing = 6, Note = "Koin paling tua di bursa ini. Harganya besar, ayunannya juga." },
            new Coin { Symbol = "SOLO", Name = "SoloChain", BasePrice = 48000, Swing = 9, Note = "Koin jaringan lokal. Naik-turun mengikuti kabar, bukan kinerja usaha." },
            new Coin { Symbol = "KLTX", Name = "KilatX", BasePrice = 12500, Swing = 14, Note = "Dipromosikan akun-akun anonim. Volume jual-belinya tipis." },
            new Coin { Symbol = "GARU", Name = "GarudaBit", BasePrice = 310000, Swing = 5, Note = "Relatif tenang, tetapi tetap tanpa aset dasar." },
            new Coin { Symbol = "DOGI", Name = "DogiKoin", BasePrice = 250, Swing = 22, Note = "Koin lelucon. Harganya digerakkan meme dan unggahan pesohor." },
            new Coin { Symbol = "PADI", Name = "PadiToken", BasePrice = 7200, Swing = 11, Note = "Mengaku terkait hasil panen, tetapi tidak ada akad atau laporan yang bisa diperiksa." },
            new Coin { Symbol = "WARG", Name = "WargaCoin", BasePrice = 1850, Swing = 17, Note = "Koin komunitas. Pemegang terbesarnya hanya tiga dompet." },
        };

        public static Coin Find(string symbol) => Coins.FirstOrDefault(c => c.Symbol == symbol);

        /// <summary>Cari berdasarkan simbol atau nama (tanpa beda huruf besar-kecil); kosong = semua.</summary>
        public static IEnumerable<Coin> Search(string query)
        {
            query = (query ?? "").Trim();
            return query.Length == 0 ? Coins : Coins.Where(c =>
                c.Symbol.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 || c.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        /// <summary>Harga satu koin pada hari cerita tertentu.</summary>
        public static int Price(Coin coin, int day, AdventureState state)
        {
            if (coin.Symbol == HypeSymbol) return HypePriceOn(state, day);
            // Ayunan semu-acak yang stabil per (simbol, hari): sama di setiap perangkat dan setiap muat ulang.
            uint hash = 2166136261;
            foreach (char c in coin.Symbol) hash = (hash ^ c) * 16777619;
            hash = (hash ^ (uint)day) * 16777619; hash ^= hash >> 13; hash *= 0x5bd1e995; hash ^= hash >> 15;
            int wave = (int)(hash % (uint)(2 * coin.Swing + 1)) - coin.Swing;
            return (int)Math.Max(1, (long)coin.BasePrice * (100 + wave) / 100);
        }

        /// <summary>Perubahan harga (%) dibanding hari sebelumnya; harga kemarin 0 dianggap tanpa perubahan.</summary>
        public static int ChangePercent(Coin coin, int day, AdventureState state)
        {
            int before = Price(coin, Math.Max(1, day - 1), state), now = Price(coin, day, state);
            return day <= 1 || before <= 0 ? 0 : (int)Math.Round((now - before) * 100.0 / before);
        }

        static int HypePriceOn(AdventureState state, int day)
        {
            if (state == null || state.CoinFirst == 0) return HypePrice;
            int since = day - state.CoinDay;
            if (since <= 0) return HypePrice;
            if (state.CoinFirst == Joined) return since == 1 ? HypePrice / 5 : HypePrice / 100;   // −80%, lalu nyaris nol
            return since == 1 ? HypePrice * 5 : 0;                                              // +400%, lalu pengembangnya kabur
        }

        public static int HypeToday(AdventureState state) => HypePriceOn(state, state.Day);
        public static int HoldingValue(AdventureState state) => (int)Math.Min(int.MaxValue, (long)state.CoinUnits * HypeToday(state));

        /// <summary>Bang Jago nongkrong di depan stasiun sampai sehari setelah tawaran pertamanya dijawab; lusa ia lenyap.</summary>
        public static bool NpcPresent(AdventureState state) =>
            state.TutorialStep == 0 && (state.CoinFirst == 0 || state.Day <= state.CoinDay + 1);

        /// <summary>Tawaran kedua hanya ada sehari: pagi setelah Alif menolak, saat harga sedang +400%.</summary>
        public static bool SecondOfferOpen(AdventureState state) =>
            state.CoinFirst == Refused && state.CoinSecond == 0 && state.Day == state.CoinDay + 1;

        /// <summary>Jawaban atas ajakan pertama. Ikut berarti Rp1.000.000 TUNAI dari dompet — kalau
        /// belum ada, tidak ada yang berubah dan Alif harus ke ATM dulu.</summary>
        public static bool AnswerFirst(AdventureState state, bool join, out string feedback)
        {
            feedback = "Tawaran ini sudah dijawab.";
            if (state.CoinFirst != 0) return false;
            if (state.TutorialStep != 0) { feedback = "Selesaikan tutorial dulu."; return false; }
            if (join)
            {
                if (state.Money < Stake) { feedback = $"Tunai di dompet kurang Rp{Stake - state.Money:N0}. Tarik dulu di ATM dalam stasiun."; return false; }
                state.Money -= Stake; state.CoinUnits += Stake / HypePrice; state.CoinSpent += Stake;
                Note(state, $"Koin {HypeSymbol}: ikut ajakan {NpcName}, Rp{Stake:N0} tunai. Tidak ada aset atau usaha yang bisa diperiksa — hanya janji \"pasti naik\".");
            }
            else Note(state, $"Koin {HypeSymbol}: menolak ajakan {NpcName}. Janji untung besar tanpa aset yang jelas adalah gharar.");
            state.CoinFirst = join ? Joined : Refused; state.CoinDay = state.Day;
            feedback = join ? $"{Stake / HypePrice:N0} {HypeSymbol} dibeli seharga Rp{Stake:N0}." : "Kamu menolak dengan sopan.";
            return true;
        }

        /// <summary>Jawaban atas tawaran kedua (saat +400%). Dibayar dari dompet dulu, lalu rekening.</summary>
        public static bool AnswerSecond(AdventureState state, bool join, out string feedback)
        {
            feedback = "Tawaran ini sudah tidak berlaku.";
            if (!SecondOfferOpen(state)) return false;
            if (join)
            {
                if (state.Money + state.Bank < Stake) { feedback = $"Uangmu kurang Rp{Stake - state.Money - state.Bank:N0}."; return false; }
                int price = HypeToday(state), fromWallet = Math.Min(Stake, state.Money);
                state.Money -= fromWallet; state.Bank -= Stake - fromWallet;
                state.CoinUnits += Stake / price; state.CoinSpent += Stake;
                Note(state, $"Koin {HypeSymbol}: tergoda setelah naik 400% dan membeli di harga puncak Rp{price:N0}. Takut ketinggalan (FOMO) bukan alasan berinvestasi.");
                feedback = $"{Stake / price:N0} {HypeSymbol} dibeli di harga Rp{price:N0}.";
            }
            else
            {
                Note(state, $"Koin {HypeSymbol}: tetap menolak meski harganya naik 400%. Kenaikan harga bukan bukti — yang diperiksa tetap aset dan akadnya.");
                feedback = "Kamu tetap menolak.";
            }
            state.CoinSecond = join ? Joined : Refused;
            return true;
        }

        /// <summary>Jual semua koin di harga hari ini; uangnya masuk dompet. Tidak bisa saat harga 0.</summary>
        public static bool Sell(AdventureState state, out string feedback)
        {
            feedback = "Kamu tidak memegang koin.";
            if (state.CoinUnits <= 0) return false;
            int price = HypeToday(state);
            if (price <= 0) { feedback = "Koin ini sudah dihapus dari bursa — tidak ada pembeli."; return false; }
            int value = HoldingValue(state);
            state.Money = Math.Min(InvestmentRules.MaxBalance, state.Money + value);
            Note(state, $"Koin {HypeSymbol} dijual: modal Rp{state.CoinSpent:N0}, kembali Rp{value:N0}.");
            feedback = $"{state.CoinUnits:N0} {HypeSymbol} terjual Rp{value:N0} (modal Rp{state.CoinSpent:N0}).";
            state.CoinUnits = 0;
            return true;
        }

        /// <summary>Dipanggil tiap hari cerita berganti: tawaran kedua yang tidak dijawab hangus
        /// (dianggap menolak), dan koin yang harganya sudah 0 dihapus dari dompet dengan catatan.</summary>
        public static void NewDay(AdventureState state)
        {
            if (state.CoinFirst == Refused && state.CoinSecond == 0 && state.Day > state.CoinDay + 1) state.CoinSecond = Refused;
            if (state.CoinUnits > 0 && HypeToday(state) == 0)
            {
                Note(state, $"Koin {HypeSymbol} lenyap dari bursa: Rp{state.CoinSpent:N0} hilang. Pengembangnya menjual semua koinnya di puncak harga, lalu kabur.");
                state.CoinUnits = 0;
            }
        }

        /// <summary>Kunci adegan pagi sesuai jalan cerita hari ini (lihat StoryContent.CoinMorning); null = tidak ada.</summary>
        public static string MorningKey(AdventureState state)
        {
            if (state.CoinFirst == 0) return null;
            int since = state.Day - state.CoinDay;
            if (since < 1) return null;
            if (state.CoinFirst == Joined) return since == 1 ? "crash" : "dust";
            if (since == 1) return "pump";
            return state.CoinSecond == Joined ? "rug.lost" : "rug.safe";
        }

        /// <summary>Satu baris untuk penutup demo, atau null bila Alif belum bersinggungan dengan koin.</summary>
        public static string OutroLine(AdventureState state)
        {
            if (state.CoinFirst == 0) return null;
            if (state.CoinFirst == Joined)
                return state.CoinUnits > 0
                    ? $"Alif (Batin)|Sejuta rupiahku di {HypeSymbol} tinggal Rp{HoldingValue(state):N0}. \"Pasti naik\" ternyata bukan akad — cuma kalimat."
                    : $"Alif (Batin)|Sejuta rupiahku di {HypeSymbol} tidak pernah kembali utuh. \"Pasti naik\" ternyata bukan akad — cuma kalimat.";
            return state.CoinSecond == Joined
                ? $"Alif (Batin)|Kemarin kutolak, pagi ini aku tergoda karena harganya naik 400%. Kalau besok {HypeSymbol} jatuh, yang kubeli hanyalah rasa takut ketinggalan."
                : $"Alif (Batin)|{HypeSymbol} naik 400% dan aku tetap menolak. Anehnya, aku tidak menyesal: aku tidak pernah tahu apa yang sebenarnya dijual koin itu.";
        }

        static void Note(AdventureState state, string note) { if (!state.Evidence.Contains(note)) state.Evidence.Add(note); }

        public static bool Valid(AdventureState state)
        {
            if (state.CoinFirst < 0 || state.CoinFirst > Refused || state.CoinSecond < 0 || state.CoinSecond > Refused ||
                state.CoinUnits < 0 || state.CoinUnits > MaxUnits || state.CoinSpent < 0 || state.CoinSpent > 2 * Stake) return false;
            if (state.CoinFirst == 0) return state.CoinDay == 0 && state.CoinSecond == 0 && state.CoinUnits == 0 && state.CoinSpent == 0;
            if (state.CoinDay < 1 || state.CoinDay > state.Day) return false;
            if (state.CoinSecond != 0 && state.CoinFirst != Refused) return false;
            bool bought = state.CoinFirst == Joined || state.CoinSecond == Joined;
            return bought ? state.CoinSpent == Stake : state.CoinUnits == 0 && state.CoinSpent == 0;
        }
    }
}
