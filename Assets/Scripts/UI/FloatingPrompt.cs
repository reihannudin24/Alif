using Alif.Campaign;
using UnityEngine;

namespace Alif.UI
{
    /// <summary>
    /// Memberi animasi melayang (bobbing), pulse halus, dan efek pop-in pada panah / prompt
    /// interaksi agar objek di dunia terlihat hidup, mengundang perhatian, dan menyenangkan.
    /// Menghormati toggle ReducedMotion — tanpa animasi, prompt tampil statis penuh.
    /// </summary>
    public class FloatingPrompt : MonoBehaviour
    {
        [Header("Bobbing Motion")]
        [SerializeField] private float _bobSpeed = 4.5f;
        [SerializeField] private float _bobAmount = 0.08f;

        [Header("Pulse")]
        [SerializeField] private float _pulseSpeed = 3f;
        [SerializeField] private float _pulseAmount = 0.06f;

        [Header("Pop In Animation")]
        [SerializeField] private float _popDuration = 0.22f;

        private Vector3 _initialLocalPos;
        private Vector3 _initialScale;
        private float _timeSinceEnabled;
        private bool _isPopping;

        private void Awake()
        {
            _initialLocalPos = transform.localPosition;
            _initialScale = transform.localScale;
            if (_initialScale.sqrMagnitude < 0.001f)
            {
                _initialScale = Vector3.one;
            }
        }

        private void OnEnable()
        {
            _timeSinceEnabled = 0f;
            _isPopping = !CampaignUI.ReducedMotion;
            transform.localScale = _isPopping ? Vector3.zero : _initialScale;
        }

        private void Update()
        {
            if (CampaignUI.ReducedMotion)
            {
                transform.localScale = _initialScale;
                transform.localPosition = _initialLocalPos;
                return;
            }

            float dt = Time.deltaTime;
            _timeSinceEnabled += dt;

            // Pop-in bounce saat baru muncul
            if (_isPopping)
            {
                if (_timeSinceEnabled >= _popDuration)
                {
                    _isPopping = false;
                    transform.localScale = _initialScale;
                }
                else
                {
                    float t = _timeSinceEnabled / _popDuration;
                    // Overshoot bounce curve
                    float bounce = Mathf.Sin(t * Mathf.PI * 0.5f);
                    float overshoot = 1f + Mathf.Sin(t * Mathf.PI) * 0.28f;
                    transform.localScale = _initialScale * (bounce * overshoot);
                }
            }
            else
            {
                // Subtle pulse
                float pulse = 1f + Mathf.Sin(_timeSinceEnabled * _pulseSpeed) * _pulseAmount;
                transform.localScale = _initialScale * pulse;
            }

            // Up-and-down bobbing
            float bobOffset = Mathf.Sin(_timeSinceEnabled * _bobSpeed) * _bobAmount;
            transform.localPosition = _initialLocalPos + new Vector3(0f, bobOffset, 0f);
        }

        public void SetBasePosition(Vector3 localPos)
        {
            _initialLocalPos = localPos;
        }
    }
}
