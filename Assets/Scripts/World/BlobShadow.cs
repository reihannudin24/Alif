using UnityEngine;

namespace Alif.World
{
    /// <summary>
    /// Bayangan ellipse lembut di kaki karakter. Background Alif adalah art painted, bukan
    /// tilemap — tanpa bayangan, karakter pixel-art tampak "melayang" di atas lantai.
    /// Bayangan mengikuti sortingOrder renderer karakter dikurangi satu sehingga selalu
    /// tergambar tepat di bawah badan karakter, di atas lantai.
    /// </summary>
    public sealed class BlobShadow : MonoBehaviour
    {
        private const int TextureWidth = 48;
        private const int TextureHeight = 24;

        private static Sprite _sharedSprite;

        private SpriteRenderer _renderer;
        private SpriteRenderer _ownerRenderer;
        private int _lastOwnerOrder = int.MinValue;

        /// <summary>
        /// Pasang bayangan di bawah karakter (idempotent). localFeetOffset adalah posisi
        /// titik kaki relatif terhadap pivot karakter.
        /// </summary>
        public static BlobShadow Ensure(Transform owner, Vector3 localFeetOffset, float width = 0.55f)
        {
            BlobShadow existing = owner.GetComponentInChildren<BlobShadow>(true);
            if (existing != null) return existing;

            var go = new GameObject("BlobShadow");
            go.transform.SetParent(owner, false);
            go.transform.localPosition = localFeetOffset;
            var shadow = go.AddComponent<BlobShadow>();
            shadow.Initialize(width);
            return shadow;
        }

        private void Initialize(float width)
        {
            // AddComponent memanggil Awake seketika — Initialize dipanggil ulang oleh Ensure(),
            // jadi harus idempotent. Duplikat SpriteRenderer dulu melempar NRE yang memutus
            // sisa Awake pemanggilnya (termasuk wiring input PlayerController).
            if (_renderer != null) return;

            _ownerRenderer = GetComponentInParent<SpriteRenderer>();
            _renderer = gameObject.AddComponent<SpriteRenderer>();
            _renderer.sprite = GetOrCreateSharedSprite();
            _renderer.color = new Color(0f, 0f, 0f, 1f);
            _renderer.sortingLayerName = "Default";
            // width = lebar dunia yang diinginkan; skala dihitung dari ukuran sprite asli,
            // bukan angka lokal mentah (dulu 0.55 dianggap skala → bayangan jadi mikro).
            Vector2 spriteSize = _renderer.sprite.bounds.size;
            _renderer.transform.localScale = new Vector3(width / spriteSize.x, width * 0.5f / spriteSize.y, 1f);
        }

        private void LateUpdate()
        {
            if (_renderer == null || _ownerRenderer == null) return;
            // Satu di bawah karakter, tapi tetap di atas lantai/prop belakang.
            if (_ownerRenderer.sortingOrder != _lastOwnerOrder)
            {
                _lastOwnerOrder = _ownerRenderer.sortingOrder;
                _renderer.sortingOrder = _lastOwnerOrder - 1;
            }
        }

        private static Sprite GetOrCreateSharedSprite()
        {
            if (_sharedSprite != null) return _sharedSprite;

            var texture = new Texture2D(TextureWidth, TextureHeight, TextureFormat.RGBA32, false)
            {
                name = "BlobShadow",
                filterMode = FilterMode.Bilinear, // bayangan memang lembut, bukan pixel-art
                wrapMode = TextureWrapMode.Clamp,
            };

            float centerX = TextureWidth * 0.5f - 0.5f, centerY = TextureHeight * 0.5f - 0.5f;
            var pixels = new Color[TextureWidth * TextureHeight];
            for (int y = 0; y < TextureHeight; y++)
            {
                for (int x = 0; x < TextureWidth; x++)
                {
                    float dx = (x - centerX) / (TextureWidth * 0.5f);
                    float dy = (y - centerY) / (TextureHeight * 0.5f);
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(1f - distance);
                    pixels[y * TextureWidth + x] = new Color(0f, 0f, 0f, alpha * alpha * 0.42f);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();

            _sharedSprite = Sprite.Create(texture, new Rect(0, 0, TextureWidth, TextureHeight), new Vector2(0.5f, 0.5f), 100f);
            return _sharedSprite;
        }
    }
}
