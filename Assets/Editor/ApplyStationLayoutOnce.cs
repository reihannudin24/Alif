using Alif.Adventure;
using Alif.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Alif.EditorTools
{
    /// <summary>
    /// Migrasi sekali-jalan untuk scene AdventureChapter1 yang terbuka di Editor (tanpa rebuild
    /// penuh; nilai dari builder): "Papan arah" memakai layar info painted, voucher promo
    /// diambil di mesin tiket painted, prop papan jadwal + tanaman serta troli koper dihapus, dan Naya
    /// berdiri di ubin depan meja loket. Semua langkah
    /// idempotent. Disimpan otomatis kalau scene tidak punya perubahan lain yang belum
    /// tersimpan. Hapus file ini setelah scene disimpan.
    /// </summary>
    [InitializeOnLoad]
    internal static class ApplyStationLayoutOnce
    {
        private const string ScenePath = "Assets/Scenes/Adventure/AdventureChapter1.unity";
        private const string UndoName = "Tata ulang stasiun";
        private static readonly Vector2 VoucherCenter = new Vector2(-4.54f, -1.9f);
        private static readonly Vector2 VoucherSize = new Vector2(.7f, .6f);
        // Kaki Naya di ubin depan dasar meja loket (y -1.89), bukan di muka meja.
        private static readonly Vector2 NayaPosition = new Vector2(-1.75f, -2.03f);

        static ApplyStationLayoutOnce()
        {
            EditorApplication.delayCall += Apply;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode) Apply();
            };
        }

        private static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedHere = !scene.isLoaded;
            if (openedHere) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            bool wasDirty = scene.isDirty;

            bool changed = UsePaintedStationSign(scene);
            changed |= UsePaintedVoucherMachine(scene);
            changed |= RemoveProp(scene, "VariantC_StationTimetable");
            changed |= RemoveProp(scene, "VariantC_StationLuggage");
            changed |= MoveRoot(scene, "NPC_naya", NayaPosition);

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                if (!wasDirty) EditorSceneManager.SaveScene(scene);
                Debug.Log(wasDirty
                    ? "[Alif] Tata letak stasiun diperbarui. Simpan scene AdventureChapter1 (Cmd/Ctrl+S)."
                    : "[Alif] Tata letak stasiun diperbarui dan scene disimpan.");
            }
            else if (wasDirty)
            {
                Debug.Log("[Alif] Tata letak stasiun sudah terpasang. Simpan scene AdventureChapter1 (Cmd/Ctrl+S).");
            }
            if (openedHere) EditorSceneManager.CloseScene(scene, true);
        }

        private static bool UsePaintedStationSign(Scene scene)
        {
            Transform station = Find(scene, "Station_NoticeBoard");
            Vector2 anchor = AlifAdventureBuilder.StationSignPosition;
            if (station == null || Vector2.Distance(station.position, anchor) < .01f) return false;

            Undo.RecordObject(station, UndoName);
            station.position = anchor;
            // YSortOrder mewajibkan SpriteRenderer — lepas dulu, baru sprite prop lamanya.
            if (station.TryGetComponent(out YSortOrder sort)) Undo.DestroyObjectImmediate(sort);
            if (station.TryGetComponent(out SpriteRenderer renderer)) Undo.DestroyObjectImmediate(renderer);
            Transform shadow = station.Find("ContactShadow");
            if (shadow != null) Undo.DestroyObjectImmediate(shadow.gameObject);

            Transform trigger = station.Find("Adventure interaction");
            if (trigger != null)
            {
                Undo.RecordObject(trigger, UndoName);
                trigger.position = anchor + new Vector2(0f, -.32f);
                var point = trigger.GetComponent<AdventurePoint>();
                if (point != null && point.Label != null)
                {
                    Undo.RecordObject(point.Label.transform, UndoName);
                    point.Label.transform.position = new Vector3(anchor.x, AlifAdventureBuilder.StationSignScreenTop + .2f, 0f);
                }
            }
            return true;
        }

        private static bool UsePaintedVoucherMachine(Scene scene)
        {
            Transform loket = Find(scene, "LoketKarcis");
            if (loket == null || Vector2.Distance(loket.position, VoucherCenter) < .01f) return false;

            Undo.RecordObject(loket, UndoName);
            loket.position = VoucherCenter;
            if (loket.TryGetComponent(out BoxCollider2D box))
            {
                Undo.RecordObject(box, UndoName);
                box.size = VoucherSize;
                box.offset = Vector2.zero;
            }
            return true;
        }

        private static bool MoveRoot(Scene scene, string name, Vector2 position)
        {
            Transform target = Find(scene, name);
            if (target == null || Vector2.Distance(target.position, position) < .01f) return false;
            Undo.RecordObject(target, UndoName);
            target.position = position;
            return true;
        }

        private static bool RemoveProp(Scene scene, string name)
        {
            Transform prop = Find(scene, name);
            if (prop == null) return false;
            Undo.DestroyObjectImmediate(prop.gameObject);
            return true;
        }

        private static Transform Find(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform found = FindDeep(root.transform, name);
                if (found != null) return found;
            }
            return null;
        }

        private static Transform FindDeep(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            foreach (Transform child in parent)
            {
                Transform found = FindDeep(child, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
