# Alif audit

Baseline: 2026-09-06. Implementation status is tracked at the end; baseline findings are not claims of completed fixes.

Unity 6000.5.3f1, URP, Input System, uGUI/TMP. Existing staged, unstaged and untracked work is preserved in `.codex/stabilization/20260906-065800` (binary diffs, source/serialized baseline, SHA-256 inventory). No package, asset or script is classified unused merely because a text search found no caller.

## Architecture and identity

Indonesian five-chapter educational adventure: financial decisions, evidence, budgets, dialogue and community support. Original scenes and managers coexist with Adventure content/state and isolated battle models. Adventure currently orchestrates runtime UI, dialogue, journal, movement and saves. Old battle controllers redirect to Adventure; restoring combat is explicitly approved for this pass.

Strong points: reusable IInteractable, serialized movement tuning, Rigidbody2D motion, content separated from state, immutable battle definitions, pure combat clocks, backup saves, existing NUnit model tests, original art and audio.

## Baseline findings

|Priority|Evidence / problem|Smallest useful response|
|---|---|---|
|P0|AdventureGame calls SceneDoor.Destination/Bind/area properties and a five-argument SceneFadeController.TransitionTo absent from current implementations.|Restore required shared transition APIs; compile before further changes.|
|P1|Build includes Adventure scenes only but runtime loads MainMenu and Chapter1Ending.|One validated scene route list including original entry and cutscenes.|
|P1|Adventure builder saves empty scenes without player/world dependencies and ordinary BuildWeb invokes it.|Build existing scenes; explicitly validate dependencies, remove destructive regeneration from normal builds.|
|P1|Chapter 0 AdventureGame enters gameplay initialization despite having no chapter content.|Use original menu and compatible AdventureMenu entry routing.|
|P1|Battle controllers are redirects; combat models and assets remain.|Restore required finales through one campaign objective gate.|
|P1|Single movement-lock boolean shared by dialogue, doors, fades and modals.|Owner-based lock API preserving serialized entry methods.|
|P1|Save version 2 has no encounter objective progress.|Non-destructive version 3 migration with preserved earned completion.|
|P2|Adventure HUD chooses nearest point independently of player collider-edge selection.|Share interaction target selection.|
|P2|Camera shake is applied after bounds clamp; sprint zoom updates after clamp.|Apply effects then clamp using current viewport; honor reduced motion.|
|P2|Adventure Update allocates LINQ lists/orderings and repeatedly computes door paths.|Profile first; remove repeated work only with measured evidence.|
|P2|Setup README describes Send Messages although controller directly subscribes to input actions.|Synchronize agent and architecture documentation.|
|P3|Broad mixed asset PPU and legacy scaffolding increase maintenance cost.|Preserve intentional families; scoped importer validator, no mass moves.|

## Scene inventory
- `Assets/Scenes/Adventure/AdventureChapter1.unity`: 3 GameObjects, 2 MonoBehaviours.
- `Assets/Scenes/Adventure/AdventureChapter2.unity`: 3 GameObjects, 2 MonoBehaviours.
- `Assets/Scenes/Adventure/AdventureChapter3.unity`: 3 GameObjects, 2 MonoBehaviours.
- `Assets/Scenes/Adventure/AdventureChapter4.unity`: 3 GameObjects, 2 MonoBehaviours.
- `Assets/Scenes/Adventure/AdventureChapter5.unity`: 3 GameObjects, 2 MonoBehaviours.
- `Assets/Scenes/Adventure/AdventureMenu.unity`: 3 GameObjects, 2 MonoBehaviours.
- `Assets/Scenes/Chapter1Cutscene.unity`: 18 GameObjects, 20 MonoBehaviours.
- `Assets/Scenes/Chapter1Ending.unity`: 18 GameObjects, 20 MonoBehaviours.
- `Assets/Scenes/Chapter2Cutscene.unity`: 18 GameObjects, 20 MonoBehaviours.
- `Assets/Scenes/Chapter2Gameplay.unity`: 202 GameObjects, 210 MonoBehaviours.
- `Assets/Scenes/Chapter3Gameplay.unity`: 182 GameObjects, 160 MonoBehaviours.
- `Assets/Scenes/Chapter4Gameplay.unity`: 182 GameObjects, 160 MonoBehaviours.
- `Assets/Scenes/Chapter5Gameplay.unity`: 182 GameObjects, 160 MonoBehaviours.
- `Assets/Scenes/ChapterSelect.unity`: 37 GameObjects, 50 MonoBehaviours.
- `Assets/Scenes/MainMenu.unity`: 25 GameObjects, 32 MonoBehaviours.
- `Assets/Scenes/SampleScene.unity`: 269 GameObjects, 210 MonoBehaviours.
- `Assets/Scenes.unity`: 1 GameObjects, 1 MonoBehaviours.
- `Assets/Settings/Scenes/URP2DSceneTemplate.unity`: 2 GameObjects, 2 MonoBehaviours.

## Prefabs, animation, rendering, input and physics

*.prefab: `Assets/Prefabs/UI/ChoiceButton.prefab`

*.controller: `Assets/Animations/Player/Alif_Player.controller`, `Assets/Animations/bu_siti/bu_siti.controller`, `Assets/Animations/dimas/dimas.controller`, `Assets/Animations/naya/naya.controller`, `Assets/Animations/pak_ustad/pak_ustad.controller`, `Assets/Animations/raka/raka.controller`, `Assets/ScriptableObjects/Campaign/AlifAction.controller`

*.inputactions: `Assets/Settings/InputSystem_Actions.inputactions`

*.physicsMaterial2D: `Assets/Settings/Frictionless2D.physicsMaterial2D`

*.shader: `Assets/TextMesh Pro/Shaders/TMP_Bitmap-Custom-Atlas.shader`, `Assets/TextMesh Pro/Shaders/TMP_Bitmap-Mobile.shader`, `Assets/TextMesh Pro/Shaders/TMP_Bitmap.shader`, `Assets/TextMesh Pro/Shaders/TMP_SDF Overlay.shader`, `Assets/TextMesh Pro/Shaders/TMP_SDF SSD SpaceWarp.shader`, `Assets/TextMesh Pro/Shaders/TMP_SDF SSD.shader`, `Assets/TextMesh Pro/Shaders/TMP_SDF SpaceWarp.shader`, `Assets/TextMesh Pro/Shaders/TMP_SDF-Mobile Masking.shader`, `Assets/TextMesh Pro/Shaders/TMP_SDF-Mobile Overlay.shader`, `Assets/TextMesh Pro/Shaders/TMP_SDF-Mobile SSD SpaceWarp.shader`, `Assets/TextMesh Pro/Shaders/TMP_SDF-Mobile SSD.shader`, `Assets/TextMesh Pro/Shaders/TMP_SDF-Mobile SpaceWarp.shader`, `Assets/TextMesh Pro/Shaders/TMP_SDF-Mobile-2-Pass.shader`, `Assets/TextMesh Pro/Shaders/TMP_SDF-Mobile.shader`, `Assets/TextMesh Pro/Shaders/TMP_SDF-Surface-Mobile.shader`, `Assets/TextMesh Pro/Shaders/TMP_SDF-Surface.shader`, `Assets/TextMesh Pro/Shaders/TMP_SDF.shader`, `Assets/TextMesh Pro/Shaders/TMP_Sprite.shader`

## Runtime/editor/test inventory

### Scripts
- `Assets/Scripts/Adventure/AdventureGame.cs`: 392 lines; AdventureGame
- `Assets/Scripts/Adventure/AdventurePoint.cs`: 21 lines; AdventurePoint
- `Assets/Scripts/Adventure/Model/AdventureContent.cs`: 164 lines; AdventureContent
- `Assets/Scripts/Adventure/Model/AdventureState.cs`: 181 lines; PuzzleStep, AdventureTask, AdventureChapter, TaskProgress, AdventureState, AdventureSave
- `Assets/Scripts/Adventure/Model/OriginalCampaign.cs`: 60 lines; ActivityBoard, OriginalCampaign
- `Assets/Scripts/Adventure/PixelArt.cs`: 142 lines; PixelArt, Draw
- `Assets/Scripts/Battle/BattleEncounterController.cs`: 10 lines; BattleEncounterController
- `Assets/Scripts/Battle/Model/ActionCombatSession.cs`: 85 lines; ActionCombatSession
- `Assets/Scripts/Battle/Model/ActionEncounterDefinition.cs`: 34 lines; ActionEncounterDefinition
- `Assets/Scripts/Battle/Model/BattleEncounterDefinition.cs`: 60 lines; BattleResponse, BattleStage, BattleEncounterDefinition
- `Assets/Scripts/Battle/Model/BattleSession.cs`: 73 lines; BattlePhase, BattleSession
- `Assets/Scripts/Campaign/ActionEncounterController.cs`: 11 lines; ActionEncounterController
- `Assets/Scripts/Campaign/CampaignUI.cs`: 94 lines; CampaignUI
- `Assets/Scripts/Campaign/ChapterAdventure.cs`: 11 lines; ChapterAdventure
- `Assets/Scripts/Campaign/FloatingCombatText.cs`: 82 lines; FloatingCombatText
- `Assets/Scripts/Campaign/InvestigationPoint.cs`: 12 lines; InvestigationPoint
- `Assets/Scripts/Characters/CharacterData.cs`: 51 lines; CharacterData
- `Assets/Scripts/Characters/IInteractable.cs`: 13 lines; IInteractable
- `Assets/Scripts/Characters/NPCController.cs`: 271 lines; NPCController
- `Assets/Scripts/Core/AudioManager.cs`: 141 lines; AudioManager
- `Assets/Scripts/Core/CameraBounds.cs`: 74 lines; CameraBounds
- `Assets/Scripts/Core/CameraFollow.cs`: 194 lines; CameraFollow
- `Assets/Scripts/Core/ChapterProgress.cs`: 88 lines; ini, ChapterProgress
- `Assets/Scripts/Core/GameManager.cs`: 55 lines; GameManager, GameState
- `Assets/Scripts/Core/SceneLoader.cs`: 62 lines; SceneLoader
- `Assets/Scripts/Dialogue/DialogueData.cs`: 82 lines; DialogueLine, DialogueChoice, DialogueData
- `Assets/Scripts/Dialogue/DialogueManager.cs`: 229 lines; ini, DialogueManager
- `Assets/Scripts/Player/FootstepDustEffect.cs`: 150 lines; FootstepDustEffect, DustPuff
- `Assets/Scripts/Player/PlayerAnimation.cs`: 119 lines; PlayerAnimation, arah
- `Assets/Scripts/Player/PlayerController.cs`: 529 lines; FacingDirection, PlayerController
- `Assets/Scripts/Systems/CurrencySystem.cs`: 102 lines; CurrencySystem
- `Assets/Scripts/Systems/EnergySystem.cs`: 98 lines; EnergySystem
- `Assets/Scripts/Systems/InventorySystem.cs`: 135 lines; InventorySlot, InventorySystem
- `Assets/Scripts/Systems/ScoreSystem.cs`: 127 lines; ScoreSystem
- `Assets/Scripts/Systems/TimeSystem.cs`: 148 lines; TimeSystem
- `Assets/Scripts/UI/AmbientImageMotion.cs`: 41 lines; AmbientImageMotion
- `Assets/Scripts/UI/AtmUI.cs`: 155 lines; AtmUI
- `Assets/Scripts/UI/ChapterCardUI.cs`: 31 lines; ChapterCardUI
- `Assets/Scripts/UI/ChapterSelectController.cs`: 73 lines; ChapterSelectController
- `Assets/Scripts/UI/CutscenePlayerController.cs`: 268 lines; CutscenePlayerController
- `Assets/Scripts/UI/DialogueUI.cs`: 627 lines; DialogueUI
- `Assets/Scripts/UI/EmoteBubble.cs`: 195 lines; EmoteType, EmoteBubble
- `Assets/Scripts/UI/FloatingPrompt.cs`: 83 lines; FloatingPrompt
- `Assets/Scripts/UI/HUDController.cs`: 144 lines; HUDController
- `Assets/Scripts/UI/InteractionPromptFX.cs`: 51 lines; InteractionPromptFX
- `Assets/Scripts/UI/InventorySlotUI.cs`: 82 lines; InventorySlotUI
- `Assets/Scripts/UI/InventoryUI.cs`: 81 lines; InventoryUI
- `Assets/Scripts/UI/MainMenuController.cs`: 64 lines; MainMenuController
- `Assets/Scripts/UI/PauseMenuController.cs`: 141 lines; PauseMenuController
- `Assets/Scripts/UI/QuitConfirmationUI.cs`: 83 lines; QuitConfirmationUI
- `Assets/Scripts/UI/SceneFadeController.cs`: 143 lines; SceneFadeController
- `Assets/Scripts/UI/ScoreBalanceFeedbackUI.cs`: 111 lines; ScoreBalanceFeedbackUI
- `Assets/Scripts/UI/TravelConfirmationUI.cs`: 114 lines; TravelConfirmationUI
- `Assets/Scripts/UI/UIManager.cs`: 58 lines; UIManager
- `Assets/Scripts/UI/VirtualJoystick.cs`: 53 lines; VirtualJoystick
- `Assets/Scripts/World/AtmController.cs`: 31 lines; AtmController
- `Assets/Scripts/World/Chapter2StoryController.cs`: 10 lines; Chapter2StoryController
- `Assets/Scripts/World/Chapter2StoryPoint.cs`: 19 lines; Chapter2StoryPoint
- `Assets/Scripts/World/InteractableObject.cs`: 37 lines; InteractableObject
- `Assets/Scripts/World/ItemPickup.cs`: 57 lines; ItemPickup
- `Assets/Scripts/World/LuckyCoinPickup.cs`: 76 lines; LuckyCoinPickup
- `Assets/Scripts/World/OpeningMonologueTrigger.cs`: 28 lines; OpeningMonologueTrigger
- `Assets/Scripts/World/RestaurantStoryPoint.cs`: 50 lines; RestaurantStoryPoint, PointType
- `Assets/Scripts/World/RestaurantStoryTrigger.cs`: 258 lines; RestaurantStoryTrigger, StoryState
- `Assets/Scripts/World/SceneDoor.cs`: 94 lines; SceneDoor
- `Assets/Scripts/World/StationCat.cs`: 74 lines; StationCat
- `Assets/Scripts/World/WalkableArea.cs`: 155 lines; WalkableArea
- `Assets/Scripts/World/YSortOrder.cs`: 60 lines; YSortOrder

### Editor
- `Assets/Editor/AlifAdventureBuilder.cs`: 64 lines; AlifAdventureBuilder
- `Assets/Editor/AlifAudioSetup.cs`: 348 lines; AlifAudioSetup
- `Assets/Editor/AlifBackgroundTexturePostprocessor.cs`: 60 lines; AlifBackgroundTexturePostprocessor
- `Assets/Editor/AlifBattleBuilder.cs`: 9 lines; AlifBattleBuilder
- `Assets/Editor/AlifBattlePlayCheck.cs`: 7 lines; AlifBattlePlayCheck
- `Assets/Editor/AlifCampaignBuilder.cs`: 10 lines; AlifCampaignBuilder
- `Assets/Editor/AlifChapter2Builder.cs`: 10 lines; AlifChapter2Builder
- `Assets/Editor/AlifChapterSelectBuilder.cs`: 291 lines; AlifChapterSelectBuilder
- `Assets/Editor/AlifCharacterAnimationBuilder.cs`: 428 lines; AlifCharacterAnimationBuilder
- `Assets/Editor/AlifCharacterTexturePostprocessor.cs`: 43 lines; AlifCharacterTexturePostprocessor
- `Assets/Editor/AlifCutsceneBuilder.cs`: 463 lines; AlifCutsceneBuilder
- `Assets/Editor/AlifDemoSceneBuilder.cs`: 2981 lines; AlifDemoSceneBuilder, masing
- `Assets/Editor/AlifFontSetup.cs`: 81 lines; AlifFontSetup
- `Assets/Editor/AlifMainMenuBuilder.cs`: 428 lines; AlifMainMenuBuilder
- `Assets/Editor/AlifPlayModeBootScene.cs`: 26 lines; AlifPlayModeBootScene
- `Assets/Editor/AlifStandaloneBuilder.cs`: 69 lines; AlifStandaloneBuilder

### Tests
- `Assets/Tests/Adventure/AdventureTests.cs`: 62 lines; AdventureTests
- `Assets/Tests/Editor/ActionCombatTests.cs`: 70 lines; ActionCombatTests
- `Assets/Tests/Editor/BattleSessionTests.cs`: 106 lines; BattleSessionTests

## Texture inventory

All TextureImporter metadata scanned; family scale is intentional until verified visually. Character PPU 256; backgrounds 200; battle and campaign exceptions retained.

|Family|Count|PPU distribution|Filter values|Compression values|Mipmaps|
|---|---:|---|---|---|---|
|Assets/Sprites/Backgrounds|10|{'200': 10}|{'1': 10}|{'1': 10}|{'0': 10}|
|Assets/Sprites/Battle|1|{'24': 1}|{'0': 1}|{'0': 1}|{'0': 1}|
|Assets/Sprites/Campaign|2|{'380': 1, '180': 1}|{'0': 2}|{'0': 2}|{'0': 2}|
|Assets/Sprites/Characters|153|{'256': 153}|{'0': 153}|{'0': 153}|{'0': 153}|
|Assets/Sprites/Cutscenes|5|{'200': 5}|{'1': 5}|{'1': 5}|{'0': 5}|
|Assets/Sprites/Portraits|5|{'200': 5}|{'1': 5}|{'1': 5}|{'0': 5}|
|Assets/Sprites/UI|10|{'100': 9, '200': 1}|{'1': 10}|{'0': 8, '1': 2}|{'0': 10}|

## Dependency report

Retain Unity modules and existing tooling for compatibility. No Yarn Spinner, UniTask, DOTween, DI, ECS, networking or Addressables additions. Existing AI, inference, collaboration, IDE, multiplayer-center, Timeline, Visual Scripting and optional 2D authoring packages require reference/editor-use evidence before removal; retained this pass.

- `com.unity.pipeline`: official Unity CLI bridge used for local Editor automation.
- `com.unity.2d.animation`: `15.1.0`
- `com.unity.2d.aseprite`: `5.0.3`
- `com.unity.2d.psdimporter`: `14.0.3`
- `com.unity.2d.sprite`: `1.0.0`
- `com.unity.2d.spriteshape`: `15.0.3`
- `com.unity.2d.tilemap`: `1.0.0`
- `com.unity.2d.tilemap.extras`: `8.0.3`
- `com.unity.2d.tooling`: `3.0.1`
- `com.unity.ai.assistant`: `2.19.0-pre.2`
- `com.unity.ai.inference`: `2.6.1`
- `com.unity.collab-proxy`: `2.13.5`
- `com.unity.ide.rider`: `3.0.38`
- `com.unity.ide.visualstudio`: `2.0.26`
- `com.unity.inputsystem`: `1.20.0`
- `com.unity.multiplayer.center`: `1.0.1`
- `com.unity.render-pipelines.universal`: `17.6.0`
- `com.unity.test-framework`: `1.7.0`
- `com.unity.timeline`: `1.8.12`
- `com.unity.ugui`: `2.5.0`
- `com.unity.visualscripting`: `1.9.12`
- `com.unity.modules.accessibility`: `1.0.0`
- `com.unity.modules.adaptiveperformance`: `1.0.0`
- `com.unity.modules.ai`: `1.0.0`
- `com.unity.modules.androidjni`: `1.0.0`
- `com.unity.modules.animation`: `1.0.0`
- `com.unity.modules.assetbundle`: `1.0.0`
- `com.unity.modules.audio`: `1.0.0`
- `com.unity.modules.cloth`: `1.0.0`
- `com.unity.modules.director`: `1.0.0`
- `com.unity.modules.imageconversion`: `1.0.0`
- `com.unity.modules.imgui`: `1.0.0`
- `com.unity.modules.jsonserialize`: `1.0.0`
- `com.unity.modules.particlesystem`: `1.0.0`
- `com.unity.modules.physics`: `1.0.0`
- `com.unity.modules.physics2d`: `1.0.0`
- `com.unity.modules.physicscore2d`: `1.0.0`
- `com.unity.modules.screencapture`: `1.0.0`
- `com.unity.modules.terrain`: `1.0.0`
- `com.unity.modules.terrainphysics`: `1.0.0`
- `com.unity.modules.tilemap`: `1.0.0`
- `com.unity.modules.ui`: `1.0.0`
- `com.unity.modules.uielements`: `1.0.0`
- `com.unity.modules.umbra`: `1.0.0`
- `com.unity.modules.unityanalytics`: `1.0.0`
- `com.unity.modules.unitywebrequest`: `1.0.0`
- `com.unity.modules.unitywebrequestassetbundle`: `1.0.0`
- `com.unity.modules.unitywebrequestaudio`: `1.0.0`
- `com.unity.modules.unitywebrequesttexture`: `1.0.0`
- `com.unity.modules.unitywebrequestwww`: `1.0.0`
- `com.unity.modules.vectorgraphics`: `1.0.0`
- `com.unity.modules.vehicles`: `1.0.0`
- `com.unity.modules.video`: `1.0.0`
- `com.unity.modules.wind`: `1.0.0`
- `com.unity.modules.xr`: `1.0.0`

## Validation baseline

Editor and MCP startup initiated; results pending. Existing EditMode coverage: Adventure, BattleSession and ActionCombat. No baseline PlayMode assembly. Static observations above must be distinguished from later runtime evidence.

## Implementation ledger

Pending: compile repair → tooling verification → safe build routes → campaign/save integration → movement/interaction/camera → tests/WebGL → profiling/documentation.
