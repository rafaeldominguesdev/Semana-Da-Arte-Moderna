using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MuseumModerna
{
    /// <summary>
    /// Configuração avançada de iluminação do museu:
    ///  - Baked Lighting otimizado para mobile
    ///  - Light Probes em grade (para objetos dinâmicos receberem luz correta)
    ///  - Reflection Probe global
    ///  - Qualidade de sombras ajustada para mobile 60fps
    ///  - Objetos estáticos marcados como Contributors GI
    ///
    /// Acesse: MuseumModerna → Iluminação Avançada (Bake + Probes)
    ///
    /// FLUXO CORRETO:
    ///   1. Execute "Configurar Tudo"
    ///   2. Vá em Window > Rendering > Lighting
    ///   3. Clique "Generate Lighting" (bake pode demorar alguns minutos)
    ///   4. Resultado: iluminação de alta qualidade sem custo em runtime
    /// </summary>
    public class MuseumAdvancedLightingSetup : EditorWindow
    {
        // ─── Parâmetros ───────────────────────────────────────────────────────

        private float probeGridSpacingX = 2f;
        private float probeGridSpacingZ = 2f;
        private float probeHeightLow    = 0.5f;
        private float probeHeightMid    = 1.5f;
        private float probeHeightHigh   = 2.4f;
        private float roomWidth         = 10f;
        private float roomDepth         = 10f;
        private float roomCenterY       = 1.5f;
        private bool  markSceneStatic   = true;
        private bool  keepPlayerDynamic = true;
        private int   lightmapAtlasSize = 1024;

        private Vector2 _scroll;

        // ─── Menu ─────────────────────────────────────────────────────────────

        [MenuItem("MuseumModerna/Iluminação Avançada (Bake + Probes)")]
        public static void ShowWindow()
        {
            GetWindow<MuseumAdvancedLightingSetup>("Advanced Lighting").minSize = new Vector2(400, 600);
        }

        // ─── GUI ──────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            GUIStyle title = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 };
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Iluminação Avançada para Mobile VR", title);
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                "Baked lighting = melhor qualidade visual com custo ZERO em runtime.\n" +
                "Ideal para mobile: sombras suaves, AO e reflexos pré-calculados.",
                MessageType.Info);

            EditorGUILayout.Space(10);

            // ── Seção: Static Objects ──
            EditorGUILayout.LabelField("1. Objetos Estáticos", EditorStyles.boldLabel);
            markSceneStatic   = EditorGUILayout.Toggle("Marcar geometria da sala como Static", markSceneStatic);
            keepPlayerDynamic = EditorGUILayout.Toggle("Manter Player como dinâmico", keepPlayerDynamic);

            EditorGUILayout.Space(8);

            // ── Seção: Lightmap ──
            EditorGUILayout.LabelField("2. Configurações de Lightmap", EditorStyles.boldLabel);
            lightmapAtlasSize = EditorGUILayout.IntPopup("Resolução do Atlas",
                lightmapAtlasSize, new[] { "512 (baixa)", "1024 (media)", "2048 (alta)" }, new[] { 512, 1024, 2048 });
            EditorGUILayout.HelpBox(
                "1024 é o equilíbrio certo para mobile.\n" +
                "2048 dá mais detalhe mas usa mais memória.",
                MessageType.None);

            EditorGUILayout.Space(8);

            // ── Seção: Light Probes ──
            EditorGUILayout.LabelField("3. Light Probes (grid automático)", EditorStyles.boldLabel);
            probeGridSpacingX = EditorGUILayout.Slider("Espaçamento X (m)", probeGridSpacingX, 1f, 4f);
            probeGridSpacingZ = EditorGUILayout.Slider("Espaçamento Z (m)", probeGridSpacingZ, 1f, 4f);
            roomWidth  = EditorGUILayout.FloatField("Largura da sala (m)", roomWidth);
            roomDepth  = EditorGUILayout.FloatField("Profundidade da sala (m)", roomDepth);
            EditorGUILayout.HelpBox(
                "Light Probes interpolam a iluminação baked para objetos dinâmicos (Player).\n" +
                "Sem eles, o Player parece iluminado de forma genérica.",
                MessageType.None);

            EditorGUILayout.Space(8);

            // ── Seção: Shadows ──
            EditorGUILayout.LabelField("4. Sombras Otimizadas para Mobile", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Configurações aplicadas:\n" +
                "  • Shadow Distance: 15m (suficiente para sala de museu)\n" +
                "  • Shadow Cascades: 2 (1/4 do custo vs. 4 cascades)\n" +
                "  • Shadow Resolution: Medium\n" +
                "  • Lights → Mode: Mixed (baked indirect + realtime direct)",
                MessageType.None);

            EditorGUILayout.Space(14);

            // ── Botões ──
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("Configurar Tudo + Instruções de Bake", GUILayout.Height(44)))
                RunFullSetup();
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Só Static Objects"))    MarkStaticObjects();
            if (GUILayout.Button("Só Light Probes"))      PlaceLightProbes();
            if (GUILayout.Button("Só Reflection Probe"))  AddReflectionProbe();
            if (GUILayout.Button("Só Sombras Mobile"))    ConfigureMobileShadows();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(14);
            DrawBakeInstructions();

            EditorGUILayout.EndScrollView();
        }

        // ─── Full Setup ───────────────────────────────────────────────────────

        private void RunFullSetup()
        {
            MarkStaticObjects();
            ConfigureLightmapSettings();
            ConfigureLightsMixed();
            PlaceLightProbes();
            AddReflectionProbe();
            ConfigureMobileShadows();

            if (!Application.isBatchMode)
                EditorUtility.DisplayDialog(
                    "Configuração Avançada Aplicada",
                    "Próximo passo obrigatório:\n\n" +
                    "1. Abra Window > Rendering > Lighting\n" +
                    "2. Aba 'Scene' → confirme que Lighting Mode = Subtractive\n" +
                    "3. Clique 'Generate Lighting' no canto inferior direito\n" +
                    "4. Aguarde o bake (pode levar 2–10 minutos)\n\n" +
                    "Resultado: iluminação cinematográfica sem custo de GPU em runtime!",
                    "Entendido");
        }

        // ─── Static Objects ───────────────────────────────────────────────────

        private void MarkStaticObjects()
        {
            StaticEditorFlags staticFlags = StaticEditorFlags.ContributeGI
                                          | StaticEditorFlags.OccluderStatic
                                          | StaticEditorFlags.OccludeeStatic
                                          | StaticEditorFlags.BatchingStatic;

            GameObject[] all = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            int markedCount = 0;

            foreach (GameObject go in all)
            {
                // Pula o Player e seus filhos
                if (keepPlayerDynamic && IsPlayerObject(go)) continue;
                // Pula luzes e câmeras (devem ser dinâmicas)
                if (go.GetComponent<Light>() != null) continue;
                if (go.GetComponent<Camera>() != null) continue;

                if (go.GetComponent<MeshRenderer>() != null ||
                    go.GetComponent<MeshFilter>()   != null)
                {
                    Undo.RecordObject(go, "Mark Static GI");
                    GameObjectUtility.SetStaticEditorFlags(go, staticFlags);
                    markedCount++;
                }
            }

            Debug.Log($"[MuseumModerna] {markedCount} objetos marcados como Static GI.");
        }

        private static bool IsPlayerObject(GameObject go)
        {
            string n = go.name.ToLowerInvariant();
            return n.Contains("player") || n.Contains("camera") || n.Contains("jogador");
        }

        // ─── Lightmap Settings ────────────────────────────────────────────────

        private void ConfigureLightmapSettings()
        {
            // Cria ou obtém o LightingSettings da cena atual (API correta no Unity 2022).
            // Nota: se a cena ainda não tem um LightingSettings associado, o getter
            // Lightmapping.lightingSettings LANÇA EXCEÇÃO em vez de retornar null
            // ("Lightmapping.lightingSettings is null...") — por isso o try/catch abaixo.
            LightingSettings lightingSettings;
            try { lightingSettings = Lightmapping.lightingSettings; }
            catch (System.Exception) { lightingSettings = null; }

            if (lightingSettings == null)
            {
                const string settingsPath = "Assets/MuseumLightingSettings.asset";
                lightingSettings = AssetDatabase.LoadAssetAtPath<LightingSettings>(settingsPath);
                if (lightingSettings == null)
                {
                    lightingSettings = new LightingSettings();
                    AssetDatabase.CreateAsset(lightingSettings, settingsPath);
                }
                Lightmapping.lightingSettings = lightingSettings;
            }

            lightingSettings.lightmapResolution  = 20f;
            lightingSettings.lightmapMaxSize      = lightmapAtlasSize;
            lightingSettings.ao                   = true;
            lightingSettings.aoMaxDistance        = 0.5f;
            lightingSettings.aoExponentDirect     = 0f;
            lightingSettings.aoExponentIndirect   = 1.2f;
            lightingSettings.indirectResolution   = 2f;
            lightingSettings.realtimeGI           = false;
            lightingSettings.bakedGI              = true;

            Debug.Log($"[MuseumModerna] Lightmap configurado: atlas {lightmapAtlasSize}px, AO habilitado.");
        }

        // ─── Luzes Mixed ──────────────────────────────────────────────────────

        private static void ConfigureLightsMixed()
        {
            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            int converted = 0;

            foreach (Light l in lights)
            {
                if (l.type == LightType.Directional) continue; // Directional fica Realtime

                Undo.RecordObject(l, "Set Light Mixed");
                l.lightmapBakeType = LightmapBakeType.Mixed;
                l.shadows = LightShadows.Soft;
                converted++;
            }

            Debug.Log($"[MuseumModerna] {converted} luz(es) configuradas como Mixed.");
        }

        // ─── Light Probes ─────────────────────────────────────────────────────

        private void PlaceLightProbes()
        {
            // Remove LightProbeGroup anterior
            LightProbeGroup existing = FindAnyObjectByType<LightProbeGroup>();
            if (existing != null)
                Undo.DestroyObjectImmediate(existing.gameObject);

            GameObject lpGO = new GameObject("Light Probe Group (Museum)");
            Undo.RegisterCreatedObjectUndo(lpGO, "Create Light Probes");
            LightProbeGroup lpg = lpGO.AddComponent<LightProbeGroup>();

            List<Vector3> positions = new List<Vector3>();

            float halfW = roomWidth  * 0.5f;
            float halfD = roomDepth  * 0.5f;
            float[] heights = { roomCenterY - 1f, probeHeightLow, probeHeightMid, probeHeightHigh };

            for (float x = -halfW; x <= halfW; x += probeGridSpacingX)
            {
                for (float z = -halfD; z <= halfD; z += probeGridSpacingZ)
                {
                    foreach (float y in heights)
                        positions.Add(new Vector3(x, y, z));
                }
            }

            lpg.probePositions = positions.ToArray();
            Debug.Log($"[MuseumModerna] {positions.Count} Light Probes posicionados em grade {roomWidth}x{roomDepth}m.");
        }

        // ─── Reflection Probe ─────────────────────────────────────────────────

        private static void AddReflectionProbe()
        {
            // Remove reflection probe anterior se existir
            ReflectionProbe[] existing = FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None);
            foreach (var rp in existing) Undo.DestroyObjectImmediate(rp.gameObject);

            GameObject rpGO = new GameObject("Reflection Probe (Museum)");
            Undo.RegisterCreatedObjectUndo(rpGO, "Create Reflection Probe");
            rpGO.transform.position = new Vector3(0, 1.5f, 0);

            ReflectionProbe probe = rpGO.AddComponent<ReflectionProbe>();
            probe.mode         = ReflectionProbeMode.Baked;
            probe.resolution   = 128;  // 128 é suficiente para mobile e poucos polígonos
            probe.size         = new Vector3(30f, 10f, 30f);
            probe.center       = Vector3.zero;
            probe.clearFlags   = ReflectionProbeClearFlags.SolidColor;
            probe.backgroundColor = new Color(0.05f, 0.04f, 0.04f);
            probe.intensity    = 0.6f;
            probe.importance   = 1;

            Debug.Log("[MuseumModerna] Reflection Probe adicionado (modo Baked, 128px para mobile).");
        }

        // ─── Sombras Mobile ───────────────────────────────────────────────────

        private static void ConfigureMobileShadows()
        {
            // Estas configurações aplicam ao Quality Level atual
            QualitySettings.shadows          = ShadowQuality.All;
            QualitySettings.shadowResolution  = ShadowResolution.Medium;
            QualitySettings.shadowDistance    = 15f;
            QualitySettings.shadowCascades    = 2;
            QualitySettings.shadowProjection  = ShadowProjection.CloseFit;
            QualitySettings.shadowNearPlaneOffset = 2f;

            // Compression de texturas: ASTC é o formato otimizado para Android moderno
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;

            Debug.Log("[MuseumModerna] Sombras mobile configuradas: 2 cascades, distance 15m, ASTC.");
        }

        // ─── Instruções ───────────────────────────────────────────────────────

        private static void DrawBakeInstructions()
        {
            EditorGUILayout.LabelField("Como fazer o Bake", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "PASSO A PASSO DO LIGHTMAP BAKE:\n\n" +
                "1. Execute 'Configurar Tudo' acima\n\n" +
                "2. Window → Rendering → Lighting\n" +
                "   • Lighting Mode: Subtractive  ← melhor para mobile\n" +
                "   • Albedo Boost: 1\n" +
                "   • Indirect Intensity: 1\n\n" +
                "3. Selecione cada luz Spot dos quadros:\n" +
                "   • Light → Mode: Mixed\n" +
                "   • Shadows: Soft\n\n" +
                "4. Clique 'Generate Lighting' (canto inferior direito)\n" +
                "   • Aguarde 2–10 minutos\n\n" +
                "5. Salve a cena (Ctrl+S)\n\n" +
                "OCCLUSION CULLING (corta o que não é visto):\n" +
                "Window → Rendering → Occlusion Culling → Bake",
                MessageType.None);
        }
    }
}
