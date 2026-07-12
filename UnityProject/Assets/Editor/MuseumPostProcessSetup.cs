using UnityEditor;
using UnityEngine;

#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif

namespace MuseumModerna
{
    /// <summary>
    /// Configura o Post Processing Stack v2 com perfil atmosférico para o museu.
    /// Acesse: MuseumModerna → Configurar Pós-processamento
    ///
    /// Requer: Post Processing Stack v2 (já adicionado ao Packages/manifest.json).
    /// Após abrir o Unity, o package será baixado automaticamente.
    ///
    /// Efeitos aplicados:
    ///  - Bloom: aureola quente nas luzes
    ///  - Vignette: bordas escurecidas (foco no centro)
    ///  - Color Grading: tons sépia/âmbar de 1922
    ///  - Grain: granulação de filme antigo
    /// </summary>
    public class MuseumPostProcessSetup : EditorWindow
    {
        [MenuItem("MuseumModerna/Configurar Pós-processamento")]
        public static void ShowWindow()
        {
            GetWindow<MuseumPostProcessSetup>("Post Process Setup").minSize = new Vector2(380, 420);
        }

        private void OnGUI()
        {
            GUIStyle title = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 };
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Pós-processamento Atmosférico", title);
            EditorGUILayout.Space(4);

#if UNITY_POST_PROCESSING_STACK_V2
            EditorGUILayout.HelpBox(
                "Post Processing Stack v2 detectado!\n\n" +
                "Clique no botão abaixo para:\n" +
                "  • Criar perfil com Bloom + Vignette + Color Grading + Grain\n" +
                "  • Adicionar PostProcessLayer na câmera\n" +
                "  • Criar PostProcessVolume global na cena",
                MessageType.Info);

            EditorGUILayout.Space(12);

            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("Aplicar Perfil Atmosférico (1922)", GUILayout.Height(44)))
                ApplyPostProcess();
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(12);
            DrawEffectsPreview();

#else
            EditorGUILayout.HelpBox(
                "Post Processing Stack v2 ainda não está instalado.\n\n" +
                "O package 'com.unity.postprocessing' foi adicionado ao manifest.json.\n" +
                "Feche e reabra o Unity (ou aguarde a compilação terminar).\n" +
                "Depois volte aqui e clique em 'Aplicar'.",
                MessageType.Warning);

            EditorGUILayout.Space(8);
            if (GUILayout.Button("Abrir Package Manager"))
                EditorApplication.ExecuteMenuItem("Window/Package Manager");
#endif
        }

#if UNITY_POST_PROCESSING_STACK_V2
        private static void ApplyPostProcess()
        {
            // ── 1. Cria o perfil ──────────────────────────────────────────────
            PostProcessProfile profile = ScriptableObject.CreateInstance<PostProcessProfile>();

            // Bloom — aureola quente nas lâmpadas e quadros iluminados
            var bloom = profile.AddSettings<Bloom>();
            bloom.enabled.Override(true);
            bloom.intensity.Override(1.1f);
            bloom.threshold.Override(0.85f);
            bloom.softKnee.Override(0.6f);
            bloom.color.Override(new Color(1f, 0.9f, 0.7f)); // brilho âmbar

            // Vignette — bordas escurecidas, isola o olhar no centro
            var vignette = profile.AddSettings<Vignette>();
            vignette.enabled.Override(true);
            vignette.mode.Override(VignetteMode.Classic);
            vignette.intensity.Override(0.42f);
            vignette.smoothness.Override(0.35f);
            vignette.roundness.Override(1f);
            vignette.rounded.Override(true);

            // Color Grading — temperatura de 1922: sépia, baixa saturação, alto contraste
            var cg = profile.AddSettings<ColorGrading>();
            cg.enabled.Override(true);
            cg.temperature.Override(14f);       // tons quentes
            cg.saturation.Override(-28f);       // levemente dessaturado (look fotográfico antigo)
            cg.contrast.Override(22f);          // alto contraste
            cg.postExposure.Override(0.15f);    // leve compensação de exposição

            // Grain — granulação de filme fotográfico antigo
            var grain = profile.AddSettings<Grain>();
            grain.enabled.Override(true);
            grain.intensity.Override(0.13f);
            grain.size.Override(1.1f);
            grain.colored.Override(false);      // grão monocromático = mais clássico

            // Ambient Occlusion — sombras de contato suaves nos cantos e embaixo de objetos
            var ao = profile.AddSettings<AmbientOcclusion>();
            ao.enabled.Override(true);
            ao.intensity.Override(0.5f);
            ao.radius.Override(0.3f);
            ao.mode.Override(AmbientOcclusionMode.ScalableAmbientObscurance); // SAO: melhor performance em mobile
            ao.quality.Override(AmbientOcclusionQuality.Medium);

            // ── 2. Salva o perfil ─────────────────────────────────────────────
            const string profilePath = "Assets/MuseumPostProcessProfile.asset";
            AssetDatabase.CreateAsset(profile, profilePath);
            AssetDatabase.SaveAssets();

            // ── 3. PostProcessLayer na câmera ─────────────────────────────────
            Camera cam = Camera.main;
            if (cam == null)
            {
                // Fallback: pega qualquer câmera na cena
                cam = FindAnyObjectByType<Camera>();
            }

            if (cam != null)
            {
                PostProcessLayer layer = cam.GetComponent<PostProcessLayer>();
                if (layer == null)
                    layer = Undo.AddComponent<PostProcessLayer>(cam.gameObject);

                // "Everything" garante que o volume global seja aplicado sem configurar layers
                layer.volumeLayer = ~0; // LayerMask.GetMask("Everything")
                layer.antialiasingMode = PostProcessLayer.Antialiasing.FastApproximateAntialiasing;
                Debug.Log($"[MuseumModerna] PostProcessLayer adicionado à câmera '{cam.gameObject.name}'.");
            }
            else
            {
                Debug.LogWarning("[MuseumModerna] Câmera não encontrada! Adicione PostProcessLayer manualmente.");
            }

            // ── 4. PostProcessVolume global na cena ───────────────────────────
            // Remove volume anterior se já existia
            PostProcessVolume[] existing = FindObjectsByType<PostProcessVolume>(FindObjectsSortMode.None);
            foreach (var v in existing)
            {
                if (v.isGlobal)
                {
                    Undo.DestroyObjectImmediate(v.gameObject);
                    break;
                }
            }

            GameObject volGO = new GameObject("Post Process Volume (Museum)");
            Undo.RegisterCreatedObjectUndo(volGO, "Create PostProcess Volume");
            PostProcessVolume vol = volGO.AddComponent<PostProcessVolume>();
            vol.isGlobal = true;
            vol.profile  = AssetDatabase.LoadAssetAtPath<PostProcessProfile>(profilePath);
            vol.priority = 1f;

            Debug.Log("[MuseumModerna] Pós-processamento configurado!");

            if (!Application.isBatchMode)
                EditorUtility.DisplayDialog(
                    "Pós-processamento Aplicado",
                    "Perfil criado: Assets/MuseumPostProcessProfile.asset\n\n" +
                    "Efeitos ativos:\n" +
                    "  ✓ Bloom (aureola âmbar)\n" +
                    "  ✓ Vignette (bordas escuras)\n" +
                    "  ✓ Color Grading (sépia 1922)\n" +
                    "  ✓ Grain (granulação de filme)\n\n" +
                    "Salve a cena com Ctrl+S.",
                    "OK");
        }

        private void DrawEffectsPreview()
        {
            EditorGUILayout.LabelField("Efeitos que serão aplicados:", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Bloom: aureola quente nas lâmpadas e nos quadros iluminados\n\n" +
                "Vignette: bordas da tela ficam escuras, concentrando o olhar no centro\n\n" +
                "Color Grading: temperatura quente + dessaturação + alto contraste,\n" +
                "                 como fotografia dos anos 1920\n\n" +
                "Grain: granulação de filme em preto e branco, estética vintage\n\n" +
                "Ambient Occlusion (SAO): sombras de contato suaves nos cantos,\n" +
                "                          embaixo de objetos, profundidade realista",
                MessageType.None);
        }
#endif
    }
}
