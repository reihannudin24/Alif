using System.Collections.Generic;
using System.Linq;

namespace Alif.Adventure
{
    /// <summary>Empat waktu dalam sehari: pagi 06–12, siang 12–18, malam 18–24, larut malam 00–06.</summary>
    public enum DayPart { Pagi, Siang, Malam, LarutMalam }

    /// <summary>
    /// Jadwal hadir tokoh kota: diacak per hari cerita dan per waktu, tapi DETERMINISTIK — dihitung
    /// dari (id tokoh, hari, waktu), jadi hasilnya sama setelah save/reload dan tidak perlu disimpan.
    /// Jaminan: (1) tiap tokoh pasti hadir di pagi atau siang setiap hari, supaya selalu bisa
    /// ditemui di jam wajar; (2) tiap map selalu punya minimal satu tokoh di waktu apa pun
    /// (<see cref="PresentIn"/>). Tokoh yang sedang punya langkah quest untuk pemain tidak pernah
    /// dihilangkan — itu diputuskan pemanggil (AdventureGame.RefreshQuestMarkers).
    /// </summary>
    public static class NpcSchedule
    {
        /// <summary>Peluang hadir (%) per waktu: ramai di siang hari, sepi di larut malam.</summary>
        static readonly int[] Chance = { 75, 80, 55, 20 };

        public static DayPart PartOf(int hour) =>
            hour < 6 ? DayPart.LarutMalam : hour < 12 ? DayPart.Pagi : hour < 18 ? DayPart.Siang : DayPart.Malam;

        public static string Label(DayPart part) =>
            part == DayPart.Pagi ? "Pagi" : part == DayPart.Siang ? "Siang" : part == DayPart.Malam ? "Malam" : "Larut malam";

        /// <summary>FNV-1a: string.GetHashCode tidak stabil antar-sesi, jadi tidak boleh dipakai di sini.</summary>
        static uint Hash(string id, int day, int salt)
        {
            uint h = 2166136261;
            foreach (char c in id) { h ^= c; h *= 16777619; }
            h ^= (uint)day; h *= 16777619;
            h ^= (uint)salt; h *= 16777619;
            h ^= h >> 15; h *= 2246822519; h ^= h >> 13;
            return h;
        }

        static bool Rolls(string npc, int day, DayPart part) => Hash(npc, day, (int)part) % 100 < Chance[(int)part];

        public static bool Present(string npc, int day, DayPart part)
        {
            if (Rolls(npc, day, part)) return true;
            if (part != DayPart.Pagi && part != DayPart.Siang) return false;
            // Undian pagi dan siang sama-sama gagal: salah satunya dipaksa hadir.
            if (Rolls(npc, day, DayPart.Pagi) || Rolls(npc, day, DayPart.Siang)) return false;
            return (Hash(npc, day, 99) % 2 == 0 ? DayPart.Pagi : DayPart.Siang) == part;
        }

        /// <summary>Tokoh yang hadir di satu map. Kalau undian mengosongkan map, satu tokoh dipilih
        /// (bergilir menurut hari & waktu) untuk tetap berjaga.</summary>
        public static HashSet<string> PresentIn(IReadOnlyCollection<string> residents, int day, DayPart part)
        {
            var present = new HashSet<string>(residents.Where(r => Present(r, day, part)));
            if (present.Count == 0 && residents.Count > 0)
                present.Add(residents.OrderBy(r => Hash(r, day, 50 + (int)part)).ThenBy(r => r, System.StringComparer.Ordinal).First());
            return present;
        }
    }
}
