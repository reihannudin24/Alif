using System;
using System.Collections;
using Alif.Campaign;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Alif.Adventure
{
    /// <summary>
    /// Cutscene di dalam game dengan tampilan yang sama seperti scene cutscene (Chapter1Cutscene):
    /// ilustrasi menutupi layar, tokoh besar di dalam adegan, kotak dialog krem selebar layar di
    /// bawah dengan tab nama pembicara, efek mesin ketik, tombol LANJUT dan LEWATI.
    /// Klik di mana saja / Space / Enter = lanjut; Esc = lewati.
    /// Gambar & tokoh bisa berganti per baris lewat tag panggung (lihat <see cref="StoryLine"/>).
    /// </summary>
    public sealed class StoryOverlay : MonoBehaviour
    {
        const float CharactersPerSecond = 48f;

        string[] _lines;
        int _index = -1;
        Action _done;
        TMP_Text _speaker, _text;
        RectTransform _tab;
        Image _art, _figure;
        AspectRatioFitter _artFitter;
        Func<string, Sprite> _findArt, _findFigure;
        Coroutine _typing;
        bool _finished;

        /// <param name="findArt">Kunci gambar → sprite (StoryArtLibrary).</param>
        /// <param name="findFigure">Nama tokoh → potret besar; null/"" = tanpa tokoh.</param>
        public static StoryOverlay Play(StoryScene scene, Func<string, Sprite> findArt, Func<string, Sprite> findFigure, Action done)
        {
            var canvas = CampaignUI.Canvas("Alif • Cutscene", 80);
            var overlay = canvas.gameObject.AddComponent<StoryOverlay>();
            overlay._findArt = findArt;
            overlay._findFigure = findFigure;
            overlay.Build(canvas);
            overlay.SetArt(findArt(scene.Art));
            overlay.SetFigure(findFigure(scene.Character));
            overlay._lines = scene.Lines;
            overlay._done = done;
            overlay.Next();
            return overlay;
        }

        void Build(RectTransform canvas)
        {
            // Klik di mana saja untuk lanjut (seperti cutscene).
            var backdrop = CampaignUI.Panel(canvas, "Backdrop", Vector2.zero, Vector2.one, Color.black, true);
            var advance = backdrop.gameObject.AddComponent<Button>();
            advance.transition = Selectable.Transition.None;
            advance.onClick.AddListener(Next);

            _art = CampaignUI.Rect(backdrop.transform, "Art", Vector2.one * .5f, Vector2.one * .5f).gameObject.AddComponent<Image>();
            _art.raycastTarget = false;
            _artFitter = _art.gameObject.AddComponent<AspectRatioFitter>();
            _artFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;

            _figure = CampaignUI.Rect(backdrop.transform, "Tokoh", new Vector2(.18f, 0f), new Vector2(.62f, .98f)).gameObject.AddComponent<Image>();
            _figure.preserveAspect = true; _figure.raycastTarget = false;

            // Kotak dialog lebar: bingkai oranye isi krem, tab nama menempel di tepi atas-kiri.
            var box = CampaignUI.Panel(canvas, "Dialog", new Vector2(.035f, .03f), new Vector2(.965f, .3f), Color.white, true);
            PixelSkin.StylePanel(box);
            var boxButton = box.gameObject.AddComponent<Button>();
            boxButton.transition = Selectable.Transition.None;
            boxButton.onClick.AddListener(Next);

            _tab = CampaignUI.Rect(box.transform, "Tab nama", new Vector2(0f, 1f), new Vector2(0f, 1f));
            _tab.pivot = new Vector2(0f, .5f);
            _tab.anchoredPosition = new Vector2(28f, 0f);
            _tab.sizeDelta = new Vector2(180f, 40f);
            var tabImage = _tab.gameObject.AddComponent<Image>();
            tabImage.sprite = PixelSkin.Tab(); tabImage.type = Image.Type.Sliced; tabImage.raycastTarget = false;
            _speaker = CampaignUI.Text(_tab, "", Vector2.zero, Vector2.one, 18);
            _speaker.color = PixelSkin.TextDark; _speaker.alignment = TextAlignmentOptions.Center;
            _speaker.margin = Vector4.zero; _speaker.textWrappingMode = TextWrappingModes.NoWrap;

            _text = CampaignUI.Text(box.transform, "", Vector2.zero, Vector2.one, 24);
            _text.color = PixelSkin.TextDark; _text.alignment = TextAlignmentOptions.TopLeft;
            _text.rectTransform.offsetMin = new Vector2(34f, 64f);
            _text.rectTransform.offsetMax = new Vector2(-34f, -34f);
            _text.margin = Vector4.zero;

            var next = CampaignUI.Button(box.transform, "LANJUT >", new Vector2(.8f, .1f), new Vector2(.98f, .36f), Next);
            next.GetComponentInChildren<TMP_Text>().fontSize = 20;

            var skip = CampaignUI.Button(canvas, "LEWATI >>", new Vector2(.84f, .9f), new Vector2(.98f, .965f), Finish);
            PixelSkin.StyleButton(skip, secondary: true);
            skip.GetComponentInChildren<TMP_Text>().fontSize = 16;
        }

        void SetArt(Sprite art)
        {
            _art.enabled = art != null;
            if (art == null) return;
            _art.sprite = art;
            _artFitter.aspectRatio = art.rect.width / art.rect.height;
        }

        void SetFigure(Sprite figure)
        {
            _figure.enabled = figure != null;
            _figure.sprite = figure;
        }

        void Update()
        {
            var k = Keyboard.current;
            if (k == null || _finished) return;
            if (k.escapeKey.wasPressedThisFrame) Finish();
            else if (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame) Next();
        }

        /// <summary>Selesaikan ketikan baris ini, atau pindah ke baris berikutnya.</summary>
        void Next()
        {
            if (_finished) return;
            if (_typing != null)
            {
                StopCoroutine(_typing); _typing = null;
                _text.maxVisibleCharacters = int.MaxValue;
                return;
            }
            _index++;
            if (_index >= _lines.Length) { Finish(); return; }

            var parsed = StoryLine.Parse(_lines[_index]);
            if (parsed.Art != null) SetArt(_findArt(parsed.Art));
            if (parsed.Character != null) SetFigure(_findFigure(parsed.Character));
            string speaker = parsed.Speaker, line = parsed.Text;
            bool narrator = speaker == "Narator";
            _tab.gameObject.SetActive(!narrator);
            _speaker.text = speaker;
            _tab.sizeDelta = new Vector2(_speaker.GetPreferredValues(speaker).x + 44f, _tab.sizeDelta.y);
            _text.fontStyle = narrator ? FontStyles.Italic : FontStyles.Normal;
            _text.text = line;
            _typing = CampaignUI.ReducedMotion ? null : StartCoroutine(Type(line.Length));
            if (_typing == null) _text.maxVisibleCharacters = int.MaxValue;
        }

        IEnumerator Type(int length)
        {
            float shown = 0f;
            _text.maxVisibleCharacters = 0;
            while (shown < length)
            {
                shown += Time.unscaledDeltaTime * CharactersPerSecond;
                _text.maxVisibleCharacters = Mathf.FloorToInt(shown);
                yield return null;
            }
            _text.maxVisibleCharacters = int.MaxValue;
            _typing = null;
        }

        void Finish()
        {
            if (_finished) return;
            _finished = true;
            var done = _done;
            Destroy(gameObject);
            done?.Invoke();
        }
    }
}
