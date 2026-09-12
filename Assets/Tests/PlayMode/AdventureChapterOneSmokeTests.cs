using System.Collections;
using System.Linq;
using Alif.Adventure;
using Alif.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class AdventureChapterOneSmokeTests
{
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
