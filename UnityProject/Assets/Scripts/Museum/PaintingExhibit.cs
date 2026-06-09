using UnityEngine;

namespace MuseumModerna
{
    /// <summary>
    /// Componente que deve ser adicionado ao GameObject de cada quadro na cena.
    /// Associa o objeto 3D do quadro com seus dados (PaintingInfo ScriptableObject).
    ///
    /// Como usar:
    ///   1. Adicione este componente ao GameObject do quadro (ex: Quadro_Abaporu).
    ///   2. Coloque o quadro na Layer "Painting" (Layer 8).
    ///   3. Adicione um BoxCollider ao quadro para detecção por OverlapSphere.
    ///   4. Arraste o arquivo .asset correspondente para o campo paintingData.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class PaintingExhibit : MonoBehaviour
    {
        [Tooltip("ScriptableObject com as informações deste quadro (título, artista, etc)")]
        [SerializeField] private PaintingInfo paintingData;

        /// <summary>Dados deste quadro (título, artista, descrição, etc).</summary>
        public PaintingInfo PaintingData => paintingData;

        private void Awake()
        {
            if (paintingData == null)
            {
                Debug.LogWarning($"[MuseumModerna] PaintingExhibit em '{gameObject.name}' não tem PaintingData definido!", this);
            }

            // Garante que o quadro esteja na layer correta
            if (gameObject.layer == 0) // Default layer
            {
                Debug.LogWarning($"[MuseumModerna] '{gameObject.name}' está na Default layer. Configure para a layer 'Painting'.", this);
            }
        }

        // Aplica a textura da pintura ao material do quadro automaticamente no Start
        private void Start()
        {
            if (paintingData != null && paintingData.paintingTexture != null)
            {
                Renderer rend = GetComponent<Renderer>();
                if (rend != null && rend.material != null)
                {
                    rend.material.mainTexture = paintingData.paintingTexture;
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Mostra um label com o título da obra no Editor
            if (paintingData != null)
            {
                UnityEditor.Handles.Label(
                    transform.position + Vector3.up * 1.5f,
                    paintingData.title,
                    new GUIStyle { normal = { textColor = Color.yellow }, fontStyle = FontStyle.Bold }
                );
            }
        }
#endif
    }
}
