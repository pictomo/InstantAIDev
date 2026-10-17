using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace StarPaws.Editor
{
    [InitializeOnLoad]
    public static class StarPawsBuild
    {
        const string Root="Assets/StarPaws/Resources/StarPaws";
        static StarPawsBuild()
        {
            EditorApplication.delayCall+=EnsureMaterials;
            EditorApplication.update+=()=>
            {
                if(!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&File.Exists("Library/StarPaws.build.request"))
                {File.Delete("Library/StarPaws.build.request");BuildMac();}
            };
        }
        static void EnsureMaterials()
        {
            if(File.Exists(Root+"/Surface.mat"))return;
            Directory.CreateDirectory(Root);AssetDatabase.Refresh();
            var surface=new Material(Shader.Find("Standard"));surface.SetFloat("_Glossiness",0.32f);AssetDatabase.CreateAsset(surface,Root+"/Surface.mat");
            var neon=new Material(surface);neon.EnableKeyword("_EMISSION");neon.SetColor("_EmissionColor",Color.white);AssetDatabase.CreateAsset(neon,Root+"/Neon.mat");
            var ribbon=new Material(Shader.Find("Sprites/Default"));AssetDatabase.CreateAsset(ribbon,Root+"/Ribbon.mat");AssetDatabase.SaveAssets();
        }
        [MenuItem("StarPaws/Build macOS app")]
        public static void BuildMac()
        {
            EnsureMaterials();Directory.CreateDirectory("Builds");
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {scenes=new[]{"Assets/Scenes/StarPaws.unity"},locationPathName="Builds/StarPaws.app",target=BuildTarget.StandaloneOSX,options=BuildOptions.None});
            string result=report.summary.result+" | errors="+report.summary.totalErrors+" | warnings="+report.summary.totalWarnings+" | size="+report.summary.totalSize;
            File.WriteAllText("Library/StarPaws-build.txt",result);Debug.Log("StarPaws macOS build: "+result);
        }
    }
}
