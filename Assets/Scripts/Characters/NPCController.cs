using System.Collections;
using UnityEngine;
using Alif.Core;
using Alif.Dialogue;
using Alif.UI;
using Alif.World;

namespace Alif.Characters
{
    /// <summary>
    /// Komponen yang ditempel ke GameObject NPC di scene. Menghubungkan data reusable
    /// (CharacterData) dan dialog (DialogueData) dengan DialogueManager saat pemain
    /// berinteraksi (dipanggil dari PlayerController.TryInteractWithNearestNPC).
    ///
    /// Mendukung rotasi 8-arah menghadap pemain saat diajak bicara, idle look-around,
    /// emote reaction bubble, serta penyesuaian Y-sorting otomatis.
    /// </summary>
    public class NPCController : MonoBehaviour, IInteractable
    {
        [Header("Data")]
        [SerializeField] private CharacterData _characterData;
        [SerializeField] private DialogueData _defaultDialogue;

        [Header("Visual")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [Tooltip("8 rotasi: 0: South, 1: SouthEast, 2: East, 3: NorthEast, 4: North, 5: NorthWest, 6: West, 7: SouthWest")]
        [SerializeField] private Sprite[] _rotationSprites;

        [Header("Behavior")]
        [SerializeField] private bool _enableIdleLookAround = true;
        [SerializeField] private EmoteType _interactionEmote = EmoteType.Exclamation;

        public CharacterData Data => _characterData;

        private Animator _animator;
        private Sprite _defaultSprite;
        private Coroutine _idleRoutine;
        private Coroutine _resetFacingRoutine;
        private bool _isInteracting;

        private void Awake()
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (_spriteRenderer != null)
            {
                _defaultSprite = _spriteRenderer.sprite;
            }

            _animator = GetComponent<Animator>();
            if (_characterData != null && _characterData.AnimatorController != null && _animator != null)
            {
                _animator.runtimeAnimatorController = _characterData.AnimatorController;
            }

            // Pastikan Y-sorting otomatis terpasang
            if (GetComponent<YSortOrder>() == null)
            {
                gameObject.AddComponent<YSortOrder>();
            }

            // Pastikan EmoteBubble terpasang
            if (GetComponent<EmoteBubble>() == null)
            {
                gameObject.AddComponent<EmoteBubble>();
            }
        }

        private void OnEnable()
        {
            if (_enableIdleLookAround)
            {
                _idleRoutine = StartCoroutine(IdleLookAroundRoutine());
            }
        }

        private void OnDisable()
        {
            if (_idleRoutine != null)
            {
                StopCoroutine(_idleRoutine);
                _idleRoutine = null;
            }

            if (_resetFacingRoutine != null)
            {
                StopCoroutine(_resetFacingRoutine);
                _resetFacingRoutine = null;
            }

            UnsubscribeDialogueEnd();
        }

        public void SetRotationSprites(Sprite[] sprites)
        {
            _rotationSprites = sprites;
        }

        public void Interact()
        {
            _isInteracting = true;

            // Suara interaksi lembut
            AudioManager.Instance?.PlayInteract();

            // Reaksi emote di atas kepala
            EmoteBubble.ShowOn(gameObject, _interactionEmote);

            // Hadap ke arah pemain
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Vector2 toPlayer = (player.transform.position - transform.position).normalized;
                FaceDirection(toPlayer);
            }

            SubscribeDialogueEnd();
            StartDialogue();
        }

        private void SubscribeDialogueEnd()
        {
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.OnDialogueEnded -= HandleDialogueEnded;
                DialogueManager.Instance.OnDialogueEnded += HandleDialogueEnded;
            }
        }

        private void UnsubscribeDialogueEnd()
        {
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.OnDialogueEnded -= HandleDialogueEnded;
            }
        }

        private void HandleDialogueEnded()
        {
            UnsubscribeDialogueEnd();
            if (_resetFacingRoutine != null)
            {
                StopCoroutine(_resetFacingRoutine);
            }
            _resetFacingRoutine = StartCoroutine(ResetFacingAfterDelay(1.8f));
        }

        private IEnumerator ResetFacingAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            _isInteracting = false;

            if (_animator != null)
            {
                _animator.enabled = true;
            }
            else if (_defaultSprite != null && _spriteRenderer != null)
            {
                _spriteRenderer.sprite = _defaultSprite;
            }
            _resetFacingRoutine = null;
        }

        public void FaceDirection(Vector2 direction)
        {
            if (_rotationSprites == null || _rotationSprites.Length < 8 || _spriteRenderer == null)
            {
                return;
            }

            int index = VectorTo8DirectionIndex(direction);
            if (index >= 0 && index < _rotationSprites.Length && _rotationSprites[index] != null)
            {
                if (_animator != null)
                {
                    _animator.enabled = false;
                }
                _spriteRenderer.sprite = _rotationSprites[index];
            }
        }

        private static int VectorTo8DirectionIndex(Vector2 dir)
        {
            if (dir.sqrMagnitude < 0.001f) return 0; // South
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg; // -180 .. 180
            // Convert to 0..360 where 270 (down) is 0
            // 0: South (-90)
            // 1: SouthEast (-45)
            // 2: East (0)
            // 3: NorthEast (45)
            // 4: North (90)
            // 5: NorthWest (135)
            // 6: West (180/-180)
            // 7: SouthWest (-135)

            if (angle >= -112.5f && angle < -67.5f) return 0; // South
            if (angle >= -67.5f && angle < -22.5f) return 1;  // SouthEast
            if (angle >= -22.5f && angle < 22.5f) return 2;   // East
            if (angle >= 22.5f && angle < 67.5f) return 3;    // NorthEast
            if (angle >= 67.5f && angle < 112.5f) return 4;   // North
            if (angle >= 112.5f && angle < 157.5f) return 5;  // NorthWest
            if (angle >= 157.5f || angle < -157.5f) return 6; // West
            if (angle >= -157.5f && angle < -112.5f) return 7; // SouthWest

            return 0;
        }

        private IEnumerator IdleLookAroundRoutine()
        {
            while (true)
            {
                float waitTime = Random.Range(6f, 12f);
                yield return new WaitForSeconds(waitTime);

                if (_isInteracting || (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive))
                {
                    continue;
                }

                // Lirik ke kiri (West/SouthWest) atau kanan (East/SouthEast) sebentar
                if (_rotationSprites != null && _rotationSprites.Length >= 8)
                {
                    int lookDir = Random.value > 0.5f ? 1 : 7; // SouthEast vs SouthWest
                    if (_animator != null) _animator.enabled = false;
                    if (_rotationSprites[lookDir] != null && _spriteRenderer != null)
                    {
                        _spriteRenderer.sprite = _rotationSprites[lookDir];
                    }

                    yield return new WaitForSeconds(Random.Range(1.2f, 2.0f));

                    if (!_isInteracting)
                    {
                        if (_animator != null)
                        {
                            _animator.enabled = true;
                        }
                        else if (_defaultSprite != null && _spriteRenderer != null)
                        {
                            _spriteRenderer.sprite = _defaultSprite;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Dipanggil oleh PlayerController saat pemain menekan tombol Interact di dekat NPC ini.
        /// Meneruskan permintaan ke DialogueManager untuk menampilkan dialog default NPC.
        /// </summary>
        public void StartDialogue()
        {
            if (_defaultDialogue == null)
            {
                Debug.LogWarning($"NPC '{name}' tidak punya DialogueData yang di-assign.");
                return;
            }

            if (DialogueManager.Instance == null)
            {
                Debug.LogWarning("DialogueManager.Instance tidak ditemukan di scene.");
                return;
            }

            DialogueManager.Instance.StartDialogue(_defaultDialogue, _characterData);
        }
    }
}
