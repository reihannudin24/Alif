using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Alif.UI;

namespace Alif.EditorTools
{
    /// <summary>
    /// Tool editor sekali-klik untuk membangun scene cutscene pembuka Chapter 1. Tiap "scene"
    /// adalah komposisi 2-3 panel gambar (kayak halaman komik) yang muncul satu-satu dengan
    /// animasi slide+fade — panel di paruh atas duluan, baru paruh bawah. Dibuka otomatis
    /// begitu pemain klik "MAIN BARU" di Main Menu.
    ///
    /// Cara pakai: menu "Alif > 5) Build Chapter 1 Cutscene Scene" — otomatis membuka/membuat
    /// Assets/Scenes/Chapter1Cutscene.unity. Aman dijalankan berulang kali.
    /// </summary>
    public static class AlifCutsceneBuilder
    {
        private const string TargetScenePath = "Assets/Scenes/Chapter1Cutscene.unity";
        private const string ArtFolder = "Assets/Sprites/Cutscenes/Chapter1";
        private const int MaxSlots = 3;

        private struct PartDef
        {
            public string File;
            public Vector2 AnchorMin;
            public Vector2 AnchorMax;
            public string SpeakerName;
            public string Line;

            public PartDef(string file, Vector2 anchorMin, Vector2 anchorMax, string speakerName, string line)
            {
                File = file;
                AnchorMin = anchorMin;
                AnchorMax = anchorMax;
                SpeakerName = speakerName;
                Line = line;
            }
        }

        // Komposisi ditentukan dari mockup yang dikasih user: Scene 1 = 2 panel (atas TibaSolo,
        // bawah DalamKereta, tanpa dialog — cold-open wordless). Scene 2 = 3 panel (atas penuh
        // Gambir, bawah kiri Peron + bawah kanan Badge), dua panel terakhir ini yang bawa baris
        // pembuka SCENE 1 skrip storyline ("Akhirnya sampai juga..." / "Perut keroncongan...").
        //
        // PENTING: nama file di Assets/Sprites/Cutscenes/Chapter1/ TIDAK cocok sama isi
        // gambarnya (salah identifikasi isi file waktu pertama kali di-import, ketauan setelah
        // user lihat urutan yang kepasang salah). Isi asli tiap file (dicek ulang manual):
        //   Chapter1_01_Gambir.png     -> sebenernya "Peron" (jalan turun di peron Solo)
        //   Chapter1_02_DalamKereta.png -> sebenernya "Gambir" (berdiri ambil bagasi atas)
        //   Chapter1_03_TibaSolo.png   -> sebenernya "Badge" (close-up tangan di perut)
        //   Chapter1_04_Peron.png      -> sebenernya "TibaSolo" (kereta masuk peron, eksterior)
        //   Chapter1_05_Badge.png      -> sebenernya "DalamKereta" (di kursi kereta, laptop)
        // Referensi File di bawah sengaja disesuaikan ke ISI ASLINYA, bukan ke nama filenya —
        // kalau nanti file-nya di-rename biar konsisten sama isinya, update juga referensi ini.
        private const string AlifBatin = "Alif (Batin)";

        private static readonly PartDef[][] Scenes =
        {
            new[]
            {
                new PartDef("Chapter1_04_Peron.png", new Vector2(0, 0.5f), new Vector2(1, 1), "", ""), // isi: TibaSolo
                new PartDef("Chapter1_05_Badge.png", new Vector2(0, 0), new Vector2(1, 0.5f), "", ""), // isi: DalamKereta
            },
            new[]
            {
                new PartDef("Chapter1_02_DalamKereta.png", new Vector2(0, 0.5f), new Vector2(1, 1), "", ""), // isi: Gambir
                new PartDef("Chapter1_01_Gambir.png", new Vector2(0, 0), new Vector2(0.5f, 0.5f), AlifBatin, "Akhirnya sampai juga..."), // isi: Peron
                new PartDef("Chapter1_03_TibaSolo.png", new Vector2(0.5f, 0), new Vector2(1, 0.5f), AlifBatin, "Perut udah keroncongan nih. Makan ayam geprek di warung Bu Siti deket sini enak kali ya."), // isi: Badge
            },
        };

        [MenuItem("Alif/5) Build Chapter 1 Cutscene Scene")]
        public static void BuildCutsceneScene()
        {
            OpenOrCreateTargetScene();
            BuildCutsceneInActiveScene();

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            RegisterInBuildSettings();

            Debug.Log("[Alif] Chapter 1 Cutscene scene selesai dibangun & disimpan.");
        }

        /// <summary>Entry point buat CLI (-executeMethod) — pola sama seperti builder lain.</summary>
        public static void BuildCutsceneSceneCLI()
        {
            BuildCutsceneScene();
        }

        private static void OpenOrCreateTargetScene()
        {
            if (File.Exists(TargetScenePath))
            {
                EditorSceneManager.OpenScene(TargetScenePath);
                return;
            }

            AlifDemoSceneBuilder.EnsureFolder("Assets/Scenes");
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(newScene, TargetScenePath);
        }

        private static void BuildCutsceneInActiveScene()
        {
            var scene = SceneManager.GetActiveScene();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Object.DestroyImmediate(root);
            }

            AlifDemoSceneBuilder.EnsureEventSystem();
            BuildCamera();

            GameObject canvasGO = AlifDemoSceneBuilder.FindOrCreateUIRoot("Canvas");
            Canvas canvas = AlifDemoSceneBuilder.GetOrAddComponent<Canvas>(canvasGO);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = AlifDemoSceneBuilder.GetOrAddComponent<CanvasScaler>(canvasGO);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;

            AlifDemoSceneBuilder.GetOrAddComponent<GraphicRaycaster>(canvasGO);

            BuildBackdrop(canvasGO.transform);
            (Transform panelContainer, Image[] slotImages, RectTransform[] slotRects, CanvasGroup[] slotGroups) = BuildPanelSlots(canvasGO.transform);
            (GameObject dialogueBox, TextMeshProUGUI speakerNameText, TextMeshProUGUI dialogueText) = BuildDialogueOverlay(panelContainer);
            Button nextButton = BuildNextButton(canvasGO.transform);
            Button skipButton = BuildSkipButton(canvasGO.transform);

            CutscenePlayerController controller = AlifDemoSceneBuilder.GetOrAddComponent<CutscenePlayerController>(canvasGO);
            AlifDemoSceneBuilder.SetSerializedObjectList(controller, "_slotImages", new List<Object>(slotImages));
            AlifDemoSceneBuilder.SetSerializedObjectList(controller, "_slotRects", new List<Object>(slotRects));
            AlifDemoSceneBuilder.SetSerializedObjectList(controller, "_slotCanvasGroups", new List<Object>(slotGroups));
            AlifDemoSceneBuilder.SetSerializedRef(controller, "_dialogueBox", dialogueBox);
            AlifDemoSceneBuilder.SetSerializedRef(controller, "_speakerNameText", speakerNameText);
            AlifDemoSceneBuilder.SetSerializedRef(controller, "_dialogueText", dialogueText);
            SetScenesArray(controller);

            nextButton.onClick = new Button.ButtonClickedEvent();
            UnityEventTools.AddPersistentListener(nextButton.onClick, controller.OnNextClicked);

            skipButton.onClick = new Button.ButtonClickedEvent();
            UnityEventTools.AddPersistentListener(skipButton.onClick, controller.OnSkipClicked);
        }

        private static void BuildCamera()
        {
            GameObject cameraGO = AlifDemoSceneBuilder.FindOrCreateRoot("Main Camera");
            cameraGO.tag = "MainCamera";

            Camera cam = AlifDemoSceneBuilder.GetOrAddComponent<Camera>(cameraGO);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.orthographic = true;

            AlifDemoSceneBuilder.GetOrAddComponent<AudioListener>(cameraGO);
        }

        private static void BuildBackdrop(Transform canvasTransform)
        {
            GameObject bg = AlifDemoSceneBuilder.FindOrCreateChild(canvasTransform, "Backdrop");
            RectTransform rt = bg.GetComponent<RectTransform>();
            AlifDemoSceneBuilder.StretchFull(rt);
            AlifDemoSceneBuilder.AddImage(rt, Color.black);
        }

        // Kolam tetap 3 slot (jumlah panel terbanyak yang dipakai scene manapun). Anchor tiap
        // slot di-set ulang runtime oleh CutscenePlayerController sesuai layout scene aktif —
        // di sini cuma disiapkan container-nya + komponen Image/CanvasGroup kosong.
        // preserveAspect = true (bukan stretch penuh) supaya gambar nggak keliatan gepeng
        // kalau aspect rasio aslinya beda dari slot yang ditempatinya.
        private static (Transform, Image[], RectTransform[], CanvasGroup[]) BuildPanelSlots(Transform canvasTransform)
        {
            GameObject container = AlifDemoSceneBuilder.FindOrCreateChild(canvasTransform, "PanelContainer");
            RectTransform containerRT = container.GetComponent<RectTransform>();
            AlifDemoSceneBuilder.SetRect(containerRT, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -16), new Vector2(1180, 620));

            var images = new Image[MaxSlots];
            var rects = new RectTransform[MaxSlots];
            var groups = new CanvasGroup[MaxSlots];

            for (int i = 0; i < MaxSlots; i++)
            {
                GameObject slot = AlifDemoSceneBuilder.FindOrCreateChild(container.transform, $"Slot{i}");
                RectTransform slotRT = slot.GetComponent<RectTransform>();
                AlifDemoSceneBuilder.SetRect(slotRT, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

                Image img = AlifDemoSceneBuilder.GetOrAddComponent<Image>(slot);
                img.preserveAspect = true;

                CanvasGroup group = AlifDemoSceneBuilder.GetOrAddComponent<CanvasGroup>(slot);

                images[i] = img;
                rects[i] = slotRT;
                groups[i] = group;
            }

            return (container.transform, images, rects, groups);
        }

        // Dibikin sengaja BEDA gayanya dari Dialogue_Panel gameplay (AlifDemoSceneBuilder.
        // BuildDialoguePanel — box gelap + portrait kiri + teks rata kiri): di sini teksnya
        // nge-overlay langsung di atas gambar (bukan box terpisah di bawahnya), nama pembicara
        // jadi chip kecil, baris dialog rata tengah miring, tanpa portrait. Ditaruh sebagai
        // child TERAKHIR dari PanelContainer supaya selalu tergambar di atas panel manapun.
        private static (GameObject, TextMeshProUGUI, TextMeshProUGUI) BuildDialogueOverlay(Transform panelContainerTransform)
        {
            GameObject overlay = AlifDemoSceneBuilder.FindOrCreateChild(panelContainerTransform, "DialogueOverlay");
            overlay.transform.SetAsLastSibling();
            RectTransform overlayRT = overlay.GetComponent<RectTransform>();
            AlifDemoSceneBuilder.SetRect(overlayRT, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(0, 140));
            AlifDemoSceneBuilder.AddImage(overlayRT, new Color(0f, 0f, 0f, 0.5f));

            GameObject chip = AlifDemoSceneBuilder.FindOrCreateChild(overlay.transform, "SpeakerChip");
            RectTransform chipRT = chip.GetComponent<RectTransform>();
            AlifDemoSceneBuilder.SetRect(chipRT, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -14), new Vector2(180, 30));
            AlifDemoSceneBuilder.AddImage(chipRT, new Color(0.93f, 0.53f, 0.16f, 1f));

            TextMeshProUGUI speakerNameText = AlifDemoSceneBuilder.FindOrCreateText(chip.transform, "SpeakerNameText", "",
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, 14, TextAlignmentOptions.Center);
            speakerNameText.fontStyle = FontStyles.Bold;
            speakerNameText.color = Color.black;

            TextMeshProUGUI dialogueText = AlifDemoSceneBuilder.FindOrCreateText(overlay.transform, "DialogueText", "",
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0), new Vector2(0, 12), new Vector2(-100, -56), 19, TextAlignmentOptions.Center);
            dialogueText.fontStyle = FontStyles.Italic;
            dialogueText.color = Color.white;

            return (overlay, speakerNameText, dialogueText);
        }

        private static Button BuildNextButton(Transform canvasTransform)
        {
            GameObject go = AlifDemoSceneBuilder.FindOrCreateChild(canvasTransform, "NextButton");
            RectTransform rt = go.GetComponent<RectTransform>();
            AlifDemoSceneBuilder.SetRect(rt, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-24, 24), new Vector2(160, 52));

            Image img = AlifDemoSceneBuilder.AddImage(rt, new Color(0.93f, 0.53f, 0.16f, 1f));
            Button button = AlifDemoSceneBuilder.GetOrAddComponent<Button>(go);
            button.targetGraphic = img;

            TextMeshProUGUI label = AlifDemoSceneBuilder.FindOrCreateText(go.transform, "Label", "LANJUT >",
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, 18, TextAlignmentOptions.Center);
            label.fontStyle = FontStyles.Bold;
            label.color = Color.white;

            return button;
        }

        private static Button BuildSkipButton(Transform canvasTransform)
        {
            GameObject go = AlifDemoSceneBuilder.FindOrCreateChild(canvasTransform, "SkipButton");
            RectTransform rt = go.GetComponent<RectTransform>();
            AlifDemoSceneBuilder.SetRect(rt, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -20), new Vector2(120, 36));

            Image img = AlifDemoSceneBuilder.AddImage(rt, new Color(1f, 1f, 1f, 0.15f));
            Button button = AlifDemoSceneBuilder.GetOrAddComponent<Button>(go);
            button.targetGraphic = img;

            TextMeshProUGUI label = AlifDemoSceneBuilder.FindOrCreateText(go.transform, "Label", "LEWATI >>",
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, 13, TextAlignmentOptions.Center);
            label.fontStyle = FontStyles.Bold;
            label.color = new Color(1f, 1f, 1f, 0.85f);

            return button;
        }

        private static void SetScenesArray(CutscenePlayerController controller)
        {
            var so = new SerializedObject(controller);
            SerializedProperty scenesProp = so.FindProperty("_scenes");
            scenesProp.arraySize = Scenes.Length;

            for (int s = 0; s < Scenes.Length; s++)
            {
                PartDef[] parts = Scenes[s];
                SerializedProperty sceneProp = scenesProp.GetArrayElementAtIndex(s);
                SerializedProperty partsProp = sceneProp.FindPropertyRelative("Parts");
                partsProp.arraySize = parts.Length;

                for (int p = 0; p < parts.Length; p++)
                {
                    PartDef def = parts[p];
                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtFolder}/{def.File}");
                    if (sprite == null)
                    {
                        Debug.LogWarning($"[Alif] Cutscene sprite tidak ditemukan: '{ArtFolder}/{def.File}'.");
                    }

                    SerializedProperty partProp = partsProp.GetArrayElementAtIndex(p);
                    partProp.FindPropertyRelative("Image").objectReferenceValue = sprite;
                    partProp.FindPropertyRelative("AnchorMin").vector2Value = def.AnchorMin;
                    partProp.FindPropertyRelative("AnchorMax").vector2Value = def.AnchorMax;
                    partProp.FindPropertyRelative("SpeakerName").stringValue = def.SpeakerName;
                    partProp.FindPropertyRelative("Line").stringValue = def.Line;
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void RegisterInBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == TargetScenePath);

            // Selipkan tepat setelah MainMenu, sebelum ChapterSelect/SampleScene.
            int insertIndex = scenes.FindIndex(s => s.path == "Assets/Scenes/MainMenu.unity");
            insertIndex = insertIndex >= 0 ? insertIndex + 1 : 0;
            scenes.Insert(insertIndex, new EditorBuildSettingsScene(TargetScenePath, true));

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
