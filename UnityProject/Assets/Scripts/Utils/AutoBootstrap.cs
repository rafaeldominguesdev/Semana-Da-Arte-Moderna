using UnityEngine;
using UnityEngine.Rendering;

namespace MuseumModerna
{
    /// <summary>
    /// Inicialização automática — roda ao abrir o app SEM precisar estar na cena.
    /// Usa [RuntimeInitializeOnLoadMethod] para injetar tudo após a cena carregar.
    ///
    /// Corrige automaticamente:
    ///   1. Normais invertidas do modelo Blender (parede preta vista de dentro)
    ///   2. Materiais pretos sem cor definida
    ///   3. Iluminação ambiente ausente ou fraca
    ///   4. Luz direcional fraca ou ausente
    ///   5. Configurações da câmera (clip planes, clear color)
    ///   6. Adiciona MuseumCeilingBuilder e PlayerSpawnAnchor se não estiverem na cena
    /// </summary>
    public static class AutoBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            Debug.Log("[MuseumModerna] AutoBootstrap: corrigindo cena...");
            FixLighting();
            FixCamera();
            FixAllMaterials();
            AddMissingComponents();
            Debug.Log("[MuseumModerna] AutoBootstrap: pronto.");
        }

        // ─── 1. Iluminação ────────────────────────────────────────────────────

        private static void FixLighting()
        {
            // Ambiente quente e visível — suficiente para ver a sala inteira
            RenderSettings.ambientMode  = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.38f, 0.32f, 0.26f);
            RenderSettings.fog          = false; // desativa névoa que poderia escurecer tudo

            // Garante que existe pelo menos uma luz direcional com boa intensidade
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            bool hasDir = false;

            foreach (Light l in lights)
            {
                if (l.type != LightType.Directional) continue;
                l.intensity = 1.0f;
                l.color     = new Color(1f, 0.96f, 0.88f); // branco levemente quente
                l.shadows   = LightShadows.Soft;
                hasDir      = true;
            }

            if (!hasDir)
            {
                GameObject lg  = new GameObject("DirectionalLight_Auto");
                Light       l  = lg.AddComponent<Light>();
                l.type         = LightType.Directional;
                l.intensity    = 1.0f;
                l.color        = new Color(1f, 0.96f, 0.88f);
                l.shadows      = LightShadows.Soft;
                lg.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                Debug.Log("[MuseumModerna] Luz direcional criada automaticamente.");
            }
        }

        // ─── 2. Câmera ────────────────────────────────────────────────────────

        private static void FixCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                cam = Object.FindAnyObjectByType<Camera>();
                if (cam == null) return;
            }

            cam.nearClipPlane  = 0.01f;          // evita clipping perto
            cam.farClipPlane   = 200f;            // enxerga sala toda
            cam.clearFlags     = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.04f, 0.04f); // fundo escuro quando não há geometria
            cam.fieldOfView    = 80f;             // FOV bom para VR no celular
        }

        // ─── 3. Materiais ─────────────────────────────────────────────────────

        private static void FixAllMaterials()
        {
            Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            int fixed_count = 0;

            foreach (Renderer r in renderers)
            {
                if (r is CanvasRenderer) continue;
                if (r.gameObject.name.StartsWith("Teto_"))  continue; // pula o teto que criamos
                if (r.gameObject.name.StartsWith("Piso_"))  continue;

                Material[] mats = r.materials;
                bool changed = false;

                foreach (Material mat in mats)
                {
                    if (mat == null) continue;

                    // ── FIX PRINCIPAL: Cull Off ──────────────────────────────
                    // Modelos de sala do Blender têm normais apontando para fora.
                    // Quando a câmera está DENTRO da sala, vê as faces traseiras,
                    // que o shader padrão descarta (Cull Back). Resultado: tudo preto.
                    // Cull Off = renderiza dos dois lados → sala visível de dentro.
                    if (mat.HasProperty("_Cull"))
                    {
                        mat.SetInt("_Cull", 0); // 0 = Off
                        changed = true;
                    }

                    // ── FIX: Material preto sem cor ──────────────────────────
                    Color c = mat.color;
                    bool isBlack = (c.r + c.g + c.b < 0.12f) && c.a > 0.5f;
                    if (isBlack)
                    {
                        mat.color = new Color(0.80f, 0.76f, 0.70f); // off-white quente
                        changed = true;
                    }

                    // ── FIX: Smoothness absurda (espelho) ────────────────────
                    if (mat.HasProperty("_Glossiness"))
                    {
                        float g = mat.GetFloat("_Glossiness");
                        if (g > 0.85f)
                        {
                            mat.SetFloat("_Glossiness", 0.35f);
                            changed = true;
                        }
                    }

                    // ── FIX: Metallic exagerado ──────────────────────────────
                    if (mat.HasProperty("_Metallic"))
                    {
                        float m = mat.GetFloat("_Metallic");
                        if (m > 0.7f)
                        {
                            mat.SetFloat("_Metallic", 0.1f);
                            changed = true;
                        }
                    }
                }

                if (changed) fixed_count++;
            }

            Debug.Log($"[MuseumModerna] Materiais corrigidos em {fixed_count} Renderers.");
        }

        // ─── 4. Componentes ausentes ──────────────────────────────────────────

        private static void AddMissingComponents()
        {
            // Teto de museu
            if (Object.FindAnyObjectByType<MuseumCeilingBuilder>() == null)
            {
                new GameObject("MuseumCeilingBuilder_Auto")
                    .AddComponent<MuseumCeilingBuilder>();
                Debug.Log("[MuseumModerna] MuseumCeilingBuilder adicionado automaticamente.");
            }

            // Âncora de spawn (anti-queda)
            if (Object.FindAnyObjectByType<PlayerSpawnAnchor>() == null)
            {
                new GameObject("PlayerSpawnAnchor_Auto")
                    .AddComponent<PlayerSpawnAnchor>();
            }

            // SaveSystem
            if (Object.FindAnyObjectByType<SaveSystem>() == null)
            {
                new GameObject("SaveSystem_Auto")
                    .AddComponent<SaveSystem>();
            }
        }
    }
}
