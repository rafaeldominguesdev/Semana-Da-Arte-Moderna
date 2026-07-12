using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MuseumModerna
{
    /// <summary>
    /// Otimizações de performance para Mobile/VR que faltavam depois da Iluminação Avançada
    /// (MuseumAdvancedLightingSetup já cuida de lightmaps, light probes, reflection probe,
    /// distância/cascades de sombra e do subtarget ASTC de build). Este arquivo é aditivo e
    /// cobre só o que faltava:
    ///
    ///  1. LOD Groups nos objetos decorativos (pedestais, bancos, barreiras, molduras de quadro,
    ///     teto caixotado) — construídos com primitivas Unity, então o "LOD" aqui é aproximado:
    ///     visibilidade total → sombra reduzida em peças pequenas → corte total (culled).
    ///  2. Bake de Occlusion Culling (StaticOcclusionCulling.Compute), reaproveitando/checando as
    ///     flags Occluder/OccludeeStatic que MuseumAdvancedLightingSetup já aplica.
    ///  3. Diagnóstico de compressão ASTC (Android) de todas as Texture2D do projeto, com uma
    ///     aplicação em massa opcional (exige confirmação explícita).
    ///
    /// Acesse: MuseumModerna → Otimização Mobile/VR
    ///
    /// ORDEM DE EXECUÇÃO RECOMENDADA:
    ///   1. MuseumModerna → Construir Museu Completo
    ///   2. MuseumModerna → Decoração do Museu  /  Texturas e Materiais PBR   (opcionais)
    ///   3. MuseumModerna → Iluminação Avançada (Bake + Probes)   ← marca Static/Occluder, faz bake de luz
    ///   4. MuseumModerna → Otimização Mobile/VR (ESTE ARQUIVO)   ← rode por ÚLTIMO
    ///
    /// Motivo de rodar por último: os LOD Groups e o bake de Occlusion Culling dependem da
    /// geometria/decoração final da cena e das flags Occluder/OccludeeStatic já setadas.
    /// </summary>
    public class MuseumOptimizationSetup : EditorWindow
    {
        // ─── Parâmetros: LOD ───────────────────────────────────────────────────
        private float _lod0Height           = 0.4f;   // até aqui: detalhe total
        private float _lod1Height           = 0.1f;   // até aqui: objeto ainda visível
        private float _cullHeight           = 0.02f;  // abaixo disso: objeto inteiro cortado (culled)
        private bool  _optimizeSmallShadows = true;
        private float _smallDecorVolumeM3   = 0.02f;

        // ─── Parâmetros: Texturas ──────────────────────────────────────────────
        private readonly List<string> _nonAstcTextures = new List<string>();
        private int  _texturesScanned  = 0;
        private bool _hasScanned       = false;
        private bool _confirmBulkApply = false;

        private Vector2 _scroll;

        // ─── Menu ─────────────────────────────────────────────────────────────

        // Nota: a barra em "Mobile/VR" é escapada com "\/" para aparecer como texto do item
        // (uma barra normal criaria um submenu "Otimização Mobile" → "VR", quebrando o padrão
        // flat dos outros itens em MuseumModerna/*).
        [MenuItem("MuseumModerna/Otimização Mobile\\/VR")]
        public static void ShowWindow()
        {
            GetWindow<MuseumOptimizationSetup>("Otimização Mobile/VR").minSize = new Vector2(430, 640);
        }

        // ─── GUI ──────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            GUIStyle title = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 };
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Otimização Mobile/VR", title);
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                "Complementa a Iluminação Avançada (lightmaps, probes e sombras já ficam lá).\n" +
                "Rode este tool por ÚLTIMO, depois de toda a geometria/decoração estar pronta na cena.",
                MessageType.Info);

            EditorGUILayout.Space(10);

            DrawLodSection();
            EditorGUILayout.Space(14);
            DrawOcclusionSection();
            EditorGUILayout.Space(14);
            DrawTextureSection();

            EditorGUILayout.Space(16);
            EditorGUILayout.EndScrollView();
        }

        // ══════════════════════════════════════════════════════════════════════
        // 1. LOD GROUPS
        // ══════════════════════════════════════════════════════════════════════

        private void DrawLodSection()
        {
            EditorGUILayout.LabelField("1. LOD Groups (objetos decorativos)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Adiciona LODGroup em pedestais, bancos, barreiras, molduras de quadro (PaintingFrame) " +
                "e no teto caixotado (Teto_Museu, se existir na cena aberta). Como esses objetos são " +
                "montados com primitivas (não há malhas high/low-poly separadas), o ganho real vem de:\n" +
                "  • LOD0 — tudo visível, sombras normais.\n" +
                "  • LOD1 — mesmo conjunto ainda visível, screen height mais baixo.\n" +
                "  • Culled — abaixo do limite mínimo, o objeto inteiro some.\n\n" +
                "Limitação real do Unity: o LODGroup não alterna ShadowCastingMode por nível " +
                "dinamicamente (isso não existe na API). Por isso, peças pequenas/finas (abaixo do " +
                "volume-limite) têm a sombra desligada de forma permanente já nesta configuração — elas " +
                "raramente projetam sombra perceptível, e o AO do bake já cobre o contato visual. Peças " +
                "grandes (corpo principal) continuam projetando sombra normalmente em qualquer distância; " +
                "o corte de LOD nelas acontece só no nível Culled.",
                MessageType.None);

            _lod0Height = EditorGUILayout.Slider("LOD0 até (screen height)", _lod0Height, 0.05f, 0.9f);
            _lod1Height = EditorGUILayout.Slider("LOD1 até (screen height)", _lod1Height, 0.01f, _lod0Height - 0.01f);
            _cullHeight = EditorGUILayout.Slider("Cull abaixo de (screen height)", _cullHeight, 0.001f, _lod1Height - 0.001f);

            EditorGUILayout.Space(4);
            _optimizeSmallShadows = EditorGUILayout.Toggle("Desligar sombra de peças pequenas", _optimizeSmallShadows);
            using (new EditorGUI.DisabledScope(!_optimizeSmallShadows))
            {
                _smallDecorVolumeM3 = EditorGUILayout.FloatField("Volume-limite peça pequena (m³)", _smallDecorVolumeM3);
            }

            EditorGUILayout.Space(6);
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("Aplicar LOD Groups", GUILayout.Height(36)))
                ApplyLodGroups();
            GUI.backgroundColor = Color.white;
        }

        private void ApplyLodGroups()
        {
            // Garante ordem estritamente decrescente exigida pelo LODGroup, mesmo que os sliders
            // (que dependem uns dos outros) tenham ficado momentaneamente inconsistentes.
            float lod0 = Mathf.Clamp01(_lod0Height);
            float lod1 = Mathf.Clamp(_lod1Height, 0.0001f, lod0 - 0.0001f);
            float cull = Mathf.Clamp(_cullHeight, 0.0001f, lod1 - 0.0001f);

            List<GameObject> targets = FindLodTargets();
            int configured = 0, shadowsOptimized = 0;

            foreach (GameObject root in targets)
            {
                Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0) continue;

                LODGroup group = root.GetComponent<LODGroup>();
                if (group == null) group = Undo.AddComponent<LODGroup>(root);

                if (_optimizeSmallShadows)
                {
                    foreach (Renderer r in renderers)
                    {
                        Vector3 size = r.bounds.size;
                        float volume = size.x * size.y * size.z;
                        if (volume > 0f && volume < _smallDecorVolumeM3 && r.shadowCastingMode != ShadowCastingMode.Off)
                        {
                            Undo.RecordObject(r, "Otimizar Sombra LOD");
                            r.shadowCastingMode = ShadowCastingMode.Off;
                            shadowsOptimized++;
                        }
                    }
                }

                LOD[] lods = new LOD[3];
                lods[0] = new LOD(lod0, renderers);
                lods[1] = new LOD(lod1, renderers);
                lods[2] = new LOD(cull, new Renderer[0]); // nível "Culled": nenhum renderer = objeto some

                Undo.RecordObject(group, "Configurar LOD Group");
                group.SetLODs(lods);
                group.RecalculateBounds();
                group.fadeMode = LODFadeMode.None; // sem cross-fade: mais barato em mobile (evita overdraw duplo)

                configured++;
            }

            Debug.Log($"[MuseumModerna] LOD Groups configurados em {configured} objeto(s) " +
                       $"({shadowsOptimized} renderer(s) com sombra desligada). Alvos varridos: {targets.Count}.");

            if (configured == 0)
                Debug.LogWarning("[MuseumModerna] Nenhum objeto decorativo encontrado. Rode " +
                    "'Decoração do Museu' / 'Construir Museu Completo' antes, ou confira os nomes na " +
                    "Hierarchy (Pedestal*, Banco_*, Barreira*, Teto_Museu, quadros com PaintingFrame).");
        }

        /// <summary>
        /// Varre a cena aberta por objetos decorativos criados pelas outras ferramentas do projeto:
        ///   • Pedestais: "Pedestal" (MuseumDecorationSetup) ou "Pedestal_*" (MuseumCompleteBuilder).
        ///   • Bancos: "Banco_*" (MuseumDecorationSetup).
        ///   • Barreiras: "Barreira" / "Barreira_*" (MuseumDecorationSetup).
        ///   • Teto caixotado: "Teto_Museu" (MuseumCeilingBuilder). Esse builder monta o teto dentro de
        ///     Start(), ou seja, só existe na Hierarchy em Play Mode/build — não em Edit Mode puro. Se o
        ///     objeto não existir ainda, esta varredura simplesmente não encontra nada com esse nome e
        ///     segue sem erro (ver HelpBox da seção 1 / nota no relatório final).
        ///   • Molduras de quadro: qualquer objeto com o componente PaintingFrame.
        /// </summary>
        private static List<GameObject> FindLodTargets()
        {
            var targets = new List<GameObject>();
            var seen = new HashSet<GameObject>();

            void Add(GameObject go)
            {
                if (go != null && seen.Add(go)) targets.Add(go);
            }

            foreach (Transform t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                string n = t.name;
                if (n.StartsWith("Pedestal") || n.StartsWith("Banco_") ||
                    n.StartsWith("Barreira") || n == "Teto_Museu")
                {
                    Add(t.gameObject);
                }
            }

            foreach (PaintingFrame frame in FindObjectsByType<PaintingFrame>(FindObjectsSortMode.None))
                Add(frame.gameObject);

            return targets;
        }

        // ══════════════════════════════════════════════════════════════════════
        // 2. OCCLUSION CULLING (BAKE)
        // ══════════════════════════════════════════════════════════════════════

        private void DrawOcclusionSection()
        {
            EditorGUILayout.LabelField("2. Occlusion Culling (Bake)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Corta o desenho de objetos totalmente escondidos atrás de paredes/geometria — essencial " +
                "em mobile VR para reduzir draw calls. Requer a cena aberta e SALVA. O botão abaixo confere " +
                "(e corrige, se faltar) as flags Occluder/OccludeeStatic na geometria antes do bake.\n\n" +
                "O tempo de bake depende do tamanho/complexidade da cena — pode levar de segundos a vários " +
                "minutos. Salve a cena (Ctrl+S) antes e depois do bake.",
                MessageType.Info);

            EditorGUILayout.Space(6);
            if (GUILayout.Button("Verificar/Corrigir Flags Occluder+Occludee", GUILayout.Height(28)))
                EnsureOcclusionStaticFlags();

            EditorGUILayout.Space(4);
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("Bake Occlusion Culling", GUILayout.Height(36)))
                BakeOcclusionCulling();
            GUI.backgroundColor = Color.white;

            EditorGUILayout.HelpBox(
                "Depois do bake: Window → Rendering → Occlusion Culling → aba 'Visualization' para " +
                "conferir as células e o que é cortado com a câmera do Player em várias posições da sala.",
                MessageType.None);
        }

        private static void BakeOcclusionCulling()
        {
            EnsureOcclusionStaticFlags();

            // API correta no Unity 2022.3: StaticOcclusionCulling.Compute() (equivalente ao botão
            // "Bake" da janela Window > Rendering > Occlusion Culling). Não existe um método chamado
            // "GenerateInEditor" na API pública do UnityEditor — Compute() é o nome real.
            StaticOcclusionCulling.Compute();

            Debug.Log("[MuseumModerna] Bake de Occlusion Culling disparado (StaticOcclusionCulling.Compute). " +
                "Acompanhe a barra de progresso do Editor. Ao concluir, salve a cena (Ctrl+S) e confira em " +
                "Window > Rendering > Occlusion Culling > aba Visualization.");
        }

        /// <summary>
        /// Confere (e corrige, se faltando) as flags OccluderStatic/OccludeeStatic na geometria da sala.
        /// Reaproveita a mesma lógica de MuseumAdvancedLightingSetup.MarkStaticObjects (duplicada aqui de
        /// forma mínima, já que aquela é privada e este arquivo não deve criar acoplamento entre os dois) —
        /// pula Player, luzes e câmeras, e só considera objetos com MeshRenderer/MeshFilter.
        /// </summary>
        private static void EnsureOcclusionStaticFlags()
        {
            const StaticEditorFlags required = StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic;

            GameObject[] all = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            int fixedCount = 0, alreadyOk = 0;

            foreach (GameObject go in all)
            {
                if (IsPlayerObject(go)) continue;
                if (go.GetComponent<Light>() != null) continue;
                if (go.GetComponent<Camera>() != null) continue;
                if (go.GetComponent<MeshRenderer>() == null && go.GetComponent<MeshFilter>() == null) continue;

                StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(go);
                if ((flags & required) == required)
                {
                    alreadyOk++;
                    continue;
                }

                Undo.RecordObject(go, "Marcar Occluder/Occludee");
                GameObjectUtility.SetStaticEditorFlags(go, flags | required);
                fixedCount++;
            }

            Debug.Log($"[MuseumModerna] Occlusion flags: {alreadyOk} objeto(s) já ok, {fixedCount} corrigido(s) agora.");
        }

        private static bool IsPlayerObject(GameObject go)
        {
            string n = go.name.ToLowerInvariant();
            return n.Contains("player") || n.Contains("camera") || n.Contains("jogador");
        }

        // ══════════════════════════════════════════════════════════════════════
        // 3. COMPRESSÃO DE TEXTURA (DIAGNÓSTICO)
        // ══════════════════════════════════════════════════════════════════════

        private void DrawTextureSection()
        {
            EditorGUILayout.LabelField("3. Compressão de Textura (ASTC / Android)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Verifica todas as Texture2D do projeto (Assets/ e pacotes) e reporta quais NÃO têm o " +
                "override ASTC para Android configurado. É só diagnóstico — nada é alterado automaticamente, " +
                "já que texturas de terceiros/pacotes podem não dever ser tocadas.",
                MessageType.Info);

            if (GUILayout.Button("Verificar Compressão ASTC", GUILayout.Height(28)))
                ScanTextureCompression();

            if (_hasScanned)
            {
                EditorGUILayout.Space(6);
                MessageType level = _nonAstcTextures.Count == 0 ? MessageType.Info : MessageType.Warning;
                EditorGUILayout.HelpBox(
                    $"{_texturesScanned} textura(s) verificada(s) · {_nonAstcTextures.Count} sem ASTC no Android.",
                    level);

                if (_nonAstcTextures.Count > 0)
                {
                    int shown = Mathf.Min(_nonAstcTextures.Count, 40);
                    for (int i = 0; i < shown; i++)
                        EditorGUILayout.LabelField("• " + _nonAstcTextures[i], EditorStyles.miniLabel);

                    if (_nonAstcTextures.Count > shown)
                        EditorGUILayout.LabelField($"… e mais {_nonAstcTextures.Count - shown} textura(s).", EditorStyles.miniLabel);

                    EditorGUILayout.Space(10);
                    EditorGUILayout.HelpBox(
                        "Aplicar ASTC em massa sobrescreve o override Android de TODAS as texturas listadas " +
                        "acima (formato ASTC_6x6 — mesmo padrão usado em MuseumTextureSetup.cs). Texturas " +
                        "dentro de Packages/ são ignoradas (não editáveis por este projeto). Confirme que não " +
                        "há texturas de terceiros com necessidades especiais (ex: normal map, UI, lightmap) " +
                        "antes de aplicar — essas podem precisar de formatos/configurações diferentes.",
                        MessageType.Warning);

                    _confirmBulkApply = EditorGUILayout.ToggleLeft(
                        "Entendo o risco e quero aplicar ASTC em massa (inclusive em texturas PBR futuras)",
                        _confirmBulkApply);

                    using (new EditorGUI.DisabledScope(!_confirmBulkApply))
                    {
                        if (GUILayout.Button("Aplicar ASTC a Todas as Texturas do Projeto", GUILayout.Height(30)))
                            ApplyAstcToAllTextures();
                    }
                }
            }
        }

        private void ScanTextureCompression()
        {
            _nonAstcTextures.Clear();
            string[] guids = AssetDatabase.FindAssets("t:Texture2D");
            int total = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                total++;
                TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings("Android");
                bool isAstc = settings.overridden && settings.format.ToString().StartsWith("ASTC");

                if (!isAstc) _nonAstcTextures.Add(path);
            }

            _texturesScanned = total;
            _hasScanned = true;
            _confirmBulkApply = false;

            Debug.Log($"[MuseumModerna] Verificação de textura: {total} textura(s) no projeto, " +
                       $"{_nonAstcTextures.Count} sem ASTC no Android.");
        }

        private void ApplyAstcToAllTextures()
        {
            bool confirmed = Application.isBatchMode || EditorUtility.DisplayDialog(
                "Aplicar ASTC a todas as texturas?",
                $"Isso vai sobrescrever o override Android de {_nonAstcTextures.Count} textura(s) para " +
                "ASTC_6x6. Import settings não entram no Undo padrão do Editor (Ctrl+Z não reverte) — " +
                "revise a lista acima antes de confirmar. Deseja continuar?",
                "Sim, aplicar", "Cancelar");
            if (!confirmed) return;

            int changed = 0, skippedPackages = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (string path in _nonAstcTextures)
                {
                    if (path.StartsWith("Packages/")) { skippedPackages++; continue; }

                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer == null) continue;

                    TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings("Android");
                    settings.overridden         = true;
                    settings.format             = TextureImporterFormat.ASTC_6x6;
                    settings.textureCompression = TextureImporterCompression.Compressed;
                    importer.SetPlatformTextureSettings(settings);
                    importer.SaveAndReimport();
                    changed++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            AssetDatabase.Refresh();

            Debug.Log($"[MuseumModerna] ASTC aplicado a {changed} textura(s) " +
                       $"({skippedPackages} em Packages/ ignorada(s)).");

            _confirmBulkApply = false;
            ScanTextureCompression();
        }
    }
}
