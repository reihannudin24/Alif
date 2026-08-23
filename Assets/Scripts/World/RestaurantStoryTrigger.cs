using UnityEngine;
using Alif.Characters;
using Alif.Dialogue;
using Alif.Systems;

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

        [Header("Dialogue")]
        [SerializeField] private DialogueData _orderDialogue;
        [SerializeField] private DialogueData _conflictDialogue;
        [SerializeField] private DialogueData _alreadyOrderedDialogue;
        [SerializeField] private DialogueData _needOrderDialogue;
        [SerializeField] private DialogueData _finishedDialogue;
        [SerializeField] private DialogueData _menuDialogue;
        [SerializeField] private CharacterData _alifData;

        [Header("Order")]
        [SerializeField] private int _ayamGeprekPrice = 18000;
        [SerializeField] private int _nasiTelurPrice = 12000;
        [SerializeField] private int _esTehPrice = 6000;
        [SerializeField] private float _energyRestoredAfterConflict = 12f;

        private StoryState _state = StoryState.NeedOrder;
        private bool _subscribed;

        private void Start()
        {
            SubscribeToDialogue();
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
                DialogueManager.Instance.StartDialogue(_conflictDialogue, _alifData);
            }
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

            int price = GetPrice(choice.EventId);
            if (price <= 0)
            {
                return;
            }

            // CurrencySystem selalu tersedia di Demo Scene. Jika project memakai scene custom
            // tanpa CurrencySystem, pesanan tetap boleh lanjut agar cerita tidak buntu.
            if (CurrencySystem.Instance == null || CurrencySystem.Instance.SpendMoney(price))
            {
                _state = StoryState.Ordered;
            }
            else
            {
                Debug.Log("[Alif] Uang Alif tidak cukup untuk memesan makanan.");
            }
        }

        private void HandleDialogueCompleted(DialogueData completedDialogue)
        {
            if (_state != StoryState.ConflictPlaying || completedDialogue != _conflictDialogue)
            {
                return;
            }

            _state = StoryState.Finished;
            if (EnergySystem.Instance != null)
            {
                EnergySystem.Instance.RestoreEnergy(_energyRestoredAfterConflict);
            }
        }

        private int GetPrice(string eventId)
        {
            switch (eventId)
            {
                case "warung.order.ayam_geprek": return _ayamGeprekPrice;
                case "warung.order.nasi_telur": return _nasiTelurPrice;
                case "warung.order.es_teh": return _esTehPrice;
                default: return 0;
            }
        }
    }

    /// <summary>
    /// Adapter kecil untuk objek interaksi di restoran. Kasir, meja, dan papan menu semuanya
    /// meneruskan aksi ke satu RestaurantStoryTrigger yang sama.
    /// </summary>
    public class RestaurantStoryPoint : MonoBehaviour, IInteractable
    {
        public enum PointType
        {
            Cashier,
            Seat,
            Menu
        }

        [SerializeField] private RestaurantStoryTrigger _story;
        [SerializeField] private PointType _pointType;

        public void Interact()
        {
            if (_story == null)
            {
                _story = FindAnyObjectByType<RestaurantStoryTrigger>();
            }

            if (_story == null)
            {
                Debug.LogWarning("[Alif] RestaurantStoryTrigger tidak ditemukan.");
                return;
            }

            switch (_pointType)
            {
                case PointType.Cashier:
                    _story.RequestOrder();
                    break;
                case PointType.Menu:
                    _story.RequestMenu();
                    break;
                default:
                    _story.RequestSeat();
                    break;
            }
        }
    }
}
