using System.Collections;
using UnityEngine;
using Alif.Core;
using Alif.Player;

namespace Alif.UI
{
    /// <summary>
    /// Overlay hitam full-screen ("layar loading") buat transisi antar area dalam satu scene
    /// yang sama — dipakai SceneDoor supaya teleport Player nggak kelihatan "diseret" kamera
    /// (CameraFollow pakai Lerp; jarak jauh bakal keliatan geser cepat kalau nggak ditutupin
    /// fade dulu). Movement Player dikunci selama transisi berlangsung.
    /// </summary>
    public class SceneFadeController : MonoBehaviour
    {
        public static SceneFadeController Instance { get; private set; }

        [SerializeField] private CanvasGroup _fadeCanvasGroup;
        [SerializeField] private float _fadeDuration = 0.25f;
        [SerializeField] private float _holdDuration = 0.2f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void TransitionTo(PlayerController player, Rigidbody2D playerRb, Transform destination, CameraFollow cameraFollow)
        {
            StartCoroutine(TransitionRoutine(player, playerRb, destination, cameraFollow));
        }

        private IEnumerator TransitionRoutine(PlayerController player, Rigidbody2D playerRb, Transform destination, CameraFollow cameraFollow)
        {
            if (player != null)
            {
                player.SetMovementLocked(true);
            }

            yield return Fade(0f, 1f);

            if (playerRb != null)
            {
                playerRb.position = destination.position;
            }

            if (cameraFollow != null)
            {
                cameraFollow.SnapToTarget();
            }

            yield return new WaitForSeconds(_holdDuration);
            yield return Fade(1f, 0f);

            if (player != null)
            {
                player.SetMovementLocked(false);
            }
        }

        private IEnumerator Fade(float from, float to)
        {
            if (_fadeCanvasGroup == null)
            {
                yield break;
            }

            _fadeCanvasGroup.blocksRaycasts = to > 0.5f;

            float t = 0f;
            while (t < _fadeDuration)
            {
                t += Time.deltaTime;
                _fadeCanvasGroup.alpha = Mathf.Lerp(from, to, t / _fadeDuration);
                yield return null;
            }

            _fadeCanvasGroup.alpha = to;
        }
    }
}
