using UnityEngine;

namespace Alif.Core
{
    /// <summary>
    /// Bikin kamera mengikuti target (biasanya Player) secara halus. Dilengkapi fitur:
    /// - Smooth dampening (exponential lerp)
    /// - Dynamic Look-Ahead (kamera mengantisipasi arah jalan pemain)
    /// - Screen Shake (guncangan dinamis saat impact, konflik dialog, atau combat)
    /// - CameraBounds clamping (mencegah void hitam di tepi peta)
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        public static CameraFollow Instance { get; private set; }

        [SerializeField] private Transform _target;
        [Tooltip("Makin besar makin cepat kamera nyusul target. 0 = kamera diam.")]
        [SerializeField] private float _smoothSpeed = 8f;
        [SerializeField] private Vector3 _offset = new Vector3(0f, 0f, -10f);

        [Header("Look-Ahead")]
        [SerializeField] private float _lookAheadDistance = 0.5f;
        [SerializeField] private float _lookAheadSmoothSpeed = 3f;

        private Camera _camera;
        private Vector3 _currentLookAhead;
        private Rigidbody2D _targetRb;
        private Vector3 _lastTargetPos;

        // Screen Shake
        private float _shakeTimeRemaining;
        private float _shakeIntensity;
        private Vector3 _shakeOffset;

        // Sprint zoom (umpan balik kecepatan: kamera sedikit melebar saat lari)
        private bool _sprintZoom;
        private float _sprintZoomOffset;
        private float _baseOrthoSize;

        private void Awake()
        {
            Instance = this;
            _camera = GetComponent<Camera>();
            if (_camera != null) _baseOrthoSize = _camera.orthographicSize;
        }

        private void Start()
        {
            if (_target != null)
            {
                _targetRb = _target.GetComponent<Rigidbody2D>();
                _lastTargetPos = _target.position;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void SetTarget(Transform target)
        {
            _target = target;
            _targetRb = target != null ? target.GetComponent<Rigidbody2D>() : null;
            if (target != null) _lastTargetPos = target.position;
        }

        /// <summary>
        /// Langsung posisikan kamera ke target TANPA Lerp — dipakai setelah teleport instan
        /// (mis. SceneDoor) supaya kamera nggak keliatan "diseret" ngejar jarak jauh sekaligus.
        /// </summary>
        public void SnapToTarget()
        {
            if (_target == null) return;
            _currentLookAhead = Vector3.zero;
            _shakeOffset = Vector3.zero;
            _shakeTimeRemaining = 0f;
            transform.position = ClampToBounds(_target.position + _offset);
        }

        /// <summary>
        /// Picu getaran layar (screen shake) untuk memberi dampak fisik dan kepuasan visual.
        /// Dilewati total saat Reduced Motion aktif — getaran adalah efek gerak murni.
        /// </summary>
        public void Shake(float duration = 0.15f, float intensity = 0.08f)
        {
            if (PlayerPrefs.GetInt("Alif_ReducedMotion", 0) == 1) return;
            _shakeTimeRemaining = Mathf.Max(_shakeTimeRemaining, duration);
            _shakeIntensity = Mathf.Max(_shakeIntensity, intensity);
        }

        public static void TriggerShake(float duration = 0.15f, float intensity = 0.08f)
        {
            if (Instance != null)
            {
                Instance.Shake(duration, intensity);
            }
        }

        /// <summary>
        /// Aktifkan/melepas zoom sprint. Dipanggil PlayerController saat status sprint berubah;
        /// perubahan ukuran kamera di-lerp pelan di sini supaya tidak menyentak.
        /// </summary>
        public void SetSprintZoom(bool active) => _sprintZoom = active;

        private void LateUpdate()
        {
            if (_target == null) return;

            float dt = Time.deltaTime;

            // Hitung look-ahead berdasarkan kecepatan target
            Vector2 targetVel = Vector2.zero;
            if (_targetRb != null)
            {
                targetVel = _targetRb.linearVelocity;
            }
            else
            {
                targetVel = (Vector2)(_target.position - _lastTargetPos) / Mathf.Max(0.0001f, dt);
            }
            _lastTargetPos = _target.position;

            Vector3 targetLookAhead = Vector3.zero;
            if (targetVel.sqrMagnitude > 0.1f)
            {
                targetLookAhead = (Vector3)(targetVel.normalized * _lookAheadDistance);
            }
            _currentLookAhead = Vector3.Lerp(_currentLookAhead, targetLookAhead, 1f - Mathf.Exp(-_lookAheadSmoothSpeed * dt));

            // Hitung guncangan layar (Screen Shake)
            if (_shakeTimeRemaining > 0f)
            {
                _shakeTimeRemaining -= dt;
                float currentPower = _shakeIntensity * (_shakeTimeRemaining > 0f ? 1f : 0f);
                _shakeOffset = new Vector3(
                    Random.Range(-currentPower, currentPower),
                    Random.Range(-currentPower, currentPower),
                    0f
                );
            }
            else
            {
                _shakeOffset = Vector3.zero;
                _shakeIntensity = 0f;
            }

            // Zoom sprint dijalankan SEBELUM clamp supaya ClampToBounds di bawah selalu
            // membaca ukuran viewport terkini (bukan ukuran frame sebelumnya).
            float zoomTarget = _sprintZoom && PlayerPrefs.GetInt("Alif_ReducedMotion", 0) == 0 ? 0.4f : 0f;
            _sprintZoomOffset = Mathf.MoveTowards(_sprintZoomOffset, zoomTarget, dt * 1.2f);
            if (_camera != null && _camera.orthographic)
            {
                // Jarak kamera sama di mana pun, termasuk di dalam ruangan: ruangan yang lebih
                // kecil dari layar dibiarkan dibingkai gelap (seperti interior stasiun yang
                // digambar tangan), bukan didekati — merapat membuat lantai terasa sempit.
                _camera.orthographicSize = _baseOrthoSize + _sprintZoomOffset;
            }

            // Shake ikut di-clamp: offset digabung SEBELUM ClampToBounds supaya getaran
            // tidak menarik kamera keluar batas peta (mencegah void hitam di tepi).
            Vector3 baseTargetPos = _target.position + _currentLookAhead + _offset + _shakeOffset;
            Vector3 desiredPosition = ClampToBounds(baseTargetPos);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, 1f - Mathf.Exp(-_smoothSpeed * dt));
        }

        /// <summary>Kunci kamera di dalam CameraBounds area yang sedang ditempati target.</summary>
        private Vector3 ClampToBounds(Vector3 desiredPosition)
        {
            if (_camera == null || !_camera.orthographic) return desiredPosition;

            CameraBounds bounds = CameraBounds.FindContaining(_target.position);
            if (bounds == null) return desiredPosition;

            float halfViewHeight = _camera.orthographicSize;
            float halfViewWidth = halfViewHeight * _camera.aspect;

            Vector2 min = bounds.Center - bounds.HalfExtents;
            Vector2 max = bounds.Center + bounds.HalfExtents;

            desiredPosition.x = ClampAxis(desiredPosition.x, min.x, max.x, halfViewWidth);
            desiredPosition.y = ClampAxis(desiredPosition.y, min.y, max.y, halfViewHeight);
            return desiredPosition;
        }

        private static float ClampAxis(float value, float min, float max, float halfView)
        {
            float lo = min + halfView;
            float hi = max - halfView;
            if (lo > hi)
            {
                return (min + max) * 0.5f;
            }

            return Mathf.Clamp(value, lo, hi);
        }
    }
}
