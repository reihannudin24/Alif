using UnityEngine;
using Alif.Characters;
using Alif.Dialogue;

namespace Alif.World
{
    /// <summary>
    /// Munculin monolog Alif otomatis begitu scene ini mulai (Start hanya sekali per aktivasi
    /// GameObject — jadi nggak keulang tiap kali SceneDoor mindahin Player, cuma pas scene
    /// baru pertama kali dimuat) — jembatan naratif dari cutscene pembuka ke gameplay bebas,
    /// sebelum Player sempat interaksi ke NPC lain.
    /// </summary>
    public class OpeningMonologueTrigger : MonoBehaviour
    {
        [SerializeField] private DialogueData _dialogue;
        [SerializeField] private CharacterData _speakerData;

        private void Start()
        {
            if (_dialogue == null || DialogueManager.Instance == null)
            {
                return;
            }

            DialogueManager.Instance.StartDialogue(_dialogue, _speakerData);
        }
    }
}
