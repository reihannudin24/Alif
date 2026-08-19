using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Alif.Characters;

namespace Alif.EditorTools
{
    /// <summary>
    /// Ubah folder sprite karakter (hasil export AI generator 8-arah) di
    /// Assets/Sprites/Characters/&lt;nama&gt;/ menjadi AnimationClip + AnimatorController
    /// siap pakai, lalu isi CharacterData ScriptableObject untuk tiap NPC.
    ///
    /// Jalankan menu "Alif > 1) Import Character Art" SEBELUM "Alif > 2) Build Demo Scene".
    /// Aman dijalankan berulang kali (rebuild bersih tiap kali, tidak menumpuk state lama).
    ///
    /// Asumsi struktur folder per karakter:
    ///   Idle/rotations/&lt;arah&gt;.png                          -> pose diam per arah (8 arah)
    ///   Idle/animations/Walking/&lt;arah&gt;/frame_000..007.png   -> walk cycle (khusus "alif" / player)
    ///   Idle/animations/Idle/south/frame_000..007.png            -> idle loop (khusus NPC)
    /// </summary>
    public static class AlifCharacterAnimationBuilder
    {
        private const string CharactersRoot = "Assets/Sprites/Characters";
        private const string AnimationsRoot = "Assets/Animations";
        private const string CharacterDataFolder = "Assets/ScriptableObjects/Characters";

        private const string PlayerCharacterFolder = "alif";

        private static readonly string[] Directions =
        {
            "south", "south-east", "east", "north-east", "north", "north-west", "west", "south-west"
        };

        private static readonly Dictionary<string, Vector2> DirectionVectors = new Dictionary<string, Vector2>
        {
            { "south", new Vector2(0f, -1f) },
            { "south-east", new Vector2(0.7071f, -0.7071f) },
            { "east", new Vector2(1f, 0f) },
            { "north-east", new Vector2(0.7071f, 0.7071f) },
            { "north", new Vector2(0f, 1f) },
            { "north-west", new Vector2(-0.7071f, 0.7071f) },
            { "west", new Vector2(-1f, 0f) },
            { "south-west", new Vector2(-0.7071f, -0.7071f) },
        };

        // NPC yang mau di-generate. Tambah/kurangi sesuai folder yang ada di Sprites/Characters.
        internal static readonly (string folder, string displayName, string description)[] NpcCharacters =
        {
            ("dimas", "Dimas", "Rekan kerja yang gampang gugup, tapi baik hati."),
            ("naya", "Naya", "Teman satu tim yang ramah dan selalu bawa laporan."),
            ("raka", "Raka", "Rekan kerja percaya diri dengan senyum menggoda."),
            ("bu_siti", "Bu Siti", "Ibu-ibu ramah di sekitar kantor, suka menyapa duluan."),
            ("pak_ustad", "Pak Ustad", "Sesepuh yang baik hati dan bijaksana."),
        };

        [MenuItem("Alif/1) Import Character Art")]
        public static void ImportCharacterArt()
        {
            ReimportAllCharacterTextures();

            BuildPlayerAnimator();

            foreach (var (folder, displayName, description) in NpcCharacters)
            {
                BuildNpcAnimatorAndData(folder, displayName, description);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Alif] Import character art selesai. Lanjut ke menu 'Alif > 2) Build Demo Scene'.");
        }

        // ---------------------------------------------------------------
        private static void ReimportAllCharacterTextures()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { CharactersRoot });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }

        // ---------------------------------------------------------------
        // PLAYER (alif): Idle Blend Tree (8 pose diam) + Walk Blend Tree (8 walk cycle)
        // ---------------------------------------------------------------
        private static void BuildPlayerAnimator()
        {
            string charFolder = $"{CharactersRoot}/{PlayerCharacterFolder}";
            if (!AssetDatabase.IsValidFolder(charFolder))
            {
                Debug.LogWarning($"[Alif] Folder karakter player '{charFolder}' tidak ditemukan, dilewati.");
                return;
            }

            string outFolder = $"{AnimationsRoot}/Player";
            EnsureFolder(outFolder);

            var idleClips = new Dictionary<string, AnimationClip>();
            var walkClips = new Dictionary<string, AnimationClip>();

            foreach (string dir in Directions)
            {
                string rotationPath = $"{charFolder}/Idle/rotations/{dir}.png";
                Sprite idleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(rotationPath);
                if (idleSprite != null)
                {
                    idleClips[dir] = CreateSingleFrameClip($"{outFolder}/Alif_Idle_{dir}.anim", idleSprite);
                }

                List<Sprite> walkFrames = LoadFrameSequence($"{charFolder}/Idle/animations/Walking/{dir}");
                if (walkFrames.Count > 0)
                {
                    walkClips[dir] = CreateSequenceClip($"{outFolder}/Alif_Walk_{dir}.anim", walkFrames, 8f);
                }
            }

            string controllerPath = $"{outFolder}/Alif_Player.controller";
            AnimatorController controller = GetOrCreateController(controllerPath);

            EnsureParameter(controller, "MoveX", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "MoveY", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "IsMoving", AnimatorControllerParameterType.Bool);

            AnimatorStateMachine rootSM = controller.layers[0].stateMachine;
            ClearStateMachine(rootSM);

            BlendTree idleTree = CreateDirectionalBlendTree(controller, "IdleBlend", idleClips);
            BlendTree walkTree = CreateDirectionalBlendTree(controller, "WalkBlend", walkClips);

            AnimatorState idleState = rootSM.AddState("Idle", new Vector3(280, 0, 0));
            idleState.motion = idleTree;

            AnimatorState walkState = rootSM.AddState("Walk", new Vector3(280, 120, 0));
            walkState.motion = walkTree;

            rootSM.defaultState = idleState;

            // Duration 0 (potongan langsung, tanpa crossfade) sengaja dipilih: ini animasi
            // sprite-swap (bukan motion halus), jadi crossfade antar 2 Blend Tree yang beda
            // justru bikin Unity gagal nge-blend properti Sprite dengan benar — hasilnya
            // karakter sempat rendering "kosong" (invisible) sekilas tiap kali mulai/berhenti
            // jalan. Potongan instan menghindari window blending itu sama sekali.
            AnimatorStateTransition toWalk = idleState.AddTransition(walkState);
            toWalk.hasExitTime = false;
            toWalk.duration = 0f;
            toWalk.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");

            AnimatorStateTransition toIdle = walkState.AddTransition(idleState);
            toIdle.hasExitTime = false;
            toIdle.duration = 0f;
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");

            EditorUtility.SetDirty(controller);

            // Kalau GameObject Player sudah ada di scene aktif (dari Build Demo Scene sebelumnya),
            // langsung pasang controller & sprite south-nya supaya kelihatan di Scene view juga.
            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                Animator animator = player.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.runtimeAnimatorController = controller;
                }

                SpriteRenderer sr = player.GetComponent<SpriteRenderer>();
                Sprite southIdle = AssetDatabase.LoadAssetAtPath<Sprite>($"{charFolder}/Idle/rotations/south.png");
                if (sr != null && southIdle != null)
                {
                    sr.sprite = southIdle;
                }
            }
        }

        // ---------------------------------------------------------------
        // NPC: 1 AnimationClip (idle loop, selalu south) + AnimatorController sederhana + CharacterData
        // ---------------------------------------------------------------
        private static void BuildNpcAnimatorAndData(string folder, string displayName, string description)
        {
            string charFolder = $"{CharactersRoot}/{folder}";
            if (!AssetDatabase.IsValidFolder(charFolder))
            {
                Debug.LogWarning($"[Alif] Folder karakter '{charFolder}' tidak ditemukan, dilewati.");
                return;
            }

            string outFolder = $"{AnimationsRoot}/{folder}";
            EnsureFolder(outFolder);

            List<Sprite> idleFrames = LoadFrameSequence($"{charFolder}/Idle/animations/Idle/south");
            AnimationClip idleClip = idleFrames.Count > 0
                ? CreateSequenceClip($"{outFolder}/{folder}_Idle.anim", idleFrames, 6f)
                : null;

            string controllerPath = $"{outFolder}/{folder}.controller";
            AnimatorController controller = GetOrCreateController(controllerPath);

            AnimatorStateMachine rootSM = controller.layers[0].stateMachine;
            ClearStateMachine(rootSM);

            if (idleClip != null)
            {
                AnimatorState idleState = rootSM.AddState("Idle", new Vector3(280, 0, 0));
                idleState.motion = idleClip;
                rootSM.defaultState = idleState;
            }

            EditorUtility.SetDirty(controller);

            Sprite portrait = AssetDatabase.LoadAssetAtPath<Sprite>($"{charFolder}/Idle/rotations/south.png");

            string dataPath = $"{CharacterDataFolder}/CharacterData_{folder}.asset";
            CharacterData data = AssetDatabase.LoadAssetAtPath<CharacterData>(dataPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<CharacterData>();
                EnsureFolder(CharacterDataFolder);
                AssetDatabase.CreateAsset(data, dataPath);
            }

            data.CharacterName = displayName;
            data.Description = description;
            data.Portrait = portrait;
            data.AnimatorController = controller;
            EditorUtility.SetDirty(data);
        }

        // ---------------------------------------------------------------
        // HELPERS: animator controller
        // ---------------------------------------------------------------
        private static AnimatorController GetOrCreateController(string path)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            }
            else
            {
                // Bersihkan BlendTree lama supaya sub-asset tidak menumpuk tiap kali di-rebuild.
                foreach (Object subAsset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (subAsset is BlendTree)
                    {
                        Object.DestroyImmediate(subAsset, true);
                    }
                }
            }

            return controller;
        }

        private static BlendTree CreateDirectionalBlendTree(AnimatorController controller, string name, Dictionary<string, AnimationClip> clipsByDirection)
        {
            var tree = new BlendTree
            {
                name = name,
                blendType = BlendTreeType.FreeformDirectional2D,
                blendParameter = "MoveX",
                blendParameterY = "MoveY"
            };
            AssetDatabase.AddObjectToAsset(tree, controller);

            foreach (KeyValuePair<string, AnimationClip> kvp in clipsByDirection)
            {
                if (kvp.Value != null && DirectionVectors.TryGetValue(kvp.Key, out Vector2 dir))
                {
                    tree.AddChild(kvp.Value, dir);
                }
            }

            return tree;
        }

        private static void ClearStateMachine(AnimatorStateMachine sm)
        {
            foreach (ChildAnimatorState child in sm.states)
            {
                sm.RemoveState(child.state);
            }
        }

        private static void EnsureParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
        {
            foreach (AnimatorControllerParameter p in controller.parameters)
            {
                if (p.name == name && p.type == type)
                {
                    return;
                }
            }

            controller.AddParameter(name, type);
        }

        // ---------------------------------------------------------------
        // HELPERS: animation clips
        // ---------------------------------------------------------------
        private static AnimationClip CreateSingleFrameClip(string path, Sprite sprite)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, path);
            }

            clip.frameRate = 1f;
            var binding = new EditorCurveBinding { path = "", type = typeof(SpriteRenderer), propertyName = "m_Sprite" };
            var keyframes = new[] { new ObjectReferenceKeyframe { time = 0f, value = sprite } };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static AnimationClip CreateSequenceClip(string path, List<Sprite> frames, float frameRate)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, path);
            }

            clip.frameRate = frameRate;
            var binding = new EditorCurveBinding { path = "", type = typeof(SpriteRenderer), propertyName = "m_Sprite" };

            var keyframes = new ObjectReferenceKeyframe[frames.Count];
            for (int i = 0; i < frames.Count; i++)
            {
                keyframes[i] = new ObjectReferenceKeyframe { time = i / frameRate, value = frames[i] };
            }
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static List<Sprite> LoadFrameSequence(string folderPath)
        {
            var result = new List<Sprite>();
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                return result;
            }

            string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { folderPath });
            List<Sprite> sprites = guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Distinct()
                .OrderBy(p => p)
                .Select(AssetDatabase.LoadAssetAtPath<Sprite>)
                .Where(s => s != null)
                .ToList();

            result.AddRange(sprites);
            return result;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
            string folderName = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
