using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Alif.Campaign
{
    /// <summary>
    /// Angka damage dan teks umpan balik pendek yang muncul mengapung di arena combat.
    /// Berupa pool world-space TextMeshPro di bawah Arena — otomatis ikut nonaktif saat
    /// arena ditutup, dan tidak pernah bocor keluar dari scene combat.
    /// </summary>
    public sealed class FloatingCombatText : MonoBehaviour
    {
        private const int PoolSize = 14;
        private struct Active
        {
            public TMP_Text Label; public float Age; public float Duration;
            public Vector2 Origin; public float Size;
        }
        private readonly List<TMP_Text> _pool = new List<TMP_Text>();
        private readonly List<Active> _active = new List<Active>();

        private void Awake()
        {
            for (int i = 0; i < PoolSize; i++)
            {
                var go = new GameObject("Float text");
                go.transform.SetParent(transform, false);
                go.SetActive(false);
                var label = go.AddComponent<TextMeshPro>();
                label.fontSize = 3.4f;
                label.alignment = TextAlignmentOptions.Center;
                label.rectTransform.sizeDelta = new Vector2(4f, 1.1f);
                var mesh = label.GetComponent<MeshRenderer>();
                mesh.sortingLayerName = "Characters"; mesh.sortingOrder = 90;
                _pool.Add(label);
            }
        }

        public void Show(Vector2 localPosition, string text, Color color, float size = 1f)
        {
            TMP_Text label = null;
            for (int i = 0; i < _pool.Count; i++)
                if (!_pool[i].gameObject.activeSelf) { label = _pool[i]; break; }
            if (label == null) return; // pool habis: feedback berikutnya dilewati, jangan menumpuk
            label.text = text; label.color = color;
            label.transform.localPosition = localPosition;
            label.transform.localScale = Vector3.one * .4f;
            label.gameObject.SetActive(true);
            _active.Add(new Active { Label = label, Duration = .8f, Origin = localPosition, Size = size });
        }

        public void Clear()
        {
            foreach (var item in _active) if (item.Label != null) item.Label.gameObject.SetActive(false);
            _active.Clear();
        }

        private void LateUpdate()
        {
            bool reduced = PlayerPrefs.GetInt("Alif_ReducedMotion", 0) == 1;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var item = _active[i];
                if (item.Label == null) { _active.RemoveAt(i); continue; }
                item.Age += Time.deltaTime;
                float p = Mathf.Clamp01(item.Age / item.Duration);
                if (p >= 1f) { item.Label.gameObject.SetActive(false); _active.RemoveAt(i); continue; }
                // Pop masuk (grow + overshoot singkat) lalu mengapung naik dan memudar.
                float grow = Mathf.Lerp(.45f, 1f, Mathf.Clamp01(p / .22f));
                float bump = reduced ? 1f : 1f + Mathf.Sin(Mathf.Clamp01(p / .35f) * Mathf.PI) * .25f;
                item.Label.transform.localScale = Vector3.one * item.Size * grow * bump;
                float rise = reduced ? .35f * p : 1f - Mathf.Pow(1f - p, 2f);
                item.Label.transform.localPosition = item.Origin + Vector2.up * rise;
                var color = item.Label.color;
                color.a = p < .55f ? 1f : 1f - (p - .55f) / .45f;
                item.Label.color = color;
                _active[i] = item;
            }
        }
    }
}
