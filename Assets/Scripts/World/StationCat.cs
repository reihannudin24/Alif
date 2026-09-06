using UnityEngine;
using Alif.Characters;
using Alif.Core;
using Alif.Dialogue;
using Alif.UI;

namespace Alif.World
{
    /// <summary>
    /// Kucing stasiun "Si Belang" — elemen interaktif menyenangkan di area stasiun.
    /// Pemain bisa mengelus Si Belang untuk memicu suara meong yang imut, emote hati,
    /// dan dialog singkat yang menghangatkan suasana.
    /// </summary>
    public class StationCat : MonoBehaviour, IInteractable
    {
        [Header("Dialogue")]
        [SerializeField] private DialogueData _catDialogue;

        private EmoteBubble _emoteBubble;
        private SpriteRenderer _spriteRenderer;
        private float _lastPetTime;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _emoteBubble = GetComponent<EmoteBubble>();
            if (_emoteBubble == null)
            {
                _emoteBubble = gameObject.AddComponent<EmoteBubble>();
            }

            if (GetComponent<YSortOrder>() == null)
            {
                gameObject.AddComponent<YSortOrder>();
            }
        }

        public void Interact()
        {
            if (Time.time - _lastPetTime < 1.0f) return;
            _lastPetTime = Time.time;

            // Suara mengeong
            AudioManager.Instance?.PlayCatMeow();

            // Reaksi emote hati manis
            _emoteBubble.Show(EmoteType.Heart, 1.8f);

            // Guncang kecil / squish kucing saat dielus
            StartCoroutine(PurrSquishRoutine());

            // Tampilkan dialog jika tersedia
            if (_catDialogue != null && DialogueManager.Instance != null && !DialogueManager.Instance.IsDialogueActive)
            {
                DialogueManager.Instance.StartDialogue(_catDialogue);
            }
        }

        private System.Collections.IEnumerator PurrSquishRoutine()
        {
            Vector3 origScale = transform.localScale;
            Vector3 squishScale = new Vector3(origScale.x * 1.12f, origScale.y * 0.88f, origScale.z);

            float dur = 0.16f;
            for (float t = 0f; t < dur; t += Time.deltaTime)
            {
                float p = Mathf.Sin((t / dur) * Mathf.PI);
                transform.localScale = Vector3.Lerp(origScale, squishScale, p);
                yield return null;
            }
            transform.localScale = origScale;
        }
    }
}
