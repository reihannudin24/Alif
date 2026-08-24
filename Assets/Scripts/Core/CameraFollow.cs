using UnityEngine;

namespace Alif.Core
{
    /// <summary>
    /// Bikin kamera mengikuti target (biasanya Player) secara halus. Tanpa script ini,
    /// kamera diam di tempat dan karakter bisa "menghilang" dari layar begitu jalan
    /// cukup jauh dari posisi awal.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [Tooltip("Makin besar makin cepat kamera nyusul target. 0 = kamera diam.")]
        [SerializeField] private float _smoothSpeed = 8f;
        [SerializeField] private Vector3 _offset = new Vector3(0f, 0f, -10f);

        private Camera _camera;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        public void SetTarget(Transform target)
        {
            _target = target;
        }

        /// <summary>
        /// Langsung posisikan kamera ke target TANPA Lerp — dipakai setelah teleport instan
        /// (mis. SceneDoor) supaya kamera nggak keliatan "diseret" ngejar jarak jauh sekaligus.
        /// </summary>
        public void SnapToTarget()
        {
            if (_target == null)
            {
                return;
            }

            transform.position = ClampToBounds(_target.position + _offset);
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                return;
            }

            Vector3 desiredPosition = ClampToBounds(_target.position + _offset);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, _smoothSpeed * Time.deltaTime);
        }

        /// <summary>
        /// Kunci kamera di dalam CameraBounds area yang sedang ditempati target, supaya area
        /// hitam kosong di luar background tidak pernah kelihatan begitu Player mendekati tepi
        /// peta (beberapa area, misal Warung Depan, lebih kecil dari jangkauan pandang kamera).
        /// Kalau area lebih sempit dari pandangan kamera pada satu sumbu, kamera dipatok ke
        /// tengah sumbu itu saja — sisa void pada kasus itu tidak bisa dihindari tanpa zoom out.
        /// </summary>
        private Vector3 ClampToBounds(Vector3 desiredPosition)
        {
            if (_camera == null || !_camera.orthographic)
            {
                return desiredPosition;
            }

            CameraBounds bounds = CameraBounds.FindContaining(_target.position);
            if (bounds == null)
            {
                return desiredPosition;
            }

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
                // Area lebih sempit dari pandangan kamera: Mathf.Clamp dengan lo > hi akan
                // membalik hasilnya, jadi patok ke tengah area alih-alih clamp normal.
                return (min + max) * 0.5f;
            }

            return Mathf.Clamp(value, lo, hi);
        }
    }
}
