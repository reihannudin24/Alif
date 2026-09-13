using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Alif.World
{
    /// <summary>
    /// Menandai collider trigger sebagai lantai yang boleh diinjak. PlayerController memakai
    /// gabungan semua area aktif sebagai whitelist, sedangkan collider solid tetap menangani
    /// tembok dan benda-benda di atas lantai.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class WalkableArea : MonoBehaviour
    {
        private static readonly List<WalkableArea> ActiveAreas = new List<WalkableArea>();

        private static readonly (string name, Vector2 center, Vector2 size)[] InteriorAreas =
        {
            ("Informasi", new Vector2(-3.10f, 1.66f), new Vector2(4.90f, 2.48f)),
            ("Toilet", new Vector2(3.20f, 1.66f), new Vector2(4.70f, 2.48f)),
            ("KoridorTengah", new Vector2(0.15f, -0.85f), new Vector2(2.40f, 7.90f)),
            ("LoketKarcis", new Vector2(-3.10f, -2.18f), new Vector2(4.90f, 3.20f)),
            ("RuangTunggu", new Vector2(3.20f, -2.18f), new Vector2(4.70f, 3.20f)),
        };

        private static readonly (string name, Vector2 center, Vector2 size)[] FrontAreas =
        {
            ("Plaza", new Vector2(0f, 0.22f), new Vector2(7.72f, 2.18f)),
            ("Jalan", new Vector2(0f, -1.39f), new Vector2(7.72f, 1.10f)),
        };

        private Collider2D _areaCollider;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry()
        {
            ActiveAreas.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallStationAreasForExistingScenes()
        {
            // SampleScene yang sudah tersimpan sebelum fitur ini ditambahkan belum memiliki
            // child WalkableArea. Pasang otomatis saat Play; scene hasil rebuild baru akan
            // mendapat objek yang sama langsung dari AlifDemoSceneBuilder.
            GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (root.name == "Background")
                {
                    EnsureAreas(root, InteriorAreas);
                }
                else if (root.name == "Background_StasiunLuar")
                {
                    EnsureAreas(root, FrontAreas);
                }
            }
        }

        private static void EnsureAreas(GameObject parent, (string name, Vector2 center, Vector2 size)[] definitions)
        {
            WalkableArea[] existing = parent.GetComponentsInChildren<WalkableArea>(true);
            if (existing.Length > 0)
            {
                return;
            }

            for (int i = 0; i < definitions.Length; i++)
            {
                (string name, Vector2 center, Vector2 size) = definitions[i];
                GameObject area = new GameObject($"WalkableArea_{name}");
                area.transform.SetParent(parent.transform, false);
                area.transform.localPosition = center;

                BoxCollider2D collider = area.AddComponent<BoxCollider2D>();
                collider.size = size;
                collider.isTrigger = true;
                area.AddComponent<WalkableArea>();
            }
        }

        private void Awake()
        {
            _areaCollider = GetComponent<Collider2D>();
        }

        private void OnEnable()
        {
            if (!ActiveAreas.Contains(this))
            {
                ActiveAreas.Add(this);
            }
        }

        private void OnDisable()
        {
            ActiveAreas.Remove(this);
        }

        /// <summary>
        /// True bila scene ini memiliki area lantai terjaga sama sekali. Map lama yang belum
        /// dimigrasi tidak punya area, jadi pengecekan whitelist boleh dilewati.
        /// </summary>
        public static bool HasAreas => ActiveAreas.Count > 0;

        /// <summary>
        /// True bila titik kaki sedang berada di salah satu area lantai yang dijaga.
        /// Dipakai agar map lama yang belum punya WalkableArea tetap memakai physics biasa.
        /// </summary>
        public static bool ContainsPoint(Vector2 worldPoint)
        {
            for (int i = ActiveAreas.Count - 1; i >= 0; i--)
            {
                WalkableArea area = ActiveAreas[i];
                if (area == null)
                {
                    ActiveAreas.RemoveAt(i);
                    continue;
                }

                if (area.AreaCollider.OverlapPoint(worldPoint))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Memastikan bukan cuma titik tengah, tetapi seluruh footprint kecil kaki Alif masih
        /// ditopang lantai. Setiap sampel boleh berada di kotak lantai yang berbeda agar sambungan
        /// antar-ruangan tetap mulus.
        /// </summary>
        public static bool ContainsFootprint(Vector2 center, Vector2 halfExtents)
        {
            float x = Mathf.Max(0f, halfExtents.x - 0.02f);
            float y = Mathf.Max(0f, halfExtents.y - 0.02f);

            return ContainsPoint(center)
                && ContainsPoint(center + new Vector2(x, 0f))
                && ContainsPoint(center + new Vector2(-x, 0f))
                && ContainsPoint(center + new Vector2(0f, y))
                && ContainsPoint(center + new Vector2(0f, -y));
        }

        private Collider2D AreaCollider
        {
            get
            {
                if (_areaCollider == null)
                {
                    _areaCollider = GetComponent<Collider2D>();
                }

                return _areaCollider;
            }
        }
    }
}
