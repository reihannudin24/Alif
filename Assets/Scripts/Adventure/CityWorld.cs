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
        [Serializable] public sealed class Pin { public string area; public int x, y; public bool up; }

        [Serializable] public sealed class MapSpec
        {
            public string id, area, file;
            public int width, height, spawnX, spawnY;
            public PixelRect[] walk, blocks;
            public NpcSpot[] npcs;
        }

        [Serializable] public sealed class Spec
        {
            public int tile, mapWidth, mapHeight;
            public float ppu;
            public MapSpec[] maps;
            public Pin[] pins;
        }

        public sealed class Built
        {
            public Vector2[] Centers, Spawns;
            public readonly Dictionary<string, (string area, Vector2 position)> Npcs = new Dictionary<string, (string, Vector2)>();
        }

        static readonly Vector2 FirstOrigin = new Vector2(100f, -66f);
        const float Gap = 8f;

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
                mapRoot.gameObject.AddComponent<CameraBounds>().Configure(mapRoot.position, size / 2f);

                foreach (var rect in map.walk) AddBox(mapRoot, "WalkableArea", World(rect.x, rect.y + rect.h), rect, spec.ppu, true);
                foreach (var rect in map.blocks) AddBox(mapRoot, "Blocker", World(rect.x, rect.y + rect.h), rect, spec.ppu, false);

                built.Centers[i] = mapRoot.position;
                built.Spawns[i] = World(map.spawnX, map.spawnY);
                foreach (var npc in map.npcs) built.Npcs[npc.id] = (map.area, World(npc.x, npc.y));
            }
            Physics2D.SyncTransforms();
            return built;
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
