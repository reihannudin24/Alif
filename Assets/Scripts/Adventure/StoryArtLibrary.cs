using System;
using UnityEngine;

namespace Alif.Adventure
{
    /// <summary>
    /// Pustaka gambar adegan cerita (StoryScene.Art). Merujuk gambar latar & panel cutscene yang
    /// sudah ada tanpa menggandakan file; ganti/tambah entri saat ilustrasi final tersedia.
    /// Dimuat dari Resources/Story/StoryArt. Kunci "kota:&lt;file&gt;" dibaca dari Resources/Kota.
    /// </summary>
    [CreateAssetMenu(menuName = "Alif/Story Art Library")]
    public sealed class StoryArtLibrary : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string Key;
            public Sprite Art;
        }

        public Entry[] Entries = Array.Empty<Entry>();

        static StoryArtLibrary _instance;

        public static Sprite Find(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (key.StartsWith("kota:")) return Resources.Load<Sprite>("Kota/" + key.Substring(5));
            if (_instance == null) _instance = Resources.Load<StoryArtLibrary>("Story/StoryArt");
            if (_instance == null) return null;
            foreach (var entry in _instance.Entries)
                if (entry.Key == key) return entry.Art;
            return null;
        }
    }
}
