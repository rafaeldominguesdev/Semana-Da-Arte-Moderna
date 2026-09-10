using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;

public class BuildScript
{
    [MenuItem("Museum/VR Box/Gerar APK Android")]
    public static void BuildAndroid()
    {
        string projectPath = Directory.GetParent(Application.dataPath).FullName;
        string workspacePath = Directory.GetParent(projectPath).FullName;
        string outputDir   = Path.Combine(workspacePath, "artifacts", "MuseumVR_Build");
        string apkPath     = Path.Combine(outputDir, "MuseudaSemanaArteModerna.apk");

        Directory.CreateDirectory(outputDir);

        // Usa SDK/NDK/JDK configurados no Unity, inclusive os módulos do Unity Hub.
        PlayerSettings.companyName = "SemanaArteModerna";
        PlayerSettings.productName = "Museu da Semana de Arte Moderna";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.SemanaArteModerna.MuseudaSemanaArteModerna");
        EditorUserBuildSettings.buildAppBundle = false;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
        MobileVrSetup.Apply();

        // Um APK do museu deve conter a cena real, nunca uma cena padrão vazia.
        string[] scenes = GetScenes();

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
            Debug.Log($"[BuildScript] BUILD OK. APK em: {Path.GetFullPath(apkPath)} ({summary.totalSize / 1024 / 1024} MB)");
        }
        else
        {
            throw new BuildFailedException($"BUILD FALHOU: {summary.totalErrors} erros ({summary.result}).");
        }
    }

    static string[] GetScenes()
    {
        const string museumScene = "Assets/Scenes/MuseumScene.unity";
        if (!File.Exists(museumScene))
            throw new BuildFailedException("Cena do museu não encontrada: " + museumScene);
        return new[] { museumScene };
    }
}
