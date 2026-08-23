using UnityEngine;

namespace Alif.UI
{
    /// <summary>
    /// Gerakan ambient halus (pan + zoom pelan, "Ken Burns effect") buat background statis di
    /// UI biar kerasa hidup tanpa perlu sprite animasi/parallax layer terpisah. RectTransform
    /// yang dipasangi ini harus sedikit di-overscale (localScale > 1) supaya gerakan pan-nya
    /// nggak nyingkap tepi gambar.
    /// </summary>
    public class AmbientImageMotion : MonoBehaviour
    {
        [SerializeField] private float _panAmplitude = 14f;
        [SerializeField] private float _panSpeed = 0.12f;
        [SerializeField] private float _zoomAmplitude = 0.02f;
        [SerializeField] private float _zoomSpeed = 0.08f;

        private RectTransform _rectTransform;
        private Vector2 _basePosition;
        private Vector3 _baseScale;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _basePosition = _rectTransform.anchoredPosition;
            _baseScale = _rectTransform.localScale;
        }

        private void Update()
        {
            float t = Time.time;

            float x = Mathf.Sin(t * _panSpeed * Mathf.PI * 2f) * _panAmplitude;
            float y = Mathf.Cos(t * _panSpeed * 0.7f * Mathf.PI * 2f) * (_panAmplitude * 0.5f);
            _rectTransform.anchoredPosition = _basePosition + new Vector2(x, y);

            float zoom = 1f + Mathf.Sin(t * _zoomSpeed * Mathf.PI * 2f) * _zoomAmplitude;
            _rectTransform.localScale = _baseScale * zoom;
        }
    }
}
