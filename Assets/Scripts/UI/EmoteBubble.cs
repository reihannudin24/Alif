using System.Collections;
using Alif.Campaign;
using TMPro;
using UnityEngine;

namespace Alif.UI
{
    public enum EmoteType
    {
        Exclamation, // !
        Question,    // ?
        Music,       // ♪
        Heart,       // ❤
        Thinking,    // ...
        Sparkle,     // ✦
        Sweat        // 💧
    }

    /// <summary>
    /// Komponen balon reaksi emosi (emote bubble) yang muncul di atas kepala karakter / NPC
    /// saat berinteraksi, kaget, senang, atau berpikir. Memberi sentuhan ekspresi RPG klasik
    /// yang imut dan membuat karakter terasa hidup.
    /// </summary>
    public class EmoteBubble : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Vector3 _offset = new Vector3(0f, 1.15f, 0f);
        [SerializeField] private float _displayDuration = 1.4f;

        private GameObject _bubbleRoot;
        private TMP_Text _emoteText;
        private Coroutine _activeRoutine;

        private void Awake()
        {
            EnsureBubbleUI();
        }

        private void EnsureBubbleUI()
        {
            if (_bubbleRoot != null) return;

            _bubbleRoot = new GameObject("EmoteBubbleView");
            _bubbleRoot.transform.SetParent(transform, false);
            _bubbleRoot.transform.localPosition = _offset;

            // Background bubble (lingkaran putih kecil)
            GameObject bgGO = new GameObject("BubbleBackground");
            bgGO.transform.SetParent(_bubbleRoot.transform, false);
            SpriteRenderer bgSr = bgGO.AddComponent<SpriteRenderer>();
            bgSr.sprite = GetOrCreateCircleSprite();
            bgSr.color = new Color(1f, 1f, 1f, 0.95f);
            bgSr.sortingLayerName = "Characters";
            bgSr.sortingOrder = 90;
            bgGO.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

            // Icon teks emotikon
            GameObject textGO = new GameObject("EmoteIcon");
            textGO.transform.SetParent(_bubbleRoot.transform, false);
            _emoteText = textGO.AddComponent<TextMeshPro>();
            _emoteText.fontSize = 3.6f;
            _emoteText.alignment = TextAlignmentOptions.Center;
            _emoteText.color = new Color(0.12f, 0.12f, 0.15f, 1f);
            _emoteText.GetComponent<MeshRenderer>().sortingLayerName = "Characters";
            _emoteText.GetComponent<MeshRenderer>().sortingOrder = 91;

            _bubbleRoot.SetActive(false);
        }

        public void Show(EmoteType type, float duration = -1f)
        {
            EnsureBubbleUI();
            if (_activeRoutine != null)
            {
                StopCoroutine(_activeRoutine);
            }

            string symbol = GetSymbol(type);
            Color symbolColor = GetSymbolColor(type);

            _emoteText.text = symbol;
            _emoteText.color = symbolColor;

            float dur = duration > 0f ? duration : _displayDuration;
            _activeRoutine = StartCoroutine(AnimateBubble(dur));
        }

        public static void ShowOn(GameObject target, EmoteType type, float duration = -1f)
        {
            if (target == null) return;
            EmoteBubble bubble = target.GetComponent<EmoteBubble>();
            if (bubble == null)
            {
                bubble = target.AddComponent<EmoteBubble>();
            }
            bubble.Show(type, duration);
        }

        private IEnumerator AnimateBubble(float duration)
        {
            // ReducedMotion: tampil-hilang tanpa pop/float — tetap terbaca, tanpa gerakan.
            if (CampaignUI.ReducedMotion)
            {
                _bubbleRoot.transform.localScale = Vector3.one;
                _bubbleRoot.transform.localPosition = _offset;
                _bubbleRoot.SetActive(true);
                yield return new WaitForSeconds(duration);
                _bubbleRoot.SetActive(false);
                _activeRoutine = null;
                yield break;
            }

            _bubbleRoot.SetActive(true);
            Vector3 basePos = _offset;
            Vector3 startScale = Vector3.zero;
            Vector3 targetScale = Vector3.one;

            // Pop in (0.18s) with elastic overshoot
            float popTime = 0.18f;
            for (float t = 0f; t < popTime; t += Time.deltaTime)
            {
                float p = t / popTime;
                float scale = Mathf.Sin(p * Mathf.PI * 0.5f) * (1f + Mathf.Sin(p * Mathf.PI) * 0.35f);
                _bubbleRoot.transform.localScale = targetScale * scale;
                _bubbleRoot.transform.localPosition = basePos + new Vector3(0f, Mathf.Sin(p * Mathf.PI * 0.5f) * 0.1f, 0f);
                yield return null;
            }

            _bubbleRoot.transform.localScale = targetScale;

            // Float gently during hold time
            float holdTime = Mathf.Max(0.2f, duration - popTime - 0.2f);
            for (float t = 0f; t < holdTime; t += Time.deltaTime)
            {
                float floatOffset = Mathf.Sin(t * 4f) * 0.04f + 0.1f;
                _bubbleRoot.transform.localPosition = basePos + new Vector3(0f, floatOffset, 0f);
                yield return null;
            }

            // Shrink / Fade out (0.2s)
            float fadeTime = 0.2f;
            for (float t = 0f; t < fadeTime; t += Time.deltaTime)
            {
                float p = 1f - (t / fadeTime);
                _bubbleRoot.transform.localScale = targetScale * p;
                yield return null;
            }

            _bubbleRoot.SetActive(false);
            _activeRoutine = null;
        }

        private static string GetSymbol(EmoteType type)
        {
            switch (type)
            {
                case EmoteType.Exclamation: return "!";
                case EmoteType.Question: return "?";
                case EmoteType.Music: return "♪";
                case EmoteType.Heart: return "♥";
                case EmoteType.Thinking: return "...";
                case EmoteType.Sparkle: return "✦";
                case EmoteType.Sweat: return "💧";
                default: return "!";
            }
        }

        private static Color GetSymbolColor(EmoteType type)
        {
            switch (type)
            {
                case EmoteType.Exclamation: return new Color(0.95f, 0.25f, 0.18f); // Merah cerah
                case EmoteType.Question: return new Color(0.18f, 0.55f, 0.95f);    // Biru cerah
                case EmoteType.Music: return new Color(0.22f, 0.78f, 0.45f);       // Hijau ceria
                case EmoteType.Heart: return new Color(0.95f, 0.2f, 0.55f);        // Pink manis
                case EmoteType.Sparkle: return new Color(0.95f, 0.75f, 0.15f);     // Kuning emas
                case EmoteType.Sweat: return new Color(0.2f, 0.65f, 0.9f);         // Biru muda
                default: return new Color(0.15f, 0.15f, 0.18f);
            }
        }

        private static Sprite _cachedCircle;
        private static Sprite GetOrCreateCircleSprite()
        {
            if (_cachedCircle != null) return _cachedCircle;
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float radius = (size - 2) * 0.5f;
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist <= radius - 1f)
                        tex.SetPixel(x, y, Color.white);
                    else if (dist <= radius)
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, radius - dist));
                    else
                        tex.SetPixel(x, y, Color.clear);
                }
            }
            tex.Apply();
            _cachedCircle = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return _cachedCircle;
        }
    }
}
