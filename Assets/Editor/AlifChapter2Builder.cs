using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Alif.Characters;
using Alif.Core;
using Alif.Dialogue;
using Alif.Player;
using Alif.World;

namespace Alif.EditorTools
{
    /// <summary>Membangun gameplay Chapter 2 di kamar kos Dimas beserta dialog bercabangnya.</summary>
    public static class AlifChapter2Builder
    {
        private const string SourceScenePath = "Assets/Scenes/SampleScene.unity";
        private const string TargetScenePath = "Assets/Scenes/Chapter2Gameplay.unity";
        private const string DialogueFolder = "Assets/ScriptableObjects/Dialogue";
        private const string MainDialoguePath = DialogueFolder + "/DialogueData_Chapter2_DimasPinjol.asset";
        private const string FinishedDialoguePath = DialogueFolder + "/DialogueData_Chapter2_DimasSelesai.asset";
        private const string InteriorPath = "Assets/Sprites/Backgrounds/KosKosan_Interior.png";
        private const int InteractableLayer = 7;

        [MenuItem("Alif/8) Build Chapter 2 Gameplay Scene")]
        public static void BuildChapter2Gameplay()
        {
            EditorSceneManager.SaveOpenScenes();
            AlifDemoSceneBuilder.EnsureFolder("Assets/Scenes");

            if (!File.Exists(TargetScenePath))
            {
                if (!AssetDatabase.CopyAsset(SourceScenePath, TargetScenePath))
                {
                    Debug.LogError("[Alif] Gagal menyalin SampleScene untuk Chapter 2.");
                    return;
                }
                AssetDatabase.Refresh();
            }

            EditorSceneManager.OpenScene(TargetScenePath);
            StripChapter1World();

            CharacterData alif = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/ScriptableObjects/Characters/CharacterData_Alif.asset");
            CharacterData dimas = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/ScriptableObjects/Characters/CharacterData_dimas.asset");
            DialogueData mainDialogue = BuildMainDialogue(alif, dimas);
            DialogueData finishedDialogue = BuildFinishedDialogue(alif, dimas);

            BuildRoom();
            PlayerController player = ConfigurePlayer();
            Chapter2StoryController story = BuildStory(mainDialogue, finishedDialogue, dimas);
            BuildDimas(story, dimas);
            ConfigureCamera(player.transform);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            RegisterInBuildSettings();
            Debug.Log("[Alif] Chapter2Gameplay selesai: kamar kos, collider, Dimas, dan 6 pilihan bercabang sudah dibangun.");
        }

        public static void BuildChapter2GameplayCLI()
        {
            BuildChapter2Gameplay();
        }

        private static void StripChapter1World()
        {
            var keep = new HashSet<string> { "Main Camera", "Global Light 2D", "_GameManagers", "Player", "Canvas", "EventSystem" };
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (!keep.Contains(root.name))
                {
                    Object.DestroyImmediate(root);
                }
            }
        }

        private static PlayerController ConfigurePlayer()
        {
            GameObject player = GameObject.Find("Player");
            player.transform.position = new Vector3(-2.25f, -2.45f, 0f);

            OpeningMonologueTrigger oldOpening = player.GetComponent<OpeningMonologueTrigger>();
            if (oldOpening != null)
            {
                Object.DestroyImmediate(oldOpening);
            }

            return player.GetComponent<PlayerController>();
        }

        private static void BuildRoom()
        {
            GameObject room = new GameObject("Background_Chapter2Kos");
            SpriteRenderer renderer = room.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(InteriorPath);
            renderer.sortingOrder = 0;

            // Hanya bidang ubin kamar yang menjadi whitelist lantai. Dinding dan area hitam
            // otomatis tidak dapat diinjak, sementara furnitur memakai collider solid.
            AddWalkable(room.transform, "LantaiKamar", new Vector2(0f, -1.7f), new Vector2(11.5f, 6.05f));
            AddSolid(room.transform, "MejaBelajar", new Vector2(-0.65f, 1.08f), new Vector2(2.55f, 1.15f));
            AddSolid(room.transform, "TempatTidur", new Vector2(2.75f, 0.15f), new Vector2(2.2f, 3.65f));
            AddSolid(room.transform, "RakKiri", new Vector2(-4.45f, 1.42f), new Vector2(1.55f, 0.75f));
            AddSolid(room.transform, "BatasAtas", new Vector2(0f, 1.75f), new Vector2(12.0f, 0.35f));
            AddSolid(room.transform, "BatasBawah", new Vector2(0f, -4.95f), new Vector2(12.0f, 0.35f));
            AddSolid(room.transform, "BatasKiri", new Vector2(-5.95f, -1.6f), new Vector2(0.35f, 6.9f));
            AddSolid(room.transform, "BatasKanan", new Vector2(5.95f, -1.6f), new Vector2(0.35f, 6.9f));
        }

        private static void AddWalkable(Transform parent, string name, Vector2 position, Vector2 size)
        {
            GameObject go = new GameObject("WalkableArea_" + name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.size = size;
            collider.isTrigger = true;
            go.AddComponent<WalkableArea>();
        }

        private static void AddSolid(Transform parent, string name, Vector2 position, Vector2 size)
        {
            GameObject go = new GameObject("PropBlocker_" + name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.AddComponent<BoxCollider2D>().size = size;
        }

        private static Chapter2StoryController BuildStory(DialogueData main, DialogueData finished, CharacterData dimas)
        {
            GameObject root = new GameObject("Chapter2Story");
            Chapter2StoryController story = root.AddComponent<Chapter2StoryController>();
            AlifDemoSceneBuilder.SetSerializedRef(story, "_mainDialogue", main);
            AlifDemoSceneBuilder.SetSerializedRef(story, "_finishedDialogue", finished);
            AlifDemoSceneBuilder.SetSerializedRef(story, "_dimasData", dimas);
            return story;
        }

        private static void BuildDimas(Chapter2StoryController story, CharacterData dimas)
        {
            GameObject go = new GameObject("Dimas");
            go.transform.position = new Vector3(-2.25f, -0.75f, 0f);
            go.layer = InteractableLayer;

            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = dimas != null ? dimas.Portrait : null;
            renderer.sortingOrder = 20;

            Animator animator = go.AddComponent<Animator>();
            animator.runtimeAnimatorController = dimas != null ? dimas.AnimatorController : null;

            CircleCollider2D collider = go.AddComponent<CircleCollider2D>();
            collider.radius = 0.32f;
            collider.offset = new Vector2(0f, -0.28f);
            collider.isTrigger = false;

            Chapter2StoryPoint point = go.AddComponent<Chapter2StoryPoint>();
            AlifDemoSceneBuilder.SetSerializedRef(point, "_story", story);

            GameObject arrow = new GameObject("InteractionArrow");
            arrow.transform.SetParent(go.transform, false);
            arrow.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            SpriteRenderer arrowRenderer = arrow.AddComponent<SpriteRenderer>();
            arrowRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/InteractionArrow.png");
            arrowRenderer.sortingOrder = 30;
        }

        private static void ConfigureCamera(Transform player)
        {
            Camera camera = Camera.main;
            if (camera == null) return;
            camera.orthographicSize = 3.2f;
            camera.backgroundColor = Color.black;
            camera.transform.position = new Vector3(player.position.x, player.position.y, -10f);

            CameraFollow follow = camera.GetComponent<CameraFollow>();
            if (follow != null)
            {
                AlifDemoSceneBuilder.SetSerializedRef(follow, "_target", player);
            }
        }

        private static DialogueData BuildMainDialogue(CharacterData alif, CharacterData dimas)
        {
            DialogueData data = GetOrCreateDialogue(MainDialoguePath);
            data.Lines.Clear();
            data.OnCompleteEventId = "chapter2.dimas_pinjol_resolved";

            Add(data, "Narator", "[Di dalam kamar, Dimas menutup pintu pelan. Ia duduk di tepi kursi sambil menggenggam ponsel, lalu menunjukkan layar aplikasi kepada Alif.]", null);
            Add(data, "Dimas", "Makasih sudah mau masuk, Lif. Aku sebenarnya malu cerita, tapi sejak tadi kepalaku buntu.", dimas);
            Add(data, "Alif", "Nggak apa-apa. Kita lihat pelan-pelan. Kamu sedang butuh apa, dan seberapa mendesak?", alif);
            Add(data, "Dimas", "Besok aku harus ikut perjalanan wajib ke Jogja. Ongkos, penginapan, sama kebutuhan di sana kurang tepat Rp2.000.000. Kalau nggak ikut, urusan kampusku bisa tertunda.", dimas);
            Add(data, "Dimas", "Di aplikasi ini ada 'Dana Kilat'. Katanya cukup KTP, cair beberapa menit. Aku tinggal tekan ajukan dan izinkan akses kontak.", dimas);

            AddChoice(data, "Apa respons pertama Alif?", alif,
                Choice("Tenangkan Dimas, lalu tanyakan kebutuhan dan tenggatnya satu per satu.", 4f, 4f, 6f, "Pendekatan tenang membuka ruang untuk keputusan yang jernih."),
                Choice("Langsung bilang semua pinjaman pasti buruk tanpa melihat situasinya.", 0f, 2f, -4f, "Niatnya mencegah, tetapi Dimas belum merasa didengar."),
                Choice("Suruh Dimas ambil saja karena kebutuhan besok mendesak.", -6f, -6f, 0f, "Kecepatan dipilih tanpa memahami biaya dan risikonya."),
                new[]
                {
                    "Aku paham kamu panik. Sebelum menekan apa pun, kita pisahkan dulu: kebutuhan wajib, waktunya, dan pilihan yang masih tersedia.",
                    "Dimas, jangan. Pokoknya pinjaman begitu jelek. Tapi... maaf, aku tetap perlu dengar dulu apa yang membuatmu terdesak.",
                    "Kalau hanya melihat tenggat, tombol cair memang terasa paling cepat. Tapi keputusan dua juta rupiah nggak aman kalau kontraknya belum dibaca."
                });

            Add(data, "Dimas", "Yang bikin aku ragu, di bawahnya tertulis denda keterlambatan dua persen per hari. Kalau telat, dendanya ditambahkan ke tagihan berikutnya.", dimas);
            Add(data, "Alif", "Nah, itu bukan catatan kecil. Dua persen dari Rp2.000.000 berarti Rp40.000 pada hari pertama keterlambatan. Kalau denda masuk ke saldo lalu dihitung lagi, bebannya bisa terus membesar.", alif);
            Add(data, "Alif", "Dalam muamalah, tambahan yang disyaratkan karena penundaan pembayaran adalah riba nasi'ah. Secara finansial pun, dendanya membuat masalah jangka pendek berubah menjadi utang yang sulit dikendalikan.", alif);

            AddChoice(data, "Bagian mana yang sebaiknya diperiksa lebih dulu?", alif,
                Choice("Jumlah cair, total pembayaran, jatuh tempo, denda, izin data, dan identitas penyedia.", 6f, 5f, 5f, "Kontrak dinilai dari keseluruhan kewajiban dan risikonya."),
                Choice("Cukup lihat tulisan 'cair cepat' dan testimoni pengguna.", -5f, -3f, 0f, "Iklan bukan pengganti rincian kontrak."),
                Choice("Hanya cek apakah cicilan pertama terlihat murah.", 1f, -2f, -2f, "Cicilan awal saja belum menunjukkan total kewajiban."),
                new[]
                {
                    "Kita cek semuanya, bukan cuma nominal yang masuk. Yang penting adalah total kewajiban, kapan harus lunas, apa akibat telat, dan data apa yang kita serahkan.",
                    "Testimoni bisa dipilih oleh pembuat iklan. Kita tetap harus membaca kontrak dan memeriksa penyedianya, bukan percaya pada kesan cepat.",
                    "Cicilan pertama bisa tampak ringan, padahal biaya lain tersembunyi di halaman berikutnya. Kita hitung total sampai lunas."
                });

            Add(data, "Narator", "[Alif mengambil kertas. Ia meminta Dimas membuka rincian perjalanan, bukan aplikasi pinjaman.]", null);
            Add(data, "Alif", "Sekarang kita buat anggaran minimum. Transportasi berapa? Penginapan bisa berbagi? Ada biaya yang bisa ditunda atau dibayar setelah pulang?", alif);
            Add(data, "Dimas", "Transportasi Rp650.000, penginapan dan kegiatan Rp900.000, makan serta cadangan Rp450.000. Totalnya memang dua juta.", dimas);
            Add(data, "Alif", "Cadangan Rp450.000 bisa kita pecah lagi. Kita cari nominal yang benar-benar harus ada malam ini, bukan langsung menutup semuanya dengan utang mahal.", alif);

            AddChoice(data, "Rencana darurat 24 jam yang paling solutif?", alif,
                Choice("Hubungi panitia untuk keringanan, cek bantuan kampus, dan negosiasikan komponen yang bisa ditunda.", 6f, 4f, 5f, "Masalah diselesaikan dari sumber kebutuhannya terlebih dahulu."),
                Choice("Batalkan perjalanan tanpa menghubungi siapa pun.", -2f, 1f, -3f, "Menghindari utang, tetapi mengabaikan alternatif dan konsekuensi akademik."),
                Choice("Ajukan pinjol dulu, baru pikirkan cara membayar setelah pulang.", -7f, -7f, 0f, "Utang diambil sebelum ada arus kas untuk melunasinya."),
                new[]
                {
                    "Pertama, telepon panitia malam ini. Minta rincian wajib, skema bayar bertahap, dana darurat mahasiswa, atau rekomendasi bantuan resmi.",
                    "Membatalkan mendadak mungkin mencegah utang, tetapi bisa merusak urusan kampus. Kita tanyakan alternatifnya dulu sebelum menyerah.",
                    "Kalau belum tahu sumber pelunasan, pinjaman hanya memindahkan panik hari ini ke hari berikutnya—ditambah denda."
                });

            Add(data, "Dimas", "Kalau bantuan kampus nggak cukup, aku masih kekurangan. Masa aku harus minta satu orang menanggung dua juta penuh?", dimas);
            Add(data, "Alif", "Tidak harus satu sumber. Solusi yang sehat boleh berupa gabungan beberapa langkah kecil, selama nominal, waktu, dan kewajibannya transparan.", alif);

            AddChoice(data, "Sumber dana alternatif mana yang paling aman?", alif,
                Choice("Gabungkan tabungan, pengurangan biaya, bantuan kampus, dan qard hasan tertulis dari keluarga/teman.", 5f, 6f, 6f, "Sumber transparan digabung tanpa tambahan karena waktu."),
                Choice("Cari aplikasi lain yang bunganya sedikit lebih rendah.", -2f, -5f, -2f, "Bunga lebih rendah belum menghilangkan akad dan risiko yang bermasalah."),
                Choice("Pinjam diam-diam dari banyak teman tanpa mencatat jatuh temponya.", -4f, 0f, -4f, "Tanpa catatan, hubungan dan kemampuan bayar tetap berisiko."),
                new[]
                {
                    "Gunakan tabungan yang aman, kurangi biaya nonwajib, ajukan bantuan resmi, lalu bila perlu minta qard hasan—pinjaman pokok tanpa tambahan—dengan jadwal pengembalian tertulis.",
                    "Mengganti aplikasi tidak otomatis menyelesaikan masalah. Kita perlu memastikan akadnya bebas tambahan karena waktu dan penyedianya jelas serta resmi.",
                    "Bantuan teman tetap harus jujur dan tercatat. Tulis siapa memberi berapa, kapan dikembalikan, dan jangan menjanjikan tambahan."
                });

            Add(data, "Alif", "Kamu juga bisa menawarkan pekerjaan sementara yang realistis setelah pulang, menjual barang yang memang tidak dipakai, atau bertanya ke koperasi syariah resmi. Tapi jangan menjual alat utama kuliah dan jangan membuat janji pendapatan palsu.", alif);
            Add(data, "Dimas", "Aku takut kalau cerita ke keluarga, mereka mengira aku nggak bisa mengatur hidup.", dimas);

            AddChoice(data, "Bagaimana Alif membantu Dimas meminta pertolongan?", alif,
                Choice("Susun pesan jujur: kebutuhan, rincian Rp2.000.000, upaya yang sudah dilakukan, dan jadwal pengembalian.", 4f, 5f, 5f, "Transparansi menjaga kepercayaan dan batas kemampuan."),
                Choice("Sembunyikan alasan sebenarnya supaya lebih cepat diberi uang.", -5f, -5f, 0f, "Informasi yang disembunyikan merusak amanah."),
                Choice("Minta Alif berbohong dan mengaku uang itu untuk dirinya.", -6f, -6f, 0f, "Masalah baru tercipta dari kebohongan."),
                new[]
                {
                    "Kita bilang apa adanya: perlu dua juta, rinciannya begini, sudah mencoba keringanan, dan akan mengembalikan sesuai kemampuan yang benar-benar ada.",
                    "Kalau alasannya disamarkan, orang membantu tanpa informasi yang cukup. Itu bukan awal yang baik untuk menyelesaikan amanah.",
                    "Aku bisa menemani, tapi tidak akan berbohong. Bantuan yang sehat harus dimulai dari cerita yang benar."
                });

            Add(data, "Narator", "[Dimas membuka kembali halaman pengajuan. Jempolnya berhenti tepat di atas tombol 'Ajukan Rp2.000.000'.]", null);
            Add(data, "Dimas", "Jadi... aku batalkan sekarang? Data KTP-ku belum kukirim, tapi aplikasinya sudah minta akses kontak dan galeri.", dimas);

            AddChoice(data, "Tindakan penutup yang tepat?", alif,
                Choice("Batalkan pengajuan, jangan unggah KTP, cabut izin aplikasi, simpan bukti, lalu jalankan daftar solusi bersama.", 6f, 6f, 7f, "Risiko dihentikan dan diganti dengan langkah nyata."),
                Choice("Rebut ponsel Dimas dan hapus semuanya tanpa penjelasan.", 0f, 2f, -3f, "Bahaya dihentikan, tetapi Dimas tidak belajar mengambil keputusan mandiri."),
                Choice("Biarkan Dimas mencoba sekali agar kapok.", -8f, -8f, 0f, "Membiarkan kerugian bukan cara aman untuk belajar."),
                new[]
                {
                    "Ya. Batalkan sebelum dikirim, cabut izin kontak dan galeri, simpan tangkapan layar syaratnya, lalu kita telepon panitia dan bagian kemahasiswaan sekarang.",
                    "Aku nggak akan mengambil keputusan atas namamu. Aku jelaskan risikonya, lalu kamu sendiri yang menekan batal supaya keputusan ini benar-benar kamu pahami.",
                    "Belajar tidak harus lewat kerugian dua juta dan data pribadi. Kita bisa berhenti sebelum terlambat dan tetap menyusun jalan keluar."
                });

            Add(data, "Dimas", "Oke. Pengajuannya kubatalkan. Izin kontak dan galeri juga sudah kucabut.", dimas);
            Add(data, "Narator", "[Dimas menghubungi panitia. Ia mendapat opsi membayar biaya kegiatan bertahap dan formulir bantuan mahasiswa. Kekurangan dana menjadi jauh lebih kecil.]", null);
            Add(data, "Dimas", "Ternyata penginapan bisa patungan dan biaya kegiatan boleh dicicil tanpa tambahan. Aku akan pakai tabungan, mengurangi uang cadangan, lalu bicara jujur ke keluarga untuk sisanya.", dimas);
            Add(data, "Alif", "Bagus. Catat semua sumber dan tanggal pengembalian. Kalau ada qard hasan, yang kembali hanya pokok yang disepakati—bukan tambahan karena waktu.", alif);
            Add(data, "Dimas", "Makasih, Lif. Kamu nggak cuma melarang. Kamu bantu aku menemukan langkah yang bisa benar-benar kulakukan malam ini.", dimas);
            Add(data, "Alif", "Kita memang harus menghindari riba, tapi orang yang sedang terdesak juga perlu jalan keluar. Besok aku bantu cek lagi anggarannya sebelum kamu berangkat.", alif);
            Add(data, "Narator", "[Malam itu, tombol pinjaman cepat tidak pernah ditekan. Dimas memilih transparansi, pertolongan yang aman, dan rencana bayar yang sesuai kemampuan.]", null);

            EditorUtility.SetDirty(data);
            return data;
        }

        private static DialogueData BuildFinishedDialogue(CharacterData alif, CharacterData dimas)
        {
            DialogueData data = GetOrCreateDialogue(FinishedDialoguePath);
            data.Lines.Clear();
            data.OnCompleteEventId = "chapter2.dimas_followup";
            Add(data, "Dimas", "Pengajuan pinjolnya tetap batal. Panitia sudah menyetujui cicilan biaya tanpa tambahan, dan keluargaku membantu sisanya sebagai qard hasan tertulis.", dimas);
            Add(data, "Alif", "Mantap. Simpan catatannya, kembalikan pokok sesuai jadwal, dan kabari lebih awal kalau ada perubahan.", alif);
            Add(data, "Dimas", "Siap. Sekarang aku ngerti: menolak pinjol saja belum cukup—aku juga harus punya anggaran dan jalur bantuan yang jelas.", dimas);
            EditorUtility.SetDirty(data);
            return data;
        }

        private static void AddChoice(DialogueData data, string prompt, CharacterData alif,
            DialogueChoice first, DialogueChoice second, DialogueChoice third, string[] reactions)
        {
            DialogueLine promptLine = new DialogueLine { SpeakerName = "Alif", SpeakerData = alif, Text = prompt };
            data.Lines.Add(promptLine);
            int firstReaction = data.Lines.Count;
            int commonNext = firstReaction + 3;

            first.NextLineIndex = firstReaction;
            second.NextLineIndex = firstReaction + 1;
            third.NextLineIndex = firstReaction + 2;
            promptLine.Choices.Add(first);
            promptLine.Choices.Add(second);
            promptLine.Choices.Add(third);

            for (int i = 0; i < 3; i++)
            {
                DialogueLine reaction = new DialogueLine
                {
                    SpeakerName = "Alif",
                    SpeakerData = alif,
                    Text = reactions[i],
                    NextLineIndex = commonNext
                };
                data.Lines.Add(reaction);
            }
        }

        private static DialogueChoice Choice(string text, float financial, float sharia, float balance, string outcome)
        {
            return new DialogueChoice
            {
                ChoiceText = text,
                FinancialLogicChange = financial,
                ShariaComplianceChange = sharia,
                BalanceCorrection = balance,
                OutcomeText = outcome
            };
        }

        private static void Add(DialogueData data, string speaker, string text, CharacterData speakerData)
        {
            data.Lines.Add(new DialogueLine { SpeakerName = speaker, SpeakerData = speakerData, Text = text });
        }

        private static DialogueData GetOrCreateDialogue(string path)
        {
            AlifDemoSceneBuilder.EnsureFolder(DialogueFolder);
            DialogueData data = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
            if (data != null) return data;
            data = ScriptableObject.CreateInstance<DialogueData>();
            AssetDatabase.CreateAsset(data, path);
            return data;
        }

        private static void RegisterInBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(scene => scene.path == TargetScenePath))
            {
                scenes.Add(new EditorBuildSettingsScene(TargetScenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
        }
    }
}
