using System.Collections.Generic;
using UnityEngine;

namespace Alif.Player
{
    /// <summary>
    /// Efek partikel kepulan debu (dust puff) di kaki saat karakter berjalan atau lari.
    /// Menggunakan pooling sederhana tanpa beban Garbage Collection untuk nuansa game feel
    /// retro yang sangat reaktif dan memuaskan.
    /// </summary>
    public class FootstepDustEffect : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Vector2 _footOffset = new Vector2(0f, -0.36f);
        [SerializeField] private float _walkInterval = 0.32f;
        [SerializeField] private float _sprintInterval = 0.18f;
        [SerializeField] private Color _dustColor = new Color(0.88f, 0.85f, 0.8f, 0.7f);

        private float _dustTimer;
        private static Sprite _dustSprite;
        private readonly List<DustPuff> _activePuffs = new List<DustPuff>();
        private readonly Queue<DustPuff> _puffPool = new Queue<DustPuff>();

        private class DustPuff
        {
            public GameObject GameObject;
            public SpriteRenderer Renderer;
            public float Age;
            public float Lifetime;
            public Vector3 Velocity;
            public Vector3 BaseScale;
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // Update active puffs
            for (int i = _activePuffs.Count - 1; i >= 0; i--)
            {
                DustPuff puff = _activePuffs[i];
                puff.Age += dt;
                if (puff.Age >= puff.Lifetime)
                {
                    puff.GameObject.SetActive(false);
                    _puffPool.Enqueue(puff);
                    _activePuffs.RemoveAt(i);
                }
                else
                {
                    float progress = puff.Age / puff.Lifetime;
                    puff.GameObject.transform.position += puff.Velocity * dt;
                    // Expand and fade out
                    float scaleMultiplier = 1f + progress * 0.8f;
                    puff.GameObject.transform.localScale = puff.BaseScale * scaleMultiplier;

                    Color c = _dustColor;
                    c.a = (1f - progress) * _dustColor.a;
                    puff.Renderer.color = c;
                }
            }
        }

        public void SpawnDust(bool isSprinting, Vector2 moveDir)
        {
            float interval = isSprinting ? _sprintInterval : _walkInterval;
            _dustTimer -= Time.deltaTime;
            if (_dustTimer > 0f) return;
            _dustTimer = interval;

            DustPuff puff = GetOrCreatePuff();
            Vector2 spawnPos = (Vector2)transform.position + _footOffset;
            // Sedikit acak ke kiri/kanan kaki
            spawnPos.x += Random.Range(-0.08f, 0.08f);

            puff.GameObject.transform.position = spawnPos;
            puff.Age = 0f;
            puff.Lifetime = isSprinting ? 0.28f : 0.22f;
            // Partikel berhembus sedikit ke arah berlawanan gerakan
            puff.Velocity = -moveDir * (isSprinting ? 0.45f : 0.2f) + new Vector2(Random.Range(-0.1f, 0.1f), Random.Range(0.05f, 0.2f));
            puff.BaseScale = Vector3.one * (isSprinting ? 0.22f : 0.15f);
            puff.GameObject.transform.localScale = puff.BaseScale;
            puff.Renderer.color = _dustColor;
            puff.GameObject.SetActive(true);

            _activePuffs.Add(puff);
        }

        private DustPuff GetOrCreatePuff()
        {
            if (_puffPool.Count > 0)
            {
                return _puffPool.Dequeue();
            }

            GameObject go = new GameObject("DustPuff");
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetOrCreateDustSprite();
            sr.sortingLayerName = "Characters";
            sr.sortingOrder = 5;

            return new DustPuff
            {
                GameObject = go,
                Renderer = sr
            };
        }

        private static Sprite GetOrCreateDustSprite()
        {
            if (_dustSprite != null) return _dustSprite;
            int size = 16;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float radius = (size - 1) * 0.5f;
            Vector2 center = new Vector2(radius, radius);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    if (d <= radius)
                    {
                        float alpha = Mathf.SmoothStep(1f, 0f, d / radius);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            _dustSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 50f);
            return _dustSprite;
        }

        private void OnDestroy()
        {
            foreach (var puff in _activePuffs)
            {
                if (puff.GameObject != null) Destroy(puff.GameObject);
            }
            while (_puffPool.Count > 0)
            {
                var puff = _puffPool.Dequeue();
                if (puff.GameObject != null) Destroy(puff.GameObject);
            }
        }
    }
}
