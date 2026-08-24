using System;
using System.Collections;
using TMPro;
using UnityEngine;
using Alif.Core;
using Alif.Player;

namespace Alif.UI
{
    /// <summary>
    /// Overlay hitam full-screen ("layar loading") buat transisi antar area dalam satu scene
    /// yang sama — dipakai SceneDoor supaya teleport Player nggak kelihatan "diseret" kamera
    /// (CameraFollow pakai Lerp; jarak jauh bakal keliatan geser cepat kalau nggak ditutupin
    /// fade dulu). Movement Player dikunci selama transisi berlangsung. Overlay yang sama juga
    /// dipakai PlayTimeSkip buat beat naratif "Beberapa saat kemudian..." (mis. RestaurantStoryTrigger
    /// begitu Alif duduk menunggu pesanan) — teksnya di-override sementara, bukan overlay baru.
    /// </summary>
    public class SceneFadeController : MonoBehaviour
    {
        public static SceneFadeController Instance { get; private set; }

        [SerializeField] private CanvasGroup _fadeCanvasGroup;
        [SerializeField] private TMP_Text _fadeText;
        [SerializeField] private float _fadeDuration = 0.25f;
        [SerializeField] private float _holdDuration = 0.2f;
        [SerializeField] private float _timeSkipHoldDuration = 1.4f;

        private const string DefaultFadeText = "Memuat...";

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

        /// <summary>
        /// Beat naratif "layar hitam + teks, tahan sebentar, terang lagi" — dipakai buat lompatan
        /// waktu singkat di tengah scene (mis. Raka baru muncul begitu Alif duduk menunggu
        /// pesanan). <paramref name="duringBlackout"/> dipanggil SELAGI layar masih hitam penuh
        /// (pas buat SetActive/reposisi objek supaya nggak keliatan "pop-in"); <paramref name="onComplete"/>
        /// dipanggil setelah layar terang kembali (pas buat mulai dialog lanjutan).
        /// </summary>
        public void PlayTimeSkip(string message, PlayerController player, Action duringBlackout, Action onComplete = null)
        {
            StartCoroutine(TimeSkipRoutine(message, player, duringBlackout, onComplete));
        }

        private IEnumerator TimeSkipRoutine(string message, PlayerController player, Action duringBlackout, Action onComplete)
        {
            if (player != null)
            {
                player.SetMovementLocked(true);
            }

            if (_fadeText != null)
            {
                _fadeText.text = message;
            }

            yield return Fade(0f, 1f);

            duringBlackout?.Invoke();

            yield return new WaitForSeconds(_timeSkipHoldDuration);
            yield return Fade(1f, 0f);

            if (_fadeText != null)
            {
                _fadeText.text = DefaultFadeText;
            }

            if (player != null)
            {
                player.SetMovementLocked(false);
            }

            onComplete?.Invoke();
        }

        private IEnumerator TransitionRoutine(PlayerController player, Rigidbody2D playerRb, Transform destination, CameraFollow cameraFollow)
        {
            if (player != null)
            {
                player.SetMovementLocked(true);
            }

            if (_fadeText != null)
            {
                _fadeText.text = DefaultFadeText;
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
