using UnityEngine;

namespace MuseumModerna
{
    /// <summary>
    /// Gera uma moldura 3D com profundidade ao redor do quadro usando cubos Unity.
    /// Funciona automaticamente no Start ou pode ser acionado via Inspector
    /// com botão direito → "Construir Moldura".
    ///
    /// Como usar:
    ///   1. Adicione ao GameObject do quadro (que deve ter um Renderer).
    ///   2. Ajuste frameThickness e frameDepth no Inspector.
    ///   3. Opcionalmente arraste um Material de moldura (ex: dourado, madeira escura).
    ///   4. Clique direito no componente → "Construir Moldura" para preview no Editor.
    ///
    /// Material sugerido: shader Standard, Metallic 0.6, Smoothness 0.7,
    ///   cor RGB (90, 65, 20) = dourado antigo.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class PaintingFrame : MonoBehaviour
    {
        [Header("Dimensões")]
        [Tooltip("Espessura da moldura em metros")]
        [SerializeField] private float frameThickness = 0.07f;

        [Tooltip("Profundidade (relevo) da moldura em metros")]
        [SerializeField] private float frameDepth = 0.05f;

        [Tooltip("Recuo da moldura para a frente do plano do quadro")]
        [SerializeField] private float frameOffset = 0.01f;

        [Header("Aparência")]
        [Tooltip("Material da moldura. Se vazio, cria um material dourado automático.")]
        [SerializeField] private Material frameMaterial;

        [Tooltip("Cor da moldura quando frameMaterial é vazio")]
        [SerializeField] private Color frameColor = new Color(0.38f, 0.26f, 0.08f);

        [Tooltip("Metallic da moldura automática (0=fosco, 1=espelho)")]
        [SerializeField][Range(0f, 1f)] private float frameMetallic = 0.65f;

        [Tooltip("Smoothness da moldura automática")]
        [SerializeField][Range(0f, 1f)] private float frameSmoothness = 0.75f;

        // ─── Ciclo de Vida ────────────────────────────────────────────────────

        private void Start()
        {
            if (transform.Find("Frame_Top") == null)
                BuildFrame();
        }

        // ─── Construção da Moldura ────────────────────────────────────────────

        [ContextMenu("Construir Moldura")]
        public void BuildFrame()
        {
            RemoveExistingFrame();

            Renderer rend = GetComponent<Renderer>();
            // Usa o tamanho local do sprite/plano (não os bounds mundiais)
            Vector3 localSize = GetLocalSize(rend);
            float w = localSize.x;
            float h = localSize.y;
            float t = frameThickness;
            float d = frameDepth;
            float z = frameOffset + d * 0.5f; // posição Z local (à frente do quadro)

            Material mat = GetOrCreateMaterial();

            // As 4 barras da moldura
            CreateBar("Frame_Top",    new Vector3(0,           h * 0.5f + t * 0.5f, z), new Vector3(w + t * 2f, t, d), mat);
            CreateBar("Frame_Bottom", new Vector3(0,          -h * 0.5f - t * 0.5f, z), new Vector3(w + t * 2f, t, d), mat);
            CreateBar("Frame_Left",   new Vector3(-w * 0.5f - t * 0.5f, 0,          z), new Vector3(t, h, d), mat);
            CreateBar("Frame_Right",  new Vector3( w * 0.5f + t * 0.5f, 0,          z), new Vector3(t, h, d), mat);
        }

        [ContextMenu("Remover Moldura")]
        public void RemoveExistingFrame()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child.name.StartsWith("Frame_"))
                    DestroyImmediate(child.gameObject);
            }
        }

        // ─── Helpers ──────────────────────────────────────────────────────────

        private void CreateBar(string barName, Vector3 localPos, Vector3 localScale, Material mat)
        {
            GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = barName;
            bar.transform.SetParent(transform, false);
            bar.transform.localPosition = localPos;
            bar.transform.localRotation = Quaternion.identity;
            bar.transform.localScale    = localScale;
            bar.GetComponent<MeshRenderer>().sharedMaterial = mat;

            // Moldura não precisa de colisão
            DestroyImmediate(bar.GetComponent<BoxCollider>());
        }

        private Material GetOrCreateMaterial()
        {
            if (frameMaterial != null) return frameMaterial;

            Material mat = new Material(Shader.Find("Standard"));
            mat.name = "FrameMaterial_Auto";
            mat.color = frameColor;
            mat.SetFloat("_Metallic",    frameMetallic);
            mat.SetFloat("_Glossiness",  frameSmoothness);
            return mat;
        }

        private static Vector3 GetLocalSize(Renderer rend)
        {
            // Para Quad/Plane: usa a escala do transform como proxy
            Vector3 s = rend.transform.localScale;
            return new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Reconstrói preview ao alterar valores no Inspector
            if (Application.isPlaying) return;
            if (transform.Find("Frame_Top") != null)
                BuildFrame();
        }
#endif
    }
}
