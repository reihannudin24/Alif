using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;
using Alif.Core;
using Alif.Systems;
using Alif.Player;
using Alif.Characters;
using Alif.Dialogue;
using Alif.UI;
using Alif.World;

namespace Alif.EditorTools
{
    /// <summary>
    /// Tool editor sekali-klik untuk membangun scene demo secara otomatis: Manager, Player,
    /// 5 NPC (karakter asli dari Assets/Sprites/Characters), dan Canvas HUD/Inventory/Dialogue.
    ///
    /// Cara pakai: jalankan menu "Alif > 1) Import Character Art" dulu (sekali saja, atau tiap
    /// nambah karakter baru), baru menu "Alif > 2) Build Demo Scene" di scene aktif (misal SampleScene).
    /// Tool ini aman dijalankan berulang kali — tidak akan membuat duplikat GameObject.
    /// </summary>
    public static class AlifDemoSceneBuilder
    {
        private const string CharacterDataFolder = "Assets/ScriptableObjects/Characters";
        private const string DialogueDataFolder = "Assets/ScriptableObjects/Dialogue";
        private const string ChoicePrefabPath = "Assets/Prefabs/UI/ChoiceButton.prefab";
        private const string InputActionsPath = "Assets/Settings/InputSystem_Actions.inputactions";

        private const string PlayerSpriteFolder = "Assets/Sprites/Characters/alif";
        private const string PlayerControllerPath = "Assets/Animations/Player/Alif_Player.controller";
        private const string BackgroundSpritePath = "Assets/Sprites/Backgrounds/Stasiun_Interior.png";
        private const string TargetScenePath = "Assets/Scenes/SampleScene.unity";

        // Sorting layer custom (Background/Ground/Characters/UI) ternyata nggak reliable
        // di-resolve lewat -executeMethod (batch mode) — baik by-name maupun by-ID selalu
        // balik ke 0, bikin karakter ke-assign ke layer yang sama dengan background dan
        // ketutup (kelihatan "hilang"). Solusi yang jauh lebih aman: SEMUA sprite dunia tetap
        // di layer "Default" bawaan Unity, urutan gambar diatur murni pakai sortingOrder
        // (integer biasa, nggak ada lookup nama/ID sama sekali).
        private const int SortingOrderGround = 0;
        private const int SortingOrderCharacters = 10;

        // Sama kasusnya dengan sorting layer: LayerMask.NameToLayer("Interactable") juga
        // nggak reliable di-resolve lewat -executeMethod (batch mode) — balik ke -1/gagal,
        // bikin NPC/bangku ke-assign ke layer Default dan nggak pernah kedeteksi
        // PlayerController (OverlapCircle dengan _interactableLayer jadi kosong). Index 7
        // ini persis sama dengan urutan "Interactable" di ProjectSettings/TagManager.asset.
        private const int LayerIndexInteractable = 7;

        /// <summary>
        /// Entry point terpisah, khusus buat import TMP Essential Resources doang lewat CLI —
        /// TIDAK digabung ke RunFullPipelineCLI supaya nggak nabrak proses build scene lain
        /// dalam satu batch yang sama (itu yang bikin masalah "missing script" sebelumnya).
        /// Jalankan sendirian: -executeMethod Alif.EditorTools.AlifDemoSceneBuilder.ImportTmpEssentialsCLI
        /// </summary>
        public static void ImportTmpEssentialsCLI()
        {
            const string settingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
            if (AssetDatabase.LoadAssetAtPath<Object>(settingsPath) != null)
            {
                Debug.Log("[Alif] TMP Essentials sudah ada, tidak perlu import ulang.");
                return;
            }

            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string packageCacheDir = Path.Combine(projectRoot, "Library", "PackageCache");
            string[] uguiDirs = Directory.Exists(packageCacheDir)
                ? Directory.GetDirectories(packageCacheDir, "com.unity.ugui@*")
                : new string[0];

            if (uguiDirs.Length == 0)
            {
                Debug.LogError("[Alif] Package com.unity.ugui tidak ditemukan.");
                return;
            }

            string packagePath = Path.Combine(uguiDirs[0], "Package Resources", "TMP Essential Resources.unitypackage");
            if (!File.Exists(packagePath))
            {
                Debug.LogError($"[Alif] '{packagePath}' tidak ditemukan.");
                return;
            }

            AssetDatabase.ImportPackage(packagePath, false);
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();

            bool success = AssetDatabase.LoadAssetAtPath<Object>(settingsPath) != null;
            Debug.Log(success
                ? "[Alif] TMP Essential Resources berhasil diimport."
                : "[Alif] Import selesai dijalankan, tapi TMP Settings.asset masih belum ketemu — kemungkinan tetap gagal.");
        }

        /// <summary>
        /// Entry point buat dipanggil dari command line (-executeMethod), bukan dari menu Editor.
        /// Buka scene target, jalankan import character art + build demo scene, lalu save.
        /// </summary>
        public static void RunFullPipelineCLI()
        {
            // TMP Essential Resources sengaja TIDAK diimport otomatis dari sini —
            // AssetDatabase.ImportPackage() lewat -executeMethod (batch mode) terbukti nggak
            // reliable (importnya nggak kepakai, dan malah bikin referensi script lain di scene
            // rusak jadi "missing script"). Import manual sekali lewat Window > TextMeshPro >
            // Import TMP Essential Resources di Editor interaktif — nggak perlu diulang tiap build.
            EditorSceneManager.OpenScene(TargetScenePath);
            AlifCharacterAnimationBuilder.ImportCharacterArt();
            BuildDemoScene();
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[Alif] RunFullPipelineCLI selesai.");
        }

        // GameObject bawaan scene yang BUKAN hasil generate tool ini — jangan pernah dihapus.
        private static readonly string[] PreservedRootNames = { "Main Camera", "Global Light 2D" };

        [MenuItem("Alif/2) Build Demo Scene")]
        public static void BuildDemoScene()
        {
            // 0. Bersihkan total semua GameObject hasil generate sebelumnya dulu. Setelah 10+ kali
            // rebuild dengan skrip yang terus berubah (rename field, ganti tipe komponen, dst),
            // pendekatan "reuse kalau sudah ada" mulai nyisain sampah (referensi script yang jadi
            // rusak/hilang, komponen basi). Rebuild bersih dari nol jauh lebih aman & predictable.
            CleanGeneratedObjects();

            // 1. EventSystem (dibutuhkan supaya tombol UI bisa diklik lewat Input System baru).
            EnsureEventSystem();

            // 2. Background interior (kalau sudah ada file-nya di Assets/Sprites/Backgrounds/).
            BuildBackground();

            // 3. Persistent managers.
            GameObject managersRoot = FindOrCreateRoot("_GameManagers");
            GetOrAddChild<GameManager>(managersRoot.transform, "GameManager");
            GetOrAddChild<SceneLoader>(managersRoot.transform, "SceneLoader");
            GetOrAddChild<TimeSystem>(managersRoot.transform, "TimeSystem");
            GetOrAddChild<EnergySystem>(managersRoot.transform, "EnergySystem");
            GetOrAddChild<CurrencySystem>(managersRoot.transform, "CurrencySystem");
            GetOrAddChild<InventorySystem>(managersRoot.transform, "InventorySystem");
            DialogueManager dialogueManager = GetOrAddChild<DialogueManager>(managersRoot.transform, "DialogueManager");

            // 4. Player.
            PlayerController playerController = BuildPlayer();
            SetupCameraFollow(playerController.transform);

            // 5. NPC (pakai karakter asli dari Assets/Sprites/Characters, hasil "Alif > 1) Import Character Art").
            BuildNpcs();

            // 5b. Bangku/kursi yang bisa di-interact (nggak bisa ditembus + munculin monolog Alif).
            BuildBenches();

            // Hubungkan DialogueManager ke PlayerController (supaya movement terkunci saat dialog).
            SetSerializedRef(dialogueManager, "_playerController", playerController);

            // 6. Canvas UI: HUD, Inventory bar, Dialogue box.
            BuildCanvas(playerController);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            Debug.Log("[Alif] Demo scene selesai dibangun. Simpan scene (Cmd/Ctrl+S) lalu tekan Play.");
        }

        private static void CleanGeneratedObjects()
        {
            var scene = SceneManager.GetActiveScene();
            GameObject[] roots = scene.GetRootGameObjects();

            foreach (GameObject root in roots)
            {
                if (System.Array.IndexOf(PreservedRootNames, root.name) < 0)
                {
                    Object.DestroyImmediate(root);
                }
            }
        }

        // ---------------------------------------------------------------
        // BACKGROUND
        // ---------------------------------------------------------------
        private static void BuildBackground()
        {
            Sprite backgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundSpritePath);
            if (backgroundSprite == null)
            {
                Debug.LogWarning($"[Alif] Background sprite tidak ditemukan di '{BackgroundSpritePath}', dilewati.");
                return;
            }

            GameObject background = FindOrCreateRoot("Background");
            background.transform.position = Vector3.zero;

            SpriteRenderer sr = GetOrAddComponent<SpriteRenderer>(background);
            sr.sprite = backgroundSprite;
            sr.sortingOrder = SortingOrderGround;

            BuildBackgroundColliders(background);
        }

        // Area kosong/hitam + area "tembok belakang" (loker/meja/wastafel yang nempel dinding
        // atas & bawah gedung) di gambar Stasiun_Interior.png — diblok supaya Player nggak bisa
        // jalan ke situ. Dideteksi dari analisis pixel gambar aslinya (2222x1888px, PPU 200,
        // pivot Center). Tembok VERTIKAL antar ruangan (misal Informasi<->Toilet) belum ada,
        // butuh analisis lebih presisi — ini baru nutup kasus jalan ke void luar & nyangkut di
        // tembok atas/bawah tiap ruangan.
        private static readonly (Vector2 center, Vector2 size)[] BackgroundVoidBlockers =
        {
            (new Vector2(3.925f, 2.595f), new Vector2(3.26f, 4.25f)),      // void kanan-atas (sebelah Toilet)
            (new Vector2(3.4675f, -4.1375f), new Vector2(4.175f, 1.165f)), // void kanan-bawah
            (new Vector2(-3.495f, -4.1375f), new Vector2(4.12f, 1.165f)),  // void kiri-bawah
            (new Vector2(-1.605f, 3.52f), new Vector2(7.9f, 2.4f)),        // tembok belakang ruangan atas (Informasi/Toilet)
            (new Vector2(-3.1775f, -0.88f), new Vector2(4.755f, 1.2f)),    // tembok belakang Loket Karcis (kiri dari celah koridor)
            (new Vector2(3.3275f, -0.88f), new Vector2(4.455f, 1.2f)),     // tembok belakang Display (kanan dari celah koridor)

            // Sudut dinding kecil di tiap pintu masuk ruangan (tempat dinding "belok" ke arah
            // koridor tengah) — celah ini belum ke-cover blocker manapun di atas.
            (new Vector2(-1.03f, 0.6825f), new Vector2(0.6f, 0.575f)),     // sudut Informasi (menghadap koridor)
            (new Vector2(1.32f, 0.6825f), new Vector2(0.6f, 0.575f)),      // sudut Toilet (menghadap koridor)
            (new Vector2(-1.03f, -0.6825f), new Vector2(0.6f, 0.575f)),    // sudut Loket Karcis (menghadap koridor)
            (new Vector2(1.32f, -0.6825f), new Vector2(0.6f, 0.575f)),     // sudut Display (menghadap koridor)

            // Toilet punya panel dinding/counter di sisi kanan yang lebih "turun" ke bawah
            // dibanding ruangan Informasi — bagian ini sebelumnya nggak ke-cover sama sekali,
            // Player bisa berdiri di atasnya kayak lantai padahal itu tembok.
            (new Vector2(1.56f, 2.195f), new Vector2(1.32f, 1.15f)),       // panel dinding kanan Toilet (disempitkan, dulu nutup gate Informasi<->Toilet)
        };

        private static void BuildBackgroundColliders(GameObject background)
        {
            // Bersihkan collider lama dulu biar aman di-rebuild berulang kali.
            for (int i = background.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = background.transform.GetChild(i);
                if (child.name.StartsWith("VoidBlocker") || child.name.StartsWith("BoundaryWall"))
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }

            for (int i = 0; i < BackgroundVoidBlockers.Length; i++)
            {
                (Vector2 center, Vector2 size) = BackgroundVoidBlockers[i];
                AddBoxBlocker(background, $"VoidBlocker_{i}", center, size);
            }

            // Boundary luar: 4 dinding tipis di tepi gambar, biar Player nggak bisa keluar
            // dari keseluruhan gambar sama sekali (bukan cuma void di dalamnya).
            const float halfW = 5.555f;
            const float halfH = 4.72f;
            const float wallThickness = 0.2f;

            AddBoxBlocker(background, "BoundaryWall_Top", new Vector2(0, halfH + wallThickness / 2f), new Vector2(halfW * 2 + wallThickness * 2, wallThickness));
            AddBoxBlocker(background, "BoundaryWall_Bottom", new Vector2(0, -halfH - wallThickness / 2f), new Vector2(halfW * 2 + wallThickness * 2, wallThickness));
            AddBoxBlocker(background, "BoundaryWall_Left", new Vector2(-halfW - wallThickness / 2f, 0), new Vector2(wallThickness, halfH * 2 + wallThickness * 2));
            AddBoxBlocker(background, "BoundaryWall_Right", new Vector2(halfW + wallThickness / 2f, 0), new Vector2(wallThickness, halfH * 2 + wallThickness * 2));
        }

        private static void AddBoxBlocker(GameObject parent, string name, Vector2 localPosition, Vector2 size)
        {
            var blocker = new GameObject(name);
            blocker.transform.SetParent(parent.transform, false);
            blocker.transform.localPosition = localPosition;

            BoxCollider2D collider = blocker.AddComponent<BoxCollider2D>();
            collider.size = size;
        }

        // ---------------------------------------------------------------
        // PLAYER
        // ---------------------------------------------------------------
        private static void SetupCameraFollow(Transform target)
        {
            GameObject cameraGO = GameObject.Find("Main Camera");
            if (cameraGO == null)
            {
                Debug.LogWarning("[Alif] GameObject 'Main Camera' tidak ditemukan, CameraFollow tidak dipasang.");
                return;
            }

            CameraFollow follow = GetOrAddComponent<CameraFollow>(cameraGO);
            SetSerializedRef(follow, "_target", target);

            // Pertahankan Z kamera yang sudah ada (biasanya -10 di project 2D), cuma X/Y yang ngikutin.
            float cameraZ = cameraGO.transform.position.z;
            SetSerializedValue(follow, "_offset", new Vector3(0f, 0f, cameraZ));

            // Langsung posisikan ke Player sekarang, biar nggak nunggu Play buat nyusul ke tengah.
            cameraGO.transform.position = new Vector3(target.position.x, target.position.y, cameraZ);

            // Zoom in (default orthographic size 5 bikin peta kelihatan kecil di tengah layar
            // kebanyakan area biru kosong) + background di luar peta jadi hitam, bukan biru.
            Camera cam = cameraGO.GetComponent<Camera>();
            if (cam != null)
            {
                cam.orthographicSize = 3.2f;
                cam.backgroundColor = Color.black;
            }
        }

        private static PlayerController BuildPlayer()
        {
            GameObject player = FindOrCreateRoot("Player");
            player.tag = "Player";
            player.transform.position = Vector3.zero;

            Rigidbody2D rb = GetOrAddComponent<Rigidbody2D>(player);
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            CapsuleCollider2D collider = GetOrAddComponent<CapsuleCollider2D>(player);
            collider.size = new Vector2(0.7f, 0.9f);

            SpriteRenderer sr = GetOrAddComponent<SpriteRenderer>(player);
            Sprite southIdle = AssetDatabase.LoadAssetAtPath<Sprite>($"{PlayerSpriteFolder}/Idle/rotations/south.png");
            sr.sprite = southIdle; // null kalau belum jalanin "Alif > 1) Import Character Art" — aman, cuma kosong.
            sr.sortingOrder = SortingOrderCharacters;

            Animator animator = GetOrAddComponent<Animator>(player);
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PlayerControllerPath);

            GetOrAddComponent<PlayerAnimation>(player);

            // PlayerInput (+ "Send Messages") pernah dipakai di sini, tapi ternyata rapuh
            // (bisa lempar MissingMethodException walau method target ada). PlayerController
            // sekarang subscribe langsung ke InputAction, jadi PlayerInput tidak diperlukan —
            // hapus kalau ada sisa dari versi sebelumnya.
            PlayerInput existingPlayerInput = player.GetComponent<PlayerInput>();
            if (existingPlayerInput != null)
            {
                Object.DestroyImmediate(existingPlayerInput);
            }

            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (actions == null)
            {
                Debug.LogWarning($"[Alif] Tidak menemukan Input Actions asset di '{InputActionsPath}'.");
            }

            PlayerController controller = GetOrAddComponent<PlayerController>(player);
            SetSerializedValue(controller, "_moveSpeed", 4f);
            SetSerializedValue(controller, "_interactRadius", 1.2f);
            SetSerializedValue(controller, "_interactableLayer", (LayerMask)(1 << LayerIndexInteractable));
            SetSerializedRef(controller, "_playerAnimation", player.GetComponent<PlayerAnimation>());
            SetSerializedRef(controller, "_inputActions", actions);

            return controller;
        }

        // ---------------------------------------------------------------
        // NPC (karakter asli, hasil menu "Alif > 1) Import Character Art")
        // ---------------------------------------------------------------
        private static readonly Vector2[] NpcPositions =
        {
            new Vector2(2f, 1f),
            new Vector2(-3.2f, 1.3f),   // naya — digeser menjauh dari pintu koridor Informasi
            new Vector2(2f, -1.5f),
            new Vector2(-2f, -1.5f),
            new Vector2(0f, 2.5f),
        };

        // NPC yang benar-benar dimunculkan di scene. Yang lain tetap punya CharacterData/Animator
        // (dari "Alif > 1) Import Character Art"), tinggal ditambah ke sini kapan pun mau dipakai.
        private static readonly string[] ActiveNpcFolders = { "naya" };

        private static void BuildNpcs()
        {
            var npcCharacters = AlifCharacterAnimationBuilder.NpcCharacters;
            for (int i = 0; i < npcCharacters.Length; i++)
            {
                var entry = npcCharacters[i];

                if (System.Array.IndexOf(ActiveNpcFolders, entry.folder) < 0)
                {
                    RemoveNpcIfExists(entry.folder);
                    continue;
                }

                Vector2 position = NpcPositions[i % NpcPositions.Length];
                BuildNpc(entry.folder, entry.displayName, position);
            }
        }

        private static void RemoveNpcIfExists(string folder)
        {
            var scene = SceneManager.GetActiveScene();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == $"NPC_{folder}")
                {
                    Undo.DestroyObjectImmediate(root);
                    return;
                }
            }
        }

        private static void BuildNpc(string folder, string displayName, Vector2 position)
        {
            string dataPath = $"{CharacterDataFolder}/CharacterData_{folder}.asset";
            CharacterData characterData = AssetDatabase.LoadAssetAtPath<CharacterData>(dataPath);
            if (characterData == null)
            {
                Debug.LogWarning($"[Alif] CharacterData untuk '{folder}' belum ada. " +
                                  "Jalankan menu 'Alif > 1) Import Character Art' dulu.");
                return;
            }

            DialogueData dialogueData = GetOrCreateDialogueData(folder, displayName);

            GameObject npc = FindOrCreateRoot($"NPC_{folder}");
            npc.transform.position = new Vector3(position.x, position.y, 0f);
            npc.layer = LayerIndexInteractable;

            SpriteRenderer sr = GetOrAddComponent<SpriteRenderer>(npc);
            sr.sprite = characterData.Portrait;
            sr.sortingOrder = SortingOrderCharacters;

            CircleCollider2D collider = GetOrAddComponent<CircleCollider2D>(npc);
            collider.isTrigger = true;
            collider.radius = 0.5f;

            Animator animator = GetOrAddComponent<Animator>(npc);
            animator.runtimeAnimatorController = characterData.AnimatorController;

            NPCController npcController = GetOrAddComponent<NPCController>(npc);
            SetSerializedRef(npcController, "_characterData", characterData);
            SetSerializedRef(npcController, "_defaultDialogue", dialogueData);
            SetSerializedRef(npcController, "_spriteRenderer", sr);

            AddInteractionArrow(npc.transform, new Vector2(0f, 1.0f));
        }

        private static DialogueData GetOrCreateDialogueData(string folder, string displayName)
        {
            string path = $"{DialogueDataFolder}/DialogueData_{folder}.asset";
            DialogueData data = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
            if (data != null)
            {
                return data;
            }

            data = ScriptableObject.CreateInstance<DialogueData>();
            data.Lines.Add(new DialogueLine
            {
                SpeakerName = displayName,
                Text = $"Halo! Aku {displayName}."
            });
            data.Lines.Add(new DialogueLine
            {
                SpeakerName = displayName,
                Text = "Semoga harimu menyenangkan!"
            });

            EnsureFolder(DialogueDataFolder);
            AssetDatabase.CreateAsset(data, path);
            return data;
        }

        // ---------------------------------------------------------------
        // BANGKU (interactable, posisi dideteksi dari analisis pixel Stasiun_Interior.png)
        // ---------------------------------------------------------------
        private static readonly (Vector2 center, Vector2 size)[] BenchPlacements =
        {
            (new Vector2(-5.2175f, 2.345f), new Vector2(0.475f, 0.65f)),   // Informasi, bangku pendek atas
            (new Vector2(-5.2175f, 1.32f), new Vector2(0.475f, 0.65f)),    // Informasi, bangku pendek bawah
            (new Vector2(-3.33f, 0.9325f), new Vector2(2.35f, 0.325f)),    // Informasi, bangku panjang
            (new Vector2(-5.1675f, -2.5675f), new Vector2(0.575f, 1.175f)), // Loket Karcis
            (new Vector2(2.82f, -1.4175f), new Vector2(1.0f, 0.575f)),     // Display, bangku kiri
            (new Vector2(3.87f, -1.4175f), new Vector2(1.0f, 0.575f)),     // Display, bangku kanan
        };

        private static void BuildBenches()
        {
            GameObject background = FindOrCreateRoot("Background");
            DialogueData benchDialogue = GetOrCreateBenchDialogue();
            CharacterData alifData = GetOrCreateAlifCharacterData();

            for (int i = 0; i < BenchPlacements.Length; i++)
            {
                (Vector2 center, Vector2 size) = BenchPlacements[i];

                GameObject bench = FindOrCreateWorldChild(background.transform, $"Bench_{i}");
                bench.transform.localPosition = center;
                bench.layer = LayerIndexInteractable;

                BoxCollider2D collider = GetOrAddComponent<BoxCollider2D>(bench);
                collider.size = size;

                InteractableObject interactable = GetOrAddComponent<InteractableObject>(bench);
                SetSerializedRef(interactable, "_dialogue", benchDialogue);
                SetSerializedRef(interactable, "_speakerData", alifData);

                AddInteractionArrow(bench.transform, new Vector2(0f, size.y / 2f + 0.25f));
            }
        }

        // Portrait Alif (ilustrasi, bukan sprite pixel-art in-game) buat ditampilkan di dialogue
        // box saat monolog (misal interact bangku). Ditaruh di Sprites/UI (bukan Sprites/Characters)
        // supaya nggak kena postprocessor pixel-art (Point filter dkk) — ini butuh Bilinear
        // karena gambarnya ilustrasi halus.
        private static CharacterData GetOrCreateAlifCharacterData()
        {
            const string portraitPath = "Assets/Sprites/UI/Portrait_Alif.png";

            // Reimport cuma kalau settingnya emang belum benar — dulu ini selalu reimport tiap
            // rebuild, dan ternyata reimport yang nyelip di TENGAH proses assign referensi lain
            // bikin serialisasi field lain (_speakerData di InteractableObject) rusak/kosong.
            var importer = (TextureImporter)AssetImporter.GetAtPath(portraitPath);
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.filterMode = FilterMode.Bilinear;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
                AssetDatabase.Refresh();
            }

            string dataPath = $"{CharacterDataFolder}/CharacterData_Alif.asset";
            CharacterData data = AssetDatabase.LoadAssetAtPath<CharacterData>(dataPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<CharacterData>();
                EnsureFolder(CharacterDataFolder);
                AssetDatabase.CreateAsset(data, dataPath);
            }

            data.CharacterName = "Alif";
            data.Description = "Pemain utama.";
            data.Portrait = AssetDatabase.LoadAssetAtPath<Sprite>(portraitPath);
            EditorUtility.SetDirty(data);

            // Pastikan asset ini benar-benar tersimpan & ke-registrasi utuh SEBELUM dipakai buat
            // ngisi field referensi di komponen lain (itu yang kemarin bikin _speakerData kosong).
            AssetDatabase.SaveAssets();

            return data;
        }

        // Panah putih mengambang di atas NPC/benda interactable, muncul otomatis saat Player
        // ada dalam jangkauan interact (dikontrol dari PlayerController.UpdateInteractionPrompt).
        private static void AddInteractionArrow(Transform parent, Vector2 localOffset)
        {
            GameObject arrow = FindOrCreateWorldChild(parent, "InteractionArrow");
            arrow.transform.localPosition = new Vector3(localOffset.x, localOffset.y, 0f);

            SpriteRenderer sr = GetOrAddComponent<SpriteRenderer>(arrow);
            sr.sprite = GetOrCreateTriangleSprite("InteractionArrow", new Color(1f, 1f, 1f, 0.95f), 64);
            sr.sortingOrder = SortingOrderCharacters + 1;

            arrow.SetActive(false);
        }

        private static DialogueData GetOrCreateBenchDialogue()
        {
            string path = $"{DialogueDataFolder}/DialogueData_Bench.asset";
            DialogueData data = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
            if (data != null)
            {
                return data;
            }

            data = ScriptableObject.CreateInstance<DialogueData>();
            data.Lines.Add(new DialogueLine
            {
                SpeakerName = "Alif",
                Text = "Duduk kayaknya enak nih, tapi aku harus segera cari makan."
            });

            EnsureFolder(DialogueDataFolder);
            AssetDatabase.CreateAsset(data, path);
            return data;
        }

        // ---------------------------------------------------------------
        // CANVAS: HUD, INVENTORY, DIALOGUE
        // ---------------------------------------------------------------
        private static void BuildCanvas(PlayerController playerController)
        {
            GameObject canvasGO = FindOrCreateUIRoot("Canvas");

            Canvas canvas = GetOrAddComponent<Canvas>(canvasGO);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = GetOrAddComponent<CanvasScaler>(canvasGO);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;

            GetOrAddComponent<GraphicRaycaster>(canvasGO);
            UIManager uiManager = GetOrAddComponent<UIManager>(canvasGO);

            GameObject hudPanel = BuildHUD(canvasGO.transform);
            GameObject inventoryPanel = BuildInventory(canvasGO.transform);
            GameObject dialoguePanel = BuildDialoguePanel(canvasGO.transform);

            // DialogueUI diletakkan di root Canvas (selalu aktif) supaya tetap subscribe ke event
            // DialogueManager walaupun panel visualnya (dialoguePanel) sedang disembunyikan.
            DialogueUI dialogueUI = GetOrAddComponent<DialogueUI>(canvasGO);
            SetSerializedRef(dialogueUI, "_dialogueBoxRoot", dialoguePanel);
            SetSerializedRef(dialogueUI, "_speakerNameText", dialoguePanel.transform.Find("SpeakerNameText")?.GetComponent<TMP_Text>());
            SetSerializedRef(dialogueUI, "_dialogueText", dialoguePanel.transform.Find("DialogueText")?.GetComponent<TMP_Text>());
            SetSerializedRef(dialogueUI, "_portraitImage", dialoguePanel.transform.Find("PortraitImage")?.GetComponent<Image>());
            SetSerializedRef(dialogueUI, "_nextButton", dialoguePanel.transform.Find("NextButton")?.GetComponent<Button>());
            SetSerializedRef(dialogueUI, "_choiceButtonContainer", dialoguePanel.transform.Find("ChoiceButtonContainer"));
            SetSerializedRef(dialogueUI, "_choiceButtonPrefab", GetOrCreateChoiceButtonPrefab());

            SetSerializedRef(uiManager, "_hudPanel", hudPanel);
            SetSerializedRef(uiManager, "_inventoryPanel", inventoryPanel);
            SetSerializedRef(uiManager, "_dialoguePanel", dialoguePanel);

            dialoguePanel.SetActive(false);

            BuildTouchControls(canvasGO.transform, playerController);
        }

        // ---------------------------------------------------------------
        // TOUCH CONTROLS: joystick on-screen + tombol interact
        // ---------------------------------------------------------------
        private static void BuildTouchControls(Transform canvasTransform, PlayerController playerController)
        {
            Sprite bgSprite = GetOrCreateCircleSprite("Joystick_Background", new Color(1f, 1f, 1f, 0.25f), 128);
            Sprite knobSprite = GetOrCreateCircleSprite("Joystick_Knob", new Color(1f, 1f, 1f, 0.55f), 64);
            Sprite interactSprite = GetOrCreateCircleSprite("Interact_Button", new Color(0.85f, 0.62f, 0.18f, 0.9f), 96);

            // --- Joystick, pojok kiri bawah ---
            GameObject joystickGO = FindOrCreateChild(canvasTransform, "VirtualJoystick");
            RectTransform joystickRT = joystickGO.GetComponent<RectTransform>();
            SetRect(joystickRT, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(110, 110), new Vector2(140, 140));
            AddImage(joystickRT, Color.white).sprite = bgSprite;

            GameObject knobGO = FindOrCreateChild(joystickGO.transform, "Knob");
            RectTransform knobRT = knobGO.GetComponent<RectTransform>();
            SetRect(knobRT, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(64, 64));
            AddImage(knobRT, Color.white).sprite = knobSprite;

            VirtualJoystick joystick = GetOrAddComponent<VirtualJoystick>(joystickGO);
            SetSerializedRef(joystick, "_background", joystickRT);
            SetSerializedRef(joystick, "_knob", knobRT);

            SetSerializedRef(playerController, "_virtualJoystick", joystick);

            // --- Tombol Interact, pojok kanan bawah ---
            GameObject interactGO = FindOrCreateChild(canvasTransform, "InteractButton");
            RectTransform interactRT = interactGO.GetComponent<RectTransform>();
            SetRect(interactRT, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-90, 90), new Vector2(96, 96));
            AddImage(interactRT, Color.white).sprite = interactSprite;

            Button interactButton = GetOrAddComponent<Button>(interactGO);
            // Reset listener biar nggak dobel kalau tool ini dijalankan berulang kali.
            interactButton.onClick = new Button.ButtonClickedEvent();
            UnityEventTools.AddPersistentListener(interactButton.onClick, playerController.Interact);

            TextMeshProUGUI interactLabel = FindOrCreateText(interactGO.transform, "Label", "E",
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, 22, TextAlignmentOptions.Center);
            interactLabel.fontStyle = FontStyles.Bold;
        }

        private static GameObject BuildHUD(Transform canvasTransform)
        {
            GameObject panel = FindOrCreateChild(canvasTransform, "HUD_Panel");
            RectTransform panelRT = panel.GetComponent<RectTransform>() ?? panel.AddComponent<RectTransform>();
            SetRect(panelRT, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -20), new Vector2(260, 120));
            AddImage(panelRT, new Color(0.25f, 0.16f, 0.08f, 0.85f));

            TextMeshProUGUI dayWeekText = FindOrCreateText(panel.transform, "DayWeekText", "Tuesday, 1st week",
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(12, -10), new Vector2(-12, 24), 16, TextAlignmentOptions.Left);

            TextMeshProUGUI clockText = FindOrCreateText(panel.transform, "ClockText", "15:03",
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(12, -36), new Vector2(-12, 24), 16, TextAlignmentOptions.Left);

            Slider energySlider = BuildEnergySlider(panel.transform);

            TextMeshProUGUI moneyText = FindOrCreateText(panel.transform, "MoneyText", "150",
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(12, 10), new Vector2(-12, 24), 16, TextAlignmentOptions.Left);

            HUDController hud = GetOrAddComponent<HUDController>(panel);
            SetSerializedRef(hud, "_dayWeekText", dayWeekText);
            SetSerializedRef(hud, "_clockText", clockText);
            SetSerializedRef(hud, "_energySlider", energySlider);
            SetSerializedRef(hud, "_moneyText", moneyText);

            return panel;
        }

        private static Slider BuildEnergySlider(Transform parent)
        {
            GameObject sliderGO = FindOrCreateChild(parent, "EnergySlider");
            RectTransform sliderRT = sliderGO.GetComponent<RectTransform>() ?? sliderGO.AddComponent<RectTransform>();
            SetRect(sliderRT, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(12, -66), new Vector2(-24, 14));

            Slider slider = GetOrAddComponent<Slider>(sliderGO);
            slider.transition = Selectable.Transition.None;
            slider.interactable = false;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            slider.direction = Slider.Direction.LeftToRight;

            GameObject background = FindOrCreateChild(sliderGO.transform, "Background");
            RectTransform bgRT = background.GetComponent<RectTransform>() ?? background.AddComponent<RectTransform>();
            StretchFull(bgRT);
            AddImage(bgRT, new Color(0.12f, 0.08f, 0.05f, 0.9f));

            GameObject fillArea = FindOrCreateChild(sliderGO.transform, "Fill Area");
            RectTransform fillAreaRT = fillArea.GetComponent<RectTransform>() ?? fillArea.AddComponent<RectTransform>();
            StretchFull(fillAreaRT);

            GameObject fill = FindOrCreateChild(fillArea.transform, "Fill");
            RectTransform fillRT = fill.GetComponent<RectTransform>() ?? fill.AddComponent<RectTransform>();
            fillRT.anchorMin = new Vector2(0f, 0f);
            fillRT.anchorMax = new Vector2(0f, 1f);
            fillRT.pivot = new Vector2(0f, 0.5f);
            fillRT.offsetMin = Vector2.zero;
            fillRT.offsetMax = Vector2.zero;
            AddImage(fillRT, new Color(0.45f, 0.75f, 0.35f, 1f));

            slider.fillRect = fillRT;

            return slider;
        }

        private static GameObject BuildInventory(Transform canvasTransform)
        {
            GameObject panel = FindOrCreateChild(canvasTransform, "Inventory_Panel");
            RectTransform panelRT = panel.GetComponent<RectTransform>() ?? panel.AddComponent<RectTransform>();
            SetRect(panelRT, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 16), new Vector2(360, 76));

            HorizontalLayoutGroup layout = GetOrAddComponent<HorizontalLayoutGroup>(panel);
            layout.spacing = 8f;
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleCenter;

            var slotViewsProp = new System.Collections.Generic.List<Object>();
            for (int i = 0; i < 5; i++)
            {
                slotViewsProp.Add(BuildInventorySlot(panel.transform, i));
            }

            InventoryUI inventoryUI = GetOrAddComponent<InventoryUI>(panel);
            SetSerializedObjectList(inventoryUI, "_slotViews", slotViewsProp);

            return panel;
        }

        private static InventorySlotUI BuildInventorySlot(Transform parent, int index)
        {
            GameObject slot = FindOrCreateChild(parent, $"Slot_{index}");
            RectTransform slotRT = slot.GetComponent<RectTransform>() ?? slot.AddComponent<RectTransform>();
            slotRT.sizeDelta = new Vector2(60, 60);
            AddImage(slotRT, new Color(0.93f, 0.87f, 0.74f, 1f));
            GetOrAddComponent<LayoutElement>(slot).preferredWidth = 60;
            slot.GetComponent<LayoutElement>().preferredHeight = 60;

            GameObject iconGO = FindOrCreateChild(slot.transform, "Icon");
            RectTransform iconRT = iconGO.GetComponent<RectTransform>() ?? iconGO.AddComponent<RectTransform>();
            SetRect(iconRT, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(44, 44));
            Image iconImage = AddImage(iconRT, Color.white);
            iconImage.enabled = false;

            TextMeshProUGUI qtyText = FindOrCreateText(slot.transform, "Qty", "",
                new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-4, 4), new Vector2(28, 18), 12, TextAlignmentOptions.BottomRight);
            qtyText.fontStyle = FontStyles.Bold;

            InventorySlotUI slotUI = GetOrAddComponent<InventorySlotUI>(slot);
            SetSerializedRef(slotUI, "_iconImage", iconImage);
            SetSerializedRef(slotUI, "_quantityLabel", qtyText);

            return slotUI;
        }

        private static GameObject BuildDialoguePanel(Transform canvasTransform)
        {
            GameObject panel = FindOrCreateChild(canvasTransform, "Dialogue_Panel");
            RectTransform panelRT = panel.GetComponent<RectTransform>() ?? panel.AddComponent<RectTransform>();
            SetRect(panelRT, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 110), new Vector2(900, 180));
            AddImage(panelRT, new Color(0f, 0f, 0f, 0.8f));

            // Portrait pembicara, di kiri. Diisi/di-toggle runtime oleh DialogueUI berdasarkan
            // CharacterData.Portrait dari DialogueManager.CurrentSpeakerPortrait.
            GameObject portraitGO = FindOrCreateChild(panel.transform, "PortraitImage");
            RectTransform portraitRT = portraitGO.GetComponent<RectTransform>();
            SetRect(portraitRT, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(140, -16));
            Image portraitImage = AddImage(portraitRT, Color.white);
            portraitImage.preserveAspect = true;
            portraitImage.enabled = false;

            const float textLeft = 172f;

            FindOrCreateText(panel.transform, "SpeakerNameText", "Nama NPC",
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(textLeft, -12), new Vector2(-16, 26), 18, TextAlignmentOptions.Left)
                .fontStyle = FontStyles.Bold;

            FindOrCreateText(panel.transform, "DialogueText", "...",
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 1), new Vector2(textLeft, -44), new Vector2(-16, -50), 16, TextAlignmentOptions.TopLeft);

            GameObject nextButtonGO = BuildSimpleButton(panel.transform, "NextButton", "Lanjut ▶");
            RectTransform nextRT = nextButtonGO.GetComponent<RectTransform>();
            SetRect(nextRT, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-16, 12), new Vector2(110, 32));

            GameObject choiceContainer = FindOrCreateChild(panel.transform, "ChoiceButtonContainer");
            RectTransform choiceRT = choiceContainer.GetComponent<RectTransform>() ?? choiceContainer.AddComponent<RectTransform>();
            SetRect(choiceRT, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(textLeft, 12), new Vector2(-32 - (textLeft - 16), 60));
            VerticalLayoutGroup vlg = GetOrAddComponent<VerticalLayoutGroup>(choiceContainer);
            vlg.spacing = 4f;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            return panel;
        }

        private static Button GetOrCreateChoiceButtonPrefab()
        {
            Button existing = AssetDatabase.LoadAssetAtPath<GameObject>(ChoicePrefabPath)?.GetComponent<Button>();
            if (existing != null)
            {
                return existing;
            }

            GameObject temp = new GameObject("ChoiceButtonTemplate", typeof(RectTransform));
            RectTransform rt = temp.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 28);
            AddImage(rt, new Color(0.3f, 0.3f, 0.3f, 0.9f));
            Button button = temp.AddComponent<Button>();

            TextMeshProUGUI label = FindOrCreateText(temp.transform, "Label", "Pilihan",
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, 14, TextAlignmentOptions.Center);

            EnsureFolder("Assets/Prefabs/UI");
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, ChoicePrefabPath);
            Object.DestroyImmediate(temp);

            return prefab.GetComponent<Button>();
        }

        private static GameObject BuildSimpleButton(Transform parent, string name, string label)
        {
            GameObject go = FindOrCreateChild(parent, name);
            RectTransform rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
            AddImage(rt, new Color(0.4f, 0.28f, 0.14f, 1f));
            GetOrAddComponent<Button>(go);

            FindOrCreateText(go.transform, "Label", label,
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, 14, TextAlignmentOptions.Center);

            return go;
        }

        // ---------------------------------------------------------------
        // HELPERS: hierarchy / komponen
        // ---------------------------------------------------------------
        private static GameObject FindOrCreateRoot(string name)
        {
            var scene = SceneManager.GetActiveScene();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                {
                    return root;
                }
            }

            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Build Alif Demo Scene");
            return go;
        }

        private static GameObject FindOrCreateChild(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing.gameObject;
            }

            // Selalu dibuat dengan RectTransform (helper ini cuma dipakai buat elemen UI di bawah
            // Canvas) — GameObject.AddComponent<RectTransform>() TIDAK bisa menggantikan Transform
            // biasa yang sudah ada di GameObject, jadi harus disertakan sejak GameObject dibuat.
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        // Sama seperti FindOrCreateChild, tapi pakai Transform biasa (bukan RectTransform) —
        // buat anak world-space seperti bangku/collider di bawah Background, bukan elemen UI.
        private static GameObject FindOrCreateWorldChild(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing.gameObject;
            }

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static GameObject FindOrCreateUIRoot(string name)
        {
            var scene = SceneManager.GetActiveScene();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                {
                    return root;
                }
            }

            var go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Build Alif Demo Scene");
            return go;
        }

        private static T GetOrAddChild<T>(Transform parent, string name) where T : Component
        {
            GameObject go = FindOrCreateChild(parent, name);
            return GetOrAddComponent<T>(go);
        }

        private static T GetOrAddComponent<T>(GameObject go) where T : Component
        {
            T comp = go.GetComponent<T>();
            return comp != null ? comp : go.AddComponent<T>();
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject go = new GameObject("EventSystem");
            Undo.RegisterCreatedObjectUndo(go, "Build Alif Demo Scene");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
            string folderName = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, folderName);
        }

        // ---------------------------------------------------------------
        // HELPERS: UI layout
        // ---------------------------------------------------------------
        private static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static Image AddImage(RectTransform rt, Color color)
        {
            Image img = GetOrAddComponent<Image>(rt.gameObject);
            img.color = color;
            return img;
        }

        private static TextMeshProUGUI FindOrCreateText(Transform parent, string name, string defaultText,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta,
            float fontSize, TextAlignmentOptions alignment)
        {
            GameObject go = FindOrCreateChild(parent, name);
            RectTransform rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
            SetRect(rt, anchorMin, anchorMax, pivot, anchoredPos, sizeDelta);

            TextMeshProUGUI tmp = GetOrAddComponent<TextMeshProUGUI>(go);
            if (string.IsNullOrEmpty(tmp.text))
            {
                tmp.text = defaultText;
            }
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.alignment = alignment;

            return tmp;
        }

        // ---------------------------------------------------------------
        // HELPERS: SerializedObject (untuk isi field private [SerializeField])
        // ---------------------------------------------------------------
        private static void SetSerializedRef(Object target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[Alif] Field '{fieldName}' tidak ditemukan di {target.GetType().Name}.");
                return;
            }

            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetSerializedValue(Object target, string fieldName, float value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null) return;
            prop.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetSerializedValue(Object target, string fieldName, LayerMask value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null) return;
            prop.intValue = value.value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetSerializedValue(Object target, string fieldName, Vector3 value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null) return;
            prop.vector3Value = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetSerializedObjectList(Object target, string fieldName, System.Collections.Generic.List<Object> values)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[Alif] Field '{fieldName}' tidak ditemukan di {target.GetType().Name}.");
                return;
            }

            prop.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------
        // HELPERS: sprite lingkaran (untuk joystick & tombol interact on-screen)
        // ---------------------------------------------------------------
        // Ikon panah segitiga menghadap bawah (buat InteractionArrow di atas NPC/benda).
        // Ini sprite WORLD-SPACE (dipakai SpriteRenderer, bukan UI Image), jadi PPU eksplisit
        // di-set (200) supaya ukurannya konsisten & nggak kegedean/kekecilan di scene.
        private static Sprite GetOrCreateTriangleSprite(string name, Color color, int size)
        {
            string relativePath = $"Assets/Sprites/UI/{name}.png";
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(relativePath);
            if (existing != null)
            {
                return existing;
            }

            EnsureFolder("Assets/Sprites/UI");

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            float center = size / 2f;

            // y=0 di atas (lebar penuh), y=size-1 di bawah (menyempit jadi titik) -> segitiga
            // mengarah ke bawah, sesuai konvensi "indikator interact" di game top-down.
            for (int y = 0; y < size; y++)
            {
                float t = y / (float)(size - 1);
                float halfWidth = center * (1f - t);

                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Abs(x + 0.5f - center);
                    float edgeAlpha = Mathf.Clamp01(halfWidth - dx + 1f);
                    Color pixel = color;
                    pixel.a *= edgeAlpha;
                    pixels[y * size + x] = pixel;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            byte[] png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            File.WriteAllBytes(Path.Combine(projectRoot, relativePath), png);
            AssetDatabase.ImportAsset(relativePath);

            var importer = (TextureImporter)AssetImporter.GetAtPath(relativePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.spritePixelsPerUnit = 200f;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(relativePath);
        }

        private static Sprite GetOrCreateCircleSprite(string name, Color color, int size)
        {
            string relativePath = $"Assets/Sprites/UI/{name}.png";
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(relativePath);
            if (existing != null)
            {
                return existing;
            }

            EnsureFolder("Assets/Sprites/UI");

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size / 2f;
            var pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    // Tepi lingkaran di-soften 1px biar nggak bergerigi (anti-alias sederhana).
                    float edgeAlpha = Mathf.Clamp01(radius - dist);
                    Color pixel = color;
                    pixel.a *= edgeAlpha;
                    pixels[y * size + x] = pixel;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            byte[] png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            File.WriteAllBytes(Path.Combine(projectRoot, relativePath), png);
            AssetDatabase.ImportAsset(relativePath);

            var importer = (TextureImporter)AssetImporter.GetAtPath(relativePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(relativePath);
        }
    }
}
