using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;

public class BuildScript
{
    public static void BuildAndroid()
    {
        string outputDir = "Builds/Android";
        string apkPath   = outputDir + "/MuseudaSemanaArteModerna.apk";

        Directory.CreateDirectory(outputDir);

        // Configurar Android
        PlayerSettings.companyName = "SemanaArteModerna";
        PlayerSettings.productName = "Museu da Semana de Arte Moderna";
        PlayerSettings.applicationIdentifier = "com.SemanaArteModerna.MuseudaSemanaArteModerna";
        PlayerSettings.Android.minSdkVersion    = AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel34;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

        // Criar uma cena se não existir nenhuma
        string[] scenes = GetScenes();
        if (scenes.Length == 0)
        {
            Debug.Log("[BuildScript] Nenhuma cena encontrada. Criando cena padrão...");
            Directory.CreateDirectory("Assets/Scenes");
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EditorSceneManager.SaveScene(newScene, "Assets/Scenes/SampleScene.unity");
            scenes = new string[] { "Assets/Scenes/SampleScene.unity" };
        }

        Debug.Log($"[BuildScript] Compilando {scenes.Length} cena(s): {string.Join(", ", scenes)}");

        BuildPlayerOptions opts = new BuildPlayerOptions
        {
            scenes           = scenes,
            locationPathName = apkPath,
            target           = BuildTarget.Android,
            options          = BuildOptions.None,
        };

        BuildReport  report  = BuildPipeline.BuildPlayer(opts);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[BuildScript] ✅ BUILD OK! APK em: {Path.GetFullPath(apkPath)} ({summary.totalSize / 1024 / 1024} MB)");
        }
        else
        {
            Debug.LogError($"[BuildScript] ❌ BUILD FALHOU: {summary.totalErrors} erros");
            EditorApplication.Exit(1);
        }
    }

    static string[] GetScenes()
    {
        var scenes = new System.Collections.Generic.List<string>();
        foreach (var s in EditorBuildSettings.scenes)
            if (s.enabled && File.Exists(s.path)) scenes.Add(s.path);

        if (scenes.Count == 0)
        {
            string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
            foreach (var g in guids)
                scenes.Add(AssetDatabase.GUIDToAssetPath(g));
        }
        return scenes.ToArray();
    }
}
