using System.Collections;
using System.Collections.Generic;
using System.IO;
using Alif.Adventure;
using Alif.Core;
using Alif.Dialogue;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Menangkap HUD runtime (canvas yang dibangun AdventureGame saat Play) ke PNG untuk QA
/// visual — screenshot editor tidak pernah memperlihatkan HUD runtime karena kanvasnya
/// dibangun/dihancurkan secara dinamis. Batch mode tidak punya Game View, jadi frame
/// dirender manual lewat camera.Render() (pola AlifScreenshotCapture).
/// </summary>
public sealed class HudCaptureTests
{
    [UnityTest]
    public IEnumerator CaptureRuntimeHudAndDiagnoseSignInteraction()
    {
        ClearSave();
        ChapterProgress.StartNewGame();
        Alif.UI.VirtualJoystick.SaveMode(Alif.UI.JoystickMode.Fixed); // pastikan joystick terlihat
        SceneManager.LoadScene("AdventureChapter1");

        AdventureGame game = null;
        for (int i = 0; i < 120 && !game; i++) { yield return null; game = Object.FindAnyObjectByType<AdventureGame>(); }
        Assert.That(game, Is.Not.Null, "AdventureGame harus hidup untuk menangkap HUD.");
        var player = Object.FindAnyObjectByType<Alif.Player.PlayerController>();
        Debug.Log($"[HudCapture] tutorial step 1, spawn={player.transform.position}");

        yield return new WaitForSecondsRealtime(.4f);
        int written = Capture("playmode-ch1-tutorial.png");

        // Mini-repro interaksi sign tutorial (langkah 2) sambil mencatat geometri aktual.
        game.SkipTutorial();
        yield return null;
        for (int i = 0; i < 90 && DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive; i++)
        {
            DialogueManager.Instance.AdvanceDialogue();
            yield return null;
        }
        // Kembali ke step tutorial 2 persis seperti smoke test: reload scene setelah StartNewGame.
        ClearSave();
        ChapterProgress.StartNewGame();
        SceneManager.LoadScene("AdventureChapter1");
        yield return null;
        game = null;
        for (int i = 0; i < 120 && !game; i++) { yield return null; game = Object.FindAnyObjectByType<AdventureGame>(); }
        player = Object.FindAnyObjectByType<Alif.Player.PlayerController>();
        player.SetAdventureDirection(Vector2.left);
        for (int i = 0; i < 120 && player.transform.position.x > -.6f; i++)
        {
            yield return new WaitForFixedUpdate();
            yield return null;
        }
        player.SetAdventureDirection(Vector2.zero);
        yield return null;
        Debug.Log($"[HudCapture] after walk: pos={player.transform.position} step={game.State.TutorialStep}");

        var hits = Physics2D.OverlapCircleAll(player.transform.position, 1.35f, 1 << 7);
        var names = new List<string>();
        foreach (var hit in hits) names.Add($"{hit.name}@{hit.transform.position:F2}");
        Debug.Log($"[HudCapture] interactables in 1.35 range: {names.Count} [{string.Join(", ", names)}]");

        player.Interact();
        yield return null;
        Debug.Log($"[HudCapture] after Interact: dialogueActive={(DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)}");

        yield return new WaitForSecondsRealtime(.3f);
        written += Capture("playmode-ch1-world.png");
        Assert.That(written, Is.GreaterThan(0), "minimal satu frame HUD harus tertulis ke Logs/Screenshots.");
    }

    static int Capture(string fileName)
    {
        Camera camera = Camera.main;
        if (camera == null) return 0;
        const int width = 1280, height = 720;

        var overlays = new List<Canvas>();
        foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay || !canvas.isActiveAndEnabled) continue;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = Mathf.Max(camera.nearClipPlane + 0.5f, 0.5f);
            overlays.Add(canvas);
        }

        float aspect = camera.aspect;
        var target = new RenderTexture(width, height, 24);
        var image = new Texture2D(width, height, TextureFormat.RGB24, false);
        RenderTexture previous = RenderTexture.active;
        try
        {
            camera.targetTexture = target;
            camera.aspect = (float)width / height;
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            string dir = Path.Combine(Application.dataPath, "..", "Logs", "Screenshots");
            Directory.CreateDirectory(dir);
            File.WriteAllBytes(Path.Combine(dir, fileName), image.EncodeToPNG());
            Debug.Log($"[HudCapture] wrote {fileName}.");
            return 1;
        }
        finally
        {
            camera.targetTexture = null;
            camera.aspect = aspect;
            RenderTexture.active = previous;
            Object.Destroy(image);
            target.Release();
            Object.Destroy(target);
            foreach (var canvas in overlays)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.worldCamera = null;
            }
        }
    }

    [TearDown]
    public void TearDown()
    {
        ClearSave();
        PlayerPrefs.DeleteKey(Alif.UI.VirtualJoystick.ModePreferenceKey);
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
