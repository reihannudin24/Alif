using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Alif.EditorTools
{
    /// <summary>
    /// Captures rendered screenshots of the key player-facing scenes into
    /// Logs/Screenshots/ for visual QA. Consumed by the GLM-5.3-Flash vision
    /// evaluator in Tools/alif-agent (invoked as ./Tools/alif capture-screens).
    /// Scenes are opened without saving; overlay canvases are temporarily
    /// converted to screen-space-camera so uGUI renders into the capture target.
    /// </summary>
    public static class AlifScreenshotCapture
    {
        struct Shot
        {
            public string Scene, Tag;
            public int Width, Height;
        }

        static readonly Shot[] Shots =
        {
            new Shot{Scene="Assets/Scenes/MainMenu.unity",Tag="menu-720p",Width=1280,Height=720},
            new Shot{Scene="Assets/Scenes/ChapterSelect.unity",Tag="select-720p",Width=1280,Height=720},
            new Shot{Scene="Assets/Scenes/Chapter1Cutscene.unity",Tag="cutscene-720p",Width=1280,Height=720},
            new Shot{Scene="Assets/Scenes/Adventure/AdventureChapter1.unity",Tag="chapter1-720p",Width=1280,Height=720},
            new Shot{Scene="Assets/Scenes/Adventure/AdventureChapter1.unity",Tag="chapter1-1080p",Width=1920,Height=1080},
            new Shot{Scene="Assets/Scenes/Adventure/AdventureChapter2.unity",Tag="chapter2-720p",Width=1280,Height=720},
            new Shot{Scene="Assets/Scenes/Adventure/AdventureChapter3.unity",Tag="chapter3-720p",Width=1280,Height=720},
            new Shot{Scene="Assets/Scenes/Adventure/AdventureChapter4.unity",Tag="chapter4-720p",Width=1280,Height=720},
            new Shot{Scene="Assets/Scenes/Adventure/AdventureChapter5.unity",Tag="chapter5-720p",Width=1280,Height=720},
            new Shot{Scene="Assets/Scenes/Chapter1Ending.unity",Tag="ending-720p",Width=1280,Height=720},
        };

        [MenuItem("Alif/QA/Capture Screenshots")]
        public static void Capture()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode before capturing screenshots.");
            if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            string outputDir=Path.Combine(ProjectRoot(),"Logs","Screenshots");
            Directory.CreateDirectory(outputDir);
            var setup=EditorSceneManager.GetSceneManagerSetup();
            int written=0;
            try
            {
                string openScene=null;
                foreach(var shot in Shots)
                {
                    if(openScene!=shot.Scene)
                    {
                        EditorSceneManager.OpenScene(shot.Scene,OpenSceneMode.Single);
                        openScene=shot.Scene;
                    }
                    var camera=FindCamera();
                    if(camera==null){Debug.LogWarning($"[Alif] No camera in {shot.Scene}; skipped.");continue;}
                    string name=Path.GetFileNameWithoutExtension(shot.Scene);
                    string path=Path.Combine(outputDir,$"{name}-{shot.Tag}.png");
                    written+=CaptureScene(camera,shot.Width,shot.Height,path)?1:0;
                }
            }
            finally{RestoreSetup(setup);}
            Debug.Log($"[Alif] Captured {written} screenshot(s) to Logs/Screenshots.");
            if(written==0)throw new InvalidOperationException("Screenshot capture produced no images.");
        }

        static bool CaptureScene(Camera camera,int width,int height,string path)
        {
            var overlay=new List<Canvas>();
            foreach(var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if(canvas.renderMode!=RenderMode.ScreenSpaceOverlay||!canvas.isActiveAndEnabled)continue;
                canvas.renderMode=RenderMode.ScreenSpaceCamera;
                canvas.worldCamera=camera;
                canvas.planeDistance=Mathf.Max(camera.nearClipPlane+0.5f,0.5f);
                overlay.Add(canvas);
            }
            float aspect=camera.aspect;
            try
            {
                var target=new RenderTexture(width,height,24);
                var image=new Texture2D(width,height,TextureFormat.RGB24,false);
                RenderTexture previous=RenderTexture.active;
                try
                {
                    camera.targetTexture=target;
                    camera.aspect=(float)width/height;
                    camera.Render();
                    RenderTexture.active=target;
                    image.ReadPixels(new Rect(0,0,width,height),0,0);
                    image.Apply();
                    File.WriteAllBytes(path,image.EncodeToPNG());
                    Debug.Log($"[Alif] Captured {Path.GetFileName(path)}.");
                    return true;
                }
                finally
                {
                    camera.targetTexture=null;
                    camera.aspect=aspect;
                    RenderTexture.active=previous;
                    Object.DestroyImmediate(image);
                    target.Release();
                    Object.DestroyImmediate(target);
                }
            }
            catch(Exception e)
            {
                Debug.LogError($"[Alif] Failed to capture {path}: {e.Message}");
                return false;
            }
            finally
            {
                foreach(var canvas in overlay)
                {
                    canvas.renderMode=RenderMode.ScreenSpaceOverlay;
                    canvas.worldCamera=null;
                }
            }
        }

        // Sama seperti CaptureScene, tetapi canvas HUD (ScreenSpaceOverlay) disembunyikan
        // dulu — thumbnail chapter harus memperlihatkan dunianya saja tanpa joystick/HUD.
        static bool CaptureWorldOnly(Camera camera,int width,int height,string path)
        {
            var hidden=new List<Canvas>();
            foreach(var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if(canvas.renderMode!=RenderMode.ScreenSpaceOverlay||!canvas.isActiveAndEnabled)continue;
                canvas.gameObject.SetActive(false);
                hidden.Add(canvas);
            }
            try{return CaptureScene(camera,width,height,path);}
            finally
            {
                foreach(var canvas in hidden)
                    if(canvas!=null)canvas.gameObject.SetActive(true);
            }
        }

        static Camera FindCamera()
        {
            var main=Camera.main;
            if(main!=null)return main;
            foreach(var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if(camera.isActiveAndEnabled)return camera;
            return null;
        }

        // Thumbnail chapter-select ditangkap dari dunia aslinya (bukan art terpisah) supaya
        // kartu selalu cocok secara visual dengan yang pemain lihat di dalam game.
        [MenuItem("Alif/QA/Capture Chapter Thumbnails")]
        public static void CaptureChapterThumbnails()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode before capturing thumbnails.");
            if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            string outputFolder="Assets/Sprites/Campaign";
            Directory.CreateDirectory(Path.Combine(ProjectRoot(),outputFolder));
            var setup=EditorSceneManager.GetSceneManagerSetup();
            int written=0;
            try
            {
                for(int chapter=1;chapter<=5;chapter++)
                {
                    string scenePath=$"Assets/Scenes/Adventure/AdventureChapter{chapter}.unity";
                    if(!File.Exists(scenePath))continue;
                    var scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Single);
                    var camera=FindCamera();
                    if(camera==null){Debug.LogWarning($"[Alif] No camera in {scenePath}; thumbnail skipped.");continue;}
                    var game=Object.FindAnyObjectByType<Alif.Adventure.AdventureGame>();
                    // Bidik area pertama chapter — representatif sebagai sampul kartu.
                    if(game!=null&&game.Centers!=null&&game.Centers.Length>0)
                        camera.transform.position=new Vector3(game.Centers[0].x,game.Centers[0].y,camera.transform.position.z);
                    string path=Path.Combine(ProjectRoot(),outputFolder,$"Thumbnail_Chapter{chapter}.png");
                    if(CaptureWorldOnly(camera,1280,560,path))
                    {
                        AssetDatabase.ImportAsset($"{outputFolder}/Thumbnail_Chapter{chapter}.png");
                        var importer=(TextureImporter)AssetImporter.GetAtPath($"{outputFolder}/Thumbnail_Chapter{chapter}.png");
                        importer.textureType=TextureImporterType.Sprite;
                        importer.spriteImportMode=SpriteImportMode.Single;
                        importer.filterMode=FilterMode.Bilinear; // render dunia painted, bukan pixel-art mentah
                        importer.mipmapEnabled=false;
                        importer.spritePixelsPerUnit=100f;
                        importer.SaveAndReimport();
                        written++;
                    }
                }
            }
            finally{RestoreSetup(setup);AssetDatabase.SaveAssets();}
            Debug.Log($"[Alif] Captured {written} chapter thumbnail(s) to {outputFolder}.");
            if(written==0)throw new InvalidOperationException("Chapter thumbnail capture produced no images.");
        }

        static string ProjectRoot()=>Path.GetFullPath(Path.Combine(Application.dataPath,".."));

        static void RestoreSetup(SceneSetup[] setup)
        {
            if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);
            else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        }
    }
}
