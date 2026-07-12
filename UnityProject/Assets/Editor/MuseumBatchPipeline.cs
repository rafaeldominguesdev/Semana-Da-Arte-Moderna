using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MuseumModerna
{
    /// <summary>
    /// Orquestrador para rodar toda a pipeline MuseumModerna sem interação manual no Editor,
    /// via: Unity -batchmode -quit -projectPath <proj> -executeMethod MuseumModerna.MuseumBatchPipeline.RunAll
    ///
    /// Chama os métodos internos (privados) de cada ferramenta MuseumModerna via reflection,
    /// na ordem recomendada em SETUP.md, evitando os métodos "wrapper" de nível superior que
    /// terminam em EditorUtility.DisplayDialog (todos já protegidos com Application.isBatchMode,
    /// mas preferimos não depender de dialogs em lote de qualquer forma).
    ///
    /// NÃO recria a geometria base (MuseumCompleteBuilder) — assume que a cena já foi construída
    /// (Museum_Geometry / Museum_Exhibits já existem), evitando destruir trabalho manual existente.
    /// </summary>
    public static class MuseumBatchPipeline
    {
        private const string ScenePath = "Assets/Scenes/MuseumScene.unity";

        public static void RunAll()
        {
            Debug.Log("[MuseumBatchPipeline] Iniciando pipeline completo...");

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
                throw new Exception($"[MuseumBatchPipeline] Não foi possível abrir a cena: {ScenePath}");

            if (GameObject.Find("Museum_Geometry") == null)
                Debug.LogWarning("[MuseumBatchPipeline] 'Museum_Geometry' não encontrado — " +
                    "geometria base pode não ter sido construída ainda. Continuando mesmo assim.");

            RunStep("1/9 Física da Sala",        RunRoomPhysics);
            RunStep("2/9 Decoração do Museu",    () => RunPrivateInstance<MuseumDecorationSetup>("CreateAll"));
            RunStep("3/9 Texturas PBR",          () => RunPrivateInstance<MuseumTextureSetup>("RunAll"));
            RunStep("4/9 Iluminação Dramática",  RunDramaticLighting);
            RunStep("5/9 Pós-processamento",     RunPostProcess);
            RunStep("6/9 Áudio Ambiente",        RunAudio);
            RunStep("7/9 Iluminação Avançada",   RunAdvancedLighting);
            RunStep("8/9 Bake de Lightmaps",     RunLightmapBake);
            RunStep("9/9 Otimização Mobile/VR",  RunOptimization);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene);
            Debug.Log($"[MuseumBatchPipeline] Cena salva: {saved}");

            Debug.Log("[MuseumBatchPipeline] Pipeline completo finalizado com sucesso!");
        }

        // ── Passo a passo (sem os wrappers que terminam em DisplayDialog) ──────

        private static void RunRoomPhysics()
        {
            // RoomPhysicsSetup.RunFullSetup() já é seguro em batch (dialog protegido),
            // mas chamamos direto para manter log determinístico.
            RunPrivateStatic<RoomPhysicsSetup>("RunFullSetup");
        }

        private static void RunDramaticLighting()
        {
            // Escolha única de iluminação dramática (ver SETUP.md: não roda o
            // Step3_Scene.SetupLighting do MuseumCompleteBuilder junto — este batch
            // nunca chama MuseumCompleteBuilder, então não há conflito de Spot_* aqui).
            RunPrivateInstance<MuseumLightingSetup>("ApplyAll");

            // Segurança extra: remove eventuais spotlights "Spot_*" antigos (nomenclatura
            // usada pelo MuseumCompleteBuilder) para não duplicar luz por quadro caso a
            // cena já tivesse sido construída com aquele padrão em uma sessão anterior.
            RemoveLegacySpotlights();
        }

        private static void RemoveLegacySpotlights()
        {
            int removed = 0;
            foreach (var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.gameObject.name.StartsWith("Spot_") && !light.gameObject.name.StartsWith("Spotlight_"))
                {
                    UnityEngine.Object.DestroyImmediate(light.gameObject);
                    removed++;
                }
            }
            if (removed > 0)
                Debug.Log($"[MuseumBatchPipeline] {removed} spotlight(s) legado(s) 'Spot_*' removido(s) para evitar luz duplicada.");
        }

        private static void RunPostProcess()
        {
            RunPrivateStatic<MuseumPostProcessSetup>("ApplyPostProcess");
        }

        private static void RunAudio()
        {
            var instance = ScriptableObject.CreateInstance<MuseumAudioSetup>();
            try
            {
                Invoke(instance, "GenerateFootstepsAndAssign");
                Invoke(instance, "GenerateAmbientAndAssign");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static void RunAdvancedLighting()
        {
            var instance = ScriptableObject.CreateInstance<MuseumAdvancedLightingSetup>();
            try
            {
                Invoke(instance, "MarkStaticObjects");
                Invoke(instance, "ConfigureLightmapSettings");
                Invoke(instance, "ConfigureLightsMixed");
                Invoke(instance, "PlaceLightProbes");
                Invoke(instance, "AddReflectionProbe");
                Invoke(instance, "ConfigureMobileShadows");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static void RunLightmapBake()
        {
            Debug.Log("[MuseumBatchPipeline] Iniciando bake de lightmaps (Lightmapping.Bake — pode levar minutos)...");
            bool ok = Lightmapping.Bake();
            Debug.Log(ok
                ? "[MuseumBatchPipeline] Bake de lightmaps concluído com sucesso."
                : "[MuseumBatchPipeline] Lightmapping.Bake() retornou falso (bake cancelado ou sem geometria estática marcada).");
        }

        private static void RunOptimization()
        {
            var instance = ScriptableObject.CreateInstance<MuseumOptimizationSetup>();
            try
            {
                Invoke(instance, "ApplyLodGroups");
                RunPrivateStatic<MuseumOptimizationSetup>("BakeOcclusionCulling");
                Invoke(instance, "ScanTextureCompression");
                Invoke(instance, "ApplyAstcToAllTextures");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        // ── Helpers de reflection ───────────────────────────────────────────────

        private const BindingFlags InstanceFlags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
        private const BindingFlags StaticFlags   = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;

        private static void RunPrivateInstance<T>(string methodName) where T : ScriptableObject
        {
            var instance = ScriptableObject.CreateInstance<T>();
            try { Invoke(instance, methodName); }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
        }

        private static void RunPrivateStatic<T>(string methodName)
        {
            MethodInfo m = typeof(T).GetMethod(methodName, StaticFlags);
            if (m == null)
                throw new MissingMethodException(typeof(T).FullName, methodName);
            m.Invoke(null, null);
        }

        private static void Invoke(object instance, string methodName)
        {
            // Alguns métodos "privados" das ferramentas MuseumModerna são instance,
            // outros static — busca nos dois binding sets em vez de assumir um só.
            MethodInfo m = instance.GetType().GetMethod(methodName, InstanceFlags)
                         ?? instance.GetType().GetMethod(methodName, StaticFlags);
            if (m == null)
                throw new MissingMethodException(instance.GetType().FullName, methodName);
            m.Invoke(m.IsStatic ? null : instance, null);
        }

        private static void RunStep(string label, Action action)
        {
            Debug.Log($"[MuseumBatchPipeline] ── {label} ──");
            action.Invoke();
        }
    }
}
