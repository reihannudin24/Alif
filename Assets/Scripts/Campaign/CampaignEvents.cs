using System;
using UnityEngine;

namespace Alif.Campaign
{
    /// <summary>
    /// Papan pengumuman sederhana untuk event campaign (mis. DialogueData.OnCompleteEventId).
    /// Sistem lain (quest, jurnal, achievement) tinggal subscribe Completed alih-alih
    /// masing-masing meng-couple ke DialogueManager.
    /// </summary>
    public static class CampaignEvents
    {
        /// <summary>Terpanggil sekali per event id yang diumumkan (string kosong/null diabaikan).</summary>
        public static event Action<string> Completed;

        /// <summary>Umumkan bahwa sebuah event campaign selesai. Aman dipanggil kapan pun.</summary>
        public static void NotifyCompleted(string eventId)
        {
            if (string.IsNullOrEmpty(eventId)) return;
            var handlers = Completed?.GetInvocationList();
            if (handlers == null) return;
            // Panggil satu per satu: subscriber yang error tidak boleh memutus
            // subscriber lain maupun alur game yang memanggil event.
            foreach (var handler in handlers)
            {
                try { ((Action<string>)handler)(eventId); }
                catch (Exception e) { Debug.LogWarning($"[Alif] Campaign event subscriber error untuk '{eventId}': {e.Message}"); }
            }
        }

        /// <summary>Bersihkan semua subscriber — untuk testing agar state tidak bocor antar test.</summary>
        public static void ResetForTests() => Completed = null;
    }
}
