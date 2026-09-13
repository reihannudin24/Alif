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
            // Frame pixel-art 9-slice sebagai default (dulu persegi coklat flat) — satu bahasa
            // visual dengan hotbar/joystick: border coklat tua, isi kayu, kilau atas, bayang bawah.
            var image = Panel(parent, text, min, max, Color.white, true);
            image.sprite = PixelButtonSprite();
            image.type = Image.Type.Sliced;
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = new Color(1f, .92f, .74f);
            colors.selectedColor = new Color(1f, .92f, .74f);
            colors.pressedColor = new Color(.74f, .62f, .42f);
            colors.disabledColor = new Color(.45f, .45f, .45f, .6f);
            button.colors = colors;
            var label = Text(image.transform, text, Vector2.zero, Vector2.one, 21); label.alignment = TextAlignmentOptions.Center; label.color = Paper;
            button.onClick.AddListener(() => action()); return button;
        }
        public static void Focus(Button button) => EventSystem.current?.SetSelectedGameObject(button.gameObject);
        private static Sprite _vignetteSprite;
        private static Sprite _pixelButtonSprite;
        private static Sprite _pixelPanelSprite;
        /// <summary>Panel gelap ber-frame pixel 9-slice untuk dialogue box & panel konten —
        /// isi tinta semi-transparan, border coklat tua, kilau tipis di atas. Teks putih
        /// yang sudah dipakai panel lama tetap terbaca tanpa perubahan warna.</summary>
        public static Sprite PixelPanelSprite()
        {
            if (_pixelPanelSprite != null) return _pixelPanelSprite;
            const int size = 48;
            const int border = 5;
            var borderC = new Color(.16f, .10f, .06f, 1f);
            var fillC = new Color(.05f, .07f, .11f, .88f);
            var highlightC = new Color(.14f, .11f, .07f, 1f);
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    bool onBorder = x < border || x >= size - border || y < border || y >= size - border;
                    Color c = onBorder ? borderC : fillC;
                    if (!onBorder && y >= size - border - 2) c = highlightC; // kilau tipis tepi atas-dalam
                    pixels[y * size + x] = c;
                }
            texture.SetPixels(pixels); texture.Apply();
            _pixelPanelSprite = Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * .5f, 100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            return _pixelPanelSprite;
        }
        /// <summary>Frame tombol pixel-art 48x48 9-slice (border 5px), digambar runtime supaya
        /// tidak butuh aset — pola yang sama dengan VignetteSprite dan SlotFrame builder.</summary>
        public static Sprite PixelButtonSprite()
        {
            if (_pixelButtonSprite != null) return _pixelButtonSprite;
            const int size = 48;
            const int border = 5;
            var borderC = new Color(.24f, .14f, .08f, 1f);
            var fillC = new Color(.55f, .38f, .21f, 1f);
            var highlightC = new Color(.66f, .49f, .29f, 1f);
            var shadeC = new Color(.45f, .30f, .16f, 1f);
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    bool onBorder = x < border || x >= size - border || y < border || y >= size - border;
                    Color c = borderC;
                    if (!onBorder)
                    {
                        // Kilau tipis di tepi atas-dalam, bayang di tepi bawah-dalam (kesan tombol timbul).
                        bool highlight = y >= size - border - 2 || x < border + 2;
                        bool shade = y < border + 2 || x >= size - border - 2;
                        c = highlight && !shade ? highlightC : shade && !highlight ? shadeC : fillC;
                    }
                    pixels[y * size + x] = c;
                }
            texture.SetPixels(pixels); texture.Apply();
            _pixelButtonSprite = Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * .5f, 100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            return _pixelButtonSprite;
        }
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
                Alif.UI.SceneTransition.Load(scene);
            });
            Button(panel, "Pilih chapter", new Vector2(.65f, .05f), new Vector2(.95f, .15f), () => Alif.UI.SceneTransition.Load("ChapterSelect"));
            Focus(next);
        }
    }
}
