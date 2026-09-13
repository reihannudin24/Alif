using System.Collections;
using System.Collections.Generic;
using Alif.Campaign;
using UnityEngine;

namespace Alif.UI
{
    /// <summary>
    /// Fade pendek bersama untuk popup CanvasGroup (Quit/Travel/ATM) yang dulunya muncul-hilang
    /// instan (alpha 0↔1). Instan saat ReducedMotion aktif. blocksRaycasts/interactable tetap
    /// dibalik SEKETIKA (bukan dianimasikan) supaya popup yang menutup tidak bisa diklik
    /// setelah perintah Hide, dan popup yang membuka langsung responsif.
    /// </summary>
    public static class PopupFade
    {
        private const float Duration = 0.15f;
        private static readonly Dictionary<CanvasGroup, (MonoBehaviour host, Coroutine routine)> Running =
            new Dictionary<CanvasGroup, (MonoBehaviour, Coroutine)>();

        public static void To(MonoBehaviour host, CanvasGroup group, bool visible, float duration = Duration)
        {
            if (group == null) return;

            if (Running.TryGetValue(group, out var prior) && prior.routine != null && prior.host != null)
            {
                prior.host.StopCoroutine(prior.routine);
                Running.Remove(group);
            }

            group.interactable = visible;
            group.blocksRaycasts = visible;

            if (duration <= 0f || CampaignUI.ReducedMotion)
            {
                group.alpha = visible ? 1f : 0f;
                return;
            }

            Running[group] = (host, host.StartCoroutine(FadeRoutine(group, visible, duration)));
        }

        private static IEnumerator FadeRoutine(CanvasGroup group, bool visible, float duration)
        {
            float from = group.alpha;
            float to = visible ? 1f : 0f;
            float t = 0f;
            while (t < duration && group != null)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration)));
                yield return null;
            }

            if (group != null) group.alpha = to;
            Running.Remove(group);
        }
    }
}
