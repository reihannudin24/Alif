using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Alif.Adventure;
using Alif.Battle;
using Alif.Campaign;
using Alif.Characters;
using Alif.Core;
using Alif.Dialogue;
using Alif.Player;
using Alif.UI;
using Alif.World;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Alif.EditorTools
{
    public static class AlifAdventureBuilder
    {
        const string VariantC = "Assets/Sprites/Generated/VariantC/";
        public static readonly string[] ScenePaths = new[] { "MainMenu", "ChapterSelect", "Chapter1Cutscene", "Chapter1Ending", "Chapter2Cutscene" }
            .Select(n=>"Assets/Scenes/"+n+".unity")
            .Concat(Enumerable.Range(1,5).Select(c=>"Assets/Scenes/Adventure/"+AdventureGame.SceneFor(c)+".unity"))
            .Concat(new[]{"Assets/Scenes/Adventure/AdventureMenu.unity","Assets/Scenes/SampleScene.unity"})
            .Concat(Enumerable.Range(2,4).Select(c=>"Assets/Scenes/Chapter"+c+"Gameplay.unity")).ToArray();

        [MenuItem("Alif/Adventure/Validate Campaign and Configure Build")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode before configuring the build.");
            ValidateContent();ValidateScenes();
            EditorBuildSettings.scenes=ScenePaths.Select(p=>new EditorBuildSettingsScene(p,true)).ToArray();
            AssetDatabase.SaveAssets();Debug.Log("[Alif] Campaign validated; no scenes regenerated.");
        }
        [MenuItem("Alif/Adventure/Apply Approved Variant C")]
        public static void ApplyVariantC()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode first.");
            if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            AssetDatabase.Refresh();ConfigureVariantCSprites();
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                for(int chapter=2;chapter<=5;chapter++)
                {
                    var scene=EditorSceneManager.OpenScene($"Assets/Scenes/Adventure/{AdventureGame.SceneFor(chapter)}.unity");
                    ApplyVariantCReferences(Find<AdventureGame>().Single());EditorSceneManager.SaveScene(scene);
                }
                RepairChapterOneScene();
            }
            finally{RestoreSetup(setup);}
            Build();Debug.Log("[Alif] Approved Variant C applied to Adventure chapters 1-5.");
        }
        [MenuItem("Alif/Adventure/Repair Missing Adventure Worlds From Original Maps")]
        public static void RepairMissingWorlds()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode first.");
            if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                for(int chapter=1;chapter<=5;chapter++)
                {
                    string path="Assets/Scenes/Adventure/"+AdventureGame.SceneFor(chapter)+".unity";
                    var existing=EditorSceneManager.OpenScene(path);
                    if(Find<PlayerController>().Length>0)continue; // Preserve authored/repaired worlds on repeated runs.
                    bool kos=chapter==2||chapter==3;
                    var scene=EditorSceneManager.OpenScene(kos?"Assets/Scenes/Chapter2Gameplay.unity":"Assets/Scenes/SampleScene.unity");
                    foreach(var behaviour in Find<MonoBehaviour>())
                        if(behaviour is RestaurantStoryTrigger || behaviour is RestaurantStoryPoint || behaviour is OpeningMonologueTrigger ||
                           behaviour is Chapter2StoryController || behaviour is Chapter2StoryPoint || behaviour is NPCController ||
                           behaviour is ChapterAdventure || behaviour is InvestigationPoint || behaviour is InteractableObject || behaviour is PauseMenuController)
                            behaviour.enabled=false;
                    foreach(var root in scene.GetRootGameObjects())
                        if(root.name=="BattleCanvas"||root.name=="ActionEncounter")root.SetActive(false);
                    var game=new GameObject("Adventure Campaign").AddComponent<AdventureGame>();game.Chapter=chapter;
                    game.Centers=kos?new[]{Vector2.zero,new Vector2(40,0),new Vector2(80,0)}:new[]{Vector2.zero,new Vector2(0,-20),new Vector2(40,-20),new Vector2(40,-40)};
                    game.Spawns=kos?new[]{new Vector2(-2.25f,-2.45f),new Vector2(40,-2),new Vector2(80,-2)}:new[]{new Vector2(0,-2.5f),new Vector2(0,-21),new Vector2(40,-21),new Vector2(39.35f,-43.38f)};
                    game.Cast=AssetDatabase.FindAssets("t:CharacterData").Select(g=>AssetDatabase.LoadAssetAtPath<CharacterData>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
                    game.InteractionArrow=Sprite("Assets/Sprites/UI/InteractionArrow.png");game.DocumentIcon=Sprite("Assets/Sprites/UI/Notice_Board.png");game.PromotionIcon=Sprite("Assets/Sprites/UI/Icon_VoucherPromo.png");
                    game.DanaKilat=AssetDatabase.LoadAssetAtPath<BattleEncounterDefinition>("Assets/ScriptableObjects/Battle/DanaKilat.asset");
                    if(chapter>=3)game.ActionEncounter=AssetDatabase.LoadAssetAtPath<ActionEncounterDefinition>($"Assets/ScriptableObjects/Campaign/Encounter{chapter}.asset");
                    if(kos)
                    {
                        AddArea("Lantai 1 kos",game.Centers[1],"Assets/Sprites/Backgrounds/KosKosan_Lantai1.png");
                        AddArea("Halaman kos",game.Centers[2],"Assets/Sprites/Backgrounds/KosKosan_Halaman.png");
                        AddBounds(Find<SpriteRenderer>().First(r=>r.name=="Background_Chapter2Kos"));
                        for(int area=0;area<2;area++)
                        {
                            AddDoor(game.Centers[area]+new Vector2(1.5f,-2),game.Spawns[area+1],"Menuju "+AdventureContent.Get(chapter).Areas[area+1]);
                            AddDoor(game.Centers[area+1]+new Vector2(-2.5f,-2),game.Spawns[area],"Kembali");
                        }
                    }
                    var player=Find<PlayerController>().Single();
                    var playerSettings=new SerializedObject(player);playerSettings.FindProperty("_interactableLayer").intValue=1<<7;playerSettings.ApplyModifiedPropertiesWithoutUndo();
                    Physics2D.SyncTransforms();
                    var content=AdventureContent.Get(chapter);
                    foreach(var group in content.Tasks.GroupBy(t=>t.Area))
                    {
                        int index=0;
                        foreach(var target in group.Select(t=>t.Target).Distinct())
                        {
                            var point=new GameObject(target+" • activity");point.layer=7;
                            Vector2 desired=game.Spawns[group.Key]+new Vector2((index%3-1)*1.2f, .8f+(index/3)*1.1f);index++;
                            point.transform.position=FindFreePosition(desired,game.Centers[group.Key],player);
                            var collider=point.AddComponent<CircleCollider2D>();collider.isTrigger=true;collider.radius=.3f;
                            var activity=point.AddComponent<AdventurePoint>();activity.Game=game;activity.Area=group.Key;activity.Target=target;
                            var visual=new GameObject("Marker").AddComponent<SpriteRenderer>();visual.transform.SetParent(point.transform,false);visual.sprite=game.DocumentIcon;visual.sortingOrder=100;
                            if(visual.sprite)visual.transform.localScale=Vector3.one*(.5f/visual.sprite.bounds.size.y);
                            var label=new GameObject("Objective label").AddComponent<TextMeshPro>();label.transform.SetParent(point.transform,false);label.transform.localPosition=new Vector3(0,.65f,0);label.fontSize=2.1f;label.alignment=TextAlignmentOptions.Center;label.rectTransform.sizeDelta=new Vector2(3,1);label.text=target;activity.Label=label;
                            var arrow=new GameObject("InteractionArrow").AddComponent<SpriteRenderer>();arrow.transform.SetParent(point.transform,false);arrow.transform.localPosition=new Vector3(0,1,0);arrow.sprite=game.InteractionArrow;arrow.sortingOrder=110;arrow.gameObject.SetActive(false);
                        }
                    }
                    EditorSceneManager.SaveScene(scene,path);
                    Debug.Log("[Alif] Repaired missing world: "+path);
                }
                RepairChapterOneScene();
                // Existing cutscenes keep their art/timing; only their gameplay destinations change.
                foreach(string name in new[]{"Chapter1Cutscene","Chapter2Cutscene"})
                {
                    var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");
                    foreach(var cutscene in Find<CutscenePlayerController>())
                    {var so=new SerializedObject(cutscene);so.FindProperty("_nextSceneName").stringValue=AdventureGame.SceneFor(name=="Chapter1Cutscene"?1:2);so.ApplyModifiedPropertiesWithoutUndo();}
                    EditorSceneManager.SaveScene(scene);
                }
            }
            finally{RestoreSetup(setup);}
            Build();
        }
        [MenuItem("Alif/Adventure/Repair Chapter 1 Direction and Interactions")]
        public static void RepairChapterOnePolish()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode first.");
            if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try{RepairChapterOneScene();}
            finally{RestoreSetup(setup);}
            Build();
        }
        static void RepairChapterOneScene()
        {
            string path="Assets/Scenes/Adventure/AdventureChapter1.unity";
            var scene=EditorSceneManager.OpenScene(path);
            var game=Find<AdventureGame>().Single();
            ApplyVariantCReferences(game);
            game.Centers=new[]{Vector2.zero,new Vector2(0,-20),new Vector2(40,-20),new Vector2(40,-40)};
            game.Spawns=new[]{new Vector2(0,-2.5f),new Vector2(0,-21),new Vector2(40,-21),new Vector2(39.35f,-43.38f)};

            foreach(var point in Find<AdventurePoint>())
                if(point.name.Contains("activity")||point.name=="Adventure interaction")Object.DestroyImmediate(point.gameObject);
                else Object.DestroyImmediate(point);
            foreach(string generated in new[]{"BuSiti_Gang","Raka_Gang","Gang_DirectionSign","PetugasStasiun","VariantC_StationLuggage","VariantC_StationTimetable","VariantC_GangPlanterCrates","VariantC_ReceiptTray","VariantC_Condiments","VariantC_ServingCounter"})
            {
                var old=Named(generated);if(old)Object.DestroyImmediate(old);
            }

            var station=Required("Station_NoticeBoard");
            var menu=Required("PapanMenu_Interact");
            var receipt=Required("MejaTungguPesanan");
            var cashier=Required("BuSiti_Kasir");
            station.transform.position=new Vector2(2.6f,-1.55f);
            SetSprite(station,game.SignFamilySprites.ElementAtOrDefault(0),1.35f);
            BindPoint(game,station,"Papan arah",0,(Vector2)station.transform.position+new Vector2(0,-.7f));

            var buGang=CloneNpc(Required("BuSiti_Kasir"),"BuSiti_Gang",new Vector2(38.35f,-21.35f));
            ConfigureReaction(buGang,game.ReactionPoseSprites.ElementAtOrDefault(1),game.EmoteSprites.ElementAtOrDefault(2));
            BindPoint(game,buGang,"Bu Siti",2,buGang.transform.position);
            var rakaGang=CloneNpc(Required("Raka_Tamu"),"Raka_Gang",new Vector2(41.55f,-21.35f));
            ConfigureReaction(rakaGang,game.ReactionPoseSprites.ElementAtOrDefault(2),game.EmoteSprites.ElementAtOrDefault(3));
            BindPoint(game,rakaGang,"Raka",2,rakaGang.transform.position);

            var gangSign=new GameObject("Gang_DirectionSign");gangSign.transform.position=new Vector2(42.15f,-18.95f);
            var signRenderer=gangSign.AddComponent<SpriteRenderer>();signRenderer.sprite=game.SignFamilySprites.ElementAtOrDefault(1)??game.DocumentIcon;signRenderer.sortingOrder=20;
            if(signRenderer.sprite)gangSign.transform.localScale=Vector3.one*(1.35f/signRenderer.sprite.bounds.size.y);
            BindPoint(game,gangSign,"Papan arah",2,new Vector2(42.15f,-20.1f));

            BindPoint(game,menu,"Papan menu",3,(Vector2)menu.transform.position+new Vector2(0,-.45f));
            BindPoint(game,receipt,"Meja jendela",3,(Vector2)receipt.transform.position+new Vector2(0,-.75f));
            ConfigureReaction(cashier,game.ReactionPoseSprites.ElementAtOrDefault(1),game.EmoteSprites.ElementAtOrDefault(0));
            BindPoint(game,cashier,"Bu Siti",3,(Vector2)cashier.transform.position+new Vector2(0,-.85f));

            var petugas=AddAmbience("PetugasStasiun",game.ReactionPoseSprites.ElementAtOrDefault(0),new Vector2(-4.35f,-1.65f),1.25f,18);
            ConfigureReaction(petugas,game.ReactionPoseSprites.ElementAtOrDefault(0),game.EmoteSprites.ElementAtOrDefault(1));
            AddAmbience("VariantC_StationLuggage",game.LocationPropSprites.ElementAtOrDefault(0),new Vector2(4.45f,-1.62f),1.15f,16);
            AddAmbience("VariantC_StationTimetable",game.LocationPropSprites.ElementAtOrDefault(1),new Vector2(-3.15f,-1.65f),.9f,15);
            AddAmbience("VariantC_GangPlanterCrates",game.LocationPropSprites.ElementAtOrDefault(2),new Vector2(35.25f,-19.05f),1.15f,14);
            AddAmbience("VariantC_ReceiptTray",game.LocationPropSprites.ElementAtOrDefault(3),new Vector2(38.35f,-39.05f),.55f,30);
            AddAmbience("VariantC_Condiments",game.LocationPropSprites.ElementAtOrDefault(4),new Vector2(42.55f,-39.05f),.65f,30);
            AddAmbience("VariantC_ServingCounter",game.LocationPropSprites.ElementAtOrDefault(5),new Vector2(43.45f,-38.15f),1.25f,18);
            EditorUtility.SetDirty(game);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene,path);
            Debug.Log("[Alif] Chapter 1 uses six explicit interaction props across the authored four-area route.");
        }
        static GameObject Named(string name)=>Find<Transform>().FirstOrDefault(t=>t.name==name)?.gameObject;
        static GameObject Required(string name)=>Named(name)??throw new InvalidOperationException("Missing Chapter 1 object: "+name);
        static GameObject CloneNpc(GameObject source,string name,Vector2 position)
        {
            var clone=Object.Instantiate(source);clone.name=name;clone.transform.SetParent(null);clone.transform.position=position;clone.SetActive(true);
            foreach(var behaviour in clone.GetComponents<MonoBehaviour>())behaviour.enabled=false;
            clone.AddComponent<AdventureNpcReaction>();return clone;
        }
        static void ConfigureReaction(GameObject npc,Sprite pose,Sprite emote)
        {
            if(!npc)return;
            var reaction=npc.GetComponent<AdventureNpcReaction>()??npc.AddComponent<AdventureNpcReaction>();
            reaction.Body=npc.GetComponentInChildren<SpriteRenderer>();reaction.ReactionPose=pose;reaction.EmoteSprite=emote;EditorUtility.SetDirty(reaction);
        }
        static GameObject AddAmbience(string name,Sprite sprite,Vector2 position,float height,int order)
        {
            var go=new GameObject(name);go.transform.position=position;var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sortingOrder=order;
            if(sprite)go.transform.localScale=Vector3.one*(height/sprite.bounds.size.y);return go;
        }
        static void SetSprite(GameObject go,Sprite sprite,float height)
        {
            var renderer=go.GetComponentInChildren<SpriteRenderer>();if(!renderer||!sprite)return;
            renderer.sprite=sprite;renderer.transform.localScale=Vector3.one*(height/sprite.bounds.size.y);EditorUtility.SetDirty(renderer);
        }
        static void ApplyVariantCReferences(AdventureGame game)
        {
            game.HudPanelSprite=Sprite(VariantC+"HudObjective.png");game.HudLocationSprite=Sprite(VariantC+"LocationTag.png");
            game.JournalButtonSprite=Sprite(VariantC+"JournalButton.png");game.PauseButtonSprite=Sprite(VariantC+"PauseButton.png");game.BatikDividerSprite=Sprite(VariantC+"BatikDivider.png");
            game.JoystickBaseSprite=Sprite(VariantC+"JoystickBase.png");game.JoystickKnobSprite=Sprite(VariantC+"JoystickKnob.png");game.InteractButtonSprite=Sprite(VariantC+"InteractButton.png");
            game.SignFamilySprites=new[]{Sprite(VariantC+"StationSign.png"),Sprite(VariantC+"GangSign.png")};
            game.LocationPropSprites=new[]{Sprite(VariantC+"StationLuggage.png"),Sprite(VariantC+"StationTimetable.png"),Sprite(VariantC+"GangPlanterCrates.png"),Sprite(VariantC+"ReceiptTray.png"),Sprite(VariantC+"Condiments.png"),Sprite(VariantC+"ServingCounter.png")};
            game.ReactionPoseSprites=new[]{Sprite(VariantC+"PetugasReaction.png"),Sprite(VariantC+"BuSitiReaction.png"),Sprite(VariantC+"RakaReaction.png")};
            game.EmoteSprites=new[]{Sprite(VariantC+"EmoteAlert.png"),Sprite(VariantC+"EmoteQuestion.png"),Sprite(VariantC+"EmoteHeart.png"),Sprite(VariantC+"EmoteSparkle.png"),Sprite(VariantC+"EmoteSweat.png")};
            EditorUtility.SetDirty(game);
        }
        static void ConfigureVariantCSprites()
        {
            foreach(string path in Directory.GetFiles(VariantC,"*.png"))
            {
                string assetPath=path.Replace('\\','/');var importer=AssetImporter.GetAtPath(assetPath) as TextureImporter;if(!importer)continue;
                bool world=assetPath.Contains("Sign.png")||assetPath.Contains("Luggage.png")||assetPath.Contains("Timetable.png")||assetPath.Contains("Crates.png")||assetPath.Contains("Tray.png")||assetPath.Contains("Condiments.png")||assetPath.Contains("Counter.png")||assetPath.Contains("Reaction.png");
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.spritePixelsPerUnit=100;
                var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=world?new Vector2(.5f,0):new Vector2(.5f,.5f);importer.SetTextureSettings(settings);
                importer.spriteBorder=assetPath.EndsWith("HudObjective.png")?new Vector4(55,55,55,55):assetPath.EndsWith("LocationTag.png")?new Vector4(28,28,28,28):Vector4.zero;
                importer.SaveAndReimport();
            }
        }
        static AdventurePoint BindPoint(AdventureGame game,GameObject prop,string target,int area,Vector2 triggerPosition)
        {
            foreach(var old in prop.transform.Cast<Transform>().Where(t=>t.name=="Objective label"||t.name=="InteractionArrow"||t.name=="Adventure interaction").ToArray())Object.DestroyImmediate(old.gameObject);
            var trigger=new GameObject("Adventure interaction");trigger.layer=7;trigger.transform.SetParent(prop.transform,true);trigger.transform.position=triggerPosition;
            var collider=trigger.AddComponent<CircleCollider2D>();collider.isTrigger=true;collider.radius=.42f;
            var point=trigger.AddComponent<AdventurePoint>();point.Game=game;point.Area=area;point.Target=target;point.Reaction=prop.GetComponent<AdventureNpcReaction>();
            var bounds=prop.GetComponentInChildren<SpriteRenderer>()?.bounds??new Bounds(prop.transform.position,Vector3.one);
            var label=new GameObject("Objective label").AddComponent<TextMeshPro>();label.transform.SetParent(prop.transform,true);label.transform.position=new Vector3(bounds.center.x,bounds.max.y+.22f,0);label.fontSize=2.1f;label.alignment=TextAlignmentOptions.Center;label.rectTransform.sizeDelta=new Vector2(3,1);label.text=target;point.Label=label;
            var arrow=new GameObject("InteractionArrow").AddComponent<SpriteRenderer>();arrow.transform.SetParent(prop.transform,true);arrow.transform.position=new Vector3(bounds.center.x,bounds.max.y+.65f,0);arrow.sprite=game.InteractionArrow;arrow.sortingOrder=110;arrow.gameObject.SetActive(false);
            return point;
        }
        static T[] Find<T>() where T:Object => Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        static void RestoreSetup(SceneSetup[] setup)
        {
            if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);
            else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        }
        static Sprite Sprite(string path)=>AssetDatabase.LoadAssetAtPath<Sprite>(path);
        static Vector2 FindFreePosition(Vector2 desired,Vector2 center,PlayerController player)
        {
            for(int ring=0;ring<30;ring++)for(int i=0;i<16;i++)
            {
                float a=i*Mathf.PI/8;Vector2 p=desired+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*ring*.15f;
                if(Vector2.Distance(p,center)>6)continue;
                if(!Physics2D.OverlapCircleAll(p,.4f).Any(c=>!c.isTrigger&&c.gameObject!=player.gameObject))return p;
            }
            throw new InvalidOperationException("No reachable activity placement near "+desired);
        }
        static void AddArea(string name,Vector2 center,string path)
        {
            var r=new GameObject(name).AddComponent<SpriteRenderer>();r.sprite=Sprite(path);r.transform.position=center;r.sortingOrder=0;
            r.transform.localScale=Vector3.one*(14/r.sprite.bounds.size.x);AddBounds(r);
            // Small shared walking rectangle; art-specific obstacle authoring remains explicit, never generated over the source map.
            var floor=new GameObject("Walkable floor");floor.transform.SetParent(r.transform,false);floor.transform.position=center;
            var box=floor.AddComponent<BoxCollider2D>();box.isTrigger=true;box.size=new Vector2(12,6)/r.transform.localScale.x;floor.AddComponent<WalkableArea>();
        }
        static void AddBounds(SpriteRenderer renderer)
        {
            var bounds=renderer.GetComponent<CameraBounds>()??renderer.gameObject.AddComponent<CameraBounds>();var so=new SerializedObject(bounds);
            so.FindProperty("_center").vector2Value=renderer.bounds.center;so.FindProperty("_halfExtents").vector2Value=renderer.bounds.extents;so.ApplyModifiedPropertiesWithoutUndo();
        }
        static void AddDoor(Vector2 position,Vector2 destination,string label)
        {
            var spawn=new GameObject(label+" spawn");spawn.transform.position=destination;
            var go=new GameObject(label);go.transform.position=position;var trigger=go.AddComponent<BoxCollider2D>();trigger.size=new Vector2(.7f,.7f);trigger.isTrigger=true;
            var door=go.AddComponent<SceneDoor>();var so=new SerializedObject(door);so.FindProperty("_destination").objectReferenceValue=spawn.transform;so.ApplyModifiedPropertiesWithoutUndo();
            var text=new GameObject("Door label").AddComponent<TextMeshPro>();text.transform.SetParent(go.transform,false);text.fontSize=2;text.text=label;text.alignment=TextAlignmentOptions.Center;text.rectTransform.sizeDelta=new Vector2(3,1);
        }
        public static void ValidateContent()
        {
            for(int c=1;c<=5;c++)
            {
                var chapter=AdventureContent.Get(c);var state=new AdventureState{Chapter=c,HighestUnlocked=c};
                if(chapter.Tasks.Select(t=>t.Id).Distinct().Count()!=chapter.Tasks.Length)throw new Exception("Duplicate objective ID");
                foreach(var task in chapter.Tasks)
                {
                    if(task.Kind=="encounter")state.CompleteEncounter(task.Id);
                    else if(task.Board!=null)
                    {
                        var board=task.Board;
                        if(board.Kind=="budget")
                        {
                            int mask=Enumerable.Range(0,1<<board.Cards.Length).First(m=>board.Solved(Enumerable.Range(0,board.Cards.Length).Select(i=>(m>>i)&1).ToArray()));
                            for(int i=0;i<board.Cards.Length;i++)state.Place(task,i,(mask>>i)&1,out _);
                        }
                        else for(int i=0;i<board.Cards.Length;i++)state.Place(task,i,board.Answers[i],out _);
                        if(!state.CommitBoard(task,out _))throw new Exception("Unsolvable board: "+task.Id);
                    }
                    else if(!state.Accept(task,0,out _))throw new Exception("Unreachable objective: "+task.Id);
                    state=AdventureSave.Decode(JsonUtility.ToJson(state));if(state==null)throw new Exception("Invalid checkpoint: "+task.Id);
                }
                state.Finish(chapter);if(!state.Completed)throw new Exception("Chapter cannot finish: "+c);
            }
        }
        public static void ValidateScenes()
        {
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach(string path in ScenePaths)
                {
                    if(!File.Exists(path))throw new Exception("Missing scene: "+path);
                    var scene=EditorSceneManager.OpenScene(path);
                    foreach(var root in scene.GetRootGameObjects())foreach(var transform in root.GetComponentsInChildren<Transform>(true))
                        if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject)>0)throw new Exception("Missing script: "+path+" / "+transform.name);
                    if(!path.Contains("AdventureChapter"))continue;
                    var game=Find<AdventureGame>().Single();var content=AdventureContent.Get(game.Chapter);
                    int players=Find<PlayerController>().Length,dialogues=Find<DialogueUI>().Length,fades=Find<SceneFadeController>().Length;
                    if(players!=1||dialogues!=1||fades!=1||game.Centers.Length!=content.Areas.Length||game.Spawns.Length!=game.Centers.Length)
                        throw new Exception($"Incomplete Adventure world: {path} (players={players}, dialogueUI={dialogues}, fades={fades}, centers={game.Centers.Length}, areas={content.Areas.Length}, spawns={game.Spawns.Length}). Run Repair Missing Adventure Worlds explicitly.");
                    if(game.Chapter==2&&(game.DanaKilat==null||game.DanaKilat.ValidationError()!=null))throw new Exception("Invalid Dana Kilat");
                    if(game.Chapter>=3&&(game.ActionEncounter==null||game.ActionEncounter.ValidationError()!=null))throw new Exception("Invalid action encounter");
                    foreach(var task in content.Tasks)if(!Find<AdventurePoint>().Any(p=>p.Area==task.Area&&p.Target==task.Target))throw new Exception("Missing point: "+task.Id);
                    if(game.Chapter==1)
                    {
                        foreach(var task in content.Tasks.GroupBy(t=>(t.Area,t.Target)).Select(g=>g.First()))
                            if(Find<AdventurePoint>().Count(p=>p.Area==task.Area&&p.Target==task.Target)!=1)throw new Exception("Chapter 1 objective must bind exactly once: "+task.Id);
                        if(Find<AdventurePoint>().Any(p=>p.name.Contains("activity")))throw new Exception("Chapter 1 still has a generic activity marker: "+path);
                    }
                    foreach(var door in Find<SceneDoor>())if(!door.Destination)throw new Exception("Missing door destination: "+path);
                }
            }
            finally{RestoreSetup(setup);}
        }
        public static void BuildMac(){Build();AlifStandaloneBuilder.BuildMacApp();}
        public static void BuildWeb(){Build();AlifStandaloneBuilder.BuildWebGL();}
    }
}
