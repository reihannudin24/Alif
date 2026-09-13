using TMPro;
using UnityEngine;

namespace Alif.UI
{
    /// <summary>
    /// Animasi ringan untuk panah interaksi: pop masuk dengan overshoot lalu mengambung
    /// pelan (bob) selama panah tampil. Dipasang otomatis oleh PlayerController saat panah
    /// pertama kali ditampilkan — tidak perlu perubahan pada scene builder. Menambahkan
    /// keycap "E" kecil di samping panah supaya pemain keyboard tahu tombolnya.
    /// </summary>
    public sealed class InteractionPromptFX : MonoBehaviour
    {
        private Vector3 _basePosition;
        private bool _initialized;
        private float _age;

        private void Awake()
        {
            _basePosition = transform.localPosition;
            _initialized = true;
            EnsureKeycap();
        }

        private void OnEnable()
        {
            if (!_initialized)
            {
                _basePosition = transform.localPosition;
                _initialized = true;
                EnsureKeycap();
            }
            _age = 0f;
            transform.localScale = Vector3.one;
            transform.localPosition = _basePosition;
        }

        // Label "E" dunia (bukan bagian canvas HUD) — dibuat sekali, ikut parent panah.
        private void EnsureKeycap()
        {
            if (transform.Find("KeycapE") != null) return;
            var keycap = new GameObject("KeycapE");
            keycap.transform.SetParent(transform, false);
            keycap.transform.localPosition = new Vector3(0.42f, -0.02f, 0f);
            var text = keycap.AddComponent<TextMeshPro>();
            text.text = "E";
            text.fontSize = 1.6f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(1f, 1f, 1f, .95f);
            text.rectTransform.sizeDelta = new Vector2(.6f, .6f);
            var mesh = text.GetComponent<MeshRenderer>();
            mesh.sortingLayerName = "Default";
            mesh.sortingOrder = 112;
        }

        private void LateUpdate()
        {
            _age += Time.deltaTime;
            if (PlayerPrefs.GetInt("Alif_ReducedMotion", 0) == 1)
            {
                transform.localScale = Vector3.one;
                transform.localPosition = _basePosition;
                return;
            }
            // EaseOutBack: sedikit melebihi 1 lalu kembali — kesan "pop" yang hidup.
            const float c = 1.70158f * 1.2f;
            float t = Mathf.Clamp01(_age / .25f);
            float eased = 1f + (c + 1f) * Mathf.Pow(t - 1f, 3f) + c * Mathf.Pow(t - 1f, 2f);
            transform.localScale = Vector3.one * Mathf.Lerp(.4f, 1f, eased);
            transform.localPosition = _basePosition + Vector3.up * (Mathf.Sin(_age * 3.2f) * .06f);
        }
    }
}
