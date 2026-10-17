using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StarPaws.Editor
{
    [InitializeOnLoad]
    public static class StarPawsEditor
    {
        const string ScenePath="Assets/Scenes/StarPaws.unity";
        const string VerifyRequest="Library/StarPaws.verify.request";
        const string CaptureRequest="Library/StarPaws.capture.request";
        static bool verifying;
        static double verifyDeadline;
        static readonly List<string> errors=new List<string>();
        static StarPawsEditor()
        {
            verifying=SessionState.GetBool("StarPaws.Verifying",false);verifyDeadline=EditorApplication.timeSinceStartup+30;
            EditorApplication.delayCall+=()=>{if(!File.Exists(ScenePath)&&!EditorApplication.isPlayingOrWillChangePlaymode)BuildScene();};
            EditorApplication.update+=Poll;
            Application.logMessageReceived+=(message,stack,type)=>{if(verifying&&(type==LogType.Error||type==LogType.Exception||type==LogType.Assert))errors.Add(message);};
        }
        [MenuItem("StarPaws/Create or open game scene")]
        public static void BuildScene()
        {
            if(File.Exists(ScenePath)){EditorSceneManager.OpenScene(ScenePath);return;}
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var game=new GameObject("STARPAWS • Play to begin").AddComponent<StarPawsGame>();
            game.skyShader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/StarPaws/Shaders/CosmicSky.shader");
            game.glowShader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/StarPaws/Shaders/SoftGlow.shader");
            game.bloomShader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/StarPaws/Shaders/Bloom.shader");
            EditorSceneManager.SaveScene(scene,ScenePath);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
            PlayerSettings.productName="StarPaws";PlayerSettings.companyName="Little Orbit";
            PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;
            PlayerSettings.defaultIsNativeResolution=false;PlayerSettings.resizableWindow=true;
            PlayerSettings.runInBackground=true;
            AssetDatabase.SaveAssets();Debug.Log("StarPaws scene is ready. Press Play, then Space to run.");
        }
        [MenuItem("StarPaws/Run gameplay verification")]
        public static void Verify(){File.WriteAllText(VerifyRequest,"run");}
        static void Poll()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            if(File.Exists(VerifyRequest)&&!verifying)
            {
                File.Delete(VerifyRequest);errors.Clear();verifying=true;SessionState.SetBool("StarPaws.Verifying",true);verifyDeadline=EditorApplication.timeSinceStartup+30;
                if(!EditorApplication.isPlaying){if(SceneManager.GetActiveScene().path!=ScenePath)EditorSceneManager.OpenScene(ScenePath);EditorApplication.isPlaying=true;}
            }
            if(verifying)
            {
                var game=UnityEngine.Object.FindObjectOfType<StarPawsGame>();
                if(EditorApplication.isPlaying&&game&&game.PlayerPosition==Vector3.zero&&game.State==FlightState.Ready)RunChecks(game);
                else if(EditorApplication.timeSinceStartup>verifyDeadline){verifying=false;SessionState.SetBool("StarPaws.Verifying",false);File.WriteAllText("Library/StarPaws-verification.txt","FAIL: Play mode did not initialize in time.");}
            }
            if(File.Exists(CaptureRequest)&&EditorApplication.isPlaying)
            {
                string path=File.ReadAllText(CaptureRequest).Trim();File.Delete(CaptureRequest);
                ScreenCapture.CaptureScreenshot(string.IsNullOrEmpty(path)?"StarPaws-preview.png":path);
            }
        }
        static void RunChecks(StarPawsGame game)
        {
            var report=new List<string>();
            Action<bool,string> check=(ok,name)=>report.Add((ok?"PASS: ":"FAIL: ")+name);
            int originalBest=PlayerPrefs.GetInt("StarPaws.Best",0);
            try
            {
                check(game.State==FlightState.Ready,"Starts on the welcome screen");
                game.StartRun();for(int i=0;i<30;i++)game.Simulate(1f/120,1,false);
                check(game.PlayerPosition.x>0.8f,"Right input moves the kitten right");
                for(int i=0;i<90;i++)game.Simulate(1f/120,-1,false);
                check(game.PlayerPosition.x<0,"Left input moves the kitten left");
                game.StartRun();game.Simulate(1f/120,0,true);check(!game.Grounded&&game.PlayerPosition.y>0,"Space launches a jump");
                for(int i=0;i<30;i++)game.Simulate(1f/120,0,true);
                check(game.JumpCount==1,"Holding jump does not add air jumps");
                for(int i=0;i<90;i++)game.Simulate(1f/120,0,false);
                check(game.LandingCount>0,"Kitten lands on a platform after jumping");
                game.StartRun();for(int i=0;i<145;i++)game.Simulate(1f/120,0,false);
                check(game.CollapseCount>0,"A stepped-on platform flashes and collapses");
                // A deterministic centre-lane pilot crosses the first two gaps and collects stars.
                game.StartRun();
                for(int i=0;i<510&&game.State==FlightState.Running;i++)
                {
                    float z=game.PlayerPosition.z;bool leap=game.Grounded&&((z>5.5f&&z<7.5f)||(z>14.5f&&z<16.5f)||(z>24.5f&&z<26.5f));
                    game.Simulate(1f/120,0,leap);
                }
                check(game.Stars>=2,"Collects stars across consecutive platforms");
                check(game.Score>=game.Stars*100,"Score includes star bonuses and distance");
                var trails=game.GetComponentsInChildren<TrailRenderer>();bool rainbow=trails.Length==7;
                foreach(var trail in trails)rainbow&=trail.emitting&&trail.time>0.55f;
                check(rainbow,"Seven rainbow ribbons grow after star collection");
                game.Pause();Vector3 paused=game.PlayerPosition;game.Simulate(1,1,true);check(game.PlayerPosition==paused&&game.State==FlightState.Paused,"Pause freezes movement");game.Resume();
                for(int i=0;i<600&&game.State==FlightState.Running;i++)game.Simulate(1f/120,1,false);
                check(game.State==FlightState.Fallen,"Leaving the course produces the retry screen");
                check(PlayerPrefs.GetInt("StarPaws.Best",0)>=game.Score,"Best score persists after falling");
                game.StartRun();check(game.State==FlightState.Running&&game.Stars==0&&game.Distance==0&&game.PlayerPosition==Vector3.zero,"Instant retry resets the run");
                bool cleared=true;foreach(var trail in trails)cleared&=!trail.emitting&&trail.positionCount==0;check(cleared,"Retry clears every rainbow trail");
                for(int repeat=0;repeat<8;repeat++)game.StartRun();
                check(game.Stars==0&&game.State==FlightState.Running,"Repeated retries remain valid");
                check(errors.Count==0,"No runtime errors during verification");
                foreach(var e in errors)report.Add("ERROR: "+e);
            }
            catch(Exception e){report.Add("FAIL: "+e);}
            finally
            {
                PlayerPrefs.SetInt("StarPaws.Best",originalBest);PlayerPrefs.Save();
                File.WriteAllLines("Library/StarPaws-verification.txt",report);verifying=false;SessionState.SetBool("StarPaws.Verifying",false);
                Debug.Log("StarPaws gameplay verification:\n"+string.Join("\n",report));EditorApplication.isPlaying=false;
            }
        }
    }
}
