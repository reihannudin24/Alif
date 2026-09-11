using UnityEngine;

namespace Alif.UI
{
    /// <summary>
    /// Animasi ringan untuk panah interaksi: pop masuk dengan overshoot lalu mengambung
    /// pelan (bob) selama panah tampil. Dipasang otomatis oleh PlayerController saat panah
    /// pertama kali ditampilkan — tidak perlu perubahan pada scene builder.
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
        }

        private void OnEnable()
        {
            if (!_initialized)
            {
                _basePosition = transform.localPosition;
                _initialized = true;
            }
            _age = 0f;
            transform.localScale = Vector3.one;
            transform.localPosition = _basePosition;
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
