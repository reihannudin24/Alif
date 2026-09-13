using System.Collections;
using System.Linq;
using Alif.Adventure;
using Alif.Core;
using Alif.Dialogue;
using Alif.Player;
using Alif.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class AdventureChapterOneSmokeTests
{
    [UnityTest]
    public IEnumerator NewGameTutorialRequiresMovementSignDialogueAndJournalClose()
    {
        ClearSave();
        ChapterProgress.StartNewGame();
        SceneManager.LoadScene("AdventureChapter1");
        AdventureGame game=null;
        for(int i=0;i<120&&!game;i++){yield return null;game=Object.FindAnyObjectByType<AdventureGame>();}

        Assert.That(game,Is.Not.Null);
        Assert.That(game.State.TutorialStep,Is.EqualTo(1));
        Assert.That(game.CurrentTaskId,Is.Empty);

        SceneManager.LoadScene("AdventureChapter1");yield return null;
        game=null;
        for(int i=0;i<120&&!game;i++){yield return null;game=Object.FindAnyObjectByType<AdventureGame>();}
        Assert.That(game.State.TutorialStep,Is.EqualTo(1));
        var player=Object.FindAnyObjectByType<PlayerController>();
        Assert.That(Keyboard.current,Is.Not.Null);
        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.D));InputSystem.Update();
        for(int i=0;i<120&&game.State.TutorialStep==1;i++)yield return new WaitForFixedUpdate();
        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());InputSystem.Update();yield return null;
        Assert.That(game.State.TutorialStep,Is.EqualTo(2));

        SceneManager.LoadScene("AdventureChapter1");yield return null;
        game=null;
        for(int i=0;i<120&&!game;i++){yield return null;game=Object.FindAnyObjectByType<AdventureGame>();}
        Assert.That(game.State.TutorialStep,Is.EqualTo(2));
        player=Object.FindAnyObjectByType<PlayerController>();
        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.D));InputSystem.Update();
        for(int i=0;i<120&&player.transform.position.x<2.1f;i++)yield return new WaitForFixedUpdate();
        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());InputSystem.Update();yield return null;
        Assert.That(Vector2.Distance(player.transform.position,new Vector2(2.6f,-2.25f)),Is.LessThan(1.35f));

        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.E));InputSystem.Update();yield return null;
        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());InputSystem.Update();
        Assert.That(DialogueManager.Instance.IsDialogueActive,Is.True);
        DialogueManager.Instance.AdvanceDialogue();yield return null;
        Assert.That(game.State.TutorialStep,Is.EqualTo(3));
        Assert.That(game.State.Tasks,Is.Empty);

        SceneManager.LoadScene("AdventureChapter1");yield return null;
        game=null;
        for(int i=0;i<120&&!game;i++){yield return null;game=Object.FindAnyObjectByType<AdventureGame>();}
        Assert.That(game.State.TutorialStep,Is.EqualTo(3));
        Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude).Single(b=>b.name=="Jurnal").onClick.Invoke();
        yield return new WaitForSecondsRealtime(.15f);
        Assert.That(game.Activity,Is.EqualTo(CampaignActivity.Journal));
        game.Pause();yield return null;Assert.That(game.Activity,Is.EqualTo(CampaignActivity.Pause));
        game.Resume();yield return null;Assert.That(game.Activity,Is.EqualTo(CampaignActivity.Journal));
        Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude).Single(b=>b.name=="Kembali").onClick.Invoke();
        yield return null;

        Assert.That(game.State.TutorialStep,Is.Zero);
        Assert.That(game.State.Tasks,Is.Empty);
        Assert.That(DialogueManager.Instance.IsDialogueActive,Is.True);
    }

    [UnityTest]
    public IEnumerator TutorialCanBeSkippedWithoutCompletingTheFirstObjective()
    {
        ClearSave();ChapterProgress.StartNewGame();SceneManager.LoadScene("AdventureChapter1");
        AdventureGame game=null;
        for(int i=0;i<120&&!game;i++){yield return null;game=Object.FindAnyObjectByType<AdventureGame>();}

        game.SkipTutorial();yield return null;

        Assert.That(game.State.TutorialStep,Is.Zero);
        Assert.That(game.State.Tasks,Is.Empty);
        Assert.That(DialogueManager.Instance.IsDialogueActive,Is.True);
    }

    [UnityTest]
    public IEnumerator ChapterOneLoadsWithUniqueBoundObjectivesAndTouchControls()
    {
        ClearSave();
        VirtualJoystick.SaveMode(JoystickMode.Fixed);
        SceneManager.LoadScene("AdventureChapter1");

        AdventureGame game = null;
        for (int i = 0; i < 120 && !game; i++)
        {
            yield return null;
            game = Object.FindAnyObjectByType<AdventureGame>();
        }

        Assert.That(game, Is.Not.Null);
        Assert.That(game.Content, Is.Not.Null);
        Assert.That(game.Centers.Length, Is.EqualTo(game.Content.Areas.Length));

        var points = Object.FindObjectsByType<AdventurePoint>(FindObjectsInactive.Include);
        foreach (var task in game.Content.Tasks.GroupBy(t => (t.Area, t.Target)).Select(g => g.First()))
            Assert.That(points.Count(p => p.Area == task.Area && p.Target == task.Target), Is.EqualTo(1), task.Id);
        Assert.That(points.Any(p => p.name.Contains("activity")), Is.False);

        var hudCanvas = GameObject.Find("Alif • Adventure").GetComponent<Canvas>();
        Assert.That(hudCanvas.enabled && hudCanvas.gameObject.activeInHierarchy, Is.True);
        var joysticks = Object.FindObjectsByType<VirtualJoystick>(FindObjectsInactive.Include);
        Assert.That(joysticks.Any(j => j.name == "VirtualJoystick"), Is.False);
        var joystick = joysticks.Single(j => j.name == "Joystick touch area");
        Assert.That(joystick.Mode, Is.EqualTo(JoystickMode.Fixed));
        joystick.SetMode(JoystickMode.Hidden);
        Assert.That(joystick.GetComponent<Graphic>().raycastTarget, Is.False);
    }

    [TearDown]
    public void TearDown()
    {
        ClearSave();
        PlayerPrefs.DeleteKey(VirtualJoystick.ModePreferenceKey);
    }

    static void ClearSave()
    {
        PlayerPrefs.DeleteKey(AdventureSave.Key);
        PlayerPrefs.DeleteKey(AdventureSave.BackupKey);
        PlayerPrefs.DeleteKey(AdventureSave.LegacyKey);
        PlayerPrefs.DeleteKey(AdventureSave.LegacyBackupKey);
        PlayerPrefs.DeleteKey(AdventureSave.RecoveryKey);
        PlayerPrefs.DeleteKey("Alif_HasSave");
        PlayerPrefs.DeleteKey("Alif_SelectedChapter");
        PlayerPrefs.DeleteKey("Alif_HighestChapterUnlocked");
    }
}
