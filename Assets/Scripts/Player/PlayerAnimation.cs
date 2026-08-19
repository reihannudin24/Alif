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

        private void Awake()
        {
            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
            }
        }

        /// <summary>
        /// Dipanggil PlayerController tiap frame untuk mengirim arah hadap dan status bergerak
        /// ke Animator, supaya Blend Tree bisa memilih animasi idle/walk yang benar.
        /// </summary>
        public void SetMovementState(FacingDirection facing, bool isMoving)
        {
            Vector2 directionVector = FacingToVector(facing);

            _animator.SetFloat(MoveXHash, directionVector.x);
            _animator.SetFloat(MoveYHash, directionVector.y);
            _animator.SetBool(IsMovingHash, isMoving);
        }

        /// <summary>
        /// Konversi enum arah hadap menjadi vector 2D normalized, dipakai sebagai parameter
        /// Blend Tree di Animator (2D Freeform Directional).
        /// </summary>
        private Vector2 FacingToVector(FacingDirection facing)
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
