using System;
using System.Linq;
using Alif.Campaign;
using Alif.Core;
using Alif.Systems;
using Alif.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Alif.Adventure
{
    /// <summary>
    /// HP Alif (gaya game simulasi kehidupan): layar utama berisi aplikasi Peta (teleport ke
    /// area mana pun), Tas, Jurnal, Kontak (hubungan NPC quest), Kalender, dan Pengaturan.
    /// Dibuka lewat ikon HP di HUD atau tombol P.
    /// </summary>
    public sealed partial class AdventureGame
    {
        static readonly Color MapsColor = new Color(.36f, .62f, .38f), BagColor = new Color(.89f, .44f, .17f),
            JournalColor = new Color(.62f, .38f, .22f), ContactsColor = new Color(.93f, .47f, .6f),
            CalendarColor = new Color(.78f, .29f, .25f), SettingsColor = new Color(.45f, .47f, .53f);

        Transform _teleportTarget;

        void OpenPhone()
        {
            var screen = PhoneScreen("HP");
            var apps = new (string name, Sprite icon, Color color, Action open)[]
            {
                ("Peta", PixelSkin.PinIcon(), MapsColor, ShowMapsApp),
                ("Tas", PixelSkin.BagIcon(), BagColor, Bag),
                ("Jurnal", PixelSkin.NotebookIcon(), JournalColor, () => Journal(0)),
                ("Kontak", PixelSkin.HeartIcon(), ContactsColor, ShowContactsApp),
                ("Kalender", PixelSkin.CalendarIcon(), CalendarColor, ShowCalendarApp),
                ("Pengaturan", PixelSkin.GearIcon(), SettingsColor, Pause),
            };
            for (int i = 0; i < apps.Length; i++)
            {
                var (name, icon, color, open) = apps[i];
                float x = .2f + (i % 3) * .3f, y = i < 3 ? .66f : .26f;
                AppTile(screen, name, icon, color, new Vector2(x, y), open);
            }
            PhoneNav(screen, null);
            FocusFirst();
        }

        /// <summary>Bingkai HP + layar; mengembalikan RectTransform area layar.</summary>
        RectTransform PhoneScreen(string title)
        {
            ClearModal(CampaignActivity.Phone);
            var body = Panel(_modal, new Vector2(.17f, .07f), new Vector2(.83f, .93f), Color.white);
            ApplySprite(body, PixelSkin.PhoneFrame());
            var screen = CampaignUI.Rect(body.transform, "Screen", Vector2.zero, Vector2.one);
            screen.offsetMin = new Vector2(34f, 30f);
            screen.offsetMax = new Vector2(-34f, -30f);

            var time = TimeSystem.Instance;
            string day = time ? (DayNamesId.TryGetValue(time.CurrentDayName, out var id) ? id : time.CurrentDayName) : "";
            var status = Text(screen, $"{day}  {(time ? time.GetFormattedTime() : "")}", new Vector2(0f, .9f), new Vector2(.5f, 1f), 15, PixelSkin.Cream);
            status.alignment = TextAlignmentOptions.TopLeft;
            var header = Text(screen, title, new Vector2(.5f, .9f), new Vector2(1f, 1f), 15, PixelSkin.Cream);
            header.alignment = TextAlignmentOptions.TopRight;
            return screen;
        }

        void AppTile(RectTransform screen, string name, Sprite icon, Color color, Vector2 center, Action open)
        {
            var tile = CampaignUI.Rect(screen, name, center, center);
            tile.sizeDelta = new Vector2(96f, 96f);
            var image = tile.gameObject.AddComponent<Image>();
            ApplySprite(image, PixelSkin.AppTile());
            image.color = color;
            var button = tile.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors; colors.highlightedColor = colors.selectedColor = new Color(1f, .93f, .8f); colors.pressedColor = new Color(.75f, .7f, .65f); button.colors = colors;
            button.onClick.AddListener(() => { if (Time.unscaledTime - _openedAt >= .13f) open(); });
            _buttons.Add(button);
            var glyph = CampaignUI.Rect(tile, "Icon", Vector2.one * .5f, Vector2.one * .5f).gameObject.AddComponent<Image>();
            glyph.sprite = icon; glyph.raycastTarget = false;
            glyph.rectTransform.sizeDelta = icon.rect.size * (icon.rect.height > 11 ? 4f : 5f);
            var label = Text(screen, name, center + new Vector2(-.14f, -.2f), center + new Vector2(.14f, -.1f), 16, PixelSkin.Cream);
            label.alignment = TextAlignmentOptions.Center;
        }

        /// <summary>Baris bawah HP: Kembali (ke layar utama) bila ada, dan Tutup.</summary>
        void PhoneNav(RectTransform screen, Action back)
        {
            if (back != null) Button(screen, "Kembali", new Vector2(.02f, -.02f), new Vector2(.3f, .08f), back);
            var close = Button(screen, "Tutup", new Vector2(.7f, -.02f), new Vector2(.98f, .08f), CloseModal);
            PixelSkin.StyleButton(close, secondary: true);
        }

        // ───────────────────────── Peta ─────────────────────────

        void ShowMapsApp()
        {
            var screen = PhoneScreen("PETA • Kota Cempaka");
            var spec = CityWorld.Load();
            var mapSprite = Resources.Load<Sprite>("Kota/PhoneMap");
            var holder = CampaignUI.Rect(screen, "Map", new Vector2(.02f, .12f), new Vector2(.98f, .88f));
            var map = CampaignUI.Rect(holder, "Map image", Vector2.one * .5f, Vector2.one * .5f);
            var fitter = map.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = spec != null ? spec.mapWidth / (float)spec.mapHeight : 5f / 3f;
            var mapImage = map.gameObject.AddComponent<Image>();
            mapImage.sprite = mapSprite; mapImage.raycastTarget = false;

            if (spec != null)
                foreach (var pin in spec.pins)
                {
                    int area = Array.IndexOf(Content.Areas, pin.area);
                    if (area < 0) continue;
                    MapPin(map, pin, area, spec, area == State.Area);
                }
            Text(screen, TutorialActive ? "Selesaikan tutorial untuk bepergian dengan Peta." : "Pilih lokasi untuk berangkat.", new Vector2(.32f, -.02f), new Vector2(.68f, .08f), 14, PixelSkin.Cream).alignment = TextAlignmentOptions.Center;
            PhoneNav(screen, OpenPhone);
            FocusFirst();
        }

        void MapPin(RectTransform map, CityWorld.Pin pin, int area, CityWorld.Spec spec, bool here)
        {
            var anchor = new Vector2(pin.x / (float)spec.mapWidth, 1f - pin.y / (float)spec.mapHeight);
            var marker = CampaignUI.Rect(map, "Pin " + pin.area, anchor, anchor);
            marker.sizeDelta = new Vector2(30f, 30f);
            var image = marker.gameObject.AddComponent<Image>();
            image.sprite = PixelSkin.Disc();
            image.color = here ? PixelSkin.OrangeLight : Color.white;
            var button = marker.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => TeleportTo(area));
            _buttons.Add(button);

            // Label di atas/bawah pin bergantian (city.json) agar pin yang berdekatan tidak bertumpuk.
            var tag = CampaignUI.Rect(marker, "Label", new Vector2(.5f, pin.up ? 1f : 0f), new Vector2(.5f, pin.up ? 1f : 0f));
            tag.pivot = new Vector2(.5f, pin.up ? 0f : 1f);
            var tagImage = tag.gameObject.AddComponent<Image>(); tagImage.raycastTarget = false;
            ApplySprite(tagImage, PixelSkin.Tab());
            string label = here ? pin.area + " • di sini" : pin.area;
            var text = Text(tag, label, Vector2.zero, Vector2.one, 12, PixelSkin.TextDark);
            text.alignment = TextAlignmentOptions.Center; text.margin = Vector4.zero; text.textWrappingMode = TextWrappingModes.NoWrap;
            tag.sizeDelta = new Vector2(text.GetPreferredValues(label).x + 22f, 26f);
            tag.anchoredPosition = new Vector2(0f, pin.up ? 2f : -2f);
        }

        /// <summary>Pindah ke titik spawn area lewat fade, seperti melewati pintu.</summary>
        void TeleportTo(int area)
        {
            if (TutorialActive) { Toast("Selesaikan atau lewati tutorial sebelum bepergian", 4); return; }
            if (area == State.Area) { CloseModal(); Toast("Kamu sudah berada di " + Content.Areas[area], 3); return; }
            if (_transition || area < 0 || area >= Spawns.Length) return;
            CloseModal();
            if (!_teleportTarget) _teleportTarget = new GameObject("Teleport target").transform;
            _teleportTarget.position = Spawns[area];
            _transition = true;
            SceneFadeController.Instance.TransitionTo(_player, _player.GetComponent<Rigidbody2D>(), _teleportTarget, Camera.main.GetComponent<CameraFollow>(), () =>
            {
                State.Area = area; _transition = false; Save(false); RefreshHud();
                Toast("Tiba di " + Content.Areas[area], 4);
            });
        }

        // ───────────────────────── Kontak & Kalender ─────────────────────────

        void ShowContactsApp()
        {
            var screen = PhoneScreen("KONTAK");
            var contacts = SideQuestContent.Npcs.Where(n => _city != null && _city.Npcs.ContainsKey(n.Id)).ToArray();
            for (int i = 0; i < contacts.Length; i++)
            {
                var npc = contacts[i];
                int hearts = SideQuestRules.Hearts(State, npc.Id);
                string where = _city.Npcs[npc.Id].area;
                int area = Array.IndexOf(Content.Areas, where);
                float top = .86f - i * .15f;
                var row = Panel(screen, new Vector2(.02f, top - .13f), new Vector2(.98f, top), Color.white);
                ApplySprite(row, PixelSkin.Slot()); row.raycastTarget = false;
                Text(row.transform, $"{npc.Name}   {new string('♥', hearts)}{new string('♡', SideQuestRules.MaxHearts - hearts)}", new Vector2(0f, .5f), new Vector2(.72f, 1f), 16, PixelSkin.TextDark);
                Text(row.transform, where, new Vector2(0f, 0f), new Vector2(.72f, .5f), 13, PixelSkin.Accent);
                var visit = Button(row.transform, "Temui", new Vector2(.74f, .15f), new Vector2(.98f, .85f), () => TeleportTo(area));
                visit.interactable = area >= 0;
            }
            PhoneNav(screen, OpenPhone);
            FocusFirst();
        }

        void ShowCalendarApp()
        {
            var screen = PhoneScreen("KALENDER");
            var time = TimeSystem.Instance;
            string day = time ? (DayNamesId.TryGetValue(time.CurrentDayName, out var id) ? id : time.CurrentDayName) : "—";
            var card = Panel(screen, new Vector2(.28f, .22f), new Vector2(.72f, .84f), Color.white);
            ApplySprite(card, PixelSkin.Panel()); card.raycastTarget = false;
            Text(card.transform, day.ToUpperInvariant(), new Vector2(0f, .62f), new Vector2(1f, .88f), 30, PixelSkin.Accent).alignment = TextAlignmentOptions.Center;
            Text(card.transform, time ? "Minggu ke-" + time.CurrentWeek : "", new Vector2(0f, .4f), new Vector2(1f, .6f), 20, PixelSkin.TextDark).alignment = TextAlignmentOptions.Center;
            Text(card.transform, time ? time.GetFormattedTime() : "--:--", new Vector2(0f, .14f), new Vector2(1f, .4f), 34, PixelSkin.TextDark).alignment = TextAlignmentOptions.Center;
            PhoneNav(screen, OpenPhone);
            FocusFirst();
        }
    }
}
