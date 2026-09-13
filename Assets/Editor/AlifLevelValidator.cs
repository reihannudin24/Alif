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

            // 1. Walkable Areas sanity (whitelist bersifat opsional per arsitektur — map tanpa
            // whitelist memakai physics biasa, jadi absennya bukan error, cukup peringatan).
            if (walkableAreas.Length == 0)
            {
                Debug.LogWarning("[AlifLevelValidator] No WalkableArea components found in scene; floor whitelist checks are skipped (plain physics mode).");
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

            // 5. Prop grounding & overlap — prop berdiri (feet blocker tipis di akar sprite)
            // harus menapak lantai whitelist (kalau regionnya memakai whitelist), tidak
            // menumpuk feet prop lain, dan tidak menanam diri ke blocker dinding/void.
            var feetProps = new List<(GameObject owner, Collider2D feet)>();
            foreach (var renderer in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include))
            {
                var go = renderer.gameObject;
                if (go.GetComponentInParent<PlayerController>(true) != null) continue; // player bergerak, posisinya bukan authored
                Collider2D feet = null;
                foreach (var collider in go.GetComponents<Collider2D>())
                {
                    if (collider.isTrigger) continue;
                    var box = collider as BoxCollider2D;
                    var capsule = collider as CapsuleCollider2D;
                    if ((box != null && box.size.y <= 0.25f) || (capsule != null && capsule.size.y <= 0.3f)) { feet = collider; break; }
                }
                if (feet != null) feetProps.Add((go, feet));
            }

            var whitelistColliders = walkableAreas.Select(a => a.GetComponent<Collider2D>()).Where(c => c != null).ToArray();
            foreach (var (owner, feet) in feetProps)
            {
                var bounds = feet.bounds;
                var feetPoint = new Vector2(bounds.center.x, bounds.min.y + 0.05f);
                bool nearWhitelist = whitelistColliders.Any(c => c.bounds.SqrDistance(feetPoint) <= 16f);
                bool insideWhitelist = whitelistColliders.Any(c => c.OverlapPoint(feetPoint));
                if (nearWhitelist && !insideWhitelist)
                {
                    errors.Add($"Prop '{owner.name}' feet at {feetPoint} hover outside the walkable whitelist.");
                }

                foreach (var blocker in UnityEngine.Object.FindObjectsByType<Collider2D>(FindObjectsInactive.Include))
                {
                    if (blocker == feet || blocker.isTrigger || blocker.transform.IsChildOf(owner.transform)) continue;
                    if (!(blocker.name.StartsWith("VoidBlocker_") || blocker.name.StartsWith("BoundaryWall_") || blocker.name.StartsWith("PropBlocker_"))) continue;
                    if (bounds.Intersects(blocker.bounds))
                    {
                        errors.Add($"Prop '{owner.name}' is embedded in solid blocker '{blocker.name}'.");
                    }
                }
            }
            for (int i = 0; i < feetProps.Count; i++)
            {
                for (int j = i + 1; j < feetProps.Count; j++)
                {
                    if (feetProps[i].feet.bounds.Intersects(feetProps[j].feet.bounds))
                    {
                        errors.Add($"Props '{feetProps[i].owner.name}' and '{feetProps[j].owner.name}' overlap each other's feet colliders.");
                    }
                }
            }

            // 6. Band sorting prompt eksklusif — hanya prompt/marker (panah, keycap, label,
            // marker objektif) yang boleh berada di atas PromptOrderBase di layer Default.
            string[] promptNames = { "InteractionArrow", "Marker", "Objective label", "KeycapE" };
            foreach (var renderer in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include))
            {
                if (renderer.sortingLayerName != "Default") continue;
                bool isPrompt = promptNames.Contains(renderer.name);
                if (!isPrompt && renderer.sortingOrder >= YSortOrder.PromptOrderBase)
                {
                    errors.Add($"SpriteRenderer '{renderer.name}' (order {renderer.sortingOrder}) intrudes into the prompt sorting band.");
                }
            }
            foreach (var mesh in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include))
            {
                if (mesh.sortingLayerName != "Default") continue;
                bool isPrompt = promptNames.Contains(mesh.name);
                if (isPrompt && mesh.sortingOrder < YSortOrder.PromptOrderBase)
                {
                    errors.Add($"World prompt '{mesh.name}' (order {mesh.sortingOrder}) sorts below the prompt band and hides behind Y-sorted sprites.");
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
            var scenesToTest = Enumerable.Range(1, 5)
                .Select(c => $"Assets/Scenes/Adventure/AdventureChapter{c}.unity")
                .Concat(new[] { "Assets/Scenes/SampleScene.unity" })
                .ToArray();

            foreach (var scenePath in scenesToTest)
            {
                if (!System.IO.File.Exists(scenePath)) continue;
                EditorSceneManager.OpenScene(scenePath);
                ValidateActiveScene();
            }
        }
    }
}
