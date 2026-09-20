using Alif.Campaign;
using Alif.World;
using UnityEngine;

namespace Alif.UI
{
    /// <summary>
    /// Balon chat "…" krem yang muncul di atas kepala NPC selama pemain berdiri cukup dekat
    /// untuk mengajaknya bicara — penanda "orang ini bisa diajak ngobrol", pelengkap keycap
    /// "E" di atas kepala pemain (<see cref="InteractionPromptFX"/>). Ditaruh agak ke kanan
    /// supaya tidak menimpa nama NPC dan penanda quest "!" yang menempel lurus di atas kepala.
    /// Animasi pop-in &amp; mengambang dipinjam dari <see cref="FloatingPrompt"/>, jadi toggle
    /// ReducedMotion ikut dihormati.
    /// </summary>
    public static class SpeechBubbleFX
    {
        /// <summary>Tinggi balon dalam unit dunia (tokoh ≈ .85 unit).</summary>
        public const float Height = .34f;

        /// <summary>Geser dari titik pivot NPC: ke kanan, lalu naik dari ubun-ubunnya.</summary>
        public const float OffsetX = .34f, OffsetAboveHead = .3f;

        public static GameObject Create(Transform owner)
        {
            var go = new GameObject("SpeechBubble");
            go.transform.SetParent(owner, false);

            var bubble = go.AddComponent<SpriteRenderer>();
            bubble.sprite = PixelSkin.SpeechBubble();
            bubble.sortingOrder = YSortOrder.PromptOrderBase + 28;

            go.transform.localScale = Vector3.one * (Height / bubble.sprite.bounds.size.y);
            go.transform.localPosition = new Vector3(OffsetX, HeadTop(owner) + OffsetAboveHead, 0f);
            go.AddComponent<FloatingPrompt>();   // meng-cache posisi & skala di Awake, jadi dipasang terakhir
            return go;
        }

        /// <summary>Ubun-ubun NPC dari sprite badannya; 1 unit kalau sprite-nya belum ada.</summary>
        private static float HeadTop(Transform owner)
        {
            var body = owner.GetComponent<SpriteRenderer>();
            return body != null && body.sprite != null ? body.sprite.bounds.max.y : 1f;
        }
    }
}
