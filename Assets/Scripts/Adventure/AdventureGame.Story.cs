using System;
using System.Linq;
using Alif.Campaign;
using Alif.Core;
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

            var data = string.IsNullOrEmpty(scene.Character) ? null : Cast.FirstOrDefault(c => c && c.name.EndsWith(scene.Character, StringComparison.OrdinalIgnoreCase));
            var figure = data ? (data.Portrait ? data.Portrait : data.WorldSprite) : null;
            _story = StoryOverlay.Play(scene, StoryArtLibrary.Find(scene.Art), figure, () =>
            {
                _story = null;
                _hud.gameObject.SetActive(true);
                SetPlaying();
                then?.Invoke();
            });
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

        void ShowInvestmentApp(int page = 0)
        {
            var screen = PhoneScreen($"INVESTASI • {State.Cards.Count}/{StoryContent.Cards.Length} kartu");
            const int perPage = 5;
            int pages = (StoryContent.Cards.Length + perPage - 1) / perPage;
            page = Mathf.Clamp(page, 0, pages - 1);
            var next = SideQuestContent.All.FirstOrDefault(q => q.Giver != null && SideQuestRules.Available(State, q));
            for (int i = 0; i < perPage; i++)
            {
                int n = page * perPage + i;
                if (n >= StoryContent.Cards.Length) break;
                var card = StoryContent.Cards[n];
                bool open = State.Cards.Contains(card.Id);
                float top = .86f - i * .15f;
                var row = Panel(screen, new Vector2(.02f, top - .13f), new Vector2(.98f, top), Color.white);
                ApplySprite(row, PixelSkin.Slot()); row.raycastTarget = false;
                Text(row.transform, open ? card.Name : "???", new Vector2(0f, .5f), new Vector2(.7f, 1f), 16, PixelSkin.TextDark);
                string hint = open ? $"Risiko: {card.Risk}" : next != null && next.Steps.Any(s => s.Card == card.Id) ? $"Temui {SideQuestContent.Npc(next.Giver)?.Name}" : "Belum terbuka";
                Text(row.transform, hint, new Vector2(0f, 0f), new Vector2(.72f, .5f), 13, PixelSkin.Accent);
                if (open) Button(row.transform, "Detail", new Vector2(.74f, .15f), new Vector2(.98f, .85f), () => ShowInvestmentCard(card, page));
            }
            if (pages > 1)
            {
                Button(screen, "<", new Vector2(.34f, -.02f), new Vector2(.44f, .08f), () => ShowInvestmentApp(page - 1)).interactable = page > 0;
                Button(screen, ">", new Vector2(.56f, -.02f), new Vector2(.66f, .08f), () => ShowInvestmentApp(page + 1)).interactable = page < pages - 1;
            }
            PhoneNav(screen, OpenPhone);
            FocusFirst();
        }

        void ShowInvestmentCard(InvestmentCard card, int page)
        {
            var screen = PhoneScreen("KARTU INVESTASI");
            var panel = Panel(screen, new Vector2(.02f, .1f), new Vector2(.98f, .88f), Color.white);
            ApplySprite(panel, PixelSkin.Panel()); panel.raycastTarget = false;
            var p = panel.transform;
            Text(p, card.Name, new Vector2(.03f, .84f), new Vector2(.97f, .97f), 22, PixelSkin.TextDark);
            Text(p, "Contoh: " + card.Examples, new Vector2(.03f, .66f), new Vector2(.97f, .83f), 15, PixelSkin.TextDark);
            Text(p, "Cara hasil: " + card.Returns, new Vector2(.03f, .52f), new Vector2(.97f, .66f), 15, PixelSkin.TextDark);
            Text(p, "Risiko umum: " + card.Risk, new Vector2(.03f, .4f), new Vector2(.97f, .52f), 15, PixelSkin.Accent);
            Text(p, card.Lesson, new Vector2(.03f, .03f), new Vector2(.97f, .4f), 15, PixelSkin.TextDark).alignment = TMPro.TextAlignmentOptions.TopLeft;
            PhoneNav(screen, () => ShowInvestmentApp(page));
            FocusFirst();
        }
    }
}
