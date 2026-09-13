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

        static Camera FindCamera()
        {
            var main=Camera.main;
            if(main!=null)return main;
            foreach(var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if(camera.isActiveAndEnabled)return camera;
            return null;
        }

        static string ProjectRoot()=>Path.GetFullPath(Path.Combine(Application.dataPath,".."));

        static void RestoreSetup(SceneSetup[] setup)
        {
            if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);
            else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        }
    }
}
