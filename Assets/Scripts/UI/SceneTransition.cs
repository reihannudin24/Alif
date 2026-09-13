using System.Collections;
using Alif.Campaign;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Alif.UI
{
    /// <summary>
    /// Transisi antar-scene dengan fade hitam pendek — dipakai di semua perpindahan scene
    /// alur game (menu → cutscene → gameplay → ending → pilih chapter) menggantikan
    /// LoadScene keras. Runner-nya dibuat runtime dan DontDestroyOnLoad supaya fade-out
    /// terjadi DI scene tujuan. Instan (tanpa fade) saat ReducedMotion aktif.
    /// </summary>
    public static class SceneTransition
    {
        private const float FadeDuration = 0.3f;
        private static Runner _active;

        public static void Load(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[Alif] SceneTransition: scene tidak tersedia di Build Settings: '{sceneName}'");
                return;
            }

            if (_active != null)
            {
                _active.Retarget(sceneName); // spam klik tombol: tujuan terakhir yang menang
                return;
            }

            var host = new GameObject("SceneTransition (runtime)");
            Object.DontDestroyOnLoad(host);
            _active = host.AddComponent<Runner>();
            _active.Begin(sceneName, CampaignUI.ReducedMotion ? 0f : FadeDuration);
        }

        private sealed class Runner : MonoBehaviour
        {
            private CanvasGroup _group;
            private string _target;
            private float _duration;

            public void Begin(string target, float duration)
            {
                _target = target;
                _duration = duration;

                var canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 40000; // di atas segalanya, termasuk LoadingOverlay
                _group = gameObject.AddComponent<CanvasGroup>();
                _group.blocksRaycasts = true; // blokir klik ganda selama transisi

                var image = new GameObject("Black").AddComponent<Image>();
                image.color = Color.black;
                image.raycastTarget = true;
                image.transform.SetParent(transform, false);
                RectTransform rect = image.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;

                StartCoroutine(Run());
            }

            public void Retarget(string target) => _target = target;

            private IEnumerator Run()
            {
                yield return Fade(0f, 1f);
                if (_target == null || !Application.CanStreamedLevelBeLoaded(_target))
                {
                    Finish();
                    yield break;
                }

                SceneManager.LoadScene(_target);
                yield return null; // beri satu frame agar scene tujuan selesai initialize
                yield return Fade(1f, 0f);
                Finish();
            }

            private void Finish()
            {
                SceneTransition._active = null;
                if (gameObject != null) Destroy(gameObject);
            }

            private IEnumerator Fade(float from, float to)
            {
                if (_duration <= 0f)
                {
                    _group.alpha = to;
                    yield break;
                }

                float t = 0f;
                while (t < _duration)
                {
                    t += Time.unscaledDeltaTime;
                    _group.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / _duration));
                    yield return null;
                }

                _group.alpha = to;
            }
        }
    }
}
