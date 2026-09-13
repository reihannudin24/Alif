using System;
using Alif.Adventure;
using Alif.Core;
using Alif.Player;
using Alif.Systems;
using Alif.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Alif.Dev
{
    /// <summary>
    /// In-game developer console and playtest cheat overlay.
    /// Toggle with F1 or Tilde (~) during PlayMode or Development Builds.
    /// Provides instant area teleportation, speed multipliers, and objective skipping
    /// to speed up manual testing without walking through every step.
    /// </summary>
    public sealed class AlifDevConsole : MonoBehaviour
    {
        private static AlifDevConsole _instance;
        private bool _visible = false;
        private float _customSpeedMultiplier = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInit()
        {
#if UNITY_EDITOR || DEBUG
            if (_instance != null) return;
            var go = new GameObject("Alif_DevConsole");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<AlifDevConsole>();
#endif
        }

        private void Update()
        {
            var k = Keyboard.current;
            if (k != null && (k.f1Key.wasPressedThisFrame || k.backquoteKey.wasPressedThisFrame))
            {
                _visible = !_visible;
            }

            // Apply speed multiplier to player controller if active
            if (_customSpeedMultiplier > 1.01f)
            {
                Time.timeScale = _customSpeedMultiplier;
            }
            else if (Time.timeScale != 1f && !_visible)
            {
                Time.timeScale = 1f;
            }
        }

        private void OnGUI()
        {
            if (!_visible) return;

            GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width / 1280f, Screen.height / 720f, 1f));

            GUILayout.BeginArea(new Rect(20, 20, 380, 520), "Alif Developer Playtest Console (F1)", GUI.skin.window);

            GUILayout.Label("<b>--- TELEPORT / AREA WARP ---</b>");
            var game = FindAnyObjectByType<AdventureGame>();
            var player = FindAnyObjectByType<PlayerController>();

            if (game != null && game.Spawns != null && player != null)
            {
                string[] areaNames = game.Content?.Areas ?? new[] { "Area 0", "Area 1", "Area 2", "Area 3" };
                for (int i = 0; i < game.Spawns.Length && i < areaNames.Length; i++)
                {
                    if (GUILayout.Button($"Warp to: {areaNames[i]}"))
                    {
                        Vector2 target = game.Spawns[i];
                        player.transform.position = target;
                        var rb = player.GetComponent<Rigidbody2D>();
                        if (rb != null) rb.position = target;
                        game.State.Area = i;
                        game.State.HasPosition = true;
                        game.State.X = target.x;
                        game.State.Y = target.y;
                    }
                }
            }

            GUILayout.Space(10);
            GUILayout.Label("<b>--- PLAYTEST GAME SPEED ---</b>");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("1x (Normal)")) _customSpeedMultiplier = 1f;
            if (GUILayout.Button("2.5x (Fast)")) _customSpeedMultiplier = 2.5f;
            if (GUILayout.Button("5x (Ultra)")) _customSpeedMultiplier = 5f;
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label("<b>--- OBJECTIVES & PROGRESSION ---</b>");
            if (game != null)
            {
                GUILayout.Label($"Current Task: {game.CurrentTaskId} (Area {game.State.Area})");
                if (GUILayout.Button("Skip / Finish Current Task"))
                {
                    game.State.Finish(game.Content);
                    game.Save(false);
                }

                if (GUILayout.Button("Skip Tutorial"))
                {
                    game.SkipTutorial();
                }
            }

            GUILayout.Space(10);
            GUILayout.Label("<b>--- CURRENCY CHEAT ---</b>");
            if (GUILayout.Button("Add Rp 100.000 Cash"))
            {
                CurrencySystem.Instance?.AddMoney(100000);
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Close Console (F1)"))
            {
                _visible = false;
            }

            GUILayout.EndArea();
        }
    }
}
