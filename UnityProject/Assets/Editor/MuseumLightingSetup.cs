using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MuseumModerna
{
    /// <summary>
    /// Ferramenta de Editor para iluminação dramática do museu.
    /// Acesse: MuseumModerna → Iluminação Dramática do Museu
    ///
    /// O que faz:
    ///  - Escurece a iluminação ambiente (sala quase no escuro)
    ///  - Adiciona Spotlight apontado para cada quadro (PaintingExhibit)
    ///  - Adiciona CandleFlicker às luzes existentes (Point Lights)
    ///  - Configura névoa atmosférica
    /// </summary>
    public class MuseumLightingSetup : EditorWindow
    {
        // ─── Configurações de Iluminação ──────────────────────────────────────

        private Color ambientColor     = new Color(0.04f, 0.02f, 0.02f); // quase preto, tom quente
        private Color spotColor        = new Color(1f, 0.82f, 0.45f);    // âmbar incandescente
        private float spotIntensity    = 2.2f;
        private float spotRange        = 5f;
        private float spotAngle        = 42f;
        private float spotHeight       = 1.8f;   // metros acima do quadro
        private float spotForward      = 1.2f;   // metros à frente do quadro
        private bool  spotShadows      = true;

        private bool  enableFog        = true;
        private Color fogColor         = new Color(0.06f, 0.04f, 0.04f);
        private float fogDensity       = 0.04f;

        private bool addCandleFlicker  = true;
        private Vector2 _scroll;

        // ─── Menu ─────────────────────────────────────────────────────────────

        [MenuItem("MuseumModerna/Iluminação Dramática do Museu")]
        public static void ShowWindow()
        {
            GetWindow<MuseumLightingSetup>("Museum Lighting").minSize = new Vector2(400, 580);
        }

        // ─── GUI ──────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            GUIStyle title = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 };
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Iluminação Dramática do Museu", title);
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                "Transforma o museu numa experiência sombria e atmosférica:\n" +
                "sala escura com spotlights sobre cada quadro, névoa e velas tremeluzentes.",
                MessageType.Info);

            EditorGUILayout.Space(12);

            // Ambiente
            EditorGUILayout.LabelField("Ambiente", EditorStyles.boldLabel);
            ambientColor  = EditorGUILayout.ColorField("Cor Ambiente (quase preta)", ambientColor);
            enableFog     = EditorGUILayout.Toggle("Névoa Atmosférica", enableFog);
            if (enableFog)
            {
                EditorGUI.indentLevel++;
                fogColor   = EditorGUILayout.ColorField("Cor da Névoa", fogColor);
                fogDensity = EditorGUILayout.Slider("Densidade", fogDensity, 0.005f, 0.15f);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(8);

            // Spotlights
            EditorGUILayout.LabelField("Spotlights nos Quadros", EditorStyles.boldLabel);
            spotColor     = EditorGUILayout.ColorField("Cor do Spot", spotColor);
            spotIntensity = EditorGUILayout.Slider("Intensidade", spotIntensity, 0.5f, 6f);
            spotRange     = EditorGUILayout.Slider("Alcance (m)", spotRange, 2f, 10f);
            spotAngle     = EditorGUILayout.Slider("Ângulo (°)", spotAngle, 20f, 80f);
            spotHeight    = EditorGUILayout.Slider("Altura acima do quadro (m)", spotHeight, 0.5f, 3f);
            spotForward   = EditorGUILayout.Slider("Distância à frente (m)", spotForward, 0.3f, 2.5f);
            spotShadows   = EditorGUILayout.Toggle("Sombras Suaves", spotShadows);

            EditorGUILayout.Space(8);

            // Velas
            addCandleFlicker = EditorGUILayout.Toggle("CandleFlicker nas Point Lights", addCandleFlicker);

            EditorGUILayout.Space(16);

            // Botões
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("Aplicar Tudo", GUILayout.Height(42)))
                ApplyAll();
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Só Ambiente + Névoa"))  ApplyAmbient();
            if (GUILayout.Button("Só Spotlights"))        ApplySpotlights();
            if (GUILayout.Button("Só CandleFlicker"))     ApplyCandleFlicker();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(12);
            EditorGUILayout.HelpBox(
                "Dica: ajuste os valores acima e clique 'Aplicar Tudo' para ver o resultado.\n" +
                "Salve a cena (Ctrl+S) depois de aplicar.\n\n" +
                "Para deletar os spotlights gerados: busque 'Spotlight_' na hierarquia e delete.",
                MessageType.None);

            EditorGUILayout.EndScrollView();
        }

        // ─── Aplicar Tudo ─────────────────────────────────────────────────────

        private void ApplyAll()
        {
            ApplyAmbient();
            ApplySpotlights();
            if (addCandleFlicker) ApplyCandleFlicker();
            Debug.Log("[MuseumModerna] Iluminação dramática aplicada. Salve a cena (Ctrl+S).");
        }

        // ─── Ambiente e Névoa ─────────────────────────────────────────────────

        private void ApplyAmbient()
        {
            RenderSettings.ambientMode  = AmbientMode.Flat;
            RenderSettings.ambientLight = ambientColor;

            if (enableFog)
            {
                RenderSettings.fog         = true;
                RenderSettings.fogMode     = FogMode.ExponentialSquared;
                RenderSettings.fogColor    = fogColor;
                RenderSettings.fogDensity  = fogDensity;
            }
            else
            {
                RenderSettings.fog = false;
            }

            // Apaga a Directional Light padrão se ainda existir (clareia tudo)
            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (Light l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    l.intensity = 0.05f; // quase apagada em vez de deletar
                    Undo.RecordObject(l, "Dim Directional Light");
                    Debug.Log($"[MuseumModerna] Luz direcional '{l.gameObject.name}' escurecida.");
                }
            }

            Debug.Log("[MuseumModerna] Ambiente escurecido. Névoa: " + (enableFog ? "ativada" : "desativada"));
        }

        // ─── Spotlights nos Quadros ───────────────────────────────────────────

        private void ApplySpotlights()
        {
            PaintingExhibit[] exhibits = FindObjectsByType<PaintingExhibit>(FindObjectsSortMode.None);

            if (exhibits.Length == 0)
            {
                Debug.LogWarning("[MuseumModerna] Nenhum PaintingExhibit encontrado na cena.");
                return;
            }

            int created = 0;
            foreach (PaintingExhibit exhibit in exhibits)
            {
                string spotName = $"Spotlight_{exhibit.gameObject.name}";

                // Remove spotlight anterior se já existia
                Transform existingSpot = exhibit.transform.Find(spotName);
                if (existingSpot != null)
                    Undo.DestroyObjectImmediate(existingSpot.gameObject);

                // Cria novo GameObject filho
                GameObject spotGO = new GameObject(spotName);
                Undo.RegisterCreatedObjectUndo(spotGO, "Create Painting Spotlight");
                spotGO.transform.SetParent(exhibit.transform);

                // Posiciona: acima + à frente do quadro
                // forward do quadro = direção que o visitante vê (para frente da parede)
                Vector3 localPos = new Vector3(0f, spotHeight, spotForward);
                spotGO.transform.localPosition = localPos;

                // Aponta para o centro do quadro
                Vector3 worldPaintingCenter = exhibit.transform.position;
                spotGO.transform.LookAt(worldPaintingCenter);

                // Configura a luz
                Light light = Undo.AddComponent<Light>(spotGO);
                light.type      = LightType.Spot;
                light.color     = spotColor;
                light.intensity = spotIntensity;
                light.range     = spotRange;
                light.spotAngle = spotAngle;
                light.shadows   = spotShadows ? LightShadows.Soft : LightShadows.None;
                light.shadowStrength = 0.7f;

                created++;
            }

            Debug.Log($"[MuseumModerna] {created} spotlights criados nos quadros.");
        }

        // ─── CandleFlicker ────────────────────────────────────────────────────

        private void ApplyCandleFlicker()
        {
            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            int added = 0;

            foreach (Light l in lights)
            {
                if (l.type != LightType.Point && l.type != LightType.Spot) continue;
                // Não adiciona nos spotlights dos quadros (já oscilam por shader)
                if (l.gameObject.name.StartsWith("Spotlight_")) continue;

                if (l.GetComponent<CandleFlicker>() == null)
                {
                    Undo.AddComponent<CandleFlicker>(l.gameObject);
                    added++;
                }
            }

            Debug.Log($"[MuseumModerna] CandleFlicker adicionado a {added} luzes.");
        }
    }
}
