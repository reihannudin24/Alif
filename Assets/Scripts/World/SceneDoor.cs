using UnityEngine;
using Alif.Core;
using Alif.Player;
using Alif.UI;

namespace Alif.World
{
    /// <summary>
    /// Pintu transisi antar area dalam satu scene yang sama (bukan ganti Unity Scene, cuma
    /// pindah posisi) — begitu Player jalan masuk trigger-nya, movement dikunci & popup
    /// konfirmasi (TravelConfirmationUI) muncul dulu ("Pindah ke ...?") SEBELUM benar-benar
    /// dipindah ke posisi _destination lewat SceneFadeController (fade hitam, biar nggak
    /// keliatan "diseret" kamera) — jadi nggak langsung "loading" pas kesenggol doang. Dipakai
    /// misalnya buat pintu keluar/masuk stasiun (interior <-> eksterior).
    /// Setup: BoxCollider2D dengan isTrigger = true (di-set scene builder, bukan di sini).
    /// </summary>
    public class SceneDoor : MonoBehaviour
    {
        [SerializeField] private Transform _destination;
        [Tooltip("Teks yang ditampilkan di popup konfirmasi.")]
        [SerializeField] private string _confirmMessage = "Pindah ke area lain?";

        // Cegah popup muncul berulang tiap frame selama Player masih nyender di dalam trigger —
        // reset lagi begitu Player keluar (OnTriggerExit2D), jadi bisa dicoba ulang.
        private bool _promptShown;
        private Alif.Adventure.AdventureGame _adventure;
        public Transform Destination => _destination;
        public int SourceArea { get; private set; }
        public int DestinationArea { get; private set; }
        /// <summary>Dipakai CityWorld saat merakit pintu kota runtime (tidak ada prefab-nya).</summary>
        public void Configure(Transform destination, string confirmMessage)
        {
            _destination = destination;
            if (!string.IsNullOrEmpty(confirmMessage)) _confirmMessage = confirmMessage;
        }

        public void Bind(Alif.Adventure.AdventureGame game, int source, int destination)
        {
            _adventure = game;
            SourceArea = source;
            DestinationArea = destination;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_destination == null || !other.CompareTag("Player") || _promptShown)
            {
                return;
            }

            PlayerController player = other.GetComponent<PlayerController>();
            if (player == null)
            {
                return;
            }

            _promptShown = true;

            if (_adventure != null)
            {
                if (_adventure.CanExplore) _adventure.Travel(this);
                else _promptShown = false;
                return;
            }

            if (TravelConfirmationUI.Instance == null)
            {
                // Fallback kalau popup belum ke-setup di scene ini — pindah langsung tanpa
                // nanya, daripada Player malah nyangkut nggak kepindah sama sekali.
                Travel(other);
                return;
            }

            player.SetMovementLocked(this, true);
            TravelConfirmationUI.Instance.Show(
                _confirmMessage,
                onConfirmed: () => Travel(other),
                onCancelled: () => player.SetMovementLocked(this, false));
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                _promptShown = false;
            }
        }

        private void Travel(Collider2D other)
        {
            PlayerController player = other.GetComponent<PlayerController>();
            CameraFollow cameraFollow = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;

            if (SceneFadeController.Instance != null)
            {
                SceneFadeController.Instance.TransitionTo(player, other.attachedRigidbody, _destination, cameraFollow);
                return;
            }

            // Fallback kalau SceneFadeController belum ke-setup di scene ini — teleport langsung
            // tanpa fade, daripada Player malah nyangkut nggak kepindah sama sekali.
            Rigidbody2D rb = other.attachedRigidbody;
            if (rb != null)
            {
                rb.position = player != null ? player.ResolveSafeLandingPosition(_destination.position) : (Vector2)_destination.position;
            }
            else
            {
                other.transform.position = _destination.position;
            }

            if (player != null)
            {
                player.SetMovementLocked(this, false);
            }
        }
    }
}
