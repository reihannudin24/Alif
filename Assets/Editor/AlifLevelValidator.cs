using System;
using System.Collections.Generic;
using System.Linq;
using Alif.Adventure;
using Alif.Player;
using Alif.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Alif.EditorTools
{
    /// <summary>
    /// Automated linter for Alif adventure scenes.
    /// Validates player reachability, walkable whitelist coverage, and prop collider clearances.
    /// Can be executed from the Unity menu (Alif/Adventure/Validate Level Integrity)
    /// or from the command line via `./Tools/alif lint-level`.
    /// </summary>
    public static class AlifLevelValidator
    {
        [MenuItem("Alif/Adventure/Validate Level Integrity")]
        public static void ValidateActiveScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            Debug.Log($"[AlifLevelValidator] Validating scene: {scene.path}");

            var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            if (player == null)
            {
                Debug.LogWarning("[AlifLevelValidator] No PlayerController found in active scene. Skipping reachability check.");
                return;
            }

            var game = UnityEngine.Object.FindAnyObjectByType<AdventureGame>();
            var walkableAreas = UnityEngine.Object.FindObjectsByType<WalkableArea>(FindObjectsInactive.Include);
            var points = UnityEngine.Object.FindObjectsByType<AdventurePoint>(FindObjectsInactive.Include);
            var doors = UnityEngine.Object.FindObjectsByType<SceneDoor>(FindObjectsInactive.Include);

            var errors = new List<string>();

            // 1. Walkable Areas sanity
            if (walkableAreas.Length == 0)
            {
                errors.Add("No WalkableArea components found in scene.");
            }
            foreach (var area in walkableAreas)
            {
                var box = area.GetComponent<BoxCollider2D>();
                if (box != null && (box.size.x <= 0.05f || box.size.y <= 0.05f))
                {
                    errors.Add($"WalkableArea '{area.name}' has invalid dimensions: size={box.size}");
                }
            }

            // 2. Spawn reachability
            if (game != null && game.Spawns != null)
            {
                for (int i = 0; i < game.Spawns.Length; i++)
                {
                    Vector2 spawn = game.Spawns[i];
                    if (!player.CanStandAt(spawn))
                    {
                        errors.Add($"Spawn point {i} at {spawn} is blocked by a solid collider or outside floor boundaries.");
                    }
                }
            }

            // 3. AdventurePoint (Interactables) reachability
            foreach (var point in points)
            {
                var col = point.GetComponent<Collider2D>();
                if (col == null)
                {
                    errors.Add($"AdventurePoint '{point.name}' (Target='{point.Target}') lacks a Collider2D trigger.");
                    continue;
                }

                Vector2 triggerPos = col.transform.position;
                bool reachable = false;
                for (int ring = 0; ring < 8 && !reachable; ring++)
                {
                    for (int i = 0; i < 16 && !reachable; i++)
                    {
                        float angle = i * Mathf.PI / 8f;
                        Vector2 candidate = triggerPos + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (0.45f + ring * 0.15f);
                        if (player.CanStandAt(candidate))
                        {
                            reachable = true;
                        }
                    }
                }

                if (!reachable)
                {
                    errors.Add($"AdventurePoint '{point.name}' (Target='{point.Target}', Area={point.Area}) is physically unreachable from any adjacent angle.");
                }
            }

            // 4. SceneDoor validation
            foreach (var door in doors)
            {
                if (door.Destination == null)
                {
                    errors.Add($"SceneDoor '{door.name}' is missing a Destination transform.");
                }
                else if (!player.CanStandAt(door.Destination.position))
                {
                    errors.Add($"SceneDoor '{door.name}' destination at {door.Destination.position} lands on an invalid or blocked position.");
                }
            }

            if (errors.Count > 0)
            {
                string msg = $"[AlifLevelValidator] FAILED with {errors.Count} error(s):\n - " + string.Join("\n - ", errors);
                Debug.LogError(msg);
                throw new InvalidOperationException(msg);
            }

            Debug.Log($"[AlifLevelValidator] SUCCESS: Scene '{scene.name}' passed all level integrity checks ({points.Length} points, {doors.Length} doors, {walkableAreas.Length} walkable areas).");
        }

        public static void ValidateAll() => ValidateAllScenes();

        public static void ValidateAllScenes()
        {
            string[] scenesToTest = new[]
            {
                "Assets/Scenes/Adventure/AdventureChapter1.unity",
                "Assets/Scenes/SampleScene.unity"
            };

            foreach (var scenePath in scenesToTest)
            {
                if (!System.IO.File.Exists(scenePath)) continue;
                EditorSceneManager.OpenScene(scenePath);
                ValidateActiveScene();
            }
        }
    }
}
