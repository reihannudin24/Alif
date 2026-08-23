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
    /// Tool editor sekali-klik untuk membangun scene "Chapter Select" — dibuka dari tombol
    /// Lanjutkan di Main Menu, nunjukin 5 chapter cerita ALIF (lihat storyline capstone project)
    /// sebagai kartu, meniru layout referensi "Lost Words: Beyond the Page".
    ///
    /// Cara pakai: menu "Alif > 4) Build Chapter Select Scene" — otomatis membuka/membuat
    /// Assets/Scenes/ChapterSelect.unity. Aman dijalankan berulang kali (rebuild bersih dari nol).
    /// </summary>
    public static class AlifChapterSelectBuilder
    {
        private const string TargetScenePath = "Assets/Scenes/ChapterSelect.unity";

        private static readonly Color CardBackground = new Color(0.97f, 0.94f, 0.88f, 1f);
        private static readonly Color PlaceholderThumb = new Color(0.55f, 0.4f, 0.28f, 1f);
        private static readonly Color LockOverlayColor = new Color(0f, 0f, 0f, 0.72f);
        private static readonly Color TitleDark = new Color(0.2f, 0.14f, 0.08f, 1f);

        private struct ChapterEntry
        {
            public int Number;
            public string Title;
            public string Subtitle;
            public string ThumbnailPath; // null -> pakai warna placeholder.

            public ChapterEntry(int number, string title, string subtitle, string thumbnailPath)
            {
                Number = number;
                Title = title;
                Subtitle = subtitle;
                ThumbnailPath = thumbnailPath;
            }
        }

        // Ringkasan dari "Storyline Alif - Capstone Project" (5 file .md yang dikirim user) —
        // tiap chapter satu kasus keuangan syariah berbeda (Riba, Gharar, Maysir, Tadlis, Ponzi).
        // Chapter 2 pakai KosKosan_Lantai1.png (koridor kos dengan pintu bernomor) karena
        // lokasinya persis sama dengan denah "Kos-kosan Tio" di skrip Chapter 2. Chapter 3-5
        // belum punya art final (baru ada sketsa denah kasar buat referensi artist), jadi
        // dikasih placeholder warna dulu.
        private static readonly ChapterEntry[] Chapters =
        {
            new ChapterEntry(1, "Chapter 1", "Prolog: Riba & Gharar", "Assets/Sprites/Backgrounds/Stasiun_Depan.jpg"),
            new ChapterEntry(2, "Chapter 2", "Jebakan Riba", "Assets/Sprites/Backgrounds/KosKosan_Lantai1.png"),
            new ChapterEntry(3, "Chapter 3", "Ilusi Maysir", null),
            new ChapterEntry(4, "Chapter 4", "Sindikat Tadlis", null),
            new ChapterEntry(5, "Chapter 5", "The Grand Ponzi", null),
        };

        // Grid 3 kolom x 2 baris (5 kartu terisi, 1 slot kanan-bawah kosong) — posisi relatif
        // ke tengah Canvas (reference resolution 1280x720, sama seperti Main Menu & Demo Scene).
        private static readonly Vector2[] CardPositions =
        {
            new Vector2(-440, 130),
            new Vector2(0, 130),
            new Vector2(440, 130),
            new Vector2(-440, -150),
            new Vector2(0, -150),
        };

        private static readonly Vector2 CardSize = new Vector2(360, 240);

        [MenuItem("Alif/4) Build Chapter Select Scene")]
        public static void BuildChapterSelectScene()
        {
            OpenOrCreateTargetScene();
            BuildChapterSelectInActiveScene();

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            RegisterInBuildSettings();

            Debug.Log("[Alif] Chapter Select scene selesai dibangun & disimpan.");
        }

        /// <summary>
        /// Entry point buat CLI (-executeMethod) — pola sama seperti builder lain di project ini.
        /// </summary>
        public static void BuildChapterSelectSceneCLI()
        {
            BuildChapterSelectScene();
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

        private static void BuildChapterSelectInActiveScene()
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
            BuildTitle(canvasGO.transform);
            Button backButton = BuildBackButton(canvasGO.transform);

            var cards = new List<Object>();
            for (int i = 0; i < Chapters.Length; i++)
            {
                cards.Add(BuildChapterCard(canvasGO.transform, Chapters[i], CardPositions[i]));
            }

            ChapterSelectController controller = AlifDemoSceneBuilder.GetOrAddComponent<ChapterSelectController>(canvasGO);
            AlifDemoSceneBuilder.SetSerializedObjectList(controller, "_chapterCards", cards);

            for (int i = 0; i < cards.Count; i++)
            {
                var card = (ChapterCardUI)cards[i];
                Button cardButton = card.GetComponent<Button>();
                cardButton.onClick = new Button.ButtonClickedEvent();
                UnityEventTools.AddIntPersistentListener(cardButton.onClick, controller.OnChapterClicked, Chapters[i].Number);
            }

            backButton.onClick = new Button.ButtonClickedEvent();
            UnityEventTools.AddPersistentListener(backButton.onClick, controller.OnBackClicked);
        }

        private static void BuildCamera()
        {
            GameObject cameraGO = AlifDemoSceneBuilder.FindOrCreateRoot("Main Camera");
            cameraGO.tag = "MainCamera";

            Camera cam = AlifDemoSceneBuilder.GetOrAddComponent<Camera>(cameraGO);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.16f, 0.11f, 0.08f);
            cam.orthographic = true;

            AlifDemoSceneBuilder.GetOrAddComponent<AudioListener>(cameraGO);
        }

        private static void BuildBackdrop(Transform canvasTransform)
        {
            GameObject bg = AlifDemoSceneBuilder.FindOrCreateChild(canvasTransform, "Backdrop");
            RectTransform rt = bg.GetComponent<RectTransform>();
            AlifDemoSceneBuilder.StretchFull(rt);
            AlifDemoSceneBuilder.AddImage(rt, new Color(0.82f, 0.75f, 0.63f, 1f)); // kertas krem, senada tema kayu/coklat menu utama.
        }

        private static void BuildTitle(Transform canvasTransform)
        {
            TextMeshProUGUI title = AlifDemoSceneBuilder.FindOrCreateText(canvasTransform, "Title", "CHAPTER SELECT",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -36), new Vector2(600, 56), 34, TextAlignmentOptions.Center);
            title.fontStyle = FontStyles.Bold;
            title.color = TitleDark;
        }

        private static Button BuildBackButton(Transform canvasTransform)
        {
            GameObject go = AlifDemoSceneBuilder.FindOrCreateChild(canvasTransform, "BackButton");
            RectTransform rt = go.GetComponent<RectTransform>();
            AlifDemoSceneBuilder.SetRect(rt, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(24, 24), new Vector2(140, 52));

            Image img = AlifDemoSceneBuilder.AddImage(rt, new Color(0.4f, 0.28f, 0.14f, 1f));
            Button button = AlifDemoSceneBuilder.GetOrAddComponent<Button>(go);
            button.targetGraphic = img;

            TextMeshProUGUI label = AlifDemoSceneBuilder.FindOrCreateText(go.transform, "Label", "< KEMBALI",
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, 16, TextAlignmentOptions.Center);
            label.fontStyle = FontStyles.Bold;
            label.color = Color.white;

            return button;
        }

        private static ChapterCardUI BuildChapterCard(Transform canvasTransform, ChapterEntry entry, Vector2 position)
        {
            GameObject card = AlifDemoSceneBuilder.FindOrCreateChild(canvasTransform, $"ChapterCard_{entry.Number}");
            RectTransform cardRT = card.GetComponent<RectTransform>();
            AlifDemoSceneBuilder.SetRect(cardRT, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, CardSize);
            AlifDemoSceneBuilder.AddImage(cardRT, CardBackground);

            // Thumbnail, mengisi 2/3 atas kartu.
            GameObject thumb = AlifDemoSceneBuilder.FindOrCreateChild(card.transform, "Thumbnail");
            RectTransform thumbRT = thumb.GetComponent<RectTransform>();
            AlifDemoSceneBuilder.SetRect(thumbRT, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f), new Vector2(0, -10), new Vector2(-20, 150));
            Image thumbImg = AlifDemoSceneBuilder.GetOrAddComponent<Image>(thumb);
            Sprite thumbSprite = entry.ThumbnailPath != null ? AssetDatabase.LoadAssetAtPath<Sprite>(entry.ThumbnailPath) : null;
            if (thumbSprite != null)
            {
                thumbImg.sprite = thumbSprite;
                thumbImg.color = Color.white;
                thumbImg.preserveAspect = false;
            }
            else
            {
                thumbImg.color = PlaceholderThumb;
            }

            // Judul + subjudul, di bawah thumbnail.
            AlifDemoSceneBuilder.FindOrCreateText(card.transform, "TitleText", entry.Title,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 46), new Vector2(-20, 28), 20, TextAlignmentOptions.Center)
                .fontStyle = FontStyles.Bold;
            TextMeshProUGUI titleText = card.transform.Find("TitleText").GetComponent<TextMeshProUGUI>();
            titleText.color = TitleDark;

            TextMeshProUGUI subtitleText = AlifDemoSceneBuilder.FindOrCreateText(card.transform, "SubtitleText", entry.Subtitle,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(-20, 26), 14, TextAlignmentOptions.Center);
            subtitleText.color = new Color(0.35f, 0.28f, 0.2f, 1f);

            Button button = AlifDemoSceneBuilder.GetOrAddComponent<Button>(card);
            button.targetGraphic = card.GetComponent<Image>();

            // Overlay terkunci — nutup seluruh kartu, di-toggle runtime oleh ChapterSelectController.
            GameObject lockOverlay = AlifDemoSceneBuilder.FindOrCreateChild(card.transform, "LockOverlay");
            RectTransform lockRT = lockOverlay.GetComponent<RectTransform>();
            AlifDemoSceneBuilder.StretchFull(lockRT);
            AlifDemoSceneBuilder.AddImage(lockRT, LockOverlayColor);

            TextMeshProUGUI lockText = AlifDemoSceneBuilder.FindOrCreateText(lockOverlay.transform, "LockText", "TERKUNCI",
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, 18, TextAlignmentOptions.Center);
            lockText.fontStyle = FontStyles.Bold;
            lockText.color = Color.white;

            ChapterCardUI cardUI = AlifDemoSceneBuilder.GetOrAddComponent<ChapterCardUI>(card);
            SetSerializedIntValue(cardUI, "_chapterNumber", entry.Number);
            AlifDemoSceneBuilder.SetSerializedRef(cardUI, "_button", button);
            AlifDemoSceneBuilder.SetSerializedRef(cardUI, "_lockOverlay", lockOverlay);

            return cardUI;
        }

        private static void SetSerializedIntValue(Object target, string fieldName, int value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[Alif] Field '{fieldName}' tidak ditemukan di {target.GetType().Name}.");
                return;
            }

            prop.intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void RegisterInBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == TargetScenePath);

            // Selipkan tepat setelah MainMenu (index 0) kalau ada, biar urutan build settings
            // masuk akal (MainMenu -> ChapterSelect -> SampleScene). Kalau MainMenu belum
            // terdaftar, taruh di awal.
            int insertIndex = scenes.FindIndex(s => s.path == "Assets/Scenes/MainMenu.unity");
            insertIndex = insertIndex >= 0 ? insertIndex + 1 : 0;
            scenes.Insert(insertIndex, new EditorBuildSettingsScene(TargetScenePath, true));

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
