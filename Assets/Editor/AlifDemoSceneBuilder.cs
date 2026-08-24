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
        // Stasiun_Luar.png (placeholder lama) diganti Stasiun_Depan.jpg (art final chapter 1)
        // begitu Player "keluar" stasiun.
        private const string ExteriorBackgroundSpritePath = "Assets/Sprites/Backgrounds/Stasiun_Depan.jpg";
        private const string WarungDepanSpritePath = "Assets/Sprites/Backgrounds/WarungBuSiti_Depan.jpg";
        // Asset PNG asli dari desain interior Warung Bu Siti. Pakai PNG agar detail pixel-art,
        // teks menu, dan garis furnitur tidak terkena artefak kompresi JPEG.
        private const string WarungDalamSpritePath = "Assets/Sprites/Backgrounds/WarungBuSiti_Interior.png";
        private const string TargetScenePath = "Assets/Scenes/SampleScene.unity";

        // Area eksterior ditaruh jauh di bawah interior (bukan scene terpisah, biar CameraFollow
        // otomatis "pindah" cuma dengan reposisi Player) — cukup jauh (20 unit) dari batas bawah
        // interior (halfH 4.72) supaya nggak pernah kelihatan bertumpuk di kamera.
        private static readonly Vector2 ExteriorOrigin = new Vector2(0f, -20f);

        // Warung Bu Siti ditaruh jauh ke kanan (bukan di bawah/nyambung visual sama Stasiun
        // Depan — pindah antar area di sini selalu lewat SceneDoor + fade, bukan jalan
        // nyambung), biar nggak pernah kelihatan bertumpuk di kamera sama area lain.
        private static readonly Vector2 WarungDepanOrigin = new Vector2(40f, -20f);
        private static readonly Vector2 WarungDalamOrigin = new Vector2(40f, -40f);

        // Stasiun_Depan.jpg, WarungBuSiti_Depan.jpg, & WarungBuSiti_Interior.jpg digambar lebih
        // "zoom in" (detail per tile lebih gede) dibanding tileset Stasiun_Interior.png — dipakai
        // apa adanya (PPU 200 sama), gambarnya jadi keliatan mini & Player kayak raksasa
        // dibanding kamera (orthoSize 3.2). Area-area chapter 1 ini di-scale up TRANSFORM-nya
        // (bukan Player) biar proporsinya pas ngisi layar — Player tetap ukuran normal, jadi
        // makin gede scale background-nya, makin "kecil" kelihatannya Player relatif ke
        // background (efek yang diminta), TANPA benar-benar nge-shrink sprite Player itu sendiri.
        // Nilai per area diminta user langsung (Stasiun Depan paling agresif, Warung Depan
        // malah dikecilin balik ke skala asli gambarnya, Warung Dalam di tengah-tengah).
        private const float StasiunDepanScale = 2.4f;
        private const float WarungDepanScale = 1.1f;
        private const float WarungDalamScale = 1.3f;

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
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.path != TargetScenePath)
            {
                Debug.LogError($"[Alif] Build Demo Scene dibatalkan: scene aktif '{activeScene.path}' bukan '{TargetScenePath}'. " +
                               "Buka SampleScene terlebih dahulu agar MainMenu atau scene lain tidak tertimpa.");
                return;
            }

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

            // 2c. Chapter 1: Warung Bu Siti (depan + dalam), terhubung dari Stasiun Depan.
            BuildWarungBuSiti();

            BuildDoors();

            // 3. Persistent managers.
            GameObject managersRoot = FindOrCreateRoot("_GameManagers");
            GetOrAddChild<GameManager>(managersRoot.transform, "GameManager");
            GetOrAddChild<SceneLoader>(managersRoot.transform, "SceneLoader");
            GetOrAddChild<TimeSystem>(managersRoot.transform, "TimeSystem");
            GetOrAddChild<EnergySystem>(managersRoot.transform, "EnergySystem");
            GetOrAddChild<CurrencySystem>(managersRoot.transform, "CurrencySystem");
            ScoreSystem scoreSystem = GetOrAddChild<ScoreSystem>(managersRoot.transform, "ScoreSystem");
            ConfigureSharedScoreBalance(scoreSystem);
            GetOrAddChild<InventorySystem>(managersRoot.transform, "InventorySystem");
            DialogueManager dialogueManager = GetOrAddChild<DialogueManager>(managersRoot.transform, "DialogueManager");

            // 4. Player.
            PlayerController playerController = BuildPlayer();
            SetupCameraFollow(playerController.transform);

            // 5. NPC (pakai karakter asli dari Assets/Sprites/Characters, hasil "Alif > 1) Import Character Art").
            BuildNpcs();

            // 5a. Chapter 1: Bu Siti sebagai kasir, Raka di dalam warung, serta titik meja
            // yang baru memicu konflik setelah pemain benar-benar memesan makanan.
            BuildWarungRestaurantStory(playerController);

            // 5b. Bangku/kursi yang bisa di-interact (nggak bisa ditembus + munculin monolog Alif).
            BuildBenches();

            // 5b-2. Loket Karcis (ambil voucher promo kereta) & ATM (tarik tunai dari saldo bank).
            BuildStationInteractables();

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

        private static void ConfigureSharedScoreBalance(ScoreSystem scoreSystem)
        {
            // Neraca selalu dimulai tepat di tengah. Semua pilihan konflik kemudian menggeser
            // sisi Finansial atau Syariah, sementara total dua bar tetap 100%.
            SetSerializedValue(scoreSystem, "_useSharedBalance", true);
            SetSerializedValue(scoreSystem, "_maxFinancialLogic", 100f);
            SetSerializedValue(scoreSystem, "_maxShariaCompliance", 100f);
            SetSerializedValue(scoreSystem, "_currentFinancialLogic", 50f);
            SetSerializedValue(scoreSystem, "_currentShariaCompliance", 50f);
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

        // Whitelist lantai Stasiun Interior. Kotak-kotak ini saling overlap di ambang pintu;
        // collider solid di atasnya tetap menutup tembok, loket, bangku, ATM, dan furnitur.
        // Dengan begitu void hitam tidak perlu ditutup oleh satu collider raksasa yang rawan
        // ikut memakan ubin lantai di sebelahnya.
        //
        // "Toilet" (kuadran kanan-atas, sebelah Informasi) sebelumnya tidak terdaftar sama
        // sekali di sini — Player jadi macet di ambang pintunya walau tidak ada tembok fisik,
        // karena footprint tujuan gagal ContainsFootprint (tidak ada whitelist yang menutupinya).
        // Ukuran tiap kotak juga sedikit dilebihkan (+0.2 per sumbu, center tetap) supaya ambang
        // pintu antar-ruangan overlap lebih lega — collider solid tetap jadi otoritas akhir untuk
        // tembok, jadi pelebaran whitelist ini tidak bisa bikin Player menembus dinding manapun.
        private static readonly (string name, Vector2 center, Vector2 size)[] StationInteriorWalkableAreas =
        {
            ("Informasi", new Vector2(-3.10f, 1.66f), new Vector2(4.90f, 2.48f)),
            ("Toilet", new Vector2(3.20f, 1.66f), new Vector2(4.70f, 2.48f)),
            ("KoridorTengah", new Vector2(0.15f, -0.85f), new Vector2(2.40f, 7.90f)),
            ("LoketKarcis", new Vector2(-3.10f, -2.18f), new Vector2(4.90f, 3.20f)),
            ("RuangTunggu", new Vector2(3.20f, -2.18f), new Vector2(4.70f, 3.20f)),
        };

        // Area krem berpola ubin di depan stasiun, plus jalan aspal di bawahnya (diminta bisa
        // dijalanin juga). Atap/badan gedung dan area di luar gambar tetap tidak termasuk lantai
        // yang boleh diinjak. Batas "Jalan" dianalisis dari sampel piksel Stasiun_Depan.jpg
        // (aspal ~y 610-820px dari 873px, PPU 200, pivot Center) — pas di bawah trotoar Plaza.
        private static readonly (string name, Vector2 center, Vector2 size)[] StationFrontWalkableAreas =
        {
            ("Plaza", new Vector2(0f, 0.22f), new Vector2(7.72f, 2.18f)),
            ("Jalan", new Vector2(0f, -1.39f), new Vector2(7.72f, 1.10f)),
        };

        // Collider detail untuk tiga peta chapter 1. Semua angka adalah koordinat lokal peta
        // (sudah mempertimbangkan pivot Center dan PPU 200), sehingga otomatis ikut skala
        // background masing-masing. Collider sengaja mengikuti inti benda yang menyentuh lantai
        // (kaki meja, alas pohon, kaki tiang), bukan seluruh gambar sprite/canopy/bayangan.
        // Lantai, paving, aspal, dan jalur pintu sengaja TIDAK masuk daftar agar tetap dilalui.
        // Footprint lain wajib solid supaya sprite karakter tidak tampak mengambang di atas prop.
        private static readonly (string name, Vector2 center, Vector2 size)[] StasiunFrontPropBlockers =
        {
            // Facade dipisah tiga agar badan bangunan solid sampai garis lantainya, tetapi
            // koridor pintu tengah tetap terbuka. Satu kotak lama berhenti terlalu tinggi,
            // sehingga kaki Alif bisa masuk ke dinding dan sprite-nya terlihat mengambang.
            ("BangunanAtas", new Vector2(0f, 1.79f), new Vector2(5.24f, 0.80f)),
            ("FacadeKiri", new Vector2(-1.75f, 1.00f), new Vector2(2.50f, 0.82f)),
            ("FacadeKanan", new Vector2(1.65f, 1.00f), new Vector2(2.30f, 0.82f)),
            ("LampuTrotoarKiri", new Vector2(-3.46f, 0.91f), new Vector2(0.16f, 0.20f)),
            ("PalemAtasKiri", new Vector2(-3.14f, 0.84f), new Vector2(0.20f, 0.20f)),
            ("PalemPintuKiri", new Vector2(-0.89f, 0.82f), new Vector2(0.20f, 0.20f)),
            ("PalemPintuKanan", new Vector2(0.88f, 0.82f), new Vector2(0.20f, 0.20f)),
            ("PalemAtasKanan", new Vector2(2.31f, 0.80f), new Vector2(0.20f, 0.20f)),
            ("PalemUjungKanan", new Vector2(3.59f, 0.79f), new Vector2(0.20f, 0.20f)),
            ("BangkuKanan", new Vector2(2.05f, -0.68f), new Vector2(0.62f, 0.16f)),
            ("PohonKiri", new Vector2(-3.28f, -0.60f), new Vector2(0.18f, 0.18f)),
            ("TiangListrikKiri", new Vector2(-3.25f, -0.55f), new Vector2(0.12f, 0.16f)),
            ("TiangListrikTengahKiri", new Vector2(-1.31f, -0.58f), new Vector2(0.12f, 0.16f)),
            ("PohonTengahKiri", new Vector2(-0.89f, -0.78f), new Vector2(0.18f, 0.18f)),
            ("PohonTengah", new Vector2(0.64f, -0.76f), new Vector2(0.18f, 0.18f)),
            ("TiangListrikTengahKanan", new Vector2(1.08f, -0.56f), new Vector2(0.12f, 0.16f)),
            ("TiangListrikKanan", new Vector2(3.27f, -0.55f), new Vector2(0.12f, 0.16f)),
            ("PohonKanan", new Vector2(3.54f, -0.76f), new Vector2(0.18f, 0.18f)),
        };

        private static readonly (string name, Vector2 center, Vector2 size)[] WarungFrontPropBlockers =
        {
            // Atap/facade dan counter dibuat terpisah agar pintu tengah tetap terbuka. Batas
            // bawah mengikuti kaki meja/pilar, bukan tepi awning, supaya Alif tidak bisa naik
            // ke meja seperti pada screenshot.
            ("AtapWarung", new Vector2(0.05f, 2.52f), new Vector2(3.82f, 1.82f)),
            ("CounterKiri", new Vector2(-0.98f, 0.78f), new Vector2(1.58f, 0.92f)),
            ("CounterKanan", new Vector2(0.86f, 0.78f), new Vector2(1.10f, 0.92f)),
            ("PilarKiri", new Vector2(-1.55f, 0.70f), new Vector2(0.18f, 1.15f)),
            ("PilarPintuKiri", new Vector2(-0.27f, 0.68f), new Vector2(0.16f, 0.92f)),
            ("PilarPintuKanan", new Vector2(0.42f, 0.68f), new Vector2(0.16f, 0.92f)),
            ("PilarKanan", new Vector2(1.35f, 0.62f), new Vector2(0.16f, 1.18f)),
            ("MejaLuar", new Vector2(2.53f, 1.16f), new Vector2(0.70f, 0.54f)),
            ("PangganganLuar", new Vector2(1.70f, 0.68f), new Vector2(0.48f, 0.58f)),
            ("MejaSampingPanggangan", new Vector2(2.06f, 0.60f), new Vector2(0.28f, 0.54f)),
            ("BangkuBiru", new Vector2(2.02f, 1.18f), new Vector2(0.20f, 0.20f)),
            ("BangkuCokelat", new Vector2(3.01f, 1.17f), new Vector2(0.20f, 0.20f)),
            ("BangkuDepanKiri", new Vector2(0.20f, 0.57f), new Vector2(0.20f, 0.20f)),
            ("BangkuDepanTengah", new Vector2(0.65f, 0.37f), new Vector2(0.20f, 0.20f)),
            ("BangkuDepanKanan", new Vector2(1.04f, 0.34f), new Vector2(0.20f, 0.20f)),
            ("PohonKiriBawah", new Vector2(-3.80f, 1.48f), new Vector2(0.18f, 0.18f)),
            ("PohonKiriAtas", new Vector2(-2.86f, 2.33f), new Vector2(0.18f, 0.18f)),
            ("PohonKiriTengah", new Vector2(-2.20f, 1.14f), new Vector2(0.18f, 0.18f)),
            ("PohonKanan", new Vector2(3.00f, 1.95f), new Vector2(0.20f, 0.20f)),
            ("PalemTengahJalan", new Vector2(0.15f, -2.54f), new Vector2(0.18f, 0.18f)),
        };

        private static readonly (string name, Vector2 center, Vector2 size)[] WarungInteriorPropBlockers =
        {
            // Dua bidang hitam di bawah gambar bukan lantai. Hanya koridor pintu di tengah
            // yang dibiarkan terbuka.
            ("VoidKiriBawah", new Vector2(-2.36f, -2.88f), new Vector2(3.26f, 1.04f)),
            ("VoidKananBawah", new Vector2(2.35f, -2.88f), new Vector2(3.30f, 1.04f)),

            // Dinding belakang dan semua counter dapur/kasir: ini mencegah kasus pada
            // screenshot ketika Alif bisa berdiri di atas grill atau masuk ke balik kasir.
            ("DindingBelakangKiri", new Vector2(-2.45f, 2.78f), new Vector2(3.10f, 1.23f)),
            ("DindingBelakangKanan", new Vector2(1.65f, 2.78f), new Vector2(4.70f, 1.23f)),
            ("CounterKasir", new Vector2(-3.08f, 1.72f), new Vector2(1.58f, 0.58f)),
            ("PilarDapur", new Vector2(-0.72f, 2.02f), new Vector2(0.34f, 2.22f)),
            ("GrillDanCounterMasak", new Vector2(1.02f, 1.68f), new Vector2(3.78f, 0.70f)),
            ("SteamerKanan", new Vector2(3.73f, 1.55f), new Vector2(0.28f, 0.66f)),
            ("CounterSajiKanan", new Vector2(2.90f, 0.44f), new Vector2(3.04f, 0.56f)),
            ("PilarTengah", new Vector2(-0.72f, -0.19f), new Vector2(0.34f, 1.78f)),

            // Collider meja mengikuti daun meja saja, bukan satu kotak besar yang menyatukan
            // meja dan kursi. Lorong di antara meja-kursi jadi bisa dilewati seperti visualnya.
            ("MejaMakanKiriAtas", new Vector2(-3.35f, 0.25f), new Vector2(0.64f, 0.30f)),
            ("KursiKiriAtasA", new Vector2(-3.62f, 0.66f), new Vector2(0.20f, 0.20f)),
            ("KursiKiriAtasB", new Vector2(-3.16f, 0.66f), new Vector2(0.20f, 0.20f)),
            ("KursiKiriAtasC", new Vector2(-3.66f, -0.06f), new Vector2(0.20f, 0.20f)),
            ("KursiKiriAtasD", new Vector2(-3.22f, -0.06f), new Vector2(0.20f, 0.20f)),
            ("MejaMakanTengahAtas", new Vector2(-1.60f, 0.24f), new Vector2(0.34f, 0.60f)),
            ("KursiTengahAtasKiri", new Vector2(-2.02f, 0.22f), new Vector2(0.20f, 0.24f)),
            ("KursiTengahAtasKanan", new Vector2(-1.18f, 0.22f), new Vector2(0.20f, 0.24f)),
            ("MejaMakanKiriTengah", new Vector2(-3.35f, -0.92f), new Vector2(0.64f, 0.30f)),
            ("KursiKiriTengahA", new Vector2(-3.62f, -0.52f), new Vector2(0.20f, 0.20f)),
            ("KursiKiriTengahB", new Vector2(-3.16f, -0.52f), new Vector2(0.20f, 0.20f)),
            ("KursiKiriTengahC", new Vector2(-3.66f, -1.22f), new Vector2(0.20f, 0.20f)),
            ("KursiKiriTengahD", new Vector2(-3.22f, -1.22f), new Vector2(0.20f, 0.20f)),
            ("MejaMakanTengah", new Vector2(-1.72f, -0.92f), new Vector2(0.34f, 0.56f)),
            ("MejaMakanKiriBawah", new Vector2(-3.20f, -2.02f), new Vector2(0.34f, 0.52f)),
            ("KursiKiriBawahKiri", new Vector2(-3.64f, -2.02f), new Vector2(0.20f, 0.24f)),
            ("KursiKiriBawahKanan", new Vector2(-2.78f, -2.02f), new Vector2(0.20f, 0.24f)),
            ("MejaMakanTengahBawah", new Vector2(-1.66f, -2.02f), new Vector2(0.34f, 0.52f)),
            ("KursiTengahBawahKiri", new Vector2(-2.08f, -2.02f), new Vector2(0.20f, 0.24f)),
            ("KursiBundarTengah", new Vector2(-1.72f, -1.32f), new Vector2(0.18f, 0.18f)),
            ("KursiBundarBawah", new Vector2(-0.96f, -2.26f), new Vector2(0.18f, 0.18f)),
            ("PapanMenu", new Vector2(1.25f, -1.01f), new Vector2(0.34f, 0.66f)),
            // Dua bangku dibuat terpisah supaya celah visual di tengahnya tetap menjadi jalan.
            ("BangkuRuangTungguKiri", new Vector2(2.12f, -1.10f), new Vector2(0.52f, 0.20f)),
            ("BangkuRuangTungguKanan", new Vector2(2.96f, -1.10f), new Vector2(0.52f, 0.20f)),
            ("TanamanRuangTunggu", new Vector2(3.72f, -1.42f), new Vector2(0.18f, 0.18f)),
            ("BangkuKananBawah", new Vector2(3.84f, -2.05f), new Vector2(0.20f, 0.64f)),
        };

        private static void BuildBackgroundColliders(GameObject background)
        {
            // Bersihkan collider lama dulu biar aman di-rebuild berulang kali.
            for (int i = background.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = background.transform.GetChild(i);
                if (child.name.StartsWith("VoidBlocker") || child.name.StartsWith("BoundaryWall") || child.name.StartsWith("WalkableArea"))
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }

            for (int i = 0; i < BackgroundVoidBlockers.Length; i++)
            {
                (Vector2 center, Vector2 size) = BackgroundVoidBlockers[i];
                AddBoxBlocker(background, $"VoidBlocker_{i}", center, size);
            }

            AddWalkableAreas(background, StationInteriorWalkableAreas);

            // Boundary luar: 4 dinding tipis di tepi gambar, biar Player nggak bisa keluar
            // dari keseluruhan gambar sama sekali (bukan cuma void di dalamnya).
            const float halfW = 5.555f;
            const float halfH = 4.72f;
            const float wallThickness = 0.2f;

            AddBoxBlocker(background, "BoundaryWall_Top", new Vector2(0, halfH + wallThickness / 2f), new Vector2(halfW * 2 + wallThickness * 2, wallThickness));
            AddBoxBlocker(background, "BoundaryWall_Bottom", new Vector2(0, -halfH - wallThickness / 2f), new Vector2(halfW * 2 + wallThickness * 2, wallThickness));
            AddBoxBlocker(background, "BoundaryWall_Left", new Vector2(-halfW - wallThickness / 2f, 0), new Vector2(wallThickness, halfH * 2 + wallThickness * 2));
            AddBoxBlocker(background, "BoundaryWall_Right", new Vector2(halfW + wallThickness / 2f, 0), new Vector2(wallThickness, halfH * 2 + wallThickness * 2));

            AddCameraBounds(background, Vector2.zero, new Vector2(halfW, halfH));
        }

        private static void AddBoxBlocker(GameObject parent, string name, Vector2 localPosition, Vector2 size)
        {
            var blocker = new GameObject(name);
            blocker.transform.SetParent(parent.transform, false);
            blocker.transform.localPosition = localPosition;

            BoxCollider2D collider = blocker.AddComponent<BoxCollider2D>();
            collider.size = size;
        }

        private static void AddPropBlockers(GameObject parent, (string name, Vector2 center, Vector2 size)[] blockers)
        {
            for (int i = 0; i < blockers.Length; i++)
            {
                (string name, Vector2 center, Vector2 size) = blockers[i];
                AddBoxBlocker(parent, $"PropBlocker_{name}", center, size);
            }
        }

        // Dipasang satu per background root (bukan child) supaya CameraFollow bisa menemukan
        // batas area yang sedang ditempati Player dan tidak pernah menampilkan hitam kosong di
        // luar gambar — lihat CameraBounds.cs. center/halfExtents di sini SUDAH dalam world
        // space (sudah memperhitungkan origin & scale area), bukan koordinat lokal seperti
        // PropBlocker/WalkableArea.
        private static void AddCameraBounds(GameObject root, Vector2 center, Vector2 halfExtents)
        {
            CameraBounds bounds = GetOrAddComponent<CameraBounds>(root);
            SetSerializedValue(bounds, "_center", center);
            SetSerializedValue(bounds, "_halfExtents", halfExtents);
        }

        private static void AddWalkableAreas(GameObject parent, (string name, Vector2 center, Vector2 size)[] areas)
        {
            for (int i = 0; i < areas.Length; i++)
            {
                (string name, Vector2 center, Vector2 size) = areas[i];
                GameObject area = new GameObject($"WalkableArea_{name}");
                area.transform.SetParent(parent.transform, false);
                area.transform.localPosition = center;

                BoxCollider2D collider = area.AddComponent<BoxCollider2D>();
                collider.size = size;
                collider.isTrigger = true;
                area.AddComponent<WalkableArea>();
            }
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
            // Art-nya digambar lebih "zoom in" dibanding tileset interior — di-scale up biar
            // ngisi layar kamera (orthoSize 3.2) dgn proporsi yg sama, nggak keliatan mini
            // ketutup banyak void hitam di sekitarnya. Blocker anak-anaknya (localPosition/size)
            // otomatis ikut ke-scale bareng transform ini, nggak perlu diubah manual.
            exterior.transform.localScale = Vector3.one * StasiunDepanScale;

            SpriteRenderer sr = GetOrAddComponent<SpriteRenderer>(exterior);
            sr.sprite = exteriorSprite;
            sr.sortingOrder = SortingOrderGround;

            for (int i = exterior.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = exterior.transform.GetChild(i);
                if (child.name.StartsWith("BoundaryWall") || child.name.StartsWith("PropBlocker") || child.name.StartsWith("WalkableArea"))
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }

            // Stasiun_Depan.jpg 1600x873px, PPU 200 -> half extents (4.0, 2.1825).
            const float halfW = 4.0f;
            const float halfH = 2.1825f;
            const float wallThickness = 0.2f;

            AddBoxBlocker(exterior, "BoundaryWall_Top", new Vector2(0, halfH + wallThickness / 2f), new Vector2(halfW * 2 + wallThickness * 2, wallThickness));
            AddBoxBlocker(exterior, "BoundaryWall_Bottom", new Vector2(0, -halfH - wallThickness / 2f), new Vector2(halfW * 2 + wallThickness * 2, wallThickness));
            AddBoxBlocker(exterior, "BoundaryWall_Left", new Vector2(-halfW - wallThickness / 2f, 0), new Vector2(wallThickness, halfH * 2 + wallThickness * 2));
            AddBoxBlocker(exterior, "BoundaryWall_Right", new Vector2(halfW + wallThickness / 2f, 0), new Vector2(wallThickness, halfH * 2 + wallThickness * 2));
            AddPropBlockers(exterior, StasiunFrontPropBlockers);
            AddWalkableAreas(exterior, StationFrontWalkableAreas);
            AddCameraBounds(exterior, ExteriorOrigin, new Vector2(halfW, halfH) * StasiunDepanScale);
        }

        // ---------------------------------------------------------------
        // WARUNG BU SITI (depan + dalam) — area baru chapter 1, terhubung dari Stasiun Depan
        // lewat Door_KeWarung, dan depan<->dalam lewat Door_MasukWarung/Door_KeluarWarung
        // (lihat BuildDoors). Sama kayak Stasiun Depan, belum ada analisis pixel detail buat
        // furniture/pohon di dalamnya — boundary luar doang biar Player nggak jalan ke void di
        // luar gambar. Bisa ditambah VoidBlocker presisi nanti kalau perlu.
        // ---------------------------------------------------------------
        private static void BuildWarungBuSiti()
        {
            // WarungBuSiti_Depan.jpg 1600x1511px, PPU 200 -> half extents (4.0, 3.7775).
            BuildAreaBackground("Background_WarungDepan", WarungDepanSpritePath, WarungDepanOrigin, 4.0f, 3.7775f, WarungDepanScale);

            // WarungBuSiti_Interior.jpg 1600x1359px, PPU 200 -> half extents (4.0, 3.3975).
            BuildAreaBackground("Background_WarungDalam", WarungDalamSpritePath, WarungDalamOrigin, 4.0f, 3.3975f, WarungDalamScale);
        }

        // ---------------------------------------------------------------
        // CERITA WARUNG BU SITI
        // ---------------------------------------------------------------
        // Alur interaksi sengaja dipisah jadi Kasir -> Meja. Dengan demikian dialog konflik
        // tidak mungkin berjalan saat Alif baru masuk warung; pemain harus memilih makanan,
        // menyelesaikan pembayaran, lalu duduk menunggu pesanan terlebih dahulu.
        private static void BuildWarungRestaurantStory(PlayerController playerController)
        {
            CharacterData alif = GetOrCreateAlifCharacterData();
            CharacterData buSiti = AssetDatabase.LoadAssetAtPath<CharacterData>($"{CharacterDataFolder}/CharacterData_bu_siti.asset");
            CharacterData raka = AssetDatabase.LoadAssetAtPath<CharacterData>($"{CharacterDataFolder}/CharacterData_raka.asset");

            if (buSiti == null || raka == null)
            {
                Debug.LogWarning("[Alif] CharacterData Bu Siti atau Raka belum tersedia. Jalankan 'Alif > 1) Import Character Art'.");
                return;
            }

            DialogueData menu = BuildWarungMenuDialogue(alif, buSiti);
            DialogueData order = BuildWarungOrderDialogue(alif, buSiti);
            DialogueData conflict = BuildWarungConflictDialogue(alif, buSiti, raka);
            DialogueData alreadyOrdered = BuildWarungInfoDialogue(
                "DialogueData_WarungSudahPesan", alif, buSiti,
                ("Bu Siti", buSiti, "Pesananmu sedang Ibu siapkan, Alif. Duduk dulu di meja dekat jendela, ya."));
            DialogueData needOrder = BuildWarungInfoDialogue(
                "DialogueData_WarungBelumPesan", alif, buSiti,
                ("Alif", alif, "Sebelum duduk, aku pesan makanan ke Bu Siti dulu."));
            DialogueData finished = BuildWarungInfoDialogue(
                "DialogueData_WarungSelesai", alif, buSiti,
                ("Bu Siti", buSiti, "Terima kasih, Alif. Ibu akan mulai dari catatan kas dan kontrak yang jelas, bukan janji yang tergesa-gesa."),
                ("Alif", alif, "Sama-sama, Bu. Usaha yang sehat harus menjaga angka sekaligus amanah."));

            GameObject storyRoot = FindOrCreateRoot("WarungBuSitiStory");
            RestaurantStoryTrigger story = GetOrAddComponent<RestaurantStoryTrigger>(storyRoot);
            SetSerializedRef(story, "_orderDialogue", order);
            SetSerializedRef(story, "_conflictDialogue", conflict);
            SetSerializedRef(story, "_alreadyOrderedDialogue", alreadyOrdered);
            SetSerializedRef(story, "_needOrderDialogue", needOrder);
            SetSerializedRef(story, "_finishedDialogue", finished);
            SetSerializedRef(story, "_menuDialogue", menu);
            SetSerializedRef(story, "_alifData", alif);

            // Posisi ditentukan terhadap peta WarungBuSiti_Interior (origin 40,-40; scale 1.3).
            // Bu Siti ada di depan kasir. Raka diposisikan di ubin kosong, bukan di atas counter.
            BuildRestaurantCharacter(storyRoot.transform, "BuSiti_Kasir", buSiti, new Vector2(37.25f, -38.35f), story, RestaurantStoryPoint.PointType.Cashier, true);

            // Raka belum "kenal" Alif di awal cerita — nonaktif dari awal scene, baru diaktifkan
            // RestaurantStoryTrigger.BeginConflictWithTimeSkip() begitu Alif duduk menunggu
            // pesanan (selagi layar hitam "Beberapa saat kemudian..."), bukan langsung terlihat
            // begitu Alif masuk warung.
            GameObject rakaGO = BuildRestaurantCharacter(storyRoot.transform, "Raka_Tamu", raka, new Vector2(40.90f, -40.72f), null, RestaurantStoryPoint.PointType.Cashier, false);
            rakaGO.SetActive(false);

            // Sebelumnya (39.45,-40.85) kegeser terlalu dekat ke PilarTengah (local -0.72,-0.19
            // di WarungInteriorPropBlockers) — panah "InteractionArrow"-nya jadi nampak nempel
            // di pilar/tembok, bukan di meja. Digeser ke meja "MejaMakanTengahAtas" (local
            // -1.60,0.24) yang beneran ada di dekat jendela sesuai dialog Bu Siti ("meja dekat
            // jendela") dan cukup jauh dari pilar.
            BuildRestaurantSeat(storyRoot.transform, story, new Vector2(37.92f, -39.69f));
            BuildRestaurantMenu(storyRoot.transform, story, new Vector2(41.62f, -41.98f));
            BuildWarungGrillAroma(storyRoot.transform);

            SetSerializedRef(story, "_rakaObject", rakaGO);
            SetSerializedRef(story, "_playerController", playerController);
        }

        // Titik interact di depan panggangan sate/ayam geprek Warung Dalam — cuma monolog Alif
        // soal wangi masakannya, nggak ada efek gameplay. Posisi ada di dalam footprint
        // "GrillDanCounterMasak" (WarungInteriorPropBlockers) yang sudah solid, jadi Player nggak
        // perlu bisa berdiri tepat di titik ini — cukup dalam radius interact dari tepi counter.
        private static readonly Vector2 WarungGrillInteractPosition = new Vector2(40.0f, -37.82f);

        private static void BuildWarungGrillAroma(Transform parent)
        {
            DialogueData dialogue = GetOrCreateGrillAromaDialogue();
            CharacterData alifData = GetOrCreateAlifCharacterData();

            GameObject grill = FindOrCreateWorldChild(parent, "GrillMasakan_Interact");
            grill.transform.position = new Vector3(WarungGrillInteractPosition.x, WarungGrillInteractPosition.y, 0f);
            grill.layer = LayerIndexInteractable;

            BoxCollider2D collider = GetOrAddComponent<BoxCollider2D>(grill);
            collider.size = new Vector2(1.0f, 0.35f);

            InteractableObject interactable = GetOrAddComponent<InteractableObject>(grill);
            SetSerializedRef(interactable, "_dialogue", dialogue);
            SetSerializedRef(interactable, "_speakerData", alifData);

            AddInteractionArrow(grill.transform, new Vector2(0f, 0.4f));
        }

        private static DialogueData GetOrCreateGrillAromaDialogue()
        {
            string path = $"{DialogueDataFolder}/DialogueData_GrillAroma.asset";
            DialogueData data = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
            if (data != null)
            {
                return data;
            }

            data = ScriptableObject.CreateInstance<DialogueData>();
            data.Lines.Add(new DialogueLine
            {
                SpeakerName = "Alif",
                Text = "Wangi banget... ayam geprek dan sate yang lagi dipanggang ini bikin makin lapar."
            });

            EnsureFolder(DialogueDataFolder);
            AssetDatabase.CreateAsset(data, path);
            return data;
        }

        private static GameObject BuildRestaurantCharacter(Transform parent, string name, CharacterData characterData, Vector2 position,
            RestaurantStoryTrigger story, RestaurantStoryPoint.PointType pointType, bool isInteractable)
        {
            GameObject character = FindOrCreateWorldChild(parent, name);
            character.transform.position = new Vector3(position.x, position.y, 0f);

            SpriteRenderer renderer = GetOrAddComponent<SpriteRenderer>(character);
            renderer.sprite = characterData.Portrait;
            renderer.sortingOrder = SortingOrderCharacters;

            Animator animator = GetOrAddComponent<Animator>(character);
            animator.runtimeAnimatorController = characterData.AnimatorController;

            CircleCollider2D collider = GetOrAddComponent<CircleCollider2D>(character);
            collider.radius = 0.34f;
            collider.isTrigger = isInteractable;

            if (!isInteractable)
            {
                character.layer = 0;
                return character;
            }

            character.layer = LayerIndexInteractable;
            RestaurantStoryPoint point = GetOrAddComponent<RestaurantStoryPoint>(character);
            SetSerializedRef(point, "_story", story);
            SetSerializedValue(point, "_pointType", (int)pointType);
            AddInteractionArrow(character.transform, new Vector2(0f, 1.0f));
            return character;
        }

        private static void BuildRestaurantSeat(Transform parent, RestaurantStoryTrigger story, Vector2 position)
        {
            GameObject seat = FindOrCreateWorldChild(parent, "MejaTungguPesanan");
            seat.transform.position = new Vector3(position.x, position.y, 0f);
            seat.layer = LayerIndexInteractable;

            BoxCollider2D collider = GetOrAddComponent<BoxCollider2D>(seat);
            collider.size = new Vector2(0.95f, 0.72f);
            collider.isTrigger = true;

            RestaurantStoryPoint point = GetOrAddComponent<RestaurantStoryPoint>(seat);
            SetSerializedRef(point, "_story", story);
            SetSerializedValue(point, "_pointType", (int)RestaurantStoryPoint.PointType.Seat);
            AddInteractionArrow(seat.transform, new Vector2(0f, 0.72f));
        }

        private static void BuildRestaurantMenu(Transform parent, RestaurantStoryTrigger story, Vector2 position)
        {
            // Titik trigger diletakkan tepat di depan papan menu agar Alif tidak perlu masuk
            // ke collider fisik papan untuk membacanya.
            GameObject menu = FindOrCreateWorldChild(parent, "PapanMenu_Interact");
            menu.transform.position = new Vector3(position.x, position.y, 0f);
            menu.layer = LayerIndexInteractable;

            BoxCollider2D collider = GetOrAddComponent<BoxCollider2D>(menu);
            collider.size = new Vector2(0.58f, 0.32f);
            collider.isTrigger = true;

            RestaurantStoryPoint point = GetOrAddComponent<RestaurantStoryPoint>(menu);
            SetSerializedRef(point, "_story", story);
            SetSerializedValue(point, "_pointType", (int)RestaurantStoryPoint.PointType.Menu);
            AddInteractionArrow(menu.transform, new Vector2(0f, 0.54f));
        }

        private static DialogueData BuildWarungInfoDialogue(string fileName, CharacterData alif, CharacterData buSiti,
            params (string speakerName, CharacterData speakerData, string text)[] lines)
        {
            DialogueData data = GetOrCreateStoryDialogue(fileName);
            foreach (var line in lines)
            {
                AddStoryLine(data, line.speakerName, line.speakerData, line.text);
            }
            return data;
        }

        private static DialogueData BuildWarungMenuDialogue(CharacterData alif, CharacterData buSiti)
        {
            DialogueData data = GetOrCreateStoryDialogue("DialogueData_WarungLihatMenu");
            AddStoryLine(data, "Alif", alif, "Aku cek menu Warung Bu Siti dulu.");
            AddStoryLine(data, "Menu Warung Bu Siti", null,
                "AYAM GEPREK + NASI — Rp18.000\nNASI TELUR — Rp12.000\nES TEH — Rp6.000\n\nPilih pesananmu di kasir.");
            AddStoryLine(data, "Bu Siti", buSiti, "Kalau sudah memilih, datang ke kasir ya, Alif. Ibu catat pesananmu di sana.");
            return data;
        }

        private static DialogueData BuildWarungOrderDialogue(CharacterData alif, CharacterData buSiti)
        {
            DialogueData data = GetOrCreateStoryDialogue("DialogueData_WarungPesanMakanan");
            AddStoryLine(data, "Bu Siti", buSiti, "Selamat datang, Alif. Mau makan apa? Hari ini lauknya baru matang.");
            DialogueLine menu = AddStoryLine(data, "Alif", alif, "Aku pilih menu yang mana, ya?");

            int branchStart = data.Lines.Count;
            const int linesPerBranch = 2;
            int rejoinIndex = branchStart + 3 * linesPerBranch;
            AddOrderBranch(data, alif, buSiti, "Aku pesan ayam geprek dan nasi, Bu.", "Siap, ayam geprek satu. Totalnya Rp18.000.", rejoinIndex);
            AddOrderBranch(data, alif, buSiti, "Nasi telur saja, Bu. Yang sederhana.", "Boleh, nasi telur satu. Totalnya Rp12.000.", rejoinIndex);
            AddOrderBranch(data, alif, buSiti, "Es teh dulu, Bu. Nanti makan setelahnya.", "Siap, es teh satu. Totalnya Rp6.000.", rejoinIndex);

            menu.Choices.Add(new DialogueChoice { ChoiceText = "Ayam geprek + nasi — Rp18.000", NextLineIndex = branchStart, EventId = "warung.order.ayam_geprek" });
            menu.Choices.Add(new DialogueChoice { ChoiceText = "Nasi telur — Rp12.000", NextLineIndex = branchStart + linesPerBranch, EventId = "warung.order.nasi_telur" });
            menu.Choices.Add(new DialogueChoice { ChoiceText = "Es teh — Rp6.000", NextLineIndex = branchStart + linesPerBranch * 2, EventId = "warung.order.es_teh" });

            AddStoryLine(data, "Bu Siti", buSiti, "Terima kasih. Duduk dulu di meja dekat jendela, ya. Pesananmu segera Ibu antar.");
            AddStoryLine(data, "Alif", alif, "Baik, Bu. Aku tunggu di sana.");
            return data;
        }

        private static void AddOrderBranch(DialogueData data, CharacterData alif, CharacterData buSiti, string alifLine, string buSitiLine, int rejoinIndex)
        {
            int buSitiIndex = data.Lines.Count + 1;
            AddStoryLine(data, "Alif", alif, alifLine, buSitiIndex);
            AddStoryLine(data, "Bu Siti", buSiti, buSitiLine, rejoinIndex);
        }

        private static DialogueData BuildWarungConflictDialogue(CharacterData alif, CharacterData buSiti, CharacterData raka)
        {
            DialogueData data = GetOrCreateStoryDialogue("DialogueData_WarungKonflikBuSiti");

            // Bagian pembuka: konflik baru terdengar ketika Alif telah duduk menunggu makanan.
            AddStoryLine(data, "Narator", null, "Alif duduk di meja dekat jendela. Suara sendok dan kompor pelan-pelan tenggelam oleh percakapan tegang di dekat kasir.");
            AddStoryLine(data, "Alif", alif, "Hmm... Bu Siti kelihatan gelisah. Orang di dekat kasir itu Raka, kan?");
            AddStoryLine(data, "Bu Siti", buSiti, "Mas Raka, Ibu memang butuh modal. Tapi kenapa formulirnya harus ditandatangani sekarang juga?");
            AddStoryLine(data, "Raka", raka, "Karena kuotanya terbatas, Bu. PT Cuan Meroket siap setor Rp5 juta hari ini. Ibu tinggal kembalikan keuntungan tetap Rp2 juta setiap bulan. Modal aman, tidak perlu memikirkan rugi.");
            AddStoryLine(data, "Bu Siti", buSiti, "Dua juta per bulan? Warung Ibu kadang ramai, kadang sepi. Kalau hujan panjang, pemasukan bisa turun.");
            AddStoryLine(data, "Raka", raka, "Justru itu enaknya program kami. Apa pun keadaan warung, keuntungan Ibu sudah pasti. Tidak perlu repot hitung untung-rugi.");
            AddStoryLine(data, "Alif", alif, "Maaf menyela, Bu. Janji hasil tetap sementara usaha bisa rugi itu bukan hal kecil. Kita perlu tahu akad dan risikonya dengan jelas.");
            AddStoryLine(data, "Raka", raka, "Alif, ini dunia nyata. Pengusaha kecil butuh uang cepat, bukan kuliah panjang soal istilah.");
            AddStoryLine(data, "Bu Siti", buSiti, "Bukan cuma soal kuliah, Mas. Minggu ini freezer rusak, harga ayam naik, dan pemasok minta pembayaran tunai. Ibu takut karyawan Ibu ikut terdampak.");
            AddStoryLine(data, "Alif", alif, "Aku paham urgensinya. Karena itu keputusan cepat tetap harus punya dasar yang jujur.");
            AddStoryLine(data, "Narator", null, "Di meja kasir ada kontrak satu halaman. Huruf kecil di bagian bawah menyebut denda tambahan jika pembayaran terlambat, tetapi tidak menjelaskan ke mana dana diputar.");
            AddStoryLine(data, "Raka", raka, "Itu hanya administrasi. Tanda tangan saja, Bu. Nanti semuanya beres.");
            AddStoryLine(data, "Bu Siti", buSiti, "Alif, kalau kamu di posisi Ibu, apa yang kamu lakukan dulu?");

            AddThreeWayDecision(data, "Alif", alif, alif, new[]
            {
                new StoryDecisionOption(
                    "Ambil dana cepat; yang penting freezer dan stok segera aman.",
                    "Kita ambil saja sekarang, Bu. Kondisi kas sedang mendesak dan nanti detailnya bisa dipikirkan.",
                    raka, "Nah, itu baru keputusan bisnis. Waktu tidak menunggu orang yang terlalu banyak bertanya.",
                    14f, 0f, 0f, "Kebutuhan uang cepat membuat neraca condong ke Finansial."),
                new StoryDecisionOption(
                    "Tolak semua bentuk pembiayaan supaya tidak ada risiko syariah.",
                    "Sebaiknya jangan ambil pembiayaan apa pun, Bu. Menolak semuanya pasti lebih aman.",
                    buSiti, "Ibu lega tidak menandatangani, tapi freezer, stok, dan gaji besok tetap perlu jalan keluar.",
                    0f, 14f, 0f, "Menghindari seluruh risiko membuat neraca condong ke Syariah."),
                new StoryDecisionOption(
                    "Jeda tanda tangan; cek arus kas dan cari akad pembiayaan yang transparan.",
                    "Kita jangan tanda tangan terburu-buru. Catat kebutuhan kas, cek kemampuan bayar, lalu bandingkan akad yang jelas dan adil.",
                    buSiti, "Ibu bisa menahan diri sebentar kalau kita punya langkah yang nyata, bukan sekadar menunggu.",
                    0f, 0f, 12f, "Jalan tengah mengarahkan keputusan kembali ke 50% / 50%.")
            });

            AddStoryLine(data, "Alif", alif, "Pertama, kita pisahkan kebutuhan yang nyata dari janji di brosur. Berapa uang yang dibutuhkan, untuk apa, dan dari mana pelunasannya akan datang?");
            AddStoryLine(data, "Bu Siti", buSiti, "Freezer butuh sekitar Rp3 juta. Sisanya untuk stok dan uang muka pemasok. Kalau stok aman, pelanggan makan siang tetap datang.");
            AddStoryLine(data, "Alif", alif, "Jadi masalahnya arus kas jangka pendek, bukan alasan untuk menerima hasil yang katanya pasti tanpa risiko.");
            AddStoryLine(data, "Raka", raka, "Angka-angka itu justru membuktikan Ibu butuh kami. Dengan Rp5 juta ini, masalah selesai dalam lima menit.");
            AddStoryLine(data, "Alif", alif, "Masalah bisa terlihat selesai hari ini, tapi denda dan kewajiban tetapnya bisa membuat warung tertekan bulan depan.");
            AddStoryLine(data, "Bu Siti", buSiti, "Ibu punya catatan penjualan tiga minggu terakhir. Tidak rapi, tapi semua pemasukan dan belanja Ibu tulis.");
            AddStoryLine(data, "Narator", null, "Alif melihat catatan sederhana itu: penjualan cukup stabil pada hari kerja, sementara pengeluaran terbesar datang dari bahan baku dan listrik.");
            AddStoryLine(data, "Alif", alif, "Catatan ini penting, Bu. Dari sini kita bisa membuat proyeksi yang masuk akal, bukan mengandalkan angka yang dijanjikan orang lain.");
            AddStoryLine(data, "Raka", raka, "Proyeksi bisa meleset. Program kami tidak meleset karena kami menjamin hasilnya.");
            AddStoryLine(data, "Alif", alif, "Justru jaminan hasil tanpa penjelasan usaha, pembagian risiko, dan dasar perhitungannya adalah tanda bahaya.");
            AddStoryLine(data, "Bu Siti", buSiti, "Kalau begitu, bagaimana Ibu membedakan bantuan modal yang sehat dengan jebakan yang dibungkus kata investasi?");

            AddThreeWayDecision(data, "Alif", alif, alif, new[]
            {
                new StoryDecisionOption(
                    "Fokus pada hasil bulanan; minta Raka menunda denda saja.",
                    "Yang penting hasil bulanannya masuk, Mas. Dendanya saja yang bisa dinegosiasikan.",
                    raka, "Bisa dibicarakan, asal Bu Siti tanda tangan sekarang. Detail akan kami kirim setelah dana cair.",
                    12f, 0f, 0f, "Janji pemasukan instan kembali menarik neraca ke Finansial."),
                new StoryDecisionOption(
                    "Hindari keuntungan usaha dan biarkan warung berhenti sementara.",
                    "Lebih baik warung berhenti dulu. Kalau ada peluang untung dan risiko, sebaiknya tidak usah diambil.",
                    buSiti, "Ibu menghargai kehati-hatian itu, tapi menutup warung juga berarti pelanggan dan dua karyawan kehilangan penghasilan.",
                    0f, 12f, 0f, "Kehati-hatian tanpa rencana usaha menggeser neraca ke Syariah."),
                new StoryDecisionOption(
                    "Minta rincian akad: tujuan dana, biaya, risiko, jangka waktu, dan mekanisme bagi hasil.",
                    "Kita minta rincian tertulis dulu: dana dipakai untuk apa, biaya apa saja, risiko siapa yang menanggung, dan bagaimana bagi hasilnya dihitung.",
                    raka, "Itu terlalu banyak pertanyaan untuk dana kecil. Klien lain biasanya langsung percaya pada nama perusahaan kami.",
                    0f, 0f, 12f, "Kejelasan akad menarik pilihan kembali ke jalan tengah.")
            });

            AddStoryLine(data, "Alif", alif, "Usaha tidak harus memilih antara bertahan secara finansial atau taat pada prinsip. Keduanya perlu dibuat saling menguatkan.");
            AddStoryLine(data, "Bu Siti", buSiti, "Berarti Ibu boleh mencari modal, asal tidak menutup mata pada isi perjanjian dan kemampuan warung membayar?");
            AddStoryLine(data, "Alif", alif, "Betul. Misalnya cicilan pembelian freezer dengan harga dan jadwal yang disepakati di awal, atau kerja sama bagi hasil yang jelas atas usaha yang nyata. Kita tetap harus cek lembaga dan kontraknya.");
            AddStoryLine(data, "Raka", raka, "Kalian membuat semua hal terasa sulit. Kalau terus bicara, kesempatan ini habis pukul empat sore.");
            AddStoryLine(data, "Bu Siti", buSiti, "Tekanan seperti itu yang membuat Ibu tidak nyaman, Mas. Ibu perlu memahami sebelum menyetujui.");
            AddStoryLine(data, "Alif", alif, "Bu Siti juga berhak memeriksa legalitas perusahaan, meminta salinan kontrak, dan tidak menyerahkan data atau uang sebelum semua terang.");
            AddStoryLine(data, "Raka", raka, "Saya tidak punya waktu menunggu rapat panjang. Jadi, keputusan akhirnya apa?");

            AddThreeWayDecision(data, "Alif", alif, alif, new[]
            {
                new StoryDecisionOption(
                    "Tanda tangani dulu agar dana masuk; perbaiki kontraknya nanti.",
                    "Bu, tanda tangani dulu saja supaya dana masuk. Nanti kalau ada masalah kita perbaiki kontraknya.",
                    raka, "Pilihan yang realistis. Saya siapkan formulirnya sekarang.",
                    10f, 0f, 0f, "Tekanan menyelesaikan hari ini menggeser neraca ke Finansial."),
                new StoryDecisionOption(
                    "Tolak setiap kerja sama dan jual peralatan untuk menutup kebutuhan.",
                    "Lebih baik kita menolak semuanya dan menjual peralatan yang ada. Setidaknya tidak ada kontrak yang meragukan.",
                    buSiti, "Itu mungkin aman dari kontrak buruk, tapi warung bisa kehilangan alat yang justru dipakai untuk bangkit.",
                    0f, 10f, 0f, "Penolakan total menggeser neraca ke Syariah."),
                new StoryDecisionOption(
                    "Tolak tekanan Raka; amankan stok hari ini dan temui lembaga pembiayaan syariah/koperasi dengan catatan kas.",
                    "Kita tolak tekanan ini, Bu. Hari ini kita atur stok yang paling penting; besok kita bawa catatan kas ke lembaga tepercaya untuk membahas pembiayaan yang transparan.",
                    buSiti, "Itu masuk akal. Ibu tidak mengabaikan kebutuhan warung, tapi juga tidak menyerahkan masa depannya pada janji yang tidak jelas.",
                    0f, 0f, 12f, "Langkah nyata dan transparan menarik neraca ke titik seimbang.")
            });

            AddStoryLine(data, "Raka", raka, "Kalau begitu saya pergi. Tapi jangan menyesal ketika pemasok tidak mau menunggu.");
            AddStoryLine(data, "Alif", alif, "Kami tidak menolak solusi, Mas. Kami hanya menolak keputusan yang disembunyikan di balik janji hasil pasti dan tekanan waktu.");
            AddStoryLine(data, "Narator", null, "Raka merapikan brosurnya. Senyumnya memudar ketika Bu Siti mengembalikan pulpen tanpa menandatangani apa pun.");
            AddStoryLine(data, "Bu Siti", buSiti, "Terima kasih, Alif. Tadi Ibu hampir percaya karena panik. Sekarang Ibu tahu panik bukan alasan untuk melewatkan pertanyaan penting.");
            AddStoryLine(data, "Alif", alif, "Kita mulai dari yang bisa Ibu kendalikan: catatan kas harian, prioritas belanja, dan kontrak yang bisa Ibu pahami sebelum menyetujui.");
            AddStoryLine(data, "Bu Siti", buSiti, "Besok Ibu hubungi pemasok untuk jadwal yang lebih realistis. Setelah itu kita cari pilihan pembiayaan yang jelas dan sesuai kebutuhan warung.");
            AddStoryLine(data, "Alif", alif, "Itu langkah yang baik, Bu. Menjaga usaha tetap hidup dan menjaga amanah tidak harus saling mengalahkan.");
            AddStoryLine(data, "Narator", null, "Pesanan Alif akhirnya tiba. Di HUD, dua bar menunjukkan arah keputusan yang ia ambil: totalnya selalu 100%, dan jalan sehat berada di tengah.");
            AddStoryLine(data, "Bu Siti", buSiti, "Makan dulu, Alif. Setelah ini Ibu punya keberanian untuk menyelesaikan masalah dengan kepala dingin.");
            return data;
        }

        private struct StoryDecisionOption
        {
            public string ChoiceText;
            public string AlifLine;
            public CharacterData Responder;
            public string ResponseLine;
            public float FinancialChange;
            public float ShariaChange;
            public float BalanceCorrection;
            public string OutcomeText;

            public StoryDecisionOption(string choiceText, string alifLine, CharacterData responder, string responseLine,
                float financialChange, float shariaChange, float balanceCorrection, string outcomeText)
            {
                ChoiceText = choiceText;
                AlifLine = alifLine;
                Responder = responder;
                ResponseLine = responseLine;
                FinancialChange = financialChange;
                ShariaChange = shariaChange;
                BalanceCorrection = balanceCorrection;
                OutcomeText = outcomeText;
            }
        }

        private static void AddThreeWayDecision(DialogueData data, string speakerName, CharacterData speakerData, CharacterData alif,
            StoryDecisionOption[] options)
        {
            DialogueLine prompt = AddStoryLine(data, speakerName, speakerData, "Pilih respons Alif:");
            int branchStart = data.Lines.Count;
            const int linesPerBranch = 2;
            int rejoinIndex = branchStart + options.Length * linesPerBranch;

            for (int i = 0; i < options.Length; i++)
            {
                StoryDecisionOption option = options[i];
                int responseIndex = data.Lines.Count + 1;
                AddStoryLine(data, "Alif", alif, option.AlifLine, responseIndex);
                AddStoryLine(data, option.Responder != null ? option.Responder.CharacterName : "Narator", option.Responder, option.ResponseLine, rejoinIndex);
                prompt.Choices.Add(new DialogueChoice
                {
                    ChoiceText = option.ChoiceText,
                    NextLineIndex = branchStart + i * linesPerBranch,
                    FinancialLogicChange = option.FinancialChange,
                    ShariaComplianceChange = option.ShariaChange,
                    BalanceCorrection = option.BalanceCorrection,
                    OutcomeText = option.OutcomeText
                });
            }
        }

        private static DialogueData GetOrCreateStoryDialogue(string fileName)
        {
            string path = $"{DialogueDataFolder}/{fileName}.asset";
            DialogueData data = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<DialogueData>();
                EnsureFolder(DialogueDataFolder);
                AssetDatabase.CreateAsset(data, path);
            }

            data.Lines.Clear();
            data.OnCompleteEventId = string.Empty;
            EditorUtility.SetDirty(data);
            return data;
        }

        private static DialogueLine AddStoryLine(DialogueData data, string speakerName, CharacterData speakerData, string text, int nextLineIndex = -1)
        {
            DialogueLine line = new DialogueLine
            {
                SpeakerName = speakerName,
                SpeakerData = speakerData,
                Text = text,
                NextLineIndex = nextLineIndex
            };
            data.Lines.Add(line);
            return line;
        }

        // Helper generik "background + boundary luar doang" — dipakai Warung Depan & Dalam,
        // pola sama persis kayak BuildStasiunLuar tapi bisa dipakai berkali-kali buat area beda.
        private static void BuildAreaBackground(string rootName, string spritePath, Vector2 origin, float halfW, float halfH, float scale)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null)
            {
                Debug.LogWarning($"[Alif] Background sprite tidak ditemukan di '{spritePath}', dilewati.");
                return;
            }

            GameObject root = FindOrCreateRoot(rootName);
            root.transform.position = new Vector3(origin.x, origin.y, 0f);
            // Lihat komentar StasiunDepanScale/WarungDepanScale/WarungDalamScale — blocker anak-anaknya
            // (localPosition/size) otomatis ikut ke-scale bareng transform ini, nggak perlu
            // diubah manual.
            root.transform.localScale = Vector3.one * scale;

            SpriteRenderer sr = GetOrAddComponent<SpriteRenderer>(root);
            sr.sprite = sprite;
            sr.sortingOrder = SortingOrderGround;

            for (int i = root.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = root.transform.GetChild(i);
                if (child.name.StartsWith("BoundaryWall") || child.name.StartsWith("PropBlocker"))
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }

            const float wallThickness = 0.2f;
            AddBoxBlocker(root, "BoundaryWall_Top", new Vector2(0, halfH + wallThickness / 2f), new Vector2(halfW * 2 + wallThickness * 2, wallThickness));
            AddBoxBlocker(root, "BoundaryWall_Bottom", new Vector2(0, -halfH - wallThickness / 2f), new Vector2(halfW * 2 + wallThickness * 2, wallThickness));
            AddBoxBlocker(root, "BoundaryWall_Left", new Vector2(-halfW - wallThickness / 2f, 0), new Vector2(wallThickness, halfH * 2 + wallThickness * 2));
            AddBoxBlocker(root, "BoundaryWall_Right", new Vector2(halfW + wallThickness / 2f, 0), new Vector2(wallThickness, halfH * 2 + wallThickness * 2));

            if (rootName == "Background_WarungDepan")
            {
                AddPropBlockers(root, WarungFrontPropBlockers);
            }
            else if (rootName == "Background_WarungDalam")
            {
                AddPropBlockers(root, WarungInteriorPropBlockers);
            }

            AddCameraBounds(root, origin, new Vector2(halfW, halfH) * scale);
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

            // Stasiun_Depan.jpg jauh lebih pendek (halfH 2.1825) daripada placeholder lama
            // (Stasiun_Luar.png, halfH 4.585) — titik spawn/pintu ikut digeser masuk biar tetap
            // di dalam gambar, di sekitar area tangga pintu masuk stasiun (dekat bagian atas).
            // Offset-nya dikali StasiunDepanScale/WarungDepanScale/WarungDalamScale (tergantung fisiknya ada di
            // area mana) karena area itu (beda dari interior) di-scale up transform-nya — lihat
            // komentar di deklarasinya.
            GameObject exteriorSpawnGO = FindOrCreateWorldChild(doorsRoot.transform, "ExteriorSpawn");
            exteriorSpawnGO.transform.position = new Vector3(0f, ExteriorOrigin.y + 0.7f * StasiunDepanScale, 0f);

            BuildDoorTrigger(doorsRoot.transform, "Door_KeluarStasiun", new Vector2(0f, -4.55f), new Vector2(1.2f, 0.3f), exteriorSpawnGO.transform, "Keluar dari stasiun?");
            BuildDoorTrigger(doorsRoot.transform, "Door_MasukStasiun", new Vector2(0f, ExteriorOrigin.y + 1.1f * StasiunDepanScale), new Vector2(1.2f, 0.3f) * StasiunDepanScale, interiorSpawnGO.transform, "Masuk ke stasiun?");

            // --- Stasiun Depan <-> Warung Bu Siti Depan (chapter 1: "jalan ke kanan") ---
            GameObject stasiunEdgeSpawnGO = FindOrCreateWorldChild(doorsRoot.transform, "StasiunEdgeSpawn");
            stasiunEdgeSpawnGO.transform.position = new Vector3(3.0f * StasiunDepanScale, ExteriorOrigin.y - 0.3f * StasiunDepanScale, 0f);

            GameObject warungEdgeSpawnGO = FindOrCreateWorldChild(doorsRoot.transform, "WarungEdgeSpawn");
            warungEdgeSpawnGO.transform.position = new Vector3(WarungDepanOrigin.x - 3.0f * WarungDepanScale, WarungDepanOrigin.y - 0.3f * WarungDepanScale, 0f);

            BuildDoorTrigger(doorsRoot.transform, "Door_KeWarung", new Vector2(3.6f * StasiunDepanScale, ExteriorOrigin.y - 0.3f * StasiunDepanScale), new Vector2(0.5f, 1.5f) * StasiunDepanScale, warungEdgeSpawnGO.transform, "Jalan ke Warung Bu Siti?");
            BuildDoorTrigger(doorsRoot.transform, "Door_KeStasiun", new Vector2(WarungDepanOrigin.x - 3.6f * WarungDepanScale, WarungDepanOrigin.y - 0.3f * WarungDepanScale), new Vector2(0.5f, 1.5f) * WarungDepanScale, stasiunEdgeSpawnGO.transform, "Balik ke Stasiun?");

            // --- Warung Bu Siti Depan <-> Dalam (interact di depan warung) ---
            GameObject warungLuarSpawnGO = FindOrCreateWorldChild(doorsRoot.transform, "WarungLuarSpawn");
            warungLuarSpawnGO.transform.position = new Vector3(WarungDepanOrigin.x + 0.08f * WarungDepanScale, WarungDepanOrigin.y + 0.18f * WarungDepanScale, 0f);

            GameObject warungDalamSpawnGO = FindOrCreateWorldChild(doorsRoot.transform, "WarungDalamSpawn");
            warungDalamSpawnGO.transform.position = new Vector3(WarungDalamOrigin.x - 0.5f * WarungDalamScale, WarungDalamOrigin.y - 2.6f * WarungDalamScale, 0f);

            BuildDoorTrigger(doorsRoot.transform, "Door_MasukWarung", new Vector2(WarungDepanOrigin.x + 0.08f * WarungDepanScale, WarungDepanOrigin.y + 0.48f * WarungDepanScale), new Vector2(0.48f, 0.34f) * WarungDepanScale, warungDalamSpawnGO.transform, "Masuk ke Warung Bu Siti?");
            BuildDoorTrigger(doorsRoot.transform, "Door_KeluarWarung", new Vector2(WarungDalamOrigin.x - 0.5f * WarungDalamScale, WarungDalamOrigin.y - 2.9f * WarungDalamScale), new Vector2(1.0f, 0.4f) * WarungDalamScale, warungLuarSpawnGO.transform, "Keluar dari Warung Bu Siti?");
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
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            CapsuleCollider2D collider = GetOrAddComponent<CapsuleCollider2D>(player);
            // Top-down: yang bertabrakan dengan dunia hanya "kaki" Alif, bukan seluruh
            // gambar badannya. Collider lama 0.7 x 0.9 membuat setiap prop terasa punya
            // zona tak terlihat yang besar dan menutup celah antar meja.
            collider.direction = CapsuleDirection2D.Vertical;
            collider.size = new Vector2(0.32f, 0.24f);
            collider.offset = new Vector2(0f, -0.34f);

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
        // LOKET KARCIS (ambil voucher promo kereta) & ATM (tarik tunai) — posisi hitbox
        // dianalisis dari Stasiun_Interior.png (2222x1888px, PPU 200, pivot Center): meja loket
        // karcis di ruang kiri-bawah, mesin ATM biru di ruang kanan-bawah (ruang Display).
        // ---------------------------------------------------------------
        private static readonly (Vector2 center, Vector2 size) LoketKarcisPlacement = (new Vector2(-3.0f, -1.7f), new Vector2(2.4f, 0.7f));
        private static readonly (Vector2 center, Vector2 size) AtmPlacement = (new Vector2(1.9f, -1.6f), new Vector2(0.6f, 0.7f));

        private static void BuildStationInteractables()
        {
            GameObject background = FindOrCreateRoot("Background");
            CharacterData alifData = GetOrCreateAlifCharacterData();

            // --- Loket Karcis: interact -> dapat "Voucher Promo Kereta" masuk inventory ---
            GameObject loket = FindOrCreateWorldChild(background.transform, "LoketKarcis");
            loket.transform.localPosition = LoketKarcisPlacement.center;
            loket.layer = LayerIndexInteractable;

            BoxCollider2D loketCollider = GetOrAddComponent<BoxCollider2D>(loket);
            loketCollider.size = LoketKarcisPlacement.size;

            Sprite voucherIcon = GetOrCreateTicketIconSprite("Icon_VoucherPromo",
                new Color(0.85f, 0.62f, 0.18f, 1f), new Color(0.98f, 0.9f, 0.75f, 1f), 64, 44);

            ItemPickup voucherPickup = GetOrAddComponent<ItemPickup>(loket);
            SetSerializedValue(voucherPickup, "_itemName", "Voucher Promo Kereta");
            SetSerializedRef(voucherPickup, "_itemIcon", voucherIcon);
            SetSerializedRef(voucherPickup, "_pickupDialogue", GetOrCreateVoucherDialogue());
            SetSerializedRef(voucherPickup, "_speakerData", alifData);

            AddInteractionArrow(loket.transform, new Vector2(0f, LoketKarcisPlacement.size.y / 2f + 0.25f));

            // --- ATM: interact -> buka AtmUI, tarik tunai dari saldo bank ---
            GameObject atm = FindOrCreateWorldChild(background.transform, "Atm");
            atm.transform.localPosition = AtmPlacement.center;
            atm.layer = LayerIndexInteractable;

            BoxCollider2D atmCollider = GetOrAddComponent<BoxCollider2D>(atm);
            atmCollider.size = AtmPlacement.size;

            GetOrAddComponent<AtmController>(atm);

            AddInteractionArrow(atm.transform, new Vector2(0f, AtmPlacement.size.y / 2f + 0.25f));
        }

        private static DialogueData GetOrCreateVoucherDialogue()
        {
            string path = $"{DialogueDataFolder}/DialogueData_VoucherPromo.asset";
            DialogueData data = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
            if (data != null)
            {
                return data;
            }

            data = ScriptableObject.CreateInstance<DialogueData>();
            data.Lines.Add(new DialogueLine
            {
                SpeakerName = "Alif",
                Text = "Ada voucher promo kereta di loket! Lumayan, aku ambil buat jaga-jaga."
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

            // Popup ATM (tarik tunai) — singleton, ditemukan runtime lewat AtmUI.Instance oleh
            // AtmController, nggak perlu di-assign ke komponen lain di sini.
            BuildAtmPopup(canvasGO.transform, audioManager, clickSfx);

            // DialogueUI diletakkan di root Canvas (selalu aktif) supaya tetap subscribe ke event
            // DialogueManager walaupun panel visualnya (dialoguePanel) sedang disembunyikan.
            DialogueUI dialogueUI = GetOrAddComponent<DialogueUI>(canvasGO);
            SetSerializedRef(dialogueUI, "_dialogueBoxRoot", dialoguePanel);
            SetSerializedRef(dialogueUI, "_panelRect", dialoguePanel.GetComponent<RectTransform>());
            SetSerializedRef(dialogueUI, "_speakerNameText", dialoguePanel.transform.Find("SpeakerNameText")?.GetComponent<TMP_Text>());
            SetSerializedRef(dialogueUI, "_dialogueText", dialoguePanel.transform.Find("DialogueText")?.GetComponent<TMP_Text>());
            SetSerializedRef(dialogueUI, "_portraitImage", dialoguePortraitImage);
            SetSerializedRef(dialogueUI, "_nextButton", dialoguePanel.transform.Find("NextButton")?.GetComponent<Button>());
            SetSerializedRef(dialogueUI, "_choiceButtonContainer", dialoguePanel.transform.Find("ChoiceButtonContainer"));
            SetSerializedRef(dialogueUI, "_choiceButtonPrefab", GetOrCreateChoiceButtonPrefab());
            SetSerializedRef(dialogueUI, "_advanceCatcherButton", advanceCatcherButton);
            SetSerializedValue(dialogueUI, "_normalPanelHeight", NormalPanelHeight);
            SetSerializedValue(dialogueUI, "_choiceTopReservedSpace", ChoiceTopReservedSpace);
            SetSerializedValue(dialogueUI, "_choiceBottomPadding", ChoiceBottomPadding);
            SetSerializedValue(dialogueUI, "_choiceButtonSpacing", ChoiceButtonSpacing);
            SetSerializedValue(dialogueUI, "_maxChoicePanelHeight", MaxChoicePanelHeight);
            SetSerializedValue(dialogueUI, "_minChoiceButtonHeight", MinChoiceButtonHeight);
            SetSerializedValue(dialogueUI, "_choiceButtonVerticalPadding", ChoiceButtonVerticalPadding);
            SetSerializedRef(dialogueUI, "_audioManager", audioManager);
            SetSerializedRef(dialogueUI, "_buttonClickSfx", clickSfx);

            // Kontrol on-screen (joystick, tombol interact "E") & bar inventory sengaja
            // disembunyikan sementara dialog aktif — nggak relevan (movement udah terkunci) dan
            // cuma nutup-nutupin layar visual novel.
            SetSerializedRef(dialogueUI, "_joystickRoot", joystickGO);
            SetSerializedRef(dialogueUI, "_interactButtonRoot", interactButtonGO);
            SetSerializedRef(dialogueUI, "_inventoryPanelRoot", inventoryPanel);
            SetSerializedRef(dialogueUI, "_pauseButtonRoot", pauseButtonGO);

            BuildScoreBalanceFeedback(canvasGO.transform, canvasGO);

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
            SetSerializedRef(fadeController, "_fadeText", loadingText);
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

            // Overlay-nya SENGAJA dibiarkan aktif — sama alasannya kayak QuitConfirmationUI di
            // atas (visibility lewat CanvasGroup, bukan SetActive, biar Awake()/Instance-nya
            // selalu ke-set pas scene load).
            CanvasGroup canvasGroup = GetOrAddComponent<CanvasGroup>(overlay);

            TravelConfirmationUI travelConfirmation = GetOrAddComponent<TravelConfirmationUI>(overlay);
            SetSerializedRef(travelConfirmation, "_canvasGroup", canvasGroup);
            SetSerializedRef(travelConfirmation, "_messageText", messageText);
            SetSerializedRef(travelConfirmation, "_confirmButton", confirmButton);
            SetSerializedRef(travelConfirmation, "_cancelButton", cancelButton);

            AddClickSfxListener(confirmButton, audioManager, clickSfx);
            AddClickSfxListener(cancelButton, audioManager, clickSfx);

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        // Popup ATM — saldo bank + tombol tarik tunai beberapa nominal + "Ambil Semua". Tarik
        // tunai mindahin saldo dari CurrencySystem.BankBalance ke uang kantong (lihat AtmUI).
        private static void BuildAtmPopup(Transform canvasTransform, AudioManager audioManager, AudioClip clickSfx)
        {
            GameObject overlay = FindOrCreateChild(canvasTransform, "AtmPopup");
            RectTransform overlayRT = overlay.GetComponent<RectTransform>();
            StretchFull(overlayRT);
            AddImage(overlayRT, new Color(0f, 0f, 0f, 0.6f));
            overlay.transform.SetAsLastSibling();

            GameObject card = FindOrCreateChild(overlay.transform, "Card");
            RectTransform cardRT = card.GetComponent<RectTransform>();
            SetRect(cardRT, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420, 520));
            AddImage(cardRT, new Color(0.16f, 0.12f, 0.08f, 0.97f));

            TMP_Text balanceText = FindOrCreateText(card.transform, "BalanceText", "Saldo ATM: Rp 1.000.000",
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f), new Vector2(0, -30), new Vector2(-32, 40), 18, TextAlignmentOptions.Center);
            balanceText.fontStyle = FontStyles.Bold;

            Button withdraw50 = BuildAtmButton(card.transform, "Withdraw50k", "Rp 50.000", new Vector2(-102, -90), new Vector2(180, 40));
            Button withdraw100 = BuildAtmButton(card.transform, "Withdraw100k", "Rp 100.000", new Vector2(102, -90), new Vector2(180, 40));
            Button withdraw200 = BuildAtmButton(card.transform, "Withdraw200k", "Rp 200.000", new Vector2(-102, -138), new Vector2(180, 40));
            Button withdraw500 = BuildAtmButton(card.transform, "Withdraw500k", "Rp 500.000", new Vector2(102, -138), new Vector2(180, 40));
            Button withdrawAll = BuildAtmButton(card.transform, "WithdrawAll", "AMBIL SEMUA", new Vector2(0, -190), new Vector2(360, 40));
            Button closeButton = BuildAtmButton(card.transform, "CloseButton", "TUTUP", new Vector2(0, -238), new Vector2(200, 34));

            var withdrawButtons = new System.Collections.Generic.List<Object> { withdraw50, withdraw100, withdraw200, withdraw500 };

            // Overlay-nya SENGAJA dibiarkan aktif — sama alasannya kayak QuitConfirmationUI /
            // TravelConfirmationUI (visibility lewat CanvasGroup, bukan SetActive, biar
            // Awake()/Instance-nya selalu ke-set pas scene load, bukan ditunda sampai nggak
            // pernah kepanggil).
            CanvasGroup canvasGroup = GetOrAddComponent<CanvasGroup>(overlay);

            AtmUI atmUI = GetOrAddComponent<AtmUI>(overlay);
            SetSerializedRef(atmUI, "_canvasGroup", canvasGroup);
            SetSerializedRef(atmUI, "_balanceText", balanceText);
            SetSerializedRef(atmUI, "_closeButton", closeButton);
            SetSerializedRef(atmUI, "_withdrawAllButton", withdrawAll);
            SetSerializedObjectList(atmUI, "_withdrawButtons", withdrawButtons);

            foreach (Button button in new[] { withdraw50, withdraw100, withdraw200, withdraw500, withdrawAll, closeButton })
            {
                AddClickSfxListener(button, audioManager, clickSfx);
            }

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        private static Button BuildAtmButton(Transform parent, string name, string label, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject go = BuildSimpleButton(parent, name, label);
            SetRect(go.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition, size);
            return go.GetComponent<Button>();
        }

        private static GameObject BuildHUD(Transform canvasTransform)
        {
            GameObject panel = FindOrCreateChild(canvasTransform, "HUD_Panel");
            RectTransform panelRT = panel.GetComponent<RectTransform>() ?? panel.AddComponent<RectTransform>();
            SetRect(panelRT, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -20), new Vector2(280, 220));
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
            // perlu label; sekarang ada 3 bar sekaligus, tanpa label bakal ambigu). Tiap label
            // juga dikasih angka persentase di ujung kanan baris yang sama — bar visual doang
            // susah dibedain pas nilainya udah tinggal sedikit (misal energi kritis).
            (Slider energySlider, TextMeshProUGUI energyPercentText) = BuildLabeledSlider(panel.transform, "Energy", "Energi", -34, new Color(0.45f, 0.75f, 0.35f, 1f));
            (Slider financialLogicSlider, TextMeshProUGUI financialLogicPercentText) = BuildLabeledSlider(panel.transform, "FinancialLogic", "Logika Finansial", -68, new Color(0.35f, 0.6f, 0.85f, 1f));
            (Slider shariaComplianceSlider, TextMeshProUGUI shariaCompliancePercentText) = BuildLabeledSlider(panel.transform, "ShariaCompliance", "Kepatuhan Syariah", -102, new Color(0.85f, 0.65f, 0.25f, 1f));

            TextMeshProUGUI balanceHint = FindOrCreateText(panel.transform, "BalanceHint", "NERACA: 100% TOTAL • IDEAL 50% / 50%",
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(12, -136), new Vector2(-12, 14), 9, TextAlignmentOptions.Left);
            balanceHint.color = new Color(1f, 1f, 1f, 0.58f);

            FindOrCreateText(panel.transform, "MoneyLabel", "Uang",
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(12, 32), new Vector2(-12, 14), 11, TextAlignmentOptions.Left)
                .color = new Color(1f, 1f, 1f, 0.75f);

            TextMeshProUGUI moneyText = FindOrCreateText(panel.transform, "MoneyText", "Rp 150",
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(12, 8), new Vector2(-12, 22), 16, TextAlignmentOptions.Left);

            HUDController hud = GetOrAddComponent<HUDController>(panel);
            SetSerializedRef(hud, "_dayWeekText", dayWeekText);
            SetSerializedRef(hud, "_energySlider", energySlider);
            SetSerializedRef(hud, "_energyPercentText", energyPercentText);
            SetSerializedRef(hud, "_financialLogicSlider", financialLogicSlider);
            SetSerializedRef(hud, "_financialLogicPercentText", financialLogicPercentText);
            SetSerializedRef(hud, "_shariaComplianceSlider", shariaComplianceSlider);
            SetSerializedRef(hud, "_shariaCompliancePercentText", shariaCompliancePercentText);
            SetSerializedRef(hud, "_moneyText", moneyText);

            return panel;
        }

        private static void BuildScoreBalanceFeedback(Transform canvasTransform, GameObject canvasGO)
        {
            // Root-nya sengaja bukan child Dialogue_Panel supaya feedback tetap tampak saat
            // pilihan baru saja menutup/membuka pergantian baris dialog.
            GameObject panel = FindOrCreateChild(canvasTransform, "ScoreBalanceFeedback");
            RectTransform panelRT = panel.GetComponent<RectTransform>();
            SetRect(panelRT, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(180, -34), new Vector2(450, 76));
            AddImage(panelRT, new Color(0.08f, 0.06f, 0.04f, 0.91f));

            TextMeshProUGUI feedbackText = FindOrCreateText(panel.transform, "FeedbackText", "",
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-20, -10), 12, TextAlignmentOptions.Center);

            ScoreBalanceFeedbackUI feedbackUI = GetOrAddComponent<ScoreBalanceFeedbackUI>(canvasGO);
            SetSerializedRef(feedbackUI, "_panelRoot", panel);
            SetSerializedRef(feedbackUI, "_messageText", feedbackText);
            panel.SetActive(false);
        }

        // Label kecil + progress bar di bawahnya, dipakai buat Energy, Financial Logic (skor
        // logika bisnis argumen pemain di minigame AI Detektif), dan Sharia Compliance (skor
        // bebas Riba/Gharar/Maysir) — beda warna fill per bar biar gampang dibedain sekilas.
        private static (Slider slider, TextMeshProUGUI percentText) BuildLabeledSlider(Transform parent, string name, string label, float topY, Color fillColor)
        {
            FindOrCreateText(parent, $"{name}Label", label,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(12, topY), new Vector2(-68, 14), 11, TextAlignmentOptions.Left)
                .color = new Color(1f, 1f, 1f, 0.75f);

            // Angka persentase di ujung kanan baris label yang sama — HUDController mengisi ini
            // tiap kali sistem terkait berubah (lihat Handle*Changed).
            TextMeshProUGUI percentText = FindOrCreateText(parent, $"{name}PercentText", "100%",
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-12, topY), new Vector2(50, 14), 11, TextAlignmentOptions.Right);
            percentText.color = new Color(1f, 1f, 1f, 0.92f);
            percentText.fontStyle = FontStyles.Bold;

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

            return (slider, percentText);
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

        // Nilai-nilai ini juga di-set ke field DialogueUI yang sama namanya (lihat pemanggilan
        // SetSerializedValue di BuildCanvas) supaya panel dialog & tombol pilihan selalu sinkron
        // dengan layout yang dibangun di sini — satu sumber angka, bukan dua yang harus diubah
        // manual berbarengan tiap kali di-tweak.
        private const float ChoiceTopReservedSpace = 78f; // ruang di atas container: nama pembicara + baris "Pilih respons Alif:".
        private const float ChoiceButtonSpacing = 8f;
        private const float ChoiceBottomPadding = 16f;
        private const float NormalPanelHeight = 180f;
        private const float MaxChoicePanelHeight = 460f;
        private const float MinChoiceButtonHeight = 44f;
        private const float ChoiceButtonVerticalPadding = 16f;

        private static (GameObject, Image) BuildDialoguePanel(Transform canvasTransform, AudioManager audioManager, AudioClip clickSfx)
        {
            GameObject panel = FindOrCreateChild(canvasTransform, "Dialogue_Panel");
            RectTransform panelRT = panel.GetComponent<RectTransform>() ?? panel.AddComponent<RectTransform>();
            SetRect(panelRT, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 110), new Vector2(900, NormalPanelHeight));
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

            // Container-nya sengaja anchor top-left TANPA stretch vertikal (anchorMin.y ==
            // anchorMax.y) — dulu ini fixed 60px dari bawah panel, jadi begitu ada 3 pilihan
            // (ukuran normal cerita chapter 1), tombol2-nya overflow ke LUAR box dialog dan
            // kelihatan sebagai teks polos nempel di atas dunia game (nggak ada kotak/warna
            // sama sekali). Sekarang tingginya di-hitung & di-set manual dari DialogueUI
            // (lihat ResizePanelForChoices) berdasarkan jumlah & panjang teks tiap pilihan,
            // lalu panel dialog-nya ikut membesar ke atas supaya semuanya tetap di dalam box.
            GameObject choiceContainer = FindOrCreateChild(panel.transform, "ChoiceButtonContainer");
            RectTransform choiceRT = choiceContainer.GetComponent<RectTransform>() ?? choiceContainer.AddComponent<RectTransform>();
            SetRect(choiceRT, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(textLeft, -ChoiceTopReservedSpace), new Vector2(-(textLeft + 16), 0));
            VerticalLayoutGroup vlg = GetOrAddComponent<VerticalLayoutGroup>(choiceContainer);
            vlg.spacing = ChoiceButtonSpacing;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
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

        // Rebuild PENUH tiap kali dipanggil (bukan cuma "kalau belum ada") — style lama (kotak
        // abu polos 200x28, teks center, nggak ada padding) itu sebabnya waktu ada 3 pilihan
        // panjang, tombolnya kepotong/numpuk jadi teks polos tanpa kotak. Style baru: warna
        // senada HUD (coklat hangat), teks rata-kiri + word-wrap + padding, tinggi minimum lebih
        // besar (dipakai LayoutElement, tinggi aktualnya dihitung ulang per baris di DialogueUI
        // berdasarkan panjang teks — lihat ResizePanelForChoices), dan warna hover/pressed biar
        // ada umpan balik visual pas disentuh.
        private static Button GetOrCreateChoiceButtonPrefab()
        {
            GameObject existingAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ChoicePrefabPath);
            GameObject temp = existingAsset != null ? Object.Instantiate(existingAsset) : new GameObject("ChoiceButtonTemplate", typeof(RectTransform));
            temp.name = "ChoiceButtonTemplate";

            RectTransform rt = temp.GetComponent<RectTransform>() ?? temp.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(820, MinChoiceButtonHeight);

            Image bgImage = AddImage(rt, new Color(0.36f, 0.24f, 0.14f, 0.95f));

            Button button = GetOrAddComponent<Button>(temp);
            button.targetGraphic = bgImage;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.36f, 0.24f, 0.14f, 0.95f);
            colors.highlightedColor = new Color(0.52f, 0.36f, 0.20f, 1f);
            colors.pressedColor = new Color(0.24f, 0.15f, 0.08f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            LayoutElement layoutElement = GetOrAddComponent<LayoutElement>(temp);
            layoutElement.minHeight = MinChoiceButtonHeight;

            TextMeshProUGUI label = FindOrCreateText(temp.transform, "Label", "Pilihan",
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0.5f), Vector2.zero, Vector2.zero, 14, TextAlignmentOptions.MidlineLeft);
            label.enableWordWrapping = true;
            label.margin = new Vector4(16, 8, 16, 8);
            label.raycastTarget = false;

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

        private static void SetSerializedValue(Object target, string fieldName, bool value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null) return;
            prop.boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetSerializedValue(Object target, string fieldName, int value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null) return;
            prop.intValue = value;
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

        private static void SetSerializedValue(Object target, string fieldName, Vector2 value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null) return;
            prop.vector2Value = value;
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

        // Ikon "tiket/voucher" sederhana buat item inventory (placeholder original, disintesis
        // dari kode — bukan reuse art dari sumber lain) — persegi rounded-corner dengan garis
        // putus-putus vertikal (kesan sobekan tiket) di sepertiga lebarnya.
        internal static Sprite GetOrCreateTicketIconSprite(string name, Color baseColor, Color perforationColor, int width, int height)
        {
            string relativePath = $"Assets/Sprites/UI/{name}.png";
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(relativePath);
            if (existing != null)
            {
                return existing;
            }

            EnsureFolder("Assets/Sprites/UI");

            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            const int cornerRadius = 8;
            int perforationX = width / 3;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float edgeAlpha = RoundedRectEdgeAlpha(x, y, width, height, cornerRadius);
                    Color pixel = baseColor;
                    pixel.a *= edgeAlpha;

                    bool onPerforationLine = Mathf.Abs(x - perforationX) <= 1;
                    bool dashOn = (y / 4) % 2 == 0;
                    if (onPerforationLine && dashOn && edgeAlpha > 0.5f)
                    {
                        pixel = perforationColor;
                        pixel.a = baseColor.a;
                    }

                    pixels[y * width + x] = pixel;
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

        // Alpha 0..1 buat 1 piksel (x,y) dalam persegi rounded-corner ukuran width x height,
        // radius sudut r — 1 penuh di tengah, di-soften 1px persis di lengkungan sudut.
        private static float RoundedRectEdgeAlpha(int x, int y, int width, int height, int r)
        {
            float px = x + 0.5f;
            float py = y + 0.5f;

            bool inLeft = px < r;
            bool inRight = px > width - r;
            bool inTop = py < r;
            bool inBottom = py > height - r;

            if ((inLeft || inRight) && (inTop || inBottom))
            {
                float cx = inLeft ? r : width - r;
                float cy = inTop ? r : height - r;
                float dist = Vector2.Distance(new Vector2(px, py), new Vector2(cx, cy));
                return Mathf.Clamp01(r - dist);
            }

            return 1f;
        }
    }
}
