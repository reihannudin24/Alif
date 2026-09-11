using Alif.World;
using UnityEditor;
using UnityEngine;

namespace Alif.EditorTools
{
    /// <summary>
    /// Mewarnai collider di Scene view supaya kelihatan mana lantai yang boleh diinjak Alif
    /// (hijau: trigger dengan WalkableArea) dan mana yang menahan langkah (merah: collider
    /// solid seperti VoidBlocker, PropBlocker, BoundaryWall, bangku). Trigger lain seperti
    /// pintu atau titik aktivitas tidak digambar. Toggle lewat Alif > Debug > Tampilkan Area Jalan.
    /// </summary>
    internal static class AlifWalkableGizmos
    {
        private const string MenuPath = "Alif/Debug/Tampilkan Area Jalan";
        private const string PrefKey = "Alif.ShowWalkableGizmos";

        private static readonly Color WalkableFill = new Color(0.2f, 0.95f, 0.35f, 0.22f);
        private static readonly Color WalkableLine = new Color(0.2f, 0.95f, 0.35f, 0.9f);
        private static readonly Color BlockerFill = new Color(1f, 0.2f, 0.2f, 0.3f);
        private static readonly Color BlockerLine = new Color(1f, 0.25f, 0.25f, 0.9f);

        private static bool Enabled => EditorPrefs.GetBool(PrefKey, true);

        [MenuItem(MenuPath)]
        private static void Toggle()
        {
            EditorPrefs.SetBool(PrefKey, !Enabled);
            SceneView.RepaintAll();
        }

        [MenuItem(MenuPath, true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, Enabled);
            return true;
        }

        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected | GizmoType.Pickable)]
        private static void DrawBox(BoxCollider2D box, GizmoType gizmoType)
        {
            if (!TryGetColors(box, out Color fill, out Color line))
            {
                return;
            }

            Gizmos.matrix = box.transform.localToWorldMatrix;
            Vector3 center = box.offset;
            Vector3 size = new Vector3(box.size.x, box.size.y, 0.01f);
            Gizmos.color = fill;
            Gizmos.DrawCube(center, size);
            Gizmos.color = line;
            Gizmos.DrawWireCube(center, size);
            Gizmos.matrix = Matrix4x4.identity;
        }

        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected | GizmoType.Pickable)]
        private static void DrawPolygon(PolygonCollider2D polygon, GizmoType gizmoType)
        {
            // Poligon cekung tidak bisa diisi dengan DrawAAConvexPolygon, jadi cukup garis tebal.
            if (!TryGetColors(polygon, out _, out Color line))
            {
                return;
            }

            Handles.color = line;
            Matrix4x4 matrix = polygon.transform.localToWorldMatrix;
            for (int p = 0; p < polygon.pathCount; p++)
            {
                Vector2[] path = polygon.GetPath(p);
                if (path.Length < 2)
                {
                    continue;
                }

                var points = new Vector3[path.Length + 1];
                for (int i = 0; i < path.Length; i++)
                {
                    points[i] = matrix.MultiplyPoint3x4(path[i] + polygon.offset);
                }

                points[path.Length] = points[0];
                Handles.DrawAAPolyLine(4f, points);
            }
        }

        private static bool TryGetColors(Collider2D collider, out Color fill, out Color line)
        {
            fill = line = default;
            if (!Enabled || !collider.isActiveAndEnabled)
            {
                return false;
            }

            if (!collider.isTrigger)
            {
                fill = BlockerFill;
                line = BlockerLine;
                return true;
            }

            if (collider.GetComponent<WalkableArea>() != null)
            {
                fill = WalkableFill;
                line = WalkableLine;
                return true;
            }

            return false;
        }
    }
}
