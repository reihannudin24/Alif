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

            tutorial.IntroductionSeen=true;
            Assert.That(tutorial.Valid(),Is.False);
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
