using System.Linq;
using Alif.Campaign;
using Alif.Systems;
using Alif.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Alif.Adventure
{
    /// <summary>
    /// Hari cerita & Kasus Warga (konten di CaseContent): tidur di ranjang kamar kos memajukan
    /// <see cref="AdventureState.Day"/>, membuka kasus hari berikutnya, dan memunculkan tokohnya.
    /// Selama tugas penutup bab masih menunggu kasus (<see cref="AdventureTask.NeedsCases"/>),
    /// HUD dan panah penunjuk mengarah ke langkah kasus yang sedang terbuka — atau ke ranjang
    /// bila hari ini sudah tidak ada yang bisa dikerjakan.
    /// </summary>
    public sealed partial class AdventureGame
    {
        /// <summary>Tugas penutup bab yang masih tertahan karena ada Kasus Warga belum selesai.</summary>
        bool WaitingForCases(AdventureTask task) => task != null && task.NeedsCases && !SideQuestRules.CasesDone(State, Chapter);

        /// <summary>Langkah kasus wajib pertama yang sudah terbuka hari ini.</summary>
        (SideQuest quest, QuestStep step) OpenCaseStep()
        {
            foreach (var quest in SideQuestRules.PendingCases(State, Chapter))
            {
                var step = SideQuestRules.CurrentStep(State, quest);
                if (step != null) return (quest, step);
            }
            return (null, null);
        }

        /// <summary>Nama tampilan titik interaksi: tokoh/benda kota memakai nama dari konten, bukan id "npc:…".</summary>
        string PointName(string target) =>
            target != null && target.StartsWith("npc:") ? SideQuestContent.Npc(target)?.Name ?? target : target;

        void SyncStoryDay(bool morning) => TimeSystem.Instance?.SetStoryDay(State.Day, morning);

        /// <summary>Teks tujuan HUD saat bab menunggu kasus.</summary>
        string CaseObjective()
        {
            var (quest, step) = OpenCaseStep();
            if (step != null) return Objective($"HARI {State.Day}  •  KASUS WARGA", $"{quest.Title} — {step.Objective}");
            return Objective($"HARI {State.Day}", "Belum ada kabar baru • pulang ke kamar kos dan tidur");
        }

        /// <summary>Sasaran panah penunjuk saat bab menunggu kasus: tokoh langkah kasus, atau ranjang.</summary>
        (string target, int area) CaseGuideTarget()
        {
            var (_, step) = OpenCaseStep();
            string target = step?.Target ?? CaseContent.Bed;
            return (target, _questNpcAreas.TryGetValue(target, out int area) ? area : -1);
        }

        // ───────────────────────── Jadwal harian warga ─────────────────────────

        DayPart _shownPart;
        bool _partShown;

        static DayPart CurrentPart => NpcSchedule.PartOf(TimeSystem.Instance ? TimeSystem.Instance.CurrentHour : 12);

        /// <summary>Tokoh & benda kota yang tampil saat ini. Benda selalu ada; tokoh harus lolos gerbang
        /// cerita (SideQuestRules.NpcVisible) lalu jadwal acak hari ini. Tokoh yang sedang punya
        /// langkah quest untuk pemain tidak pernah dihilangkan — pemain tidak boleh dibuat menunggu.</summary>
        System.Collections.Generic.HashSet<string> ScheduledNpcs()
        {
            var part = CurrentPart;
            var shown = new System.Collections.Generic.HashSet<string>();
            foreach (var map in _questNpcAreas.GroupBy(pair => pair.Value, pair => pair.Key))
            {
                var residents = new System.Collections.Generic.List<string>();
                foreach (string id in map)
                {
                    var info = SideQuestContent.Npc(id);
                    if (info == null || !SideQuestRules.NpcVisible(State, info, SideQuestContent.All)) continue;
                    if (info.IsObject || SideQuestRules.StepAt(State, SideQuestContent.All, id).step != null) shown.Add(id);
                    else residents.Add(id);
                }
                // Map yang sudah berisi tokoh quest tidak perlu penjaga paksa; selain itu minimal satu.
                bool occupied = map.Any(id => shown.Contains(id) && SideQuestContent.Npc(id)?.IsObject == false);
                foreach (string id in occupied ? residents.Where(r => NpcSchedule.Present(r, State.Day, part)) : NpcSchedule.PresentIn(residents, State.Day, part))
                    shown.Add(id);
            }
            return shown;
        }

        void WatchClock()
        {
            var time = TimeSystem.Instance;
            if (!time) return;
            UnwatchClock();
            time.OnMinuteChanged += ClockTicked;
            time.OnStoryDawn += DawnWithoutSleep;
        }

        void UnwatchClock()
        {
            var time = TimeSystem.Instance;
            if (!time) return;
            time.OnMinuteChanged -= ClockTicked;
            time.OnStoryDawn -= DawnWithoutSleep;
        }

        /// <summary>Pergantian waktu (pagi → siang → malam → larut malam) mengacak ulang siapa yang hadir.
        /// Ditunda selama pemain sedang berdialog/membuka papan supaya lawan bicaranya tidak lenyap.</summary>
        void ClockTicked()
        {
            if (State == null || !CanExplore) return;
            var part = CurrentPart;
            if (_partShown && part == _shownPart) return;
            bool announce = _partShown;
            _shownPart = part; _partShown = true;
            RefreshQuestMarkers();
            if (announce && !TutorialActive) Toast($"{NpcSchedule.Label(part)} tiba • warga berganti kesibukan", 4);
        }

        /// <summary>Fajar tanpa tidur: hari cerita tetap maju (kasus & jadwal bergulir), energi tidak pulih.</summary>
        void DawnWithoutSleep()
        {
            if (State == null || !State.PassNight()) return;
            SyncStoryDay(false);
            _partShown = false;
            RefreshQuestMarkers();
            Save(false);
            RefreshHud();
            string news = CaseContent.MorningNews(State.Day);
            Toast(news != null ? $"Hari ke-{State.Day} • {news.Substring(news.IndexOf('|') + 1)}" : $"Hari ke-{State.Day} • Alif begadang semalaman, energi tidak pulih", 8);
        }

        // ───────────────────────── Siang & malam ─────────────────────────

        /// <summary>Area luar ruang ikut gelap-terang langit; sisanya (interior) diterangi lampu.</summary>
        static readonly System.Collections.Generic.HashSet<string> OutdoorAreas = new System.Collections.Generic.HashSet<string>
        { "Depan stasiun", "Depan warung", "Halaman kos", "Jalan Pasar", "Jalan Kafe", "Pusat Kota", "Kampus Cempaka", "Taman Cempaka" };

        Light2D _sun;
        bool _sunSearched;

        /// <summary>Warnai Global Light 2D menurut jam & tempat. Bergeser halus tiap frame, jadi masuk
        /// gedung di malam hari atau bangun tidur tidak membuat layar berkedip.</summary>
        void LateUpdate()
        {
            if (State == null || Content == null) return;
            if (!_sunSearched)
            {
                _sunSearched = true;
                _sun = FindObjectsByType<Light2D>(FindObjectsSortMode.None).FirstOrDefault(l => l.lightType == Light2D.LightType.Global);
                if (!_sun)
                {
                    _sun = new GameObject("Global Light 2D (siang-malam)").AddComponent<Light2D>();
                    _sun.lightType = Light2D.LightType.Global;
                }
            }
            if (!_sun) return;
            var time = TimeSystem.Instance;
            float hour = time ? time.CurrentHour + time.CurrentMinute / 60f : 12f;
            // Map yang punya lukisan malam (Kafe Senja) menukar latarnya di jam yang sama dengan
            // langit berubah ungu, jadi lampunya menyala bersamaan dengan gelapnya dunia.
            _city?.SetNight(DayLight.IsNight(hour));
            bool outdoor = State.Area >= 0 && State.Area < Content.Areas.Length && OutdoorAreas.Contains(Content.Areas[State.Area]);
            Color target = outdoor ? DayLight.Outdoor(hour) : DayLight.Indoor(hour);
            var now = _sun.color;
            float step = Time.unscaledDeltaTime * 1.5f;
            _sun.color = new Color(Mathf.MoveTowards(now.r, target.r, step), Mathf.MoveTowards(now.g, target.g, step), Mathf.MoveTowards(now.b, target.b, step), 1f);
        }

        // ───────────────────────── Tidur ─────────────────────────

        void InteractBed()
        {
            if (!State.CanSleep(Content))
            {
                SayLines(new[] { State.Day >= AdventureState.MaxDay
                    ? "Alif (Batin)|Sudah terlalu banyak hari berlalu. Saatnya menuntaskan yang tertunda."
                    : "Alif (Batin)|Aku belum punya kamar di sini. Sebaiknya kuselesaikan dulu urusan di Warung Bu Siti." }, null);
                return;
            }
            int open = SideQuestRules.PendingCases(State, Chapter).Count(q => SideQuestRules.CurrentStep(State, q) != null);
            ClearModal(CampaignActivity.Item);
            var panel = Panel(_modal, new Vector2(.3f, .26f), new Vector2(.7f, .74f), Color.white); ApplySprite(panel, PixelSkin.Panel());
            var p = panel.rectTransform;
            Text(p, "Tidur sampai besok pagi?", new Vector2(.06f, .62f), new Vector2(.94f, .9f), 24).alignment = TextAlignmentOptions.Center;
            Text(p, open > 0 ? $"{open} kasus warga masih terbuka — tetap menunggu besok." : "Hari berganti dan kabar warga yang baru datang tiap pagi.",
                new Vector2(.08f, .34f), new Vector2(.92f, .6f), 17).alignment = TextAlignmentOptions.Center;
            Button(p, "Tidur", new Vector2(.08f, .08f), new Vector2(.48f, .26f), () => { CloseModal(); Sleep(); });
            Button(p, "Nanti dulu", new Vector2(.52f, .08f), new Vector2(.92f, .26f), CloseModal);
            FocusFirst();
        }

        void Sleep()
        {
            _transition = true;
            SceneFadeController.Instance.PlayTimeSkip($"Hari ke-{State.Day + 1}", _player, () =>
            {
                State.Sleep(Content);
                string rent = State.PayRent();                 // sewa kamar ditagih tiap malam
                EnergySystem.Instance?.RestoreFull();
                SyncStoryDay(true);
                _partShown = false;          // pagi baru: jadwal warga diacak ulang tanpa pengumuman
                RefreshQuestMarkers();
                CurrencySystem.Instance?.RestoreBalances(State.Money, State.Bank);
                Save(false);
                RefreshHud();
                if (!string.IsNullOrEmpty(rent)) Toast(rent, 6);
            }, () =>
            {
                _transition = false;
                string news = CaseContent.MorningNews(State.Day)
                    ?? (SideQuestRules.CasesDone(State, Chapter)
                        ? "Alif (Batin)|Pagi yang tenang. Semua urusan warga sudah beres—saatnya mengabari Bu Siti."
                        : "Alif (Batin)|Pagi yang tenang. Masih ada urusan warga yang belum kutuntaskan.");
                SayLines(new[] { news }, SetPlaying);
            });
        }
    }
}
