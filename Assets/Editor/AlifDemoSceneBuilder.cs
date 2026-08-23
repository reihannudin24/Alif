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
        private const string ExteriorBackgroundSpritePath = "Assets/Sprites/Backgrounds/Stasiun_Luar.png";
        private const string TargetScenePath = "Assets/Scenes/SampleScene.unity";

        // Area eksterior ditaruh jauh di bawah interior (bukan scene terpisah, biar CameraFollow
        // otomatis "pindah" cuma dengan reposisi Player) — cukup jauh (20 unit) dari batas bawah
        // interior (halfH 4.72) supaya nggak pernah kelihatan bertumpuk di kamera.
        private static readonly Vector2 ExteriorOrigin = new Vector2(0f, -20f);

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

            // 2b. Area luar stasiun (eksterior) + pintu penghubung interior<->eksterior, biar
            // Player bisa "keluar" dari stasiun lewat koridor tengah paling bawah.
            BuildStasiunLuar();
            BuildDoors();

            // 3. Persistent managers.
            GameObject managersRoot = FindOrCreateRoot("_GameManagers");
            GetOrAddChild<GameManager>(managersRoot.transform, "GameManager");
            GetOrAddChild<SceneLoader>(managersRoot.transform, "SceneLoader");
            GetOrAddChild<TimeSystem>(managersRoot.transform, "TimeSystem");
            GetOrAddChild<EnergySystem>(managersRoot.transform, "EnergySystem");
            GetOrAddChild<CurrencySystem>(managersRoot.transform, "CurrencySystem");
            GetOrAddChild<ScoreSystem>(managersRoot.transform, "ScoreSystem");
            GetOrAddChild<InventorySystem>(managersRoot.transform, "InventorySystem");
            DialogueManager dialogueManager = GetOrAddChild<DialogueManager>(managersRoot.transform, "DialogueManager");

            // 4. Player.
            PlayerController playerController = BuildPlayer();
            SetupCameraFollow(playerController.transform);

            // 5. NPC (pakai karakter asli dari Assets/Sprites/Characters, hasil "Alif > 1) Import Character Art").
            BuildNpcs();

            // 5b. Bangku/kursi yang bisa di-interact (nggak bisa ditembus + munculin monolog Alif).
            BuildBenches();

            // 5c. Monolog pembuka Alif — jembatan naratif dari cutscene ke gameplay bebas,
            // otomatis muncul begitu scene ini pertama kali dimuat, sebelum interaksi ke NPC lain.
            BuildOpeningMonologue(playerController);

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
        // AREA LUAR STASIUN (EKSTERIOR)
        // ---------------------------------------------------------------
        // Belum ada analisis pixel detail kayak interior (belum tau letak persis tembok/void di
        // gambar ini) — buat sekarang cukup boundary luar doang, biar Player nggak jalan ke void
        // di luar gambar. Bisa ditambah VoidBlocker presisi nanti kalau perlu.
        private static void BuildStasiunLuar()
        {
            Sprite exteriorSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ExteriorBackgroundSpritePath);
            if (exteriorSprite == null)
            {
                Debug.LogWarning($"[Alif] Background sprite tidak ditemukan di '{ExteriorBackgroundSpritePath}', dilewati.");
                return;
            }

            GameObject exterior = FindOrCreateRoot("Background_StasiunLuar");
            exterior.transform.position = new Vector3(ExteriorOrigin.x, ExteriorOrigin.y, 0f);

            SpriteRenderer sr = GetOrAddComponent<SpriteRenderer>(exterior);
            sr.sprite = exteriorSprite;
            sr.sortingOrder = SortingOrderGround;

            for (int i = exterior.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = exterior.transform.GetChild(i);
                if (child.name.StartsWith("BoundaryWall"))
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }

            // Stasiun_Luar.png 1540x1834px, PPU 200 -> half extents (3.85, 4.585).
            const float halfW = 3.85f;
            const float halfH = 4.585f;
            const float wallThickness = 0.2f;

            AddBoxBlocker(exterior, "BoundaryWall_Top", new Vector2(0, halfH + wallThickness / 2f), new Vector2(halfW * 2 + wallThickness * 2, wallThickness));
            AddBoxBlocker(exterior, "BoundaryWall_Bottom", new Vector2(0, -halfH - wallThickness / 2f), new Vector2(halfW * 2 + wallThickness * 2, wallThickness));
            AddBoxBlocker(exterior, "BoundaryWall_Left", new Vector2(-halfW - wallThickness / 2f, 0), new Vector2(wallThickness, halfH * 2 + wallThickness * 2));
            AddBoxBlocker(exterior, "BoundaryWall_Right", new Vector2(halfW + wallThickness / 2f, 0), new Vector2(wallThickness, halfH * 2 + wallThickness * 2));
        }

        // ---------------------------------------------------------------
        // PINTU (interior <-> eksterior) — lihat SceneDoor.cs. Posisi diambil dari analisis
        // koridor tengah interior (celah antara VoidBlocker "kanan-bawah"/"kiri-bawah", x
        // sekitar -1.4..1.4) yang memang dibiarkan terbuka sampai BoundaryWall_Bottom — ini
        // persis titik di skrinsyot user, di antara dua bingkai pintu koridor paling bawah.
        // ---------------------------------------------------------------
        private static void BuildDoors()
        {
            GameObject doorsRoot = FindOrCreateRoot("Doors");

            GameObject interiorSpawnGO = FindOrCreateWorldChild(doorsRoot.transform, "InteriorSpawn");
            interiorSpawnGO.transform.position = new Vector3(0f, -3.8f, 0f);

            GameObject exteriorSpawnGO = FindOrCreateWorldChild(doorsRoot.transform, "ExteriorSpawn");
            exteriorSpawnGO.transform.position = new Vector3(0f, ExteriorOrigin.y + 3.5f, 0f);

            BuildDoorTrigger(doorsRoot.transform, "Door_KeluarStasiun", new Vector2(0f, -4.55f), new Vector2(1.2f, 0.3f), exteriorSpawnGO.transform, "Keluar dari stasiun?");
            BuildDoorTrigger(doorsRoot.transform, "Door_MasukStasiun", new Vector2(0f, ExteriorOrigin.y + 5f), new Vector2(1.2f, 0.3f), interiorSpawnGO.transform, "Masuk ke stasiun?");
        }

        private static void BuildDoorTrigger(Transform parent, string name, Vector2 worldPosition, Vector2 size, Transform destination, string confirmMessage)
        {
            GameObject door = FindOrCreateWorldChild(parent, name);
            door.transform.position = new Vector3(worldPosition.x, worldPosition.y, 0f);

            BoxCollider2D collider = GetOrAddComponent<BoxCollider2D>(door);
            collider.isTrigger = true;
            collider.size = size;

            SceneDoor sceneDoor = GetOrAddComponent<SceneDoor>(door);
            SetSerializedRef(sceneDoor, "_destination", destination);
            SetSerializedValue(sceneDoor, "_confirmMessage", confirmMessage);
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

        // Titik awal Alif waktu pertama kali masuk SampleScene (New Game maupun lewat Chapter
        // Select) — deket bangku & lemari kaca ruang Display, sesuai posisi yang diminta user.
        private static readonly Vector3 PlayerSpawnPosition = new Vector3(4.8f, -2.5f, 0f);

        private static PlayerController BuildPlayer()
        {
            GameObject player = FindOrCreateRoot("Player");
            player.tag = "Player";
            player.transform.position = PlayerSpawnPosition;

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
        // MONOLOG PEMBUKA (jembatan naratif cutscene -> gameplay bebas)
        // ---------------------------------------------------------------
        private static void BuildOpeningMonologue(PlayerController playerController)
        {
            DialogueData dialogue = GetOrCreateOpeningMonologueDialogue();
            CharacterData alifData = GetOrCreateAlifCharacterData();

            OpeningMonologueTrigger trigger = GetOrAddComponent<OpeningMonologueTrigger>(playerController.gameObject);
            SetSerializedRef(trigger, "_dialogue", dialogue);
            SetSerializedRef(trigger, "_speakerData", alifData);
        }

        private static DialogueData GetOrCreateOpeningMonologueDialogue()
        {
            string path = $"{DialogueDataFolder}/DialogueData_OpeningMonologue.asset";
            DialogueData data = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
            if (data != null)
            {
                return data;
            }

            data = ScriptableObject.CreateInstance<DialogueData>();
            data.Lines.Add(new DialogueLine
            {
                SpeakerName = "Alif",
                Text = "Baiklah... Ayam geprek Bu Siti kedengerannya enak banget sekarang."
            });
            data.Lines.Add(new DialogueLine
            {
                SpeakerName = "Alif",
                Text = "Tapi kayaknya aku harus cari jalan keluar dari stasiun ini dulu."
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

            // Audio dibangun duluan (sebelum tombol2 lain) supaya AudioManager + clip klik-nya
            // siap dipakai buat nempelin SFX ke tombol Next/Interact di bawah.
            (AudioManager audioManager, AudioClip clickSfx) = BuildGameplayAudio(canvasGO.transform);

            GameObject hudPanel = BuildHUD(canvasGO.transform);
            GameObject inventoryPanel = BuildInventory(canvasGO.transform);
            (GameObject dialoguePanel, Image dialoguePortraitImage) = BuildDialoguePanel(canvasGO.transform, audioManager, clickSfx);

            // Klik di area manapun SELAIN teks dialog & portrait karakter dihitung "lanjut/tutup"
            // dialog — lapisan penuh-layar ini ditaruh PALING BELAKANG di antara elemen dialog
            // (lihat pengurutan sibling index di BuildDialoguePanel) supaya box, tombol Next/Choice,
            // dan portrait tetap dapat prioritas klik masing-masing; sisanya jatuh ke sini.
            Button advanceCatcherButton = BuildDialogueAdvanceCatcher(canvasGO.transform, dialoguePortraitImage);

            (GameObject joystickGO, GameObject interactButtonGO) = BuildTouchControls(canvasGO.transform, playerController, audioManager, clickSfx);

            // Tombol pause (pojok kanan-atas) + panel "Lanjut/Keluar" + popup konfirmasi keluar,
            // juga bisa dipicu tombol back Android/ESC (lihat PauseMenuController).
            GameObject pauseButtonGO = BuildPauseMenu(canvasGO.transform, audioManager, clickSfx);

            // Popup konfirmasi "Pindah ke ...?" dipakai SceneDoor (lihat TravelConfirmationUI) —
            // singleton, ditemukan runtime lewat TravelConfirmationUI.Instance, nggak perlu
            // di-assign ke komponen lain di sini.
            BuildTravelConfirmationPopup(canvasGO.transform, audioManager, clickSfx);

            // DialogueUI diletakkan di root Canvas (selalu aktif) supaya tetap subscribe ke event
            // DialogueManager walaupun panel visualnya (dialoguePanel) sedang disembunyikan.
            DialogueUI dialogueUI = GetOrAddComponent<DialogueUI>(canvasGO);
            SetSerializedRef(dialogueUI, "_dialogueBoxRoot", dialoguePanel);
            SetSerializedRef(dialogueUI, "_speakerNameText", dialoguePanel.transform.Find("SpeakerNameText")?.GetComponent<TMP_Text>());
            SetSerializedRef(dialogueUI, "_dialogueText", dialoguePanel.transform.Find("DialogueText")?.GetComponent<TMP_Text>());
            SetSerializedRef(dialogueUI, "_portraitImage", dialoguePortraitImage);
            SetSerializedRef(dialogueUI, "_nextButton", dialoguePanel.transform.Find("NextButton")?.GetComponent<Button>());
            SetSerializedRef(dialogueUI, "_choiceButtonContainer", dialoguePanel.transform.Find("ChoiceButtonContainer"));
            SetSerializedRef(dialogueUI, "_choiceButtonPrefab", GetOrCreateChoiceButtonPrefab());
            SetSerializedRef(dialogueUI, "_advanceCatcherButton", advanceCatcherButton);
            SetSerializedRef(dialogueUI, "_audioManager", audioManager);
            SetSerializedRef(dialogueUI, "_buttonClickSfx", clickSfx);

            // Kontrol on-screen (joystick, tombol interact "E") & bar inventory sengaja
            // disembunyikan sementara dialog aktif — nggak relevan (movement udah terkunci) dan
            // cuma nutup-nutupin layar visual novel.
            SetSerializedRef(dialogueUI, "_joystickRoot", joystickGO);
            SetSerializedRef(dialogueUI, "_interactButtonRoot", interactButtonGO);
            SetSerializedRef(dialogueUI, "_inventoryPanelRoot", inventoryPanel);
            SetSerializedRef(dialogueUI, "_pauseButtonRoot", pauseButtonGO);

            SetSerializedRef(uiManager, "_hudPanel", hudPanel);
            SetSerializedRef(uiManager, "_inventoryPanel", inventoryPanel);
            SetSerializedRef(uiManager, "_dialoguePanel", dialoguePanel);

            dialoguePanel.SetActive(false);
            advanceCatcherButton.gameObject.SetActive(false);

            // Overlay fade hitam ("layar loading") dipakai SceneDoor buat transisi antar area —
            // dibuat PALING TERAKHIR supaya render di atas semua elemen lain (HUD, joystick, dsb).
            BuildLoadingOverlay(canvasGO.transform);
        }

        private static void BuildLoadingOverlay(Transform canvasTransform)
        {
            GameObject overlay = FindOrCreateChild(canvasTransform, "LoadingOverlay");
            overlay.transform.SetAsLastSibling();
            RectTransform overlayRT = overlay.GetComponent<RectTransform>();
            StretchFull(overlayRT);
            AddImage(overlayRT, Color.black);

            CanvasGroup canvasGroup = GetOrAddComponent<CanvasGroup>(overlay);
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            TextMeshProUGUI loadingText = FindOrCreateText(overlay.transform, "LoadingText", "Memuat...",
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, 22, TextAlignmentOptions.Center);
            loadingText.fontStyle = FontStyles.Bold;

            SceneFadeController fadeController = GetOrAddComponent<SceneFadeController>(overlay);
            SetSerializedRef(fadeController, "_fadeCanvasGroup", canvasGroup);
        }

        // ---------------------------------------------------------------
        // TOUCH CONTROLS: joystick on-screen + tombol interact
        // ---------------------------------------------------------------
        private static (GameObject joystick, GameObject interactButton) BuildTouchControls(Transform canvasTransform, PlayerController playerController, AudioManager audioManager, AudioClip clickSfx)
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
            AddClickSfxListener(interactButton, audioManager, clickSfx);

            TextMeshProUGUI interactLabel = FindOrCreateText(interactGO.transform, "Label", "E",
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, 22, TextAlignmentOptions.Center);
            interactLabel.fontStyle = FontStyles.Bold;

            return (joystickGO, interactGO);
        }

        // ---------------------------------------------------------------
        // PAUSE MENU: tombol pause pojok kanan-atas (satu-satunya sudut layar yang masih
        // kosong) -> panel kecil "Lanjut"/"Keluar". Tombol back Android/ESC (action "Cancel")
        // juga nyambung ke alur yang sama lewat PauseMenuController.
        // ---------------------------------------------------------------
        private static GameObject BuildPauseMenu(Transform canvasTransform, AudioManager audioManager, AudioClip clickSfx)
        {
            QuitConfirmationUI quitConfirmation = BuildGameplayQuitConfirmationPopup(canvasTransform, audioManager, clickSfx);

            GameObject pauseButtonGO = FindOrCreateChild(canvasTransform, "PauseButton");
            RectTransform pauseRT = pauseButtonGO.GetComponent<RectTransform>();
            SetRect(pauseRT, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-16, -16), new Vector2(48, 48));
            Image pauseImg = AddImage(pauseRT, new Color(0.25f, 0.16f, 0.08f, 0.85f));
            Button pauseButton = GetOrAddComponent<Button>(pauseButtonGO);
            pauseButton.targetGraphic = pauseImg;
            AddClickSfxListener(pauseButton, audioManager, clickSfx);

            FindOrCreateText(pauseButtonGO.transform, "Label", "=",
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, 22, TextAlignmentOptions.Center)
                .fontStyle = FontStyles.Bold;

            // Panel pause kecil, muncul di tengah waktu tombol pause di atas diklik.
            GameObject pauseMenuRoot = FindOrCreateChild(canvasTransform, "PauseMenuPanel");
            RectTransform pausePanelRT = pauseMenuRoot.GetComponent<RectTransform>();
            StretchFull(pausePanelRT);
            AddImage(pausePanelRT, new Color(0f, 0f, 0f, 0.6f));
            pauseMenuRoot.transform.SetAsLastSibling();

            GameObject card = FindOrCreateChild(pauseMenuRoot.transform, "Card");
            RectTransform cardRT = card.GetComponent<RectTransform>();
            SetRect(cardRT, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(320, 220));
            AddImage(cardRT, new Color(0.2f, 0.14f, 0.08f, 0.97f));

            FindOrCreateText(card.transform, "TitleText", "Jeda",
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f), new Vector2(0, -20), new Vector2(-24, 32), 20, TextAlignmentOptions.Center)
                .fontStyle = FontStyles.Bold;

            GameObject resumeButtonGO = BuildSimpleButton(card.transform, "ResumeButton", "Lanjut");
            SetRect(resumeButtonGO.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -6), new Vector2(240, 44));
            AddClickSfxListener(resumeButtonGO.GetComponent<Button>(), audioManager, clickSfx);

            GameObject exitButtonGO = BuildSimpleButton(card.transform, "ExitButton", "Keluar");
            SetRect(exitButtonGO.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -62), new Vector2(240, 44));
            AddClickSfxListener(exitButtonGO.GetComponent<Button>(), audioManager, clickSfx);

            pauseMenuRoot.SetActive(false);

            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);

            PauseMenuController pauseMenuController = GetOrAddComponent<PauseMenuController>(canvasTransform.gameObject);
            SetSerializedRef(pauseMenuController, "_inputActions", actions);
            SetSerializedRef(pauseMenuController, "_pauseMenuRoot", pauseMenuRoot);
            SetSerializedRef(pauseMenuController, "_pauseButton", pauseButton);
            SetSerializedRef(pauseMenuController, "_resumeButton", resumeButtonGO.GetComponent<Button>());
            SetSerializedRef(pauseMenuController, "_exitButton", exitButtonGO.GetComponent<Button>());
            SetSerializedRef(pauseMenuController, "_quitConfirmation", quitConfirmation);

            return pauseButtonGO;
        }

        // Popup konfirmasi "Apakah anda yakin mau keluar?" versi gameplay — style-nya konsisten
        // sama dialogue box (BuildSimpleButton), beda instance dari yang di Main Menu (scene
        // terpisah). Dipicu tombol "Keluar" di pause menu ATAU tombol back Android/ESC.
        private static QuitConfirmationUI BuildGameplayQuitConfirmationPopup(Transform canvasTransform, AudioManager audioManager, AudioClip clickSfx)
        {
            GameObject overlay = FindOrCreateChild(canvasTransform, "QuitConfirmPopup");
            RectTransform overlayRT = overlay.GetComponent<RectTransform>();
            StretchFull(overlayRT);
            AddImage(overlayRT, new Color(0f, 0f, 0f, 0.6f));
            overlay.transform.SetAsLastSibling();

            GameObject card = FindOrCreateChild(overlay.transform, "Card");
            RectTransform cardRT = card.GetComponent<RectTransform>();
            SetRect(cardRT, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420, 230));
            AddImage(cardRT, new Color(0.16f, 0.12f, 0.08f, 0.97f));

            FindOrCreateText(card.transform, "MessageText", "Apakah anda yakin mau keluar?",
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f), new Vector2(0, -28), new Vector2(-32, 64), 20, TextAlignmentOptions.Center)
                .fontStyle = FontStyles.Bold;

            GameObject cancelButtonGO = BuildSimpleButton(card.transform, "CancelButton", "BATAL");
            SetRect(cancelButtonGO.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -8), new Vector2(340, 44));
            Button cancelButton = cancelButtonGO.GetComponent<Button>();

            GameObject confirmButtonGO = BuildSimpleButton(card.transform, "ConfirmButton", "YA, KELUAR");
            SetRect(confirmButtonGO.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(340, 40));
            Button confirmButton = confirmButtonGO.GetComponent<Button>();

            QuitConfirmationUI quitConfirmation = GetOrAddComponent<QuitConfirmationUI>(overlay);
            SetSerializedRef(quitConfirmation, "_root", overlay);
            SetSerializedRef(quitConfirmation, "_confirmButton", confirmButton);
            SetSerializedRef(quitConfirmation, "_cancelButton", cancelButton);

            AddClickSfxListener(confirmButton, audioManager, clickSfx);
            AddClickSfxListener(cancelButton, audioManager, clickSfx);

            overlay.SetActive(false);

            return quitConfirmation;
        }

        // Popup konfirmasi umum "Pindah ke ...?" — dipakai SceneDoor SEBELUM benar-benar
        // transisi/fade ke area lain (lihat TravelConfirmationUI), biar nggak langsung
        // "loading" pas Player kesenggol trigger pintu doang.
        private static void BuildTravelConfirmationPopup(Transform canvasTransform, AudioManager audioManager, AudioClip clickSfx)
        {
            GameObject overlay = FindOrCreateChild(canvasTransform, "TravelConfirmPopup");
            RectTransform overlayRT = overlay.GetComponent<RectTransform>();
            StretchFull(overlayRT);
            AddImage(overlayRT, new Color(0f, 0f, 0f, 0.6f));
            overlay.transform.SetAsLastSibling();

            GameObject card = FindOrCreateChild(overlay.transform, "Card");
            RectTransform cardRT = card.GetComponent<RectTransform>();
            SetRect(cardRT, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420, 230));
            AddImage(cardRT, new Color(0.16f, 0.12f, 0.08f, 0.97f));

            TMP_Text messageText = FindOrCreateText(card.transform, "MessageText", "Pindah ke area lain?",
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f), new Vector2(0, -28), new Vector2(-32, 64), 20, TextAlignmentOptions.Center);
            messageText.fontStyle = FontStyles.Bold;

            GameObject cancelButtonGO = BuildSimpleButton(card.transform, "CancelButton", "TIDAK");
            SetRect(cancelButtonGO.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(340, 40));
            Button cancelButton = cancelButtonGO.GetComponent<Button>();

            GameObject confirmButtonGO = BuildSimpleButton(card.transform, "ConfirmButton", "YA, PINDAH");
            SetRect(confirmButtonGO.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -8), new Vector2(340, 44));
            Button confirmButton = confirmButtonGO.GetComponent<Button>();

            TravelConfirmationUI travelConfirmation = GetOrAddComponent<TravelConfirmationUI>(overlay);
            SetSerializedRef(travelConfirmation, "_root", overlay);
            SetSerializedRef(travelConfirmation, "_messageText", messageText);
            SetSerializedRef(travelConfirmation, "_confirmButton", confirmButton);
            SetSerializedRef(travelConfirmation, "_cancelButton", cancelButton);

            AddClickSfxListener(confirmButton, audioManager, clickSfx);
            AddClickSfxListener(cancelButton, audioManager, clickSfx);

            overlay.SetActive(false);
        }

        private static GameObject BuildHUD(Transform canvasTransform)
        {
            GameObject panel = FindOrCreateChild(canvasTransform, "HUD_Panel");
            RectTransform panelRT = panel.GetComponent<RectTransform>() ?? panel.AddComponent<RectTransform>();
            SetRect(panelRT, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -20), new Vector2(280, 200));
            AddImage(panelRT, new Color(0.25f, 0.16f, 0.08f, 0.85f));

            // Jam (jam:menit) sengaja dihapus dari HUD — kalau ada sisa "ClockText" dari build
            // versi lama, bersihkan supaya nggak jadi GameObject mati yang nggak kepakai.
            Transform staleClockText = panel.transform.Find("ClockText");
            if (staleClockText != null)
            {
                Object.DestroyImmediate(staleClockText.gameObject);
            }

            TextMeshProUGUI dayWeekText = FindOrCreateText(panel.transform, "DayWeekText", "Tuesday, 1st week",
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(12, -10), new Vector2(-12, 24), 16, TextAlignmentOptions.Left);

            // Tiga bar diberi label kecil di atasnya (dulu cuma Energy sendirian jadi nggak
            // perlu label; sekarang ada 3 bar sekaligus, tanpa label bakal ambigu).
            Slider energySlider = BuildLabeledSlider(panel.transform, "Energy", "Energi", -34, new Color(0.45f, 0.75f, 0.35f, 1f));
            Slider financialLogicSlider = BuildLabeledSlider(panel.transform, "FinancialLogic", "Logika Finansial", -68, new Color(0.35f, 0.6f, 0.85f, 1f));
            Slider shariaComplianceSlider = BuildLabeledSlider(panel.transform, "ShariaCompliance", "Kepatuhan Syariah", -102, new Color(0.85f, 0.65f, 0.25f, 1f));

            FindOrCreateText(panel.transform, "MoneyLabel", "Uang",
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(12, 32), new Vector2(-12, 14), 11, TextAlignmentOptions.Left)
                .color = new Color(1f, 1f, 1f, 0.75f);

            TextMeshProUGUI moneyText = FindOrCreateText(panel.transform, "MoneyText", "Rp 150",
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(12, 8), new Vector2(-12, 22), 16, TextAlignmentOptions.Left);

            HUDController hud = GetOrAddComponent<HUDController>(panel);
            SetSerializedRef(hud, "_dayWeekText", dayWeekText);
            SetSerializedRef(hud, "_energySlider", energySlider);
            SetSerializedRef(hud, "_financialLogicSlider", financialLogicSlider);
            SetSerializedRef(hud, "_shariaComplianceSlider", shariaComplianceSlider);
            SetSerializedRef(hud, "_moneyText", moneyText);

            return panel;
        }

        // Label kecil + progress bar di bawahnya, dipakai buat Energy, Financial Logic (skor
        // logika bisnis argumen pemain di minigame AI Detektif), dan Sharia Compliance (skor
        // bebas Riba/Gharar/Maysir) — beda warna fill per bar biar gampang dibedain sekilas.
        private static Slider BuildLabeledSlider(Transform parent, string name, string label, float topY, Color fillColor)
        {
            FindOrCreateText(parent, $"{name}Label", label,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(12, topY), new Vector2(-12, 14), 11, TextAlignmentOptions.Left)
                .color = new Color(1f, 1f, 1f, 0.75f);

            GameObject sliderGO = FindOrCreateChild(parent, $"{name}Slider");
            RectTransform sliderRT = sliderGO.GetComponent<RectTransform>() ?? sliderGO.AddComponent<RectTransform>();
            SetRect(sliderRT, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(12, topY - 16), new Vector2(-24, 12));

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
            AddImage(fillRT, fillColor);

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

        private static (GameObject, Image) BuildDialoguePanel(Transform canvasTransform, AudioManager audioManager, AudioClip clickSfx)
        {
            GameObject panel = FindOrCreateChild(canvasTransform, "Dialogue_Panel");
            RectTransform panelRT = panel.GetComponent<RectTransform>() ?? panel.AddComponent<RectTransform>();
            SetRect(panelRT, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 110), new Vector2(900, 180));
            Image panelImage = AddImage(panelRT, new Color(0f, 0f, 0f, 0.8f));

            // Panel background sendiri juga bisa diklik buat "lanjut" (area box yang nggak ke-
            // tutup teks/tombol) — wiring listener-nya dilakukan runtime di DialogueUI karena
            // butuh cek kondisi (jangan lanjut kalau lagi nunjukin choices). Transition None biar
            // background nggak ikut nge-tint pas ditekan (dia dekorasi, bukan tombol visual).
            Button panelButton = GetOrAddComponent<Button>(panel);
            panelButton.transition = Selectable.Transition.None;
            panelButton.targetGraphic = panelImage;

            // Sisa "PortraitImage" dari build versi lama (dulu kecil, di dalam box kiri) —
            // bersihkan biar nggak nyisa GameObject mati; portrait-nya sekarang elemen terpisah
            // di luar box (lihat DialoguePortrait di bawah).
            Transform stalePortrait = panel.transform.Find("PortraitImage");
            if (stalePortrait != null)
            {
                Object.DestroyImmediate(stalePortrait.gameObject);
            }

            const float textLeft = 24f; // dulu 172 (nyisain ruang portrait kecil di kiri box) — portrait udah pindah keluar.

            FindOrCreateText(panel.transform, "SpeakerNameText", "Nama NPC",
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(textLeft, -12), new Vector2(-16, 26), 18, TextAlignmentOptions.Left)
                .fontStyle = FontStyles.Bold;

            FindOrCreateText(panel.transform, "DialogueText", "...",
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 1), new Vector2(textLeft, -44), new Vector2(-16, -50), 16, TextAlignmentOptions.TopLeft);

            GameObject nextButtonGO = BuildSimpleButton(panel.transform, "NextButton", "Lanjut >");
            RectTransform nextRT = nextButtonGO.GetComponent<RectTransform>();
            SetRect(nextRT, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-16, 12), new Vector2(110, 32));
            AddClickSfxListener(nextButtonGO.GetComponent<Button>(), audioManager, clickSfx);

            GameObject choiceContainer = FindOrCreateChild(panel.transform, "ChoiceButtonContainer");
            RectTransform choiceRT = choiceContainer.GetComponent<RectTransform>() ?? choiceContainer.AddComponent<RectTransform>();
            SetRect(choiceRT, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(textLeft, 12), new Vector2(-32 - (textLeft - 16), 60));
            VerticalLayoutGroup vlg = GetOrAddComponent<VerticalLayoutGroup>(choiceContainer);
            vlg.spacing = 4f;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // Portrait pembicara — sekarang BESAR & di kanan LUAR box (gaya visual novel:
            // karakter berdiri nongol di atas-belakang box dialog), bukan thumbnail kecil di
            // dalam box kiri lagi. Sibling dari Dialogue_Panel (bukan child) supaya nggak
            // ke-clip tinggi box yang cuma 180px. Diisi/di-toggle runtime oleh DialogueUI
            // berdasarkan CharacterData.Portrait dari DialogueManager.CurrentSpeakerPortrait.
            GameObject portraitGO = FindOrCreateChild(canvasTransform, "DialoguePortrait");
            RectTransform portraitRT = portraitGO.GetComponent<RectTransform>();
            SetRect(portraitRT, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, 100), new Vector2(340, 480));
            Image portraitImage = AddImage(portraitRT, Color.white);
            portraitImage.preserveAspect = true;
            portraitImage.enabled = false;

            // BUG yang bikin NextButton "hilang": portrait dibuat SETELAH panel jadi kalau
            // sibling-nya dibiarkan apa adanya dia ke-render DI ATAS panel (sibling belakangan =
            // digambar belakangan = di depan), nutupin NextButton yang posisinya nempel di
            // pojok kanan-bawah panel (persis di bawah area portrait). Pindahkan portrait ke
            // sibling index panel SEKARANG (dorong panel maju satu index) supaya urutannya jadi
            // portrait dulu (di belakang), baru panel + tombol2-nya (di depan, kebaca & keklik).
            portraitGO.transform.SetSiblingIndex(panel.transform.GetSiblingIndex());

            return (panel, portraitImage);
        }

        // Bunyi klik ditambahkan sebagai persistent listener KEDUA di tiap tombol (Button.onClick
        // mendukung banyak listener) — jadi nggak ganggu listener lain yang sudah ada. Sama persis
        // kayak AlifMainMenuBuilder.AddClickSfxListener, cuma diduplikasi di sini karena scoped
        // private ke class masing-masing.
        private static void AddClickSfxListener(Button button, AudioManager audioManager, AudioClip clickSfx)
        {
            if (button == null || audioManager == null || clickSfx == null)
            {
                return;
            }

            UnityEventTools.AddObjectPersistentListener(button.onClick, audioManager.PlaySfx, clickSfx);
        }

        // ---------------------------------------------------------------
        // AUDIO GAMEPLAY — SFX klik tombol (di-generate lewat "Alif > 6) Generate Placeholder
        // Audio"). Beda instance dari AudioManager Main Menu (scene terpisah, kebutuhan beda).
        // ---------------------------------------------------------------
        private const string ClickSfxClipPath = "Assets/Audio/SFX_ButtonClick.wav";

        private static (AudioManager, AudioClip) BuildGameplayAudio(Transform canvasTransform)
        {
            GameObject audioGO = FindOrCreateChild(canvasTransform, "Audio");

            GameObject sfxGO = FindOrCreateChild(audioGO.transform, "SfxSource");
            AudioSource sfxSource = GetOrAddComponent<AudioSource>(sfxGO);
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.volume = 0.8f;

            AudioManager audioManager = GetOrAddComponent<AudioManager>(audioGO);
            SetSerializedRef(audioManager, "_sfxSource", sfxSource);

            AudioClip clickSfx = AssetDatabase.LoadAssetAtPath<AudioClip>(ClickSfxClipPath);
            if (clickSfx == null)
            {
                Debug.LogWarning("[Alif] SFX klik tombol belum digenerate — jalankan menu 'Alif > 6) Generate Placeholder Audio' dulu, lalu build ulang scene ini.");
            }

            return (audioManager, clickSfx);
        }

        // ---------------------------------------------------------------
        // KLIK-DI-LUAR-UNTUK-LANJUT: lapisan penuh-layar transparan yang nangkep klik di mana
        // aja SELAIN teks dialog & portrait (keduanya raycast target sendiri tanpa listener,
        // jadi klik di situ ke-serap diam-diam, nggak ngapa-ngapain) atau tombol Next/Choice
        // (yang tetap dapat prioritas karena jadi child Dialogue_Panel, digambar di depan).
        // ---------------------------------------------------------------
        private static Button BuildDialogueAdvanceCatcher(Transform canvasTransform, Image dialoguePortraitImage)
        {
            GameObject catcherGO = FindOrCreateChild(canvasTransform, "DialogueAdvanceCatcher");
            RectTransform catcherRT = catcherGO.GetComponent<RectTransform>();
            StretchFull(catcherRT);

            Image catcherImage = AddImage(catcherRT, new Color(0f, 0f, 0f, 0f));

            Button catcherButton = GetOrAddComponent<Button>(catcherGO);
            catcherButton.transition = Selectable.Transition.None;
            catcherButton.targetGraphic = catcherImage;

            // Taruh TEPAT di sibling index DialoguePortrait sekarang (dorong portrait & panel
            // maju), jadi urutan akhirnya: catcher (paling belakang) -> portrait -> panel
            // (paling depan). Portrait & panel sudah saling terurut benar duluan di
            // BuildDialoguePanel; ini cuma nyisipin catcher di posisi paling belakang dari
            // ketiganya, BUKAN di antara portrait & panel (kalau salah taruh di situ, catcher
            // malah nutupin portrait dan klik di karakter jadi ke-anggap "klik area lain").
            catcherGO.transform.SetSiblingIndex(dialoguePortraitImage.transform.GetSiblingIndex());

            return catcherButton;
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
        internal static GameObject FindOrCreateRoot(string name)
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

        internal static GameObject FindOrCreateChild(Transform parent, string name)
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

        internal static GameObject FindOrCreateUIRoot(string name)
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

        internal static T GetOrAddComponent<T>(GameObject go) where T : Component
        {
            T comp = go.GetComponent<T>();
            return comp != null ? comp : go.AddComponent<T>();
        }

        internal static void EnsureEventSystem()
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

        internal static void EnsureFolder(string path)
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
        internal static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
        }

        internal static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        internal static Image AddImage(RectTransform rt, Color color)
        {
            Image img = GetOrAddComponent<Image>(rt.gameObject);
            img.color = color;
            return img;
        }

        internal static TextMeshProUGUI FindOrCreateText(Transform parent, string name, string defaultText,
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

            TMP_FontAsset poppins = AlifFontSetup.FontAssetInstance();
            if (poppins != null)
            {
                tmp.font = poppins;
            }

            return tmp;
        }

        // ---------------------------------------------------------------
        // HELPERS: SerializedObject (untuk isi field private [SerializeField])
        // ---------------------------------------------------------------
        internal static void SetSerializedRef(Object target, string fieldName, Object value)
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

        private static void SetSerializedValue(Object target, string fieldName, string value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null) return;
            prop.stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void SetSerializedObjectList(Object target, string fieldName, System.Collections.Generic.List<Object> values)
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

        internal static Sprite GetOrCreateCircleSprite(string name, Color color, int size)
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
