using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Alif.EditorTools
{
    public static class AiTileRpgBuilder
    {
        [Serializable]
        public sealed class Point
        {
            public int x;
            public int y;
        }

        [Serializable]
        public sealed class PaletteEntry
        {
            public string symbol;
            public string sprite;
            public string layer;
            public bool solid;
        }

        [Serializable]
        public sealed class WorldSpec
        {
            public string name;
            public int width;
            public int height;
            public float cellSize = 1f;
            public PaletteEntry[] palette;
            public string[] rows;
            public Point spawn;
            public Point exit;
        }

        [MenuItem("Alif/AI/Generate Tile RPG Preview")]
        public static void Generate()
        {
            SelfCheck();
            string specPath = Argument("-aiTileRpgSpec") ?? "Assets/AI/TileRpg/Specs/sample-world.json";
            string fullPath = SafeProjectPath(specPath);
            WorldSpec spec = JsonUtility.FromJson<WorldSpec>(File.ReadAllText(fullPath));
            Dictionary<char, PaletteEntry> palette = Validate(spec, true);

            string rootFolder = "Assets/Generated/AiTileRpg";
            EnsureFolder(rootFolder);
            string safeName = new string(spec.name.Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-').ToArray());
            GameObject root = new GameObject(safeName);
            Grid grid = root.AddComponent<Grid>();
            grid.cellSize = new Vector3(spec.cellSize, spec.cellSize, 0f);

            Tilemap ground = CreateLayer(root.transform, "Ground", 0, false);
            Tilemap detail = CreateLayer(root.transform, "Detail", 10, false);
            Tilemap collision = CreateLayer(root.transform, "Collision", 20, true);
            var maps = new Dictionary<string, Tilemap> { ["ground"] = ground, ["detail"] = detail, ["collision"] = collision };
            var tiles = new Dictionary<char, Tile>();

            foreach (KeyValuePair<char, PaletteEntry> item in palette)
            {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(item.Value.sprite);
                string tilePath = $"{rootFolder}/{safeName}_{(int)item.Key}.asset";
                Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
                if (tile == null)
                {
                    tile = ScriptableObject.CreateInstance<Tile>();
                    AssetDatabase.CreateAsset(tile, tilePath);
                }
                tile.sprite = sprite;
                float scale = spec.cellSize / Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
                tile.transform = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
                tile.colliderType = item.Value.solid ? Tile.ColliderType.Sprite : Tile.ColliderType.None;
                EditorUtility.SetDirty(tile);
                tiles[item.Key] = tile;
            }

            for (int row = 0; row < spec.height; row++)
            {
                for (int x = 0; x < spec.width; x++)
                {
                    char symbol = spec.rows[row][x];
                    PaletteEntry entry = palette[symbol];
                    maps[entry.layer].SetTile(new Vector3Int(x, spec.height - row - 1, 0), tiles[symbol]);
                }
            }

            CreateMarker(root.transform, "Player Spawn", spec.spawn, spec.height, spec.cellSize);
            CreateMarker(root.transform, "Exit", spec.exit, spec.height, spec.cellSize);
            string prefabPath = $"{rootFolder}/{safeName}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root);

            SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                PrefabUtility.InstantiatePrefab(prefab, scene);
                var cameraObject = new GameObject("Preview Camera");
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.backgroundColor = new Color(0.08f, 0.1f, 0.14f);
                camera.transform.position = new Vector3((spec.width - 1) * spec.cellSize / 2f, (spec.height - 1) * spec.cellSize / 2f, -10f);
                camera.orthographicSize = Mathf.Max(spec.height * .6f, spec.width * .32f) * spec.cellSize;
                EditorSceneManager.SaveScene(scene, $"{rootFolder}/{safeName}Preview.unity");
                Capture(camera, $"{rootFolder}/{safeName}Preview.png");
            }
            finally
            {
                if (setup.Any(item => item.isLoaded)) EditorSceneManager.RestoreSceneManagerSetup(setup);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Alif] AI tile RPG generated: {prefabPath} ({spec.width}x{spec.height}).");
        }

        public static Dictionary<char, PaletteEntry> Validate(WorldSpec spec, bool requireAssets)
        {
            if (spec == null || string.IsNullOrWhiteSpace(spec.name)) throw new InvalidDataException("World name is required.");
            if (spec.width < 3 || spec.height < 3 || spec.width > 256 || spec.height > 256) throw new InvalidDataException("World size must be 3..256 cells.");
            if (spec.cellSize <= 0f || spec.cellSize > 16f) throw new InvalidDataException("cellSize must be > 0 and <= 16.");
            if (spec.palette == null || spec.palette.Length == 0 || spec.palette.Length > 64) throw new InvalidDataException("Palette must contain 1..64 entries.");
            if (spec.rows == null || spec.rows.Length != spec.height || spec.rows.Any(row => row == null || row.Length != spec.width)) throw new InvalidDataException("Rows must match width and height.");

            var palette = new Dictionary<char, PaletteEntry>();
            foreach (PaletteEntry entry in spec.palette)
            {
                if (entry == null || string.IsNullOrEmpty(entry.symbol) || entry.symbol.Length != 1) throw new InvalidDataException("Every palette symbol must be one character.");
                if (!new[] { "ground", "detail", "collision" }.Contains(entry.layer)) throw new InvalidDataException($"Unsupported layer: {entry.layer}");
                if (entry.solid && entry.layer != "collision") throw new InvalidDataException($"Solid symbol {entry.symbol} must use the collision layer.");
                if (!palette.TryAdd(entry.symbol[0], entry)) throw new InvalidDataException($"Duplicate palette symbol: {entry.symbol}");
                if (requireAssets && (string.IsNullOrEmpty(entry.sprite) || !entry.sprite.StartsWith("Assets/", StringComparison.Ordinal) || AssetDatabase.LoadAssetAtPath<Sprite>(entry.sprite) == null))
                    throw new InvalidDataException($"Missing sprite asset: {entry.sprite}");
            }

            foreach (char symbol in spec.rows.SelectMany(row => row))
                if (!palette.ContainsKey(symbol)) throw new InvalidDataException($"Unknown map symbol: {symbol}");
            ValidatePoint(spec.spawn, "spawn", spec, palette);
            ValidatePoint(spec.exit, "exit", spec, palette);
            if (!Reachable(spec, palette)) throw new InvalidDataException("No walkable path from spawn to exit.");
            return palette;
        }

        private static bool Reachable(WorldSpec spec, Dictionary<char, PaletteEntry> palette)
        {
            int start = spec.spawn.y * spec.width + spec.spawn.x;
            int goal = spec.exit.y * spec.width + spec.exit.x;
            var queue = new Queue<int>();
            var visited = new HashSet<int> { start };
            queue.Enqueue(start);
            int[] steps = { -1, 0, 1, 0, -1 };
            while (queue.Count > 0)
            {
                int cell = queue.Dequeue();
                if (cell == goal) return true;
                int x = cell % spec.width;
                int y = cell / spec.width;
                for (int i = 0; i < 4; i++)
                {
                    int nx = x + steps[i];
                    int ny = y + steps[i + 1];
                    if (nx < 0 || ny < 0 || nx >= spec.width || ny >= spec.height) continue;
                    int next = ny * spec.width + nx;
                    if (!palette[spec.rows[ny][nx]].solid && visited.Add(next)) queue.Enqueue(next);
                }
            }
            return false;
        }

        private static void ValidatePoint(Point point, string label, WorldSpec spec, Dictionary<char, PaletteEntry> palette)
        {
            if (point == null || point.x < 0 || point.y < 0 || point.x >= spec.width || point.y >= spec.height) throw new InvalidDataException($"{label} is outside the map.");
            if (palette[spec.rows[point.y][point.x]].solid) throw new InvalidDataException($"{label} is on a solid tile.");
        }

        private static Tilemap CreateLayer(Transform parent, string name, int order, bool collision)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            Tilemap map = gameObject.AddComponent<Tilemap>();
            TilemapRenderer renderer = gameObject.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = order;
            if (collision) gameObject.AddComponent<TilemapCollider2D>();
            return map;
        }

        private static void CreateMarker(Transform parent, string name, Point point, int height, float cellSize)
        {
            var marker = new GameObject(name);
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition = new Vector3((point.x + .5f) * cellSize, (height - point.y - .5f) * cellSize, 0f);
        }

        private static void Capture(Camera camera, string assetPath)
        {
            var target = new RenderTexture(768, 512, 24);
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                image.Apply();
                File.WriteAllBytes(SafeGeneratedPath(assetPath), image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(image);
                UnityEngine.Object.DestroyImmediate(target);
            }
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        }

        private static string SafeGeneratedPath(string assetPath)
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string fullPath = Path.GetFullPath(Path.Combine(root, assetPath));
            string generated = Path.GetFullPath(Path.Combine(root, "Assets/Generated/AiTileRpg")) + Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(generated, StringComparison.Ordinal)) throw new InvalidDataException("Generated output escaped its folder.");
            return fullPath;
        }

        private static string SafeProjectPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath) || !assetPath.StartsWith("Assets/", StringComparison.Ordinal) || Path.GetExtension(assetPath) != ".json")
                throw new InvalidDataException("Spec must be a JSON file under Assets/.");
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string fullPath = Path.GetFullPath(Path.Combine(root, assetPath));
            if (!fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal) || !File.Exists(fullPath)) throw new FileNotFoundException("Tile RPG spec not found.", assetPath);
            return fullPath;
        }

        private static string Argument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }

        private static void EnsureFolder(string path)
        {
            string current = "Assets";
            foreach (string part in path.Split('/').Skip(1))
            {
                string next = current + "/" + part;
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, part);
                current = next;
            }
        }

        private static void SelfCheck()
        {
            var floor = new PaletteEntry { symbol = ".", layer = "ground" };
            var wall = new PaletteEntry { symbol = "#", layer = "collision", solid = true };
            var valid = new WorldSpec { name = "check", width = 3, height = 3, palette = new[] { floor, wall }, rows = new[] { "...", ".#.", "..." }, spawn = new Point { x = 0, y = 0 }, exit = new Point { x = 2, y = 2 } };
            Validate(valid, false);
            valid.rows = new[] { ".#.", ".#.", ".#." };
            try
            {
                Validate(valid, false);
                throw new Exception("AI tile RPG validator self-check failed.");
            }
            catch (InvalidDataException)
            {
                // Expected: the wall column disconnects spawn and exit.
            }
        }
    }
}
