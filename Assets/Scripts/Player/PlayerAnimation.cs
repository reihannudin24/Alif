using UnityEngine;

namespace Alif.Player
{
    /// <summary>
    /// Menjembatani PlayerController dengan Animator. Bertugas mengatur parameter Animator
    /// supaya animasi idle/walk berganti sesuai arah hadap (South, North, East, West, dan diagonal).
    ///
    /// Setup Animator Controller yang disarankan: gunakan 2 parameter float "MoveX" dan "MoveY"
    /// (nilai arah hadap terakhir, -1..1) plus 1 parameter bool "IsMoving". Blend Tree di Animator
    /// bisa memakai MoveX/MoveY sebagai 2D Freeform Directional untuk memilih clip yang tepat,
    /// termasuk untuk 4 arah diagonal.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimation : MonoBehaviour
    {
        private static readonly int MoveXHash = Animator.StringToHash("MoveX");
        private static readonly int MoveYHash = Animator.StringToHash("MoveY");
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

        [SerializeField] private Animator _animator;
        [SerializeField] private FootstepDustEffect _footstepDust;

        [Header("Procedural Juice")]
        [SerializeField] private bool _enableIdleBreathing = true;
        [SerializeField] private float _breathingSpeed = 2.4f;
        [SerializeField] private float _breathingAmount = 0.02f;

        private Vector3 _baseScale = Vector3.one;
        private bool _wasMoving;

        private void Awake()
        {
            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
            }

            if (_footstepDust == null)
            {
                _footstepDust = GetComponent<FootstepDustEffect>();
                if (_footstepDust == null)
                {
                    _footstepDust = gameObject.AddComponent<FootstepDustEffect>();
                }
            }

            _baseScale = transform.localScale;
            if (_baseScale.sqrMagnitude < 0.001f)
            {
                _baseScale = Vector3.one;
            }
        }

        private void LateUpdate()
        {
            if (!_enableIdleBreathing) return;

            if (!_wasMoving)
            {
                // Subtle breathing squash and stretch
                float sin = Mathf.Sin(Time.time * _breathingSpeed);
                float scaleY = 1f + sin * _breathingAmount;
                float scaleX = 1f - sin * (_breathingAmount * 0.6f);
                transform.localScale = new Vector3(_baseScale.x * scaleX, _baseScale.y * scaleY, _baseScale.z);
            }
            else
            {
                // Smoothly restore normal scale while walking
                transform.localScale = Vector3.Lerp(transform.localScale, _baseScale, Time.deltaTime * 10f);
            }
        }

        /// <summary>
        /// Dipanggil PlayerController tiap frame untuk mengirim arah hadap dan status bergerak
        /// ke Animator, serta mengatur laju animasi dan efek debu langkah.
        /// </summary>
        public void SetMovementState(FacingDirection facing, bool isMoving, bool isSprinting = false)
        {
            _wasMoving = isMoving;
            Vector2 directionVector = FacingToVector(facing);

            if (_animator != null)
            {
                _animator.SetFloat(MoveXHash, directionVector.x);
                _animator.SetFloat(MoveYHash, directionVector.y);
                _animator.SetBool(IsMovingHash, isMoving);

                // Kecepatan putar animasi meningkat saat sprint agar langkah selaras dengan laju gerak
                _animator.speed = isMoving ? (isSprinting ? 1.4f : 1.05f) : 1f;
            }

            if (isMoving && _footstepDust != null)
            {
                _footstepDust.SpawnDust(isSprinting, directionVector);
            }
        }

        /// <summary>
        /// Konversi enum arah hadap menjadi vector 2D normalized, dipakai sebagai parameter
        /// Blend Tree di Animator (2D Freeform Directional).
        /// </summary>
        public static Vector2 FacingToVector(FacingDirection facing)
        {
            switch (facing)
            {
                case FacingDirection.North: return new Vector2(0f, 1f);
                case FacingDirection.South: return new Vector2(0f, -1f);
                case FacingDirection.East: return new Vector2(1f, 0f);
                case FacingDirection.West: return new Vector2(-1f, 0f);
                case FacingDirection.NorthEast: return new Vector2(1f, 1f).normalized;
                case FacingDirection.NorthWest: return new Vector2(-1f, 1f).normalized;
                case FacingDirection.SouthEast: return new Vector2(1f, -1f).normalized;
                case FacingDirection.SouthWest: return new Vector2(-1f, -1f).normalized;
                default: return Vector2.down;
            }
        }
    }
}
