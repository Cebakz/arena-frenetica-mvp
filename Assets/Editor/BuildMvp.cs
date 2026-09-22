using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BuildMvp
{
    const string Scene = "Assets/Scenes/Arena.unity";

    public static void CreateScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var go = new GameObject("Arena Frenetica MVP");
        go.AddComponent<ArenaGame>();
        EditorSceneManager.SaveScene(scene, Scene);
        PlayerSettings.productName = "Arena Frenetica";
        PlayerSettings.companyName = "Criteria";
        PlayerSettings.defaultScreenWidth = 540;
        PlayerSettings.defaultScreenHeight = 960;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Scene, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("MVP_SCENE_READY");
    }

    public static void BuildWindows()
    {
        CreateScene();
        var report = BuildPipeline.BuildPlayer(new[] { Scene }, "Builds/Windows/ArenaFrenetica.exe", BuildTarget.StandaloneWindows64, BuildOptions.None);
        Debug.Log("MVP_WINDOWS_BUILD=" + report.summary.result);
    }

    public static void BuildWeb()
    {
        CreateScene();
        var report = BuildPipeline.BuildPlayer(new[] { Scene }, "Builds/Web", BuildTarget.WebGL, BuildOptions.None);
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            string htmlPath = "Builds/Web/index.html";
            string html = File.ReadAllText(htmlPath);
            html = html.Replace("width=960 height=600", "width=540 height=960");
            html = html.Replace("canvas.style.width = \"960px\";", "canvas.style.width = \"100%\";");
            html = html.Replace("canvas.style.height = \"600px\";", "canvas.style.height = \"100%\";");
            html = html.Replace("<html lang=\"en-us\">", "<html lang=\"pt-BR\">");
            html = html.Replace("<meta charset=\"utf-8\">", "<meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1, user-scalable=no\">");
            File.WriteAllText(htmlPath, html);
            string cssPath = "Builds/Web/TemplateData/style.css";
            File.AppendAllText(cssPath, "\nbody { background: #101c29; overflow: hidden; }\n" +
                "#unity-container.unity-desktop { width: min(100vw, 56.25vh); height: min(100vh, 177.78vw); }\n" +
                "#unity-container.unity-desktop #unity-canvas { width: 100%; height: 100%; }\n" +
                "#unity-footer { display: none; }\n");
        }
        Debug.Log("MVP_WEB_BUILD=" + report.summary.result);
    }
}
