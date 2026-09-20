using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Alif.Characters;
using Alif.Core;
using Alif.UI;
using Alif.World;

namespace Alif.Player
{
    /// <summary>
    /// Enum arah hadap karakter. Dipakai PlayerAnimation untuk memilih animasi
    /// idle/walk yang sesuai, termasuk 4 arah diagonal.
    /// </summary>
    public enum FacingDirection
    {
        South,
        North,
        East,
        West,
        SouthEast,
        SouthWest,
        NorthEast,
        NorthWest
    }

    /// <summary>
    /// Controller top-down 8 arah menggunakan Rigidbody2D dan Unity Input System (baru).
    /// Menangani movement, arah hadap, dan interaksi dengan NPC terdekat.
    ///
    /// Subscribe langsung ke InputAction "Move"/"Interact" dari sebuah InputActionAsset
    /// (bukan lewat komponen PlayerInput + "Send Messages" — behavior itu ternyata rapuh,
    /// bisa lempar MissingMethodException walau method-nya sudah ada, karena reflection
    /// internalnya sensitif terhadap urutan compile/reload domain).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Input")]
        [Tooltip("Input Action Asset yang punya action map \"Player\" dengan action \"Move\" (Vector2) dan \"Interact\" (Button).")]
        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private string _actionMapName = "Player";

        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 4f;
        [SerializeField] private float _sprintMultiplier = 1.4f;
        [Tooltip("Percepatan menuju kecepatan target saat ada input (unit/detik²). Membuat start berhenti terasa instan.")]
        [SerializeField] private float _acceleration = 34f;
        [Tooltip("Perlambatan ke diam saat input dilepas (unit/detik²).")]
        [SerializeField] private float _deceleration = 46f;

        [Header("Interaction")]
        [Tooltip("Jarak maksimum untuk mendeteksi NPC terdekat saat tombol Interact ditekan.")]
        [SerializeField] private float _interactRadius = 1.2f;
        [SerializeField] private LayerMask _interactableLayer;

        [Header("References")]
        [SerializeField] private PlayerAnimation _playerAnimation;
        [Tooltip("Opsional: joystick on-screen. Kalau di-assign, nilainya digabung dengan input keyboard.")]
        [SerializeField] private VirtualJoystick _virtualJoystick;

        private Rigidbody2D _rigidbody;
        private InputAction _moveAction;
        private InputAction _interactAction;
        private Vector2 _moveInput;
        private FacingDirection _currentFacing = FacingDirection.South;
        private Vector2 _feetOffset;
        private Vector2 _feetHalfExtents = new Vector2(0.16f, 0.12f);
        private CapsuleCollider2D _feetCollider;
        private bool _isSprinting;
        private float _footstepTimer;
        private float _lastBumpTime;
        private Vector2 _smoothVelocity;
        private CameraFollow _cameraFollow;
        private bool _sprintZoomActive;
        // Buffer physics reuse untuk scan interaksi — hindari alokasi array tiap frame.
        private readonly Collider2D[] _overlapBuffer = new Collider2D[16];
        private float _promptScanTimer;
        private static readonly Dictionary<Collider2D, IInteractable> InteractableCache = new Dictionary<Collider2D, IInteractable>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticCache()
        {
            InteractableCache.Clear();
        }

        // Bendera sederhana untuk mengunci input pergerakan, misalnya saat dialog sedang berlangsung.
        private bool _movementLocked = false;
        private readonly HashSet<Object> _movementOwners = new HashSet<Object>();
        private PhysicsMaterial2D _runtimeMaterial;
        public Vector2 FacingVector => PlayerAnimation.FacingToVector(_currentFacing);
        public Vector2 MoveInput => _moveInput;
        public bool IsSprinting => _isSprinting;
        private Vector2 _adventureDirection;
        /// <summary>Mode Adventure: jangkauan interaksi lebih jauh dan langkah lebih pelan. 2 unit/detik
        /// dipilih setelah seluruh dunia dikecilkan ke 1 petak = 0,3125 unit (INTERIOR_SCALE) — pada
        /// 3 unit/detik yang lama, Alif menyeberangi satu ruangan dalam dua detik.</summary>
        public void ConfigureAdventure() { _interactableLayer = 1 << 7; _interactRadius = 1.35f; _moveSpeed = 2f; }
        public void SetAdventureDirection(Vector2 direction) { _adventureDirection = direction; }

        // Jalan otomatis (mis. adegan masuk di awal game): Player berjalan sendiri ke titik
        // tujuan lalu berhenti. Input gerak dari pemain membatalkannya dan langsung memberi
        // kontrol; batas waktu mencegah macet selamanya kalau jalurnya terhalang collider.
        private Vector2? _autoWalkTarget;
        private float _autoWalkDeadline;
        private System.Action _autoWalkFinished;

        public bool IsAutoWalking => _autoWalkTarget.HasValue;

        public void AutoWalkTo(Vector2 target, System.Action finished)
        {
            _autoWalkTarget = target;
            _autoWalkFinished = finished;
            float distance = Vector2.Distance(transform.position, target);
            _autoWalkDeadline = Time.time + distance / Mathf.Max(_moveSpeed, .1f) * 2f + 1f;
        }

        private void FinishAutoWalk()
        {
            _autoWalkTarget = null;
            System.Action finished = _autoWalkFinished;
            _autoWalkFinished = null;
            finished?.Invoke();
        }
        public bool MovementLocked => _movementLocked || _movementOwners.Count > 0;
        public bool CanStandAt(Vector2 bodyPosition)
        {
            // Edit-mode (validator/teleport safety) tidak lewat Awake — resolve malas supaya
            // collider sendiri tetap dikenali dan probe memakai geometri kaki yang sebenarnya.
            if (_rigidbody == null || _feetCollider == null)
            {
                _rigidbody = GetComponent<Rigidbody2D>();
                _feetCollider = GetComponent<CapsuleCollider2D>();
                if (_feetCollider != null)
                {
                    _feetOffset = _feetCollider.offset;
                    _feetHalfExtents = _feetCollider.size * 0.5f;
                }
            }
            Vector2 feet=FeetCenterAt(bodyPosition),size=FeetHalfExtents*2f;
            Collider2D[] hits=_feetCollider
                ?Physics2D.OverlapCapsuleAll(feet,size,_feetCollider.direction,transform.eulerAngles.z)
                :Physics2D.OverlapBoxAll(feet,size,transform.eulerAngles.z);
            foreach (Collider2D hit in hits)
                if (!hit.isTrigger && hit.attachedRigidbody != _rigidbody) return false;
            return true;
        }

        /// <summary>
        /// Posisi pendaratan teleport yang aman: tujuan pintu sudah divalidasi saat build, tapi
        /// kalau runtime ternyata tertutup collider solid (prop bergeser, objek spawn di titik
        /// door), geser ke titik berdiri terdekat yang bebas daripada menempelkan Player
        /// ke dalam blocker. Kalau semua kandidat gagal, kembalikan tujuan apa adanya.
        /// </summary>
        public Vector2 ResolveSafeLandingPosition(Vector2 desiredPosition)
        {
            if (CanStandAt(desiredPosition)) return desiredPosition;

            for (int ring = 1; ring <= 2; ring++)
            {
                float radius = 0.35f * ring;
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * (Mathf.PI * 2f / 8f);
                    Vector2 candidate = desiredPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    if (!CanStandAt(candidate)) continue;
                    // Di map ber-whitelist, kandidat juga harus di atas lantai sah supaya
                    // Player tidak mendarat di region non-walkable yang hanya tampak seperti lantai.
                    if (WalkableArea.HasAreas && !WalkableArea.ContainsPoint(FeetCenterAt(candidate))) continue;
                    return candidate;
                }
            }

            return desiredPosition;
        }
        private Vector2 FeetCenterAt(Vector2 bodyPosition) => bodyPosition+(Vector2)transform.TransformVector(_feetOffset);
        private Vector2 FeetHalfExtents => Vector2.Scale(_feetHalfExtents,new Vector2(Mathf.Abs(transform.lossyScale.x),Mathf.Abs(transform.lossyScale.y)));
        private bool InputBlocked => MovementLocked || (GameManager.Instance != null &&
            (GameManager.Instance.CurrentState == GameManager.GameState.Paused || GameManager.Instance.CurrentState == GameManager.GameState.Dialogue || GameManager.Instance.CurrentState == GameManager.GameState.Combat));
        private bool InteractionBlocked => InputBlocked;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();

            // Pastikan physics material tanpa friksi agar karakter meluncur mulus tanpa tersangkut di dinding
            PhysicsMaterial2D frictionlessMat = _runtimeMaterial = new PhysicsMaterial2D("PlayerFrictionless") { friction = 0f, bounciness = 0f };
            _rigidbody.sharedMaterial = frictionlessMat;

            _feetCollider = GetComponent<CapsuleCollider2D>();
            if (_feetCollider != null)
            {
                _feetCollider.sharedMaterial = frictionlessMat;
                _feetOffset = _feetCollider.offset;
                _feetHalfExtents = _feetCollider.size * 0.5f;
            }

            // Pastikan kedalaman render Y-sorting otomatis terpasang
            if (GetComponent<YSortOrder>() == null)
            {
                gameObject.AddComponent<YSortOrder>();
            }

            // Bayangan kaki mengikat karakter ke lantai background painted
            BlobShadow.Ensure(transform);

            // Physics2D biasanya berjalan 50 Hz, sedangkan render umumnya 60 Hz atau lebih.
            // Tanpa interpolation, sprite terlihat melompat antar-tick saat berjalan dan
            // kamera yang mengikuti Player ikut tampak tersendat.
            _rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;

            if (_playerAnimation == null)
            {
                _playerAnimation = GetComponent<PlayerAnimation>();
            }

            if (_inputActions != null)
            {
                InputActionMap map = _inputActions.FindActionMap(_actionMapName);
                if (map != null)
                {
                    _moveAction = map.FindAction("Move");
                    _interactAction = map.FindAction("Interact");
                }
            }
        }

        private void OnEnable()
        {
            _moveAction?.Enable();

            if (_interactAction != null)
            {
                _interactAction.Enable();
                _interactAction.performed += HandleInteractPerformed;
            }
        }

        private void OnDisable()
        {
            _moveAction?.Disable();

            if (_interactAction != null)
            {
                _interactAction.performed -= HandleInteractPerformed;
                _interactAction.Disable();
            }

            UpdateTalkBubble(null);
        }

        private void HandleInteractPerformed(InputAction.CallbackContext context)
        {
            TryInteractWithNearestNPC();
        }

        private void Update()
        {
            _movementOwners.RemoveWhere(owner => owner == null);
            bool shiftPressed = Keyboard.current != null && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
            _moveInput = ReadMoveInput();
            _isSprinting = shiftPressed && _moveInput.sqrMagnitude > 0.01f;

            // Sinkronkan zoom kamera sprint hanya saat statusnya berubah (bukan tiap frame).
            if (_isSprinting != _sprintZoomActive)
            {
                _sprintZoomActive = _isSprinting;
                if (_cameraFollow == null) _cameraFollow = FindAnyObjectByType<CameraFollow>();
                _cameraFollow?.SetSprintZoom(_isSprinting);
            }

            UpdateFacingDirection();
            UpdateAnimation();
            // Scan proximity interaksi tidak perlu per-frame; 12 Hz cukup responsif untuk
            // panah prompt dan jauh lebih murah daripada overlap tiap frame.
            _promptScanTimer -= Time.deltaTime;
            if (_promptScanTimer <= 0f)
            {
                _promptScanTimer = 0.08f;
                UpdateInteractionPrompt();
            }
            HandleMouseClick();
            HandleFootstepSound();
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                TryInteractWithNearestNPC();
        }

        private void HandleFootstepSound()
        {
            if (InputBlocked || _rigidbody.linearVelocity.sqrMagnitude < 0.01f)
            {
                _footstepTimer = 0.06f;
                return;
            }

            _footstepTimer -= Time.deltaTime;
            if (_footstepTimer <= 0f)
            {
                _footstepTimer = _isSprinting ? 0.22f : 0.34f;
                AudioManager.Instance?.PlayFootstep(0.08f, _isSprinting ? 0.62f : 0.42f);
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (Time.time - _lastBumpTime > 0.35f && collision.relativeVelocity.sqrMagnitude > 3.5f)
            {
                _lastBumpTime = Time.time;
                AudioManager.Instance?.PlayBump(0.65f);
            }
        }

        /// <summary>
        /// Gabungkan input keyboard (InputAction "Move") dengan joystick on-screen (kalau ada).
        /// Yang dipakai adalah mana yang magnitude-nya lebih besar, supaya pemain bisa pakai
        /// keduanya secara bebas tanpa saling mengganggu.
        /// </summary>
        private Vector2 ReadMoveInput()
        {
            if (InputBlocked)
            {
                return Vector2.zero;
            }

            Vector2 keyboardInput = _moveAction != null ? _moveAction.ReadValue<Vector2>() : Vector2.zero;
            if (keyboardInput.sqrMagnitude < 0.01f && Keyboard.current != null)
            {
                var k = Keyboard.current;
                keyboardInput = new Vector2((k.dKey.isPressed || k.rightArrowKey.isPressed ? 1 : 0) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1 : 0),
                    (k.wKey.isPressed || k.upArrowKey.isPressed ? 1 : 0) - (k.sKey.isPressed || k.downArrowKey.isPressed ? 1 : 0));
            }
            if (keyboardInput.sqrMagnitude < 0.01f)
            {
                keyboardInput = _adventureDirection;
            }
            Vector2 joystickInput = _virtualJoystick != null ? _virtualJoystick.Direction : Vector2.zero;
            Vector2 playerInput = keyboardInput.sqrMagnitude >= joystickInput.sqrMagnitude ? keyboardInput : joystickInput;

            if (_autoWalkTarget.HasValue)
            {
                Vector2 toTarget = _autoWalkTarget.Value - (Vector2)transform.position;
                bool arrived = toTarget.sqrMagnitude < .05f * .05f;
                if (playerInput.sqrMagnitude >= 0.01f || arrived || Time.time > _autoWalkDeadline)
                {
                    FinishAutoWalk();
                    return playerInput;
                }
                // Perlambat di ujung supaya berhenti tepat di tujuan tanpa lewat.
                return toTarget.normalized * Mathf.Clamp01(toTarget.magnitude / .3f + .25f);
            }

            return playerInput;
        }

        private void FixedUpdate()
        {
            if (InputBlocked)
            {
                _rigidbody.linearVelocity = Vector2.zero;
                _smoothVelocity = Vector2.zero;
                return;
            }

            // Normalize supaya gerak diagonal tidak lebih cepat dari gerak lurus. WalkableArea
            // menjadi whitelist lantai; collider physics biasa tetap menangani tembok/objek.
            float currentSpeed = _isSprinting ? (_moveSpeed * _sprintMultiplier) : _moveSpeed;
            Vector2 target = Vector2.ClampMagnitude(_moveInput, 1f) * currentSpeed;
            float rate = _moveInput.sqrMagnitude > .01f ? _acceleration : _deceleration;
            _smoothVelocity = Vector2.MoveTowards(_smoothVelocity, target, rate * Time.fixedDeltaTime);
            Vector2 velocity = ConstrainVelocityToWalkableFloor(_smoothVelocity);
            _smoothVelocity = velocity; // constraint bisa memangkas kecepatan (dinding/lantai) — jangan menumpuk selisihnya
            _rigidbody.linearVelocity = velocity;
        }

        private Vector2 ConstrainVelocityToWalkableFloor(Vector2 requestedVelocity)
        {
            if (requestedVelocity.sqrMagnitude < 0.0001f)
            {
                return Vector2.zero;
            }

            Vector2 currentFeet = FeetCenterAt(_rigidbody.position);
            Vector2 feetHalfExtents = FeetHalfExtents;

            // Hanya aktif ketika Player sedang berada di map yang punya whitelist lantai.
            // Area lain yang belum dimigrasikan tetap berjalan dengan collider lamanya.
            if (!WalkableArea.ContainsPoint(currentFeet))
            {
                return requestedVelocity;
            }

            Vector2 displacement = requestedVelocity * Time.fixedDeltaTime;
            if (WalkableArea.ContainsFootprint(currentFeet + displacement, feetHalfExtents))
            {
                return requestedVelocity;
            }

            // Coba tiap sumbu terpisah supaya Player tetap bisa meluncur menyusuri tepi lantai,
            // alih-alih terasa tersangkut total ketika input diagonal menyentuh batas.
            Vector2 xVelocity = new Vector2(requestedVelocity.x, 0f);
            Vector2 yVelocity = new Vector2(0f, requestedVelocity.y);
            bool canMoveX = Mathf.Abs(xVelocity.x) > 0.0001f
                && WalkableArea.ContainsFootprint(currentFeet + xVelocity * Time.fixedDeltaTime, feetHalfExtents);
            bool canMoveY = Mathf.Abs(yVelocity.y) > 0.0001f
                && WalkableArea.ContainsFootprint(currentFeet + yVelocity * Time.fixedDeltaTime, feetHalfExtents);

            if (canMoveX && canMoveY)
            {
                // Evaluasi apakah langkah diagonal parsial bisa lolos sebelum mengorbankan salah satu sumbu
                Vector2 partialDisplacement = requestedVelocity * 0.65f * Time.fixedDeltaTime;
                if (WalkableArea.ContainsFootprint(currentFeet + partialDisplacement, feetHalfExtents))
                {
                    return requestedVelocity * 0.65f;
                }
                return Mathf.Abs(requestedVelocity.x) >= Mathf.Abs(requestedVelocity.y) ? xVelocity : yVelocity;
            }

            if (canMoveX) return xVelocity;
            if (canMoveY) return yVelocity;
            return Vector2.zero;
        }

        /// <summary>
        /// Tentukan arah hadap (termasuk diagonal) berdasarkan vector input pergerakan.
        /// </summary>
        private void UpdateFacingDirection()
        {
            if (_moveInput.sqrMagnitude < 0.01f)
            {
                // Tidak ada input, pertahankan arah hadap terakhir (untuk idle animation).
                return;
            }

            float x = _moveInput.x;
            float y = _moveInput.y;

            // Ambang batas untuk mendeteksi gerakan diagonal murni vs lurus.
            const float diagonalThreshold = 0.4f;

            bool movingRight = x > diagonalThreshold;
            bool movingLeft = x < -diagonalThreshold;
            bool movingUp = y > diagonalThreshold;
            bool movingDown = y < -diagonalThreshold;

            if (movingUp && movingRight) _currentFacing = FacingDirection.NorthEast;
            else if (movingUp && movingLeft) _currentFacing = FacingDirection.NorthWest;
            else if (movingDown && movingRight) _currentFacing = FacingDirection.SouthEast;
            else if (movingDown && movingLeft) _currentFacing = FacingDirection.SouthWest;
            else if (movingUp) _currentFacing = FacingDirection.North;
            else if (movingDown) _currentFacing = FacingDirection.South;
            else if (movingRight) _currentFacing = FacingDirection.East;
            else if (movingLeft) _currentFacing = FacingDirection.West;
        }

        private void UpdateAnimation()
        {
            if (_playerAnimation == null)
            {
                return;
            }

            bool isMoving = _rigidbody.linearVelocity.sqrMagnitude > 0.01f;
            _playerAnimation.SetMovementState(_currentFacing, isMoving, _isSprinting);
        }

        // Keycap "E" di atas kepala Player, dibuat saat pertama kali ada target interaksi.
        private InteractionPromptFX _interactPrompt;

        // Balon chat di atas kepala NPC yang sedang jadi target. Dibuat ulang tiap ganti NPC
        // (bukan dipindah) supaya animasi pop-in FloatingPrompt ikut mengulang dari awal.
        private Transform _talkAnchor;
        private GameObject _talkBubble;

        /// <summary>
        /// Cari collider IInteractable terdekat dalam radius interact (NPC atau benda statis
        /// seperti bangku). Dipakai baik oleh tombol Interact maupun klik mouse.
        /// </summary>
        private static string HierarchyPath(Transform value)
        {
            string path = value.name + ":" + value.GetSiblingIndex();
            while (value.parent != null) { value = value.parent; path = value.name + ":" + value.GetSiblingIndex() + "/" + path; }
            return path;
        }
        public Collider2D InteractionTarget { get; private set; }
        private Collider2D FindNearestInteractableCollider()
        {
            if (InteractionBlocked) return null;
            int count = Physics2D.OverlapCircleNonAlloc(transform.position, _interactRadius, _overlapBuffer, _interactableLayer);
            Collider2D best = null;
            int bestPriority = int.MinValue;
            float bestFacing = float.MinValue, bestDistance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                Collider2D hit = _overlapBuffer[i];
                if (hit == null) continue;
                if (GetInteractable(hit) == null) continue;
                Vector2 delta = hit.ClosestPoint(transform.position) - (Vector2)transform.position;
                float distance = delta.magnitude;
                int priority = hit.GetComponentInParent<InteractionPriority>()?.Priority ?? 0;
                float facing = Vector2.Dot(FacingVector, delta.normalized) >= 0 ? 1 : 0;
                if (best == null || priority > bestPriority || (priority == bestPriority &&
                    (facing > bestFacing || (facing == bestFacing && (distance < bestDistance ||
                    (Mathf.Approximately(distance, bestDistance) && string.CompareOrdinal(HierarchyPath(hit.transform), HierarchyPath(best.transform)) < 0))))))
                { best = hit; bestPriority = priority; bestFacing = facing; bestDistance = distance; }
            }
            return best;
        }

        /// <summary>
        /// Trigger interaksi dengan objek IInteractable terdekat, kalau ada.
        /// </summary>
        private void TryInteractWithNearestNPC()
        {
            Collider2D nearest = InteractionTarget = FindNearestInteractableCollider();
            if (nearest == null)
            {
                return;
            }

            IInteractable interactable = GetInteractable(nearest);
            interactable?.Interact();
        }

        /// <summary>
        /// Tampilkan keycap "E" di atas kepala Player selama ada target interaksi dalam jangkauan,
        /// plus balon chat di atas kepala target kalau targetnya NPC. Panah "InteractionArrow"
        /// per-objek buatan builder tidak lagi ditampilkan: objek painted cuma zona tak terlihat,
        /// sehingga panahnya menutupi gambar objek itu sendiri.
        /// </summary>
        private void UpdateInteractionPrompt()
        {
            bool hasTarget = (InteractionTarget = FindNearestInteractableCollider()) != null;
            if (hasTarget && _interactPrompt == null)
            {
                var body = GetComponent<SpriteRenderer>();
                float headTop = body != null && body.sprite != null ? body.sprite.bounds.max.y : 1f;
                _interactPrompt = InteractionPromptFX.Create(transform, headTop + .12f);
            }

            if (_interactPrompt != null && _interactPrompt.gameObject.activeSelf != hasTarget)
            {
                _interactPrompt.gameObject.SetActive(hasTarget);
            }

            UpdateTalkBubble(hasTarget ? TalkAnchorOf(InteractionTarget) : null);
        }

        /// <summary>Balon chat hanya untuk NPC — bukan ATM, papan, bangku, atau titik pindah area.</summary>
        private static Transform TalkAnchorOf(Collider2D collider)
        {
            IInteractable interactable = GetInteractable(collider);
            if (interactable is NPCController npc) return npc.transform;
            // NPC quest kota: AdventurePoint-nya ada di anak "Adventure interaction", badannya di induk.
            if (interactable is Alif.Adventure.AdventurePoint point && point.IsNpc)
                return point.transform.parent != null ? point.transform.parent : point.transform;
            return null;
        }

        private void UpdateTalkBubble(Transform anchor)
        {
            if (anchor == _talkAnchor && (anchor == null || _talkBubble != null)) return;
            if (_talkBubble != null) Destroy(_talkBubble);
            _talkAnchor = anchor;
            _talkBubble = anchor != null ? SpeechBubbleFX.Create(anchor) : null;
        }

        /// <summary>
        /// Klik langsung ke NPC/benda interactable (lewat mouse, kalau masih dalam jangkauan
        /// interact) sebagai alternatif tombol Interact/E. Diabaikan kalau klik-nya kena
        /// elemen UI (tombol HUD dsb), supaya nggak dobel trigger.
        /// </summary>
        private void HandleMouseClick()
        {
            if (InteractionBlocked) return;
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            {
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            Camera cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            Vector2 worldPoint = cam.ScreenToWorldPoint(mouseScreenPos);

            Collider2D target = InteractionTarget = FindNearestInteractableCollider();
            if (target != null && target.OverlapPoint(worldPoint)) GetInteractable(target)?.Interact();
        }

        private static IInteractable GetInteractable(Collider2D collider)
        {
            if (collider == null)
            {
                return null;
            }

            // Pencarian IInteractable menaiki hierarchy mengalokasikan array GetComponents —
            // cache hasil per-collider; entri hanya sah selama MonoBehaviour-nya masih hidup
            // dan aktif (di-disable = harus dihitung ulang saat aktif kembali).
            if (InteractableCache.TryGetValue(collider, out IInteractable cached))
            {
                if (cached is MonoBehaviour cachedBehaviour && cachedBehaviour != null && cachedBehaviour.isActiveAndEnabled)
                {
                    return cached;
                }
                InteractableCache.Remove(collider);
            }

            IInteractable found = FindInteractableUpward(collider.transform);
            if (found != null)
            {
                if (InteractableCache.Count > 256) InteractableCache.Clear();
                InteractableCache[collider] = found;
            }
            return found;
        }

        private static IInteractable FindInteractableUpward(Transform start)
        {
            Transform current = start;
            while (current != null)
            {
                MonoBehaviour[] behaviours = current.GetComponents<MonoBehaviour>();
                for (int i = 0; i < behaviours.Length; i++)
                {
                    MonoBehaviour behaviour = behaviours[i];
                    if (behaviour != null && behaviour.isActiveAndEnabled && behaviour is IInteractable interactable)
                    {
                        return interactable;
                    }
                }
                current = current.parent;
            }
            return null;
        }

        /// <summary>
        /// Trigger interaksi dengan NPC terdekat. Dipanggil dari tombol Interact on-screen
        /// (Button.onClick), selain dari InputAction "Interact" via keyboard/gamepad.
        /// </summary>
        public void Interact()
        {
            TryInteractWithNearestNPC();
        }

        /// <summary>
        /// Kunci/lepas pergerakan player, dipanggil misalnya oleh DialogueManager saat dialog aktif.
        /// </summary>
        public void SetMovementLocked(Object owner, bool isLocked)
        {
            if (owner == null) return;
            if (isLocked) _movementOwners.Add(owner); else _movementOwners.Remove(owner);
            if (MovementLocked) StopMotion();
        }
        public void BindJoystick(VirtualJoystick joystick) => _virtualJoystick = joystick;
        public void StopMotion()
        {
            _moveInput = _smoothVelocity = _adventureDirection = Vector2.zero;
            if (_rigidbody != null) _rigidbody.linearVelocity = Vector2.zero;
        }
        private void OnDestroy() { if (_runtimeMaterial != null) Destroy(_runtimeMaterial); }
        public void SetMovementLocked(bool isLocked)
        {
            _movementLocked = isLocked;
            if (isLocked)
            {
                StopMotion();
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Visualisasi radius interact di Scene view untuk memudahkan tuning.
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _interactRadius);
        }
    }
}
