using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Alif.Adventure.Tests
{
    /// <summary>Hari cerita, tidur, dan Kasus Warga Bab 1 (CaseContent).</summary>
    public class CaseTests
    {
        static AdventureState WithRoom()
        {
            var state = new AdventureState();
            foreach (var id in new[] { "c1.arrival", "c1.menu", "c1.receipt", "c1.resolve", "c1.room" })
            {
                var task = AdventureContent.Get(1).Tasks.First(t => t.Id == id);
                var progress = new TaskProgress { Id = id, Step = task.Steps.Length, Complete = true, Choices = task.Steps.Select(s => s.Answer).ToList() };
                if (task.Board != null)
                {
                    progress.Placements = task.Board.Kind == "budget"
                        ? Enumerable.Range(0, task.Board.Cards.Length).Select(i => i == 0 || i == 2 || i == 4 ? 1 : -1).ToList()
                        : task.Board.Answers.ToList();
                }
                state.Tasks.Add(progress);
            }
            return SideQuestTests.Reload(state);
        }

        /// <summary>Jalani semua kasus wajib bab ini seperti pemain: tidur sampai harinya tiba, lalu
        /// selesaikan tiap langkah dengan reload di antaranya.</summary>
        internal static AdventureState SolveAllCases(AdventureState state, AdventureChapter chapter)
        {
            foreach (var quest in SideQuestRules.PendingCases(state, chapter.Number).ToList())
            {
                while (state.Day < quest.Day) Assert.That(state.Sleep(chapter), Is.True, "Tidur harus bisa setelah punya kamar.");
                foreach (var step in quest.Steps)
                {
                    Assert.That(SideQuestRules.CurrentStep(state, quest), Is.SameAs(step), quest.Id);
                    SideQuestTests.Solve(state, quest, step);
                    state = SideQuestTests.Reload(state);
                }
            }
            Assert.That(SideQuestRules.CasesDone(state, chapter.Number), Is.True);
            return state;
        }

        [Test]
        public void ChapterOneHasOneMandatoryCasePerDayFromDayTwo()
        {
            var cases = CaseContent.All;
            Assert.That(cases.All(q => q.Mandatory && q.Chapter == 1), Is.True);
            CollectionAssert.AreEqual(new[] { 2, 3, 4, 5, 6, 7 }, cases.Select(q => q.Day).ToArray(), "Hari 1 milik tutorial; tiap hari berikutnya satu kasus.");
            Assert.That(cases.All(q => !string.IsNullOrEmpty(q.News) && q.News.Contains("|")), Is.True, "Tiap kasus butuh kabar pagi.");
            Assert.That(AdventureContent.Get(1).Tasks.Last().NeedsCases, Is.True);
            for (int number = 2; number <= 5; number++)
                Assert.That(AdventureContent.Get(number).Tasks.Any(t => t.NeedsCases), Is.False, "Bab lain belum punya Kasus Warga.");
        }

        [Test]
        public void CasesOpenOnTheirDayAndStayOpenAfterwards()
        {
            var state = WithRoom();
            var waqf = SideQuestContent.Find("case.wakaf");
            Assert.That(SideQuestRules.Available(state, waqf), Is.False, "Hari 1: taman masih tenang.");
            state.Sleep(AdventureContent.Get(1));
            Assert.That(SideQuestRules.Available(state, waqf), Is.False, "Hari 2: belum.");
            Assert.That(SideQuestRules.Available(state, SideQuestContent.Find("case.harga")), Is.True);
            state.Sleep(AdventureContent.Get(1));
            Assert.That(SideQuestRules.Available(state, waqf), Is.True, "Hari 3: kasus wakaf terbuka.");
            Assert.That(SideQuestRules.Available(state, SideQuestContent.Find("case.harga")), Is.True, "Kasus kemarin tidak hangus.");
        }

        [Test]
        public void CaseCharactersArriveOnTheirDayWhileResidentsAreAlwaysThere()
        {
            var state = new AdventureState();
            var gunawan = SideQuestContent.Npc(CaseContent.Gunawan);
            var salamah = SideQuestContent.Npc(CaseContent.Salamah);
            Assert.That(SideQuestRules.NpcVisible(state, salamah, SideQuestContent.All), Is.True);
            Assert.That(SideQuestRules.NpcVisible(state, gunawan, SideQuestContent.All), Is.False);
            state.Day = 3;
            Assert.That(SideQuestRules.NpcVisible(state, gunawan, SideQuestContent.All), Is.True);
        }

        [Test]
        public void SleepNeedsARoomAdvancesTheDayAndSurvivesReload()
        {
            var chapter = AdventureContent.Get(1);
            var state = new AdventureState();
            Assert.That(state.Sleep(chapter), Is.False, "Belum punya kamar kos.");
            state = WithRoom();
            Assert.That(state.Sleep(chapter), Is.True);
            Assert.That(SideQuestTests.Reload(state).Day, Is.EqualTo(2));
            state.TutorialStep = 1;
            Assert.That(state.CanSleep(chapter), Is.False);
            state.TutorialStep = 0; state.Day = AdventureState.MaxDay;
            Assert.That(state.Sleep(chapter), Is.False, "Hari cerita dibatasi.");
            state.Day = AdventureState.MaxDay + 1;
            Assert.That(state.Valid(), Is.False);
        }

        [Test]
        public void FinaleStaysLockedUntilEveryCaseIsSolved()
        {
            var chapter = AdventureContent.Get(1);
            var finale = chapter.Tasks.Last();
            var state = WithRoom();
            Assert.That(state.CanStart(finale), Is.False);
            Assert.That(state.Accept(finale, 0, out _), Is.False);
            state.Tasks.Add(new TaskProgress { Id = finale.Id, Complete = true });
            Assert.That(state.Valid(), Is.False, "Save tidak boleh menutup bab selagi kasus belum selesai.");
            state.Tasks.RemoveAll(t => t.Id == finale.Id);
            state = SolveAllCases(state, chapter);
            Assert.That(state.Day, Is.EqualTo(7));
            Assert.That(state.Accept(finale, 0, out _), Is.True);
            state.Finish(chapter);
            Assert.That(state.Completed, Is.True);
            Assert.That(state.StartChapter(2).Day, Is.EqualTo(7), "Hari cerita terbawa ke bab berikutnya.");
        }

        [Test]
        public void VersionThreeSaveMigratesToDayOne()
        {
            var old = new AdventureState { Chapter = 3, HighestUnlocked = 3, Day = 9 };
            old.CompletedChapters.Add(1); old.CompletedChapters.Add(2);
            string json = JsonUtility.ToJson(old).Replace("\"Version\":4", "\"Version\":3");
            var migrated = AdventureSave.Decode(json);
            Assert.That(migrated, Is.Not.Null);
            Assert.That(migrated.Version, Is.EqualTo(4));
            Assert.That(migrated.Day, Is.EqualTo(1));
        }

        [Test]
        public void IdleLineChangesOnceTheCaseIsSolved()
        {
            var state = new AdventureState { Day = 2 };
            var yanto = SideQuestContent.Npc(CaseContent.Yanto);
            Assert.That(SideQuestRules.IdleLine(state, yanto), Is.EqualTo(yanto.Idle));
            var quest = SideQuestContent.Find("case.harga");
            foreach (var step in quest.Steps) SideQuestTests.Solve(state, quest, step);
            Assert.That(SideQuestRules.IdleLine(state, yanto), Is.EqualTo(yanto.IdleAfter));
        }

        [Test]
        public void EveryCaseCharacterItemAndPlacementExists()
        {
            var spec = JsonUtility.FromJson<CityWorld.Spec>(Resources.Load<TextAsset>("Kota/city").text);
            var placed = spec.maps.SelectMany(m => m.npcs.Select(n => (n.id, m.area))).ToDictionary(p => p.id, p => p.area);
            foreach (var npc in CaseContent.Npcs)
            {
                Assert.That(placed.ContainsKey(npc.Id), Is.True, npc.Id + " belum ditempatkan di city.json (Tools/city-art/generate_city.py).");
                Assert.That(AdventureContent.CityAreas, Does.Contain(placed[npc.Id]), npc.Id);
                Assert.That(npc.IsObject || !string.IsNullOrEmpty(npc.Placeholder), Is.True, npc.Id + " butuh sprite placeholder.");
            }
            foreach (var step in CaseContent.All.SelectMany(q => q.Steps))
            {
                Assert.That(placed.ContainsKey(step.Target), Is.True, step.Target + " (" + step.Objective + ")");
                foreach (var item in new[] { step.RewardItem, step.RequiredItem }.Where(i => !string.IsNullOrEmpty(i)))
                    Assert.That(Alif.Systems.ItemCatalog.Get(item).Usage, Does.Not.Contain("Barang bawaan Alif"), item + " belum ada di ItemCatalog.");
            }
            Assert.That(placed[CaseContent.Bed], Is.EqualTo("Kamar Alif"));
        }
    }
}
