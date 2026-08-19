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

        public void SetTarget(Transform target)
        {
            _target = target;
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                return;
            }

            Vector3 desiredPosition = _target.position + _offset;
            transform.position = Vector3.Lerp(transform.position, desiredPosition, _smoothSpeed * Time.deltaTime);
        }
    }
}
