using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSetupScript
{
    public static void SetupAndBuild()
    {
        Debug.Log("[SceneSetup] Iniciando setup da cena com calibração 3D...");

        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // 1. Configurar o Importer do modelo para garantir escala e materiais
        string modelPath = "Assets/Models/versao0.2_sModerna.blend";
        ModelImporter importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
        if (importer != null)
        {
            importer.globalScale = 1f;
            importer.importCameras = false;
            importer.importLights = false;
            // importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.SaveAndReimport();
            Debug.Log("[SceneSetup] ModelImporter configurado e reimportado.");
        }

        // 2. Instanciar o modelo do museu
        GameObject museumPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (museumPrefab != null)
        {
            GameObject museumInstance = (GameObject)PrefabUtility.InstantiatePrefab(museumPrefab);
            museumInstance.name = "MuseumEnvironment";
            museumInstance.transform.position = Vector3.zero;
            museumInstance.transform.localScale = Vector3.one;

            // Encontrar os limites (bounds) do museu para posicionar a câmera
            Renderer[] renderers = museumInstance.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                foreach (Renderer r in renderers) bounds.Encapsulate(r.bounds);
                
                Debug.Log($"[SceneSetup] Museu bounds: Center={bounds.center}, Size={bounds.size}");

                // Posicionar a câmera no centro do museu, altura 1.6m acima do chão
                GameObject mainCamera = GameObject.Find("Main Camera");
                if (mainCamera != null)
                {
                    mainCamera.name = "PlayerCamera";
                    mainCamera.transform.position = new Vector3(bounds.center.x, bounds.min.y + 1.6f, bounds.center.z);
                    
                    // Adicionar scripts
                    System.Type playerControllerType = GetType("MuseumModerna.PlayerController") ?? GetType("PlayerController");
                    System.Type gyroControllerType = GetType("MuseumModerna.GyroscopeController") ?? GetType("GyroscopeController");
                    System.Type headGazeType = GetType("MuseumModerna.HeadGazeMovement") ?? GetType("HeadGazeMovement");

                    if (playerControllerType != null && mainCamera.GetComponent(playerControllerType) == null) mainCamera.AddComponent(playerControllerType);
                    if (gyroControllerType != null && mainCamera.GetComponent(gyroControllerType) == null) mainCamera.AddComponent(gyroControllerType);
                    if (headGazeType != null && mainCamera.GetComponent(headGazeType) == null) mainCamera.AddComponent(headGazeType);
                    
                    // Adicionar a trava absoluta para evitar quedas no vazio
                    System.Type lockType = GetType("MuseumModerna.LockPosition") ?? GetType("LockPosition");
                    if (lockType != null && mainCamera.GetComponent(lockType) == null) mainCamera.AddComponent(lockType);
                }
            }
            else
            {
                Debug.LogWarning("[SceneSetup] O modelo do museu não possui nenhum Renderer (geometria invisível ou vazia)!");
            }
        }
        else
        {
            Debug.LogError("[SceneSetup] Falha ao carregar o prefab do museu. O arquivo existe?");
        }

        // 3. Adicionar uma luz caso a cena esteja escura (garantia)
        if (GameObject.FindObjectOfType<Light>() == null)
        {
            GameObject lightGO = new GameObject("Directional Light");
            Light light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            lightGO.transform.rotation = Quaternion.Euler(50, -30, 0);
        }

        // Salvar a cena
        string scenePath = "Assets/Scenes/MuseumScene.unity";
        EditorSceneManager.SaveScene(newScene, scenePath);
        Debug.Log($"[SceneSetup] Cena salva em {scenePath}");

        // Atualizar Build Settings
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };

        // Chamar o script de build
        BuildScript.BuildAndroid();
    }

    private static System.Type GetType(string typeName)
    {
        var type = System.Type.GetType(typeName);
        if (type != null) return type;
        foreach (var a in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            type = a.GetType(typeName);
            if (type != null) return type;
        }
        return null;
    }
}
