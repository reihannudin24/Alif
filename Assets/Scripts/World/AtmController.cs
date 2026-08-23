using UnityEngine;
using Alif.Characters;
using Alif.Player;
using Alif.UI;

namespace Alif.World
{
    /// <summary>
    /// Mesin ATM di dunia — interact buka AtmUI (tarik tunai dari saldo bank ke uang kantong).
    /// Setup: taruh di layer "Interactable", tambahkan Collider2D.
    /// </summary>
    public class AtmController : MonoBehaviour, IInteractable
    {
        public void Interact()
        {
            if (AtmUI.Instance == null)
            {
                Debug.LogWarning("[Alif] AtmUI.Instance tidak ditemukan di scene.");
                return;
            }

            // IInteractable.Interact() sengaja tanpa parameter (kontrak sama buat NPC & benda
            // statis) — jadi player-nya dicari lewat tag, bukan diteruskan langsung. Aman karena
            // game ini single-player (cuma satu GameObject bertag "Player").
            GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
            PlayerController player = playerGO != null ? playerGO.GetComponent<PlayerController>() : null;

            AtmUI.Instance.Show(player);
        }
    }
}
