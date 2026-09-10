using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Management;

/// <summary>
/// Configura o visor de celular com o Cardboard oficial, fixado na tag v1.30.1.
/// A inicialização é manual para permitir preparar o visor antes de entrar em VR.
/// </summary>
public static class MobileVrSetup
{
    private const string LoaderType = "Google.XR.Cardboard.XRLoader";
    private const string XrSettingsPath = "Assets/XR/XRGeneralSettingsPerBuildTarget.asset";

    [MenuItem("Museum/VR Box/Configurar Android")]
    public static void Apply()
    {
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;
        PlayerSettings.Android.optimizedFramePacing = false;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel35;
        PlayerSettings.Android.forceInternetPermission = true;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });

        // Unity 2022 expõe estas opções somente pela serialização de PlayerSettings.
        // Both preserva os controles de prévia enquanto Cardboard usa Input System.
        var playerSettings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        SetInteger(playerSettings, "activeInputHandler", 2);
        SetBoolean(playerSettings, "useCustomMainManifest", true);
        SetBoolean(playerSettings, "useCustomMainGradleTemplate", true);
        SetBoolean(playerSettings, "useCustomGradlePropertiesTemplate", true);
        playerSettings.ApplyModifiedPropertiesWithoutUndo();

        ConfigureLoader();
        AssetDatabase.SaveAssets();
        ValidateConfiguration();
        Debug.Log("[MobileVrSetup] Android preparado: Cardboard, landscape, OpenGLES3 e entrada manual em VR.");
    }

    private static void ConfigureLoader()
    {
        EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget targets);
        if (targets == null)
        {
            string[] assets = AssetDatabase.FindAssets("t:XRGeneralSettingsPerBuildTarget");
            if (assets.Length > 0)
                targets = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(AssetDatabase.GUIDToAssetPath(assets[0]));
        }

        if (targets == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/XR"))
                AssetDatabase.CreateFolder("Assets", "XR");
            targets = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            AssetDatabase.CreateAsset(targets, XrSettingsPath);
        }

        EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, targets, true);
        if (!targets.HasSettingsForBuildTarget(BuildTargetGroup.Android))
            targets.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.Android);
        if (!targets.HasManagerSettingsForBuildTarget(BuildTargetGroup.Android))
            targets.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);

        XRGeneralSettings settings = targets.SettingsForBuildTarget(BuildTargetGroup.Android);
        settings.InitManagerOnStart = false;
        settings.Manager.automaticLoading = false;
        settings.Manager.automaticRunning = false;

        if (!XRPackageMetadataStore.IsLoaderAssigned(LoaderType, BuildTargetGroup.Android) &&
            !XRPackageMetadataStore.AssignLoader(settings.Manager, LoaderType, BuildTargetGroup.Android))
            throw new BuildFailedException("Não foi possível configurar Cardboard. Aguarde a importação dos pacotes e execute novamente.");

        EditorUtility.SetDirty(settings.Manager);
        EditorUtility.SetDirty(settings);
        EditorUtility.SetDirty(targets);
    }

    public static void ValidateConfiguration()
    {
        XRGeneralSettings settings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
        if (settings == null || settings.Manager == null ||
            !XRPackageMetadataStore.IsLoaderAssigned(LoaderType, BuildTargetGroup.Android))
            throw new BuildFailedException("Cardboard não está configurado para Android. Execute Museum > VR Box > Configurar Android.");
        if (settings.InitManagerOnStart)
            throw new BuildFailedException("A inicialização automática de XR deve estar desligada para a preparação do VR Box.");

        foreach (string file in new[] { "AndroidManifest.xml", "mainTemplate.gradle", "gradleTemplate.properties" })
        {
            if (!File.Exists(Path.Combine("Assets/Plugins/Android", file)))
                throw new BuildFailedException("Arquivo Android obrigatório não encontrado: " + file);
        }
    }

    private static void SetInteger(SerializedObject settings, string name, int value)
    {
        SerializedProperty property = settings.FindProperty(name);
        if (property == null)
            throw new InvalidOperationException("Opção não encontrada nesta versão do Unity: " + name);
        property.intValue = value;
    }

    private static void SetBoolean(SerializedObject settings, string name, bool value)
    {
        SerializedProperty property = settings.FindProperty(name);
        if (property == null)
            throw new InvalidOperationException("Opção não encontrada nesta versão do Unity: " + name);
        property.boolValue = value;
    }
}
