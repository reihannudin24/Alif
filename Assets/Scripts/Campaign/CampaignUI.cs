using System;
using Alif.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Alif.Campaign
{
    public static class CampaignUI
    {
        public static readonly Color Ink = new Color(.06f, .1f, .16f, .97f);
        public static readonly Color Paper = new Color(.95f, .93f, .84f);
        public static bool ReducedMotion => PlayerPrefs.GetInt("Alif_ReducedMotion", 0) == 1;
        public static bool ReducedFlash => PlayerPrefs.GetInt("Alif_ReducedFlash", 0) == 1;
        public static RectTransform Canvas(string name, int order)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            go.GetComponent<Canvas>().sortingOrder = order;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
            return (RectTransform)go.transform;
        }
        public static RectTransform Rect(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false); rt.anchorMin = min; rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero; return rt;
        }
        public static Image Panel(Transform parent, string name, Vector2 min, Vector2 max, Color color, bool blocks = false)
        {
            var image = Rect(parent, name, min, max).gameObject.AddComponent<Image>();
            image.color = color; image.raycastTarget = blocks; return image;
        }
        public static TMP_Text Text(Transform parent, string text, Vector2 min, Vector2 max, int size = 24)
        {
            var label = Rect(parent, "Text", min, max).gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text; label.fontSize = size; label.color = Paper; label.raycastTarget = false;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.margin = new Vector4(12, 6, 12, 6); return label;
        }
        public static Button Button(Transform parent, string text, Vector2 min, Vector2 max, Action action)
        {
            var image = Panel(parent, text, min, max, new Color(.30f, .18f, .11f), true);
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = new Color(.86f, .72f, .43f);
            colors.selectedColor = new Color(.86f, .72f, .43f);
            colors.pressedColor = new Color(.67f, .48f, .25f);
            colors.disabledColor = new Color(.20f, .16f, .13f, .65f);
            button.colors = colors;
            var label = Text(image.transform, text, Vector2.zero, Vector2.one, 21); label.alignment = TextAlignmentOptions.Center;
            button.onClick.AddListener(() => action()); return button;
        }
        public static void Focus(Button button) => EventSystem.current?.SetSelectedGameObject(button.gameObject);
        private static Sprite _vignetteSprite;
        /// <summary>Sprite radial alpha (transparan di tengah, pekat di tepi) untuk efek
        /// vignette darah/tekanan HP. Digambar runtime supaya tidak butuh aset.</summary>
        public static Sprite VignetteSprite()
        {
            if (_vignetteSprite != null) return _vignetteSprite;
            const int size = 160;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    Vector2 d = (new Vector2(x / (float)(size - 1), y / (float)(size - 1)) - Vector2.one * .5f) * 2f;
                    float edge = Mathf.Clamp01((d.magnitude - .55f) / .45f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Pow(edge, 1.7f));
                }
            texture.SetPixels(pixels); texture.Apply();
            _vignetteSprite = Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * .5f);
            return _vignetteSprite;
        }
        public static void Summary(int chapter, string title, string lesson, string referenceTitle, string url)
        {
            GameManager.Instance?.SetState(GameManager.GameState.Dialogue);
            var canvas = Canvas("ChapterSummary", 150);
            var panel = Panel(canvas, "Resolution", new Vector2(.12f, .1f), new Vector2(.88f, .9f), Ink, true).transform;
            Text(panel, chapter == 5 ? "ALIF  /  EPILOG" : $"CHAPTER {chapter}  /  SELESAI", new Vector2(.04f, .84f), new Vector2(.96f, .97f), 20);
            Text(panel, title, new Vector2(.04f, .69f), new Vector2(.96f, .85f), 34);
            Text(panel, lesson, new Vector2(.04f, .28f), new Vector2(.96f, .69f), 24);
            Button(panel, referenceTitle + "  ↗", new Vector2(.05f, .19f), new Vector2(.95f, .27f), () => Application.OpenURL(url));
            var next = Button(panel, chapter == 5 ? "Kembali ke menu" : "Lanjut ke chapter berikutnya", new Vector2(.05f, .05f), new Vector2(.62f, .15f), () =>
            {
                UnityEngine.Object.Destroy(canvas.gameObject);
                string scene = chapter == 5 ? "MainMenu" : chapter == 1 ? "Chapter2Cutscene" : $"Chapter{chapter + 1}Gameplay";
                GameManager.Instance?.SetState(GameManager.GameState.Playing);
                UnityEngine.SceneManagement.SceneManager.LoadScene(scene);
            });
            Button(panel, "Pilih chapter", new Vector2(.65f, .05f), new Vector2(.95f, .15f), () => UnityEngine.SceneManagement.SceneManager.LoadScene("ChapterSelect"));
            Focus(next);
        }
    }
}
