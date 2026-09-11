using UnityEngine;

namespace Alif.World
{
    /// <summary>
    /// Mengatur sortingOrder SpriteRenderer secara dinamis berdasarkan posisi Y (kaki objek).
    /// Karakter atau objek yang berada lebih bawah di layar (Y lebih kecil) akan otomatis
    /// digambar di DEPAN objek yang berada lebih atas (Y lebih besar), sehingga tidak ada
    /// lagi masalah kepala terpotong atau sprite menembus furniture.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class YSortOrder : MonoBehaviour
    {
        [Tooltip("Offset Y dari pivot ke titik tumpu kaki (misalnya -0.35 untuk karakter).")]
        [SerializeField] private float _offsetY = -0.3f;

        [Tooltip("Pengali ketelitian posisi Y ke sorting order. 100 artinya beda 0.01 unit posisi Y sudah membedakan layer.")]
        [SerializeField] private int _precision = 100;

        [Tooltip("Offset dasar sorting order.")]
        [SerializeField] private int _baseOrder = 1000;

        [Tooltip("Jika true, hanya dihitung sekali saat Start (cocok untuk prop statis).")]
        [SerializeField] private bool _isStatic = false;

        private SpriteRenderer _spriteRenderer;
        private float _lastY;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            UpdateSortOrder();
            if (_isStatic)
            {
                enabled = false;
            }
        }

        private void LateUpdate()
        {
            float currentY = transform.position.y;
            if (Mathf.Abs(currentY - _lastY) > 0.001f)
            {
                UpdateSortOrder();
                _lastY = currentY;
            }
        }

        public void UpdateSortOrder()
        {
            if (_spriteRenderer == null) return;
            float footY = transform.position.y + _offsetY;
            _spriteRenderer.sortingOrder = -Mathf.RoundToInt(footY * _precision) + _baseOrder;
        }
    }
}
