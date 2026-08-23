using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Alif.Core;
using Alif.UI;

namespace Alif.EditorTools
{
    /// <summary>
    /// Tool editor sekali-klik untuk membangun scene Main Menu: background, logo, tombol
    /// "Main Baru"/"Lanjutkan", label versi, dan pill bahasa (dekoratif, belum ada sistem
    /// localization di project ini). Layout meniru referensi game "Citampi Stories".
    ///
    /// Cara pakai: menu "Alif > 3) Build Main Menu Scene" — otomatis membuka/membuat
    /// Assets/Scenes/MainMenu.unity. Aman dijalankan berulang kali (rebuild bersih dari nol).
    /// </summary>
    public static class AlifMainMenuBuilder
    {
        private const string TargetScenePath = "Assets/Scenes/MainMenu.unity";

        private const string BackgroundSpritePath = "Assets/Sprites/Backgrounds/KosKosan_Halaman.png";
        private const string LogoSpritePath = "Assets/Sprites/UI/Logo_Alif.png";
        private const string BgmClipPath = "Assets/Audio/BGM_MainMenu.wav";
        private const string ClickSfxClipPath = "Assets/Audio/SFX_ButtonClick.wav";

        private static readonly Color OrangeButton = new Color(0.93f, 0.53f, 0.16f, 1f);
        private static readonly Color OrangeButtonDisabled = new Color(0.6f, 0.6f, 0.6f, 1f);
        private static readonly Color QuitButton = new Color(0.32f, 0.24f, 0.16f, 0.9f); // coklat gelap, sengaja kurang menonjol dari 2 tombol utama.
        private static readonly Color PillBackground = new Color(1f, 1f, 1f, 0.92f);
        private static readonly Color PillText = new Color(0.15f, 0.15f, 0.15f, 1f);

        [MenuItem("Alif/3) Build Main Menu Scene")]
        public static void BuildMainMenuScene()
        {
            OpenOrCreateTargetScene();
            BuildMainMenuInActiveScene();

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            RegisterInBuildSettings();

            Debug.Log("[Alif] Main Menu scene selesai dibangun & disimpan. Tekan Play untuk cek tampilannya.");
        }

        /// <summary>
        /// Entry point buat CLI (-executeMethod), dipisah dari menu item supaya bisa dipanggil
        /// dari batch mode tanpa lewat Editor interaktif — pola sama seperti
        /// AlifDemoSceneBuilder.RunFullPipelineCLI.
        /// </summary>
        public static void BuildMainMenuSceneCLI()
        {
            BuildMainMenuScene();
        }

        private static void OpenOrCreateTargetScene()
        {
            if (File.Exists(TargetScenePath))
            {
                EditorSceneManager.OpenScene(TargetScenePath);
                return;
            }

            EnsureFolder("Assets/Scenes");
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(newScene, TargetScenePath);
        }

        private static void BuildMainMenuInActiveScene()
        {
            CleanAllRootObjects();

            AlifDemoSceneBuilder.EnsureEventSystem();
            BuildCamera();

            GameObject canvasGO = FindOrCreateUIRoot("Canvas");
            Canvas canvas = GetOrAddComponent<Canvas>(canvasGO);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = GetOrAddComponent<CanvasScaler>(canvasGO);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;

            GetOrAddComponent<GraphicRaycaster>(canvasGO);

            BuildBackground(canvasGO.transform);
            BuildLogo(canvasGO.transform);
            BuildVersionLabel(canvasGO.transform);
            BuildLanguagePill(canvasGO.transform);

            Button newGameButton = BuildMenuButton(canvasGO.transform, "NewGameButton", "MAIN BARU", 0f);
            Button continueButton = BuildMenuButton(canvasGO.transform, "ContinueButton", "LANJUTKAN", -84f);
            Button quitButton = BuildMenuButton(canvasGO.transform, "QuitButton", "KELUAR", -152f, QuitButton, new Vector2(200, 44), 16f);

            AudioManager audioManager = BuildAudio(canvasGO.transform);
            AudioClip clickSfx = AssetDatabase.LoadAssetAtPath<AudioClip>(ClickSfxClipPath);

            QuitConfirmationUI quitConfirmation = BuildQuitConfirmationPopup(canvasGO.transform, audioManager, clickSfx);

            MainMenuController controller = GetOrAddComponent<MainMenuController>(canvasGO);
            SetSerializedRef(controller, "_newGameButton", newGameButton);
            SetSerializedRef(controller, "_continueButton", continueButton);
            SetSerializedRef(controller, "_audioManager", audioManager);
            SetSerializedRef(controller, "_backgroundMusic", AssetDatabase.LoadAssetAtPath<AudioClip>(BgmClipPath));
            SetSerializedRef(controller, "_quitConfirmation", quitConfirmation);

            newGameButton.onClick = new Button.ButtonClickedEvent();
            UnityEventTools.AddPersistentListener(newGameButton.onClick, controller.OnNewGameClicked);
            AddClickSfxListener(newGameButton, audioManager, clickSfx);

            continueButton.onClick = new Button.ButtonClickedEvent();
            UnityEventTools.AddPersistentListener(continueButton.onClick, controller.OnContinueClicked);
            AddClickSfxListener(continueButton, audioManager, clickSfx);

            quitButton.onClick = new Button.ButtonClickedEvent();
            UnityEventTools.AddPersistentListener(quitButton.onClick, controller.OnQuitClicked);
            AddClickSfxListener(quitButton, audioManager, clickSfx);
        }

        // Bunyi klik ditambahkan sebagai persistent listener KEDUA di tiap tombol (Button.onClick
        // mendukung banyak listener) — jadi nggak ganggu listener navigasi yang sudah ada.
        private static void AddClickSfxListener(Button button, AudioManager audioManager, AudioClip clickSfx)
        {
            if (audioManager == null || clickSfx == null)
            {
                return;
            }

            UnityEventTools.AddObjectPersistentListener(button.onClick, audioManager.PlaySfx, clickSfx);
        }

        // ---------------------------------------------------------------
        // AUDIO — BGM menu + SFX klik tombol, di-generate "Alif > 6) Generate Placeholder Audio".
        // ---------------------------------------------------------------
        private static AudioManager BuildAudio(Transform canvasTransform)
        {
            GameObject audioGO = FindOrCreateChild(canvasTransform, "Audio");

            GameObject musicGO = FindOrCreateChild(audioGO.transform, "MusicSource");
            AudioSource musicSource = GetOrAddComponent<AudioSource>(musicGO);
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.volume = 0.5f;

            GameObject sfxGO = FindOrCreateChild(audioGO.transform, "SfxSource");
            AudioSource sfxSource = GetOrAddComponent<AudioSource>(sfxGO);
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.volume = 0.8f;

            AudioManager audioManager = GetOrAddComponent<AudioManager>(audioGO);
            SetSerializedRef(audioManager, "_musicSource", musicSource);
            SetSerializedRef(audioManager, "_sfxSource", sfxSource);

            if (AssetDatabase.LoadAssetAtPath<AudioClip>(BgmClipPath) == null)
            {
                Debug.LogWarning($"[Alif] Audio belum digenerate — jalankan menu 'Alif > 6) Generate Placeholder Audio' dulu, lalu build ulang scene ini.");
            }

            return audioManager;
        }

        private static void CleanAllRootObjects()
        {
            var scene = SceneManager.GetActiveScene();
            GameObject[] roots = scene.GetRootGameObjects();
            foreach (GameObject root in roots)
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void BuildCamera()
        {
            GameObject cameraGO = FindOrCreateRoot("Main Camera");
            cameraGO.tag = "MainCamera";

            Camera cam = GetOrAddComponent<Camera>(cameraGO);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.53f, 0.75f, 0.87f); // langit, cadangan kalau ada letterboxing.
            cam.orthographic = true;

            GetOrAddComponent<AudioListener>(cameraGO);
        }

        // ---------------------------------------------------------------
        // BACKGROUND (art ilustrasi kos-kosan yang sudah ada, dipakai sebagai backdrop menu)
        // ---------------------------------------------------------------
        private static void BuildBackground(Transform canvasTransform)
        {
            Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundSpritePath);
            if (bgSprite == null)
            {
                Debug.LogWarning($"[Alif] Background sprite tidak ditemukan di '{BackgroundSpritePath}', dilewati.");
                return;
            }

            GameObject bg = FindOrCreateChild(canvasTransform, "Background");
            RectTransform rt = bg.GetComponent<RectTransform>();
            StretchFull(rt);
            // Overscale dikit (8%) supaya ada "slack" buat AmbientImageMotion pan+zoom pelan
            // tanpa nyingkap tepi gambar (area di luar frame layar).
            rt.localScale = new Vector3(1.08f, 1.08f, 1f);

            Image img = GetOrAddComponent<Image>(bg);
            img.sprite = bgSprite;
            img.preserveAspect = false; // full-bleed, boleh sedikit stretch — background art bukan pixel-art presisi.
            img.color = Color.white;

            GetOrAddComponent<AmbientImageMotion>(bg);
        }

        // ---------------------------------------------------------------
        // LOGO
        // ---------------------------------------------------------------
        private static void BuildLogo(Transform canvasTransform)
        {
            EnsureLogoImportSettings();

            Sprite logoSprite = AssetDatabase.LoadAssetAtPath<Sprite>(LogoSpritePath);
            if (logoSprite == null)
            {
                Debug.LogWarning($"[Alif] Logo sprite tidak ditemukan di '{LogoSpritePath}', dilewati.");
                return;
            }

            GameObject logo = FindOrCreateChild(canvasTransform, "Logo");
            RectTransform rt = logo.GetComponent<RectTransform>();
            SetRect(rt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -36), new Vector2(360, 290));

            Image img = GetOrAddComponent<Image>(logo);
            img.sprite = logoSprite;
            img.preserveAspect = true;
        }

        // Logo-nya ilustrasi halus (bukan pixel art), jadi butuh Bilinear + alpha transparency —
        // sama kayak penanganan Portrait_Alif.png di AlifDemoSceneBuilder.GetOrCreateAlifCharacterData.
        // Selalu di-enforce (bukan skip kalau sudah "Sprite") karena default import project ini
        // (2D template) kadang otomatis pilih Sprite Mode "Multiple" dengan auto-slice per pulau
        // alpha — logo ini harus tetap SATU sprite utuh, bukan kepotong per huruf.
        private static void EnsureLogoImportSettings()
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(LogoSpritePath);
            if (importer == null)
            {
                return;
            }

            bool alreadyCorrect = importer.textureType == TextureImporterType.Sprite
                && importer.spriteImportMode == SpriteImportMode.Single
                && importer.filterMode == FilterMode.Bilinear;
            if (alreadyCorrect)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            AssetDatabase.Refresh();
        }

        // ---------------------------------------------------------------
        // VERSION LABEL (pojok kanan atas)
        // ---------------------------------------------------------------
        private static void BuildVersionLabel(Transform canvasTransform)
        {
            string version = $"v{Application.version}";
            TextMeshProUGUI text = FindOrCreateText(canvasTransform, "VersionText", version,
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-16, -14), new Vector2(140, 24), 14, TextAlignmentOptions.Right);
            text.fontStyle = FontStyles.Bold;
            text.color = new Color(1f, 1f, 1f, 0.85f);
        }

        // ---------------------------------------------------------------
        // LANGUAGE PILL (pojok kiri atas) — dekoratif, project belum punya sistem localization.
        // Cuma ikon bendera bulat, tanpa label teks "INDONESIA".
        // ---------------------------------------------------------------
        private static void BuildLanguagePill(Transform canvasTransform)
        {
            GameObject pill = FindOrCreateChild(canvasTransform, "LanguagePill");
            RectTransform pillRT = pill.GetComponent<RectTransform>();
            SetRect(pillRT, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -16), new Vector2(44, 44));

            Image pillImg = AddImage(pillRT, PillBackground);
            pillImg.sprite = GetOrCreateCircleSprite("LanguagePillCircle", Color.white, 128);

            GameObject flag = FindOrCreateChild(pill.transform, "FlagIcon");
            RectTransform flagRT = flag.GetComponent<RectTransform>();
            SetRect(flagRT, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(22, 16));
            AddImage(flagRT, new Color(0.8f, 0.1f, 0.1f, 1f));

            GameObject flagWhite = FindOrCreateChild(flag.transform, "White");
            RectTransform flagWhiteRT = flagWhite.GetComponent<RectTransform>();
            SetRect(flagWhiteRT, new Vector2(0, 0), new Vector2(1, 0.5f), new Vector2(0.5f, 0), Vector2.zero, Vector2.zero);
            AddImage(flagWhiteRT, Color.white);

            // Sisa "Label" dari build versi lama (kalau ada) — bersihkan biar nggak nyisa GameObject mati.
            Transform staleLabel = pill.transform.Find("Label");
            if (staleLabel != null)
            {
                Object.DestroyImmediate(staleLabel.gameObject);
            }
        }

        // ---------------------------------------------------------------
        // POPUP KONFIRMASI KELUAR — "Apakah anda yakin mau keluar?" (Ya, Keluar / Batal).
        // Dipakai tombol KELUAR sebelum Application.Quit() beneran dieksekusi, lihat
        // MainMenuController.OnQuitClicked & QuitConfirmationUI.
        // ---------------------------------------------------------------
        private static QuitConfirmationUI BuildQuitConfirmationPopup(Transform canvasTransform, AudioManager audioManager, AudioClip clickSfx)
        {
            GameObject overlay = FindOrCreateChild(canvasTransform, "QuitConfirmPopup");
            RectTransform overlayRT = overlay.GetComponent<RectTransform>();
            StretchFull(overlayRT);
            AddImage(overlayRT, new Color(0f, 0f, 0f, 0.6f));
            // Paling belakangan dibangun di antara elemen menu, tapi pastikan tetap di depan
            // (di bawah/nutupin semua tombol menu) selama build ini berjalan berkali-kali.
            overlay.transform.SetAsLastSibling();

            GameObject card = FindOrCreateChild(overlay.transform, "Card");
            RectTransform cardRT = card.GetComponent<RectTransform>();
            SetRect(cardRT, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420, 230));
            AddImage(cardRT, new Color(0.16f, 0.12f, 0.08f, 0.97f));

            FindOrCreateText(card.transform, "MessageText", "Apakah anda yakin mau keluar?",
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f), new Vector2(0, -28), new Vector2(-32, 64), 20, TextAlignmentOptions.Center)
                .fontStyle = FontStyles.Bold;

            // "Batal" (aman, dominan) di atas — "Ya, Keluar" (destruktif) di bawah & warna
            // kurang menonjol, sama filosofinya kayak QuitButton di menu utama.
            Button cancelButton = BuildMenuButton(card.transform, "CancelButton", "BATAL", -8f, OrangeButton, new Vector2(340, 44), 16f);
            Button confirmButton = BuildMenuButton(card.transform, "ConfirmButton", "YA, KELUAR", -60f, QuitButton, new Vector2(340, 40), 15f);

            // Overlay-nya SENGAJA dibiarkan aktif (bukan SetActive(false)) — visibility diatur
            // lewat CanvasGroup di QuitConfirmationUI, karena Instance-nya cuma ke-set di
            // Awake() dan Awake() nggak pernah jalan buat GameObject yang nonaktif dari awal
            // scene di-load.
            CanvasGroup canvasGroup = GetOrAddComponent<CanvasGroup>(overlay);

            QuitConfirmationUI quitConfirmation = GetOrAddComponent<QuitConfirmationUI>(overlay);
            SetSerializedRef(quitConfirmation, "_canvasGroup", canvasGroup);
            SetSerializedRef(quitConfirmation, "_confirmButton", confirmButton);
            SetSerializedRef(quitConfirmation, "_cancelButton", cancelButton);

            AddClickSfxListener(confirmButton, audioManager, clickSfx);
            AddClickSfxListener(cancelButton, audioManager, clickSfx);

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            return quitConfirmation;
        }

        // ---------------------------------------------------------------
        // TOMBOL MENU (Main Baru / Lanjutkan)
        // ---------------------------------------------------------------
        private static Button BuildMenuButton(Transform canvasTransform, string name, string label, float centerYOffset)
        {
            return BuildMenuButton(canvasTransform, name, label, centerYOffset, OrangeButton, new Vector2(320, 64), 24f);
        }

        private static Button BuildMenuButton(Transform canvasTransform, string name, string label, float centerYOffset,
            Color color, Vector2 size, float fontSize)
        {
            GameObject go = FindOrCreateChild(canvasTransform, name);
            RectTransform rt = go.GetComponent<RectTransform>();
            SetRect(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, centerYOffset), size);

            Image img = AddImage(rt, color);
            Button button = GetOrAddComponent<Button>(go);
            button.targetGraphic = img;

            var colors = button.colors;
            colors.disabledColor = OrangeButtonDisabled;
            button.colors = colors;

            TextMeshProUGUI text = FindOrCreateText(go.transform, "Label", label,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, fontSize, TextAlignmentOptions.Center);
            text.fontStyle = FontStyles.Bold;
            text.color = Color.white;

            return button;
        }

        // ---------------------------------------------------------------
        // BUILD SETTINGS: pastikan MainMenu ada & jadi scene pertama (index 0).
        // ---------------------------------------------------------------
        private static void RegisterInBuildSettings()
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == TargetScenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(TargetScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ---------------------------------------------------------------
        // Wrapper tipis ke helper generik di AlifDemoSceneBuilder (internal static, assembly sama)
        // supaya nggak duplikat logic hierarchy/layout di sini.
        // ---------------------------------------------------------------
        private static GameObject FindOrCreateRoot(string name) => AlifDemoSceneBuilder.FindOrCreateRoot(name);
        private static GameObject FindOrCreateChild(Transform parent, string name) => AlifDemoSceneBuilder.FindOrCreateChild(parent, name);
        private static GameObject FindOrCreateUIRoot(string name) => AlifDemoSceneBuilder.FindOrCreateUIRoot(name);
        private static T GetOrAddComponent<T>(GameObject go) where T : Component => AlifDemoSceneBuilder.GetOrAddComponent<T>(go);
        private static void EnsureFolder(string path) => AlifDemoSceneBuilder.EnsureFolder(path);
        private static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta)
            => AlifDemoSceneBuilder.SetRect(rt, anchorMin, anchorMax, pivot, anchoredPos, sizeDelta);
        private static void StretchFull(RectTransform rt) => AlifDemoSceneBuilder.StretchFull(rt);
        private static Image AddImage(RectTransform rt, Color color) => AlifDemoSceneBuilder.AddImage(rt, color);
        private static TextMeshProUGUI FindOrCreateText(Transform parent, string name, string defaultText,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta,
            float fontSize, TextAlignmentOptions alignment)
            => AlifDemoSceneBuilder.FindOrCreateText(parent, name, defaultText, anchorMin, anchorMax, pivot, anchoredPos, sizeDelta, fontSize, alignment);
        private static void SetSerializedRef(Object target, string fieldName, Object value) => AlifDemoSceneBuilder.SetSerializedRef(target, fieldName, value);
        private static Sprite GetOrCreateCircleSprite(string name, Color color, int size) => AlifDemoSceneBuilder.GetOrCreateCircleSprite(name, color, size);
    }
}
