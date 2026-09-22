using System;
using System.Linq;
using Alif.Campaign;
using Alif.Core;
using Alif.Systems;
using TMPro;
using UnityEngine;

namespace Alif.Adventure
{
    /// <summary>
    /// Adegan cerita layar penuh bergaya cutscene (StoryContent): gambar menutupi layar di
    /// belakang kotak dialog, tokoh digambar besar, tombol "Lewati" di atas segalanya. Diputar
    /// sekali saja — saat bab dimulai, saat pertama bertemu tokoh main quest, dan saat pertama
    /// bertemu NPC side quest. Gambar & tokoh masih placeholder dari aset yang ada.
    /// </summary>
    public sealed partial class AdventureGame
    {
        StoryOverlay _story;

        bool StorySeen(StoryScene scene) => scene == null || State.SeenStories.Contains(scene.Id);

        /// <summary>Putar adegan bila belum pernah dilihat, lalu lanjutkan; bila sudah, langsung lanjut.</summary>
        void PlayStoryOnce(StoryScene scene, Action then)
        {
            if (StorySeen(scene)) { then?.Invoke(); return; }
            State.SeenStories.Add(scene.Id);
            Save(false);
            PlayStory(scene, then);
        }

        /// <summary>Putar adegan tanpa mencatatnya (mis. pembuka bab, yang diputar setiap bab dimulai).
        /// Tampilannya sama dengan scene cutscene: ilustrasi penuh + kotak dialog lebar bertab nama.</summary>
        void PlayStory(StoryScene scene, Action then)
        {
            if (_story) Destroy(_story.gameObject);
            if (_modal) { Destroy(_modal.gameObject); _modal = null; }
            Activity = CampaignActivity.Dialogue;
            _hud.gameObject.SetActive(false);
            SetTouchControlsVisible();
            _player?.SetMovementLocked(this, true); _player?.SetAdventureDirection(Vector2.zero); _walkTarget = null;
            GameManager.Instance?.SetState(GameManager.GameState.Dialogue);

            _story = StoryOverlay.Play(scene, StoryArtLibrary.Find, StoryFigure, () =>
            {
                _story = null;
                _hud.gameObject.SetActive(true);
                SetPlaying();
                then?.Invoke();
            });
        }

        /// <summary>Potret besar tokoh adegan (nama CharacterData placeholder); null bila tanpa tokoh.</summary>
        Sprite StoryFigure(string character)
        {
            var data = string.IsNullOrEmpty(character) ? null : Cast.FirstOrDefault(c => c && c.name.EndsWith(character, StringComparison.OrdinalIgnoreCase));
            return data ? (data.Portrait ? data.Portrait : data.WorldSprite) : null;
        }

        /// <summary>Hati hubungan: ♥ penuh + ♥ pudar (glyph ♡ tidak ada di font).</summary>
        static string HeartText(int hearts) =>
            new string('♥', hearts) + "<color=#C4AA88>" + new string('♥', SideQuestRules.MaxHearts - hearts) + "</color>";

        // ───────────────────────── Kartu investasi ─────────────────────────

        void UnlockCard(string cardId)
        {
            var card = StoryContent.Card(cardId);
            if (card == null || State.Cards.Contains(cardId)) return;
            State.Cards.Add(cardId);
            Toast($"Kartu investasi baru: {card.Name} • buka HP › Investasi", 7);
        }

        // ───────────────────────── Aplikasi Investasi (HP) ─────────────────────────
        // Instrumen → penjelasan → penawaran berjalan → beli dari rekening → portofolio.
        // Aturan & angkanya ada di model (InvestmentContent / InvestmentRules); di sini hanya layar.

        /// <summary>Saldo yang hidup ada di CurrencySystem; tarik ke State sebelum menghitung apa pun.</summary>
        void PullBalances()
        {
            if (!CurrencySystem.Instance) return;
            State.Money = CurrencySystem.Instance.CurrentMoney; State.Bank = CurrencySystem.Instance.BankBalance;
        }

        void ShowInvestmentApp(int page = 0)
        {
            PullBalances();
            var screen = PhoneScreen($"INVESTASI • Dompet Rp{State.Money:N0} • Rek Rp{State.Bank:N0}");
            var summary = Panel(screen, new Vector2(.02f, .77f), new Vector2(.98f, .88f), Color.white);
            ApplySprite(summary, PixelSkin.Panel()); summary.raycastTarget = false;
            Text(summary.transform, $"Rp{InvestmentRules.Invested(State):N0} • {State.Holdings.Count} aktif • {State.Cards.Count}/{StoryContent.Cards.Length} kartu",
                new Vector2(.03f, 0f), new Vector2(.47f, 1f), 13, PixelSkin.TextDark).alignment = TextAlignmentOptions.MidlineLeft;
            Button(summary.transform, "Pasar Koin", new Vector2(.48f, .12f), new Vector2(.72f, .88f), () => ShowCoinMarket());
            Button(summary.transform, "Portofolio", new Vector2(.74f, .12f), new Vector2(.98f, .88f), () => ShowPortfolio(0));

            // Instrumen yang sudah bisa dibuka tampil lebih dulu.
            var cards = StoryContent.Cards.OrderByDescending(c => InvestmentRules.Open(State, c)).ToArray();
            const int perPage = 4;
            int pages = (cards.Length + perPage - 1) / perPage;
            page = Mathf.Clamp(page, 0, pages - 1);
            var next = SideQuestContent.All.FirstOrDefault(q => q.Giver != null && SideQuestRules.Available(State, q));
            for (int i = 0; i < perPage; i++)
            {
                int n = page * perPage + i;
                if (n >= cards.Length) break;
                var card = cards[n];
                bool open = InvestmentRules.Open(State, card);
                int offers = InvestmentContent.ListingsFor(card.Id).Count();
                float top = .75f - i * .165f;
                var row = Panel(screen, new Vector2(.02f, top - .15f), new Vector2(.98f, top), Color.white);
                ApplySprite(row, PixelSkin.Slot()); row.raycastTarget = false;
                Text(row.transform, open ? card.Name : "???", new Vector2(.03f, .5f), new Vector2(.72f, 1f), 17, PixelSkin.TextDark);
                string hint = open ? (offers > 0 ? $"{offers} penawaran berjalan  •  Risiko: {card.Risk}" : $"Risiko: {card.Risk}")
                    : next != null && next.Steps.Any(s => s.Card == card.Id) ? $"Temui {SideQuestContent.Npc(next.Giver)?.Name}" : "Belum terbuka";
                Text(row.transform, hint, new Vector2(.03f, 0f), new Vector2(.72f, .5f), 13, PixelSkin.Accent);
                if (open) Button(row.transform, "Buka", new Vector2(.74f, .15f), new Vector2(.98f, .85f), () => ShowInvestmentCard(card, page));
            }
            if (pages > 1)
            {
                Button(screen, "<", new Vector2(.34f, -.02f), new Vector2(.44f, .08f), () => ShowInvestmentApp(page - 1)).interactable = page > 0;
                Button(screen, ">", new Vector2(.56f, -.02f), new Vector2(.66f, .08f), () => ShowInvestmentApp(page + 1)).interactable = page < pages - 1;
            }
            PhoneNav(screen, OpenPhone);
            FocusFirst();
        }

        /// <summary>Halaman penjelasan instrumen — selalu dibaca dulu sebelum melihat penawarannya.</summary>
        void ShowInvestmentCard(InvestmentCard card, int page)
        {
            var screen = PhoneScreen("KENALI DULU");
            var panel = Panel(screen, new Vector2(.02f, .1f), new Vector2(.98f, .88f), Color.white);
            ApplySprite(panel, PixelSkin.Panel()); panel.raycastTarget = false;
            var p = panel.transform;
            Text(p, card.Name, new Vector2(.03f, .86f), new Vector2(.97f, .98f), 22, PixelSkin.TextDark);
            Text(p, "Contoh: " + card.Examples, new Vector2(.03f, .7f), new Vector2(.97f, .86f), 15, PixelSkin.TextDark);
            Text(p, "Cara hasil: " + card.Returns, new Vector2(.03f, .57f), new Vector2(.97f, .7f), 15, PixelSkin.TextDark);
            Text(p, "Risiko umum: " + card.Risk, new Vector2(.03f, .47f), new Vector2(.97f, .57f), 15, PixelSkin.Accent);
            Text(p, card.Lesson, new Vector2(.03f, .17f), new Vector2(.97f, .47f), 15, PixelSkin.TextDark).alignment = TextAlignmentOptions.TopLeft;
            int offers = InvestmentContent.ListingsFor(card.Id).Count();
            if (offers > 0) Button(p, $"Lihat penawaran ({offers})", new Vector2(.5f, .03f), new Vector2(.97f, .15f), () => ShowListings(card, page));
            else Text(p, "Belum ada penawaran berjalan untuk instrumen ini.", new Vector2(.03f, .03f), new Vector2(.97f, .15f), 13, PixelSkin.Accent);
            PhoneNav(screen, () => ShowInvestmentApp(page));
            FocusFirst();
        }

        void ShowListings(InvestmentCard card, int page)
        {
            PullBalances();
            var screen = PhoneScreen($"Dompet Rp{State.Money:N0} • Rek Rp{State.Bank:N0}");
            Text(screen, $"{card.Name} — sedang menghimpun dana", new Vector2(.02f, .8f), new Vector2(.98f, .88f), 16, PixelSkin.Cream);
            var listings = InvestmentContent.ListingsFor(card.Id).Take(3).ToArray();
            for (int i = 0; i < listings.Length; i++)
            {
                var listing = listings[i];
                float top = .78f - i * .23f;
                var row = Panel(screen, new Vector2(.02f, top - .21f), new Vector2(.98f, top), Color.white);
                ApplySprite(row, PixelSkin.Slot()); row.raycastTarget = false;
                var r = row.transform;
                Text(r, listing.Name, new Vector2(.03f, .68f), new Vector2(.74f, .98f), 16, PixelSkin.TextDark);
                Text(r, $"{listing.Place}  •  {listing.Akad}", new Vector2(.03f, .46f), new Vector2(.74f, .68f), 13, PixelSkin.Accent);
                Text(r, $"Ujrah {listing.RateLabel}  •  tenor {listing.TenorDays} hari  •  Rp{listing.UnitPrice:N0}/unit", new Vector2(.03f, .24f), new Vector2(.74f, .46f), 13, PixelSkin.TextDark);
                // Batang dana terkumpul.
                var track = Panel(r, new Vector2(.03f, .08f), new Vector2(.5f, .19f), new Color(.78f, .72f, .64f)); track.raycastTarget = false;
                var fill = Panel(track.transform, Vector2.zero, new Vector2(Mathf.Clamp01(listing.FundedPercent / 100f), 1f), PixelSkin.Orange); fill.raycastTarget = false;
                Text(r, $"Terkumpul {listing.FundedPercent}%", new Vector2(.52f, .04f), new Vector2(.74f, .23f), 12, PixelSkin.TextDark);
                Button(r, "Detail", new Vector2(.76f, .2f), new Vector2(.98f, .8f), () => ShowListing(listing, 1, page));
            }
            PhoneNav(screen, () => ShowInvestmentCard(StoryContent.Card(card.Id), page));
            FocusFirst();
        }

        /// <summary>Detail satu penawaran + pemilih jumlah unit. Pembelian selalu lewat layar konfirmasi.</summary>
        void ShowListing(InvestmentListing listing, int units, int page, string feedback = null)
        {
            PullBalances();
            units = Mathf.Clamp(units, 1, InvestmentRules.MaxUnits);
            var screen = PhoneScreen($"Dompet Rp{State.Money:N0} • Rek Rp{State.Bank:N0}");
            var panel = Panel(screen, new Vector2(.02f, .1f), new Vector2(.98f, .88f), Color.white);
            ApplySprite(panel, PixelSkin.Panel()); panel.raycastTarget = false;
            var p = panel.transform;
            Text(p, listing.Name, new Vector2(.03f, .87f), new Vector2(.97f, .98f), 19, PixelSkin.TextDark);
            Text(p, $"{listing.Issuer}  •  {listing.Place}  •  Terkumpul {listing.FundedPercent}%", new Vector2(.03f, .79f), new Vector2(.97f, .87f), 13, PixelSkin.Accent);
            Text(p, listing.Description, new Vector2(.03f, .61f), new Vector2(.97f, .79f), 13, PixelSkin.TextDark).alignment = TextAlignmentOptions.TopLeft;
            Text(p, "Dampak: " + listing.Impact, new Vector2(.03f, .53f), new Vector2(.97f, .61f), 13, PixelSkin.Accent).alignment = TextAlignmentOptions.TopLeft;
            Text(p, $"Akad {listing.Akad}: hasilmu adalah sewa (ujrah) atas aset nyata — bukan bunga, dan bukan jaminan untung.", new Vector2(.03f, .43f), new Vector2(.97f, .53f), 13, PixelSkin.TextDark).alignment = TextAlignmentOptions.TopLeft;
            Text(p, "Risiko: " + listing.Risk, new Vector2(.03f, .35f), new Vector2(.97f, .43f), 13, PixelSkin.Accent).alignment = TextAlignmentOptions.TopLeft;

            int cost = units * listing.UnitPrice;
            Button(p, "-", new Vector2(.03f, .19f), new Vector2(.13f, .33f), () => ShowListing(listing, units - 1, page)).interactable = units > 1;
            Text(p, $"{units} unit", new Vector2(.13f, .19f), new Vector2(.33f, .33f), 18, PixelSkin.TextDark).alignment = TextAlignmentOptions.Center;
            Button(p, "+", new Vector2(.33f, .19f), new Vector2(.43f, .33f), () => ShowListing(listing, units + 1, page)).interactable = units < InvestmentRules.MaxUnits && cost + listing.UnitPrice <= InvestmentRules.Funds(State);
            Text(p, $"Total Rp{cost:N0}\nUjrah Rp{InvestmentRules.Payout(listing, units):N0} tiap {(listing.PeriodDays == 1 ? "hari" : listing.PeriodDays + " hari")}  •  pokok kembali Hari ke-{State.Day + listing.TenorDays}",
                new Vector2(.46f, .17f), new Vector2(.97f, .35f), 13, PixelSkin.TextDark).alignment = TextAlignmentOptions.MidlineLeft;
            Text(p, feedback ?? (TutorialActive ? "Selesaikan tutorial dulu untuk berinvestasi." : cost > InvestmentRules.Funds(State) ? "Uangmu belum cukup." : "Dibayar dari dompet dulu, lalu rekening. Dana terkunci sampai jatuh tempo."),
                new Vector2(.03f, .03f), new Vector2(.55f, .16f), 13, PixelSkin.Accent).alignment = TextAlignmentOptions.MidlineLeft;
            Button(p, "Investasikan", new Vector2(.57f, .03f), new Vector2(.97f, .16f), () => ConfirmInvestment(listing, units, page)).interactable = !TutorialActive && cost <= InvestmentRules.Funds(State);
            PhoneNav(screen, () => ShowListings(StoryContent.Card(listing.Card), page));
            FocusFirst();
        }

        void ConfirmInvestment(InvestmentListing listing, int units, int page)
        {
            var screen = PhoneScreen("KONFIRMASI");
            var panel = Panel(screen, new Vector2(.08f, .24f), new Vector2(.92f, .8f), Color.white);
            ApplySprite(panel, PixelSkin.Panel()); panel.raycastTarget = false;
            var p = panel.transform;
            Text(p, $"Investasikan Rp{units * listing.UnitPrice:N0}?", new Vector2(.05f, .72f), new Vector2(.95f, .94f), 21, PixelSkin.TextDark).alignment = TextAlignmentOptions.Center;
            int cost = units * listing.UnitPrice, fromWallet = Mathf.Min(cost, State.Money);
            int left = InvestmentRules.Funds(State) - cost, nights = State.RentPerDay > 0 ? left / State.RentPerDay : 99;
            Text(p, $"{units} unit {listing.Name} — Rp{fromWallet:N0} dari dompet{(cost > fromWallet ? $", Rp{cost - fromWallet:N0} dari rekening" : "")}.\nDana baru kembali pada Hari ke-{State.Day + listing.TenorDays}. Setelah ini dompetmu Rp{State.Money - fromWallet:N0}, total uangmu Rp{left:N0}" +
                    (nights < 5 ? $" — uangmu hanya cukup untuk sewa {nights} malam. Jangan sampai terpaksa berutang." : ". Sisakan dana darurat untuk makan dan sewa kamar."),
                new Vector2(.06f, .3f), new Vector2(.94f, .72f), 15, PixelSkin.TextDark).alignment = TextAlignmentOptions.Center;
            Button(p, "Ya, investasikan", new Vector2(.06f, .06f), new Vector2(.5f, .26f), () =>
            {
                PullBalances();
                bool ok = InvestmentRules.Buy(State, listing, units, out string feedback);
                if (!ok) { ShowListing(listing, units, page, feedback); return; }
                CurrencySystem.Instance?.RestoreBalances(State.Money, State.Bank);
                ScoreSystem.Instance?.MoveTowardsBalance(5f);   // untung yang halal: logika finansial & kepatuhan syariah seimbang
                Save(false);
                ShowPortfolio(0);
                Toast(feedback, 7);
            });
            Button(p, "Batal", new Vector2(.54f, .06f), new Vector2(.94f, .26f), () => ShowListing(listing, units, page));
            FocusFirst();
        }

        void ShowPortfolio(int page)
        {
            PullBalances();
            var screen = PhoneScreen($"PORTOFOLIO • Dompet Rp{State.Money:N0}");
            Text(screen, $"Total diinvestasikan Rp{InvestmentRules.Invested(State):N0}", new Vector2(.02f, .8f), new Vector2(.98f, .88f), 16, PixelSkin.Cream);
            const int perPage = 4;
            int pages = Mathf.Max(1, (State.Holdings.Count + perPage - 1) / perPage);
            page = Mathf.Clamp(page, 0, pages - 1);
            if (State.Holdings.Count == 0)
                Text(screen, "Belum ada investasi. Buka satu instrumen, baca penjelasannya, lalu pilih penawaran yang kamu pahami.",
                    new Vector2(.08f, .4f), new Vector2(.92f, .7f), 16, PixelSkin.Cream).alignment = TextAlignmentOptions.Center;
            for (int i = 0; i < perPage; i++)
            {
                int n = page * perPage + i;
                if (n >= State.Holdings.Count) break;
                var holding = State.Holdings[n];
                var listing = InvestmentContent.Find(holding.Listing);
                if (listing == null) continue;
                float top = .78f - i * .17f;
                var row = Panel(screen, new Vector2(.02f, top - .155f), new Vector2(.98f, top), Color.white);
                ApplySprite(row, PixelSkin.Slot()); row.raycastTarget = false;
                Text(row.transform, listing.Name, new Vector2(.03f, .6f), new Vector2(.97f, 1f), 15, PixelSkin.TextDark);
                Text(row.transform, $"{holding.Units} unit  •  Rp{holding.Units * listing.UnitPrice:N0}  •  ujrah Rp{InvestmentRules.Payout(listing, holding.Units):N0}/periode",
                    new Vector2(.03f, .3f), new Vector2(.97f, .6f), 13, PixelSkin.TextDark);
                Text(row.transform, $"Periode {holding.PaidPeriods}/{listing.Periods}  •  ujrah berikutnya Hari ke-{InvestmentRules.NextPayoutDay(holding)}  •  jatuh tempo Hari ke-{holding.StartDay + listing.TenorDays}",
                    new Vector2(.03f, 0f), new Vector2(.97f, .3f), 12, PixelSkin.Accent);
            }
            if (pages > 1)
            {
                Button(screen, "<", new Vector2(.34f, -.02f), new Vector2(.44f, .08f), () => ShowPortfolio(page - 1)).interactable = page > 0;
                Button(screen, ">", new Vector2(.56f, -.02f), new Vector2(.66f, .08f), () => ShowPortfolio(page + 1)).interactable = page < pages - 1;
            }
            PhoneNav(screen, () => ShowInvestmentApp(0));
            FocusFirst();
        }
    }
}
