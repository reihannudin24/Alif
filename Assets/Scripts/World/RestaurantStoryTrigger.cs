using UnityEngine;
using UnityEngine.SceneManagement;
using Alif.Characters;
using Alif.Dialogue;
using Alif.Player;
using Alif.Systems;
using Alif.UI;

namespace Alif.World
{
    /// <summary>
    /// Mengatur alur Warung Bu Siti:
    /// 1. Alif memilih dan membayar makanan di kasir.
    /// 2. Alif duduk di meja untuk menunggu pesanan.
    /// 3. Cutscene konflik Bu Siti - Raka - Alif hanya dimulai setelah dua syarat tersebut terpenuhi.
    ///
    /// State dibuat eksplisit supaya pemain tidak bisa memicu konflik hanya dengan mendekati meja
    /// atau mengulang transaksi berkali-kali.
    /// </summary>
    public class RestaurantStoryTrigger : MonoBehaviour
    {
        private enum StoryState
        {
            NeedOrder,
            Ordered,
            ConflictPlaying,
            Finished
        }

        private const string OrderEventPrefix = "warung.order.";
        private const string ChapterEndingSceneName = "Chapter1Ending";

        [Header("Dialogue")]
        [SerializeField] private DialogueData _orderDialogue;
        [SerializeField] private DialogueData _conflictDialogue;
        [SerializeField] private DialogueData _alreadyOrderedDialogue;
        [SerializeField] private DialogueData _needOrderDialogue;
        [SerializeField] private DialogueData _finishedDialogue;
        [SerializeField] private DialogueData _menuDialogue;
        [SerializeField] private CharacterData _alifData;

        [Header("Order")]
        [SerializeField] private float _energyRestoredAfterConflict = 12f;

        [Header("Raka")]
        [Tooltip("Raka nonaktif dari awal (belum kenal Alif) — baru dimunculkan begitu Alif duduk menunggu pesanan, lihat RequestSeat/BeginConflictWithTimeSkip.")]
        [SerializeField] private GameObject _rakaObject;
        [SerializeField] private PlayerController _playerController;
        [SerializeField] private string _seatTimeSkipMessage = "Beberapa saat kemudian...";

        private StoryState _state = StoryState.NeedOrder;
        private bool _subscribed;
        private bool _endingTransitionStarted;

        private static readonly string[] MealObjectiveSteps =
        {
            "Masuk ke Warung Bu Siti",
            "Pesan makanan di kasir",
            "Duduk di meja dekat jendela"
        };

        private static readonly string[] ConflictObjectiveSteps =
        {
            "Bantu Bu Siti mengambil keputusan yang seimbang"
        };

        private void Start()
        {
            SubscribeToDialogue();
            ObjectiveUI.Instance?.SetObjective("Cari makan di Warung Bu Siti", MealObjectiveSteps);
        }

        private void OnDestroy()
        {
            UnsubscribeFromDialogue();
        }

        /// <summary>Dipanggil titik interaksi kasir/Bu Siti.</summary>
        public void RequestOrder()
        {
            if (!CanOpenDialogue())
            {
                return;
            }

            switch (_state)
            {
                case StoryState.NeedOrder:
                    ObjectiveUI.Instance?.SetCurrentStep(1);
                    DialogueManager.Instance.StartDialogue(_orderDialogue, _alifData);
                    break;
                case StoryState.Ordered:
                    DialogueManager.Instance.StartDialogue(_alreadyOrderedDialogue, _alifData);
                    break;
                case StoryState.Finished:
                    DialogueManager.Instance.StartDialogue(_finishedDialogue, _alifData);
                    break;
            }
        }

        /// <summary>Dipanggil titik interaksi kursi/meja.</summary>
        public void RequestSeat()
        {
            if (!CanOpenDialogue())
            {
                return;
            }

            if (_state == StoryState.NeedOrder)
            {
                DialogueManager.Instance.StartDialogue(_needOrderDialogue, _alifData);
                return;
            }

            if (_state == StoryState.Ordered)
            {
                _state = StoryState.ConflictPlaying;
                ObjectiveUI.Instance?.SetObjective("Hadapi situasi di warung", ConflictObjectiveSteps);
                BeginConflictWithTimeSkip();
            }
        }

        /// <summary>
        /// Raka belum pernah muncul di scene sebelum titik ini (nonaktif sejak dibangun scene
        /// builder). Begitu Alif duduk, layar menghitam dulu ("Beberapa saat kemudian..."),
        /// Raka baru diaktifkan/muncul sedang berhadapan dengan Bu Siti SELAGI layar masih hitam,
        /// baru dialog konflik dimulai setelah layar terang lagi — supaya kemunculannya nggak
        /// keliatan "pop-in" mendadak di depan mata pemain.
        /// </summary>
        private void BeginConflictWithTimeSkip()
        {
            if (SceneFadeController.Instance != null)
            {
                SceneFadeController.Instance.PlayTimeSkip(_seatTimeSkipMessage, _playerController,
                    () =>
                    {
                        if (_rakaObject != null)
                        {
                            _rakaObject.SetActive(true);
                        }
                    },
                    () => DialogueManager.Instance.StartDialogue(_conflictDialogue, _alifData));
                return;
            }

            // Fallback kalau SceneFadeController belum ada di scene (mis. scene custom tanpa HUD
            // lengkap) — tetap munculkan Raka & lanjut cerita walau tanpa transisi layar hitam.
            if (_rakaObject != null)
            {
                _rakaObject.SetActive(true);
            }

            DialogueManager.Instance.StartDialogue(_conflictDialogue, _alifData);
        }

        /// <summary>Dipanggil dari papan menu. Ini hanya menampilkan menu, tanpa membuat pesanan.</summary>
        public void RequestMenu()
        {
            if (!CanOpenDialogue() || _menuDialogue == null)
            {
                return;
            }

            DialogueManager.Instance.StartDialogue(_menuDialogue, _alifData);
        }

        private bool CanOpenDialogue()
        {
            SubscribeToDialogue();
            return DialogueManager.Instance != null && !DialogueManager.Instance.IsDialogueActive;
        }

        private void SubscribeToDialogue()
        {
            if (_subscribed || DialogueManager.Instance == null)
            {
                return;
            }

            DialogueManager.Instance.OnChoiceSelected += HandleChoiceSelected;
            DialogueManager.Instance.OnDialogueCompleted += HandleDialogueCompleted;
            _subscribed = true;
        }

        private void UnsubscribeFromDialogue()
        {
            if (!_subscribed || DialogueManager.Instance == null)
            {
                _subscribed = false;
                return;
            }

            DialogueManager.Instance.OnChoiceSelected -= HandleChoiceSelected;
            DialogueManager.Instance.OnDialogueCompleted -= HandleDialogueCompleted;
            _subscribed = false;
        }

        private void HandleChoiceSelected(DialogueChoice choice)
        {
            if (_state != StoryState.NeedOrder || choice == null || string.IsNullOrEmpty(choice.EventId) ||
                !choice.EventId.StartsWith(OrderEventPrefix))
            {
                return;
            }

            // DialogueManager sudah memvalidasi dan membayar MoneyCost sebelum event ini
            // dipanggil. Jika uang tidak cukup, pilihan ditolak dan event tidak diteruskan.
            _state = StoryState.Ordered;
            ObjectiveUI.Instance?.SetCurrentStep(2);
        }

        private void HandleDialogueCompleted(DialogueData completedDialogue)
        {
            if (_state != StoryState.ConflictPlaying || completedDialogue != _conflictDialogue)
            {
                return;
            }

            _state = StoryState.Finished;
            ObjectiveUI.Instance?.CompleteObjective();
            if (EnergySystem.Instance != null)
            {
                EnergySystem.Instance.RestoreEnergy(_energyRestoredAfterConflict);
            }

            StartChapterEnding();
        }

        private void StartChapterEnding()
        {
            if (_endingTransitionStarted)
            {
                return;
            }

            _endingTransitionStarted = true;
            if (!Application.CanStreamedLevelBeLoaded(ChapterEndingSceneName))
            {
                Debug.LogError($"[Alif] Scene ending chapter '{ChapterEndingSceneName}' belum tersedia di Build Settings.");
                _endingTransitionStarted = false;
                return;
            }

            SceneManager.LoadScene(ChapterEndingSceneName);
        }

    }

}
