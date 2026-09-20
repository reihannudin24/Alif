using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Alif.Campaign
{
    /// <summary>
    /// Skin UI pixel-art bergaya "papan oranye": garis luar coklat tua, badan oranye dengan
    /// kilau di atas & bayang di bawah, isi krem untuk panel/tab. Semua sprite digambar runtime
    /// (tanpa aset) dan di-9-slice; satu piksel tekstur = <see cref="PixelScale"/> unit kanvas
    /// sehingga ketebalan garis konsisten di semua ukuran panel.
    /// </summary>
    public static class PixelSkin
    {
        public const float PixelScale = 3f;

        public static readonly Color Outline = Rgb(62, 31, 16);
        public static readonly Color Orange = Rgb(228, 113, 44);
        public static readonly Color OrangeLight = Rgb(246, 166, 106);
        public static readonly Color OrangeShade = Rgb(184, 84, 32);
        public static readonly Color Cream = Rgb(245, 240, 227);
        public static readonly Color CreamShade = Rgb(221, 212, 192);
        public static readonly Color SlotEdge = Rgb(196, 170, 136);
        /// <summary>Teks gelap di atas krem.</summary>
        public static readonly Color TextDark = Rgb(58, 37, 25);
        /// <summary>Teks terang di atas oranye / dunia.</summary>
        public static readonly Color TextLight = Rgb(255, 248, 238);
        /// <summary>Penekanan (label tutorial, umpan balik) di atas krem.</summary>
        public static readonly Color Accent = OrangeShade;
        public static readonly Color Warning = Rgb(176, 42, 30);
        public static readonly Color Gold = Rgb(236, 184, 60);
        public static readonly Color GoldLight = Rgb(252, 224, 122);
        public static readonly Color GoldDark = Rgb(188, 128, 36);

        private static TMP_FontAsset _font;
        private static bool _fontLoaded;
        private static Sprite _button, _panel, _tab, _slot, _barFill, _clockIcon, _coinIcon, _boltIcon, _entranceArrow, _speechBubble, _menuIcon, _notebookIcon, _bagIcon, _chartIcon, _crescentIcon, _phoneIcon, _pinIcon, _heartIcon, _calendarIcon, _gearIcon, _phoneFrame, _appTile, _disc, _knob, _joystickBase;

        /// <summary>Font pixel (Pixelify Sans, OFL) sebagai TMP font asset dinamis. Glyph yang
        /// tidak ada (✓, ↗, …) jatuh ke font default TMP Settings. Null bila TTF hilang.</summary>
        public static TMP_FontAsset Font
        {
            get
            {
                if (_fontLoaded) return _font;
                _fontLoaded = true;
                var source = Resources.Load<Font>("Fonts/PixelifySans-SemiBold");
                if (source != null) _font = TMP_FontAsset.CreateFontAsset(source);
                return _font;
            }
        }

        /// <summary>Tombol oranye: kilau 1px di atas, bayang 2px di bawah, sudut terpotong.</summary>
        public static Sprite Button() => _button ??= Sliced("PixelSkinButton", 12, 1, 4, (y, ring, size) =>
            ring == 0 ? Outline : y == size - 2 ? OrangeLight : y <= 2 ? OrangeShade : Orange);

        /// <summary>Panel berbingkai oranye tebal dengan isi krem (kotak dialog / kartu).</summary>
        public static Sprite Panel() => _panel ??= Sliced("PixelSkinPanel", 16, 2, 6, (y, ring, size) =>
            ring switch { 0 => Outline, 1 => OrangeLight, 2 or 3 => Orange, 4 => OrangeShade, _ => Cream });

        /// <summary>Tab/label krem berbingkai oranye (nama area, judul kartu).</summary>
        public static Sprite Tab() => _tab ??= Sliced("PixelSkinTab", 12, 2, 4, (y, ring, size) =>
            ring switch { 0 => Outline, 1 or 2 => Orange, _ => Cream });

        /// <summary>Slot inventory: kotak krem bergaris tipis dengan bayang dalam di atas.</summary>
        public static Sprite Slot() => _slot ??= Sliced("PixelSkinSlot", 8, 1, 3, (y, ring, size) =>
            ring == 0 ? SlotEdge : y == size - 2 ? CreamShade : Cream);

        /// <summary>Isi bar status (putih, diwarnai lewat Image.color): kilau 1px di atas,
        /// bayang 1px di bawah, badan sedikit lebih gelap dari kilau.</summary>
        public static Sprite BarFill() => _barFill ??= Sliced("PixelSkinBarFill", 6, 0, 2, (y, ring, size) =>
            y == size - 1 ? Color.white : y == 0 ? new Color(.66f, .66f, .66f) : new Color(.86f, .86f, .86f));

        /// <summary>Ikon jam: muka krem bercincin oranye, jarum coklat tua.</summary>
        public static Sprite ClockIcon() => _clockIcon ??= Round("PixelSkinClock", 13, (x, y, d, r) =>
            d > r - 1.3f ? Outline : d > r - 2.6f ? Orange
            : (x == 6 && y >= 6 && y <= 10) || (y == 6 && x >= 6 && x <= 9) ? Outline : Cream);

        /// <summary>Ikon koin emas dengan kilau cincin dalam dan garis tengah.</summary>
        public static Sprite CoinIcon() => _coinIcon ??= Round("PixelSkinCoin", 13, (x, y, d, r) =>
            d > r - 1.3f ? Outline : d > r - 2.3f ? GoldLight
            : x == 6 && y >= 4 && y <= 8 ? GoldDark : Gold);

        /// <summary>Ikon petir kuning (energi).</summary>
        public static Sprite BoltIcon() => _boltIcon ??= PatternSprite("PixelSkinBolt", new[]
        {
            "....oooo",
            "...oYYo.",
            "..oYYo..",
            ".oYYYooo",
            "oYYYYYYo",
            "oooYYYo.",
            "..oYYo..",
            ".oYYo...",
            ".oYo....",
            ".oo.....",
        });

        /// <summary>Panah bawah penanda pintu masuk (dipakai di atas pintu gedung kota).</summary>
        public static Sprite EntranceArrow() => _entranceArrow ??= PatternSprite("PixelSkinEntranceArrow", new[]
        {
            "...oooo...",
            "...oSSo...",
            "...oSSo...",
            "...oSSo...",
            "ooooSSoooo",
            "oSSSSSSSSo",
            ".oSSSSSSo.",
            "..oSSSSo..",
            "...oSSo...",
            "....oo....",
        });

        /// <summary>Balon chat "…" — penanda NPC yang bisa diajak bicara. Ekornya di kiri-bawah,
        /// jadi balonnya dipasang agak ke kanan dari kepala NPC.</summary>
        public static Sprite SpeechBubble() => _speechBubble ??= PatternSprite("PixelSkinSpeechBubble", new[]
        {
            "...ooooooooo...",
            "..occccccccco..",
            "..occccccccco..",
            "..ockcckcckco..",
            "..ockcckcckco..",
            "..occccccccco..",
            "..occccccccco..",
            "...occoooooo...",
            "...occo........",
            "..occo.........",
            ".occo..........",
            ".ooo...........",
        });

        /// <summary>Ikon menu (tiga garis) untuk tombol Jeda.</summary>
        public static Sprite MenuIcon() => _menuIcon ??= PatternSprite("PixelSkinMenu", new[]
        {
            "oooooooooooo",
            "occcccccccco",
            "oooooooooooo",
            "............",
            "oooooooooooo",
            "occcccccccco",
            "oooooooooooo",
            "............",
            "oooooooooooo",
            "occcccccccco",
            "oooooooooooo",
        });

        /// <summary>Ikon buku catatan bersampul dengan garis halaman (Jurnal).</summary>
        public static Sprite NotebookIcon() => _notebookIcon ??= PatternSprite("PixelSkinNotebook", new[]
        {
            "ooooooooooo",
            "oSSccccccco",
            "oSScllllcco",
            "oSSccccccco",
            "oSScllllcco",
            "oSSccccccco",
            "oSScllllcco",
            "oSSccccccco",
            "oSScllllcco",
            "oSSccccccco",
            "oSSccccccco",
            "ooooooooooo",
        });

        /// <summary>Ikon tas berpegangan dengan gesper emas (Tas).</summary>
        public static Sprite BagIcon() => _bagIcon ??= PatternSprite("PixelSkinBag", new[]
        {
            "....oooo....",
            "...o....o...",
            "...o....o...",
            ".oooooooooo.",
            "occcccccccco",
            "occcccccccco",
            "oooooooooooo",
            "occccYYcccco",
            "occcccccccco",
            "occcccccccco",
            ".oooooooooo.",
        });

        /// <summary>Ikon grafik batang naik (Logika Finansial).</summary>
        public static Sprite ChartIcon() => _chartIcon ??= PatternSprite("PixelSkinChart", new[]
        {
            ".......ooo",
            ".......oYo",
            "....ooooYo",
            "....oYooYo",
            ".ooooYooYo",
            ".oYooYooYo",
            ".oYooYooYo",
            "oooooooooo",
        });

        /// <summary>Ikon bulan sabit (Kepatuhan Syariah).</summary>
        public static Sprite CrescentIcon() => _crescentIcon ??= PatternSprite("PixelSkinCrescent", new[]
        {
            "..oooo...",
            ".oYYo....",
            "oYYo.....",
            "oYYo.....",
            "oYYo.....",
            "oYYo.....",
            ".oYYo....",
            "..oooo...",
        });

        /// <summary>Ikon HP (tombol HUD).</summary>
        public static Sprite PhoneIcon() => _phoneIcon ??= PatternSprite("PixelSkinPhone", new[]
        {
            "oooooooo",
            "okkkkkko",
            "okbbbbko",
            "okbbbbko",
            "okbbbbko",
            "okbbbbko",
            "okbbbbko",
            "okbbbbko",
            "okkkkkko",
            "okkcckko",
            "okkkkkko",
            "oooooooo",
        });

        /// <summary>Pin lokasi (aplikasi Peta).</summary>
        public static Sprite PinIcon() => _pinIcon ??= PatternSprite("PixelSkinPin", new[]
        {
            "..ooooo..",
            ".occccco.",
            "occcoccco",
            "occo.occo",
            "occcoccco",
            ".occccco.",
            "..occco..",
            "...oco...",
            "....o....",
        });

        /// <summary>Hati (aplikasi Kontak / hubungan).</summary>
        public static Sprite HeartIcon() => _heartIcon ??= PatternSprite("PixelSkinHeart", new[]
        {
            ".oo...oo.",
            "occo.occo",
            "occcoccco",
            "occccccco",
            ".occccco.",
            "..occco..",
            "...oco...",
            "....o....",
        });

        /// <summary>Kalender (aplikasi Kalender).</summary>
        public static Sprite CalendarIcon() => _calendarIcon ??= PatternSprite("PixelSkinCalendar", new[]
        {
            ".o......o.",
            "oooooooooo",
            "oSSSSSSSSo",
            "oooooooooo",
            "occcccccco",
            "oclclclcco",
            "occcccccco",
            "oclclclcco",
            "occcccccco",
            "oooooooooo",
        });

        /// <summary>Roda gigi (aplikasi Pengaturan).</summary>
        public static Sprite GearIcon() => _gearIcon ??= Round("PixelSkinGear", 13, (x, y, d, r) =>
        {
            float angle = Mathf.Atan2(y - 6f, x - 6f);
            float edge = Mathf.Cos(angle * 8f) > .25f ? r - .4f : r - 2.4f;
            if (d > edge) return Color.clear;
            if (d > edge - 1.1f || (d < 2.7f && d >= 1.5f)) return Outline;
            return d < 1.5f ? Color.clear : Cream;
        });

        /// <summary>Bingkai HP: badan abu tua bergaris luar, layar hitam di dalam.</summary>
        public static Sprite PhoneFrame() => _phoneFrame ??= Sliced("PixelSkinPhoneFrame", 16, 3, 6, (y, ring, size) =>
            ring switch { 0 => Outline, 1 => Rgb(96, 96, 108), 2 or 3 => Rgb(62, 62, 72), 4 => Rgb(36, 36, 42), _ => Rgb(16, 16, 20) });

        /// <summary>Ubin aplikasi putih (diwarnai lewat Image.color): kilau atas, bayang bawah.</summary>
        public static Sprite AppTile() => _appTile ??= Sliced("PixelSkinAppTile", 12, 2, 4, (y, ring, size) =>
            ring == 0 ? Outline : y == size - 2 ? Color.white : y <= 2 ? new Color(.7f, .7f, .7f) : new Color(.88f, .88f, .88f));

        /// <summary>Tombol bulat oranye (tombol interaksi).</summary>
        public static Sprite Disc() => _disc ??= Circle("PixelSkinDisc", 26, 0);

        public static Sprite JoystickKnob() => _knob ??= Circle("PixelSkinKnob", 16, 0);

        /// <summary>Cincin oranye dengan tengah gelap transparan (alas joystick).</summary>
        public static Sprite JoystickBase() => _joystickBase ??= Circle("PixelSkinJoystickBase", 36, 3);

        /// <summary>Pasang skin pada tombol yang sudah ada (dibuat builder / scene). Tombol
        /// sekunder (mis. Keluar) memakai tab krem bertinta gelap agar kalah menonjol.</summary>
        public static void StyleButton(UnityEngine.UI.Button button, bool secondary = false)
        {
            if (button.targetGraphic is Image image)
            {
                image.sprite = secondary ? Tab() : Button();
                image.type = Image.Type.Sliced;
                image.color = Color.white;
            }
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, .93f, .80f);
            colors.selectedColor = new Color(1f, .93f, .80f);
            colors.pressedColor = new Color(.78f, .66f, .58f);
            colors.disabledColor = new Color(.62f, .58f, .55f, .75f);
            button.colors = colors;
            foreach (var label in button.GetComponentsInChildren<TMP_Text>(true))
            {
                if (Font != null) label.font = Font;
                label.fontStyle = FontStyles.Normal; // font sudah tebal; faux-bold merusak grid piksel
                label.color = secondary ? TextDark : TextLight;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void HookSceneLoads()
        {
            SceneManager.sceneLoaded -= SkinScene;
            SceneManager.sceneLoaded += SkinScene;
        }

        /// <summary>Tombol di scene dibangun builder sebagai kotak warna flat (tanpa sprite);
        /// semuanya diberi skin saat scene dimuat supaya satu gaya di seluruh game.</summary>
        private static void SkinScene(Scene scene, LoadSceneMode mode)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var button in root.GetComponentsInChildren<UnityEngine.UI.Button>(true))
                {
                    if (!(button.targetGraphic is Image image)) continue;
                    // Lewati tombol bersprite (ikon), penangkap klik transparan, dan panel dialog
                    // yang kebetulan juga Button.
                    if (image.sprite != null || image.color.a < .01f || button.name == "Dialogue_Panel") continue;
                    if (button.name.StartsWith("ChapterCard"))
                    {
                        // Kartu bab berisi konten sendiri: cukup bingkai tab tipis, teks tetap.
                        image.sprite = Tab(); image.type = Image.Type.Sliced; image.color = Color.white;
                        continue;
                    }
                    // Tombol yang sengaja dibuat redup (Lewati, Keluar) menjadi varian sekunder.
                    StyleButton(button, secondary: image.color.a < .95f);
                }
        }

        /// <summary>Ubah panel/kartu yang sudah ada menjadi bingkai oranye berisi krem.</summary>
        public static void StylePanel(Image panel)
        {
            panel.sprite = Panel();
            panel.type = Image.Type.Sliced;
            panel.color = Color.white;
        }

        private static Sprite Sliced(string name, int size, int cornerCut, int border, Func<int, int, int, Color> shade)
        {
            var texture = NewTexture(name, size);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    // Jarak cincin dari tepi, dengan sudut dipotong diagonal agar tampak membulat.
                    int dx = Mathf.Min(x, size - 1 - x), dy = Mathf.Min(y, size - 1 - y);
                    int ring = Mathf.Min(Mathf.Min(dx, dy), dx + dy - cornerCut);
                    pixels[y * size + x] = ring < 0 ? Color.clear : shade(y, ring, size);
                }
            texture.SetPixels(pixels); texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * .5f, 100f / PixelScale, 0,
                SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }

        private static Sprite Circle(string name, int size, int hollowRing)
        {
            var texture = NewTexture(name, size);
            var pixels = new Color[size * size];
            float center = (size - 1) * .5f, radius = size * .5f;
            var hollow = new Color(Outline.r, Outline.g, Outline.b, .4f);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float up = y - center; // tekstur: y besar = atas
                    Color c;
                    if (d > radius - .3f) c = Color.clear;
                    else if (d > radius - 1.3f) c = Outline;
                    else if (hollowRing > 0 && d < radius - 1.3f - hollowRing) c = d > radius - 2.3f - hollowRing ? Outline : hollow;
                    else if (up > radius * .45f && d > radius - 2.6f) c = OrangeLight;
                    else if (up < -radius * .45f && d > radius - 3.3f) c = OrangeShade;
                    else c = Orange;
                    pixels[y * size + x] = c;
                }
            texture.SetPixels(pixels); texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * .5f, 100f / PixelScale);
        }

        /// <summary>Sprite bulat piksel: shade(x, y, jarak ke pusat, radius); di luar radius transparan.</summary>
        private static Sprite Round(string name, int size, Func<int, int, float, float, Color> shade)
        {
            var texture = NewTexture(name, size);
            var pixels = new Color[size * size];
            float center = (size - 1) * .5f, radius = size * .5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    pixels[y * size + x] = d > radius - .3f ? Color.clear : shade(x, y, d, radius);
                }
            texture.SetPixels(pixels); texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * .5f, 100f / PixelScale);
        }

        /// <summary>Sprite dari pola ASCII (baris atas dulu): o = garis, Y = kuning, c = krem,
        /// S = oranye tua, l = garis halaman, g = hijau, p = merah muda, b = biru, . = kosong.</summary>
        public static Sprite PatternSprite(string name, string[] rows)
        {
            int width = rows[0].Length, height = rows.Length;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { name = name, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[width * height];
            for (int r = 0; r < height; r++)
                for (int x = 0; x < width; x++)
                {
                    char c = rows[r][x];
                    pixels[(height - 1 - r) * width + x] = c switch
                    {
                        'o' => Outline, 'Y' => GoldLight, 'c' => Cream, 'S' => OrangeShade, 'l' => SlotEdge,
                        'g' => Rgb(92, 158, 96), 'p' => Rgb(240, 150, 176), 'b' => Rgb(120, 160, 214), 'k' => Rgb(58, 58, 68),
                        _ => Color.clear,
                    };
                }
            texture.SetPixels(pixels); texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, width, height), Vector2.one * .5f, 100f / PixelScale);
        }

        private static Texture2D NewTexture(string name, int size) =>
            new Texture2D(size, size, TextureFormat.RGBA32, false) { name = name, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };

        private static Color Rgb(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f);
    }
}
