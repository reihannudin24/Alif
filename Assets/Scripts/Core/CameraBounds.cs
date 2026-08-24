using System.Collections.Generic;
using UnityEngine;

namespace Alif.Core
{
    /// <summary>
    /// Menandai batas dunia satu area/background (Stasiun Interior, Stasiun Luar, Warung Depan,
    /// Warung Dalam, dst). CameraFollow memakai ini untuk clamp posisinya sendiri supaya tidak
    /// pernah menampilkan area hitam kosong di luar gambar begitu Player mendekati tepi peta —
    /// beberapa area (misal Warung Depan) ukurannya lebih kecil dari jangkauan pandang kamera,
    /// jadi tanpa clamp ini kamera gampang "mengintip" ke luar batas gambar.
    /// </summary>
    public sealed class CameraBounds : MonoBehaviour
    {
        [SerializeField] private Vector2 _center;
        [SerializeField] private Vector2 _halfExtents;

        private static readonly List<CameraBounds> ActiveBounds = new List<CameraBounds>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry()
        {
            ActiveBounds.Clear();
        }

        public Vector2 Center => _center;
        public Vector2 HalfExtents => _halfExtents;

        private void OnEnable()
        {
            if (!ActiveBounds.Contains(this))
            {
                ActiveBounds.Add(this);
            }
        }

        private void OnDisable()
        {
            ActiveBounds.Remove(this);
        }

        /// <summary>
        /// Cari area yang memuat titik dunia tertentu (biasanya posisi Player). Null kalau titik
        /// itu tidak berada di area manapun (misal belum ada CameraBounds yang dipasang di scene).
        /// </summary>
        public static CameraBounds FindContaining(Vector2 worldPoint)
        {
            for (int i = ActiveBounds.Count - 1; i >= 0; i--)
            {
                CameraBounds bounds = ActiveBounds[i];
                if (bounds == null)
                {
                    ActiveBounds.RemoveAt(i);
                    continue;
                }

                Vector2 min = bounds._center - bounds._halfExtents;
                Vector2 max = bounds._center + bounds._halfExtents;
                if (worldPoint.x >= min.x && worldPoint.x <= max.x && worldPoint.y >= min.y && worldPoint.y <= max.y)
                {
                    return bounds;
                }
            }

            return null;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(new Vector3(_center.x, _center.y, 0f), new Vector3(_halfExtents.x * 2f, _halfExtents.y * 2f, 0f));
        }
    }
}
