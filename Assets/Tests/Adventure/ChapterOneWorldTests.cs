using System.Linq;
using Alif.World;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Alif.Adventure.Tests
{
    /// <summary>
    /// Menjaga AdventureChapter1.unity tetap sinkron dengan tabel otoritatif di
    /// AlifDemoSceneBuilder. Scene hasil clone pernah ter-edit manual: whitelist lantai
    /// menyusut dan duplikat (Toilet tinggal sepotong, KoridorTengah pecah tiga, ada
    /// "WalkableArea_Informasi (1)" jembatan hantu), blocker bergeser dari art, dan ada
    /// dinding yatim — semuanya bikin player tersangkut di ambang pintu. Ekspektasi di
    /// sini adalah salinan independen dari tabel builder: setelah mengubah tabel, jalankan
    /// menu "Alif/Adventure/Repair Chapter 1 Collision", lalu perbarui ekspektasi ini
    /// bersama tabelnya agar keduanya tetap sengaja sama.
    /// </summary>
    public sealed class ChapterOneWorldTests
    {
        const string ScenePath = "Assets/Scenes/Adventure/AdventureChapter1.unity";

        [SetUp]
        public void OpenChapterOne() => EditorSceneManager.OpenScene(ScenePath);

        [TearDown]
        public void CloseChapterOne() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        [Test]
        public void StationInteriorWhitelistIsExactlyTheAuthoritativeSet()
        {
            var expected = new (string name, float x, float y, float w, float h)[]
            {
                ("WalkableArea_Informasi", -3.10f, 1.66f, 4.90f, 2.48f),
                ("WalkableArea_Toilet", 3.20f, 1.66f, 4.70f, 2.48f),
                ("WalkableArea_KoridorTengah", 0.15f, -0.85f, 2.40f, 7.90f),
                ("WalkableArea_LoketKarcis", -3.10f, -2.18f, 4.90f, 3.20f),
                ("WalkableArea_RuangTunggu", 3.20f, -2.18f, 4.70f, 3.20f),
            };
            AssertAreasMatch(expected, Root("Background").GetComponentsInChildren<WalkableArea>(true));
        }

        [Test]
        public void StationFrontWhitelistIsExactlyTheAuthoritativeSet()
        {
            var expected = new (string name, float x, float y, float w, float h)[]
            {
                ("WalkableArea_Plaza", 0f, 0.22f, 7.72f, 2.18f),
                ("WalkableArea_Jalan", 0f, -1.39f, 7.72f, 1.10f),
            };
            AssertAreasMatch(expected, Root("Background_StasiunLuar").GetComponentsInChildren<WalkableArea>(true));
        }

        [Test]
        public void NoStrayWalkableAreasBeyondTheTwoStationRoots()
        {
            WalkableArea[] all = AllWalkable();
            Assert.That(all, Has.Length.EqualTo(7),
                "Whitelist hanya boleh 5 kotak interior + 2 kotak depan stasiun; kotak lain = sisa edit manual.");
        }

        [Test]
        public void SolidBlockerSetsMatchAuthoritativeTables()
        {
            AssertBlockerCounts(Root("Background"), voidBlockers: 11, boundaryWalls: 4, propBlockers: 0);
            AssertBlockerCounts(Root("Background_StasiunLuar"), voidBlockers: 0, boundaryWalls: 4, propBlockers: 18);
            AssertBlockerCounts(Root("Background_WarungDepan"), voidBlockers: 0, boundaryWalls: 4, propBlockers: 20);
            AssertBlockerCounts(Root("Background_WarungDalam"), voidBlockers: 0, boundaryWalls: 4, propBlockers: 36);

            // Sentinel dinding yatim era edit manual: solid 0.5x2.6 di local (-1, 2.92)
            // interior, menutup sebagian koridor atas.
            foreach (BoxCollider2D box in Root("Background").GetComponentsInChildren<BoxCollider2D>(true))
                if (!box.isTrigger)
                    Assert.That(((Vector2)box.transform.localPosition - new Vector2(-1f, 2.92f)).sqrMagnitude,
                        Is.GreaterThan(0.01f), "Dinding yatim muncul lagi di local (-1, 2.92).");
        }

        [Test]
        public void AmbiencePropsAreGroundedSolidAndInsideTheMap()
        {
            foreach (var (name, surfaceY) in new[]
                     {
                         ("VariantC_ServingCounter", -39.06f),
                         ("VariantC_Condiments", -39.06f),
                         ("VariantC_ReceiptTray", -37.385f),
                     })
                Assert.That(Required(name).transform.position.y, Is.EqualTo(surfaceY).Within(0.05f),
                    name + " harus duduk di permukaan counternya, bukan melayang.");

            Bounds warungDepan = Root("Background_WarungDepan").GetComponent<SpriteRenderer>().bounds;
            Assert.That(Required("VariantC_GangPlanterCrates").transform.position.x,
                Is.InRange(warungDepan.min.x, warungDepan.max.x),
                "Crates harus di dalam gambar Warung Depan (dulu mengambang di void x=35.25).");

            foreach (string name in new[] { "VariantC_GangPlanterCrates", "PetugasStasiun" })
                Assert.That(Required(name).GetComponents<Collider2D>().Any(c => !c.isTrigger), Is.True,
                    name + " butuh collider kaki solid supaya tidak ditembus player.");

            // Troli koper & papan jadwal dihapus: menutupi furnitur painted dan troli tampak
            // seperti barang yang bisa diambil padahal tidak ada event ambil barang.
            foreach (string removed in new[] { "VariantC_StationLuggage", "VariantC_StationTimetable" })
                Assert.That(FindByName(removed), Is.Null, removed + " tidak boleh ada lagi di stasiun.");

            foreach (string legacy in new[] { "Cat_SiBelang", "Secret_LuckyCoin" })
            {
                GameObject go = FindByName(legacy);
                if (go != null)
                    Assert.That(go.activeInHierarchy, Is.False,
                        legacy + " (easter egg warisan SampleScene) harus nonaktif di scene Adventure.");
            }
        }

        static GameObject Root(string name) =>
            SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g => g.name == name)
            ?? throw new AssertionException("Root hilang dari Chapter 1: " + name);

        static GameObject FindByName(string name) =>
            SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(t => t.name == name)?.gameObject;

        static GameObject Required(string name) =>
            FindByName(name) ?? throw new AssertionException("Objek Chapter 1 hilang: " + name);

        static WalkableArea[] AllWalkable() =>
            SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<WalkableArea>(true)).ToArray();

        static void AssertAreasMatch((string name, float x, float y, float w, float h)[] expected, WalkableArea[] actual)
        {
            Assert.That(actual.Select(area => area.name).ToArray(),
                Is.EquivalentTo(expected.Select(e => e.name).ToArray()),
                "Set whitelist berubah — jalankan Repair Chapter 1 Collision atau perbarui tabel+test bersama.");
            foreach (var (name, x, y, w, h) in expected)
            {
                WalkableArea area = actual.First(a => a.name == name);
                BoxCollider2D box = area.GetComponent<BoxCollider2D>();
                Assert.That(box, Is.Not.Null, name + " memakai BoxCollider2D trigger.");
                Assert.That(box.isTrigger, Is.True, name);
                Assert.That((Vector2)area.transform.localPosition, Is.EqualTo(new Vector2(x, y)).Within(0.01f), name + " center");
                Assert.That(box.size, Is.EqualTo(new Vector2(w, h)).Within(0.01f), name + " size");
            }
        }

        static void AssertBlockerCounts(GameObject root, int voidBlockers, int boundaryWalls, int propBlockers)
        {
            Transform[] children = root.transform.Cast<Transform>().ToArray();
            Assert.That(children.Count(t => t.name.StartsWith("VoidBlocker")), Is.EqualTo(voidBlockers), root.name + " VoidBlocker");
            Assert.That(children.Count(t => t.name.StartsWith("BoundaryWall")), Is.EqualTo(boundaryWalls), root.name + " BoundaryWall");
            Assert.That(children.Count(t => t.name.StartsWith("PropBlocker")), Is.EqualTo(propBlockers), root.name + " PropBlocker");
            foreach (Transform child in children)
                if (child.name.StartsWith("VoidBlocker") || child.name.StartsWith("BoundaryWall") || child.name.StartsWith("PropBlocker"))
                {
                    BoxCollider2D box = child.GetComponent<BoxCollider2D>();
                    Assert.That(box, Is.Not.Null, child.name + " kehilangan BoxCollider2D.");
                    Assert.That(box.isTrigger, Is.False, child.name + " harus solid.");
                }
        }
    }
}
