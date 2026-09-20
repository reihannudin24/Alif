using UnityEngine;

namespace Alif.Adventure
{
    /// <summary>
    /// Warna cahaya dunia menurut jam (0–24): terang netral di siang hari, keemasan saat senja,
    /// biru redup di malam dan larut malam, hangat saat fajar. Dipakai AdventureGame untuk mewarnai
    /// Global Light 2D; teks & UI tidak terpengaruh. Warnanya sengaja tidak pernah terlalu gelap —
    /// pemain harus tetap bisa membaca map dan menemukan tokoh di larut malam.
    /// </summary>
    public static class DayLight
    {
        // (jam, r, g, b) — diinterpolasi lurus di antaranya; entri pertama dan terakhir harus sama.
        static readonly float[,] Keys =
        {
            {  0f, .34f, .38f, .62f },   // larut malam
            {  4.5f, .34f, .38f, .62f },
            {  6f, .96f, .78f, .66f },   // fajar hangat
            {  8f, 1f, 1f, 1f },         // pagi–siang
            { 16f, 1f, 1f, 1f },
            { 17.5f, 1f, .84f, .64f },   // sore keemasan
            { 19f, .58f, .52f, .76f },   // senja ungu
            { 21f, .38f, .42f, .66f },   // malam
            { 24f, .34f, .38f, .62f },
        };

        /// <summary>Warna lampu ruangan yang menyala saat langit menggelap.</summary>
        static readonly Color Lamp = new Color(1f, .93f, .80f);

        public static Color Outdoor(float hour)
        {
            hour = Mathf.Repeat(hour, 24f);
            for (int i = 1; i < Keys.GetLength(0); i++)
            {
                if (hour > Keys[i, 0]) continue;
                float t = Mathf.InverseLerp(Keys[i - 1, 0], Keys[i, 0], hour);
                return new Color(Mathf.Lerp(Keys[i - 1, 1], Keys[i, 1], t), Mathf.Lerp(Keys[i - 1, 2], Keys[i, 2], t), Mathf.Lerp(Keys[i - 1, 3], Keys[i, 3], t));
            }
            return new Color(Keys[0, 1], Keys[0, 2], Keys[0, 3]);
        }

        /// <summary>Makin gelap di luar, makin dominan lampu ruangan; di siang bolong ruangan seterang luar.</summary>
        public static Color Indoor(float hour)
        {
            var sky = Outdoor(hour);
            float dark = Mathf.InverseLerp(1f, .45f, (sky.r + sky.g + sky.b) / 3f);
            return Color.Lerp(sky, Lamp, .78f * dark);
        }
    }
}
