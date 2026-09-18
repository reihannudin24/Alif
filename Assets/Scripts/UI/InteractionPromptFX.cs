using Alif.Campaign;
using TMPro;
using UnityEngine;

namespace Alif.UI
{
    /// <summary>
    /// Prompt interaksi: keycap "E" krem berbingkai oranye (PixelSkin) yang melayang di atas
    /// kepala pemain selama ada objek yang bisa diinteraksi. Dulu berupa panah di atas tiap
    /// objek — objek painted (ATM, bangku, loket) hanya zona tak terlihat, jadi panahnya jatuh
    /// menutupi gambar objek itu sendiri. Pop masuk dengan overshoot lalu mengambang pelan.
    /// </summary>
    public sealed class InteractionPromptFX : MonoBehaviour
    {
        private const float KeycapSize = .3f;

        private Vector3 _basePosition;
        private float _age;

        public static InteractionPromptFX Create(Transform owner, float localY)
        {
            var root = new GameObject("InteractPrompt");
            root.transform.SetParent(owner, false);
            root.transform.localPosition = new Vector3(0f, localY, 0f);

            var keycap = new GameObject("Keycap").AddComponent<SpriteRenderer>();
            keycap.transform.SetParent(root.transform, false);
            keycap.sprite = PixelSkin.Tab();
            keycap.transform.localScale = Vector3.one * (KeycapSize / keycap.sprite.bounds.size.y);
            keycap.sortingOrder = Alif.World.YSortOrder.PromptOrderBase + 30;

            var label = new GameObject("KeyLabel").AddComponent<TextMeshPro>();
            label.transform.SetParent(root.transform, false);
            if (PixelSkin.Font != null) label.font = PixelSkin.Font;
            label.text = "E";
            label.fontSize = 2.2f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = PixelSkin.TextDark;
            label.rectTransform.sizeDelta = Vector2.one * KeycapSize;
            label.GetComponent<MeshRenderer>().sortingOrder = Alif.World.YSortOrder.PromptOrderBase + 31;

            return root.AddComponent<InteractionPromptFX>();
        }

        private void Awake()
        {
            _basePosition = transform.localPosition;
        }

        private void OnEnable()
        {
            _age = 0f;
            transform.localScale = Vector3.one;
            transform.localPosition = _basePosition;
        }

        private void LateUpdate()
        {
            _age += Time.deltaTime;
            if (PlayerPrefs.GetInt("Alif_ReducedMotion", 0) == 1)
            {
                transform.localScale = Vector3.one;
                transform.localPosition = _basePosition;
                return;
            }
            // EaseOutBack: sedikit melebihi 1 lalu kembali — kesan "pop" yang hidup.
            const float c = 1.70158f * 1.2f;
            float t = Mathf.Clamp01(_age / .25f);
            float eased = 1f + (c + 1f) * Mathf.Pow(t - 1f, 3f) + c * Mathf.Pow(t - 1f, 2f);
            transform.localScale = Vector3.one * Mathf.Lerp(.4f, 1f, eased);
            transform.localPosition = _basePosition + Vector3.up * (Mathf.Sin(_age * 3.2f) * .04f);
        }
    }
}
