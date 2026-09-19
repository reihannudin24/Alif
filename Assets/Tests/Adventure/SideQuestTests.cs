using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Alif.Adventure.Tests
{
    public class SideQuestTests
    {
        static AdventureState Reload(AdventureState state)
        {
            var reloaded = AdventureSave.Decode(JsonUtility.ToJson(state));
            Assert.That(reloaded, Is.Not.Null, "Save dengan side quest harus tetap valid.");
            return reloaded;
        }

        /// <summary>Selesaikan satu langkah persis seperti runtime: talk/give lewat Advance,
        /// papan lewat Place + Commit dengan jawaban yang benar.</summary>
        static void Solve(AdventureState state, SideQuest quest, QuestStep step)
        {
            if (step.Board == null)
            {
                Assert.That(SideQuestRules.Advance(state, quest, step.Target), Is.True, quest.Id + ": " + step.Objective);
                return;
            }
            var board = step.Board;
            SideQuestRules.Prepare(state, quest);
            if (board.Kind == "budget")
            {
                for (int i = 0; i < board.Cards.Length; i++)
                    if (board.Groups[i] >= 0) Assert.That(SideQuestRules.Place(state, quest, i, 1, out _), Is.True);
            }
            else
            {
                for (int i = 0; i < board.Cards.Length; i++) Assert.That(SideQuestRules.Place(state, quest, i, board.Answers[i], out _), Is.True);
            }
            Assert.That(SideQuestRules.Commit(state, quest, out _), Is.True, quest.Id + ": " + step.Objective);
        }

        [Test]
        public void EverySideQuestCompletesInOrderWithReloadAfterEveryStep()
        {
            var state = new AdventureState();
            foreach (var quest in SideQuestContent.All)
            {
                Assert.That(SideQuestRules.Available(state, quest), Is.True, quest.Id + " harus terbuka setelah prasyaratnya selesai.");
                foreach (var step in quest.Steps)
                {
                    Assert.That(SideQuestRules.CurrentStep(state, quest), Is.SameAs(step));
                    Solve(state, quest, step);
                    state = Reload(state);
                }
                Assert.That(SideQuestRules.Progress(state, quest.Id).Complete, Is.True);
                Assert.That(SideQuestRules.CurrentStep(state, quest), Is.Null);
            }
        }

        [Test]
        public void LaterQuestsStayLockedUntilPrerequisitesAndTutorialAreDone()
        {
            var state = new AdventureState { TutorialStep = 1 };
            var first = SideQuestContent.Find("kpop.skincare");
            var second = SideQuestContent.Find("kpop.pockets");
            Assert.That(SideQuestRules.Available(state, first), Is.False, "Tutorial masih aktif.");
            state.TutorialStep = 0;
            Assert.That(SideQuestRules.Available(state, first), Is.True);
            Assert.That(SideQuestRules.Available(state, second), Is.False);
            Assert.That(SideQuestRules.CurrentStep(state, SideQuestContent.Find("dosen.phishing")), Is.Null);
        }

        [Test]
        public void StepOnlyAdvancesAtItsOwnTarget()
        {
            var state = new AdventureState();
            var quest = SideQuestContent.Find("kpop.skincare");
            Assert.That(SideQuestRules.Advance(state, quest, SideQuestContent.Tara), Is.False);
            Assert.That(SideQuestRules.Advance(state, quest, SideQuestContent.Kirana), Is.True);
            // Langkah kedua adalah papan: tidak bisa dilewati dengan Advance biasa.
            Assert.That(SideQuestRules.Advance(state, quest, SideQuestContent.Kirana), Is.False);
            Assert.That(SideQuestRules.Commit(state, quest, out _), Is.False, "Papan kosong tidak boleh selesai.");
        }

        [Test]
        public void WrongPlacementIsRejectedWithExplanation()
        {
            var state = new AdventureState();
            var quest = SideQuestContent.Find("kpop.checkout");
            state.SideQuests.Add(new SideQuestProgress { Id = "kpop.skincare", Step = 4, Complete = true });
            state.SideQuests.Add(new SideQuestProgress { Id = "kpop.pockets", Step = 4, Complete = true });
            SideQuestRules.Advance(state, quest, SideQuestContent.Tara);
            var board = SideQuestRules.CurrentStep(state, quest).Board;
            int wrong = (board.Answers[2] + 1) % board.Slots.Length;
            Assert.That(SideQuestRules.Place(state, quest, 2, wrong, out string feedback), Is.False);
            Assert.That(feedback, Is.EqualTo(board.Notes[2]));
        }

        [Test]
        public void BudgetBoardRejectsTakingSchoolMoney()
        {
            var state = new AdventureState();
            var quest = SideQuestContent.Find("kpop.pockets");
            state.SideQuests.Add(new SideQuestProgress { Id = "kpop.skincare", Step = 4, Complete = true });
            for (int i = 0; i < 3; i++) SideQuestRules.Advance(state, quest, quest.Steps[i].Target);
            var board = SideQuestRules.CurrentStep(state, quest).Board;
            SideQuestRules.Prepare(state, quest);
            for (int i = 0; i < board.Cards.Length; i++) SideQuestRules.Place(state, quest, i, 1, out _);
            Assert.That(SideQuestRules.Commit(state, quest, out _), Is.False, "Memakai uang SPP melewati batas tabungan.");
        }

        [Test]
        public void BondsAreClampedAndCarriedIntoTheNextChapter()
        {
            var state = new AdventureState { HighestUnlocked = 2 };
            SideQuestRules.AddBond(state, SideQuestContent.Kirana, 9);
            state.Items.Add(new ItemStack { Name = SideQuestContent.SparePhotocard, Quantity = 1 });
            state.PickedUp.Add("Voucher Promo Kereta");
            state.SideQuests.Add(new SideQuestProgress { Id = "kpop.skincare", Step = 1 });
            Assert.That(SideQuestRules.Hearts(state, SideQuestContent.Kirana), Is.EqualTo(SideQuestRules.MaxHearts));

            var next = Reload(state.StartChapter(2));
            Assert.That(SideQuestRules.Hearts(next, SideQuestContent.Kirana), Is.EqualTo(SideQuestRules.MaxHearts));
            Assert.That(next.Items.Single().Name, Is.EqualTo(SideQuestContent.SparePhotocard));
            Assert.That(next.PickedUp, Does.Contain("Voucher Promo Kereta"));
            Assert.That(SideQuestRules.Progress(next, "kpop.skincare").Step, Is.EqualTo(1));
        }

        [Test]
        public void SaveWithUnknownSideQuestIsRejected()
        {
            var state = new AdventureState();
            state.SideQuests.Add(new SideQuestProgress { Id = "tidak.ada" });
            Assert.That(AdventureSave.Decode(JsonUtility.ToJson(state)), Is.Null);
        }

        [Test]
        public void EveryGiveStepItemIsRewardedEarlierInTheSameQuest()
        {
            foreach (var quest in SideQuestContent.All)
                for (int i = 0; i < quest.Steps.Length; i++)
                {
                    var step = quest.Steps[i];
                    if (step.Kind != "give") continue;
                    Assert.That(quest.Steps.Take(i).Any(s => s.RewardItem == step.RequiredItem), Is.True,
                        $"{quest.Id}: '{step.RequiredItem}' harus bisa didapat sebelum diberikan.");
                }
        }

        [Test]
        public void EveryStepTargetsAKnownNpcAndEveryBoardIsConsistent()
        {
            foreach (var quest in SideQuestContent.All)
                foreach (var step in quest.Steps)
                {
                    Assert.That(SideQuestContent.Npc(step.Target), Is.Not.Null, $"{quest.Id}: target {step.Target}");
                    var board = step.Board;
                    if (board == null) continue;
                    Assert.That(board.Notes.Length, Is.GreaterThan(0));
                    if (board.Kind == "budget")
                    {
                        Assert.That(board.Costs.Length, Is.EqualTo(board.Cards.Length));
                        Assert.That(board.Groups.Length, Is.EqualTo(board.Cards.Length));
                    }
                    else
                    {
                        Assert.That(board.Answers.Length, Is.EqualTo(board.Cards.Length));
                        Assert.That(board.Notes.Length, Is.EqualTo(board.Cards.Length));
                        Assert.That(board.Answers.All(a => a >= 0 && a < board.Slots.Length), Is.True, quest.Id);
                    }
                }
        }
    }
}
