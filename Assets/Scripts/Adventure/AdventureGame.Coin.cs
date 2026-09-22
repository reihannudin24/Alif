using System;
using System.Linq;
using Alif.Campaign;
using Alif.Systems;
using Alif.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Alif.Adventure
{
    /// <summary>
    /// Skenario koin "Bang Jago" di depan stasiun + layar Pasar Koin di aplikasi Investasi.
    /// Aturan, harga, dan jalan ceritanya ada di model (CoinMarket); di sini hanya NPC, dialog
    /// pilihan, adegan pagi, dan layarnya. Ikut berarti tunai Rp1.000.000 — Alif harus menarik uang di
    /// ATM dalam stasiun dulu. "Tidak, terima kasih" selalu jadi tombol yang terfokus.
    /// </summary>
    public sealed partial class AdventureGame
    {
        GameObject _coinNpc;
        static readonly Color CoinUp = new Color(.2f, .55f, .25f), CoinDown = new Color(.75f, .22f, .18f);

        // ───────────────────────── NPC di depan stasiun ─────────────────────────

        void BuildCoinNpc()
        {
            int area = Array.IndexOf(Content.Areas, CoinMarket.NpcArea);
            if (area < 0 || Spawns == null || area >= Spawns.Length) return;        // bab tanpa area stasiun
            // Titik spawn area pasti lantai yang bisa diinjak; Bang Jago berdiri sedikit di sampingnya
            // supaya tidak menghalangi pemain yang baru keluar dari stasiun.
            if (!TryFindNpcSpot(Spawns[area] + new Vector2(1.4f, .2f), out Vector2 spot))
            {
                Debug.LogWarning($"[Alif] {CoinMarket.NpcName} tidak dimunculkan: tidak ada lantai bebas di {CoinMarket.NpcArea}.");
                return;
            }
            _coinNpc = new GameObject("QuestNpc " + CoinMarket.NpcName);
            _coinNpc.transform.position = spot;
            var body = _coinNpc.AddComponent<SpriteRenderer>();
            var data = Cast.FirstOrDefault(c => c && c.name.EndsWith("dimas", StringComparison.OrdinalIgnoreCase));
            body.sprite = data ? (data.WorldSprite ? data.WorldSprite : data.Portrait) : null;
            body.color = new Color(1f, .86f, .45f);                                  // placeholder: sprite lama diwarnai emas
            _coinNpc.AddComponent<YSortOrder>();
            BlobShadow.Ensure(_coinNpc.transform);
            var feet = _coinNpc.AddComponent<CapsuleCollider2D>();
            feet.direction = CapsuleDirection2D.Horizontal; feet.size = new Vector2(.3f, .18f); feet.offset = new Vector2(0f, .09f);

            var trigger = new GameObject("Adventure interaction") { layer = 7 };
            trigger.transform.SetParent(_coinNpc.transform, false);
            var circle = trigger.AddComponent<CircleCollider2D>(); circle.isTrigger = true; circle.radius = .45f; circle.offset = new Vector2(0f, .3f);
            var point = trigger.AddComponent<AdventurePoint>(); point.Game = this; point.Target = CoinMarket.NpcId; point.Area = area;

            float head = body.sprite ? body.sprite.bounds.max.y : 1f;
            var name = WorldLabel(_coinNpc.transform, CoinMarket.NpcName, head + .12f, .42f, PixelSkin.TextLight);
            name.outlineColor = PixelSkin.Outline; name.outlineWidth = .25f;
            RefreshCoinNpc();
        }

        void RefreshCoinNpc() { if (_coinNpc && State != null) _coinNpc.SetActive(CoinMarket.NpcPresent(State)); }

        void InteractCoinNpc()
        {
            if (TutorialActive) { Toast("Selesaikan atau lewati tutorial dulu", 4); return; }
            string who = CoinMarket.NpcName;
            int since = State.Day - State.CoinDay;
            if (State.CoinFirst == 0)
                SayLines(new[]
                {
                    $"{who}|Bro! Sini, sini. Lihat HP-ku — MoonCempaka, koin paling panas minggu ini. Aku masuk kemarin, hari ini sudah cuan.",
                    $"{who}|Minimal sejuta, tunai. Besok bisa jadi lima juta. Dijamin! Yang telat masuk cuma bisa nonton.",
                    "Alif (Batin)|\"Dijamin\"… Koin ini usahanya apa? Asetnya apa? Dia tidak menyebut satu pun.",
                }, () => ShowCoinOffer(false, SetPlaying));
            else if (CoinMarket.SecondOfferOpen(State))
                SayLines(new[] { $"{who}|Nah, ini dia yang kemarin nolak! Lihat sendiri kan — naik 400%. Masih ada tempat, Bro. Sejuta saja." }, () => ShowCoinOffer(true, SetPlaying));
            else if (State.CoinFirst == CoinMarket.Joined)
                SayLines(new[] { since == 0 ? $"{who}|Mantap, Bro! Tinggal duduk manis. Besok kita pesta." : $"{who}|Santai… ini cuma koreksi sehat. Jangan dijual, HODL! Besok balik, dijamin." }, SetPlaying);
            else
                SayLines(new[] { since == 0 ? $"{who}|Yakin nih nolak? Jangan nyesel besok ya, Bro." : State.CoinSecond == CoinMarket.Joined ? $"{who}|Pilihan cerdas, Bro! Besok kita ke bulan." : $"{who}|Ya sudah. Rezeki orang beda-beda, Bro." }, SetPlaying);
        }

        /// <summary>Dialog pilihan ikut / menolak. <paramref name="then"/> dijalankan setelah pilihan diambil.</summary>
        void ShowCoinOffer(bool second, Action then)
        {
            PullBalances();
            ClearModal(CampaignActivity.Item);
            var panel = Panel(_modal, new Vector2(.24f, .2f), new Vector2(.76f, .8f), Color.white); ApplySprite(panel, PixelSkin.Panel());
            var p = panel.rectTransform;
            int price = CoinMarket.HypeToday(State);
            Text(p, second ? "Ikut sekarang, di harga puncak?" : $"Ikut beli {CoinMarket.HypeSymbol}?", new Vector2(.06f, .78f), new Vector2(.94f, .94f), 23).alignment = TextAlignmentOptions.Center;
            Text(p, $"{CoinMarket.NpcName} minta Rp{CoinMarket.Stake:N0} {(second ? "(dompet dulu, lalu rekening)" : "TUNAI")}. Harga {CoinMarket.HypeSymbol} hari ini Rp{price:N0}{(second ? " — naik 400% sejak kemarin" : "")}.\n" +
                    $"Dompet Rp{State.Money:N0}  •  Rekening Rp{State.Bank:N0}",
                new Vector2(.07f, .5f), new Vector2(.93f, .78f), 16).alignment = TextAlignmentOptions.Center;
            Text(p, "Tanyakan dulu: usahanya apa, asetnya apa, akadnya apa? Janji \"pasti naik\" tanpa jawaban itu adalah gharar.",
                new Vector2(.07f, .3f), new Vector2(.93f, .5f), 14, PixelSkin.Accent).alignment = TextAlignmentOptions.Center;
            // "Tidak" dibuat lebih dulu supaya jadi tombol yang terfokus.
            Button(p, "Tidak, terima kasih", new Vector2(.06f, .07f), new Vector2(.5f, .25f), () => AnswerCoinOffer(second, false, then));
            var join = Button(p, $"Ikut • Rp{CoinMarket.Stake:N0}", new Vector2(.54f, .07f), new Vector2(.94f, .25f), () => AnswerCoinOffer(second, true, then));
            PixelSkin.StyleButton(join, secondary: true);
            FocusFirst();
        }

        void AnswerCoinOffer(bool second, bool join, Action then)
        {
            PullBalances();
            bool ok = second ? CoinMarket.AnswerSecond(State, join, out string feedback) : CoinMarket.AnswerFirst(State, join, out feedback);
            CloseModal();
            if (!ok)
            {
                // Belum pegang tunai: tawaran tetap terbuka, Alif diarahkan ke ATM dalam stasiun.
                bool needsCash = !second && join && State.CoinFirst == 0;
                SayLines(new[] { needsCash ? $"{CoinMarket.NpcName}|Belum pegang tunai? ATM ada di dalam stasiun, Bro. Aku tunggu di sini — jangan lama-lama, nanti harganya keburu naik." : "Alif (Batin)|" + feedback },
                    () => { then?.Invoke(); if (needsCash) Toast(feedback, 7); });
                return;
            }
            CurrencySystem.Instance?.RestoreBalances(State.Money, State.Bank);
            if (join) ScoreSystem.Instance?.AdjustShariaCompliance(-8f);      // spekulasi tanpa aset: menjauh dari Kepatuhan Syariah
            else ScoreSystem.Instance?.MoveTowardsBalance(4f);
            Save(false); RefreshHud();
            SayLines(new[] { join ? $"{CoinMarket.NpcName}|MANTAP! Selamat datang di klub, Bro. Pantau terus di HP-mu — Investasi, Pasar Koin." : $"{CoinMarket.NpcName}|Yah… ya sudah. Jangan nyesel ya kalau besok terbang." },
                () => { then?.Invoke(); Toast(feedback + (join ? "  •  cek HP › Investasi › Pasar Koin" : ""), 7); });
        }

        /// <summary>Pagi setelah tidur: adegan koin hari ini (sekali), lalu tawaran kedua bila sedang +400%.</summary>
        void CoinMorning(Action then)
        {
            string key = CoinMarket.MorningKey(State);
            PlayStoryOnce(StoryContent.CoinMorning(key), () =>
            {
                if (key == "pump" && CoinMarket.SecondOfferOpen(State)) ShowCoinOffer(true, then);
                else then?.Invoke();
            });
        }

        // ───────────────────────── Pasar Koin (HP › Investasi) ─────────────────────────

        static string ChangeLabel(int percent) => percent > 0 ? $"+{percent}%" : $"{percent}%";

        void ShowCoinMarket(string query = "", int page = 0)
        {
            PullBalances();
            var screen = PhoneScreen($"PASAR KOIN • Hari ke-{State.Day}");
            var search = SearchField(screen, new Vector2(.02f, .79f), new Vector2(.72f, .88f), query, "Cari simbol atau nama koin…", text => ShowCoinMarket(text, 0));
            Button(screen, "Cari", new Vector2(.74f, .79f), new Vector2(.98f, .88f), () => ShowCoinMarket(search.text, 0));

            var coins = CoinMarket.Search(query).ToArray();
            const int perPage = 5;
            int pages = Mathf.Max(1, (coins.Length + perPage - 1) / perPage);
            page = Mathf.Clamp(page, 0, pages - 1);
            if (coins.Length == 0)
                Text(screen, $"Tidak ada koin bernama \"{query}\".", new Vector2(.06f, .45f), new Vector2(.94f, .65f), 16, PixelSkin.Cream).alignment = TextAlignmentOptions.Center;
            for (int i = 0; i < perPage; i++)
            {
                int n = page * perPage + i;
                if (n >= coins.Length) break;
                var coin = coins[n];
                int price = CoinMarket.Price(coin, State.Day, State), change = CoinMarket.ChangePercent(coin, State.Day, State);
                float top = .77f - i * .135f;
                var row = Panel(screen, new Vector2(.02f, top - .125f), new Vector2(.98f, top), Color.white);
                ApplySprite(row, PixelSkin.Slot()); row.raycastTarget = false;
                bool held = coin.Symbol == CoinMarket.HypeSymbol && State.CoinUnits > 0;
                Text(row.transform, $"{coin.Symbol}  •  {coin.Name}{(held ? "  (dimiliki)" : "")}", new Vector2(.03f, .5f), new Vector2(.6f, 1f), 16, PixelSkin.TextDark);
                Text(row.transform, price > 0 ? $"Rp{price:N0}" : "Dihapus dari bursa", new Vector2(.03f, 0f), new Vector2(.45f, .5f), 14, PixelSkin.TextDark);
                var delta = Text(row.transform, ChangeLabel(change), new Vector2(.45f, 0f), new Vector2(.72f, .5f), 15, change > 0 ? CoinUp : change < 0 ? CoinDown : PixelSkin.TextDark);
                delta.fontStyle = FontStyles.Bold;
                Button(row.transform, "Lihat", new Vector2(.76f, .12f), new Vector2(.98f, .88f), () => ShowCoin(coin, query, page));
            }
            if (pages > 1)
            {
                Button(screen, "<", new Vector2(.34f, -.02f), new Vector2(.44f, .08f), () => ShowCoinMarket(query, page - 1)).interactable = page > 0;
                Button(screen, ">", new Vector2(.56f, -.02f), new Vector2(.66f, .08f), () => ShowCoinMarket(query, page + 1)).interactable = page < pages - 1;
            }
            PhoneNav(screen, () => ShowInvestmentApp(0));
            FocusFirst();
        }

        void ShowCoin(Coin coin, string query, int page, string feedback = null)
        {
            PullBalances();
            var screen = PhoneScreen($"{coin.Symbol} • Hari ke-{State.Day}");
            var panel = Panel(screen, new Vector2(.02f, .1f), new Vector2(.98f, .88f), Color.white);
            ApplySprite(panel, PixelSkin.Panel()); panel.raycastTarget = false;
            var p = panel.transform;
            int price = CoinMarket.Price(coin, State.Day, State), change = CoinMarket.ChangePercent(coin, State.Day, State);
            Text(p, $"{coin.Name} ({coin.Symbol})", new Vector2(.04f, .86f), new Vector2(.96f, .98f), 21, PixelSkin.TextDark);
            Text(p, price > 0 ? $"Rp{price:N0}" : "Rp0 — dihapus dari bursa", new Vector2(.04f, .72f), new Vector2(.66f, .86f), 28, PixelSkin.TextDark);
            Text(p, ChangeLabel(change), new Vector2(.66f, .72f), new Vector2(.96f, .86f), 24, change > 0 ? CoinUp : change < 0 ? CoinDown : PixelSkin.TextDark).alignment = TextAlignmentOptions.MidlineRight;

            // Riwayat lima hari terakhir: batang setinggi harga relatif terhadap yang tertinggi.
            int from = Mathf.Max(1, State.Day - 4), days = State.Day - from + 1;
            int[] history = Enumerable.Range(from, days).Select(d => CoinMarket.Price(coin, d, State)).ToArray();
            float peak = Mathf.Max(1, history.Max());
            for (int i = 0; i < days; i++)
            {
                float x = .06f + i * .18f, height = Mathf.Max(.01f, .2f * history[i] / peak);
                bool up = i > 0 && history[i] > history[i - 1], down = i > 0 && history[i] < history[i - 1];
                var bar = Panel(p, new Vector2(x, .47f), new Vector2(x + .1f, .47f + height), up ? CoinUp : down ? CoinDown : PixelSkin.Accent); bar.raycastTarget = false;
                Text(p, $"H{from + i}", new Vector2(x - .03f, .4f), new Vector2(x + .13f, .47f), 12, PixelSkin.TextDark).alignment = TextAlignmentOptions.Center;
            }
            Text(p, coin.Note, new Vector2(.04f, .27f), new Vector2(.96f, .4f), 13, PixelSkin.TextDark).alignment = TextAlignmentOptions.TopLeft;
            Text(p, "Spekulatif: tanpa aset atau usaha nyata di baliknya. Naik-turunnya tebakan (gharar) — bukan instrumen syariah.",
                new Vector2(.04f, .16f), new Vector2(.96f, .27f), 13, PixelSkin.Accent).alignment = TextAlignmentOptions.TopLeft;

            if (coin.Symbol == CoinMarket.HypeSymbol && State.CoinUnits > 0)
            {
                Text(p, feedback ?? $"Kamu pegang {State.CoinUnits:N0} {coin.Symbol}  •  modal Rp{State.CoinSpent:N0}  •  kini Rp{CoinMarket.HoldingValue(State):N0}",
                    new Vector2(.04f, .03f), new Vector2(.62f, .15f), 13, PixelSkin.TextDark).alignment = TextAlignmentOptions.MidlineLeft;
                Button(p, "Jual semua", new Vector2(.64f, .03f), new Vector2(.96f, .15f), () =>
                {
                    PullBalances();
                    bool ok = CoinMarket.Sell(State, out string result);
                    if (ok) { CurrencySystem.Instance?.RestoreBalances(State.Money, State.Bank); Save(false); RefreshHud(); }
                    ShowCoin(coin, query, page, result);
                }).interactable = price > 0;
            }
            else Text(p, feedback ?? "Alif (Batin): Aku cuma memantau. Harga yang bergerak bukan alasan untuk membeli.", new Vector2(.04f, .03f), new Vector2(.96f, .15f), 13, PixelSkin.TextDark).alignment = TextAlignmentOptions.MidlineLeft;
            PhoneNav(screen, () => ShowCoinMarket(query, page));
            FocusFirst();
        }

        /// <summary>Kolom teks sederhana (TMP_InputField dirakit lewat kode, gaya slot krem). Enter = kirim.</summary>
        TMP_InputField SearchField(Transform parent, Vector2 min, Vector2 max, string value, string hint, Action<string> submit)
        {
            var box = Panel(parent, min, max, Color.white); ApplySprite(box, PixelSkin.Slot());
            var input = box.gameObject.AddComponent<TMP_InputField>();
            var area = CampaignUI.Rect(box.transform, "Text Area", Vector2.zero, Vector2.one);
            area.offsetMin = new Vector2(14f, 4f); area.offsetMax = new Vector2(-14f, -4f);
            area.gameObject.AddComponent<RectMask2D>();
            var placeholder = Text(area, hint, Vector2.zero, Vector2.one, 15, new Color(.55f, .48f, .4f));
            var text = Text(area, "", Vector2.zero, Vector2.one, 16, PixelSkin.TextDark);
            foreach (var label in new[] { placeholder, text })
            { label.alignment = TextAlignmentOptions.MidlineLeft; label.textWrappingMode = TextWrappingModes.NoWrap; label.margin = Vector4.zero; label.raycastTarget = false; }
            input.targetGraphic = box; input.textViewport = area; input.textComponent = text; input.placeholder = placeholder;
            input.lineType = TMP_InputField.LineType.SingleLine; input.characterLimit = 16;
            input.text = value ?? "";
            input.onSubmit.AddListener(entered => submit(entered));
            return input;
        }
    }
}
