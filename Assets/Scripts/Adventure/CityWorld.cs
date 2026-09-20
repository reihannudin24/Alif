using System;
using System.Collections.Generic;
using Alif.Core;
using Alif.World;
using UnityEngine;

namespace Alif.Adventure
{
    /// <summary>
    /// Merakit Kota Cempaka saat runtime dari Resources/Kota (hasil Tools/city-art/generate_city.py):
    /// gambar latar tiap map, lantai (WalkableArea), penghalang properti, batas kamera, titik
    /// spawn, dan posisi NPC quest. Map dijajarkan di y ≈ -60 — di atas plafon Y-sort prompt
    /// (1000 - y*100 &lt; YSortOrder.PromptOrderBase) dan jauh dari area scene yang ada.
    /// </summary>
    public static class CityWorld
    {
        [Serializable] public sealed class PixelRect { public int x, y, w, h; }
        [Serializable] public sealed class NpcSpot { public string id; public int x, y; }
        /// <summary>Pintu antar area: (x, y) titik lantai di depan daun pintunya, h tinggi bebas
        /// penanda, area tujuan, (tx, ty) titik mendarat di map tujuan.</summary>
        [Serializable] public sealed class Door { public string label, area; public int x, y, h, tx, ty; }
        [Serializable] public sealed class Pin { public string area; public int x, y; public bool up; }
        /// <summary>Pasangan area bertetangga di peta HP yang bisa dilalui dengan berjalan kaki
        /// (roads di city.json): <c>west</c> ada di kiri <c>east</c>.</summary>
        [Serializable] public sealed class Road { public string west, east; }

        /// <summary>Pintu dari map kota ke interior yang tinggal di scene bab (sceneDoors di
        /// city.json): <c>map</c> map kotanya, <c>area</c> nama area scene tujuannya.</summary>
        [Serializable] public sealed class RetiredArea
        { public string area, to; public int x, y; public string[] keep; }

        [Serializable] public sealed class SceneDoorSpec
        { public string map, area, label; public int x, y, h, landX, landY; }

        [Serializable] public sealed class MapSpec
        {
            public string id, area, file;
            /// <summary>Ukuran map dalam petak. Pecahan karena interior dikecilkan lewat `scale`
            /// di generate_city.py (INTERIOR_SCALE) — gambarnya tetap resolusi penuh, PPU impornya
            /// yang dinaikkan.</summary>
            public float width, height;
            public int spawnX, spawnY;
            public PixelRect[] walk, blocks;
            public NpcSpot[] npcs;
            public Door[] doors;
        }

        [Serializable] public sealed class Spec
        {
            public int tile, mapWidth, mapHeight;
            public float ppu;
            public MapSpec[] maps;
            public Pin[] pins;
            public Road[] roads;
            public SceneDoorSpec[] sceneDoors;
            /// <summary>Area scene yang sudah tidak dipakai lagi: isinya dimatikan runtime dan
            /// penghuninya pindah ke <c>to</c> pada titik (x, y).</summary>
            public RetiredArea[] retired;
            /// <summary>Prop scene yang sudah tidak cocok dengan interior berlukisan.</summary>
            public string[] hiddenProps;
        }

        /// <summary>Tepi kiri/kanan sebuah map jalan: strip pemicu setinggi lantainya, plus titik
        /// mendarat di dalam map saat pemain datang dari tetangga di sisi itu.</summary>
        public sealed class RoadEdge { public Vector2 Foot, Landing, Size; }

        public sealed class Built
        {
            public Vector2[] Centers, Spawns;
            public readonly Dictionary<string, (string area, Vector2 position)> Npcs = new Dictionary<string, (string, Vector2)>();
            /// <summary>Pintu masuk per map: titik lantai di depan pintu, tinggi bebas penanda (unit), label.</summary>
            public readonly List<(string area, Vector2 foot, float height, string label, string destination)> Entrances =
                new List<(string, Vector2, float, string, string)>();
            /// <summary>Map yang punya latar malam (Kota/&lt;file&gt;_Malam): latar siangnya ditukar
            /// saat malam lewat <see cref="SetNight"/>.</summary>
            readonly List<(SpriteRenderer renderer, Sprite day, Sprite night)> _nightSwaps =
                new List<(SpriteRenderer, Sprite, Sprite)>();
            bool _night;

            internal void AddNightSwap(SpriteRenderer renderer, Sprite day, Sprite night) =>
                _nightSwaps.Add((renderer, day, night));

            /// <summary>Tukar latar siang/malam. Aman dipanggil tiap frame: hanya bekerja saat berubah.</summary>
            public void SetNight(bool night)
            {
                if (night == _night) return;
                _night = night;
                foreach (var (renderer, day, dark) in _nightSwaps)
                    if (renderer) renderer.sprite = night ? dark : day;
            }

            /// <summary>Pemeta piksel map → dunia per map kota, untuk memasang pintu tambahan
            /// setelah kota berdiri (mis. pintu ke interior yang tinggal di scene bab).</summary>
            public readonly Dictionary<string, Func<float, float, Vector2>> Worlds =
                new Dictionary<string, Func<float, float, Vector2>>();

            /// <summary>Tepi barat/timur tiap map kota, untuk jalur jalan kaki antar area.</summary>
            public readonly Dictionary<string, RoadEdge> WestEdges = new Dictionary<string, RoadEdge>();
            public readonly Dictionary<string, RoadEdge> EastEdges = new Dictionary<string, RoadEdge>();
        }

        static readonly Vector2 FirstOrigin = new Vector2(100f, -66f);
        static readonly Vector2 DoorTriggerSize = new Vector2(1.1f, .5f);
        const float Gap = 8f;
        // Strip pemicu di tepi map (piksel map dari tepinya) dan titik mendaratnya. Jaraknya
        // sengaja 36 px (> 1 unit): kalau lebih dekat, pemain mendarat di dalam strip tepi map
        // tujuan dan langsung terlempar balik.
        const float RoadEdgeInset = 10f, RoadLandingInset = 46f, RoadEdgeWidth = .5f;
        /// <summary>Akhiran file lukisan malam (lihat NIGHT_SUFFIX di generate_city.py).</summary>
        const string NightSuffix = "_Malam";

        static Spec _spec;

        public static Spec Load()
        {
            if (_spec != null) return _spec;
            var json = Resources.Load<TextAsset>("Kota/city");
            _spec = json ? JsonUtility.FromJson<Spec>(json.text) : null;
            return _spec;
        }

        /// <summary>Bangun semua map sesuai urutan <see cref="AdventureContent.CityAreas"/>.</summary>
        public static Built Build()
        {
            var spec = Load();
            var built = new Built { Centers = new Vector2[AdventureContent.CityAreas.Length], Spawns = new Vector2[AdventureContent.CityAreas.Length] };
            if (spec == null) { Debug.LogError("[Alif] Resources/Kota/city.json tidak ditemukan — jalankan Tools/city-art/generate_city.py."); return built; }

            var root = new GameObject("Kota Cempaka").transform;
            var places = new Dictionary<string, (Transform root, Func<float, float, Vector2> world)>();
            float x = FirstOrigin.x;
            for (int i = 0; i < AdventureContent.CityAreas.Length; i++)
            {
                var map = Array.Find(spec.maps, m => m.area == AdventureContent.CityAreas[i]);
                if (map == null) { Debug.LogError("[Alif] Map kota hilang di city.json: " + AdventureContent.CityAreas[i]); continue; }
                var size = new Vector2(map.width, map.height) * spec.tile / spec.ppu;
                var origin = new Vector2(x, FirstOrigin.y);
                x += size.x + Gap;
                Vector2 World(float px, float py) => origin + new Vector2(px, map.height * spec.tile - py) / spec.ppu;

                var mapRoot = new GameObject("Map " + map.area).transform;
                mapRoot.SetParent(root, false);
                mapRoot.position = origin + size / 2f;
                var background = mapRoot.gameObject.AddComponent<SpriteRenderer>();
                background.sprite = Resources.Load<Sprite>("Kota/" + map.file);
                background.sortingOrder = 0;
                // Latar malam opsional (mis. Kafe Senja): lukisan kedua dengan lampu menyala.
                var nightSprite = Resources.Load<Sprite>("Kota/" + map.file + NightSuffix);
                if (nightSprite) built.AddNightSwap(background, background.sprite, nightSprite);
                mapRoot.gameObject.AddComponent<CameraBounds>().Configure(mapRoot.position, size / 2f);

                foreach (var rect in map.walk) AddBox(mapRoot, "WalkableArea", World(rect.x, rect.y + rect.h), rect, spec.ppu, true);
                foreach (var rect in map.blocks) AddBox(mapRoot, "Blocker", World(rect.x, rect.y + rect.h), rect, spec.ppu, false);

                // Tepi jalan: strip setinggi lantai utama map, mendarat sejajar titik spawn-nya
                // (selalu di trotoar) supaya pemain tidak muncul di tengah jalan raya.
                var band = map.walk != null && map.walk.Length > 0 ? map.walk[0] : null;
                if (band != null)
                {
                    float middle = band.y + band.h / 2f, right = map.width * spec.tile;
                    var edgeSize = new Vector2(RoadEdgeWidth, band.h / spec.ppu);
                    built.WestEdges[map.area] = new RoadEdge
                    { Foot = World(RoadEdgeInset, middle), Landing = RoadLanding(map, band, RoadLandingInset, World), Size = edgeSize };
                    built.EastEdges[map.area] = new RoadEdge
                    { Foot = World(right - RoadEdgeInset, middle), Landing = RoadLanding(map, band, right - RoadLandingInset, World), Size = edgeSize };
                }

                built.Centers[i] = mapRoot.position;
                built.Spawns[i] = World(map.spawnX, map.spawnY);
                foreach (var npc in map.npcs) built.Npcs[npc.id] = (map.area, World(npc.x, npc.y));
                places[map.area] = (mapRoot, World);
                built.Worlds[map.area] = World;
            }

            // Pintu dipasang setelah semua map berdiri: tujuannya ada di map lain yang mungkin
            // baru dibangun belakangan.
            foreach (var map in spec.maps)
            {
                if (map.doors == null || !places.TryGetValue(map.area, out var here)) continue;
                foreach (var door in map.doors)
                {
                    Vector2 foot = here.world(door.x, door.y);
                    built.Entrances.Add((map.area, foot, door.h / spec.ppu, door.label, door.area));
                    if (!places.TryGetValue(door.area, out var there))
                    { Debug.LogError($"[Alif] Pintu {map.area} → {door.area}: area tujuan tidak ada di city.json."); continue; }
                    AddDoor(here.root, there.root, foot, there.world(door.tx, door.ty), door.area, door.label);
                }
            }
            Physics2D.SyncTransforms();
            return built;
        }

        /// <summary>Titik mendarat di tepi jalan: sejajar titik spawn map (selalu di trotoar),
        /// digeser naik-turun kalau kena properti — bangku depan masjid, pot, atau halte persis
        /// menempel tepi map di beberapa lukisan.</summary>
        static Vector2 RoadLanding(MapSpec map, PixelRect band, float px, Func<float, float, Vector2> world)
        {
            for (int step = 0; step < 16; step++)
                for (int sign = 1; sign >= -1; sign -= 2)
                {
                    float py = map.spawnY + sign * step * 6f;
                    if (py < band.y + 8 || py > band.y + band.h - 8) continue;
                    if (!Covered(map.blocks, px, py)) return world(px, py);
                    if (step == 0) break;
                }
            return world(px, map.spawnY);
        }

        /// <summary>Titik itu kena penghalang (dengan margin supaya pemain tidak mendarat menempel).</summary>
        static bool Covered(PixelRect[] rects, float x, float y)
        {
            const float Margin = 6f;
            if (rects == null) return false;
            foreach (var r in rects)
                if (x >= r.x - Margin && x <= r.x + r.w + Margin && y >= r.y - Margin && y <= r.y + r.h + Margin) return true;
            return false;
        }

        /// <summary>Trigger SceneDoor + titik mendarat di area tujuan. Titik mendaratnya sengaja
        /// dijauhkan dari trigger seberang supaya pemain tidak langsung terpental balik.</summary>
        static void AddDoor(Transform parent, Transform destinationMap, Vector2 position, Vector2 destination, string destinationArea, string label = null)
        {
            var landing = new GameObject("Door landing " + destinationArea);
            landing.transform.SetParent(destinationMap, false);
            landing.transform.position = destination;

            var go = new GameObject("Door to " + destinationArea);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var box = go.AddComponent<BoxCollider2D>();
            box.size = DoorTriggerSize;
            box.isTrigger = true;
            go.AddComponent<SceneDoor>().Configure(landing.transform,
                (label == "OUT" ? "Keluar ke " : "Masuk ke ") + destinationArea + "?", label != "OUT");
        }

        static void AddBox(Transform parent, string name, Vector2 bottomLeft, PixelRect rect, float ppu, bool walkable)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var size = new Vector2(rect.w, rect.h) / ppu;
            go.transform.position = bottomLeft + size / 2f;
            var box = go.AddComponent<BoxCollider2D>();
            box.size = size;
            box.isTrigger = walkable;
            if (walkable) go.AddComponent<WalkableArea>();
        }
    }
}
