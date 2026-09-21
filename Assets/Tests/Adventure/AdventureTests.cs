using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace Alif.Adventure.Tests
{
    public class AdventureTests
    {
        [TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)]
        public void EveryChapterCompletesWithReloadAfterEveryStep(int number)
        {
            var c=AdventureContent.Get(number);var s=new AdventureState {Chapter=number,HighestUnlocked=number};
            Assert.That(s.CanStart(c.Tasks[1]),Is.False);
            foreach(var task in c.Tasks)
            {
                if(task.NeedsCases)
                {
                    // Tugas penutup tertahan sampai semua Kasus Warga bab ini selesai.
                    Assert.That(s.CanStart(task),Is.False);
                    s=CaseTests.SolveAllCases(s,c);
                }
                Assert.That(s.CanStart(task));
                if(task.Kind=="encounter")
                {
                    Assert.That(s.CompleteEncounter(task.Id));
                    s=AdventureSave.Decode(JsonUtility.ToJson(s));Assert.That(s,Is.Not.Null);
                    continue;
                }
                if(task.Board!=null)
                {
                    s.PrepareBoard(task);
                    if(task.Board.Kind=="budget")
                    {
                        int[] pick = task.Id == "c1.menu" ? new[]{0,2,4} : task.Id == "c2.plan" ? new[]{0,1,2} : new[]{0,2,3};
                        foreach(int i in pick) s.Place(task, i, 1, out _);
                    }
                    else
                    {
                        for(int i=0;i<task.Board.Cards.Length;i++) s.Place(task, i, task.Board.Answers[i], out _);
                    }
                    Assert.That(s.CommitBoard(task, out _));
                    s=AdventureSave.Decode(JsonUtility.ToJson(s));Assert.That(s,Is.Not.Null);
                    continue;
                }
                if(task.Steps.Length==0)Assert.That(s.Accept(task,0,out _));
                foreach(var step in task.Steps)
                {
                    int wrong=(step.Answer+1)%step.Options.Length;
                    int before=s.Progress(task.Id)?.Step??0;
                    Assert.That(s.Accept(task,wrong,out _),Is.False);
                    Assert.That(s.Progress(task.Id)?.Step ?? 0,Is.EqualTo(before));
                    Assert.That(s.Accept(task,step.Answer,out _));
                    s=AdventureSave.Decode(JsonUtility.ToJson(s));Assert.That(s,Is.Not.Null);
                }
                int money=s.Money;Assert.That(s.Accept(task,0,out _),Is.False);Assert.That(s.Money,Is.EqualTo(money));
            }
            s.Finish(c);Assert.That(s.Completed);Assert.That(s.HighestUnlocked,Is.EqualTo(System.Math.Min(number+1,5)));
            Assert.That(s.Money,Is.EqualTo(number==1?82000:100000));
            Assert.That(AdventureSave.Decode(JsonUtility.ToJson(s)),Is.Not.Null);
        }
        [Test]
        public void RejectsCorruptOrImpossibleCheckpoint()
        {
            Assert.That(AdventureSave.Decode("not json"),Is.Null);
            var s=new AdventureState {Chapter=6};Assert.That(AdventureSave.Decode(JsonUtility.ToJson(s)),Is.Null);
            s=new AdventureState();s.Tasks.Add(new TaskProgress{Id="c1.resolve",Step=4,Complete=true});
            Assert.That(s.Valid(),Is.False);
            s=new AdventureState {Money=-1};Assert.That(s.Valid(),Is.False);
        }
        [Test]
        public void ReplayPreservesUnlocksAndResetsPurchases()
        {
            var s=new AdventureState {Chapter=4,HighestUnlocked=4,Money=82000};s.CompletedChapters.Add(1);
            var replay=s.StartChapter(1);Assert.That(replay.HighestUnlocked,Is.EqualTo(4));Assert.That(replay.CompletedChapters,Does.Contain(1));
            Assert.That(replay.Tasks,Is.Empty);Assert.That(replay.Money,Is.EqualTo(100000));
            Assert.Throws<System.ArgumentOutOfRangeException>(()=>s.StartChapter(5));
        }
        [Test]
        public void TutorialCheckpointReloadsWhileOldSavesAndReplaysSkipIt()
        {
            var tutorial=new AdventureState {TutorialStep=2};
            Assert.That(AdventureSave.Decode(JsonUtility.ToJson(tutorial)).TutorialStep,Is.EqualTo(2));
            Assert.That(tutorial.StartChapter(1).TutorialStep,Is.Zero);

            string oldJson=JsonUtility.ToJson(new AdventureState()).Replace("\"TutorialStep\":0,","");
            Assert.That(AdventureSave.Decode(oldJson).TutorialStep,Is.Zero);

            // Pembuka bab kini diputar sebelum tutorial: checkpoint tutorial tetap sah setelah pembuka dilihat.
            tutorial.IntroductionSeen=true;
            Assert.That(tutorial.Valid(),Is.True);
            Assert.That(AdventureSave.Decode(JsonUtility.ToJson(tutorial)).IntroductionSeen,Is.True);
            tutorial.Completed=true;
            Assert.That(tutorial.Valid(),Is.False);
        }
        /// <summary>Selesaikan tugas Bab 1 sampai tepat sebelum <paramref name="stopAt"/> (semua dengan jawaban benar).</summary>
        static AdventureState PlayChapterOneUntil(string stopAt)
        {
            var s=new AdventureState();
            foreach(var task in AdventureContent.Get(1).Tasks)
            {
                if(task.Id==stopAt)return s;
                if(task.Board!=null)
                {
                    s.PrepareBoard(task);
                    if(task.Board.Kind=="budget")foreach(int i in new[]{0,4})s.Place(task,i,1,out _);
                    else for(int i=0;i<task.Board.Cards.Length;i++)s.Place(task,i,task.Board.Answers[i],out _);
                    Assert.That(s.CommitBoard(task,out _),task.Id);
                }
                else Assert.That(s.Accept(task,0,out _),task.Id);
            }
            return s;
        }
        [Test]
        public void LoanOfferAcceptsAnyJudgementAndItsAccuracyPicksTheNextMorning()
        {
            var offer=AdventureContent.Get(1).Tasks.First(t=>t.Id=="c1.offer");
            Assert.That(offer.Board.Free,Is.True);
            Assert.That(offer.Outcomes.Distinct().Count(),Is.EqualTo(3));
            // wrongCards = berapa kartu yang sengaja dinilai keliru → tingkat hasil yang diharapkan.
            foreach(var (wrongCards,tier) in new[]{(0,2),(1,1),(2,1),(3,0),(4,0)})
            {
                var s=PlayChapterOneUntil("c1.offer");
                Assert.That(s.BoardTier("c1.offer"),Is.EqualTo(-1),"Belum dinilai: belum ada akibat.");
                Assert.That(s.CommitBoard(offer,out _),Is.False,"Papan kosong belum bisa disampaikan.");
                for(int i=0;i<offer.Board.Cards.Length;i++)
                {
                    int answer=offer.Board.Answers[i],slot=i<wrongCards?(answer+1)%offer.Board.Slots.Length:answer;
                    Assert.That(s.Place(offer,i,slot,out _),Is.True,"Penilaian keliru pun diterima.");
                }
                // Penilaian boleh diubah sebelum disampaikan.
                Assert.That(s.Place(offer,0,offer.Board.Slots.Length-1,out _),Is.True);
                Assert.That(s.Place(offer,0,wrongCards>0?(offer.Board.Answers[0]+1)%offer.Board.Slots.Length:offer.Board.Answers[0],out _),Is.True);

                Assert.That(s.CommitBoard(offer,out string feedback),Is.True);
                Assert.That(feedback,Is.EqualTo(offer.Outcomes[tier]));
                s=AdventureSave.Decode(JsonUtility.ToJson(s));
                Assert.That(s,Is.Not.Null,"Save dengan penilaian keliru harus tetap sah.");
                Assert.That(s.BoardTier("c1.offer"),Is.EqualTo(tier));
                Assert.That(s.Place(offer,0,0,out _),Is.False,"Setelah disampaikan, penilaian terkunci.");

                var morning=StoryContent.OfferAftermath(tier);
                Assert.That(morning,Is.Not.Null);Assert.That(StoryContent.AllScenes,Does.Contain(morning));
                var outro=StoryContent.DemoOutroFor(tier);
                Assert.That(outro.Id,Is.EqualTo(StoryContent.DemoOutro.Id));
                Assert.That(outro.Lines.Length,Is.EqualTo(StoryContent.DemoOutro.Lines.Length+1));
                Assert.That(outro.Lines.Last(),Is.EqualTo(StoryContent.DemoOutro.Lines.Last()),"Baris penutup demo tetap paling akhir.");
            }
            Assert.That(StoryContent.OfferAftermath(-1),Is.Null);
            Assert.That(StoryContent.DemoOutroFor(-1),Is.SameAs(StoryContent.DemoOutro));
            // Papan lain tetap menolak penempatan yang keliru.
            var receipt=AdventureContent.Get(1).Tasks.First(t=>t.Id=="c1.receipt");
            var strict=PlayChapterOneUntil("c1.receipt");
            Assert.That(strict.Place(receipt,0,(receipt.Board.Answers[0]+1)%receipt.Board.Slots.Length,out _),Is.False);
        }
        [Test]
        public void ReceiptDocumentAddsUpAndShowsBothWrongRowsBesideTheOrder()
        {
            var doc=AdventureContent.Get(1).Tasks.First(t=>t.Id=="c1.receipt").Board.Document;
            Assert.That(doc,Is.Not.Null,"Papan struk dibuka lewat tampilan struknya dulu.");
            int Amount(string row)=>int.Parse(row.Split('|')[1].Replace(".",""));
            Assert.That(doc.Rows.Sum(Amount),Is.EqualTo(Amount(doc.Total)));Assert.That(Amount(doc.Total),Is.EqualTo(28000));
            Assert.That(doc.CompareRows.Sum(Amount),Is.EqualTo(Amount(doc.CompareTotal)));Assert.That(Amount(doc.CompareTotal),Is.EqualTo(18000));
            Assert.That(doc.Rows.Any(r=>r.Contains("Es teh")&&r.Contains("2 x")),Is.True);
            Assert.That(doc.Rows.Any(r=>r.Contains("Kerupuk")),Is.True);
            Assert.That(doc.CompareRows.Any(r=>r.Contains("Kerupuk")),Is.False);
            // Papan lain tidak berubah: tanpa dokumen, langsung ke kartunya.
            Assert.That(AdventureContent.Get(1).Tasks.First(t=>t.Id=="c1.menu").Board.Document,Is.Null);
        }
        [Test]
        public void EveryRequiredTargetHasAreaAndEveryPuzzleHasValidOptions()
        {
            for(int c=1;c<=5;c++)
            {
                var chapter=AdventureContent.Get(c);Assert.That(AdventureContent.SceneAreaCount(chapter),Is.InRange(3,4));Assert.That(chapter.Optional.Length,Is.EqualTo(3));
                Assert.That(chapter.Tasks.Count(t=>t.Steps.Length>0),Is.EqualTo(3));
                Assert.That(chapter.Tasks.Select(t=>t.Id).Distinct().Count(),Is.EqualTo(chapter.Tasks.Length));
                foreach(var t in chapter.Tasks){Assert.That(t.Area,Is.InRange(0,chapter.Areas.Length-1));Assert.That(t.Target,Is.Not.Empty);
                    foreach(var step in t.Steps){Assert.That(step.Answer,Is.InRange(0,step.Options.Length-1));Assert.That(step.Evidence,Is.Not.Empty);Assert.That(step.Explanation,Is.Not.Empty);}}
            }
        }
    }
}
