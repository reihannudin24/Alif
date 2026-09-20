using System.Linq;
using Alif.Campaign;
using Alif.Systems;
using Alif.UI;
using TMPro;
using UnityEngine;

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
                EnergySystem.Instance?.RestoreFull();
                SyncStoryDay(true);
                RefreshQuestMarkers();
                Save(false);
                RefreshHud();
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
