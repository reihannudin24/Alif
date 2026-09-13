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
        // Input fisik (Keyboard.current/InputAction) tidak bisa disimulasikan pada editor
        // yang tidak fokus: state perangkat di-reset setiap frame, termasuk keyboard
        // software (dibuktikan lewat diagnostik QueueStateEvent). Gerakkan pemain lewat
        // API scripted-direction publik — jalur FixedUpdate → whitelist lantai →
        // akumulasi jarak tutorial yang diuji tetap jalur produksi yang sama.
        player.SetAdventureDirection(Vector2.right);
        for(int i=0;i<120&&game.State.TutorialStep==1;i++)
        {
            yield return new WaitForFixedUpdate();
            yield return null;
        }
        player.SetAdventureDirection(Vector2.zero);yield return null;
        Assert.That(game.State.TutorialStep,Is.EqualTo(2));

        SceneManager.LoadScene("AdventureChapter1");yield return null;
        game=null;
        for(int i=0;i<120&&!game;i++){yield return null;game=Object.FindAnyObjectByType<AdventureGame>();}
        Assert.That(game.State.TutorialStep,Is.EqualTo(2));
        player=Object.FindAnyObjectByType<PlayerController>();
        player.SetAdventureDirection(Vector2.right);
        for(int i=0;i<120&&player.transform.position.x<2.1f;i++)
        {
            yield return new WaitForFixedUpdate();
            yield return null;
        }
        player.SetAdventureDirection(Vector2.zero);yield return null;
        Assert.That(Vector2.Distance(player.transform.position,new Vector2(2.6f,-2.25f)),Is.LessThan(1.35f));

        player.Interact();yield return null;
        Assert.That(DialogueManager.Instance.IsDialogueActive,Is.True);
        DialogueManager.Instance.AdvanceDialogue();
        // Di editor tanpa fokus, satu Advance kadang tertelan oleh timing frame — ulangi
        // selama dialog masih aktif, seperti pemain menekan tombol lanjut lagi.
        for(int i=0;i<60&&game.State.TutorialStep<3;i++)
        {
            if(DialogueManager.Instance.IsDialogueActive)DialogueManager.Instance.AdvanceDialogue();
            yield return null;
        }
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
        // Tombol modal menolak klik dalam 0.13 detik setelah card dibuka (anti click-through
        // dari layar sebelumnya) — beri jeda realistis sebelum menutup jurnal.
        yield return new WaitForSecondsRealtime(.15f);
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
        if(DialogueManager.Instance!=null)
        {
            while(DialogueManager.Instance.IsDialogueActive)
                DialogueManager.Instance.AdvanceDialogue();
        }
        if(GameManager.Instance!=null)
            GameManager.Instance.SetState(GameManager.GameState.Playing);
    }
}
