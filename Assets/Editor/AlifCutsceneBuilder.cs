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
    /// Tool editor sekali-klik untuk membangun scene cutscene pembuka Chapter 1. Scene pertama
    /// adalah "title card" teks prolog (layar hitam + teks tengah). Scene setelahnya berisi
    /// 1-3 gambar yang tampil layar penuh satu per satu bergaya visual novel (tata letak
    /// dipasang CutscenePlayerController). Dibuka otomatis begitu pemain klik "MAIN BARU".
    ///
    /// Cara pakai: menu "Alif > 5) Build Chapter 1 Cutscene Scene" — otomatis membuka/membuat
    /// Assets/Scenes/Chapter1Cutscene.unity. Aman dijalankan berulang kali.
    /// </summary>
    public static class AlifCutsceneBuilder
    {
        private const string TargetScenePath = "Assets/Scenes/Chapter1Cutscene.unity";
        private const string ArtFolder = "Assets/Sprites/Cutscenes/Chapter1";
        private const string Chapter1EndingScenePath = "Assets/Scenes/Chapter1Ending.unity";
        private const string Chapter2CutsceneScenePath = "Assets/Scenes/Chapter2Cutscene.unity";
        private const int MaxSlots = 3;

        private struct PartDef
        {
            public string File;
            public string SpeakerName;
            public string Line;

            public PartDef(string file, string speakerName, string line)
            {
                File = file;
                SpeakerName = speakerName;
                Line = line;
            }
        }

        // Parts kosong = "title card" teks doang (layar hitam + teks di tengah), dipakai buat
        // scene prolog paling awal sebelum panel gambar mulai muncul.
        private struct SceneDef
        {
            public PartDef[] Parts;
            public string IntroText;

            public SceneDef(PartDef[] parts)
            {
                Parts = parts;
                IntroText = "";
            }

            public SceneDef(string introText)
            {
                Parts = System.Array.Empty<PartDef>();
                IntroText = introText;
            }
        }

        // Urutan dari mockup user: Scene 1 = TibaSolo lalu DalamKereta (tanpa dialog — cold-open
        // wordless). Scene 2 = Gambir, Peron, Badge; dua gambar terakhir membawa baris pembuka
        // SCENE 1 skrip storyline ("Akhirnya sampai juga..." / "Perut keroncongan...").
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

        // Teks prolog (scene 0) — sengaja cuma nyeritain "pulang ke Solo", TANPA nyinggung
        // warung Bu Siti atau kasus apa pun, karena di titik cerita ini Alif belum tau soal itu
        // sama sekali (baru ketemu & connect ke plot itu belakangan, lewat dialog Scene 2 skrip).
        private const string PrologText =
            "Solo, sore itu.\n\n" +
            "Setelah bertahun-tahun merantau ke Jakarta, Alif akhirnya kembali menginjakkan kaki di kota kelahirannya.\n\n" +
            "Ia belum tahu, kepulangan ini akan membawanya pada satu misi yang tak pernah ia bayangkan sebelumnya...";

        private static readonly SceneDef[] Scenes =
        {
            new SceneDef(PrologText),
            new SceneDef(new[]
            {
                new PartDef("Chapter1_04_Peron.png", "", ""), // isi: TibaSolo
                new PartDef("Chapter1_05_Badge.png", "", ""), // isi: DalamKereta
            }),
            new SceneDef(new[]
            {
                new PartDef("Chapter1_02_DalamKereta.png", "", ""), // isi: Gambir
                new PartDef("Chapter1_01_Gambir.png", AlifBatin, "Akhirnya sampai juga..."), // isi: Peron
                new PartDef("Chapter1_03_TibaSolo.png", AlifBatin, "Perut udah keroncongan nih. Makan ayam geprek di warung Bu Siti deket sini enak kali ya."), // isi: Badge
            }),
        };

        private static readonly SceneDef[] Chapter1EndingScenes =
        {
            new SceneDef(
                "Warung Bu Siti, menjelang malam.\n\n" +
                "Tekanan untuk menandatangani kontrak telah berlalu. Bu Siti memilih berhenti, memeriksa angka, dan mencari jalan yang transparan."),
            new SceneDef(new[]
            {
                new PartDef("Assets/Sprites/Backgrounds/WarungBuSiti_Interior.png", "Bu Siti", "Ibu tidak akan lagi mengambil keputusan karena panik. Mulai besok, semua pemasukan, kebutuhan, dan akad akan Ibu catat dengan jelas."),
                new PartDef("Assets/Sprites/Backgrounds/WarungBuSiti_Depan.jpg", "Alif (Batin)", "Masalah hari ini selesai, tetapi perjalananku memahami amanah dalam setiap keputusan baru saja dimulai."),
            }),
            new SceneDef(
                "CHAPTER 1 SELESAI\n\n" +
                "Alif membantu Bu Siti menolak janji hasil pasti dan memilih langkah usaha yang lebih jujur, terukur, dan bertanggung jawab.\n\n" +
                "Chapter 2 — Jebakan Riba — telah terbuka."),
        };

        private static readonly SceneDef[] Chapter2IntroScenes =
        {
            new SceneDef(
                "Chapter 2 — Jebakan Riba\n\n" +
                "Beberapa hari setelah persoalan Bu Siti selesai, Alif kembali menjalani harinya di Solo.\n\n" +
                "Sebuah pertemuan tak sengaja akan membawanya pada persoalan baru: jalan keluar cepat yang menyimpan beban panjang."),
            new SceneDef(new[]
            {
                new PartDef("Assets/Sprites/Backgrounds/KosKosan_Halaman.png", "Narator", "Menjelang sore, Alif melintasi sebuah jalan kecil di depan deretan kos mahasiswa."),
            }),
            new SceneDef(new[]
            {
                new PartDef("Assets/Sprites/Backgrounds/KosKosan_Halaman.png", "Narator", "Di depan gerbang, Dimas berdiri mondar-mandir. Tatapannya terpaku pada ponsel dan wajahnya terlihat bingung."),
                new PartDef("Assets/Sprites/Characters/alif/Idle/rotations/east.png", "Alif", "Dimas? Dari tadi kamu kelihatan gelisah. Ada masalah?"),
                new PartDef("Assets/Sprites/Characters/dimas/Idle/rotations/south.png", "Dimas", "Lif... kebetulan banget kamu lewat. Aku perlu pendapatmu, tapi nggak enak ngomong di luar."),
            }),
            new SceneDef(new[]
            {
                new PartDef("Assets/Sprites/Backgrounds/KosKosan_Lantai1.png", "Dimas", "Masuk ke kamarku sebentar, ya. Aku sedang butuh Rp2.000.000 dan hampir mengajukan pinjaman online."),
                new PartDef("Assets/Sprites/Backgrounds/KosKosan_Interior.png", "Alif", "Baik. Jangan tekan tombol pengajuan dulu. Kita baca syaratnya dan cari jalan keluar yang aman bersama-sama."),
            }),
        };

        [MenuItem("Alif/5) Build Chapter 1 Cutscene Scene")]
        public static void BuildCutsceneScene()
        {
            BuildAndSaveCutscene(TargetScenePath, Scenes, ArtFolder, "SampleScene", 0, false);

            Debug.Log("[Alif] Chapter 1 Cutscene scene selesai dibangun & disimpan.");
        }

        [MenuItem("Alif/7) Build Chapter 1 Ending + Chapter 2 Intro")]
        public static void BuildChapterTransitions()
        {
            BuildAndSaveCutscene(Chapter1EndingScenePath, Chapter1EndingScenes, string.Empty, "ChapterSelect", 1, true);
            BuildAndSaveCutscene(Chapter2CutsceneScenePath, Chapter2IntroScenes, string.Empty, "Chapter2Gameplay", 0, false);
            Debug.Log("[Alif] Chapter 1 Ending dan Chapter 2 Intro selesai dibangun & disimpan.");
        }

        /// <summary>Entry point buat CLI (-executeMethod) — pola sama seperti builder lain.</summary>
        public static void BuildCutsceneSceneCLI()
        {
            BuildCutsceneScene();
        }

        public static void BuildAllChapterCutscenesCLI()
        {
            BuildCutsceneScene();
            BuildChapterTransitions();
        }

        private static void BuildAndSaveCutscene(string targetScenePath, SceneDef[] sceneDefinitions, string artFolder,
            string nextSceneName, int completedChapterNumber, bool unlockNextChapter)
        {
            OpenOrCreateTargetScene(targetScenePath);
            BuildCutsceneInActiveScene(sceneDefinitions, artFolder, nextSceneName, completedChapterNumber, unlockNextChapter);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            RegisterInBuildSettings(targetScenePath);
        }

        private static void OpenOrCreateTargetScene(string targetScenePath)
        {
            if (File.Exists(targetScenePath))
            {
                EditorSceneManager.OpenScene(targetScenePath);
                return;
            }

            AlifDemoSceneBuilder.EnsureFolder("Assets/Scenes");
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(newScene, targetScenePath);
        }

        private static void BuildCutsceneInActiveScene(SceneDef[] sceneDefinitions, string artFolder, string nextSceneName,
            int completedChapterNumber, bool unlockNextChapter)
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
            (GameObject introTextRoot, TextMeshProUGUI introText) = BuildIntroTextPanel(canvasGO.transform);
            Button nextButton = BuildNextButton(canvasGO.transform);
            Button skipButton = BuildSkipButton(canvasGO.transform);

            CutscenePlayerController controller = AlifDemoSceneBuilder.GetOrAddComponent<CutscenePlayerController>(canvasGO);
            AlifDemoSceneBuilder.SetSerializedObjectList(controller, "_slotImages", new List<Object>(slotImages));
            AlifDemoSceneBuilder.SetSerializedObjectList(controller, "_slotRects", new List<Object>(slotRects));
            AlifDemoSceneBuilder.SetSerializedObjectList(controller, "_slotCanvasGroups", new List<Object>(slotGroups));
            AlifDemoSceneBuilder.SetSerializedRef(controller, "_dialogueBox", dialogueBox);
            AlifDemoSceneBuilder.SetSerializedRef(controller, "_speakerNameText", speakerNameText);
            AlifDemoSceneBuilder.SetSerializedRef(controller, "_dialogueText", dialogueText);
            AlifDemoSceneBuilder.SetSerializedRef(controller, "_introTextRoot", introTextRoot);
            AlifDemoSceneBuilder.SetSerializedRef(controller, "_introText", introText);
            SetControllerSettings(controller, nextSceneName, completedChapterNumber, unlockNextChapter);
            SetScenesArray(controller, sceneDefinitions, artFolder);

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

        // "Title card" prolog — layar hitam polos + teks di tengah, kayak intro RPG klasik.
        // Backdrop (hitam, fullscreen) sudah ada dari BuildBackdrop, jadi di sini cuma perlu
        // teksnya. Nonaktif secara default; CutscenePlayerController yang nyalain kalau scene
        // aktifnya "text-only" (Parts kosong).
        private static (GameObject, TextMeshProUGUI) BuildIntroTextPanel(Transform canvasTransform)
        {
            GameObject root = AlifDemoSceneBuilder.FindOrCreateChild(canvasTransform, "IntroTextPanel");
            RectTransform rootRT = root.GetComponent<RectTransform>();
            AlifDemoSceneBuilder.SetRect(rootRT, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 400));

            TextMeshProUGUI introText = AlifDemoSceneBuilder.FindOrCreateText(root.transform, "IntroText", "",
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, 22, TextAlignmentOptions.Center);
            introText.fontStyle = FontStyles.Italic;
            introText.color = new Color(1f, 1f, 1f, 0.9f);
            introText.lineSpacing = 12f;

            root.SetActive(false);
            return (root, introText);
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

        private static void SetControllerSettings(CutscenePlayerController controller, string nextSceneName,
            int completedChapterNumber, bool unlockNextChapter)
        {
            var so = new SerializedObject(controller);
            so.FindProperty("_nextSceneName").stringValue = nextSceneName;
            so.FindProperty("_completedChapterNumber").intValue = completedChapterNumber;
            so.FindProperty("_unlockNextChapterOnFinish").boolValue = unlockNextChapter;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetScenesArray(CutscenePlayerController controller, SceneDef[] sceneDefinitions, string artFolder)
        {
            var so = new SerializedObject(controller);
            SerializedProperty scenesProp = so.FindProperty("_scenes");
            scenesProp.arraySize = sceneDefinitions.Length;

            for (int s = 0; s < sceneDefinitions.Length; s++)
            {
                PartDef[] parts = sceneDefinitions[s].Parts;
                SerializedProperty sceneProp = scenesProp.GetArrayElementAtIndex(s);
                sceneProp.FindPropertyRelative("IntroText").stringValue = sceneDefinitions[s].IntroText;

                SerializedProperty partsProp = sceneProp.FindPropertyRelative("Parts");
                partsProp.arraySize = parts.Length;

                for (int p = 0; p < parts.Length; p++)
                {
                    PartDef def = parts[p];
                    string spritePath = def.File.StartsWith("Assets/") ? def.File : $"{artFolder}/{def.File}";
                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                    if (sprite == null)
                    {
                        Debug.LogWarning($"[Alif] Cutscene sprite tidak ditemukan: '{spritePath}'.");
                    }

                    SerializedProperty partProp = partsProp.GetArrayElementAtIndex(p);
                    partProp.FindPropertyRelative("Image").objectReferenceValue = sprite;
                    partProp.FindPropertyRelative("Crop").rectValue = OpaqueCrop(spritePath);
                    partProp.FindPropertyRelative("SpeakerName").stringValue = def.SpeakerName;
                    partProp.FindPropertyRelative("Line").stringValue = def.Line;
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Panel komik Chapter 1 berbentuk miring dengan sudut transparan. Cutscene menampilkan
        /// gambar layar penuh, jadi ambil persegi opaque terbesar (dinormalisasi 0-1, origin
        /// kiri-bawah seperti Sprite.rect). Gambar tanpa transparansi = Rect kosong (utuh).
        /// </summary>
        private static Rect OpaqueCrop(string assetPath)
        {
            if (!File.Exists(assetPath)) return default;
            var texture = new Texture2D(2, 2);
            if (!texture.LoadImage(File.ReadAllBytes(assetPath)))
            {
                Object.DestroyImmediate(texture);
                return default;
            }

            int width = texture.width, height = texture.height;
            Color32[] pixels = texture.GetPixels32();
            Object.DestroyImmediate(texture);

            // Tepi opaque kiri/kanan per baris (panelnya cembung, jadi satu rentang per baris).
            var left = new int[height];
            var right = new int[height];
            bool fullyOpaque = true;
            for (int y = 0; y < height; y++)
            {
                int l = 0, r = width - 1;
                while (l < width && pixels[y * width + l].a < 250) l++;
                while (r >= 0 && pixels[y * width + r].a < 250) r--;
                left[y] = l; right[y] = r;
                fullyOpaque &= l == 0 && r == width - 1;
            }
            if (fullyOpaque) return default;

            long bestArea = 0;
            int bestX0 = 0, bestY0 = 0, bestX1 = 0, bestY1 = 0;
            for (int y0 = 0; y0 < height; y0 += 2)
            {
                int maxLeft = left[y0], minRight = right[y0];
                for (int y1 = y0; y1 < height; y1 += 2)
                {
                    maxLeft = Mathf.Max(maxLeft, left[y1]);
                    minRight = Mathf.Min(minRight, right[y1]);
                    if (minRight <= maxLeft) break;
                    long area = (long)(minRight - maxLeft + 1) * (y1 - y0 + 1);
                    if (area > bestArea)
                    {
                        bestArea = area;
                        bestX0 = maxLeft; bestX1 = minRight; bestY0 = y0; bestY1 = y1;
                    }
                }
            }
            if (bestArea == 0) return default;

            return new Rect(bestX0 / (float)width, bestY0 / (float)height,
                (bestX1 - bestX0 + 1) / (float)width, (bestY1 - bestY0 + 1) / (float)height);
        }

        private static void RegisterInBuildSettings(string targetScenePath)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == targetScenePath);

            // Cutscene disisipkan sebelum ChapterSelect/SampleScene agar semua scene transisi
            // selalu ikut build, tanpa bergantung pada urutan menu builder dijalankan.
            int insertIndex = scenes.FindIndex(s => s.path == "Assets/Scenes/ChapterSelect.unity");
            insertIndex = insertIndex >= 0 ? insertIndex : scenes.Count;
            scenes.Insert(insertIndex, new EditorBuildSettingsScene(targetScenePath, true));

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
