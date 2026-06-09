using UnityEngine;

namespace MuseumModerna
{
    /// <summary>
    /// ScriptableObject com todas as informações de um quadro do museu.
    /// Crie um arquivo de dados para cada obra via:
    ///   Assets → Create → Museum → Painting Info
    ///
    /// Exemplo de uso no Editor:
    ///   1. Botão direito na pasta Resources/PaintingData
    ///   2. Create → Museum → Painting Info
    ///   3. Preencha os dados e arraste para o componente PaintingExhibit
    /// </summary>
    [CreateAssetMenu(fileName = "Painting_NomeDaObra", menuName = "Museum/Painting Info", order = 0)]
    public class PaintingInfo : ScriptableObject
    {
        // ─── Identificação da Obra ────────────────────────────────────────────

        [Header("Identificação")]
        [Tooltip("Título completo da obra de arte")]
        public string title;

        [Tooltip("Nome completo do artista")]
        public string artist;

        [Tooltip("Ano de criação da obra")]
        public int year;

        [Tooltip("Movimento artístico ou estilo (ex: Modernismo, Expressionismo)")]
        public string movement;

        // ─── Conteúdo ─────────────────────────────────────────────────────────

        [Header("Conteúdo")]
        [Tooltip("Descrição detalhada da obra — aparece no painel de informações")]
        [TextArea(4, 10)]
        public string description;

        [Tooltip("Curiosidade ou fato histórico sobre a obra (opcional)")]
        [TextArea(2, 5)]
        public string funFact;

        // ─── Mídia ────────────────────────────────────────────────────────────

        [Header("Mídia")]
        [Tooltip("Textura da pintura usada no plano 3D na cena")]
        public Texture2D paintingTexture;

        [Tooltip("Sprite da miniatura para exibição na UI (pode ser igual a paintingTexture)")]
        public Sprite thumbnailSprite;

        [Tooltip("Áudio narrado sobre a obra (opcional — guia de áudio)")]
        public AudioClip audioGuide;

        [Tooltip("Duração do áudio em segundos (preenchido automaticamente se audioGuide != null)")]
        public float audioDuration;

        // ─── Metadados ────────────────────────────────────────────────────────

        [Header("Metadados")]
        [Tooltip("ID único da obra — use para rastrear progresso do visitante")]
        public string uniqueId;

        [Tooltip("Posição sugerida de visualização na cena (relativa ao quadro)")]
        public Vector3 suggestedViewOffset = new Vector3(0f, 0f, 2f);

        // ─── Validação no Editor ──────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Preenche audioDuration automaticamente ao atribuir um AudioClip
            if (audioGuide != null)
            {
                audioDuration = audioGuide.length;
            }

            // Gera um ID único baseado no título se ainda não houver
            if (string.IsNullOrEmpty(uniqueId) && !string.IsNullOrEmpty(title))
            {
                uniqueId = title.ToLower().Replace(" ", "_").Replace("ã", "a").Replace("ç", "c");
            }
        }
#endif

        // ─── Utilitários ──────────────────────────────────────────────────────

        /// <summary>
        /// Retorna o cabeçalho formatado para exibição na UI.
        /// Exemplo: "O Abaporu (1928) — Tarsila do Amaral"
        /// </summary>
        public string GetDisplayHeader()
        {
            return $"{title} ({year}) — {artist}";
        }

        /// <summary>
        /// Retorna verdadeiro se esta obra possui guia de áudio disponível.
        /// </summary>
        public bool HasAudioGuide => audioGuide != null;
    }
}
